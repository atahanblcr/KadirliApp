using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Domain.Common;

namespace KadirliApp.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    IQueryable<T> Query(bool tracking = false);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    void SoftRemove(ISoftDeletable e);
}
