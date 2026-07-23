using System;

namespace KadirliApp.Application.Features.Guide.Dtos;

public class QueryGuideCategoryDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? ParentId { get; set; }
}
