using System;
using KadirliApp.Application.Features.Pharmacies.Dtos;
using KadirliApp.Application.Common.Caching;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

public record UpdatePharmacyCommand(Guid Id, UpdatePharmacyDto Dto) : IRequest<bool>, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Pharmacies };
}
