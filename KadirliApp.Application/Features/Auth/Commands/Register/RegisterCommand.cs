using System.Collections.Generic;
using System.Net;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Legal.Dtos;
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
/// <param name="Consents">
/// Faz 12.16 — kullanıcının <b>gördüğü sürümlere</b> verdiği KVKK kararları (§7 madde 71).
/// <para>
/// 🔴 <b>Zorunlu belgelerin hepsi <c>granted=true</c> gelmeden kayıt TAMAMLANMAZ</b> ve
/// eksik gelirse komut <b>sebebini söyler</b> (<c>MISSING_CONSENT</c> + hangi belge).
/// Sessizce kaydetmek, bu bloğun kapatmaya çalıştığı hasarın ta kendisi olurdu.
/// </para>
/// <para>
/// ⚠️ Alan <b>additive</b> (§5): göndermeyen istemciler için davranış, zorunlu <b>ve yayında
/// sürümü olan</b> bir belge var olana kadar birebir eskisi gibidir. Zorunluluğun kendisi
/// ayrıca bir <b>yapılandırma kapısına</b> bağlı (<c>LegalSettings</c>) — mağazaya çıkılmış
/// olsa bile eski sürümler tek commit'te kırılmasın diye.
/// </para>
/// </param>
/// <param name="IpAddress">⚠️ Sunucuda doldurulur (controller) — rıza kaydının bağlamı.</param>
/// <param name="UserAgent">⚠️ Sunucuda doldurulur (controller) — rıza kaydının bağlamı.</param>
public record RegisterCommand(
    string TempToken,
    string Username,
    Guid PrimaryNeighborhoodId,
    int? Age,
    string? SocialToken = null,
    List<ConsentDecisionDto>? Consents = null,
    IPAddress? IpAddress = null,
    string? UserAgent = null) : IRequest<AuthTokens>;
