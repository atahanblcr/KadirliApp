using System;
using KadirliApp.Application.Features.Deaths.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public record CreateDeathNoticeCommand(CreateDeathNoticeDto Dto, Guid? AddedBy = null, bool AutoApprove = false) : IRequest<Guid>;
