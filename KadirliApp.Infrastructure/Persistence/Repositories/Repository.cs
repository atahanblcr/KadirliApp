using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _db;
    public Repository(AppDbContext db) => _db = db;

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public IQueryable<T> Query(bool tracking = false)
        => tracking ? _db.Set<T>() : _db.Set<T>().AsNoTracking();

    public async Task AddAsync(T e, CancellationToken ct = default) => await _db.Set<T>().AddAsync(e, ct);
    public void Update(T e) => _db.Set<T>().Update(e);
    public void Remove(T e) => _db.Set<T>().Remove(e);
    public void SoftRemove(ISoftDeletable e) => e.DeletedAt = DateTime.UtcNow;
}
