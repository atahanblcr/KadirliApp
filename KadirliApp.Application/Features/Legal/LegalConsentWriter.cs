using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal;

/// <summary>Rıza kaydının bağlamı — "nereden, hangi adresten, hangi uygulamayla".</summary>
/// <remarks>
/// ⚠️ <see cref="Source"/> <b>sunucuda sabitlenir</b>, istemciden gelmez (§7 madde 33'ün
/// <c>ErrorLog.Source</c> kararının aynısı).
/// </remarks>
public record ConsentContext(string Source, IPAddress? IpAddress, string? UserAgent);

/// <summary>
/// Faz 12.16 — bir rıza satırı yazmanın <b>TEK SAHİBİ</b>
/// (12.7'nin <c>SocialIdentityLinker</c> deseni).
/// </summary>
/// <remarks>
/// <para>
/// 🔑 İki çağıranı var: <c>RegisterCommand</c> (kayıt biterken) ve
/// <c>SaveMyConsentsCommand</c> (kullanıcı ayarlardan). Ayrı yazılsalardı biri "gönderilen
/// sürüm gerçekten yayında mı?" kontrolünü yapar, diğeri unuturdu — ve o an rıza,
/// kullanıcının göremediği (yürürlükten kalkmış ya da taslak) bir metne yazılırdı: uçlar
/// 200 döner, tablo dolar, <b>kanıt sessizce geçersiz olur</b>.
/// </para>
/// <para>
/// ⚠️ <c>SaveChanges</c> <b>ÇAĞIRMAZ</b> — çağıranın işlemine katılır (§7 madde 73).
/// Kayıt akışında rıza satırları kullanıcıyla <b>aynı</b> <c>SaveChanges</c>'te yazılmak
/// zorunda: ayrı yazılsalardı araya düşen bir hata <b>rızasız bir hesap</b> bırakırdı —
/// uygulama çalışır, uçlar 200 döner ve o hesabın verisini işlemenin dayanağı
/// <b>hiçbir yerde olmaz</b>.
/// </para>
/// </remarks>
public static class LegalConsentWriter
{
    /// <summary>
    /// Kayıt akışı: rızaları <b>henüz veritabanına yazılmamış</b> bir kullanıcıya bağlar.
    /// </summary>
    /// <remarks>
    /// 🐛 Bağ <b>gezinme özelliğinden</b> kurulur, FK skalerinden değil: <c>users.id</c>
    /// <c>gen_random_uuid()</c> varsayılanıyla <b>store-generated</b>, yani <c>AddAsync</c>'ten
    /// sonra <c>user.Id</c> hâlâ <see cref="Guid.Empty"/>'dir (12.2b'nin canlı hatası,
    /// 12.7'de bir kez daha karşılaşıldı). Skalerden bağlansaydı rıza satırı <b>var olmayan
    /// bir kullanıcıya</b> bağlanır ve kayıt FK ihlaliyle patlardı.
    /// </remarks>
    /// <returns>Zorunlu rızalar eksikse <c>IsValid=false</c> ve <b>hangi belgenin</b> eksik olduğu.</returns>
    public static async Task<ConsentValidationResult> AttachToNewUserAsync(
        IUnitOfWork uow,
        User user,
        IReadOnlyList<ConsentDecisionDto>? decisions,
        ConsentContext context,
        bool enforceMandatory,
        DateTime now,
        CancellationToken ct)
    {
        var live = await LiveVersionsAsync(uow, ct);

        var validation = Validate(live, decisions, enforceMandatory);
        if (!validation.IsValid) return validation;

        foreach (var (versionId, granted) in Accepted(live, decisions))
        {
            var consent = new UserConsent { DocumentVersionId = versionId };
            Decide(consent, granted, now, context);
            user.Consents.Add(consent);
        }

        return validation;
    }

    /// <summary>
    /// Ayarlar/yeniden onay akışı: var olan bir kullanıcının rızalarını günceller.
    /// </summary>
    /// <remarks>
    /// ⚠️ Zorunlu rızanın <b>geri alınması burada mümkün değildir</b> — karşılığı var olan
    /// <c>DELETE /v1/users/me</c>'dir (10.8). İkinci bir "hesabı kullanılamaz hâle getirme"
    /// yolu açılmadı: zorunlu rızayı geri alan ama hesabı duran bir kullanıcı, uygulamayı
    /// kullanmaya devam eder ve <b>dayanağı olmayan</b> bir işleme doğar.
    /// </remarks>
    public static async Task<ConsentValidationResult> ApplyAsync(
        IUnitOfWork uow,
        Guid userId,
        IReadOnlyList<ConsentDecisionDto>? decisions,
        ConsentContext context,
        DateTime now,
        CancellationToken ct)
    {
        var live = await LiveVersionsAsync(uow, ct);

        // Ayarlar ekranında zorunluluk denetlenmez (kullanıcı zaten kayıtlı); ama zorunlu
        // bir belgeyi "hayır"a çevirme denemesi sessizce yutulmaz.
        foreach (var decision in decisions ?? Array.Empty<ConsentDecisionDto>())
        {
            if (!live.TryGetValue(decision.VersionId, out var doc)) continue;
            if (doc.IsMandatory && !decision.Granted)
                return new ConsentValidationResult(false, doc.Title, new[] { decision.VersionId });
        }

        var repo = uow.Repository<UserConsent>();
        var accepted = Accepted(live, decisions).ToList();
        var versionIds = accepted.Select(x => x.VersionId).ToList();

        var existing = await repo.Query(tracking: true)
            .Where(c => c.UserId == userId && versionIds.Contains(c.DocumentVersionId))
            .ToListAsync(ct);

        foreach (var (versionId, granted) in accepted)
        {
            var consent = existing.FirstOrDefault(c => c.DocumentVersionId == versionId);
            if (consent is null)
            {
                consent = new UserConsent { UserId = userId, DocumentVersionId = versionId };
                Decide(consent, granted, now, context);
                await repo.AddAsync(consent, ct);
                continue;
            }

            Decide(consent, granted, now, context);
        }

        return new ConsentValidationResult(true, null, Array.Empty<Guid>());
    }

    /// <summary>
    /// 🔴 Zorunlu belgelerin <b>hepsi</b> onaylandı mı — ve gönderilen sürümler gerçekten
    /// yayında mı?
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Yayında olmayan sürüme rıza yazılmaz</b> (sessizce yok sayılır, bkz.
    /// <see cref="Accepted"/>). Yürürlükten kalkmış bir sürüme yazılsaydı kanıt, kullanıcının
    /// o an ekranda <b>göremeyeceği</b> bir metni gösterirdi.
    /// </para>
    /// <para>
    /// 🔑 Bunun görünür bedeli şudur ve bilinçlidir: kullanıcı formu doldururken yönetici
    /// yeni sürüm yayınlarsa kayıt <b>reddedilir</b> ve kullanıcı ekranı tazeleyip yeni metni
    /// onaylar. Alternatifi — eski sürümün onayını kabul etmek — yürürlükten kalkmış bir
    /// metne dayanan bir kayıt üretirdi. Yayınlama nadir bir iştir; sessizce yanlış kanıt
    /// üretmek geri alınamaz.
    /// </para>
    /// </remarks>
    private static ConsentValidationResult Validate(
        IReadOnlyDictionary<Guid, LegalDocument> live,
        IReadOnlyList<ConsentDecisionDto>? decisions,
        bool enforceMandatory)
    {
        if (!enforceMandatory)
            return new ConsentValidationResult(true, null, Array.Empty<Guid>());

        var granted = (decisions ?? Array.Empty<ConsentDecisionDto>())
            .Where(d => d.Granted)
            .Select(d => d.VersionId)
            .ToHashSet();

        // Sıra, kullanıcıya HANGİ belgenin eksik olduğunu söylerken anlamlı: kayıt
        // ekranındaki sırayla ilk eksiği bildiriyoruz.
        var missing = live
            .Where(kv => kv.Value.IsMandatory && !granted.Contains(kv.Key))
            .OrderBy(kv => kv.Value.SortOrder)
            .ToList();

        return missing.Count == 0
            ? new ConsentValidationResult(true, null, Array.Empty<Guid>())
            : new ConsentValidationResult(false, missing[0].Value.Title, missing.Select(m => m.Key).ToList());
    }

    /// <summary>Yalnız <b>yayındaki</b> sürümlere ait kararlar; mükerrer gönderimde sonuncusu geçerli.</summary>
    private static IEnumerable<(Guid VersionId, bool Granted)> Accepted(
        IReadOnlyDictionary<Guid, LegalDocument> live,
        IReadOnlyList<ConsentDecisionDto>? decisions) =>
        (decisions ?? Array.Empty<ConsentDecisionDto>())
            .Where(d => live.ContainsKey(d.VersionId))
            .GroupBy(d => d.VersionId)
            .Select(g => (g.Key, g.Last().Granted));

    private static void Decide(UserConsent consent, bool granted, DateTime now, ConsentContext context)
    {
        // ⚠️ Geri alma ile "hayır dedi" AYRI geçişlerdir: ilki `RevokedAt`'i doldurur, ikincisi
        // doldurmaz. Tek metotta toplansaydı "hiç onaylamamış" kullanıcı, defterde "rızasını
        // geri aldı" olarak görünürdü.
        if (granted) consent.Grant(now, context.Source);
        else if (consent.Granted) consent.Revoke(now);
        else consent.Deny(now, context.Source);

        consent.IpAddress = context.IpAddress;
        consent.UserAgent = Truncate(context.UserAgent, 500);
    }

    /// <summary>Yayındaki sürüm kimliği → belgesi. Tek sorgu; iki çağıran da aynı tanımı kullanır.</summary>
    private static async Task<IReadOnlyDictionary<Guid, LegalDocument>> LiveVersionsAsync(
        IUnitOfWork uow, CancellationToken ct)
    {
        var documents = await LegalConsentRules
            .Available(uow.Repository<LegalDocument>().Query().Include(d => d.Versions))
            .ToListAsync(ct);

        return documents
            .Select(d => (Version: LegalConsentRules.LiveVersionOf(d), Document: d))
            .Where(x => x.Version is not null)
            .ToDictionary(x => x.Version!.Id, x => x.Document);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
