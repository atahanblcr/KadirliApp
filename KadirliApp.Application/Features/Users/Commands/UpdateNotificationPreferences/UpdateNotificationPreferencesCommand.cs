using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Commands.UpdateNotificationPreferences;

/// <summary>
/// Faz 10.3: PATCH /v1/users/me/notifications — yalnız gönderilen (null olmayan) anahtarlar değişir.
/// </summary>
public sealed class UpdateNotificationPreferencesCommand : IRequest<NotificationPreferencesDto>
{
    public Guid UserId { get; set; }
    public bool? Announcements { get; set; }
    public bool? Deaths { get; set; }
    public bool? Pharmacy { get; set; }
    public bool? Events { get; set; }
    public bool? Ads { get; set; }
    public bool? Campaigns { get; set; }
}

public sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, NotificationPreferencesDto>
{
    private readonly IUnitOfWork _uow;

    public UpdateNotificationPreferencesCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NotificationPreferencesDto> Handle(
        UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var users = _uow.Repository<User>();
        var user = await users.Query()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var p = user.NotificationPreferences;
        p.Announcements = request.Announcements ?? p.Announcements;
        p.Deaths = request.Deaths ?? p.Deaths;
        p.Pharmacy = request.Pharmacy ?? p.Pharmacy;
        p.Events = request.Events ?? p.Events;
        p.Ads = request.Ads ?? p.Ads;
        p.Campaigns = request.Campaigns ?? p.Campaigns;

        users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return NotificationPreferencesDto.FromEntity(p);
    }
}
