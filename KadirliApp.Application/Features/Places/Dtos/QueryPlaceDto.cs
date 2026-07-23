using System;

namespace KadirliApp.Application.Features.Places.Dtos;

public class QueryPlaceDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
}
