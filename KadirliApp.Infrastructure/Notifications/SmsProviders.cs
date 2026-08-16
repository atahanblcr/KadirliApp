namespace KadirliApp.Infrastructure.Notifications;

/// <summary>
/// Faz 12.21b — <b>hangi SMS sağlayıcılarının GERÇEKTEN gerçeklendiğinin tek sahibi.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu sınıf ölçülmüş bir kilitlenmeden doğdu.</b> 12.21'in paketleme adımında API
/// Production'da başlatılmak istendi ve <b>hiçbir değerle açılmadığı</b> görüldü:
/// </para>
/// <list type="bullet">
/// <item><c>Sms:Provider=Dev</c> → <c>ProductionReadinessGuard</c> açılışı durdurur
/// (*"OTP kodu SMS ile gönderilmez … HİÇBİR kullanıcı giriş yapamaz"*) — <b>haklı olarak</b>.</item>
/// <item><c>Sms:Provider=</c>başka bir şey → <c>AddInfrastructure</c>
/// *"Bilinmeyen SMS sağlayıcısı"* fırlatır — çünkü <b>gerçeklenmiş başka sağlayıcı yok</b>.</item>
/// </list>
/// <para>
/// İki kapı da tek başına doğru, birlikte <b>geçilemez</b>. Bu bir hata değil, gerçek bir
/// yayın blokajıdır: SMS olmadan kimse kayıt olamaz/giremez, yani sistem zaten yayına
/// hazır değildir. Yanlış olan tek şey, bunun <b>hiçbir yerde yazmıyor</b> olmasıydı.
/// </para>
/// <para>
/// 🔑 <b>Bu sınıfın işi o boşluğu kapatmak:</b> hem DI kaydı hem readiness kapısı aynı
/// listeden konuşur, yani kapının mesajı operatöre *"bugün seçebileceğin bir alternatif
/// yok"*u <b>söyler</b>. Öncesinde operatör mesajı okuyup <c>Sms:Provider=Netgsm</c>
/// yazar ve <b>tamamen ilgisiz görünen</b> ikinci bir hatayla karşılaşırdı.
/// </para>
/// <para>
/// 🐛 <b>Ve bunu hiçbir test söylemiyordu — çünkü testin kendisi de aynı hatayı yapıyordu:</b>
/// <c>ProductionReadinessGuardTests.HealthyProductionSettings()</c> *"sağlıklı üretim
/// yapılandırması"* olarak <c>Sms:Provider = "Netgsm"</c> veriyordu; yani kapı, <b>hiçbir
/// zaman var olamayacak</b> bir yapılandırmayla doğrulanıyordu. Kilit artık
/// <c>SmsProviderAgreementTests</c>'te ve <b>iki yönlü</b>.
/// </para>
/// <para>
/// ➕ <b>Yeni bir sağlayıcı yazarken:</b> <c>ISmsService</c> gerçeklemesini
/// <c>Infrastructure/Notifications/</c> altına koy, adını buraya ekle ve
/// <see cref="KadirliApp.Infrastructure.DependencyInjection"/>'daki haritaya kaydet.
/// Üçünden biri unutulursa test kırmızıya döner.
/// </para>
/// </remarks>
public static class SmsProviders
{
    /// <summary>Log'a yazan geliştirme adaptörü — üretimde <b>kabul edilmez</b>.</summary>
    public const string Dev = "Dev";

    /// <summary>
    /// Bugün <b>gerçeklenmiş</b> sağlayıcı adları. Liste DI haritasından türetilir,
    /// elle ikinci bir kopya tutulmaz.
    /// </summary>
    public static IReadOnlyCollection<string> Implemented =>
        DependencyInjection.SmsImplementations.Keys.ToArray();

    /// <summary>
    /// Üretimde seçilebilecek sağlayıcılar = gerçeklenmiş olanlar <b>eksi</b> <see cref="Dev"/>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bugün <b>BOŞ</b> ve bu, projenin bilinen tek yayın blokajıdır. Gerçek bir
    /// sağlayıcı yazıldığı gün kendiliğinden dolar; kimsenin bu dosyayı hatırlaması
    /// gerekmez.
    /// </remarks>
    public static IReadOnlyCollection<string> ProductionCapable =>
        Implemented.Where(p => !string.Equals(p, Dev, StringComparison.OrdinalIgnoreCase)).ToArray();

    /// <summary>
    /// Yapılandırmayı yazan insana <b>ne yazabileceğini</b> söyleyen cümle.
    /// Hem DI'nin hata mesajı hem readiness kapısı bunu kullanır.
    /// </summary>
    public static string AvailabilitySentence() =>
        ProductionCapable.Count == 0
            ? "🔴 BUGÜN SEÇEBİLECEĞİNİZ BAŞKA BİR DEĞER YOK: projede gerçeklenmiş tek SMS " +
              $"sağlayıcısı '{Dev}' ve o da üretimde kabul edilmiyor. Yayın için önce bir " +
              "ISmsService gerçeklemesi yazılmalı (Infrastructure/Notifications/), adı " +
              "SmsProviders'a ve DependencyInjection.SmsImplementations haritasına eklenmeli."
            : $"Üretimde seçilebilecek sağlayıcılar: {string.Join(", ", ProductionCapable)}.";
}
