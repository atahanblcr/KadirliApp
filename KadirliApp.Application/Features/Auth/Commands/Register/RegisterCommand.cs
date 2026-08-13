using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Auth.Commands.Register;

/// <summary>
/// Kayıt tamamlama (masterclass 12.3): verify-otp'nin döndürdüğü TempToken + onboarding
/// ekranından gelen username/mahalle(/yaş). Başarıda tam token çifti döner.
/// </summary>
/// <param name="SocialToken">
/// Faz 12.7 — <b>opsiyonel</b> sosyal kayıt taşıyıcısı (<c>POST /v1/auth/social</c>).
/// Verilirse kayıt biterken sosyal kimlik <b>aynı işlemde</b> hesaba bağlanır.
/// <para>
/// 🔴 <b>İKİ jeton birden isteniyor ve bu, fazın en önemli güvenlik kararı.</b> Telefon
/// <see cref="TempToken"/>'dan (yani <b>doğrulanmış bir OTP'den</b>) gelir; sosyal jeton
/// telefon <b>taşımaz</b>. Tek bir jetona indirgenseydi — yani sosyal jeton telefonu da
/// taşısaydı — Google hesabı olan herkes <b>OTP'siz</b> hesap açar ve "her hesabın
/// doğrulanmış bir telefonu vardır" varsayımı sessizce çökerdi (§7 madde 70).
/// </para>
/// <para>
/// ⚠️ Alan <b>additive</b>: göndermeyen istemciler (bugünkü mobil sürüm) hiçbir değişiklik
/// görmez.
/// </para>
/// </param>
public record RegisterCommand(
    string TempToken,
    string Username,
    Guid PrimaryNeighborhoodId,
    int? Age,
    string? SocialToken = null) : IRequest<AuthTokens>;
