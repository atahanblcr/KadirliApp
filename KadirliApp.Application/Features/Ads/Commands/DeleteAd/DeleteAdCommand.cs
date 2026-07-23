using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.DeleteAd;

public record DeleteAdCommand(Guid AdId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "ads";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => AdId;
    public string? AuditAffectedType => "Ad";
}
