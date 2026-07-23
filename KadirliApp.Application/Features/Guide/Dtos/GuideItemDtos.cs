using System;

namespace KadirliApp.Application.Features.Guide.Dtos;

public class QueryGuideItemDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
}

public class GuideItemResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WorkingHours { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
