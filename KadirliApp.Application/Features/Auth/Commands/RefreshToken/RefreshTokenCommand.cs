using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokens>;
