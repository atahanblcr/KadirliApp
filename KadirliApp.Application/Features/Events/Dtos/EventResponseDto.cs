using System;

namespace KadirliApp.Application.Features.Events.Dtos;

public class EventResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }

    // ---- Faz 12.4: konum (additive — eski istemciler bu alanları yok sayar) ----

    /// <summary>Sözlükteki ilçe kimliği; <c>null</c> = konumu bilinmeyen eski kayıt.</summary>
    public Guid? DistrictId { get; set; }

    /// <summary>İlçe adı ("Kadirli", "Merkez").</summary>
    public string? DistrictName { get; set; }

    /// <summary>İl adı ("Osmaniye", "Adana").</summary>
    public string? ProvinceName { get; set; }

    /// <summary>
    /// 🔴 Kullanıcıya gösterilecek <b>hazır</b> konum metni ("Kadirli" · "Osmaniye / Merkez" ·
    /// "Adana"). Sunucuda tek yerde üretilir (<c>DistrictLabel</c>) — istemcide üretilseydi
    /// panel ile mobil aynı etkinliği farklı yazardı ve kimse hata almazdı.
    /// </summary>
    public string? LocationLabel { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public bool IsLocal { get; set; }
    public Guid? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
