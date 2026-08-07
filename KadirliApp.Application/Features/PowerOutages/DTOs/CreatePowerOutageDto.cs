using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class CreatePowerOutageDto
{
    /// <summary>
    /// Serbest metin mahalle — <b>yalnız geriye dönük uyumluluk için</b> duruyor.
    /// <see cref="NeighborhoodId"/> verilirse yok sayılır ve ad sözlükten yazılır.
    /// </summary>
    public string? Neighborhood { get; set; }

    /// <summary>Faz 12.3: sözlükteki mahalle. Bildirim göndermenin <b>ön koşulu</b>.</summary>
    public Guid? NeighborhoodId { get; set; }

    /// <summary>Faz 12.3: mahallenin hangi kısmı.</summary>
    public string? AreaDetail { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }

    /// <summary>Faz 12.3: bu kesinti için duyuru + bildirim üretilsin mi.</summary>
    public bool SendNotification { get; set; }

    /// <summary>
    /// Faz 12.3: bildirimin gideceği <b>ek</b> mahalleler. Kesintinin kendi mahallesi her
    /// zaman dâhildir; bu liste onu genişletir (bir trafo komşu mahalleyi de karartabilir).
    /// </summary>
    public List<Guid> TargetNeighborhoodIds { get; set; } = new();
}
