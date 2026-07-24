using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.RejectAd;

public record RejectAdCommand(Guid AdId, Guid AdminId, string? Reason = null) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "reject";
    public Guid? AuditAffectedId => AdId;
    public string? AuditAffectedType => "Ad";
    public object? AuditDetails => Reason is not null ? new { reason = Reason } : null;
}
