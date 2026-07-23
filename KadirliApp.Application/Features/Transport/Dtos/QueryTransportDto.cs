using KadirliApp.Application.Common.Models;

namespace KadirliApp.Application.Features.Transport.Dtos;

public class QueryTransportDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? SearchTerm { get; set; }
}
