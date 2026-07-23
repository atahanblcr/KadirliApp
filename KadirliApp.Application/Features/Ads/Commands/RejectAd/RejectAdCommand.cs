using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.RejectAd;

public record RejectAdCommand(Guid AdId, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "reject";
    public Guid? AuditAffectedId => AdId;
    public string? AuditAffectedType => "Ad";
}
