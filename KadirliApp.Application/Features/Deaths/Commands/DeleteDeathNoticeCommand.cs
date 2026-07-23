using System;
using KadirliApp.Application.Common.Auditing;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public record DeleteDeathNoticeCommand(Guid Id) : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => "deaths";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "DeathNotice";
}
