using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public record ApproveDeathNoticeCommand(Guid Id, Guid AdminId) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "deaths";
    public string AuditAction => "approve";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "DeathNotice";
}
