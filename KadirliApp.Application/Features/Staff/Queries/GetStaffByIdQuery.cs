using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Staff.DTOs;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Queries;

public record GetStaffByIdQuery(Guid Id) : IRequest<StaffDto>;

public class GetStaffByIdQueryHandler : IRequestHandler<GetStaffByIdQuery, StaffDto>
{
    private readonly IUnitOfWork _uow;

    public GetStaffByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<StaffDto> Handle(GetStaffByIdQuery request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().Query()
            .Include(u => u.AdminPermissions)
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role != UserRole.User, ct);

        if (user == null)
            throw new NotFoundException(nameof(User), request.Id);

        return StaffMapper.ToDto(user);
    }
}
