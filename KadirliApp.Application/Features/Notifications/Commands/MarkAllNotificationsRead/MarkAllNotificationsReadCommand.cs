using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

/// <summary>
/// Faz 10.10: POST /v1/notifications/read-all — kullanıcının tüm okunmamışlarını tek atomik
/// UPDATE ile okundu yapar (10.6'daki ExecuteUpdateAsync deseni); işaretlenen sayıyı döner.
/// </summary>
public record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<int>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly IUnitOfWork _uow;

    public MarkAllNotificationsReadCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await _uow.Repository<Notification>().Query()
            .Where(x => x.UserId == request.UserId && !x.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAt, now)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
    }
}
