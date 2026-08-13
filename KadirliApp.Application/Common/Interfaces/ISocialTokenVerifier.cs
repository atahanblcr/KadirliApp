namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Sağlayıcının jetonundan <b>doğrulanmış</b> olarak çıkarılan kimlik.
/// </summary>
/// <remarks>
/// ⚠️ Bu nesne yalnız <see cref="ISocialTokenVerifier"/> tarafından üretilebilir. İstemciden
/// gelen hiçbir alan (ad, e-posta, <c>sub</c>) doğrudan kaydedilmez — hepsi imzası
/// doğrulanmış jetondan okunur. Aksi hâlde istemci "ben şu Google kullanıcısıyım" diyerek
/// başkasının hesabına bağlanabilirdi.
/// </remarks>
public sealed record SocialIdentityPayload(
    string Provider,
    string ProviderUserId,
    string? Email,
    bool EmailVerified,
    string? DisplayName);

/// <summary>
/// Faz 12.7 — sosyal giriş jetonunun <b>sunucuda</b> doğrulanması.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>İstemciye asla güvenilmez.</b> Mobil uygulama Google/Apple'dan aldığı <c>id_token</c>'ı
/// olduğu gibi gönderir; imza, <c>iss</c>, <c>aud</c> ve süre kontrolünü <b>bu arayüzün
/// gerçeklemesi</b> yapar.
/// </para>
/// <para>
/// 🔴 <b><c>aud</c> doğrulaması sosyal girişin bir numaralı gerçek zafiyetidir.</b> Doğrulanmazsa
/// <i>başka bir uygulamanın</i> Google jetonu bizde de geçerli olur: saldırgan kendi
/// uygulamasına giren kurbanın jetonunu alıp bizim hesabına girer. İmza doğru, issuer doğru,
/// süre doğru — ve hesap ele geçirilmiş olur. Bu yüzden ayrı testle kilitlidir (§7 madde 68).
/// </para>
/// </remarks>
public interface ISocialTokenVerifier
{
    /// <summary>
    /// Sağlayıcı <b>yapılandırılmış mı</b>? (Client id listesi boşsa kapalıdır.)
    /// </summary>
    /// <remarks>
    /// 🔴 Kapalı sağlayıcı <b>fail-closed</b>'dır: doğrulama yapılamıyorsa jeton kabul
    /// edilmez. "Yapılandırma yoksa geçir" tam olarak <c>aud</c> deliğinin en geniş hâli olurdu.
    /// </remarks>
    bool IsEnabled(string provider);

    /// <summary>Bugün açık olan sağlayıcılar (yapılandırma kapısı ve tanılama için).</summary>
    IReadOnlyList<string> EnabledProviders { get; }

    /// <summary>
    /// Jetonu doğrular. Geçersiz imza / yanlış <c>iss</c> / yanlış <c>aud</c> / süresi dolmuş
    /// jeton ve <b>kapalı sağlayıcı</b> için <c>null</c> döner — <b>asla fırlatmaz</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Sebeplerin tek bir <c>null</c>'a düşmesi bilinçli: çağıran zaten hepsini "geçersiz
    /// oturum" olarak ele alır ve <b>istemciye hangi kontrolün düştüğünü söylemek</b>
    /// saldırgana ücretsiz bir hata ayıklama kanalı açardı.
    /// </remarks>
    Task<SocialIdentityPayload?> VerifyAsync(string provider, string idToken, CancellationToken cancellationToken);
}
