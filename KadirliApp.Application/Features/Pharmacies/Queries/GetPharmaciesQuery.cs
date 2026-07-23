using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Queries;

/// <summary>OnlyActive=true (public uç): yalnız aktif eczaneler döner; istemcinin ?isActive= parametresi yok sayılır. Admin varsayılanla (false) filtreyi kullanır.</summary>
public record GetPharmaciesQuery(QueryPharmacyDto Dto, bool OnlyActive = false)
    : IRequest<PagedResult<PharmacyResponseDto>>, ICacheableQuery
{
    // Faz 10.7: OnlyActive cache anahtarında olmalı — public ve admin sonuçları aynı anahtarı paylaşmamalı.
    public string CacheKey => $"pharmacies:p{Dto.Page}:l{Dto.Limit}:s{Dto.Search}:a{Dto.IsActive}:pub{OnlyActive}";
    public string CacheGroup => CacheGroups.Pharmacies;
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(15);
}
