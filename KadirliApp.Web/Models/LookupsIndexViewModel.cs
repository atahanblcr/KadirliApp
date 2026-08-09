using KadirliApp.Application.Features.Lookups;

namespace KadirliApp.Web.Models;

/// <summary>Faz 10.9(d-panel): Tanımlar sayfasının 5 lookup listesini tek modelde taşır.</summary>
public class LookupsIndexViewModel
{
    public IReadOnlyList<NeighborhoodAdminDto> Neighborhoods { get; set; } = [];

    /// <summary>Faz 12.4 — il/ilçe sözlüğü (etkinlik konumunun kaynağı).</summary>
    public IReadOnlyList<DistrictAdminDto> Districts { get; set; } = [];
    /// <summary>Faz 12.5 — kalkış noktası sözlüğü (şehirlerarası hattın kalkış yerinin kaynağı).</summary>
    public IReadOnlyList<DeparturePointAdminDto> DeparturePoints { get; set; } = [];
    public IReadOnlyList<NamedLookupDto> Cemeteries { get; set; } = [];
    public IReadOnlyList<NamedLookupDto> Mosques { get; set; } = [];
    public IReadOnlyList<SluggedLookupDto> EventCategories { get; set; } = [];
    public IReadOnlyList<PlaceCategoryAdminDto> PlaceCategories { get; set; } = [];

    /// <summary>POST-redirect sonrası açık kalacak akordiyon bölümü.</summary>
    public string? OpenSection { get; set; }
}
