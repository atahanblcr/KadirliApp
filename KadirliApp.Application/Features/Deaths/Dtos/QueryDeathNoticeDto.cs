using System;

namespace KadirliApp.Application.Features.Deaths.Dtos;

public record QueryDeathNoticeDto(
    DateTime? Date,
    string? Search,
    string? Status,
    int Page = 1,
    int Limit = 20
);
