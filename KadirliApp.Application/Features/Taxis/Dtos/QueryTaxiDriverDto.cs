using KadirliApp.Application.Common.Models;

namespace KadirliApp.Application.Features.Taxis.Dtos;

public class QueryTaxiDriverDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public bool? IsVerified { get; set; }
    public bool? IsActive { get; set; }
}
