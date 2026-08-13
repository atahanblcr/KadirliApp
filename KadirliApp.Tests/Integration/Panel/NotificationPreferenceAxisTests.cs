using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Notifications;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.15b — <b>bildirim tercihi artık KAYNAĞA GÖRE seçiliyor.</b>
/// </summary>
/// <remarks>
/// 🔴 <b>Bu dosyanın kapattığı hata 12.15'in bıraktığı sessiz bir delikti:</b>
/// <c>NotificationDispatcher</c> <b>her kaynağı</b> <c>NotificationPreferences.Announcements</c>'a
/// bağlıyordu ve haber gönderimi eklendiği an iki yönlü hasar doğdu:
/// <list type="number">
///   <item>"Duyurular"ı kapatan kullanıcı <b>haberleri de</b> kaybediyordu; ayar ekranı
///         bunu hiçbir yerde söylemiyordu.</item>
///   <item>Daha kötüsü tersi: haber push'u istemeyen kullanıcının <b>tek çıkışı</b>
///         "Duyurular"ı kapatmaktı — o da §7 madde 41 gereği <b>kesinti bildirimini</b>
///         öldürüyordu. 12.15'in "elle gönderim" gerekçesinin (<i>bildirim yorgunluğu →
///         kullanıcı hepsini kapatır → kesintiyi de almaz</i>) korktuğu senaryo, tek
///         anahtarla ulaşılabilir durumdaydı.</item>
/// </list>
/// </remarks>
[Collection(PanelCollection.Name)]
public class NotificationPreferenceAxisTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;

    private const string Marker = "CLAUDE-PREFAXIS";

    private Guid _all;         // her şey açık
    private Guid _noNews;      // haber KAPALI, duyuru açık
    private Guid _noAnnounce;  // duyuru KAPALI, haber açık

    public NotificationPreferenceAxisTests(WebPanelApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await CleanAsync();
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            _all = await EnsureUserAsync(db, "+905550000961", announcements: true, news: true);
            _noNews = await EnsureUserAsync(db, "+905550000962", announcements: true, news: false);
            _noAnnounce = await EnsureUserAsync(db, "+905550000963", announcements: false, news: true);
        });
    }

    public Task DisposeAsync() => CleanAsync();

    // ────────────────────────────────────────────────────────────────────────
    // 1) İki eksen GERÇEKTEN ayrı
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NewsGoesToNewsSubscribers_AnnouncementsGoToAnnouncementSubscribers()
    {
        var newsRecipients = await DispatchAsync(PushCampaignSources.News, "haber");
        var announceRecipients = await DispatchAsync(PushCampaignSources.Announcement, "duyuru");

        newsRecipients.Should().Contain(_all);
        newsRecipients.Should().Contain(_noAnnounce, "duyuruyu kapatmak haberi kapatmaz");
        newsRecipients.Should().NotContain(_noNews);

        announceRecipients.Should().Contain(_all);
        announceRecipients.Should().Contain(_noNews, "haberi kapatmak duyuruyu kapatmaz");
        announceRecipients.Should().NotContain(_noAnnounce);
    }

    /// <summary>
    /// 🔴 <b>Kesinti bildirimi DUYURU eksenine bağlı KALMALI</b> (§7 madde 41).
    /// </summary>
    /// <remarks>
    /// Kesinti bildirimi ayrı bir tür değil, bir <c>Announcement</c>'tır. Kendi eksenine
    /// taşınsaydı bugün kesinti bildirimi alan kullanıcıların bir kısmı — hiçbir tercih
    /// değiştirmeden — <b>sessizce</b> susardı; ve bu modülde susan bildirim, vatandaşın
    /// elektriğinin ne zaman kesileceğini öğrenememesi demek.
    /// </remarks>
    [Fact]
    public async Task PowerOutage_StaysOnTheAnnouncementAxis()
    {
        var recipients = await DispatchAsync(PushCampaignSources.PowerOutage, "kesinti");

        recipients.Should().Contain(_noNews, "haberi kapatan kesintiyi almaya devam eder");
        recipients.Should().NotContain(_noAnnounce);
    }

    [Fact]
    public async Task ManualSend_StaysOnTheAnnouncementAxis()
    {
        var recipients = await DispatchAsync(PushCampaignSources.Manual, "elle");

        recipients.Should().Contain(_noNews);
        recipients.Should().NotContain(_noAnnounce);
    }

    /// <summary>
    /// Bilinmeyen kaynak <b>bugünkü davranışa</b> (duyuru) düşer, "süzme"ye değil.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu, §5'in *"bilinmeyen değer varsayılana düşer, listeyi boşaltmaz"* kuralının
    /// <b>tersi</b> ve bilinçli: orada bedel bir kaydın görünmemesi, burada bedel
    /// <b>tercihini kapatmış birine bildirim göndermek</b>.
    /// </remarks>
    [Fact]
    public async Task AnUnknownSource_FallsBackToTheAnnouncementAxis()
    {
        // ⚠️ `push_campaigns.source` varchar(20) — uydurma kaynak da sığmak zorunda.
        var recipients = await DispatchAsync("gelecek_kaynak", "bilinmeyen");

        recipients.Should().Contain(_noNews);
        recipients.Should().NotContain(_noAnnounce);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 2) Önizleme = gönderim (§7 madde 38)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Önizleme <b>kaynağı</b> söylemek zorunda; söylemezse duyuru eksenine göre sayar.
    /// </summary>
    [Fact]
    public async Task TheEstimateMatchesTheActualSend_PerSource()
    {
        var estimate = await EstimateAsync(PushCampaignSources.News);
        var actual = await DispatchAsync(PushCampaignSources.News, "sayim");

        actual.Count.Should().Be(estimate);

        // 🐛 İlk yazımda buraya "iki eksenin SAYILARI farklı olmalı" yazılmıştı ve test
        // kırıldı — haklı olarak: paylaşılan veritabanındaki diğer kullanıcılar her iki
        // eksende de abone olduğu için sayılar tesadüfen eşitlenebiliyor (4 = 4). Sayı
        // iddiası bu ortamda hem kırılgan hem zayıf; ayrımı KÜMEYLE kurmak gerekiyor.
        var announceRecipients = await DispatchAsync(PushCampaignSources.Announcement, "sayim2");

        actual.Should().Contain(_noAnnounce).And.NotContain(_noNews);
        announceRecipients.Should().Contain(_noNews).And.NotContain(_noAnnounce);
    }

    // ────────────────────────────────────────────────────────────────────────
    // 3) 🔬 ÖLÇÜM: anahtarsız JSON "abone" olarak materyalize oluyor mu?
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔬 <b>ÖLÇÜLDÜ: anahtarsız JSON <c>false</c> materyalize oluyor</b> — ve tam bu
    /// yüzden bir geri doldurma migration'ı var.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Bu testin ilk hâli yanlış bir iddia kuruyordu</b> ve kırılması bu oturumun en
    /// değerli anıydı: <c>public bool News { get; set; } = true;</c> yazdığımız için
    /// anahtarsız bir JSON'un <c>true</c> okunacağı <i>varsayılmıştı</i>. Gerçek Postgres'te
    /// ölçüldüğünde <c>false</c> çıktı — EF'in JSON materyalizasyonu varsayılan başlatıcıyı
    /// <b>çalıştırmıyor</b>.
    ///
    /// Varsayıma güvenilseydi 12.15b, mevcut <b>bütün kullanıcıları</b> haber bildiriminden
    /// sessizce çıkarırdı: uçlar 200 döner, kampanya satırı yine açılır, hiçbir hata oluşmaz.
    /// Bu yüzden test <b>silinmedi ya da beklentisi çevrilmedi</b> — ölçümü <b>belge</b>
    /// hâline getirip yanına geri doldurmanın kanıtını koyduk. Biri yarın migration'ı
    /// "gereksiz" diye kaldırırsa, buradaki iki test birlikte sebebini anlatır.
    /// </remarks>
    [Fact]
    public async Task MissingJsonKey_MaterialisesAsFalse()
    {
        var phone = "+905550000964";
        Guid userId = Guid.Empty;

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            userId = await EnsureUserAsync(db, phone, announcements: true, news: true);

            // 12.15b ÖNCESİNİN gerçek satırı (canlıdan kopyalandı): `News` anahtarı yok.
            // ⚠️ JSON PARAMETRE olarak gider: `ExecuteSqlRaw` gövdeyi `string.Format` gibi
            // okuyor ve JSON'un süslü parantezleri onu bozuyor.
            const string legacyJson =
                "{\"Ads\":false,\"Deaths\":true,\"Events\":true,\"Pharmacy\":true," +
                "\"Campaigns\":false,\"Announcements\":true}";

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE users SET notification_preferences = CAST({0} AS jsonb) WHERE id = {1}",
                legacyJson, userId);
        });

        var preferences = await InDbAsync(db => db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.NotificationPreferences)
            .FirstAsync());

        preferences.News.Should().BeFalse(
            "EF'in JSON materyalizasyonu varsayılan başlatıcıyı ÇALIŞTIRMIYOR — geri doldurmanın var olma sebebi bu");

        // Diğer anahtarlar okunuyor: sorun "JSON hiç okunmuyor" değil, EKSİK anahtar.
        preferences.Announcements.Should().BeTrue();
    }

    /// <summary>
    /// 🔴 <b>Geri doldurma gerçekten koştu:</b> hiçbir kullanıcı satırında <c>News</c>
    /// anahtarı eksik değil.
    /// </summary>
    /// <remarks>
    /// Yukarıdaki ölçümün doğrudan sonucu. Bu iddia şemaya değil <b>veriye</b> bakıyor.
    /// ⚠️ Sorgu ham SQL: global soft-delete süzgeci uygulanmamalı — silinmiş bir kullanıcı
    /// geri getirildiğinde anahtarsız dönerdi.
    ///
    /// 🐛 <b>DÜRÜST NOT — bu testin iddiası SINIRLI ve bozma turunda ölçüldü.</b>
    /// Migration'ın <c>Up()</c>'ı boşaltıldı ve test <b>yeşil kaldı</b>.
    ///
    /// 🔬 <b>SEBEP DÜZELTİLDİ (Faz 0 — T2, 13 Ağu 2026).</b> Burada önce *"migration bir kez
    /// koşar ve test veritabanı koşular arasında yeniden kullanılıyor"* yazıyordu — <b>yanlış</b>.
    /// Ölçüldü: <c>WebPanelApplicationFactory</c> her koşuda <b>yeni</b> bir Testcontainers
    /// konteyneri kurar (<c>WithReuse</c> yok) ve migration'lar her koşuda baştan uygulanır.
    /// Gerçek sebep başka: migration <b>boş</b> bir <c>users</c> tablosunda koşar, satırları
    /// sonradan EF yazar ve EF her zaman <b>tam</b> JSON yazar — yani <i>anahtarsız bir satır
    /// test ortamında hiç doğmaz</i> ve bu iddia <b>tanım gereği vakumdur</b>.
    ///
    /// 🔑 Ayrım önemliydi, çünkü yanlış sebep yanlış çözüme götürüyordu: *"ayrı, tek
    /// kullanımlık bir veritabanı"* seçeneği <b>işe yaramazdı</b> (sıfırdan kurulan bir
    /// veritabanında da eski biçimli satır yoktur). Doğru çözüm ifadeyi <b>dışarı almak</b>
    /// oldu → <see cref="TheBackfillStatement_AddsTheMissingKey_ButNeverOverwritesAnExplicitChoice"/>
    /// eski biçimli satırı kendi eliyle üretip aynı metni koşturuyor. Burası bir <b>duman
    /// testi</b> olarak kalıyor (yeni kurulan gerçek bir ortamda değeri var) ama "kilitli"
    /// sayılmıyor.
    /// </remarks>
    [Fact]
    public async Task TheBackfill_LeftNoUserRowWithoutTheNewsKey()
    {
        var missing = await InDbAsync(async db =>
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT count(*) FROM users WHERE NOT (notification_preferences ? 'News');";
            await db.Database.OpenConnectionAsync();
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        });

        missing.Should().Be(0);
    }

    /// <summary>
    /// 🔑 <b>Faz 0 denetimi (T2) — geri doldurmanın GERÇEK kilidi.</b>
    ///
    /// <para>
    /// Yukarıdaki duman testi bir migration regresyonunu yakalayamıyor ve <b>sebebi ölçüldü</b>:
    /// migration <b>boş</b> bir <c>users</c> tablosunda koşar, satırları sonradan EF yazar ve
    /// EF her zaman <b>tam</b> JSON yazar — yani anahtarsız bir satır test ortamında
    /// <b>hiç doğmaz</b>. (⚠️ Eski gerekçe *"test veritabanı koşular arasında yeniden
    /// kullanılıyor"*du ve <b>yanlıştı</b>: Testcontainers her koşuda sıfırdan kurar,
    /// migration'lar baştan uygulanır. Aynı gözlemin iki sebebi vardı, yanlış olanı
    /// seçilmişti — ve yanlış sebep yanlış çözüme götürüyordu: "ayrı, tek kullanımlık
    /// veritabanı" <b>işe yaramazdı</b>, çünkü sıfırdan kurulan bir veritabanında da eski
    /// biçimli satır yoktur.)
    /// </para>
    ///
    /// <para>
    /// Bu test eksik parçayı sağlar: eski biçimli satırı <b>kendi eliyle</b> üretir ve
    /// migration'ın koşturduğu <b>aynı metni</b>
    /// (<see cref="NotificationPreferenceBackfill.Statement"/>) koşturur. Böylece kilitlenen
    /// şey "migration çalıştı mı" değil, <b>ifadenin doğru olup olmadığıdır</b> — asıl
    /// kırılgan yer de burası:
    /// </para>
    ///
    /// <list type="number">
    ///   <item>eksik anahtar <b>tamamlanmalı</b> (yoksa mevcut kullanıcılar sessizce
    ///         haber bildiriminden düşer),</item>
    ///   <item><b>açık tercih EZİLMEMELİ</b> — *"haber bildirimi istemiyorum"* diyen bir
    ///         kullanıcının tercihi geri doldurmadan sağ çıkmalı; ezilseydi tercih sessizce
    ///         <b>açılırdı</b>, hiçbir hata vermeden.</item>
    /// </list>
    ///
    /// <para>
    /// 🔬 <b>Bozma turunun ölçtüğü (dürüst not):</b> ikinci iddiayı <b>iki</b> mekanizma
    /// birden koruyor — <c>WHERE NOT (… ? 'News')</c> ve <c>||</c> operand sırası — ve
    /// <b>yalnız birini</b> bozmak testi <b>yeşil bırakıyor</b>, çünkü davranış değişmiyor.
    /// İkisi birden bozulduğunda kırmızıya dönüyor (ölçüldü). Bu yüzden iddia bilerek
    /// <b>davranış</b> olarak yazıldı ("tercih sağ çıkar"), gerçeklemenin şekli olarak değil:
    /// SQL'in nasıl yazıldığını iddia eden bir test, derinlemesine savunmanın bir ayağını
    /// kaldıran zararsız bir düzenlemede de kırmızıya döner ve yanlış şeyi kilitler.
    /// İlk iddia (eksik anahtar tamamlanır) ise <b>tek başına</b> kilitli: ifade
    /// etkisizleştirildiğinde kırmızıya döndü.
    /// </para>
    /// </summary>
    [Fact]
    public async Task TheBackfillStatement_AddsTheMissingKey_ButNeverOverwritesAnExplicitChoice()
    {
        var legacyId = Guid.NewGuid();   // anahtarı HİÇ olmayan satır (12.15b öncesinin hâli)
        var optedOutId = Guid.NewGuid(); // "haber istemiyorum" demiş satır (News: false)

        await InDbAsync(async db =>
        {
            await db.Database.OpenConnectionAsync();

            // ⚠️ JSON literali ExecuteSqlRaw'a verilemez ('{' yer tutucu sanılır) → ham komut.
            await using (var seed = db.Database.GetDbConnection().CreateCommand())
            {
                // ⚠️ `$$"""…"""`: JSON'un `{` karakteri düz metin kalsın diye enterpolasyon
                // ayracı `{{…}}`'ye çekildi. Tek `$` ile yazılsaydı `{"Announcements"` bir
                // ifade sanılır ve dosya DERLENMEZ (ilk yazımda tam bu oldu).
                seed.CommandText = $$"""
                    INSERT INTO users (id, phone, role, notification_preferences, created_at, updated_at, is_active, is_banned)
                    VALUES
                      ('{{legacyId}}',   '+905550000965', 'user', '{"Announcements": true}'::jsonb,                now(), now(), true, false),
                      ('{{optedOutId}}', '+905550000966', 'user', '{"Announcements": true, "News": false}'::jsonb, now(), now(), true, false);
                    """;
                await seed.ExecuteNonQueryAsync();
            }

            // Migration'ın koşturduğu METNİN TA KENDİSİ.
            await using (var backfill = db.Database.GetDbConnection().CreateCommand())
            {
                backfill.CommandText = NotificationPreferenceBackfill.Statement;
                await backfill.ExecuteNonQueryAsync();
            }
            return 0;
        });

        try
        {
            var (legacy, optedOut) = await InDbAsync(async db =>
            {
                await db.Database.OpenConnectionAsync();
                await using var read = db.Database.GetDbConnection().CreateCommand();
                read.CommandText =
                    $"SELECT id, notification_preferences ->> 'News' FROM users WHERE id IN ('{legacyId}', '{optedOutId}');";
                await using var reader = await read.ExecuteReaderAsync();

                string? legacyValue = null, optedOutValue = null;
                while (await reader.ReadAsync())
                {
                    var id = reader.GetGuid(0);
                    var value = reader.IsDBNull(1) ? null : reader.GetString(1);
                    if (id == legacyId) legacyValue = value; else optedOutValue = value;
                }
                return (legacyValue, optedOutValue);
            });

            legacy.Should().Be("true",
                "anahtarı olmayan satıra varlığın beyan ettiği varsayılan yazılmalı — " +
                "yoksa mevcut bütün kullanıcılar haber bildiriminden SESSİZCE düşer (§7 madde 67)");

            optedOut.Should().Be("false",
                "açık tercih EZİLMEMELİ: '||' çakışan anahtarda SAĞDAKİ operandı seçer, bu yüzden " +
                "mevcut değer sağda durmalı. Operandlar ters yazılsaydı geri doldurma, haber " +
                "bildirimi istemeyen herkesin tercihini sessizce AÇARDI");
        }
        finally
        {
            // ⚠️ Temizlik YALNIZ kendi satırlarını kapsar (12.15b'nin dersi: geniş bir
            // onarım/temizlik başka bir testin iddiasını iddiasız bırakır).
            await InDbAsync(async db =>
            {
                await db.Database.OpenConnectionAsync();
                await using var cleanup = db.Database.GetDbConnection().CreateCommand();
                cleanup.CommandText = $"DELETE FROM users WHERE id IN ('{legacyId}', '{optedOutId}');";
                await cleanup.ExecuteNonQueryAsync();
                return 0;
            });
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Yardımcılar
    // ────────────────────────────────────────────────────────────────────────

    private async Task<System.Collections.Generic.List<Guid>> DispatchAsync(string source, string tag)
    {
        System.Collections.Generic.List<Guid> ids = null!;
        await _factory.WithScopeAsync(async sp =>
        {
            var dispatcher = sp.GetRequiredService<INotificationDispatcher>();
            var result = await dispatcher.DispatchAsync(new PushDispatchRequest(
                Title: $"{Marker}-{tag}",
                Body: "gövde",
                TargetType: PushTargetTypes.All,
                NeighborhoodIds: null,
                Source: source));

            var db = sp.GetRequiredService<AppDbContext>();
            ids = await db.Notifications.AsNoTracking()
                .Where(n => n.CampaignId == result.CampaignId)
                .Select(n => n.UserId)
                .ToListAsync();
        });
        return ids;
    }

    private async Task<int> EstimateAsync(string source)
    {
        var count = 0;
        await _factory.WithScopeAsync(async sp =>
            count = await sp.GetRequiredService<INotificationDispatcher>()
                .EstimateRecipientsAsync(PushTargetTypes.All, null, source));
        return count;
    }

    private static async Task<Guid> EnsureUserAsync(
        AppDbContext db, string phone, bool announcements, bool news)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Phone == phone);
        if (user is null)
        {
            user = new User { Phone = phone, Role = UserRole.User };
            db.Users.Add(user);
        }

        user.IsActive = true;
        user.IsBanned = false;
        user.DeletedAt = null;
        user.NotificationPreferences = new NotificationPreferences
        {
            Announcements = announcements,
            News = news
        };

        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<T> InDbAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        T result = default!;
        await _factory.WithScopeAsync(async sp =>
            result = await action(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    private Task CleanAsync() => _factory.WithScopeAsync(async sp =>
    {
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Notifications.Where(n => n.Title.StartsWith(Marker)).ExecuteDeleteAsync();
        await db.PushCampaigns.Where(c => c.Title.StartsWith(Marker)).ExecuteDeleteAsync();

        // 🐛 `MissingJsonKey_MaterialisesAsFalse` bir satırı BİLEREK 12.15b öncesi hâline
        // döndürüyor (ölçümün kendisi bu). Onarılmazsa aynı sınıftaki
        // `TheBackfill_LeftNoUserRowWithoutTheNewsKey` onu sayar ve **haklı olarak** kırılır —
        // paylaşılan veritabanında bir testin yan etkisi diğerinin iddiasına karışıyor
        // (12.14b'deki "yeni tekilleştirme testleri kırar" dersinin aynısı).
        // Onarım migration'ın SQL'inin aynısı ve idempotent.
        // 🐛 JSON yine PARAMETRE olarak gidiyor — `ExecuteSqlRaw` gövdeyi `string.Format`
        // gibi okuyor ve gömülü `{` bir yer tutucu sanılıyor. Aynı tuzağa bu oturumda
        // İKİ KEZ düşüldü: gövdeye JSON literali yazma refleksi güçlü.
        await db.Database.ExecuteSqlRawAsync(
            // 🐛 Onarım YALNIZ bu testin kendi satırlarını kapsar (`+90555000096%`).
            // İlk yazımda `WHERE NOT (… ? 'News')` ile BÜTÜN tabloyu onarıyordu ve bu,
            // `TheBackfill_LeftNoUserRowWithoutTheNewsKey`'i **iddiasız** bırakıyordu:
            // bozma turunda migration boşaltıldı, kolon tablodan silindi ve test yine
            // YEŞİL kaldı — çünkü kurulum onu her koşuda kendisi onarıyordu.
            // (12.10/12.13'ün "iddiası zayıf test, testsizlikten kötüdür" dersi.)
            "UPDATE users SET notification_preferences = CAST({0} AS jsonb) || notification_preferences " +
            "WHERE phone LIKE '+90555000096%' AND NOT (notification_preferences ? 'News');",
            "{\"News\": true}");

        // ⚠️ Kendi kullanıcılarımızı BIRAKMIYORUZ: panelin kullanıcı listesi sayfalı ve
        // biriken test satırları ilgisiz testleri kaydırıyor (yukarıdaki `PanelUsabilityTests`
        // bulgusu). Bildirimler önce düşer — FK.
        var phones = new[] { "+905550000961", "+905550000962", "+905550000963", "+905550000964" };
        var ids = await db.Users.IgnoreQueryFilters()
            .Where(u => phones.Contains(u.Phone)).Select(u => u.Id).ToListAsync();

        if (ids.Count > 0)
        {
            await db.Notifications.Where(n => ids.Contains(n.UserId)).ExecuteDeleteAsync();
            await db.Users.IgnoreQueryFilters().Where(u => ids.Contains(u.Id)).ExecuteDeleteAsync();
        }
    });
}
