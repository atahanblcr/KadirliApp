namespace KadirliApp.Application.Features.Ads.Dtos;

/// <summary>POST /v1/ads/{id}/extend gövdesi (opsiyonel); AdsWatched mobilin "reklam izle, süre uzat" akışı için.</summary>
public record ExtendAdDto(int AdsWatched = 0);
