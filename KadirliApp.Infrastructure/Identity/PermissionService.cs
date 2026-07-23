using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Infrastructure.Identity;

public sealed class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _uow;

    public PermissionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> HasAsync(Guid userId, string module, string action, CancellationToken ct = default)
    {
        var query = _uow.Repository<AdminPermission>().Query()
            .Where(p => p.UserId == userId && p.Module == module);

        return action.ToLowerInvariant() switch
        {
            "read" => await query.AnyAsync(p => p.CanRead, ct),
            "create" => await query.AnyAsync(p => p.CanCreate, ct),
            "update" => await query.AnyAsync(p => p.CanUpdate, ct),
            "delete" => await query.AnyAsync(p => p.CanDelete, ct),
            "approve" => await query.AnyAsync(p => p.CanApprove, ct),
            _ => false
        };
    }
}
