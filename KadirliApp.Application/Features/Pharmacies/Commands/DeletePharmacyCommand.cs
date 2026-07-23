using KadirliApp.Application.Common.Auditing;
using System;
using KadirliApp.Application.Common.Caching;
using MediatR;

namespace KadirliApp.Application.Features.Pharmacies.Commands;

public record DeletePharmacyCommand(Guid Id) : IRequest<bool>, ICacheInvalidator, IAuditableCommand
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Pharmacies };

    public string AuditModule => "pharmacies";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "Pharmacy";
}
