namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 12.9 — panelin yerelleştirilmiş varlıkları için Production açılış kapısı.
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden ayrı bir kapı:</b> <c>ProductionReadinessGuard</c> (11.16)
/// <c>KadirliApp.Api</c> içinde yaşıyor ve <c>Web</c> ona referans <b>veremez</b>
/// (katman yönü: <c>Web → Infrastructure, Application</c>; <c>Api</c> yok).
/// Denetlenecek şey de zaten Api'de değil burada: panelin <c>wwwroot</c>'u.
/// </para>
/// <para>
/// 🔴 <b>Neden görünümler DEĞİL dosyalar denetleniyor.</b> 12.9'un planı
/// "Production'da panel görünümlerinde dış origin kalmışsa açılmasın" diyordu.
/// Bu, çalışma anında <b>yapılamaz ve yapılsaydı yalan söylerdi</b>: Razor
/// görünümleri derlenip assembly'ye gömülüyor, <c>.cshtml</c> dosyalarının
/// yayında bulunması garanti değil. Dosyaları tarayan bir kapı yayında
/// <b>sıfır dosya bulur, hiçbir şey diyemez ve yeşil geçer</b> — yani tam olarak
/// bu projenin "sessiz başarısızlık" dediği şey olurdu.
/// Bu yüzden iş <b>ikiye</b> bölündü:
/// <list type="bullet">
/// <item>Dış origin taraması <b>derleme zamanında</b>, yapısal testte
/// (<c>PanelExternalOriginTests</c>) — kaynak orada gerçekten var.</item>
/// <item>Çalışma anında ise <b>gözlenebilir</b> olan denetlenir: türetilmiş
/// varlıklar yerinde mi. Bu gerçek bir yayın arızası — <c>npm run build</c>
/// koşmadan dağıtım yapılırsa panel <b>tamamen stilsiz</b> açılır ve harita
/// seçici ölür; hiçbir istisna oluşmaz, loglar temizdir.</item>
/// </list>
/// </para>
/// <para>
/// ⚠️ Kapı yalnız <c>Production</c>'da çalışır — geliştirici, <c>npm install</c>
/// yapmadan da paneli açabilmeli (çıktılar depoda olduğu için zaten açabiliyor).
/// </para>
/// </remarks>
public static class PanelAssetGuard
{
    /// <summary>
    /// Bulunması zorunlu türetilmiş varlıklar (<c>wwwroot</c>'a göreli) ve
    /// eksik olmalarının panelde ne kırdığı.
    /// </summary>
    /// <remarks>
    /// Liste <b>elle tutuluyor</b> ve bu bilinçli: burada denetlenmesi gereken şey
    /// "hangi dosyalar var" değil, "panelin ÇALIŞMASI için hangileri şart".
    /// Bir görünüm yeni bir varlık kullanmaya başlarsa yapısal test
    /// (<c>PanelExternalOriginTests.EveryLocalAssetReference_ExistsOnDisk</c>) bunu
    /// bağımsız olarak yakalar — yani listenin çürümesi sessiz kalmaz.
    /// </remarks>
    public static readonly IReadOnlyList<(string Path, string Breaks)> RequiredAssets =
    [
        ("css/panel.css",
            "panel TAMAMEN STİLSİZ açılır (Tailwind derlenmemiş — `npm run build:css` atlanmış)"),
        ("js/panel.js",
            "silme onayı, toplu işlem seçimi ve fotoğraf önizlemesi çalışmaz"),
        ("lib/leaflet/leaflet.js",
            "HARİTA SEÇİCİ ölür — duyuru · vefat · rehber · etkinlik · mekan formlarının 10'unda koordinat girilemez"),
        ("lib/leaflet/leaflet.css",
            "harita kutusu düzensiz çizilir, seçim görünmez"),
        ("lib/fontawesome/css/all.min.css",
            "panelin bütün ikonları kaybolur (menü, butonlar, rozetler)"),
        ("lib/inter/inter.css",
            "giriş ekranının yazı tipi yedeğe düşer")
    ];

    /// <summary>
    /// Production'da eksik varlık varsa fırlatır.
    /// </summary>
    public static void Validate(IWebHostEnvironment env, ILogger logger)
    {
        if (!env.IsProduction())
        {
            return;
        }

        var root = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "WebRootPath boş → panelin statik varlıkları (css/js/lib) hiç servis edilemez. " +
                "Dağıtımda wwwroot klasörü eksik.");
        }

        var missing = new List<string>();

        foreach (var (path, breaks) in RequiredAssets)
        {
            var full = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(full);

            // ⚠️ "Var mı" yetmez, "boş mu" da sorulur: başarısız bir derleme adımı
            // 0 baytlık bir dosya bırakabilir ve yalnız varlığa bakan bir kapı
            // onu geçerli sayar — koruma var gibi görünür, yoktur.
            if (!info.Exists || info.Length == 0)
            {
                missing.Add($"wwwroot/{path} {(info.Exists ? "BOŞ" : "YOK")} → {breaks}");
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Panelin yerelleştirilmiş varlıkları eksik — {missing.Count} dosya:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, missing.Select((m, i) => $"  {i + 1}. {m}")) +
                Environment.NewLine +
                "Çözüm: cd KadirliApp.Web && npm install && npm run build " +
                "(çıktılar depoya commit edilir; dağıtım onları taşımalı).");
        }

        logger.LogInformation(
            "Panel varlıkları yerinde ({Count} dosya) — dış origin gerekmiyor.",
            RequiredAssets.Count);
    }
}
