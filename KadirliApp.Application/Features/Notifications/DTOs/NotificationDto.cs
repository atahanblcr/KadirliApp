using System;
using KadirliApp.Application.Common.Models;

namespace KadirliApp.Application.Features.Notifications.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? Type { get; set; }
    public Guid? RelatedId { get; set; }
    public string? RelatedType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Faz 10.10: sayfalı bildirim listesi + rozet için unreadCount (filtreden bağımsız,
/// kullanıcının TOPLAM okunmamış sayısı). Zarf meta'sı filter'da sabit kurulduğundan
/// unreadCount data içinde taşınır.
/// </summary>
public class NotificationListDto : PagedResult<NotificationDto>
{
    public int UnreadCount { get; set; }
}
