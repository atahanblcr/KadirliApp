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
        new("StaffAdmin",         "fa-user-shield",     "Personel",             "staff"),
        // Faz 11.17 — denetim izi. Module NULL: aynı gerekçeyle matrisin dışında (denetlenen
        // kişi denetim ekranını yönetmemeli) ve modül anahtarı verilseydi izin matrisinde
        // **karşılığı olmayan bir yetki** belirirdi — 11.15b'nin en büyük bulgusu buydu.
        new("AuditLogsAdmin",     "fa-clipboard-list",  "Denetim İzi",          null),
        // Faz 11.17 — çöp kutusu. Aynı gerekçe: geri getirme, moderatörün silme kararını
        // tersine çevirmektir; silme yetkisiyle aynı güven değildir.
        new("TrashAdmin",         "fa-trash-can",       "Çöp Kutusu",           null)
    };

    /// <summary>Yalnız admin/super_admin'in görebileceği satırlar.</summary>
    public static readonly IReadOnlySet<string> AdminOnlyControllers =
        new HashSet<string>(StringComparer.Ordinal) { "StaffAdmin", "AuditLogsAdmin", "TrashAdmin" };
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
