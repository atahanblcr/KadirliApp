using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PowerOutage : BaseEntity
{
    /// <summary>
    /// Faz 12.3'te <b>doldurulmaya başlandı</b>: kesinti bildirimi bir duyurudur ve bu alan
    /// o duyuruyu işaret eder. 10.x'ten beri var olan ama hiç yazılmayan çengeldi.
    /// </summary>
    public Guid? AnnouncementId { get; set; }

    /// <summary>
    /// 🔴 <b>TÜRETİLMİŞ ALAN (Faz 12.3'ten beri).</b> <see cref="NeighborhoodId"/> doluyken
    /// değer sözlükten (<see cref="Entities.Neighborhood.Name"/>) yazılır, elle düzenlenmez.
    /// </summary>
    /// <remarks>
    /// Kolon <b>bilerek duruyor ve DTO'daki adı değişmedi</b>: <c>GET /v1/power-outages</c>
    /// bu alanı düz metin olarak veriyor ve mağazadaki eski sürümler mahalle eşleşmesini
    /// <b>ad üzerinden</b> yapıyor (<c>power_outage.dart → matchesNeighborhood</c>). Kolonu
    /// kaldırmak ya da adını değiştirmek kırıcı bir değişiklik olurdu (<c>ARCHITECTURE.md</c> §5);
    /// türetmek ise additive: eski istemci aynı metni görmeye devam eder, üstelik artık
    /// yazım farkı olmadığı için eşleşme <b>daha</b> güvenilirdir.
    ///
    /// ⚠️ FK'sı olmayan (12.3 öncesinden kalan, geri doldurmada eşleşmemiş) kayıtlarda bu alan
    /// hâlâ serbest metindir — ve o kayıtlar <b>bildirim gönderemez</b>, çünkü hedeflenecek
    /// bir mahalle kimliği yoktur.
    /// </remarks>
    public string? Neighborhood { get; set; }

    /// <summary>
    /// Faz 12.3: kesintinin sözlükteki mahallesi. <c>null</c> = şehir geneli ya da henüz
    /// eşleşmemiş eski kayıt.
    /// </summary>
    public Guid? NeighborhoodId { get; set; }

    /// <summary>
    /// Faz 12.3: mahallenin hangi kısmı ("Atatürk Caddesi ve çevresi"). Kesinti çoğu zaman
    /// mahallenin tamamını kapsamıyor; bu ayrıntı önce serbest metin mahalle alanına
    /// sıkıştırılıyordu ve sözlük eşleşmesini imkânsız kılan asıl sebep oydu.
    /// </summary>
    public string? AreaDetail { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
    public string? Source { get; set; }

    public Announcement? Announcement { get; set; }
    public Neighborhood? NeighborhoodRef { get; set; }
}
