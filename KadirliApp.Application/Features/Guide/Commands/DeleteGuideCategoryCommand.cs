using System;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Caching;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Commands;

public record DeleteGuideCategoryCommand(Guid Id) : IRequest<bool>, ICacheInvalidator, IAuditableCommand
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Guide };

    public string AuditModule => "guide";
    public string AuditAction => "delete-category";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "GuideCategory";
}
