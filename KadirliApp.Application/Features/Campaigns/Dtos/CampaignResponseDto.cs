using System;

namespace KadirliApp.Application.Features.Campaigns.Dtos;

public class CampaignResponseDto
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string? BusinessName { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? DiscountPercentage { get; set; }
    public string? DiscountCode { get; set; }
    public string? Terms { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int CodeViewCount { get; set; }
    public Guid? CoverImageId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
