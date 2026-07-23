using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.Logout;

/// <summary>UserId access token claim'inden gelir (body'den değil); RefreshToken opsiyoneldir.</summary>
public record LogoutCommand(Guid UserId, string? RefreshToken) : IRequest;
