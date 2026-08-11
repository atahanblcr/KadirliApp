using KadirliApp.Application.Features.News.Commands;

namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 12.13 — panelin "Senkronu başlat" butonunun <b>kuyruğa atma</b> kapısı.
/// </summary>
/// <remarks>
/// 🔴 <b>Neden istek içinde koşturmuyoruz:</b> bir koşu en kötü hâlde 20 sayfa × 30 sn
/// sürebiliyor. İstek içinde çalıştırılsaydı panelin (ve önündeki vekilin) zaman aşımı
/// dolar, yönetici sayfayı yeniler ve <b>ikinci bir koşu</b> başlatırdı — düzeltmeye
/// çalıştığımız şeyin ta kendisi. Buton kuyruğa atıp <b>hemen</b> döner.
///
/// 🔑 Arayüz Application'da, gerçeklemesi Infrastructure'da (Hangfire): katman kuralı gereği
/// Application <c>BackgroundJob</c>'ı göremez — ve bu ayrım testlerin kuyruğu sahteleyip
/// <b>gerçekten kuyruğa atıldı mı</b> sorusunu sorabilmesini sağlıyor.
///
/// ⚠️ Kuyruğa atmak <b>koşunun açılacağını garanti etmez</b>: o sırada başka bir koşu
/// sürüyorsa veritabanındaki kısmi unique indeks ikincisini reddeder ve iş
/// <c>NewsSyncOutcome.AlreadyRunning</c> ile döner. Panelin mesajı bu yüzden "başlatıldı"
/// değil <b>"kuyruğa alındı"</b> der — söylediğimiz şey, bildiğimiz şeyden fazla olamaz.
/// </remarks>
public interface INewsSyncQueue
{
    /// <summary>Koşuyu arka plana atar ve iş kimliğini döner.</summary>
    string Enqueue(NewsSyncRequestMode mode, Guid? adminId);
}
