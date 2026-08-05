using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KadirliApp.Application.Common.Observability;

/// <summary>
/// Faz 12.1 — **hata tekilleştirmesinin tek sahibi.**
///
/// Aynı hatanın binlerce kopyası yerine tek satır + sayaç tutulmasını sağlar. Bu bir
/// performans süsü değil: tekilleştirme olmadan tek bir 500 döngüsü <c>error_logs</c>'u
/// dakikada on binlerce satırla doldurur, tablo projenin en büyüğü olur ve panel açılmaz
/// hâle gelir — üstelik bunu **hiçbir hata vermeden** yapar.
///
/// 🔑 <b>İşin püf noktası <see cref="Normalize"/>.</b> Ham mesaj kullanılırsa tekilleştirme
/// hiç çalışmaz: <c>"Ad 3f2a… bulunamadı"</c> her istekte farklı GUID taşır, yani her
/// istek ayrı parmak izi üretir. Değişken parçalar (GUID · sayı · tarih · saat) yer
/// tutucuya çevrilir; geriye hatanın <b>şekli</b> kalır.
///
/// ⚠️ Yığın karesinden **satır numaraları atılır**. Aksi hâlde aynı hata, kod bir satır
/// kaydığı için yeni bir derlemede yepyeni bir kayıt gibi görünür ve "bu hata ne zamandır
/// var?" sorusu her yayında sıfırlanır.
/// </summary>
public static class ErrorFingerprint
{
    private static readonly Regex GuidPattern = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled);

    // ISO tarih/saat: 2026-08-05, 2026-08-05T14:30:00, 2026-08-05 14:30:00.123Z
    private static readonly Regex DatePattern = new(
        @"\d{4}-\d{2}-\d{2}([T ]\d{2}:\d{2}(:\d{2})?(\.\d+)?Z?)?",
        RegexOptions.Compiled);

    private static readonly Regex TimePattern = new(@"\b\d{1,2}:\d{2}(:\d{2})?\b", RegexOptions.Compiled);

    private static readonly Regex NumberPattern = new(@"\d+([.,]\d+)?", RegexOptions.Compiled);

    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Yığın karesindeki dosya/satır eki: <c> in /Users/…/File.cs:line 42</c>.</summary>
    private static readonly Regex StackFrameLocationPattern = new(@"\s+in\s+.*?:line\s+\d+", RegexOptions.Compiled);

    /// <summary>
    /// Değişken parçaları yer tutucuya çevirir. Sıra **önemli**: GUID'ler sayı deseninden
    /// önce yakalanmalı (içlerinde rakam var), tarihler de saatlerden ve sayılardan önce.
    /// </summary>
    public static string Normalize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var text = message;
        text = GuidPattern.Replace(text, "{id}");
        text = DatePattern.Replace(text, "{date}");
        text = TimePattern.Replace(text, "{time}");
        text = NumberPattern.Replace(text, "{n}");
        text = WhitespacePattern.Replace(text, " ");

        return text.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Yığının **ilk anlamlı karesi** — hatanın nerede doğduğu. Satır numarası atılır
    /// (bkz. sınıf belgesi). Yığın yoksa boş döner; parmak izi yine de üretilir.
    /// </summary>
    public static string FirstFrame(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            return string.Empty;

        foreach (var line in stackTrace.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            trimmed = StackFrameLocationPattern.Replace(trimmed, string.Empty);
            return trimmed.Trim();
        }

        return string.Empty;
    }

    /// <summary>
    /// Parmak izi = SHA-256(kaynak | kod | normalize(mesaj) | ilk kare), ilk 32 onaltılık karakter.
    /// 128 bit çakışma için fazlasıyla yeterli, kolon da kısa kalır.
    /// </summary>
    public static string Compute(string source, string code, string? message, string? stackTrace)
    {
        var material = string.Join('|',
            source.Trim().ToLowerInvariant(),
            code.Trim().ToLowerInvariant(),
            Normalize(message),
            FirstFrame(stackTrace).ToLowerInvariant());

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }
}
