using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class IntercitySchedule : BaseEntity
{
    public Guid RouteId { get; set; }
    public TimeSpan DepartureTime { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Faz 12.5 — seferin çalıştığı günler, 7 bitlik maske (Pazartesi=1 … Pazar=64).
    /// Yorumlamanın tek sahibi <see cref="Enums.OperatingDays"/>.
    /// </summary>
    /// <remarks>
    /// 🔴 Varsayılan <see cref="Enums.OperatingDays.Daily"/> (127) ve 12.5 öncesi bütün satırlar
    /// migration'da 127 ile göç etti: o güne kadar <b>her sefer her gün varsayılıyordu</b>,
    /// yani davranış birebir korundu. 0 yasaktır — bkz. <see cref="Enums.OperatingDays.IsValid"/>.
    /// </remarks>
    public int OperatingDays { get; set; } = Enums.OperatingDays.Daily;

    public IntercityRoute Route { get; set; } = default!;
}
