using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.VerifyOtp;

public record VerifyOtpCommand(string Phone, string Otp) : IRequest<VerifyOtpResult>;

/// <summary>
/// OTP doğrulama sonucu (masterclass 12.3): kullanıcı kayıtlıysa tam token çifti,
/// değilse yalnız TempToken (istemci POST /v1/auth/register ile kaydı tamamlar).
/// </summary>
public sealed record VerifyOtpResult(
    bool IsNewUser,
    string? TempToken,
    string? AccessToken,
    string? RefreshToken,
    int? ExpiresIn)
{
    public static VerifyOtpResult NewUser(string tempToken) =>
        new(true, tempToken, null, null, null);

    public static VerifyOtpResult ExistingUser(AuthTokens tokens) =>
        new(false, null, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
}
