using System.Linq;
using KadirliApp.Application.Common.Exceptions;

namespace KadirliApp.Application.Common.Security;

/// <summary>
/// Faz 11.18 — panel parola politikasının **TEK sahibi**.
///
/// Öncesinde kural üç ayrı yerde, üç ayrı kopya olarak yaşıyordu:
/// <c>ChangeMyPasswordCommandHandler</c>, <c>CreateStaffCommandHandler</c> ve
/// <c>ResetStaffPasswordCommandHandler</c> her biri elle <c>Length &lt; 6</c> yazıyordu —
/// yani politikayı sıkılaştırmak isteyen biri üç yeri birden bulmak zorundaydı ve
/// birini atlarsa **o kapıdan zayıf parola girmeye devam ederdi** (`SlugHelper` /
/// `PanelDisplay` ile aynı "tek sahip" dersi, ARCHITECTURE.md §7 madde 21).
///
/// ⚠️ <see cref="Infrastructure.Persistence.DbSeeder"/>'ın yazdığı varsayılan parola bu
/// politikayı **bilerek** karşılamaz: seed komut üzerinden geçmez, doğrudan DB'ye yazar.
/// Onun emniyeti politikanın kendisi değil <c>User.MustChangePassword</c> bayrağıdır —
/// varsayılan parolayla giren yönetici panelde hiçbir sayfayı açamadan parola
/// değiştirme ekranına düşer.
/// </summary>
public static class PanelPasswordPolicy
{
    /// <summary>Asgari uzunluk. 11.18 öncesi 6'ydı (11.15c'nin C grubunda "zayıf" olarak işaretlenmişti).</summary>
    public const int MinLength = 10;

    /// <summary>Kullanıcıya gösterilecek kural özeti — form yardım metni ve hata mesajı aynı yerden gelir.</summary>
    public const string Description =
        "Parola en az 10 karakter olmalı, en az bir harf ve en az bir rakam içermelidir.";

    /// <summary>
    /// Kuralı denetler; uyuyorsa <c>null</c>, uymuyorsa **Türkçe** hata metni döndürür.
    /// (Kullanıcı adı/telefon verilirse parolanın onlarla aynı olmadığı da denetlenir.)
    /// </summary>
    public static string? Validate(string? password, string? username = null, string? phone = null)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Parola zorunludur.";

        if (password.Length < MinLength)
            return $"Parola en az {MinLength} karakter olmalıdır.";

        if (!password.Any(char.IsLetter))
            return "Parola en az bir harf içermelidir.";

        if (!password.Any(char.IsDigit))
            return "Parola en az bir rakam içermelidir.";

        // Kullanıcı adının kendisi parola olamaz — kilidi olan kapıya anahtarı üstüne asmak gibi.
        if (!string.IsNullOrWhiteSpace(username) &&
            string.Equals(password, username, StringComparison.OrdinalIgnoreCase))
            return "Parola kullanıcı adınızla aynı olamaz.";

        if (!string.IsNullOrWhiteSpace(phone) &&
            string.Equals(password, phone, StringComparison.OrdinalIgnoreCase))
            return "Parola telefon numaranızla aynı olamaz.";

        return null;
    }

    /// <summary>
    /// Kural ihlalinde <see cref="AppException"/> (<c>VALIDATION_ERROR</c>) fırlatır.
    /// Komut handler'ları bunu çağırır — panel bu istisnayı yakalayıp forma basar.
    /// </summary>
    public static void Enforce(string? password, string? username = null, string? phone = null)
    {
        var error = Validate(password, username, phone);
        if (error is not null)
            throw new AppException(error, "VALIDATION_ERROR");
    }
}
