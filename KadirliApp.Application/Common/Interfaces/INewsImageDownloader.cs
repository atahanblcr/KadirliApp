namespace KadirliApp.Application.Common.Interfaces;

/// <summary>İndirilen görselin ham içeriği.</summary>
public sealed record NewsImageDownload(byte[] Content, string ContentType, string FileName);

/// <summary>
/// Faz 12.12 — kapak görselinin <b>indirici</b>si (aynalamanın ağ ayağı).
///
/// 🔑 Aynalamanın <b>kayıt</b> ayağı (tekilleştirme + <c>files</c> satırı) Application'da
/// (<c>NewsImageMirror</c>) kalır; burada yalnız "baytları getir" var. Ayrım bilinçli:
/// aynalama kuralları ağa çıkmadan test edilebilmeli.
///
/// 🔴 <b>Sözleşme: asla fırlatmaz.</b> Sınırların hepsi burada uygulanır ve ihlalde
/// <c>null</c> döner: boyut tavanı (2 MB), <c>Content-Type</c> denetimi (<c>image/*</c>),
/// zaman aşımı. Kaynak bizim olsa da doğrulanmamış bir indiriciyi sınırsız bırakmak yanlış;
/// üstelik indirme hatası <b>haberin alınmasını engellememeli</b> — görselsiz bir haber,
/// hiç inmemiş bir haberden iyidir.
/// </summary>
public interface INewsImageDownloader
{
    Task<NewsImageDownload?> TryDownloadAsync(string url, CancellationToken ct);
}
