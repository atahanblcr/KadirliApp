using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class UpdatePowerOutageDto
{
    /// <summary>Serbest metin mahalle — <see cref="NeighborhoodId"/> verilirse yok sayılır.</summary>
    public string? Neighborhood { get; set; }

    /// <summary>Faz 12.3: sözlükteki mahalle. Bildirim göndermenin ön koşulu.</summary>
    public Guid? NeighborhoodId { get; set; }

    /// <summary>Faz 12.3: mahallenin hangi kısmı.</summary>
    public string? AreaDetail { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }

    /// <summary>
    /// Faz 12.3: bildirim <b>henüz gönderilmemişse</b> gönderilsin mi.
    /// ⚠️ Gönderilmiş bir bildirimi geri almaz — kutuyu boşaltmak duyuruyu silmez
    /// (<c>FcmSent</c> terminaldir, görünmez sözleşme #37).
    /// </summary>
    public bool SendNotification { get; set; }

    /// <summary>Bildirimin gideceği ek mahalleler.</summary>
    public List<Guid> TargetNeighborhoodIds { get; set; } = new();
}
