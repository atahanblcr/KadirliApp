namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — haber modülünün izin/denetim anahtarı, <b>tek yerde</b>.
/// </summary>
/// <remarks>
/// 🔑 Anahtar üç yerde birden kullanılıyor (§7 madde 20): <c>[RequirePermission("news", …)]</c>,
/// panel <c>[PanelPermission("news")]</c> + <c>PanelMenu.Items</c> (12.13) ve komutların
/// <c>AuditModule</c>'ü. Serbest metin yazılırsa yöneticinin matriste verdiği yetkinin
/// panelde karşılığı olmaz ve <b>sebep hiçbir yerde görünmez</b>.
/// ⚠️ Denetim izi ekranı bu anahtarın Türkçesini <c>PanelDisplay</c>'den okur; karşılığı
/// yoksa ham İngilizce basar (Değişmez Kural #6).
/// </remarks>
public static class NewsAudit
{
    public const string Module = "news";
}
