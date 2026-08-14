using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KadirliApp.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private readonly ConcurrentDictionary<Type, object> _repositories;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        _repositories = new ConcurrentDictionary<Type, object>();
    }

    public IRepository<T> Repository<T>() where T : BaseEntity
    {
        var type = typeof(T);
        
        if (!_repositories.ContainsKey(type))
        {
            var repository = new Repository<T>(_db);
            _repositories.TryAdd(type, repository);
        }

        return (IRepository<T>)_repositories[type];
    }

    public IQueryable<T> SetQuery<T>() where T : class
    {
        return _db.Set<T>().AsNoTracking();
    }

    public async Task AddToSetAsync<T>(T entity, CancellationToken ct = default) where T : class
    {
        await _db.Set<T>().AddAsync(entity, ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _db.Database.BeginTransactionAsync();
    }

    /// <inheritdoc />
    /// <remarks>
    /// 🔑 Yeniden deneme stratejisi (<c>EnableRetryOnFailure</c>) elle açılan işlemleri
    /// reddediyor; tek meşru yol stratejinin <b>kendi</b> yürütücüsünden geçmek. Blok
    /// bir bağlantı hatasında <b>bütünüyle</b> yeniden koşar — bu yüzden içindeki iş
    /// tekrarlanabilir olmalı (12.16'da: iki UPDATE, ikisi de idempotent geçişler).
    /// </remarks>
    public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            await work();
            await transaction.CommitAsync(ct);
        });
    }
}
