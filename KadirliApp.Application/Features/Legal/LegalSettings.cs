namespace KadirliApp.Application.Features.Legal;

/// <summary>
/// Faz 12.16 — KVKK bloğunun <b>yapılandırma kapısı</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden bir kapı var?</b> <c>register</c>'a eklenen <c>consents</c> alanı §5'e göre
/// <b>additive</b>'dir (eski istemciler onu hiç göndermez, alan yok sayılır). Ama
/// <i>zorunluluk</i> additive değildir: zorunlu bir belge yayına alındığı gün, o alanı
/// göndermeyen <b>her eski sürümde kayıt 400 döner</b>. Bu kapı, o günü bir
/// <b>yapılandırma kararına</b> çeviriyor — mağazaya çıkılmış olsa bile zorunluluk tek
/// commit'le değil, bilinçli bir ayarla açılır/kapanır.
/// </para>
/// <para>
/// 🔴 <b>Varsayılan <c>true</c> ve bu bilinçli.</b> Uygulama henüz mağazada değil, yani
/// bugün zorunlu kılmanın bedeli <b>sıfır</b>; ve kapının kapalı olduğu her gün, "rızasız
/// kayıt" mümkün olduğu bir gündür. ⚠️ Ayrıca kapı açıkken bile tek başına hiçbir şeyi
/// zorunlu <b>kılmaz</b>: zorunluluk ancak <i>yayında sürümü olan</i> zorunlu bir belge
/// varsa doğar (<see cref="LegalConsentRules"/>), <c>DbSeeder</c> de metin seed etmez.
/// Yani taze bir kurulumda kapı açıktır ve kayıt akışı <b>birebir eskisi gibi</b> çalışır.
/// </para>
/// <para>
/// ⚠️ Ayar <b>çözülme anında</b> okunur, DI kaydında değil (12.7'nin bulduğu gerçek hata,
/// <c>ARCHITECTURE.md</c> §8): kayıt anında okunan bir değer <c>ConfigureAppConfiguration</c>
/// ile ezilemez, yani <b>kod kendi testinden erişilemez</b> olurdu.
/// </para>
/// </remarks>
public sealed class LegalSettings
{
    /// <summary>Yapılandırma bölümü: <c>Legal</c>.</summary>
    public const string SectionName = "Legal";

    /// <summary>
    /// Zorunlu rızalar olmadan <c>POST /v1/auth/register</c> reddedilsin mi?
    /// </summary>
    public bool RequireConsentAtRegistration { get; init; } = true;
}
