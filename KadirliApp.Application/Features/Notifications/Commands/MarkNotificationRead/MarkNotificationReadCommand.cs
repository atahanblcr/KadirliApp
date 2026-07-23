using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Notifications.Commands.MarkNotificationRead;

/// <summary>
/// Faz 10.10: PATCH /v1/notifications/{id}/read — sahiplik kontrolü UserId ile;
/// başkasının bildirimi (varlığı sızmasın diye) 404 döner. İkinci çağrı idempotent (200).
/// </summary>
public record MarkNotificationReadCommand(Guid UserId, Guid NotificationId) : IRequest<bool>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public MarkNotificationReadCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Notification>();
        var notification = await repo.Query()
            .FirstOrDefaultAsync(x => x.Id == request.NotificationId && x.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (notification.IsRead)
            return true;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        repo.Update(notification);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
