using System.Text;

namespace KadirliApp.Application.Common.Utils;

/// <summary>Türkçe karakter destekli slug üretimi (CreateAnnouncementType/BusinessRules emsali — Faz 10.9'da ortaklaştı).</summary>
public static class SlugHelper
{
    public static string Slugify(string value)
    {
        var map = new (char From, char To)[] { ('ç', 'c'), ('ğ', 'g'), ('ı', 'i'), ('ö', 'o'), ('ş', 's'), ('ü', 'u') };
        var lower = value.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            var c = ch;
            foreach (var (from, to) in map)
                if (c == from) { c = to; break; }

            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
