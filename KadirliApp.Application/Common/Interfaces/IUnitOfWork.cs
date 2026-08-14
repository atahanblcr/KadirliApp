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

    /// <summary>
    /// ⚠️ <b>Doğrudan kullanmayın</b> — <see cref="ExecuteInTransactionAsync"/> kullanın.
    /// </summary>
    /// <remarks>
    /// 🐛 <b>Faz 12.16'da ölçüldü:</b> bu metot bugüne kadar <b>hiçbir yerde çağrılmamış</b>
    /// ve çağrıldığı an <c>InvalidOperationException</c> fırlatıyor:
    /// <i>"The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not
    /// support user-initiated transactions."</i> Sebep <c>DependencyInjection</c>'daki
    /// <c>EnableRetryOnFailure(3)</c> — yeniden denenebilir bir bağlantıda elle açılan bir
    /// işlemin yarısı tekrarlanamaz. Yani arayüzde <b>çalışmayan bir kapı</b> duruyordu.
    /// </remarks>
    Task<IDbContextTransaction> BeginTransactionAsync();

    /// <summary>
    /// Verilen işi <b>tek bir veritabanı işlemi</b> içinde, yeniden deneme stratejisiyle
    /// uyumlu biçimde koşturur. İçeride birden fazla <see cref="SaveChangesAsync"/>
    /// çağrılabilir; hepsi ya birlikte yazılır ya birlikte geri alınır.
    /// </summary>
    /// <remarks>
    /// 🔑 <b>Neden gerekti (12.16):</b> bir <b>kısmi unique indeks</b> altında iki satırı
    /// tek <c>SaveChanges</c>'te güncellemek güvenli değil — Postgres kısıtı <b>deyim
    /// başına</b> denetler ve EF, aynı tablonun UPDATE'lerini birincil anahtar sırasına göre
    /// gönderir; anahtarlar <c>gen_random_uuid()</c> olduğu için sıra <b>rastgeledir</b>.
    /// İşi ikiye bölmek gerekiyordu, ama atomikliği kaybetmeden.
    /// ⚠️ İş <b>yeniden çalıştırılabilir</b> olmalı: bağlantı hatasında strateji bloğun
    /// tamamını baştan koşar.
    /// </remarks>
    Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default);
}
