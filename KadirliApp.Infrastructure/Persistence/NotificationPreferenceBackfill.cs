namespace KadirliApp.Infrastructure.Persistence;

/// <summary>
/// 🔑 12.15b'nin geri doldurma ifadesinin <b>tek sahibi</b> (§7 madde 67).
///
/// <para>
/// <c>notification_preferences</c> tek bir JSON kolonda saklanıyor
/// (<c>OwnsOne(...).ToJson()</c>) ve <b>ölçüldü</b>: EF'in JSON materyalizasyonu varsayılan
/// başlatıcıyı <b>çalıştırmıyor</b> — <c>News { get; set; } = true;</c> yazılmış olmasına
/// rağmen anahtarsız bir JSON <c>false</c> okunuyor (canlıda 13/13 satırda anahtar yoktu).
/// Bu ifade olmadan 12.15b mevcut <b>bütün</b> kullanıcıları haber bildiriminden sessizce
/// çıkarırdı.
/// </para>
///
/// <para>
/// 🔴 <b>Neden ayrı bir sabit (Faz 0 denetimi — T2):</b> ifade migration'ın gövdesinde
/// yaşarken <b>hiçbir test onu koşturamıyordu</b> — migration bir kez koşar, üstelik test
/// veritabanında <b>boş</b> bir <c>users</c> tablosunda koşar ve satırları sonradan EF yazar
/// (EF her zaman tam JSON yazar), yani <i>anahtarsız satır test ortamında hiç doğmaz</i> ve
/// "geri doldurma çalıştı" iddiası <b>tanım gereği vakumdur</b>. Bozma turunda ölçüldü:
/// <c>Up()</c> boşaltıldı, test yeşil kaldı.
/// </para>
///
/// <para>
/// ⚠️ Bu, planda yazılı olan sebebin <b>düzeltilmiş hâlidir</b>: eski gerekçe *"test
/// veritabanı koşular arasında yeniden kullanılıyor"* diyordu — yanlış. Testcontainers
/// her koşuda sıfırdan bir container kuruyor (<c>WithReuse</c> yok, her koşuda migration'lar
/// baştan uygulanıyor). Aynı gözlemin iki farklı sebebi vardı ve yanlış olanı seçilmişti;
/// bu projede yanlış bir sebep, yanlış bir düzeltmeden pahalıdır (12.13'ün dersi):
/// o gerekçeye inanan biri "ayrı, tek kullanımlık bir veritabanı" çözümüne giderdi ve
/// <b>o çözüm işe yaramazdı</b> — sıfırdan kurulan bir veritabanında da eski biçimli satır yoktur.
/// </para>
///
/// <para>
/// 🔑 Artık kilitlenen şey ifadenin <b>kendisi</b>: test eski biçimli (anahtarsız) bir satırı
/// <b>eliyle</b> üretir, tam bu metni koşturur ve iki şeyi birden iddia eder — anahtar geldi mi,
/// ve <b>açık tercih ezilmedi mi</b>. İkincisi ifadenin en ince yeri: <c>||</c> operatöründe
/// çakışan anahtarda <b>sağdaki operand kazanır</b>, bu yüzden mevcut değer sağda durmak
/// zorundadır. Ters yazılsaydı geri doldurma, "haber bildirimi istemiyorum" diyen herkesin
/// tercihini <b>sessizce açardı</b>.
/// </para>
///
/// <para>
/// ⚠️ İfade <c>ExecuteSqlRaw</c>'a <b>doğrudan verilemez</b>: EF gövdedeki <c>{</c>
/// karakterini yer tutucu sanıp <c>FormatException</c> fırlatır (12.15b'de aynı tuzağa iki
/// kez düşüldü). Migration'da <c>migrationBuilder.Sql</c>, testte ham bir
/// <c>DbCommand</c> kullanılır — ikisi de metni <b>olduğu gibi</b> gönderir.
/// </para>
/// </summary>
public static class NotificationPreferenceBackfill
{
    /// <summary>
    /// Eksik <c>News</c> anahtarını varsayılanıyla (<c>true</c>) tamamlar.
    /// </summary>
    /// <remarks>
    /// 🔬 <b>Açık tercihi İKİ mekanizma birden koruyor ve bu ölçüldü (Faz 0 — T2):</b>
    /// <list type="number">
    ///   <item><c>WHERE NOT (… ? 'News')</c> — anahtarı olan satıra <b>hiç dokunulmaz</b>
    ///         (aynı zamanda idempotentliğin kaynağı),</item>
    ///   <item><c>||</c> operand sırası — çakışan anahtarda <b>sağdaki</b> kazanır, yani
    ///         mevcut değer sağda durduğu için ezilmez.</item>
    /// </list>
    ///
    /// <para>
    /// 🐛 <b>Bozma turunda ölçüldü:</b> ikisinden <b>yalnız birini</b> bozmak testi
    /// <b>yeşil bırakıyor</b> — çünkü davranış değişmiyor. Bu bir test zaafı <i>değil</i>,
    /// <b>derinlemesine savunma</b>: ikisi birden bozulduğunda test kırmızıya dönüyor
    /// (ölçüldü). Yani testin kilitlediği şey <i>"şu SQL şöyle yazılmış"</i> değil,
    /// <b>"açık tercih geri doldurmadan sağ çıkar"</b> davranışıdır — doğru iddia da budur.
    /// ⚠️ Buradan çıkan kural: birini kaldırmayı düşünen biri, kalanın <b>tek başına</b>
    /// yeterli olduğunu bilmeli — bugün öyle, ama o zaman koruma <b>tek ayaklı</b> kalır.
    /// </para>
    /// </remarks>
    public const string Statement =
        """
        UPDATE users
        SET notification_preferences = '{"News": true}'::jsonb || notification_preferences
        WHERE NOT (notification_preferences ? 'News');
        """;
}
