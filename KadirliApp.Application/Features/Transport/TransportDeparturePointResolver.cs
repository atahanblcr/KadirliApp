using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Transport;

/// <param name="Id">Hatta yazılacak kalkış noktası kimliği (boş bırakılabilir).</param>
/// <param name="NotFound">Kimlik verildi ama sözlükte yok / pasif — çağıran doğrulama hatası döndürmeli.</param>
public readonly record struct ResolvedDeparturePoint(Guid? Id, bool NotFound)
{
    public bool IsValid => !NotFound;
}

/// <summary>
/// Faz 12.5 — hattın kalkış noktasını doğrular. Create ve Update <b>aynı metottan</b> geçer.
/// </summary>
/// <remarks>
/// 🔑 12.3'ün <c>PowerOutageNeighborhoodResolver</c>'ı ve 12.4'ün <c>EventDistrictResolver</c>'ı
/// ile aynı gerekçe: iki komutta ayrı yazılan bir doğrulama, biri güncellenip diğeri
/// unutulduğunda <b>sessizce ayrışır</b>.
///
/// ⚠️ İlçeden bir fark var ve bilinçli: <b>kalkış noktası zorunlu DEĞİL.</b> Etkinlikte ilçe
/// zorunluydu çünkü geri doldurmanın "boşsa eskiden kalma" varsayımı ona dayanıyordu
/// (görünmez sözleşme #45). Burada geri doldurma <b>yok</b> — 12.5 öncesi hatların kalkış
/// noktası gerçekten bilinmiyor ve bir tahmin ("hepsi otogardan kalkar") vatandaşı
/// <b>yanlış yere</b> götürürdü. Panel bu boşluğu uyarı olarak gösterir, doldurmaz.
///
/// ⚠️ <b>Pasif nokta YENİ OLARAK seçilemez</b> ama kayıtta zaten duran değer korunur:
/// pasifleştirme "bundan sonra kullanılmasın" demektir, "geçmişi sil" değil (12.4'teki aynı karar).
///
/// 🐛 <b>12.5 canlı denetiminde bulunan hata sınıfı (buraya da uygulandı):</b> kapı
/// <paramref name="currentDeparturePointId"/> olmadan yazılırsa, form pasif noktayı seçili
/// tuttuğu için (bu doğru bir karar) ikisi birlikte <b>düzenlenemeyen bir kayıt</b> üretir:
/// kalkış noktası sonradan pasifleştirilen bir hatta yönetici yalnız <i>fiyatı</i> güncellemek
/// istese bile <b>hiç dokunmadığı bir alan</b> yüzünden hata alır. Etkinlik ilçesinde bu canlıda
/// birebir yaşandı; aynı şekilli kod burada da vardı ve aynı anda düzeltildi.
/// </remarks>
public static class TransportDeparturePointResolver
{
    public const string NotFoundMessage = "Seçilen kalkış noktası bulunamadı veya pasif durumda.";

    /// <param name="currentDeparturePointId">
    /// Hattın <b>şu anki</b> kalkış noktası (güncellemede verilir, oluşturmada <c>null</c>).
    /// Değer değişmiyorsa pasiflik kapısı uygulanmaz.
    /// </param>
    public static async Task<ResolvedDeparturePoint> ResolveAsync(
        IUnitOfWork uow, Guid? departurePointId, CancellationToken ct, Guid? currentDeparturePointId = null)
    {
        if (departurePointId is not { } id || id == Guid.Empty)
            return new ResolvedDeparturePoint(null, NotFound: false);

        var point = await uow.Repository<TransportDeparturePoint>().Query()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        var unchanged = currentDeparturePointId is { } current && current == id;

        if (point is null || (!point.IsActive && !unchanged))
            return new ResolvedDeparturePoint(null, NotFound: true);

        return new ResolvedDeparturePoint(point.Id, NotFound: false);
    }
}
