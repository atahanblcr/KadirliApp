using System;

namespace KadirliApp.Application.Features.Deaths.Dtos;

public record UpdateDeathNoticeDto(
    string DeceasedName,
    Guid? PhotoFileId,
    DateTime FuneralDate,
    TimeSpan FuneralTime,
    Guid? CemeteryId,
    Guid? MosqueId,
    Guid? NeighborhoodId,
    string? CondolenceAddress,
    decimal? CondolenceLatitude,
    decimal? CondolenceLongitude,
    string Status
);
