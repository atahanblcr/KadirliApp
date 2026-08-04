using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Common.Security;

/// <summary>
/// Faz 11.18 — panel giriş kilidinin tek sahibi.
///
/// 🔑 **Neden hız sınırı yetmiyordu:** panelde 9.2'den beri IP başına bir hız sınırı var
/// (<c>panel-login</c>, 5 deneme/dakika) — ama o **IP**'yi kısıtlar, **hesabı** değil.
/// Farklı IP'lerden (ya da mobil şebekede dolaşan tek bir saldırgandan) gelen denemeler
/// aynı hesaba sınırsızca çarpabiliyordu ve hesap sahibi bunu **hiçbir yerde göremiyordu**.
/// Kilit hesabın kendisinde durur, IP'den bağımsızdır.
///
/// ⚠️ Kilitliyken **doğru parola da reddedilir** — aksi hâlde kilit yalnızca yanlış
/// tahminleri yavaşlatır, doğru tahmini hiç engellemez ve kilidin bir anlamı kalmaz.
/// </summary>
public static class PanelLockoutPolicy
{
    /// <summary>Kilitten önceki hatalı deneme hakkı.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Kilit süresi. Süre dolunca sayaç sıfırlanır (yönetici müdahalesi gerekmez).</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Hesap şu an kilitli mi?</summary>
    public static bool IsLockedOut(User user, DateTime utcNow) =>
        user.LockedOutUntil.HasValue && user.LockedOutUntil.Value > utcNow;

    /// <summary>Kilidin bitmesine kalan süre (dakika, yukarı yuvarlanır; en az 1).</summary>
    public static int RemainingMinutes(User user, DateTime utcNow)
    {
        if (!IsLockedOut(user, utcNow)) return 0;
        return Math.Max(1, (int)Math.Ceiling((user.LockedOutUntil!.Value - utcNow).TotalMinutes));
    }

    /// <summary>
    /// Hatalı denemeyi işler: sayacı artırır, hak dolduysa kilidi kurar.
    /// Kilit süresi geçmişse sayaç baştan başlar.
    /// </summary>
    public static void RegisterFailure(User user, DateTime utcNow)
    {
        if (user.LockedOutUntil.HasValue && user.LockedOutUntil.Value <= utcNow)
        {
            // Önceki kilit dolmuş; yeni bir tur başlıyor.
            user.FailedLoginAttempts = 0;
            user.LockedOutUntil = null;
        }

        user.FailedLoginAttempts++;

        if (user.FailedLoginAttempts >= MaxFailedAttempts)
            user.LockedOutUntil = utcNow.Add(LockoutDuration);
    }

    /// <summary>Başarılı girişte sayaç ve kilit temizlenir.</summary>
    public static void RegisterSuccess(User user)
    {
        user.FailedLoginAttempts = 0;
        user.LockedOutUntil = null;
    }
}
