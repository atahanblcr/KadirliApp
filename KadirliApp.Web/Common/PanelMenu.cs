using System.Security.Claims;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Web.Common;

/// <summary>
/// Faz 11.15b — **panel menüsünün tek doğruluk kaynağı.**
///
/// Menü daha önce <c>_Sidebar.cshtml</c> içinde 17 kez kopyalanmış bir <c>&lt;a&gt;</c>
/// bloğuydu. Tek listeye çekilmesinin iki somut getirisi var:
/// <list type="number">
///   <item>Yeni modül eklenince menüye satır atmak unutulmaz — mobildeki
///         <c>kAppModules</c> ile aynı fikir ("işlevsiz buton yok" kuralının panel eşi).</item>
///   <item>Moderatörün **göremeyeceği** modül menüde hiç çizilmez. Aksi hâlde moderatör
///         17 bağlantı görür, 3'ü çalışır, 14'ü "Yetkiniz yok" der.</item>
/// </list>
/// <c>Module</c> alanı <c>StaffAdminController.Modules</c> ve
/// <c>PanelPermissionAttribute</c> ile aynı anahtarı kullanır — üçü ayrışırsa
/// <c>PanelMenuTests</c> kırmızıya döner.
/// </summary>
public sealed record PanelMenuItem(string Controller, string Icon, string Label, string? Module)
{
    /// <summary>Modülü olmayan satır (Dashboard) izin matrisine tabi değildir.</summary>
    public bool RequiresPermission => Module is not null;
}

public static class PanelMenu
{
    /// <summary>Sıra ekrandaki sıradır.</summary>
    public static readonly IReadOnlyList<PanelMenuItem> Items = new List<PanelMenuItem>
    {
        new("Dashboard",          "fa-home",            "Dashboard",            null),
        new("AdsAdmin",           "fa-bullhorn",        "İlanlar",              "ads"),
        new("AnnouncementsAdmin", "fa-bell",            "Duyurular",            "announcements"),
        new("EventsAdmin",        "fa-calendar-alt",    "Etkinlikler",          "events"),
        new("CampaignsAdmin",     "fa-percent",         "Kampanyalar",          "campaigns"),
        new("BusinessesAdmin",    "fa-store",           "İşletmeler",           "businesses"),
        new("UsersAdmin",         "fa-users",           "Kullanıcılar",         "users"),
        new("DeathsAdmin",        "fa-book-dead",       "Vefat İlanları",       "deaths"),
        new("PharmaciesAdmin",    "fa-pills",           "Nöbetçi Eczaneler",    "pharmacies"),
        new("PowerOutagesAdmin",  "fa-bolt",            "Elektrik Kesintileri", "power-outages"),
        new("TransportAdmin",     "fa-bus",             "Ulaşım",               "transport"),
        new("TaxiAdmin",          "fa-taxi",            "Taksiciler",           "taxis"),
        new("PlacesAdmin",        "fa-map-marker-alt",  "Mekanlar",             "places"),
        new("GuideAdmin",         "fa-map-marked-alt",  "Şehir Rehberi",        "guide"),
        new("ComplaintsAdmin",    "fa-flag",            "Şikayetler",           "complaints"),
        new("LookupsAdmin",       "fa-tags",            "Tanımlar",             "lookups"),
        // Personel yönetimi bilinçli olarak matrisin DIŞINDA: izin veren rolü, izinleri
        // kendine yazabilecek biri yönetmemeli.
        //
        // 🐛 Faz 12.2 DÜZELTMESİ: bu satır 12.1'e kadar `Module = "staff"` taşıyordu ve
        // yorumu kendini yalanlıyordu — anahtar dolu olduğu için `StaffAdminController.Modules`
        // menüden türerken "staff"ı da alıyor, izin matrisinde bir SATIR olarak beliriyordu.
        // Yönetici moderatöre "Personel: okuma" veriyor, kutu işaretleniyor, kaydediliyor —
        // ama rol kapısı ([Authorize(Roles = "admin,super_admin")]) yüzünden o yetki ASLA
        // çalışmıyordu. 11.15b'nin kapattığı "karşılığı olmayan yetki" hatasının hâlâ ayakta
        // duran örneğiydi. Artık null; `PanelDisplay.NonMatrixModules` Türkçe karşılığını taşıyor.
        new("StaffAdmin",         "fa-user-shield",     "Personel",             null),
        // Faz 11.17 — denetim izi. Module NULL: aynı gerekçeyle matrisin dışında (denetlenen
        // kişi denetim ekranını yönetmemeli) ve modül anahtarı verilseydi izin matrisinde
        // **karşılığı olmayan bir yetki** belirirdi — 11.15b'nin en büyük bulgusu buydu.
        new("AuditLogsAdmin",     "fa-clipboard-list",  "Denetim İzi",          null),
        // Faz 11.17 — çöp kutusu. Aynı gerekçe: geri getirme, moderatörün silme kararını
        // tersine çevirmektir; silme yetkisiyle aynı güven değildir.
        new("TrashAdmin",         "fa-trash-can",       "Çöp Kutusu",           null),
        // Faz 12.1 — hata kayıtları. Module NULL, aynı gerekçe: kayıtlar yığın izi, istek
        // yolu ve kullanıcı kimliği taşıyor; moderatöre dağıtılabilir bir yetki değil.
        new("ErrorLogsAdmin",     "fa-triangle-exclamation", "Hata Kayıtları",  null),
        // Faz 12.2 — giriş denemeleri. Module NULL, aynı gerekçe: kayıtlar IP, tarayıcı
        // ve şüphe işareti taşıyor; "kim nereden girdi" moderatöre dağıtılabilir bir
        // yetki değil. Üstelik moderatörün KENDİ girişleri de bu listede.
        new("LoginAttemptsAdmin", "fa-fingerprint",     "Giriş Denemeleri",     null),
        // Faz 12.2b — bildirim gönderimleri (teslim panosu + elle gönderim). Module NULL,
        // gerekçe diğerlerinden biraz farklı ve daha ağır: bu ekran yalnız GÖSTERMİYOR,
        // tek tıkla ŞEHRİN TAMAMINA push atabiliyor. İzin matrisine girseydi "bildirimler:
        // güncelleme" yetkisi verilen bir moderatör bunu yapabilirdi — üstelik aksiyon adı
        // (`Send`) hiçbir moderasyon önekiyle eşleşmediği için sessizce `update`'e düşerek
        // (görünmez sözleşme #19).
        new("PushCampaignsAdmin", "fa-paper-plane",     "Bildirim Gönderimleri", null)
    };

    /// <summary>
    /// Yalnız admin/super_admin'in görebileceği satırlar.
    /// </summary>
    /// <remarks>
    /// 🔑 Faz 12.2'de yapısal bir kural eklendi: buradaki <b>her</b> controller'ın menü
    /// satırında <c>Module</c> <c>null</c> olmak ZORUNDA
    /// (<c>PanelModeratorPermissionTests.AdminOnlyControllers_AreOutsideThePermissionMatrix</c>).
    /// Dört ekran uyup beşincisinin uymaması tam olarak testle yakalanması gereken şeydi —
    /// ve 12.1'e kadar <c>StaffAdmin</c> uymuyordu.
    /// </remarks>
    public static readonly IReadOnlySet<string> AdminOnlyControllers =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "StaffAdmin", "AuditLogsAdmin", "TrashAdmin", "ErrorLogsAdmin", "LoginAttemptsAdmin",
            "PushCampaignsAdmin"
        };

    /// <summary>
    /// Faz 11.16b — <b>ekranı değil SONUCU süzen</b> controller'lar.
    /// </summary>
    /// <remarks>
    /// Panelin kuralı nettir: moderatöre açık her controller <c>[PanelPermission("&lt;modül&gt;")]</c>
    /// taşır (görünmez sözleşme #20, <c>PanelModeratorPermissionTests</c>). Ama global arama
    /// <b>tek bir modüle ait değil</b> — dokuz modülde birden arıyor, dolayısıyla takabileceği
    /// tek bir modül anahtarı yok.
    ///
    /// 🔑 Bu durumda yetki, ekranın kapısında değil <b>sorgunun içinde</b> uygulanır: kullanıcının
    /// okuma izni olan modüller <c>IPanelMenuProvider</c>'dan alınır ve arama yalnız o kümede koşar.
    /// Yani moderatör arama ekranını açabilir, ama göremeyeceği bir modülden **tek sonuç bile**
    /// dönmez.
    ///
    /// ⚠️ "Merak etmeyin, içeride süzüyor" bir belge cümlesi olarak çürür. Bu yüzden bu liste
    /// <b>bildirilmiş</b> olmak zorunda: yapısal test, <c>[PanelPermission]</c> taşımayan bir
    /// controller'ı ancak burada adı geçiyorsa hoş görür — ve buradaki her ad için ayrıca
    /// süzmenin gerçekten çalıştığını kanıtlayan bir davranış testi vardır. Böylece özniteliği
    /// unutan biri testi "listeye ekleyerek" susturamaz; süzmeyi de yazmak zorunda kalır.
    /// </remarks>
    public static readonly IReadOnlySet<string> PermissionFilteredControllers =
        new HashSet<string>(StringComparer.Ordinal) { "GlobalSearch" };
}

/// <summary>Görünümün "bu kullanıcı neyi görebilir?" sorusunu tek sorguyla cevaplar.</summary>
public interface IPanelMenuProvider
{
    Task<IReadOnlyList<PanelMenuItem>> VisibleItemsAsync(ClaimsPrincipal user);
}

public sealed class PanelMenuProvider : IPanelMenuProvider
{
    private readonly IUnitOfWork _uow;
    private IReadOnlyList<PanelMenuItem>? _cached; // istek başına (scoped) — layout birden çok kez sorabilir

    public PanelMenuProvider(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<PanelMenuItem>> VisibleItemsAsync(ClaimsPrincipal user)
    {
        if (_cached is not null) return _cached;

        var role = user.FindFirstValue(ClaimTypes.Role);

        if (role is "admin" or "super_admin")
            return _cached = PanelMenu.Items;

        if (role != "moderator" || !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return _cached = Array.Empty<PanelMenuItem>();

        // Tek sorgu — menü başına 17 ayrı izin sorgusu atmak sayfa açılışını yavaşlatırdı.
        var readable = await _uow.Repository<AdminPermission>().Query()
            .Where(p => p.UserId == userId && p.CanRead)
            .Select(p => p.Module)
            .ToListAsync();

        var readableSet = new HashSet<string>(readable, StringComparer.Ordinal);

        return _cached = PanelMenu.Items
            .Where(i => !PanelMenu.AdminOnlyControllers.Contains(i.Controller))
            .Where(i => !i.RequiresPermission || readableSet.Contains(i.Module!))
            .ToList();
    }
}
