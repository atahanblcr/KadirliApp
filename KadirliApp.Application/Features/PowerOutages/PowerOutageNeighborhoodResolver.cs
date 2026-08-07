using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.PowerOutages;

/// <param name="Id">Bağlanacak mahalle kimliği (yoksa <c>null</c>).</param>
/// <param name="Name">Kesintinin <c>Neighborhood</c> metnine yazılacak değer.</param>
/// <param name="NotFound">Kimlik verildi ama sözlükte yok — çağıran doğrulama hatası döndürmeli.</param>
public readonly record struct ResolvedOutageNeighborhood(Guid? Id, string? Name, bool NotFound);

/// <summary>
/// Faz 12.3 — kesintinin mahalle alanlarını <b>tek kuralla</b> çözer: kimlik verilmişse ad
/// sözlükten yazılır, verilmemişse serbest metin (eski davranış) korunur.
/// </summary>
/// <remarks>
/// 🔴 Bu "yardımcı"nın var olma sebebi bir kopyalamayı engellemek: aynı karar
/// <c>CreatePowerOutageCommand</c> ve <c>UpdatePowerOutageCommand</c>'de iki kez yazılsaydı
/// biri güncellenip diğeri unutulduğunda kayıt <b>FK'sı dolu ama adı eski</b> hâle düşerdi —
/// ve o kaydın mahallesi panelde bir şey, mobilde başka bir şey görünürdü (görünmez sözleşme
/// #23'ün sınıfı: iki taraf farklı gerçeklik görür, kimse hata almaz).
/// </remarks>
public static class PowerOutageNeighborhoodResolver
{
    public static async Task<ResolvedOutageNeighborhood> ResolveAsync(
        IUnitOfWork uow, Guid? neighborhoodId, string? freeText, CancellationToken ct)
    {
        if (neighborhoodId is not { } id)
        {
            var trimmed = string.IsNullOrWhiteSpace(freeText) ? null : freeText.Trim();
            return new ResolvedOutageNeighborhood(null, trimmed, false);
        }

        var neighborhood = await uow.Repository<Neighborhood>().Query()
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        return neighborhood is null
            ? new ResolvedOutageNeighborhood(null, null, true)
            : new ResolvedOutageNeighborhood(neighborhood.Id, neighborhood.Name, false);
    }
}
