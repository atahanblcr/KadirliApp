using System;

namespace KadirliApp.Application.Features.Deaths.Dtos;

public record DeathNoticeResponseDto(
    Guid Id,
    string DeceasedName,
    Guid? PhotoFileId,
    DateTime FuneralDate,
    TimeSpan FuneralTime,
    Guid? CemeteryId,
    string? CemeteryName,
    Guid? MosqueId,
    string? MosqueName,
    Guid? NeighborhoodId,
    string? CondolenceAddress,
    decimal? CondolenceLatitude,
    decimal? CondolenceLongitude,
    Guid AddedBy,
    string Status,
    DateTime CreatedAt
)
{
    /// <summary>Mobil istemci taziye yeri için "Konuma Git" butonunu bu alana göre gösterir.</summary>
    public bool HasCondolenceLocation => CondolenceLatitude.HasValue && CondolenceLongitude.HasValue;

    /// <summary>Merhum fotoğrafının URL'i (files.cdn_url); fotoğraf yoksa null.</summary>
    public string? PhotoUrl { get; init; }
}
