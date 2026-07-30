using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(string? Search = null, bool? IsBanned = null, int Page = 1, int Limit = 20)
    : IRequest<PagedResult<UserListItemDto>>;

public class UserListItemDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string Role { get; set; } = default!;
    public bool IsActive { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }

    // Panel listesinin gösterdiği alanlar (admin-only sorgu; ban izi + avatar).
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly IUnitOfWork _uow;

    public GetUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<User>().Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(u =>
                u.Phone.Contains(s) ||
                (u.Username != null && u.Username.ToLower().Contains(s)) ||
                (u.Email != null && u.Email.ToLower().Contains(s)));
        }

        if (request.IsBanned.HasValue)
            query = query.Where(u => u.IsBanned == request.IsBanned.Value);

        var totalCount = await query.CountAsync(ct);

        var (page, limit) = Pagination.Clamp(request.Page, request.Limit, Pagination.AdminMaxLimit);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Phone = u.Phone,
                Email = u.Email,
                Username = u.Username,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                IsBanned = u.IsBanned,
                CreatedAt = u.CreatedAt,
                BanReason = u.BanReason,
                BannedAt = u.BannedAt,
                ProfilePhotoUrl = u.ProfilePhotoUrl
            })
            .ToListAsync(ct);

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
