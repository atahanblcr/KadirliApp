using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Auth;

/// <summary>
/// Faz 12.7 — bir sosyal kimliği bir hesaba bağlamanın <b>TEK SAHİBİ</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 İki çağıranı var ve bu yüzden tek sahiplik şart: <c>RegisterCommand</c> (kayıt biterken
/// bağlar) ve <c>LinkSocialIdentityCommand</c> (kullanıcı ayarlardan bağlar). İkisi ayrı
/// yazılsaydı bu projenin en sık tekrarlayan hasar sınıfı doğardı (§7 madde 38/43/55): biri
/// "başka hesaba bağlı mı?" kontrolünü yapar, diğeri unutur ve <b>iki hesap tek Google
/// hesabına bağlanır</b> — benzersiz indeks yüzünden ikincisi 500 alır ve kullanıcı sebebini
/// hiçbir yerde göremez.
/// </para>
/// <para>
/// ⚠️ Kimlik <b>yalnız</b> <see cref="SocialIdentityPayload"/> olarak, yani doğrulanmış
/// jetondan gelebilir. İstemciden gelen ham <c>sub</c> hiçbir yoldan buraya ulaşamaz.
/// </para>
/// </remarks>
public static class SocialIdentityLinker
{
    /// <summary>
    /// Kimliği hesaba bağlar. <b>Idempotenttir</b>: zaten aynı hesaba bağlıysa bilgileri
    /// tazeler ve başarıyla döner.
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>SaveChanges</c> <b>ÇAĞIRMAZ</b> — çağıranın işlemine katılır. Kayıt akışında
    /// kullanıcı ile kimlik <b>aynı</b> <c>SaveChanges</c>'te yazılmak zorunda: ayrı
    /// yazılsalardı araya düşen bir hata "hesabı var ama sosyal bağlantısı yok" bırakırdı
    /// ve kullanıcı bir daha o düğmeyle giremezdi (§7 madde 66'nın "çağıranın işlemine
    /// katıl" kuralıyla aynı aile).
    /// </remarks>
    /// <exception cref="ConflictException">Kimlik başka bir hesaba bağlı.</exception>
    public static async Task<UserIdentity> LinkAsync(
        IUnitOfWork uow, Guid userId, SocialIdentityPayload identity, CancellationToken ct)
    {
        if (await ExistingLinkAsync(uow, userId, identity, ct) is { } already)
            return already;

        var link = NewLink(identity);
        link.UserId = userId;

        await uow.Repository<UserIdentity>().AddAsync(link, ct);
        return link;
    }

    /// <summary>
    /// Kayıt akışı için: kimliği <b>henüz veritabanına yazılmamış</b> bir kullanıcıya bağlar.
    /// </summary>
    /// <remarks>
    /// 🐛 <b>Bu ayrı metot 12.2b'nin canlı hatasının tekrarını önlemek için var</b>
    /// (<c>CODE_REVIEW_CHECKLIST</c> §4): <c>users.id</c> <c>gen_random_uuid()</c> varsayılanıyla
    /// tanımlı, yani EF onu <b>store-generated</b> sayıyor ve değer ancak INSERT'ten sonra
    /// geri dönüyor — <c>AddAsync</c>'ten hemen sonra <c>user.Id</c> hâlâ
    /// <see cref="Guid.Empty"/>. FK skalerinden bağlansaydı kimlik satırı <b>var olmayan bir
    /// kullanıcıya</b> bağlanır ve kayıt FK ihlaliyle patlardı. Bağ bu yüzden
    /// <b>gezinme özelliğinden</b> kuruluyor: EF sırayı kendisi çözüyor ve ikisi
    /// <b>tek</b> <c>SaveChanges</c>'te yazılıyor.
    /// </remarks>
    public static async Task<UserIdentity> AttachToNewUserAsync(
        IUnitOfWork uow, User user, SocialIdentityPayload identity, CancellationToken ct)
    {
        // Kullanıcı yeni olduğu için "aynı sağlayıcıdan ikinci bağlantı" imkânsız;
        // ama "bu sosyal hesap başkasına bağlı mı?" sorusu burada da sorulmak ZORUNDA.
        await GuardIdentityIsFreeAsync(uow, expectedOwner: null, identity, ct);

        var link = NewLink(identity);
        user.Identities.Add(link);
        return link;
    }

    /// <summary>
    /// Bağlantıyı <b>fiziksel olarak</b> siler; yoksa <c>false</c> döner.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔑 Bağlama ile çözme <b>aynı sınıfta</b> duruyor ki simetrileri görünsün: iki
    /// çağıranı var (kullanıcının kendi ucu ve panelin yönetici aksiyonu) ve ikisi de
    /// buradan geçiyor. Ayrı yazılsalardı biri "aynı sağlayıcıdan tek bağlantı" varsayımını
    /// korur, diğeri unuturdu.
    /// </para>
    /// <para>
    /// ⚠️ Soft-delete <b>yok</b>: benzersiz indeks yüzünden duran bir satır aynı hesabın
    /// <b>yeniden bağlanmasını</b> engellerdi — "kaldırdım" diyen düğme, kullanıcıyı bir
    /// daha bağlanamaz hâle getirirdi ve sebebi hiçbir ekranda yazmazdı.
    /// </para>
    /// ⚠️ <c>SaveChanges</c> çağırmaz — çağıranın işlemine katılır.
    /// </remarks>
    public static async Task<bool> UnlinkAsync(
        IUnitOfWork uow, Guid userId, string provider, CancellationToken ct)
    {
        var repo = uow.Repository<UserIdentity>();
        var link = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == provider, ct);

        if (link is null) return false;

        repo.Remove(link);
        return true;
    }

    /// <summary>Var olan bağı (idempotent tazelenmiş hâliyle) döner; yoksa <c>null</c>.</summary>
    private static async Task<UserIdentity?> ExistingLinkAsync(
        IUnitOfWork uow, Guid userId, SocialIdentityPayload identity, CancellationToken ct)
    {
        var repo = uow.Repository<UserIdentity>();

        var sameIdentity = await GuardIdentityIsFreeAsync(uow, expectedOwner: userId, identity, ct);
        if (sameIdentity is not null)
        {
            Refresh(sameIdentity, identity);
            return sameIdentity;
        }

        // Aynı sağlayıcıdan zaten bir bağlantı var mı? (Ucun şekli bir tane olmasını
        // gerektiriyor — bkz. UserIdentityConfiguration'daki benzersiz indeks.)
        var sameProvider = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Provider == identity.Provider, ct);

        if (sameProvider is not null)
        {
            // Kullanıcı ikinci bir Google hesabı bağlamaya çalışıyor. Sessizce ezmek yerine
            // söylüyoruz: "bağladım" deyip başka bir hesabı bağlamış olmak, hiç bağlamamaktan
            // kötüdür (§7 madde 37'nin "panelin en sinsi yalanı" kuralının uç karşılığı).
            throw new ConflictException(
                "Bu sağlayıcıdan zaten bir hesap bağlı. Önce mevcut bağlantıyı kaldırın.");
        }

        return null;
    }

    /// <summary>
    /// 🔴 Bu sosyal hesap <b>başka</b> bir kullanıcıya bağlı mı? Bağlıysa fırlatır.
    /// Sessizce taşımak, sosyal hesabına erişimi olan birinin <b>dilediği KadirliApp
    /// hesabına</b> geçebilmesi demek olurdu.
    /// </summary>
    private static async Task<UserIdentity?> GuardIdentityIsFreeAsync(
        IUnitOfWork uow, Guid? expectedOwner, SocialIdentityPayload identity, CancellationToken ct)
    {
        var sameIdentity = await uow.Repository<UserIdentity>().Query(tracking: true)
            .FirstOrDefaultAsync(
                x => x.Provider == identity.Provider && x.ProviderUserId == identity.ProviderUserId, ct);

        if (sameIdentity is not null && sameIdentity.UserId != expectedOwner)
            throw new ConflictException("Bu hesap başka bir kullanıcıya bağlı.");

        return sameIdentity;
    }

    private static UserIdentity NewLink(SocialIdentityPayload identity)
    {
        var link = new UserIdentity
        {
            Provider = identity.Provider,
            ProviderUserId = identity.ProviderUserId,
            LinkedAt = DateTime.UtcNow
        };
        Refresh(link, identity);
        return link;
    }

    private static void Refresh(UserIdentity link, SocialIdentityPayload identity)
    {
        link.Email = identity.Email;
        link.EmailVerified = identity.EmailVerified;
        link.DisplayName = identity.DisplayName;
        link.LastUsedAt = DateTime.UtcNow;
    }
}
