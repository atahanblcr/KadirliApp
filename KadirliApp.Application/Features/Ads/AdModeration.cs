using System;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Ads;

/// <summary>
/// Faz 12.10 — <b>bir ilanın moderasyon durumunu değiştirmenin tek yeri.</b>
/// Saf ve container'sız test edilebilir (<c>AdSubmissionRules</c> deseni).
/// </summary>
/// <remarks>
/// <para>
/// Kural <b>taşındı, değişmedi</b>: 11.15c'de <c>ApproveAdCommandHandler</c>'a yazılan
/// taze pencere (#25) ve 10.14(1)'de eklenen bayat gerekçe temizliği burada yaşıyor.
/// Handler'lar artık yalnız veriyi getirip bu metotları çağırıyor.
/// </para>
/// <para>
/// 🔑 <b>Neden ayrı bir sınıf:</b> aynı kural iki yerde yazılamaz. 12.10 öncesinde
/// Düzenle formunun açtığı ikinci yol (<c>UpdateAdCommandHandler</c>) bu kuralların
/// <b>hiçbirini</b> uygulamıyordu ve panel ile vatandaş farklı gerçeklik görüyordu.
/// </para>
/// </remarks>
public static class AdModeration
{
    /// <summary>
    /// Süresi dolmuş bir ilan onaylandığında verilen yeni yayın süresi.
    /// <c>CreateAdCommandHandler</c>'ın ilk yayın süresi ve <c>ExtendMyAdCommand</c>'in
    /// uzatma süresiyle aynı: 30 gün.
    /// </summary>
    public const int PublishDays = 30;

    /// <summary>
    /// İlanı yayına alır.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Görünmez sözleşme #25 — onay, ilanı GERÇEKTEN görünür kılmalı.</b>
    /// Canlı denetimde görülen çelişki: süresi geçmiş (<c>expired</c>) bir ilan panelden
    /// onaylandığında "İlan başarıyla onaylandı." yazıyor, ama <c>ExpiresAt</c> geçmişte
    /// kaldığı için mobil listede <b>hiç görünmüyor</b> (<c>GetAdsQueryHandler:32</c>) ve
    /// saatlik <c>ExpireAdsJob</c> durumu sessizce yeniden <c>expired</c> yapıyor.
    /// <para>
    /// Aynı sessiz hata <c>expired</c> olmayan ilanlarda da vardı: onay kuyruğunda 30 günden
    /// fazla bekleyen bir <c>pending</c> ilan onaylandığı anda süresi dolmuş oluyordu.
    /// Bu yüzden koşul <b>duruma değil TARİHE</b> bakar.
    /// </para>
    /// Karar: yayın penceresi ilanın gönderildiği an değil, <b>görünür olduğu an</b> başlar.
    /// </remarks>
    public static void Approve(Ad ad, Guid adminId, DateTime now)
    {
        if (ad.ExpiresAt <= now)
            ad.ExpiresAt = now.AddDays(PublishDays);

        ad.Status = "approved";
        ad.ApprovedBy = adminId;
        ad.ApprovedAt = now;

        // Faz 10.14(1): reddedilmiş bir ilan sonradan onaylanırsa bayat red gerekçesi kalmasın —
        // yoksa panelde "Onaylandı" rozetiyle "Reddedilme sebebi: …" satırı yan yana görünür.
        ad.RejectedReason = null;
        ad.RejectedAt = null;
    }

    /// <summary>
    /// İlanı reddeder.
    /// </summary>
    /// <remarks>
    /// Faz 10.14(1): red gerekçesi <c>RejectedReason</c>/<c>RejectedAt</c>'e yazılır
    /// (<c>MyAdDto</c> sahibe bunu döner). "Kim reddetti" izi <c>ApprovedBy</c>'ı ezerek
    /// <b>değil</b>, <c>IAuditableCommand</c> üzerinden tutulur. Bir ilan aynı anda hem
    /// onaylı hem reddedilmiş olamaz → onay izleri temizlenir.
    /// </remarks>
    public static void Reject(Ad ad, string? reason, DateTime now)
    {
        ad.Status = "rejected";
        ad.RejectedReason = reason;
        ad.RejectedAt = now;
        ad.ApprovedBy = null;
        ad.ApprovedAt = null;
    }

    /// <summary>
    /// İlanı <b>sahibinin</b> düzenlemesi üzerine yeniden moderasyon kuyruğuna alır.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🐛 <b>Bu metot 12.10'un yapısal testi tarafından bulundu.</b> Geçiş
    /// <c>UpdateMyAdCommandHandler</c>'ın içinde yazılıydı ve onay/red izlerini
    /// <i>elle</i> temizliyordu — yani <see cref="Approve"/> ve <see cref="Reject"/>
    /// ile aynı bilgiyi tekrarlayan **üçüncü bir kopya**. Kopya olduğu için de sessizce
    /// ayrışabilirdi: ilana yarın bir onay izi alanı eklendiğinde iki yer güncellenip
    /// üçüncüsü unutulurdu ve kayıt "pending ama onaylayanı dolu" hâline düşerdi.
    /// </para>
    /// <para>
    /// ⚠️ <c>ExpiresAt</c>'e <b>dokunulmaz</b> — süre işi <c>ExtendMyAdCommand</c>'in.
    /// Düzenleme bir uzatma yolu olsaydı vatandaş ilanını başlığına nokta ekleyerek
    /// sonsuza kadar yayında tutabilirdi.
    /// </para>
    /// <para>
    /// 📌 Bu, moderasyon durumunu yazan ama <i>yönetici kararı olmayan</i> tek geçiş —
    /// yönü de tek: her zaman <c>pending</c>'e <b>iner</b>. Reddedilmiş bir ilanı
    /// düzeltip yeniden gönderme yolu da budur.
    /// </para>
    /// </remarks>
    public static void Resubmit(Ad ad)
    {
        ad.Status = "pending";
        ad.ApprovedBy = null;
        ad.ApprovedAt = null;
        ad.RejectedReason = null;
        ad.RejectedAt = null;
    }
}
