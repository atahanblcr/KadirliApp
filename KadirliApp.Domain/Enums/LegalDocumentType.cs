namespace KadirliApp.Domain.Enums;

/// <summary>
/// Faz 12.16 — hukuki belge türü. Değer veritabanında ve DTO'da <b>metin</b> olarak durur
/// (<c>"kvkk_aydinlatma"</c>, <c>"acik_riza"</c>…), enum'un sayısal sırası değil.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Bu değerler <b>kontrattır</b> (§7 madde 47'nin <c>vehicle_type</c> kararının aynısı):
/// mobil <c>GET /v1/legal/documents/{type}</c> ile tek belge okuyor ve ayarlar ekranındaki
/// bağlantılar bu metinlere gidiyor. Yeniden adlandırılırsa mağazadaki eski sürümler
/// <b>404 alır</b> ve kullanıcı onayladığı metni bir daha göremez.
/// </para>
/// <para>
/// 🔴 <see cref="Normalize"/> bilinmeyen değeri <b>varsayılana DÜŞÜRMEZ</b> — <c>null</c>
/// döner. Kardeş sınıf <see cref="TransportVehicleTypes"/>'ın davranışının tersi ve
/// <see cref="SocialProviders"/>'ınkiyle aynı; sebep de aynı sınıftan: orada bedel bir kaydın
/// yanlış süzülmesiydi, burada bedel <b>yanlış hukuki metni göstermek</b> olurdu.
/// <c>?type=acik-riza</c> yazan bir istemciye "aydınlatma metni" döndürmek, kullanıcıya
/// okumadığı bir belgeyi onaylatmanın en sessiz yoludur.
/// </para>
/// </remarks>
public static class LegalDocumentTypes
{
    /// <summary>KVKK aydınlatma metni (m.10) — rıza değil <b>bilgilendirme</b>dir.</summary>
    public const string Kvkk = "kvkk_aydinlatma";

    /// <summary>Açık rıza metni (m.5/1) — asıl "onaylıyorum" kutusu budur.</summary>
    public const string ExplicitConsent = "acik_riza";

    public const string TermsOfUse = "kullanim_kosullari";
    public const string PrivacyPolicy = "gizlilik_politikasi";

    /// <summary>Ticari elektronik ileti izni — <b>her zaman isteğe bağlıdır</b> (bkz. sınıf notu).</summary>
    public const string CommercialMessage = "ticari_ileti";

    public static IReadOnlyList<string> All { get; } =
        new[] { Kvkk, ExplicitConsent, TermsOfUse, PrivacyPolicy, CommercialMessage };

    /// <summary>
    /// Ham metni kanonik hâle getirir; <b>tanınmayan/boş değer için <c>null</c></b>
    /// ("böyle bir belge yok" — varsayılana düşülmez, bkz. sınıf açıklaması).
    /// </summary>
    public static string? Normalize(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        return value is not null && All.Contains(value) ? value : null;
    }

    public static bool IsKnown(string? raw) => Normalize(raw) is not null;
}

/// <summary>
/// Faz 12.16 — bir rızanın <b>hangi ekrandan</b> alındığı. KVKK'nın "rıza nasıl alındı?"
/// sorusunun cevabı; denetimde kanıtın bağlamıdır.
/// </summary>
/// <remarks>
/// ⚠️ Değer <b>sunucuda sabitlenir</b>, istemciden gelmez (§7 madde 33'ün
/// <c>ErrorLog.Source</c> kararının aynısı): istemci "registration" diyebilseydi ayarlardan
/// verilmiş bir rızayı kayıt anında alınmış gibi gösterebilir ve kanıtın bağlamı
/// <b>hiçbir hata vermeden</b> yalan söylerdi.
/// </remarks>
public static class ConsentSources
{
    /// <summary>Kayıt akışı (<c>POST /v1/auth/register</c>).</summary>
    public const string Registration = "registration";

    /// <summary>Ayarlar ekranı (<c>POST /v1/users/me/consents</c>).</summary>
    public const string Settings = "settings";

    /// <summary>Yeniden onay akışı — yeni sürüm <c>RequiresReconsent</c> işaretliyken.</summary>
    public const string Reconsent = "reconsent";

    public static IReadOnlyList<string> All { get; } = new[] { Registration, Settings, Reconsent };
}
