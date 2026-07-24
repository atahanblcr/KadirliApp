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
public class PushNotificationsJobTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PushNotificationsJobTests(CustomWebApplicationFactory factory) => _factory = factory;

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
