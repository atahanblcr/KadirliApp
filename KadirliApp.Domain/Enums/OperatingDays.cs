using System.Globalization;

namespace KadirliApp.Domain.Enums;

/// <summary>
/// Faz 12.5 — bir seferin <b>hangi günler çalıştığı</b>: 7 bitlik maske,
/// Pazartesi=1 · Salı=2 · Çarşamba=4 · Perşembe=8 · Cuma=16 · Cumartesi=32 · Pazar=64.
/// </summary>
/// <remarks>
/// 🔴 <b>Gün dönüşümünün tek sahibi burasıdır.</b> .NET <see cref="DayOfWeek"/> <b>Pazar=0</b>'dan
/// başlar, bu maske <b>Pazartesi</b>'den — klasik bir sessiz kayma hatası. İki yerde ayrı
/// yazılsaydı "Salı seferi Pazartesi görünür" ve <b>kimse hata almazdı</b> (görünmez sözleşme
/// #23'ün sınıfı). Bu yüzden hem panel, hem DTO, hem de 12.6'daki "sıradaki sefer" hesabı
/// aynı metotlardan geçer.
///
/// 🔴 <b><see cref="Mask"/> = 0 yasaktır</b> (<see cref="IsValid"/>): hiçbir gün çalışmayan bir
/// sefer, panelde <i>duran</i> ama mobilde <i>hiç görünmeyen</i> bir kayıttır — yönetici saati
/// girdiğini sanır, vatandaş hiçbir zaman göremez. Doğrulama komutta yapılır.
///
/// ⚠️ Kodlar (<c>"mon"</c>…<c>"sun"</c>) DTO'ya çıkar, yani <b>kontrattır</b>: değişirse
/// mağazadaki eski sürümler günü tanımaz. Türkçe karşılıkları panelde/mobilde üretilir.
/// </remarks>
public readonly record struct OperatingDays
{
    public const int Monday = 1;
    public const int Tuesday = 2;
    public const int Wednesday = 4;
    public const int Thursday = 8;
    public const int Friday = 16;
    public const int Saturday = 32;
    public const int Sunday = 64;

    /// <summary>Her gün — 12.5 öncesinden kalan bütün seferlerin değeri (davranış değişmedi).</summary>
    public const int Daily = 127;

    /// <summary>Pazartesi–Cuma.</summary>
    public const int Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday;

    /// <summary>Cumartesi–Pazar.</summary>
    public const int Weekend = Saturday | Sunday;

    /// <summary>Maskedeki bit sırası — <b>Pazartesi'den başlar</b>, DTO kodları da bu sıradadır.</summary>
    private static readonly (int Bit, string Code, DayOfWeek Day)[] Days =
    {
        (Monday, "mon", DayOfWeek.Monday),
        (Tuesday, "tue", DayOfWeek.Tuesday),
        (Wednesday, "wed", DayOfWeek.Wednesday),
        (Thursday, "thu", DayOfWeek.Thursday),
        (Friday, "fri", DayOfWeek.Friday),
        (Saturday, "sat", DayOfWeek.Saturday),
        (Sunday, "sun", DayOfWeek.Sunday)
    };

    public int Mask { get; }

    public OperatingDays(int mask) => Mask = mask;

    public static OperatingDays All => new(Daily);

    /// <summary>1–127 aralığı. 0 "hiçbir gün" demektir ve <b>geçersizdir</b>.</summary>
    public bool IsValid => Mask is > 0 and <= Daily;

    public bool RunsDaily => Mask == Daily;
    public bool RunsWeekdaysOnly => Mask == Weekdays;
    public bool RunsWeekendOnly => Mask == Weekend;

    /// <summary>🔴 Pazar=0 kayması tam olarak burada çözülür — başka hiçbir yerde tekrarlanmaz.</summary>
    public static int BitFor(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Monday,
        DayOfWeek.Tuesday => Tuesday,
        DayOfWeek.Wednesday => Wednesday,
        DayOfWeek.Thursday => Thursday,
        DayOfWeek.Friday => Friday,
        DayOfWeek.Saturday => Saturday,
        _ => Sunday
    };

    public bool Runs(DayOfWeek day) => (Mask & BitFor(day)) != 0;

    /// <summary>DTO'ya çıkan kodlar, <b>Pazartesi'den Pazar'a</b> sıralı.</summary>
    public IReadOnlyList<string> Codes()
    {
        var codes = new List<string>(7);
        foreach (var (bit, code, _) in Days)
            if ((Mask & bit) != 0)
                codes.Add(code);
        return codes;
    }

    /// <summary>
    /// Kodlardan maske üretir; tanınmayan kod <b>yok sayılır</b> (bilinmeyen değer listeyi
    /// patlatmamalı). Hiçbiri tanınmazsa maske 0 döner — çağıran <see cref="IsValid"/> ile
    /// reddeder.
    /// </summary>
    public static OperatingDays FromCodes(IEnumerable<string>? codes)
    {
        var mask = 0;
        if (codes is null) return new OperatingDays(0);

        foreach (var raw in codes)
        {
            var code = raw?.Trim().ToLowerInvariant();
            foreach (var (bit, known, _) in Days)
                if (string.Equals(code, known, StringComparison.Ordinal))
                    mask |= bit;
        }

        return new OperatingDays(mask);
    }

    /// <summary>Panel formundaki 7 onay kutusunun ürettiği bit listesinden maske.</summary>
    public static OperatingDays FromBits(IEnumerable<int>? bits)
    {
        var mask = 0;
        if (bits is null) return new OperatingDays(0);

        foreach (var bit in bits)
            if (Days.Any(d => d.Bit == bit))
                mask |= bit;

        return new OperatingDays(mask);
    }

    /// <summary>
    /// <paramref name="from"/> gününden başlayarak (o gün <b>dâhil</b>) seferin çalıştığı ilk
    /// güne kaç gün olduğu; hiç çalışmıyorsa <c>null</c>. 12.6'nın "sıradaki sefer"i buna dayanır.
    /// </summary>
    public int? DaysUntilNext(DayOfWeek from)
    {
        if (Mask == 0) return null;

        for (var offset = 0; offset < 7; offset++)
        {
            var day = (DayOfWeek)(((int)from + offset) % 7);
            if (Runs(day)) return offset;
        }

        return null;
    }

    /// <summary>Maskedeki günlerin <see cref="DayOfWeek"/> karşılıkları (Pazartesi'den sıralı).</summary>
    public IReadOnlyList<DayOfWeek> ToDayOfWeeks()
    {
        // ⚠️ `this` bir struct olduğu için lambda içinde kullanılamaz (CS1673) — yerele kopyala.
        var mask = Mask;
        return Days.Where(d => (mask & d.Bit) != 0).Select(d => d.Day).ToList();
    }

    public override string ToString() => Mask.ToString(CultureInfo.InvariantCulture);
}
