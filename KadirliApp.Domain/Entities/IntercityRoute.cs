using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class IntercityRoute : BaseEntity
{
    public string Destination { get; set; } = default!;
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Faz 12.5 — <c>"bus"</c> / <c>"minibus"</c>. Metin olarak durur, dönüşümün tek sahibi
    /// <see cref="TransportVehicleTypes"/>. 12.5 öncesi satırlar migration'da <c>"bus"</c>
    /// ile göç etti (davranış değişmedi).
    /// </summary>
    public string VehicleType { get; set; } = TransportVehicleTypes.Default;

    /// <summary>
    /// Faz 12.5 — kalkış noktası sözlüğüne bağ. <c>null</c> = "henüz girilmemiş"
    /// (12.5 öncesinden kalma); panel bunu <b>uyarı</b> olarak gösterir.
    /// FK <c>SetNull</c>: sözlükte silme yok ama olsaydı hat kaybolmamalı.
    /// </summary>
    public Guid? DeparturePointId { get; set; }
    public TransportDeparturePoint? DeparturePoint { get; set; }

    public ICollection<IntercitySchedule> Schedules { get; set; } = new List<IntercitySchedule>();
}
