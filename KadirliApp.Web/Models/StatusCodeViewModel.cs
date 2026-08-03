namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 11.15c: markalı durum kodu sayfası. Panelde 404 bugüne dek <b>0 bayt</b> dönüyordu.
/// Metinler Türkçe ve teknik terim içermez (CLAUDE.md Değişmez Kural #6).
/// </summary>
public sealed record StatusCodeViewModel(int Code, string? OriginalPath)
{
    public string Title => Code switch
    {
        404 => "Sayfa bulunamadı",
        403 => "Bu sayfaya erişim yetkiniz yok",
        400 => "İstek anlaşılamadı",
        _ => "Bir sorun oluştu"
    };

    public string Description => Code switch
    {
        404 => "Aradığınız sayfa taşınmış, silinmiş ya da adresi yanlış yazılmış olabilir.",
        403 => "Hesabınızın bu bölüme girme izni bulunmuyor. Gerekiyorsa bir yöneticiden izin isteyin.",
        400 => "Gönderilen bilgi beklenen biçimde değil. Formu kontrol edip yeniden deneyin.",
        _ => "Beklenmeyen bir durum oluştu. Sayfayı yenilemeyi deneyebilirsiniz."
    };

    public string Icon => Code switch
    {
        404 => "fa-map-signs",
        403 => "fa-lock",
        _ => "fa-triangle-exclamation"
    };
}
