namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 12.19a — örnek (sahte) veri basma işleminin Application katmanındaki soyutlaması.
/// Gerçeklemesi Infrastructure'da (<c>MockDataSeederService</c>) ve <c>MockDataSeeder</c>'ı sarar.
/// </summary>
/// <remarks>
/// 🔑 <b>Neden soyutlandı:</b> 12.19a öncesinde <c>DashboardController</c> doğrudan
/// <c>AppDbContext</c> enjekte ediyordu. Katman olarak yasaldı (§1: <c>Web → Infrastructure</c>
/// meşru) ama <b>MediatR'ı atlıyordu</b> — yani denetim izi (<c>AuditBehavior</c>) hiç
/// düşmüyordu: canlıda sahte içerik basan tek aksiyonun <b>kim tarafından çalıştırıldığı
/// hiçbir yerde yazmıyordu</b>.
/// </remarks>
public interface IMockDataSeeder
{
    Task<MockDataSeedResult> SeedAsync(CancellationToken ct = default);
}

/// <summary>Basma işleminin sonucu — hangi tabloya kaç satır yazıldı.</summary>
/// <remarks>
/// 🔴 <b>Sonuç raporu 12.19a'da eklendi ve bir dürüstlük düzeltmesidir</b> (plan dışı):
/// aksiyon o güne kadar <i>her</i> koşuda "Örnek veriler başarıyla eklendi." diyordu —
/// oysa <c>MockDataSeeder</c> tablo bazında idempotent, yani dolu bir veritabanında
/// <b>hiçbir şey yazmadan</b> aynı cümleyi kuruyordu. Yönetici için "eklendi" ile
/// "zaten vardı, dokunulmadı" arasındaki farkı görmenin <b>hiçbir yolu yoktu</b>
/// (§7'nin "hiçbir şey yapmayan buton" sınıfı — burada butonun kendisi değil <i>mesajı</i>
/// yalan söylüyordu).
/// </remarks>
/// <param name="Tables">
/// Yazılan satırlar, <b>tablo adı → eklenen satır sayısı</b>. Yalnız değişen tablolar bulunur.
/// ⚠️ Ham tablo adı (Türkçe etiket değil) bilinçli: aksiyon 12.19a'dan beri <b>yalnız
/// geliştirme ortamında</b> çizilir, yani okuyucusu geliştiricinin ta kendisidir ve
/// <c>ads (5)</c> onun için <i>İlanlar (5)</i>'den daha kullanışlıdır. Kullanıcıya dönük
/// hiçbir ekranda bu değerler görünmez.
/// </param>
public sealed record MockDataSeedResult(IReadOnlyDictionary<string, int> Tables)
{
    /// <summary>Toplam eklenen satır. 0 ise hiçbir tabloya dokunulmadı.</summary>
    public int TotalRows => Tables.Values.Sum();
}
