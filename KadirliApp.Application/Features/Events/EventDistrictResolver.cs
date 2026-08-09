using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Events;

/// <param name="Id">Etkinliğe yazılacak ilçe kimliği.</param>
/// <param name="IsLocal">Türetilmiş: ilçe Kadirli mi.</param>
/// <param name="NotFound">Kimlik verildi ama sözlükte yok / pasif — çağıran doğrulama hatası döndürmeli.</param>
/// <param name="Missing">Kimlik hiç verilmedi — ilçe zorunludur.</param>
public readonly record struct ResolvedEventDistrict(Guid? Id, bool IsLocal, bool NotFound, bool Missing)
{
    public bool IsValid => !NotFound && !Missing;
}

/// <summary>
/// Faz 12.4 — etkinliğin ilçesini doğrular ve <b>tek kuralla</b> <c>IsLocal</c>'ı türetir.
/// </summary>
/// <remarks>
/// 🔴 Bu yardımcının var olma sebebi bir kopyalamayı engellemek: aynı karar
/// <c>CreateEventCommand</c> ve <c>UpdateEventCommand</c>'de iki kez yazılsaydı, biri
/// güncellenip diğeri unutulduğunda kayıt <b>ilçesi Kadirli ama <c>IsLocal=false</c></b> hâline
/// düşerdi — ve mobilin "Kadirli" süzgeci o etkinliği <b>hiç göstermezdi</b>, kimse hata almadan
/// (görünmez sözleşme #23'ün sınıfı; 12.3'te kesinti mahallesinde birebir aynı gerekçeyle
/// <c>PowerOutageNeighborhoodResolver</c> yazıldı).
///
/// ⚠️ <b>İlçe zorunludur.</b> Boş bırakılabilseydi geri doldurmanın "ilçesi boş kayıt =
/// 12.4 öncesinden kalma" varsayımı çürürdü ve yöneticinin bilerek boş bıraktığı kayıt
/// bir sonraki açılışta sessizce "Kadirli" olurdu (<c>EventDistrictBackfill</c>).
///
/// ⚠️ <b>Pasif ilçe seçilemez</b> ama var olan kayıt korunur: pasifleştirme "bundan sonra
/// kullanılmasın" demektir, "geçmişi sil" değil.
/// </remarks>
public static class EventDistrictResolver
{
    public const string MissingMessage = "İlçe seçilmelidir.";
    public const string NotFoundMessage = "Seçilen ilçe bulunamadı veya pasif durumda.";

    public static async Task<ResolvedEventDistrict> ResolveAsync(
        IUnitOfWork uow, Guid? districtId, CancellationToken ct)
    {
        if (districtId is not { } id || id == Guid.Empty)
            return new ResolvedEventDistrict(null, false, false, Missing: true);

        var district = await uow.Repository<District>().Query()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (district is null || !district.IsActive)
            return new ResolvedEventDistrict(null, false, NotFound: true, false);

        return new ResolvedEventDistrict(
            district.Id,
            IsLocal: string.Equals(district.Slug, DistrictDefaults.HomeSlug, StringComparison.Ordinal),
            NotFound: false,
            Missing: false);
    }
}
