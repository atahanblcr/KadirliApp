using System;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using KadirliApp.Application.Features.Notifications.Commands.MarkNotificationRead;
using KadirliApp.Application.Features.Notifications.Commands.RegisterFcmToken;
using KadirliApp.Application.Features.Notifications.Queries.GetMyNotifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Api.Controllers;

// Faz 10.3: fcm-token kaydı; Faz 10.10: bildirim listesi + read/read-all uçları.
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private Guid RequiredUserId => CurrentUserId
        ?? throw new UnauthorizedException("Token'da user_id claim'i yok.");

    /// <summary>Kullanıcının bildirimleri — yanıttaki unreadCount filtre bağımsız toplam okunmamış sayısıdır (rozet).</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] bool unreadOnly = false)
    {
        return Success(await Sender.Send(new GetMyNotificationsQuery(RequiredUserId, page, limit, unreadOnly)));
    }

    /// <summary>Tek bildirimi okundu yapar (sahiplik: başkasınınki 404; tekrar çağrı idempotent).</summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await Sender.Send(new MarkNotificationReadCommand(RequiredUserId, id));
        return Success(new { Message = "Bildirim okundu olarak işaretlendi." });
    }

    /// <summary>Tüm okunmamışları okundu yapar; işaretlenen sayıyı döner.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var marked = await Sender.Send(new MarkAllNotificationsReadCommand(RequiredUserId));
        return Success(new { MarkedCount = marked });
    }

    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenCommand command)
    {
        command.UserId = RequiredUserId;
        await Sender.Send(command);
        return Success(new { Message = "FCM token kaydedildi." });
    }
}
