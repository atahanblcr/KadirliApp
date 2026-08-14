using System.Linq;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Legal;

/// <summary>
/// Faz 12.16 — belge DTO'sunun <b>TEK SAHİBİ</b> (12.4'ün <c>EventProjection</c> deseni).
/// </summary>
/// <remarks>
/// 🔑 Liste (<c>GET /v1/legal/documents</c>) ile tek belge (<c>…/{type}</c>) <b>aynı</b>
/// projeksiyondan geçer. Ayrı yazılsalardı 12.4'ün sinsi hasarı doğardı (§7 madde 43):
/// listede görünen <c>versionId</c> ile detayda görünen başka olurdu ve istemci, kullanıcının
/// <b>okuduğu</b> metinden farklı bir sürüme rıza gönderirdi — hiçbir hata vermeden.
/// </remarks>
public static class LegalProjection
{
    /// <summary>Yayında sürümü olmayan belge <c>null</c> döner — çağıran onu listeye koymaz.</summary>
    public static LegalDocumentDto? ToDto(LegalDocument document)
    {
        var live = LegalConsentRules.LiveVersionOf(document);
        if (live is null) return null;

        return new LegalDocumentDto
        {
            Id = document.Id,
            Type = document.Type,
            Title = document.Title,
            VersionId = live.Id,
            VersionNumber = live.VersionNumber,
            Summary = live.Summary,
            Body = live.Body,
            IsMandatory = document.IsMandatory,
            ShowAtRegistration = document.ShowAtRegistration,
            SortOrder = document.SortOrder,
            EffectiveFrom = live.EffectiveFrom,
            RequiresReconsent = live.RequiresReconsent
        };
    }

    /// <summary>
    /// 🔑 <b>"Yeniden onay gerekiyor mu?"</b>un tek hesaplandığı yer.
    /// </summary>
    /// <remarks>
    /// Ölçüt <i>"sürüm numarası değişti mi"</i> <b>değildir</b>: yazım hatası düzeltmesi de
    /// sürüm numarasını artırır. Doğru soru, kullanıcının onayladığı sürümden <b>sonra</b>
    /// yayınlanmış sürümlerden <b>herhangi birinin</b> <c>RequiresReconsent</c> taşıyıp
    /// taşımadığıdır — yönetici arada iki sürüm yayınladıysa (biri esaslı, biri değil)
    /// yalnız sonuncuya bakmak esaslı değişikliği <b>sessizce atlardı</b>.
    /// </remarks>
    public static bool NeedsReconsent(LegalDocument document, int? consentedVersionNumber, bool granted)
    {
        var live = LegalConsentRules.LiveVersionOf(document);
        if (live is null) return false;

        // Hiç karar vermemiş: zorunluysa sorulmalı, isteğe bağlıysa rahatsız edilmez.
        if (consentedVersionNumber is null) return document.IsMandatory;

        // Reddetmiş ya da geri almış: yeni sürüm onu yeniden onaya çağırmaz — kararı bellidir.
        // (Zorunlu belgede bu durum kayıt akışında zaten mümkün değil.)
        if (!granted) return false;

        if (consentedVersionNumber >= live.VersionNumber) return false;

        return document.Versions.Any(v =>
            v.VersionNumber > consentedVersionNumber &&
            v.VersionNumber <= live.VersionNumber &&
            v.PublishedAt != null &&
            v.RequiresReconsent);
    }
}
