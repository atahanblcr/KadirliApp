using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Phone) : IRequest<OtpRequestResult>;
