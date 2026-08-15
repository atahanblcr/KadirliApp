using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Legal.Dtos;

/// <summary>
/// Faz 12.16 — kayıt/ayarlar ekranının göreceği belge. <b>Metni taşır</b>: ikinci bir istek
/// gerekmesin diye (kayıt akışında ağ turu ne kadar azsa o kadar iyi).
/// </summary>
public class LegalDocumentDto
{
    public Guid Id { get; set; }

    /// <summary>⚠️ <b>Kontrat</b> — <c>LegalDocumentTypes</c> metinleri (§7 madde 47 deseni).</summary>
    public string Type { get; set; } = default!;

    public string Title { get; set; } = default!;

    /// <summary>
    /// 🔴 <b>Rıza BU kimliğe verilir</b>, belgeye değil (§7 madde 71). İstemci bunu geri
    /// gönderir; sunucu "o anki yayında sürüm"ü kendi başına seçmez.
    /// </summary>
    public Guid VersionId { get; set; }

    /// <summary>Kullanıcıya "v3" olarak gösterilir.</summary>
    public int VersionNumber { get; set; }

    /// <summary>Onay kutusunun yanındaki tek cümle.</summary>
    public string? Summary { get; set; }

    /// <summary>Metnin kendisi (HTML).</summary>
    public string Body { get; set; } = default!;

    /// <summary>🔴 <c>true</c> ise bu kutu işaretlenmeden kayıt tamamlanmaz.</summary>
    public bool IsMandatory { get; set; }

    /// <summary>Kayıt ekranında gösterilsin mi (ayarlar ekranı hepsini gösterir).</summary>
    public bool ShowAtRegistration { get; set; }

    public int SortOrder { get; set; }

    public DateTime EffectiveFrom { get; set; }

    /// <summary>Bu sürüm yeniden onay gerektiriyor mu (12.17'nin açılış ekranı bunu okur).</summary>
    public bool RequiresReconsent { get; set; }
}

/// <summary>
/// Faz 12.17 (plan dışı ek) — <b>belirli bir sürümün</b> metni
/// (<c>GET /v1/legal/versions/{id}</c>).
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden gerekti:</b> 12.16 rızayı sürüme bağladı ve <c>GET /v1/users/me/consents</c>
/// kullanıcının onayladığı <c>consentedVersionId</c>'yi <b>söylüyor</b> — ama o kimliğe
/// karşılık gelen <b>metni okuyacak bir uç yoktu</b>. Yani v2'yi onaylayan bir vatandaş, v3
/// yayınlandığı an <i>neyi kabul ettiğini bir daha hiç göremiyordu</i>. Bu, bloğun kapatmak
/// için yazıldığı hasarın (<i>"kayıt duruyor, metin ortada yok"</i>) vatandaş tarafındaki
/// yüzüdür: kanıt <b>bizde</b> vardı, <b>sahibinde</b> yoktu.
/// </para>
/// <para>
/// 🔴 <b>Yalnız YAYINLANMIŞ sürüm döner</b> (<c>PublishedAt != null</c>); taslak <b>404</b>'tür.
/// Taslak dönseydi henüz yayınlanmamış bir metin, kimliğini tahmin eden herkese açılırdı —
/// ve daha kötüsü, kullanıcı onu "yürürlükteki metin" sanabilirdi.
/// </para>
/// <para>
/// ⚠️ Yürürlükten kalkmış sürüm <b>bilerek</b> dönüyor (<see cref="IsLive"/> <c>false</c> ile):
/// bu ucun varlık sebebi zaten <b>eski</b> metni okuyabilmek. Ekran farkı söylemek zorunda —
/// söylemezse kullanıcı yürürlükten kalkmış bir metni güncel sanar.
/// </para>
/// </remarks>
public class LegalVersionDto
{
    /// <summary>Sürümün kimliği — rıza kaydının işaret ettiği değer.</summary>
    public Guid Id { get; set; }

    public string DocumentType { get; set; } = default!;
    public string DocumentTitle { get; set; } = default!;

    public int VersionNumber { get; set; }
    public string? Summary { get; set; }
    public string Body { get; set; } = default!;

    public DateTime EffectiveFrom { get; set; }
    public DateTime PublishedAt { get; set; }

    /// <summary>⚠️ <c>false</c> ise metin <b>yürürlükten kalkmıştır</b> — ekran bunu söylemeli.</summary>
    public bool IsLive { get; set; }

    public DateTime? SupersededAt { get; set; }
}

/// <summary>Faz 12.16 — kullanıcının kendi rıza durumu (<c>GET /v1/users/me/consents</c>).</summary>
public class MyConsentDto
{
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public bool IsMandatory { get; set; }

    /// <summary>Yayındaki sürümün kimliği — "onayınız güncel mi?" karşılaştırmasının sol tarafı.</summary>
    public Guid CurrentVersionId { get; set; }
    public int CurrentVersionNumber { get; set; }

    /// <summary>Kullanıcının onay verdiği sürüm; hiç karar vermemişse <c>null</c>.</summary>
    public Guid? ConsentedVersionId { get; set; }
    public int? ConsentedVersionNumber { get; set; }

    /// <summary>Şu anki durum. ⚠️ <c>false</c>, "hiç sorulmadı" demek <b>değildir</b> — onun cevabı <see cref="DecidedAt"/>'in <c>null</c> olmasıdır.</summary>
    public bool Granted { get; set; }

    public DateTime? DecidedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 🔑 Yeniden onay gerekiyor mu? <b>Sunucuda türetilir</b>: onaylanan sürüm yayındaki
    /// sürümden eskiyse <b>ve</b> aradaki sürümlerden biri <c>RequiresReconsent</c> ise.
    /// İstemcide hesaplansaydı iki sahip doğardı ve mağazadaki eski sürümler kuralın
    /// eski hâlini uygulamaya devam ederdi.
    /// </summary>
    public bool NeedsReconsent { get; set; }
}

/// <summary>Faz 12.16 — kayıt/ayarlar akışında istemcinin gönderdiği tek karar.</summary>
public class ConsentDecisionDto
{
    /// <summary>🔴 Kullanıcının <b>gördüğü</b> sürümün kimliği (§7 madde 71).</summary>
    public Guid VersionId { get; set; }

    /// <summary>⚠️ <c>false</c> da <b>kaydedilir</b>: "sormadık" ile "sorduk, hayır dedi" ayrı şeylerdir.</summary>
    public bool Granted { get; set; }
}

/// <summary>Faz 12.16 — panelin belge listesi satırı.</summary>
public class LegalDocumentAdminDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public bool IsMandatory { get; set; }
    public bool ShowAtRegistration { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    public int VersionCount { get; set; }

    /// <summary>Yayındaki sürüm — <c>null</c> ise belge <b>hiçbir yerde görünmüyor</b> (panel bunu söyler).</summary>
    public int? LiveVersionNumber { get; set; }
    public DateTime? LivePublishedAt { get; set; }

    /// <summary>Yayındaki sürümü kaç kişi onayladı. 🔑 Gerçek rıza sorgusundan gelir, sayaç kolonundan değil.</summary>
    public int LiveGrantedCount { get; set; }

    /// <summary>Taslak sürüm var mı (panelde "yayınlanmayı bekliyor" rozeti).</summary>
    public bool HasDraft { get; set; }
}

/// <summary>Faz 12.16 — panelin sürüm listesi satırı.</summary>
public class LegalDocumentVersionAdminDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string? Summary { get; set; }
    public string Body { get; set; } = default!;
    public bool RequiresReconsent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SupersededAt { get; set; }
    public string? PublishedByName { get; set; }

    /// <summary>Yayında mı (taslak/yayında/yürürlükten kalktı ayrımının tek türetildiği yer).</summary>
    public bool IsLive { get; set; }
    public bool IsDraft { get; set; }

    /// <summary>Bu sürüme kaç kişi onay verdi / kaç kişi reddetti.</summary>
    public int GrantedCount { get; set; }
    public int DeniedCount { get; set; }
}

/// <summary>Faz 12.16 — rıza defteri satırı (<b>yalnız admin</b>: IP ve tarayıcı taşır).</summary>
public class ConsentLedgerRowDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? Phone { get; set; }
    public string DocumentTitle { get; set; } = default!;
    public string DocumentType { get; set; } = default!;
    public int VersionNumber { get; set; }
    public bool Granted { get; set; }
    public DateTime DecidedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Source { get; set; } = default!;
}

/// <summary>Faz 12.16 — kayıt akışının rıza sonucu (uçtan dönmez; komutlar arası taşınır).</summary>
public record ConsentValidationResult(bool IsValid, string? MissingDocumentTitle, IReadOnlyList<Guid> MissingVersionIds);
