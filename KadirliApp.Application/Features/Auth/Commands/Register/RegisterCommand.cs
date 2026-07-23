using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.Register;

/// <summary>
/// Kayıt tamamlama (masterclass 12.3): verify-otp'nin döndürdüğü TempToken + onboarding
/// ekranından gelen username/mahalle(/yaş). Başarıda tam token çifti döner.
/// </summary>
public record RegisterCommand(
    string TempToken,
    string Username,
    Guid PrimaryNeighborhoodId,
    int? Age) : IRequest<AuthTokens>;
