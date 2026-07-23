using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Notifications.DTOs;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Queries.GetMyNotifications;

/// <summary>
/// Faz 10.10: GET /v1/notifications — kullanıcının bildirimleri (paged, ?unreadOnly=).
/// Cache'siz: kullanıcıya özel ve is_read ile sık değişen veri.
/// </summary>
public record GetMyNotificationsQuery(Guid UserId, int Page = 1, int Limit = 20, bool UnreadOnly = false)
    : IRequest<NotificationListDto>;

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, NotificationListDto>
{
    private readonly IUnitOfWork _uow;

    public GetMyNotificationsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NotificationListDto> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var baseQuery = _uow.Repository<Notification>().Query()
            .Where(x => x.UserId == request.UserId);

        var unreadCount = await baseQuery.CountAsync(x => !x.IsRead, cancellationToken);

        var query = request.UnreadOnly ? baseQuery.Where(x => !x.IsRead) : baseQuery;
        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(request.Page, request.Limit);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Body,
                Type = x.Type,
                RelatedId = x.RelatedId,
                RelatedType = x.RelatedType,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new NotificationListDto
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit,
            UnreadCount = unreadCount
        };
    }
}
