using System;
using System.Text;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages;

/// <summary>
/// Faz 12.3 — kesintiden doğan duyurunun <b>başlığı ve gövdesi</b>. Saf sınıf, birim testli.
/// </summary>
/// <remarks>
/// 🔑 Metin neden ayrı bir sınıfta: bu iki dize hem duyuru kaydına hem <b>push bildiriminin
/// kendisine</b> gidiyor — yani vatandaşın kilit ekranında okuyacağı şey. Handler'ın içine
/// gömülseydi test edilebilmesi için veritabanı gerekirdi ve "saat doğru yazılıyor mu?"
/// sorusu bir entegrasyon testine bağlanırdı.
///
/// ⚠️ <b>Saatler TR yerel saatiyle (UTC+3) yazılır.</b> Kayıtlar UTC tutuluyor; gövdede ham
/// UTC yazılsaydı "20:00–23:00" olan kesinti bildirimde "17:00–20:00" görünürdü ve kimse
/// hata almazdı (görünmez sözleşme #6'nın aynı sınıfı). Mobil tarafta da sabit +03 var
/// (<c>AppDate</c>) — iki taraf aynı varsayımda.
/// </remarks>
public static class PowerOutageAnnouncementText
{
    /// <summary>Türkiye kalıcı olarak UTC+3 (2016'dan beri yaz saati yok) — mobildeki <c>AppDate</c> ile aynı sabit.</summary>
    public static readonly TimeSpan TurkeyOffset = TimeSpan.FromHours(3);

    private static readonly string[] Months =
    [
        "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
        "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"
    ];

    public static string Title(PowerOutage outage)
    {
        var place = string.IsNullOrWhiteSpace(outage.Neighborhood)
            ? "Kadirli geneli"
            : outage.Neighborhood!.Trim();

        return $"Elektrik kesintisi: {place}";
    }

    public static string Body(PowerOutage outage)
    {
        var sb = new StringBuilder();

        sb.Append(Range(outage.StartTime, outage.EndTime));

        if (!string.IsNullOrWhiteSpace(outage.AreaDetail))
            sb.Append(" · ").Append(outage.AreaDetail!.Trim());

        if (!string.IsNullOrWhiteSpace(outage.Reason))
            sb.Append(" · ").Append(outage.Reason!.Trim());

        return sb.ToString();
    }

    /// <summary>
    /// "7 Ağustos 09:00 – 13:00" (aynı gün) ya da "7 Ağustos 22:00 – 8 Ağustos 06:00".
    /// Gün aşan kesintide bitiş tarihi <b>yazılmak zorunda</b>: yalnız saat yazılsaydı
    /// "22:00 – 06:00" okuyan vatandaş kesintinin 16 saat değil 0 saat sürdüğünü sanardı.
    /// </summary>
    public static string Range(DateTime startUtc, DateTime endUtc)
    {
        var start = ToLocal(startUtc);
        var end = ToLocal(endUtc);

        var sameDay = start.Date == end.Date;
        return sameDay
            ? $"{Day(start)} {Time(start)} – {Time(end)}"
            : $"{Day(start)} {Time(start)} – {Day(end)} {Time(end)}";
    }

    private static DateTime ToLocal(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).Add(TurkeyOffset);

    private static string Day(DateTime local) => $"{local.Day} {Months[local.Month - 1]}";

    private static string Time(DateTime local) => $"{local.Hour:D2}:{local.Minute:D2}";
}
