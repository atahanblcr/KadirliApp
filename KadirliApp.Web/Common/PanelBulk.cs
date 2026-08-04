using KadirliApp.Application.Common.Exceptions;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 11.18 — **toplu işlemin ortak çekirdeği** (11.15c B grubu).
///
/// Önceki durum: hiçbir listede satır seçimi yoktu, 40 bekleyen ilan tek tek
/// onaylanıyordu — her biri ayrı sayfa yüklemesi, ayrı tıklama.
///
/// 🔑 <b>En önemli karar: toplu işlem YENİ İŞ MANTIĞI YAZMAZ.</b> Seçilen her kimlik için
/// modülün **zaten var olan tek-kayıt komutu** çağrılır. Toplu bir SQL <c>UPDATE</c>
/// yazılsaydı, o yolda şunların hepsi sessizce kaybolurdu: denetim izi satırları
/// (<c>AuditBehavior</c> komut başına çalışır), önbellek geçersizleştirme, red gerekçesinin
/// temizlenmesi ve ⚠️ <b>süresi geçmiş ilana taze pencere verilmesi</b> (görünmez sözleşme
/// #25 — o kural onay komutunun içinde yaşıyor). Yani panel "42 ilan onaylandı" derdi ve
/// mobil hiçbirini göstermezdi; tam olarak 11.15c'nin kapattığı hata sınıfı.
///
/// ⚠️ Bir kaydın başarısız olması partiyi durdurmaz — 41 kaydı, 1 tanesi yüzünden geri
/// çevirmek yöneticiyi "hangisiydi?" diye aramaya bırakır. Başarısızlar sayılır ve
/// sonuç mesajında ayrıca belirtilir.
/// </summary>
public static class PanelBulk
{
    /// <summary>
    /// Tek istekte işlenebilecek azami kayıt. Sayfa boyu 100'ü geçmediği için pratikte
    /// hiç dolmaz; sınır, elle hazırlanmış devasa bir POST'un paneli meşgul etmesini önler.
    /// </summary>
    public const int MaxItems = 500;

    public sealed record Outcome(int Succeeded, int Failed)
    {
        public int Total => Succeeded + Failed;
        public bool NothingSelected => Total == 0;

        /// <summary>
        /// Sonucu Türkçe bir bildirime çevirir ve <c>TempData</c>'ya yazar.
        /// <paramref name="itemLabel"/> modülün adı ("ilan", "etkinlik"),
        /// <paramref name="verb"/> geçmiş zaman eylem ("onaylandı", "silindi").
        ///
        /// 📌 Uzantı metodu değil **örnek metodu**: uzantı olsaydı her controller'a
        /// <c>using KadirliApp.Web.Common;</c> eklemek gerekirdi ve unutulan yerde
        /// derleme hatası verirdi (ilk yazımda tam olarak öyle oldu).
        /// </summary>
        public void Report(
            Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary tempData,
            string itemLabel,
            string verb)
        {
            if (NothingSelected)
            {
                tempData["Error"] = "Hiçbir kayıt seçilmedi.";
                return;
            }

            if (Failed == 0)
            {
                tempData["Success"] = $"{Succeeded} {itemLabel} {verb}.";
                return;
            }

            if (Succeeded == 0)
            {
                tempData["Error"] = $"Seçilen {Failed} {itemLabel} işlenemedi.";
                return;
            }

            // Kısmi başarı bilinçli olarak **hata değil başarı** balonunda: iş büyük ölçüde
            // yapıldı; kaç tanesinin yapılmadığı da aynı cümlede yazıyor.
            tempData["Success"] = $"{Succeeded} {itemLabel} {verb}, {Failed} tanesi işlenemedi.";
        }
    }

    /// <summary>
    /// Seçilen kimlikleri sırayla işler. <paramref name="action"/> tek-kayıt komutunu
    /// gönderen delegedir; <c>false</c> dönmesi ya da <see cref="AppException"/> fırlatması
    /// "bu kayıt işlenemedi" sayılır.
    /// </summary>
    public static async Task<Outcome> RunAsync(IEnumerable<Guid>? ids, Func<Guid, Task<bool>> action)
    {
        var distinct = (ids ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(MaxItems)
            .ToList();

        var succeeded = 0;
        var failed = 0;

        foreach (var id in distinct)
        {
            try
            {
                if (await action(id)) succeeded++;
                else failed++;
            }
            catch (AppException)
            {
                // İş kuralı reddi (ör. zaten onaylanmış, süresi dolmuş) ya da kayıt yok
                // (`NotFoundException` da `AppException`'dan türüyor).
                // Partiyi durdurmaz — sayılır ve mesajda söylenir.
                failed++;
            }
        }

        return new Outcome(succeeded, failed);
    }

}
