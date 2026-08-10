using System;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Deaths;

/// <summary>
/// Faz 12.10 — <b>bir vefat ilanının moderasyon durumunu değiştirmenin tek yeri.</b>
/// Saf, container'sız test edilebilir.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Bu modülde 12.10 yalnız bir yolu KAPATMADI, iki yolu AÇTI.</b> Vefat modülünde
/// Reddet ve Arşivle komutları <b>hiç yoktu</b>: her ikisinin de tek yolu Düzenle
/// formundaki durum menüsüydü. Menü kaldırılıp karşılığı yazılmasaydı, "reddet" ve
/// "arşivle" panelden <b>tamamen kaybolurdu</b> — bir hatayı düzeltirken bir işlevi
/// silmek olurdu.
/// </para>
/// <para>
/// <b>Arşiv neden moderasyon:</b> <c>archived</c> kaydı public listeden düşürür
/// (<c>GetDeathNoticesQuery</c> yalnız <c>approved</c> döner), yani "içeriği yayından
/// kaldırma" kararıdır — <c>ArchiveDeathsJob</c> aynı geçişi <c>AutoArchiveAt</c>
/// dolduğunda kendiliğinden yapıyor. Elle arşivlemenin gerçek kullanımı ailenin erken
/// kaldırma talebidir; geri almanın yolu <see cref="Approve"/>'dur.
/// </para>
/// </remarks>
public static class DeathNoticeModeration
{
    /// <summary>Vefat ilanını yayına alır (arşivlenmiş bir kaydı geri getirmenin de yolu budur).</summary>
    public static void Approve(DeathNotice notice, Guid adminId, DateTime now)
    {
        notice.Status = "approved";
        notice.ApprovedBy = adminId;
        notice.ApprovedAt = now;

        // 12.10: ilan/kampanyayla aynı kural — bayat red gerekçesi "Onaylandı" rozetinin
        // yanında durmamalı. Vefatta red bugüne kadar hiç yazılamadığı için bu alan boştu;
        // RejectDeathNoticeCommand ile birlikte anlamlı hâle geldi.
        notice.RejectedReason = null;
    }

    /// <summary>Vefat ilanını reddeder.</summary>
    /// <remarks>
    /// Vefat ilanını <b>vatandaş da gönderebiliyor</b> (<c>POST /v1/deaths</c>), yani onay
    /// kuyruğunda gerçekten reddedilmesi gereken kayıtlar oluşuyor. 12.10 öncesinde bunun
    /// tek yolu Düzenle formuydu ve o yol ne izi ne gerekçeyi tutuyordu.
    /// </remarks>
    public static void Reject(DeathNotice notice, string? reason, DateTime now)
    {
        notice.Status = "rejected";
        notice.RejectedReason = reason;
        notice.ApprovedBy = null;
        notice.ApprovedAt = null;
    }

    /// <summary>Yayındaki bir vefat ilanını erken arşivler.</summary>
    /// <remarks>
    /// ⚠️ <c>AutoArchiveAt</c>'e <b>dokunulmaz</b>: o alan "ne zaman kendiliğinden
    /// arşivlenecekti" bilgisidir ve elle arşivleme onu geçersiz kılmaz — ilan sonradan
    /// tekrar onaylanırsa iş yine doğru tarihte devreye girmelidir. <c>ArchiveDeathsJob</c>
    /// yalnız <c>approved</c> satırlara dokunduğu için burada bir çakışma da doğmaz.
    /// <para>
    /// 📌 Diğer geçişlerin aksine <c>now</c> almaz — yazacağı bir zaman damgası yok.
    /// Simetri için boş bir parametre taşımak, ilk okuyana "bir yere yazılıyor olmalı"
    /// dedirtirdi.
    /// </para>
    /// </remarks>
    public static void Archive(DeathNotice notice)
    {
        notice.Status = "archived";
    }
}
