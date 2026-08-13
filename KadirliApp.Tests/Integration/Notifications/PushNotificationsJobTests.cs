using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Jobs;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KadirliApp.Tests.Integration.Notifications;

/// <summary>
/// Faz 10.11 doğrulaması: SendPushNotificationsJob. No-op sağlayıcıda (IsConfigured=false) hiç göndermez ve DB'ye
/// dokunmaz; gerçek sağlayıcıda fcm_sent/fcm_sent_at/fcm_error doğru yazılır ve UNREGISTERED token temizlenir.
/// </summary>
public class PushNotificationsJobTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    /// <summary>Bu sınıfın ürettiği kullanıcıların telefon öneki — temizliğin kapsamı budur.</summary>
    private const string PhonePrefix = "+90509999";

    private readonly CustomWebApplicationFactory _factory;

    public PushNotificationsJobTests(CustomWebApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => CleanAsync();

    public Task DisposeAsync() => CleanAsync();

    /// <summary>
    /// 🧹 <b>T1 (Faz 0 denetimi):</b> bu sınıf 12.15b'de "kalıcı <c>users</c> satırı bırakan
    /// dört sınıftan biri" olarak işaretlenmişti ve tek temizliği bile yoktu.
    ///
    /// <para>
    /// Biriken test kullanıcıları sayfalı panel listelerini kaydırır ve <b>kendisiyle
    /// ilgisiz</b> testleri kırar (12.15b'de seed'deki süper admin kullanıcı listesinin ilk
    /// sayfasından düştü ve <c>PanelUsabilityTests</c> kırmızıya döndü). Denetimin bozma
    /// turlarının güvenilir olması da buna bağlı: kırmızıya dönen bir test, bozduğumuz kural
    /// yüzünden mi yoksa satır sayısı yüzünden mi kırıldı ayırt edilemezse ölçüm işe yaramaz.
    /// </para>
    ///
    /// <para>
    /// ⚠️ Kapsam <b>dar</b>: yalnız bu sınıfın telefon öneki. Geniş bir temizlik başka bir
    /// testin kurulumunu götürür ve onun iddiasını <b>iddiasız</b> bırakır — 12.15b'de
    /// birebir yaşandı. ⚠️ Sıra da önemli: bildirimler kullanıcıya FK ile bağlı, önce onlar.
    /// </para>
    /// </summary>
    private async Task CleanAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userIds = await db.Users.IgnoreQueryFilters()
            .Where(u => u.Phone.StartsWith(PhonePrefix))
            .Select(u => u.Id)
            .ToListAsync();
        if (userIds.Count == 0) return;

        await db.Notifications.Where(n => userIds.Contains(n.UserId)).ExecuteDeleteAsync();
        await db.Users.IgnoreQueryFilters().Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync();
    }

    /// <summary>Test için IPushService sahtesi: belirli token'ları başarısız/geçersiz işaretler, gönderileni kaydeder.</summary>
    private sealed class FakePushService : IPushService
    {
        private readonly bool _configured;
        private readonly HashSet<string> _unregisteredTokens;
        public List<PushMessage> Sent { get; } = new();
        public int SendCallCount { get; private set; }

        public FakePushService(bool configured, params string[] unregisteredTokens)
            => (_configured, _unregisteredTokens) = (configured, unregisteredTokens.ToHashSet());

        public bool IsConfigured => _configured;

        public Task<IReadOnlyList<PushResult>> SendAsync(IReadOnlyList<PushMessage> messages, CancellationToken ct = default)
        {
            SendCallCount++;
            Sent.AddRange(messages);
            IReadOnlyList<PushResult> results = messages
                .Select(m => _unregisteredTokens.Contains(m.Token)
                    ? PushResult.Failed("Unregistered", tokenInvalid: true)
                    : PushResult.Ok())
                .ToList();
            return Task.FromResult(results);
        }
    }

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(db);
    }

    private SendPushNotificationsJob NewJob(AppDbContext db, IPushService push)
        => new(db, push, NullLogger<SendPushNotificationsJob>.Instance);

    [Fact]
    public async Task NoOpProvider_DoesNothing_AndLeavesRowsUnsent()
    {
        var (userId, notifId) = await InDbAsync(async db =>
        {
            var user = new User { Id = Guid.NewGuid(), Phone = "+905099990101", Role = UserRole.User, FcmToken = "noop-token-101" };
            db.Users.Add(user);
            var n = new Notification { UserId = user.Id, Title = "CLAUDE-TEST NoOp", Body = "gövde", Type = "announcement" };
            db.Notifications.Add(n);
            await db.SaveChangesAsync();
            return (user.Id, n.Id);
        });

        var push = new FakePushService(configured: false);
        await InDbAsync(async db => { await NewJob(db, push).RunAsync(); return 0; });

        push.SendCallCount.Should().Be(0, "IsConfigured=false iken sorgu bile atılmamalı");
        await InDbAsync(async db =>
        {
            var n = await db.Notifications.AsNoTracking().FirstAsync(x => x.Id == notifId);
            n.FcmSent.Should().BeFalse();
            n.FcmSentAt.Should().BeNull();
            return 0;
        });
    }

    [Fact]
    public async Task ConfiguredProvider_MarksSent_WritesError_And_ClearsUnregisteredToken()
    {
        const string goodToken = "good-token-202";
        const string badToken = "bad-token-203";

        var seeded = await InDbAsync(async db =>
        {
            var goodUser = new User { Id = Guid.NewGuid(), Phone = "+905099990201", Role = UserRole.User, FcmToken = goodToken };
            var badUser = new User { Id = Guid.NewGuid(), Phone = "+905099990202", Role = UserRole.User, FcmToken = badToken };
            var noTokenUser = new User { Id = Guid.NewGuid(), Phone = "+905099990203", Role = UserRole.User, FcmToken = null };
            db.Users.AddRange(goodUser, badUser, noTokenUser);

            var okNotif = new Notification { UserId = goodUser.Id, Title = "CLAUDE-TEST OK", Body = "gövde", Type = "announcement", RelatedId = Guid.NewGuid(), RelatedType = "announcement" };
            var badNotif = new Notification { UserId = badUser.Id, Title = "CLAUDE-TEST BAD", Body = "gövde" };
            var noTokenNotif = new Notification { UserId = noTokenUser.Id, Title = "CLAUDE-TEST NOTOKEN", Body = "gövde" };
            db.Notifications.AddRange(okNotif, badNotif, noTokenNotif);
            await db.SaveChangesAsync();
            return new { goodUser.Id, badUserId = badUser.Id, okNotif = okNotif.Id, badNotif = badNotif.Id, noTokenNotif = noTokenNotif.Id };
        });

        var push = new FakePushService(configured: true, unregisteredTokens: badToken);
        await InDbAsync(async db => { await NewJob(db, push).RunAsync(); return 0; });

        // Token'lı iki bildirim gönderildi; token'sız hiç gönderilmedi (veri yükünde notificationId var)
        push.Sent.Select(m => m.Token).Should().Contain(new[] { goodToken, badToken }).And.NotContain((string?)null);
        push.Sent.Should().OnlyContain(m => m.Data != null && m.Data.ContainsKey("notificationId"));

        // 🔴 §7 madde 16 — anahtar ADLARI kontrat. Faz 0 (B1) öncesinde yalnız yukarıdaki
        // "notificationId var mı" iddiası vardı; `relatedType` yeniden adlandırılsa deep-link
        // ölür ve HİÇBİR test kırmızıya dönmezdi. Aşağıda anahtarlar **düz metin** yazılı:
        // sabiti (PushDataKeys) yeniden adlandırmak bu testi kurtarmaz, çünkü test sabite
        // değil mobilin okuduğu dizenin kendisine bakıyor (push_messaging.dart).
        var okMessage = push.Sent.Single(m => m.Data!["notificationId"] == seeded.okNotif.ToString());
        okMessage.Data!.Keys.Should().BeEquivalentTo(
            new[] { "notificationId", "type", "relatedId", "relatedType" },
            "mobil `PushPayload.fromData` tam olarak bu dört anahtarı okuyor (§7 madde 16); " +
            "biri yeniden adlandırılırsa kullanıcı bildirime dokunur, hiçbir yere gitmez, hata da almaz");
        okMessage.Data["type"].Should().Be("announcement");
        okMessage.Data["relatedType"].Should().Be("announcement",
            "mobil rotayı bu değerden üretiyor (§7 madde 18)");
        okMessage.Data["relatedId"].Should().NotBeNullOrWhiteSpace();
        okMessage.Data["relatedId"].Should().NotBe(Guid.Empty.ToString(),
            "hedef kimliği yazılamıyorsa istemci boş sayfaya gider");

        // Alanı boş olan bildirimde anahtar HİÇ yazılmaz (null string olarak değil):
        // FCM `data`'sı Map<String,String>'tir, "null" dizesi istemcide geçerli bir kimlik sanılırdı.
        push.Sent.Single(m => m.Data!["notificationId"] == seeded.badNotif.ToString())
            .Data!.Keys.Should().Equal(new[] { "notificationId" },
                "türü/hedefi olmayan bildirimde diğer anahtarlar yazılmaz");

        await InDbAsync(async db =>
        {
            var ok = await db.Notifications.AsNoTracking().FirstAsync(x => x.Id == seeded.okNotif);
            ok.FcmSent.Should().BeTrue();
            ok.FcmSentAt.Should().NotBeNull();
            ok.FcmError.Should().BeNull();

            var bad = await db.Notifications.AsNoTracking().FirstAsync(x => x.Id == seeded.badNotif);
            bad.FcmSent.Should().BeTrue();
            bad.FcmSentAt.Should().BeNull("başarısız gönderimde iletim zamanı yazılmaz");
            bad.FcmError.Should().Be("Unregistered");

            // UNREGISTERED → geçersiz token temizlendi
            (await db.Users.AsNoTracking().FirstAsync(u => u.Id == seeded.badUserId)).FcmToken.Should().BeNull();

            // Token'sız kullanıcının bildirimi sorguya girmedi → hâlâ gönderilmemiş
            var noTok = await db.Notifications.AsNoTracking().FirstAsync(x => x.Id == seeded.noTokenNotif);
            noTok.FcmSent.Should().BeFalse();
            return 0;
        });
    }
}
