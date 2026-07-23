using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace KadirliApp.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : BaseEntity;

    // Faz 10.12: BaseEntity olmayan (composite-key join) entity'ler için ham set erişimi —
    // ilk kullanıcı AnnouncementView; UserNeighborhood gibi tablolar da buradan yazılabilir.
    IQueryable<T> SetQuery<T>() where T : class;
    Task AddToSetAsync<T>(T entity, CancellationToken ct = default) where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync();
}
