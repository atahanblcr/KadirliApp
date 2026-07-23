using System;

namespace KadirliApp.Application.Features.Campaigns.Dtos;

public class QueryCampaignDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
    public string? Search { get; set; }
    public Guid? BusinessId { get; set; }

    /// <summary>İşletme kategorisine göre filtre (business_categories.id).</summary>
    public Guid? CategoryId { get; set; }

    public string? Status { get; set; }

    /// <summary>true ise yalnızca tarih aralığı geçerli (aktif) kampanyalar döner.</summary>
    public bool OnlyActive { get; set; }
}
