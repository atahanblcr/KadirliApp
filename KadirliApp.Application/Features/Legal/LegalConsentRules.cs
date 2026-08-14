using System;
using System.Linq;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Legal;

/// <summary>
/// Faz 12.16 — <b>"hangi belge sorulur, hangisi kaydı bloklar?"</b> sorusunun TEK SAHİBİ.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 Üç çağıranı var ve tek sahiplik tam bu yüzden şart: kayıt ekranının belge listesi
/// (<c>GET /v1/legal/documents</c>), kaydın kendisi (<c>RegisterCommand</c>) ve ayarlardaki
/// rıza ekranı. Ayrı yazılsalardı bu projenin en sık tekrarlayan hasar sınıfı doğardı
/// (§7 madde 38/43/55/65): liste bir belgeyi <b>göstermez</b>, kayıt onu <b>zorunlu tutar</b>
/// ve kullanıcı, ekranında hiç görünmeyen bir kutuyu işaretlemediği için kayıt olamaz —
/// hata mesajı doğru, sebebi ekranda <b>yok</b>.
/// </para>
/// <para>
/// 🔴 <b>YAYINDA SÜRÜMÜ OLMAYAN BELGE ZORUNLU OLAMAZ.</b> Görünmezliğin ekseni burada
/// <b>iki</b> tane: belgenin <see cref="LegalDocument.IsActive"/>'i ve <b>metninin var
/// olması</b>. İkincisi sayılmasaydı, <c>IsMandatory</c> işaretli ama hiç sürüm yayınlanmamış
/// bir belge <b>kaydı tamamen kilitlerdi</b>: istemci gösterecek metin bulamaz, sunucu
/// onaysız kaydı reddeder ve uygulama <b>hiç yeni kullanıcı alamaz</b> hâle gelirdi.
/// Belirti "kayıt olmuyor"dur, sebep hiçbir ekranda yazmaz. 📌 Bu, 12.15'in
/// <i>"görünmezliğin KAÇ ekseni var?"</i> dersinin (§7 madde 65) birebir tekrarı — ve tam
/// bu yüzden <c>DbSeeder</c> belgelerin yalnız <b>kabuğunu</b> açıyor, metnini değil.
/// </para>
/// </remarks>
public static class LegalConsentRules
{
    /// <summary>
    /// Kullanıcıya <b>gösterilebilir</b> belgeler: aktif <b>ve</b> yayında bir sürümü olan.
    /// Ölçüt <see cref="LegalDocumentVersion.IsLive"/>'ın sorgu hâlidir — ikinci bir
    /// bellek kopyası yazılmadı (§7 madde 65'in "ölçüt sorgunun kendisi" kuralı).
    /// </summary>
    public static IQueryable<LegalDocument> Available(IQueryable<LegalDocument> documents) =>
        documents.Where(d => d.IsActive &&
                             d.Versions.Any(v => v.PublishedAt != null && v.SupersededAt == null));

    /// <summary>Kayıt ekranında sorulacaklar (gösterilebilir + <c>ShowAtRegistration</c>).</summary>
    public static IQueryable<LegalDocument> AtRegistration(IQueryable<LegalDocument> documents) =>
        Available(documents).Where(d => d.ShowAtRegistration);

    /// <summary>
    /// 🔴 Kaydı <b>bloklayan</b> belgeler: gösterilebilir + zorunlu.
    /// </summary>
    /// <remarks>
    /// ⚠️ <see cref="LegalDocument.ShowAtRegistration"/> koşulu <b>bilerek yok</b>: zorunlu
    /// ama kayıt ekranında gösterilmeyen bir belge <b>tanım gereği tutarsızdır</b> ve o
    /// tutarsızlığı burada sessizce "gösterilmiyorsa zorunlu değildir" diye yorumlamak,
    /// yöneticinin zorunlu işaretlediği bir metni <b>hiç kimseye sormadan</b> geçerdi.
    /// Panel bu tutarsızlığı uyarı olarak gösterir; kural onu <b>yutmaz</b>.
    /// </remarks>
    public static IQueryable<LegalDocument> Mandatory(IQueryable<LegalDocument> documents) =>
        Available(documents).Where(d => d.IsMandatory);

    /// <summary>Bir belgenin yayındaki sürümü (yoksa <c>null</c>).</summary>
    public static LegalDocumentVersion? LiveVersionOf(LegalDocument document) =>
        document.Versions.FirstOrDefault(v => v.IsLive);
}
