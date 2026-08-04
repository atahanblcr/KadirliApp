using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Search.Dtos;
using KadirliApp.Domain.Common;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Search.Queries;

/// <summary>
/// Faz 11.16b — panelin **global araması** (11.18'den kalan madde).
/// </summary>
/// <param name="Term">Aranan metin. Boş/çok kısaysa hiçbir sorgu koşmaz.</param>
/// <param name="AllowedModules">
/// 🔑 <b>Aranacak modüller ÇAĞIRAN tarafından verilir</b> — sorgu "bu kullanıcı kim?"
/// sorusunu sormaz, yalnız verilen kümede arar. Sebep: yetki bilgisi panel katmanının
/// (rol + <c>AdminPermission</c>) işi; sorgunun içine gömülseydi aynı mantık iki yerde
/// (menü sağlayıcı ve arama) yaşar ve ayrışabilirdi — görünmez sözleşme #20'nin
/// uyardığı ayrışmanın tam kendisi.
/// ⚠️ Boş küme verilirse sonuç da boştur: "izin yoksa sonuç yok" varsayılan davranış.
/// </param>
public record GlobalSearchQuery(string? Term, IReadOnlySet<string> AllowedModules)
    : IRequest<GlobalSearchResult>;

public class GlobalSearchQueryHandler : IRequestHandler<GlobalSearchQuery, GlobalSearchResult>
{
    /// <summary>
    /// Tek harfle arama neredeyse tüm tabloyu döndürür ve dokuz sorgu birden koşar;
    /// asgari uzunluk hem anlamsız sonucu hem gereksiz yükü engelliyor.
    /// </summary>
    public const int MinTermLength = 2;

    /// <summary>Modül başına gösterilen azami sonuç — kalanı için modülün kendi listesi var.</summary>
    public const int PerModuleLimit = 5;

    private readonly IUnitOfWork _uow;

    public GlobalSearchQueryHandler(IUnitOfWork uow) => _uow = uow;

    /// <summary>
    /// Aranabilen modüller. ⚠️ Anahtarlar <c>PanelMenu.Items</c> ve izin matrisiyle
    /// **birebir aynı** olmak zorunda; ayrışırsa yetkili olduğu bir modülde arama yapan
    /// yönetici hiç sonuç alamaz ve sebebini hiçbir yerde göremez (görünmez sözleşme #20).
    /// Testle kilitli.
    /// </summary>
    public static readonly IReadOnlySet<string> SearchableModules =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "ads", "announcements", "events", "campaigns", "deaths",
            "places", "guide", "businesses", "users"
        };

    public async Task<GlobalSearchResult> Handle(GlobalSearchQuery request, CancellationToken ct)
    {
        var term = request.Term?.Trim() ?? string.Empty;

        if (term.Length < MinTermLength)
            return new GlobalSearchResult(term, Array.Empty<GlobalSearchGroup>());

        var groups = new List<GlobalSearchGroup>();

        // Büyük/küçük harf duyarsızlık: projenin her yerinde kullanılan desen
        // (`GetPlacesQueryHandler`, `GetGuideItemsQuery`, `GetAdsQueryHandler`).
        // ⚠️ Npgsql'e özel `EF.Functions.ILike` BURADA KULLANILAMAZ: o uzantı
        // veritabanı sağlayıcısıyla gelir ve `Application` katmanı sağlayıcıyı tanımaz
        // (katman kuralı — `Domain ← Application ← Infrastructure`). Tek yerde farklı
        // bir arama semantiği kullanmak, modül listesiyle global aramanın aynı terimde
        // farklı sonuç vermesi demek olurdu.
        // 📌 Bilinen sınır: Türkçe `İ` (U+0130) `ToLower()` ile beklendiği gibi
        // küçülmeyebilir (görünmez sözleşme #21'in aynı kökü) — bu davranış projenin
        // tamamında aynı, burada bilinçli olarak AYRIŞTIRILMADI.
        var needle = term.ToLower();

        if (Allowed(request, "ads"))
            groups.Add(await SearchAsync<Ad>(
                "ads", needle, ct,
                match: (q, p) => q.Where(x => x.Title.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("ads", x.Id, x.Title, null, x.Status, x.CreatedAt)));

        if (Allowed(request, "announcements"))
            groups.Add(await SearchAsync<Announcement>(
                "announcements", needle, ct,
                match: (q, p) => q.Where(x => x.Title.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("announcements", x.Id, x.Title, null, x.Status, x.CreatedAt)));

        if (Allowed(request, "events"))
            groups.Add(await SearchAsync<Event>(
                "events", needle, ct,
                match: (q, p) => q.Where(x => x.Title.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("events", x.Id, x.Title, null, x.Status, x.CreatedAt)));

        if (Allowed(request, "campaigns"))
            groups.Add(await SearchAsync<Campaign>(
                "campaigns", needle, ct,
                match: (q, p) => q.Where(x => x.Title.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("campaigns", x.Id, x.Title, null, x.Status, x.CreatedAt)));

        if (Allowed(request, "deaths"))
            groups.Add(await SearchAsync<DeathNotice>(
                "deaths", needle, ct,
                match: (q, p) => q.Where(x => x.DeceasedName.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("deaths", x.Id, x.DeceasedName, null, x.Status, x.CreatedAt)));

        if (Allowed(request, "places"))
            groups.Add(await SearchAsync<Place>(
                "places", needle, ct,
                match: (q, p) => q.Where(x => x.Name.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("places", x.Id, x.Name, x.Address, null, x.CreatedAt)));

        if (Allowed(request, "guide"))
            groups.Add(await SearchAsync<GuideItem>(
                "guide", needle, ct,
                match: (q, p) => q.Where(x => x.Name.ToLower().Contains(p) ||
                                              (x.Phone != null && x.Phone.ToLower().Contains(p))),
                project: x => new GlobalSearchHit("guide", x.Id, x.Name, x.Phone, null, x.CreatedAt)));

        if (Allowed(request, "businesses"))
            groups.Add(await SearchAsync<Business>(
                "businesses", needle, ct,
                match: (q, p) => q.Where(x => x.BusinessName.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("businesses", x.Id, x.BusinessName, x.Phone, null, x.CreatedAt)));

        if (Allowed(request, "users"))
            groups.Add(await SearchAsync<User>(
                "users", needle, ct,
                // Yöneticinin elinde genelde ya kullanıcı adı ya telefon olur; ikisi de aransın.
                match: (q, p) => q.Where(x => (x.Username != null && x.Username.ToLower().Contains(p)) ||
                                              x.Phone.ToLower().Contains(p)),
                project: x => new GlobalSearchHit("users", x.Id, x.Username ?? x.Phone, x.Phone, null, x.CreatedAt)));

        return new GlobalSearchResult(term, groups.Where(g => g.TotalCount > 0).ToList());
    }

    private static bool Allowed(GlobalSearchQuery request, string module) =>
        request.AllowedModules.Contains(module);

    /// <summary>
    /// Bir modülü arar: toplam sayı + ilk <see cref="PerModuleLimit"/> sonuç.
    /// </summary>
    /// <remarks>
    /// ⚠️ Soft-delete edilmiş kayıtlar <b>bilinçli olarak görünmez</b>: global query
    /// filtresi onları zaten eliyor ve <c>IgnoreQueryFilters()</c> ÇAĞRILMIYOR. Silinen
    /// kaydı arayan yönetici için doğru yer **Çöp Kutusu** (Faz 11.17); arama sonuçlarına
    /// karışsalardı "silmiştim ama hâlâ çıkıyor" karmaşası doğardı.
    /// </remarks>
    private async Task<GlobalSearchGroup> SearchAsync<TEntity>(
        string module,
        string needle,
        CancellationToken ct,
        Func<IQueryable<TEntity>, string, IQueryable<TEntity>> match,
        System.Linq.Expressions.Expression<Func<TEntity, GlobalSearchHit>> project)
        where TEntity : BaseEntity
    {
        var query = match(_uow.Repository<TEntity>().Query(), needle);

        var total = await query.CountAsync(ct);
        if (total == 0) return new GlobalSearchGroup(module, 0, Array.Empty<GlobalSearchHit>());

        var hits = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id) // görünmez sözleşme #30: kararsız sıra sayfalı olmasa da yanıltır
            .Take(PerModuleLimit)
            .Select(project)
            .ToListAsync(ct);

        return new GlobalSearchGroup(module, total, hits);
    }

}
