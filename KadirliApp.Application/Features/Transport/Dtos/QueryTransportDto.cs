using KadirliApp.Application.Common.Models;

namespace KadirliApp.Application.Features.Transport.Dtos;

public class QueryTransportDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;

    /// <summary>
    /// ⚠️ Görünmez sözleşme #4: ulaşım ve taksi <b>`searchTerm`</b> kullanır, diğer modüller
    /// `search`. Yanlış ad 400 vermez, <b>sessizce yok sayılır</b>.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Faz 12.5: <c>"bus"</c> | <c>"minibus"</c>. Tanınmayan değer <b>süzmez</b>
    /// (<c>TransportVehicleTypes.NormalizeFilter</c>) — bir yazım hatası listeyi boşaltmamalı
    /// (<c>ARCHITECTURE.md</c> §5, 12.4'te <c>locationScope</c> için verilen aynı karar).
    /// </summary>
    public string? VehicleType { get; set; }
}
