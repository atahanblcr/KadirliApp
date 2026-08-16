using KadirliApp.Application.Features.Dashboard.Commands;
using KadirliApp.Application.Features.Dashboard.Queries;
using KadirliApp.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KadirliApp.Web.Controllers;

/// <summary>
/// Faz 11.15b: Dashboard, giriş sonrası **iniş sayfası**dır — moderatöre de açıktır.
/// Kapalı olsaydı moderatör panele girer girmez "Yetkiniz yok" ekranına düşer ve
/// izin verilen modüllerine hiç ulaşamazdı (çıkmaz sokak). Gösterdiği şey toplu sayaç;
/// modül içeriği değil.
/// </summary>
[Authorize(Roles = "admin,super_admin,moderator")]
public class DashboardController : Controller
{
    private readonly ISender _sender;

    /// <summary>
    /// 🔴 Faz 12.19a — <c>AppDbContext</c> buradan DÜŞTÜ. Katman olarak yasaldı
    /// (<c>Web → Infrastructure</c>, §1) ama MediatR'ı atlıyordu: sahte içerik basan tek
    /// aksiyonun denetim izi hiç düşmüyordu. Ortam bilgisi ise <b>iki</b> yerde gerekiyor —
    /// aksiyonun kapısında ve butonun çizilip çizilmeyeceğine karar veren
    /// <see cref="Index"/>'te.
    /// </summary>
    private readonly IWebHostEnvironment _env;

    public DashboardController(ISender sender, IWebHostEnvironment env)
    {
        _sender = sender;
        _env = env;
    }

    // Faz 9.4: inline COUNT sorguları yerine Application query'leri — Admin API ile aynı
    // handler'lar kullanılır ve sonuç Redis'te 60 sn cache'lenir (CachingBehavior).
    public async Task<IActionResult> Index()
    {
        var stats = await _sender.Send(new GetDashboardStatsQuery());
        var recent = await _sender.Send(new GetRecentActivitiesQuery(8));

        // Faz 12.1 — hata rozeti YALNIZ admin'e. Sayacı paylaşılan GetDashboardStatsQuery'ye
        // eklemedik bilerek: o sorgu 60 sn Redis'te cache'leniyor ve moderatöre de dönüyor;
        // eklenseydi moderatör göremeyeceği bir ekranın sayacını görürdü ("gizli buton"un tersi).
        var isAdmin = User.IsInRole("admin") || User.IsInRole("super_admin");

        int? openErrors = isAdmin
            ? await _sender.Send(new KadirliApp.Application.Features.ErrorLogs.Queries.GetOpenErrorCountQuery(24))
            : null;

        // Faz 12.2 — şüpheli giriş rozeti, hata rozetiyle aynı kural: yalnız admin.
        // 🔑 Uyarı e-postası 5 dakikada bir gidiyor ama kimse posta kutusuna 7/24 bakmıyor;
        // panele giren yönetici ise iniş sayfasını mutlaka görüyor. İki kanal aynı olayı
        // farklı zamanlarda yakalar ve bu tekrar bilinçli.
        int? suspiciousLogins = isAdmin
            ? await _sender.Send(new KadirliApp.Application.Features.LoginAttempts.Queries.GetSuspiciousLoginCountQuery(24))
            : null;

        // Faz 12.2b — son gönderimin teslim özeti, aynı rol kapısı.
        // 🔑 Gerekçe diğer iki rozetten biraz farklı: hata ve şüpheli giriş "bir şey oldu mu"
        // sorusunu cevaplıyor, bu satır "benim yaptığım şey işe yaradı mı" sorusunu. Duyuruyu
        // yayınlayan yönetici panele geri döndüğünde teslim sayısını görmeden çıkmamalı —
        // 12.2b'den önce o sayıyı görmenin tek yolu veritabanına girmekti.
        var lastCampaign = isAdmin
            ? await _sender.Send(new KadirliApp.Application.Features.PushCampaigns.Queries.GetLastPushCampaignQuery())
            : null;

        // Faz 12.13 — haber senkronu sağlığı. Rol kapısı DİĞERLERİNDEN FARKLI: bu satır
        // moderatöre de gösteriliyor, çünkü "Haberler" modülü ona açık olabilir ve boş bir
        // haber listesine bakan moderatörün sebebi görebilmesi gerekiyor. Kutu bir eyleme
        // değil bir DURUMA bakıyor; senkron panosunun bağlantısını taşımıyor.
        // 🔑 Bu bloğun 1 numaralı hasar sınıfının iniş sayfasındaki karşılığı: kaynak susarsa
        // uygulama eski haberi göstermeye devam eder ve BAŞKA HİÇBİR YERDE belirti olmaz.
        var newsSync = await _sender.Send(
            new KadirliApp.Application.Features.News.Queries.GetNewsSyncStatusQuery());

        var model = new DashboardViewModel
        {
            // 🔴 Faz 12.19a — buton yalnız geliştirmede çizilir. Rol kapısı (admin) ile
            // ortam kapısı BİRLİKTE değerlendirilir: ikisinden biri yetmez, çünkü aksiyonun
            // kendisi de ikisini birden istiyor ve panelde "tıklayınca hata veren buton"
            // bırakmıyoruz (§5).
            CanSeedMockData = _env.IsDevelopment() && isAdmin,
            NewsSync = newsSync,
            LastPushCampaign = lastCampaign,
            TotalUsers = stats.TotalUsers,
            ActiveAds = stats.ActiveAds,
            PendingApprovals = stats.PendingApprovals,
            TotalAnnouncements = stats.TotalAnnouncements,
            NewUsersLast7Days = stats.NewUsersLast7Days,
            TaxiCallsLast7Days = stats.TaxiCallsLast7Days,
            NewAdsLast7Days = stats.NewAdsLast7Days,
            TotalAnnouncementViews = stats.TotalAnnouncementViews,
            OpenErrorCount = openErrors,
            SuspiciousLoginCount = suspiciousLogins,
            // Faz 11.15c: kırılım artık ekrana çıkıyor; sıfır olan modül satırı çizilmez
            // ("0 bekliyor" satırı gürültü, tıklanınca boş liste açar).
            PendingQueue = new List<PendingQueueItem>
            {
                new("İlanlar",       "fa-bullhorn",     stats.PendingBreakdown.Ads,        "AdsAdmin",        "pending"),
                new("Vefat İlanları","fa-book-dead",    stats.PendingBreakdown.Deaths,     "DeathsAdmin",     "pending"),
                new("Etkinlikler",   "fa-calendar-alt", stats.PendingBreakdown.Events,     "EventsAdmin",     "pending"),
                new("Kampanyalar",   "fa-percent",      stats.PendingBreakdown.Campaigns,  "CampaignsAdmin",  "pending"),
                new("Şikayetler",    "fa-flag",         stats.PendingBreakdown.Complaints, "ComplaintsAdmin", "pending"),
            }.Where(x => x.Count > 0).ToList(),
            RecentActivities = recent.Select(a => new ActivityItem
            {
                Type = a.Type == "ad" ? "İlan" : "Duyuru",
                Icon = a.Type == "ad" ? "fa-bullhorn" : "fa-bell",
                Title = a.Title,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        return View(model);
    }

    /// <summary>
    /// ⚠️ Örnek veri basar — <b>yalnız geliştirme ortamında</b>, yalnız admin'e.
    /// İzin matrisinde karşılığı olmayan, veritabanına toplu yazan tek panel aksiyonu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Faz 12.19a — bu aksiyon üçüncü dış analiz denetiminin bir numaralı bulgusuydu
    /// ve üç ayrı deliği vardı:</b> ortam kapısı <b>hiç yazılmamıştı</b>, <c>[HttpGet]</c>
    /// olduğu için <c>AutoValidateAntiforgeryToken</c> onu <b>kapsamıyordu</b> (global filtre
    /// yalnız POST/PUT/DELETE doğrular) ve butonu düz bir <c>&lt;a href&gt;</c> idi.
    /// </para>
    /// <para>
    /// 🔑 <b>Bileşik hasar GET olmasından geliyordu:</b> bir yöneticinin ziyaret ettiği kötü
    /// niyetli sayfadaki tek bir <c>&lt;img src="…/Dashboard/Seed"&gt;</c>, <b>onun
    /// oturumuyla</b> canlıda boş kalan her tabloya sahte içerik yazdırırdı — sahte ilanlar,
    /// uydurma telefon numaraları, <b>sahte vefat ilanları</b>. Yönetici hiçbir şey tıklamamış
    /// olurdu. (<c>MockDataSeeder</c> tablo bazında idempotent olduğu için <i>dolu</i> bir
    /// tablo zarar görmezdi; risk yeni açılmış, henüz boş modüllerdi.)
    /// </para>
    /// <para>
    /// ⚠️ <b>404, 403 değil</b> — Production'da bu adres <i>var olmamalı</i>. 403, "burada
    /// bir şey var ama sana kapalı" der ve yolun varlığını doğrular; 404 hiçbir şey söylemez.
    /// </para>
    /// <para>
    /// 🔑 <b>Bu kapı İKİNCİ hattır, birincisi değil.</b> Asıl kapı boru hattındadır
    /// (<c>DevelopmentOnlyBehavior</c> + <c>IDevelopmentOnlyCommand</c>) ve kapsamını
    /// <b>tipten</b> türetir — buradaki kontrol yarın silinse bile komut yine reddedilir.
    /// Buradaki kontrolün işi güvenlik değil <b>dürüstlük</b>: adres Production'da hiç
    /// açılmasın, buton hiç çizilmesin.
    /// </para>
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Seed()
    {
        if (!_env.IsDevelopment()) return NotFound();

        try
        {
            var result = await _sender.Send(new SeedMockDataCommand());

            // 🔴 12.19a (plan dışı) — mesaj artık NE OLDUĞUNU söylüyor. Eskiden her koşuda
            // "Örnek veriler başarıyla eklendi." yazıyordu; dolu bir veritabanında seeder
            // hiçbir satır yazmadan aynı cümleyi kuruyordu ve yönetici farkı göremiyordu.
            TempData["Success"] = result.TotalRows == 0
                ? "Hiçbir tabloya dokunulmadı — örnek verinin gireceği tabloların hepsi zaten dolu."
                : $"{result.TotalRows} satır eklendi ({result.Tables.Count} tablo): " +
                  string.Join(", ", result.Tables.OrderByDescending(t => t.Value).Select(t => $"{t.Key} ({t.Value})"));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Örnek veri eklenirken hata oluştu: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
