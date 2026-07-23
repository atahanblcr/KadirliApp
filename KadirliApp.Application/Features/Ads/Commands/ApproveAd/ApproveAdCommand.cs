using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.ApproveAd;

public record ApproveAdCommand(Guid AdId, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "approve";
    public Guid? AuditAffectedId => AdId;
    public string? AuditAffectedType => "Ad";
}
