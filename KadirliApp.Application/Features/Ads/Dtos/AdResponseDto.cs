using System;
using System.Collections.Generic;

namespace KadirliApp.Application.Features.Ads.Dtos;

public record AdResponseDto(
    Guid Id, 
    string Title, 
    string? Description, 
    decimal? Price,
    string Status, 
    string ContactPhone, 
    int ViewCount,
    DateTime CreatedAt, 
    List<string> ImageUrls
);
