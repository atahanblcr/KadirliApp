using System;
using KadirliApp.Application.Common.Exceptions;

namespace KadirliApp.Application.Common.Moderation;

/// <summary>
/// Faz 12.10 — <b>moderasyon durumunu yazmanın tek yolu Onayla/Reddet komutlarıdır.</b>
/// Bu sınıf, <c>Update*</c> komutlarının açtığı <i>ikinci yolu</i> kapatır.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden var.</b> Panelin Düzenle formunda bir "Durum" açılır menüsü vardı ve o menü
/// <c>Update*CommandHandler</c> üzerinden doğrudan <c>entity.Status</c>'e yazıyordu.
/// Üç ayrı kural bu yoldan atlanıyordu ve <b>hiçbiri hata vermiyordu</b>:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>İş kuralı.</b> <c>ApproveAdCommandHandler</c> süresi geçmiş bir ilana taze 30 günlük
///     pencere veriyor (görünmez sözleşme #25). Düzenle yolundan <c>approved</c> yapılan
///     süresi dolmuş ilan panelde "Onaylandı" görünüyor, mobilde <b>hiç görünmüyor</b> ve
///     <c>ExpireAdsJob</c> bir saat içinde durumu sessizce geri alıyordu. Aynı yoldan
///     onaylanan reddedilmiş bir kayıtta <c>RejectedReason</c> temizlenmediği için panelde
///     "Onaylandı" rozetiyle "Reddedilme sebebi: …" satırı <b>yan yana</b> duruyordu.
///   </description></item>
///   <item><description>
///     <b>Yetki.</b> Panelde izin eylemi aksiyon adından türetilir (#19): <c>Edit</c> →
///     <c>update</c>, <c>Approve</c>/<c>Reject</c> → <c>approve</c>. Form <c>approved</c>
///     sunduğu için <b>yalnız düzenleme yetkisi verilmiş moderatör moderasyon kararı
///     verebiliyordu</b> — #29'daki <c>BulkApprove</c> hatasının üçüncü biçimi.
///   </description></item>
///   <item><description>
///     <b>Denetim izi.</b> Dört <c>Update</c> komutundan üçünde <c>IAuditableCommand</c> hiç
///     yoktu, dördüncüsü izi <c>update</c> olarak yazıyordu → "bu ilanı kim onayladı?"
///     sorusunun cevabı yoktu.
///   </description></item>
/// </list>
/// <para>
/// 🔑 <b>Alan DTO'dan SİLİNMEDİ</b> (<c>ARCHITECTURE.md</c> §5 — silmek kırıcı olurdu ve
/// Faz 12'nin "hepsi additive" sözünü bozardı). Ama <b>sessizce yok da sayılmıyor</b>:
/// kaydın mevcut durumundan farklı bir değer gelirse komut <b>reddeder ve sebebini söyler</b>.
/// Sessizce yutmak, #37'nin savaştığı sınıf — hiçbir şey yapmayan bir buton, işlevsiz
/// butondan kötüdür.
/// </para>
/// <para>
/// ⚠️ <b>Çağrı yeri kritiktir:</b> guard, handler'ın varlığa <b>ilk yazmasından ÖNCE</b>
/// çağrılmalıdır. Sonra çağrılırsa reddedilen istek kaydın diğer alanlarını yine de ezer —
/// #46'nın "reddetme kaydı ezmemeli" kuralı.
/// </para>
/// </remarks>
public static class ModerationStatusGuard
{
    /// <summary>
    /// Yöneticiye gösterilen sebep. Türkçe ve <b>ne yapması gerektiğini</b> söylüyor
    /// (Değişmez Kural #6): "geçersiz durum" demek yöneticiyi ekranda bırakırdı.
    /// </summary>
    public const string Message =
        "Durum değişikliği düzenleme formundan yapılamaz; listedeki Onayla / Reddet işlemlerini kullanın.";

    /// <summary>
    /// Gelen <paramref name="requested"/> durumu kaydın mevcut durumundan farklıysa fırlatır.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Boş/eksik değer sessizce kabul edilir</b> ve bu bilinçli: alan DTO'da duruyor ama
    /// artık form onu göndermiyor, admin API'sinin eski istemcileri de göndermeyebilir.
    /// "Boş = hiçbir şey isteme" saymak, additive bir alanın <i>yokluğunun</i> o alan
    /// eklenmeden önceki davranışı vermesi kuralının aynısı (#49).
    /// </para>
    /// <para>
    /// Karşılaştırma <b>harf duyarsız</b> ve kırpılmış: durumlar sözleşmede küçük harf
    /// (<c>pending</c>/<c>approved</c>/<c>rejected</c>) ama bir istemcinin <c>"Approved"</c>
    /// göndermesi bir <i>değişiklik talebi</i> değildir — reddetmek yalnız gürültü üretirdi.
    /// </para>
    /// </remarks>
    public static void EnsureUnchanged(string? current, string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested)) return;

        if (string.Equals((current ?? string.Empty).Trim(), requested.Trim(), StringComparison.OrdinalIgnoreCase))
            return;

        throw new AppException(Message, "VALIDATION_ERROR");
    }
}
