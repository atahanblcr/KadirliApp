using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

/// <summary>
/// Faz 12.16 — hukuki bir belgenin <b>kimliği</b> (KVKK aydınlatma metni, açık rıza metni…).
/// Metnin kendisi burada <b>değil</b>, <see cref="LegalDocumentVersion"/>'dadır.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Neden iki tablo?</b> Bu bloğun tamamı tek bir cümleden türüyor: <i>metin panelden
/// değiştirilebiliyorsa, rıza kaydı metnin HANGİ HÂLİNE verildiğini bilmek zorundadır.</i>
/// Metin belgenin üstünde saklansaydı yönetici onu düzelttiği anda <b>bütün geçmiş rızalar
/// retroaktif olarak</b> başka bir metni işaret ederdi: tablo dolu, kanıt yok, hata yok.
/// </para>
/// <para>
/// 🔴 <see cref="IsMandatory"/> ile isteğe bağlı rıza <b>AYRI BELGELERDİR</b>, tek belgenin
/// iki kutusu değil. KVKK'nın en sık ihlal edilen kuralı budur: "hizmet için gerekli işleme"
/// ile "ticari elektronik ileti"yi tek kutuda toplamak rızayı <b>geçersiz</b> kılar.
/// Zorunlu belge kaydı <b>bloklar</b>, isteğe bağlı olan <b>bloklamaz</b>.
/// </para>
/// <para>
/// ⚠️ Bu modülde <b>moderasyon YOK</b> ve <c>Approve*</c> adlı bir dosya
/// <b>yazılamaz</b> — <c>ModerationSingleOwnerTests</c> moderasyonlu modül kümesini o addan
/// türetiyor (12.12'de Haberler için konan aynı kural). Belgenin yayına çıkması bir
/// moderasyon kararı değil, <see cref="LegalDocumentVersion.Publish"/>'tir.
/// </para>
/// </remarks>
public class LegalDocument : BaseEntity
{
    /// <summary>
    /// <see cref="Enums.LegalDocumentTypes"/> — <b>unique</b>. Bir türden yalnız bir belge
    /// olabilir: iki "açık rıza metni" olsaydı kayıt ekranı hangisini soracağını bilemez,
    /// ikisini birden sorsa kullanıcı aynı şeyi iki kez onaylardı.
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>Kullanıcıya gösterilen ad ("KVKK Aydınlatma Metni").</summary>
    public string Title { get; set; } = default!;

    /// <summary>
    /// 🔴 Zorunlu mu? Zorunlu belgelerin <b>hepsi</b> onaylanmadan kayıt tamamlanmaz.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu bayrağı sonradan <b>açmak</b> geriye dönük değildir: zaten kayıtlı kullanıcıların
    /// o belge için rıza satırı yoktur ve bu tablo onları bulmaz. Karşılığı 12.17'nin
    /// <b>yeniden onay akışıdır</b> — bayrağı açan yönetici, sürümü de
    /// <see cref="LegalDocumentVersion.RequiresReconsent"/> ile yayınlamalıdır.
    /// </remarks>
    public bool IsMandatory { get; set; }

    /// <summary>
    /// Kayıt ekranında gösterilsin mi? <see cref="IsMandatory"/>'den <b>ayrı</b> bir sorudur:
    /// "gizlilik politikası" kayıt ekranında gösterilmeden de yayında olabilir (ayarlardan
    /// okunur), "ticari ileti izni" ise gösterilir ama zorunlu değildir.
    /// </summary>
    public bool ShowAtRegistration { get; set; }

    /// <summary>Kayıt ekranındaki sıra — hukuki metinlerde sıra anlamlıdır (önce aydınlatma, sonra rıza).</summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Pasif belge hiçbir uçta görünmez. ⚠️ <b>Silme yok</b>: belgeye bağlı sürümler ve
    /// onlara bağlı rıza kayıtları <b>kanıttır</b>; silinirse geçmişte yapılmış işlemenin
    /// hukuki dayanağı kaybolur (§7 madde 74).
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public List<LegalDocumentVersion> Versions { get; set; } = new();
}
