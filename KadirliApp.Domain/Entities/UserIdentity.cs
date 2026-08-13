using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.7 — bir hesabın bağlı sosyal kimliği (Google / Apple).
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b><see cref="User"/> tablosuna hiç dokunulmadı</b> — sosyal giriş tümüyle additive bir
/// tablo olarak eklendi. Telefon (<c>User.Phone</c>) <b>çıpa olarak kalır</b> (Faz 12 başında
/// alınan karar): 42 dosya kimliği ondan okuyor (JWT claim'i, OTP, ban, hesap silme, ilan
/// iletişimi) ve telefonsuz bir hesap <i>doğrulanmamış</i> kullanıcıya ilan verme/taksi çağırma
/// açardı. Sosyal giriş bu yüzden bir <b>kısayoldur</b>, ikinci bir kimlik modeli değil.
/// </para>
/// <para>
/// ⚠️ <c>(Provider, ProviderUserId)</c> <b>benzersizdir</b> — aynı Google hesabı iki
/// KadirliApp hesabına bağlanamaz. Bu kısıt olmasaydı bir kişi aynı sosyal hesapla iki
/// kayıt açıp <b>ilan/şikayet kısıtlarını</b> ikiye katlayabilirdi ve hiçbir yerde hata
/// görünmezdi.
/// </para>
/// <para>
/// 🔴 <b>Hesap silinince bu satırlar FİZİKSEL olarak silinir</b>
/// (<c>DeleteMyAccountCommand</c>). İki ayrı sebep, ikisi de sessiz hasar üretirdi:
/// (a) <c>ProviderUserId</c> + <c>Email</c> <b>kişisel veridir</b> ve hesap silme
/// anonimleştirme sözü verir — kalsalardı silinmiş hesabın Google adresi tabloda durmaya
/// devam ederdi; (b) benzersiz kısıt yüzünden o kişi <b>aynı Google hesabıyla bir daha asla
/// kayıt olamazdı</b> — telefonunu yeniden kayda açan bir silme akışının (10.8) tam tersi.
/// </para>
/// </remarks>
public class UserIdentity : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary><c>google</c> | <c>apple</c> — <see cref="Enums.SocialProviders"/>.</summary>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// Sağlayıcının kullanıcı kimliği (OIDC <c>sub</c>). ⚠️ E-posta <b>değildir</b> ve
    /// e-posta yerine kullanılamaz: kullanıcı Google hesabının e-postasını değiştirebilir,
    /// <c>sub</c> ise sabittir. Eşleştirme <b>yalnız</b> bu alandan yapılır.
    /// </summary>
    public string ProviderUserId { get; set; } = default!;

    /// <summary>Sağlayıcının bildirdiği e-posta. <b>Yalnız gösterim/ön doldurma içindir.</b></summary>
    public string? Email { get; set; }

    /// <summary>
    /// Sağlayıcı bu e-postayı doğrulamış mı? Saklanıyor çünkü <b>doğrulanmamış e-posta bir
    /// kimlik değildir</b>; ileride bir karar bu alana bakacaksa varsayması değil okuması gerekir.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>Sağlayıcının bildirdiği ad — kayıt formunu ön doldurmak için.</summary>
    public string? DisplayName { get; set; }

    public DateTime LinkedAt { get; set; }

    /// <summary>Bu kimlikle en son ne zaman giriş yapıldı (panelde "ölü bağlantı" görünür olsun diye).</summary>
    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
