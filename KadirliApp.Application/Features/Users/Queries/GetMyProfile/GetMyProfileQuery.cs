using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Queries.GetMyProfile;

/// <summary>Faz 10.3: GET /v1/users/me — claim'deki kullanıcının profili.</summary>
public sealed record GetMyProfileQuery(Guid UserId) : IRequest<MyProfileDto>;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfileDto>
{
    private readonly IUnitOfWork _uow;

    public GetMyProfileQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<MyProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(x => x.PrimaryNeighborhood)
            // Faz 12.7 — bağlı sosyal hesaplar (12.8'in "Bağlı hesaplar" ekranı).
            .Include(x => x.Identities)
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        return MyProfileDto.FromUser(user, user.PrimaryNeighborhood?.Name, user.Identities);
    }
}
