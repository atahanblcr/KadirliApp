using System;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Application.Common.Caching;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

public record CreatePharmacyCommand(CreatePharmacyDto Dto) : IRequest<Guid>, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Pharmacies };
}
