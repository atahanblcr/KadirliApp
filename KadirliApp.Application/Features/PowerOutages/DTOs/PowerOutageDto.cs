using System;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class PowerOutageDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Mahalle adı. Faz 12.3'ten beri <see cref="NeighborhoodId"/> doluyken <b>sözlükten
    /// türetilir</b>; alan adı ve tipi <b>değişmedi</b> (kontrat additive — mağazadaki eski
    /// sürümler mahalle eşleşmesini hâlâ bu metinden yapıyor).
    /// </summary>
    public string? Neighborhood { get; set; }

    /// <summary>Faz 12.3 (yeni): sözlükteki mahalle kimliği. Eski kayıtlarda ve şehir geneli kesintide <c>null</c>.</summary>
    public Guid? NeighborhoodId { get; set; }

    /// <summary>Faz 12.3 (yeni): mahallenin hangi kısmı ("Atatürk Caddesi ve çevresi").</summary>
    public string? AreaDetail { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }

    /// <summary>
    /// Faz 12.3 (yeni): bu kesinti için üretilmiş duyuru. Dolu olması "bildirim gönderildi"
    /// demektir — panel rozeti ve mobil derin bağlantısı buradan okunabilir.
    /// </summary>
    public Guid? AnnouncementId { get; set; }
}
