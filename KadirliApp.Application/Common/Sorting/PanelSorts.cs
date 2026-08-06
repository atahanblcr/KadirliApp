using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Common.Sorting;

/// <summary>
/// Faz 11.18 — panel listelerinin sıralama tanımları, **tek dosyada**.
///
/// Neden burada toplandı: her handler kendi <c>switch</c>'ini yazsaydı anahtar adları
/// modülden modüle ayrışırdı (<c>newest</c> mi <c>date_desc</c> mi) ve panelin sütun
/// başlıkları hangi anahtarı üreteceğini bilemezdi. Başlık partial'ı bu adları
/// doğrudan kullanır; iki taraf aynı sözlükten okur.
///
/// 🔑 <b>Her tanım <c>ThenBy(Id)</c> ile biter.</b> Eşit değerli satırlarda kesin bir
/// ayraç yoksa Postgres satır sırasını garanti etmez ve sayfalı listede aynı kayıt iki
/// sayfada birden görünüp bir başkası hiç görünmeyebilir — hata vermeyen, sessiz veri kaybı.
/// ⚠️ "Bir ikincil anahtar koymak" YETMİYOR: ilk yazımda <c>title_asc</c> ikincil olarak
/// <c>CreatedAt</c> kullanıyordu, ama başlığı VE tarihi aynı olan iki kayıtta o da eşitti.
/// Ayraç, benzersiz olduğu bilinen bir kolon olmalı — testle yakalandı
/// (<c>PanelSortingTests.EveryKey_ProducesStableOrderForTiedRows</c>).
///
/// ⚠️ Varsayılan anahtarlar (<c>Default…</c>) modüllerin 11.18 ÖNCESİNDEKİ sırasının
/// birebir aynısıdır. Değiştirmek mobil listeyi sessizce ters çevirir (checklist §1).
/// </summary>
public static class PanelSorts
{
    // ————————————————————————————————————————————————————————————————
    // Duyurular — 11.18 öncesi: OrderByDescending(CreatedAt)
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<Announcement> Announcements = new(
        defaultKey: "created_desc",
        entries: new (string, Func<IQueryable<Announcement>, IOrderedQueryable<Announcement>>)[]
        {
            ("created_desc", q => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)),
            ("created_asc",  q => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("title_asc",    q => q.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("title_desc",   q => q.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
        });

    // ————————————————————————————————————————————————————————————————
    // Kampanyalar — 11.18 öncesi: OrderByDescending(CreatedAt)
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<Campaign> Campaigns = new(
        defaultKey: "created_desc",
        entries: new (string, Func<IQueryable<Campaign>, IOrderedQueryable<Campaign>>)[]
        {
            ("created_desc", q => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)),
            ("created_asc",  q => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("title_asc",    q => q.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("title_desc",   q => q.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("end_asc",      q => q.OrderBy(x => x.EndDate).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("end_desc",     q => q.OrderByDescending(x => x.EndDate).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
        });

    // ————————————————————————————————————————————————————————————————
    // Vefat — 11.18 öncesi: OrderByDescending(FuneralDate).ThenByDescending(CreatedAt)
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<DeathNotice> Deaths = new(
        defaultKey: "funeral_desc",
        entries: new (string, Func<IQueryable<DeathNotice>, IOrderedQueryable<DeathNotice>>)[]
        {
            ("funeral_desc", q => q.OrderByDescending(x => x.FuneralDate).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("funeral_asc",  q => q.OrderBy(x => x.FuneralDate).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("name_asc",     q => q.OrderBy(x => x.DeceasedName).ThenByDescending(x => x.FuneralDate).ThenBy(x => x.Id)),
            ("name_desc",    q => q.OrderByDescending(x => x.DeceasedName).ThenByDescending(x => x.FuneralDate).ThenBy(x => x.Id)),
        });

    // ————————————————————————————————————————————————————————————————
    // Etkinlikler — 11.18 öncesi: Sort=="date_asc" ? artan : azalan (EventDate, EventTime)
    // ⚠️ Bu modülde bilinmeyen anahtarın varsayılana düşmesi 11.10'dan beri DTO'da
    //    yazılı bir sözleşme; SortMap'in varsayılan davranışı zaten bu.
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<Event> Events = new(
        defaultKey: "date_desc",
        entries: new (string, Func<IQueryable<Event>, IOrderedQueryable<Event>>)[]
        {
            ("date_desc",  q => q.OrderByDescending(x => x.EventDate).ThenByDescending(x => x.EventTime).ThenBy(x => x.Id)),
            ("date_asc",   q => q.OrderBy(x => x.EventDate).ThenBy(x => x.EventTime).ThenBy(x => x.Id)),
            ("title_asc",  q => q.OrderBy(x => x.Title).ThenByDescending(x => x.EventDate).ThenBy(x => x.Id)),
            ("title_desc", q => q.OrderByDescending(x => x.Title).ThenByDescending(x => x.EventDate).ThenBy(x => x.Id)),
        });

    // ————————————————————————————————————————————————————————————————
    // Hata kayıtları (Faz 12.1) — YENİ modül, "eski sıra" kısıtı yok.
    // Varsayılan "en son görülen önce": yönetici panele "şu an ne patlıyor?" diye bakar,
    // "en eski hata neydi" diye değil.
    // ⚠️ Sıralama LastSeenAt üzerinden — CreatedAt DEĞİL. Tekilleştirme yüzünden bir satır
    //    aylar önce açılıp bugün hâlâ artıyor olabilir; CreatedAt'e göre sıralasaydık
    //    şu anda patlayan hata listenin dibinde kalırdı.
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<ErrorLog> ErrorLogs = new(
        defaultKey: "last_seen_desc",
        entries: new (string, Func<IQueryable<ErrorLog>, IOrderedQueryable<ErrorLog>>)[]
        {
            ("last_seen_desc",  q => q.OrderByDescending(x => x.LastSeenAt).ThenBy(x => x.Id)),
            ("last_seen_asc",   q => q.OrderBy(x => x.LastSeenAt).ThenBy(x => x.Id)),
            ("first_seen_desc", q => q.OrderByDescending(x => x.FirstSeenAt).ThenBy(x => x.Id)),
            ("first_seen_asc",  q => q.OrderBy(x => x.FirstSeenAt).ThenBy(x => x.Id)),
            ("count_desc",      q => q.OrderByDescending(x => x.OccurrenceCount).ThenByDescending(x => x.LastSeenAt).ThenBy(x => x.Id)),
            ("count_asc",       q => q.OrderBy(x => x.OccurrenceCount).ThenByDescending(x => x.LastSeenAt).ThenBy(x => x.Id)),
        });

    // ————————————————————————————————————————————————————————————————
    // Giriş denemeleri (Faz 12.2) — YENİ modül, "eski sıra" kısıtı yok.
    // Varsayılan "en yeni önce": güvenlik ekranına "şu an ne oluyor" diye bakılır.
    // ⚠️ "suspicious_first" bir SÜZGEÇ DEĞİL sıralamadır: şüpheliler üste çıkar ama
    //    sıradan denemeler listede kalır. Süzgeç ayrı ("Yalnız şüpheli"); ikisini
    //    karıştırmak "listede 3 satır var" derken tabloda 4.000 satır olmasına yol açardı.
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<LoginAttempt> LoginAttempts = new(
        defaultKey: "created_desc",
        entries: new (string, Func<IQueryable<LoginAttempt>, IOrderedQueryable<LoginAttempt>>)[]
        {
            ("created_desc",     q => q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("created_asc",      q => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("suspicious_first", q => q.OrderByDescending(x => x.IsSuspicious).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("channel_asc",      q => q.OrderBy(x => x.Channel).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("channel_desc",     q => q.OrderByDescending(x => x.Channel).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
        });

    // ————————————————————————————————————————————————————————————————
    // Bildirim gönderimleri (Faz 12.2b) — YENİ modül, "eski sıra" kısıtı yok.
    // Varsayılan "en yeni önce": panoya "az önce yolladığım gitti mi" diye bakılır.
    // ⚠️ "failed_desc" bir SÜZGEÇ DEĞİL sıralamadır — en çok başarısız olan kampanya üste
    //    çıkar ama sorunsuz gönderimler listede kalır (LoginAttempts'teki aynı ayrım).
    // ⚠️ Sayaçlara göre sıralama, sayaçların KOLONDA tutulmasının somut getirisi: türetilmiş
    //    bir alan olsaydı bu anahtarlar ya yazılamaz ya da tam tarama yapardı.
    // ————————————————————————————————————————————————————————————————
    public static readonly SortMap<PushCampaign> PushCampaigns = new(
        defaultKey: "created_desc",
        entries: new (string, Func<IQueryable<PushCampaign>, IOrderedQueryable<PushCampaign>>)[]
        {
            ("created_desc",   q => q.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("created_asc",    q => q.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("title_asc",      q => q.OrderBy(x => x.Title).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("title_desc",     q => q.OrderByDescending(x => x.Title).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("recipients_desc",q => q.OrderByDescending(x => x.RecipientCount).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("recipients_asc", q => q.OrderBy(x => x.RecipientCount).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("failed_desc",    q => q.OrderByDescending(x => x.FailedCount).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
            ("failed_asc",     q => q.OrderBy(x => x.FailedCount).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Id)),
        });
}
