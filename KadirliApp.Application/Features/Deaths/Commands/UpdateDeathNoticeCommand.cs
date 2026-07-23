using System;
using KadirliApp.Application.Features.Deaths.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.Deaths.Commands;

public record UpdateDeathNoticeCommand(Guid Id, UpdateDeathNoticeDto Dto) : IRequest<bool>;
