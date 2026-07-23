using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Staff.DTOs;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Queries;

public record GetStaffQuery(string? Search = null, int Page = 1, int Limit = 20)
    : IRequest<PagedResult<StaffDto>>;

public class GetStaffQueryHandler : IRequestHandler<GetStaffQuery, PagedResult<StaffDto>>
{
    private readonly IUnitOfWork _uow;

    public GetStaffQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<StaffDto>> Handle(GetStaffQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<User>().Query()
            .Where(u => u.Role != UserRole.User);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(u =>
                u.Phone.Contains(s) ||
                (u.Username != null && u.Username.ToLower().Contains(s)) ||
                (u.Email != null && u.Email.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(ct);

        var (page, limit) = Pagination.Clamp(request.Page, request.Limit, Pagination.AdminMaxLimit);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(u => u.AdminPermissions)
            .ToListAsync(ct);

        return new PagedResult<StaffDto>
        {
            Items = users.Select(StaffMapper.ToDto).ToList(),
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}

/// <summary>ToRoleString EF tarafından SQL'e çevrilemediği için map bellekte yapılır.</summary>
public static class StaffMapper
{
    public static StaffDto ToDto(User u) => new()
    {
        Id = u.Id,
        Phone = u.Phone,
        Email = u.Email,
        Username = u.Username,
        Role = u.Role.ToRoleString(),
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        Permissions = u.AdminPermissions.Select(p => new StaffPermissionDto
        {
            Module = p.Module,
            CanRead = p.CanRead,
            CanCreate = p.CanCreate,
            CanUpdate = p.CanUpdate,
            CanDelete = p.CanDelete,
            CanApprove = p.CanApprove
        }).ToList()
    };
}
