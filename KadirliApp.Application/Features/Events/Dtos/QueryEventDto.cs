using System;

namespace KadirliApp.Application.Features.Events.Dtos;

public class QueryEventDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Status { get; set; }

    /// <summary>Etkinlik tarihi bu tarihte veya sonrasında olanlar (mobil: "yaklaşan etkinlikler").</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Etkinlik tarihi bu tarihte veya öncesinde olanlar.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>true → yalnızca ücretsiz, false → yalnızca ücretli etkinlikler.</summary>
    public bool? IsFree { get; set; }

    /// <summary>
    /// Sıralama: <c>date_asc</c> (tarihi en yakın önce) | <c>date_desc</c> (varsayılan, en ileri tarih önce).
    /// Faz 11.10: mobil "Yaklaşan etkinlikler" listesi StartDate filtresiyle birlikte date_asc kullanır —
    /// varsayılan azalan sırada ilk sayfa EN UZAK tarihli etkinlikleri getirdiği için sayfalama anlamsız oluyordu.
    /// Bilinmeyen değer varsayılana düşer (istemci hatası liste bozmaz).
    /// </summary>
    public string? Sort { get; set; }
}
