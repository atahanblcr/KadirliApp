using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KadirliApp.Infrastructure.Persistence;

/// <summary>
/// İdempotent başlangıç verisi: super_admin kullanıcısı ve panelin çalışması için
/// zorunlu lookup tabloları (mahalle, duyuru türü, ilan/etkinlik/mekan/işletme/rehber
/// kategorileri, mezarlık, cami). Her blok yalnızca tablo boşsa çalışır.
/// </summary>
public static class DbSeeder
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "Admin123!";
    public const string AdminPhone = "+905000000001";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
        {
            db.Users.Add(new User
            {
                Phone = AdminPhone,
                Username = AdminUsername,
                Email = "admin@kadirli.app",
                Password = hasher.HashPassword(AdminPassword),
                Role = UserRole.SuperAdmin,
                IsActive = true
            });
        }

        if (!await db.Neighborhoods.AnyAsync())
        {
            var names = new[]
            {
                "Cengiz Topel", "Savrun", "Şehit Kansu", "Kurtuluş", "Yenimahalle",
                "Karataş", "Fatih", "Cumhuriyet", "Bahçelievler", "Tufanpaşa"
            };
            var order = 0;
            foreach (var n in names)
                db.Neighborhoods.Add(new Neighborhood { Name = n, Slug = Slugify(n), Type = "merkez", DisplayOrder = order++, IsActive = true });
        }

        if (!await db.AnnouncementTypes.AnyAsync())
        {
            var types = new (string Name, string Icon, string Color)[]
            {
                ("Genel Duyuru", "fa-bullhorn", "#4F46E5"),
                ("Elektrik Kesintisi", "fa-bolt", "#F59E0B"),
                ("Su Kesintisi", "fa-tint", "#3B82F6"),
                ("Belediye Duyurusu", "fa-landmark", "#10B981"),
                ("Etkinlik Duyurusu", "fa-calendar", "#EC4899")
            };
            var order = 0;
            foreach (var t in types)
                db.AnnouncementTypes.Add(new AnnouncementType { Name = t.Name, Slug = Slugify(t.Name), Icon = t.Icon, Color = t.Color, DisplayOrder = order++ });
        }

        if (!await db.AdCategories.AnyAsync())
        {
            var cats = new[] { "Araçlar", "Emlak", "Elektronik", "Ev Eşyası", "Giyim", "Hayvanlar", "İş Makineleri", "Diğer" };
            var order = 0;
            foreach (var c in cats)
                db.AdCategories.Add(new AdCategory { Name = c, Slug = Slugify(c), DisplayOrder = order++, IsActive = true });
        }

        if (!await db.EventCategories.AnyAsync())
        {
            foreach (var c in new[] { "Konser", "Festival", "Tiyatro", "Spor", "Sergi", "Diğer" })
                db.EventCategories.Add(new EventCategory { Name = c, Slug = Slugify(c) });
        }

        if (!await db.BusinessCategories.AnyAsync())
        {
            foreach (var c in new[] { "Kafe & Restoran", "Market", "Giyim", "Elektronik", "Kuaför & Güzellik", "Diğer" })
                db.BusinessCategories.Add(new BusinessCategory { Name = c, Slug = Slugify(c) });
        }

        if (!await db.PlaceCategories.AnyAsync())
        {
            var cats = new[] { "Doğa & Yayla", "Tarihi Yerler", "Piknik Alanları", "Müzeler", "Parklar" };
            var order = 0;
            foreach (var c in cats)
                db.PlaceCategories.Add(new PlaceCategory { Name = c, Slug = Slugify(c), DisplayOrder = order++ });
        }

        if (!await db.GuideCategories.AnyAsync())
        {
            var cats = new[] { "Resmi Kurumlar", "Sağlık", "Eğitim", "Ulaşım", "Acil Numaralar", "Esnaf" };
            var order = 0;
            foreach (var c in cats)
                db.GuideCategories.Add(new GuideCategory { Name = c, Slug = Slugify(c), DisplayOrder = order++ });
        }

        if (!await db.Cemeteries.AnyAsync())
        {
            foreach (var c in new[] { "Kadirli Asri Mezarlığı", "Savrun Mezarlığı", "Karataş Mezarlığı" })
                db.Cemeteries.Add(new Cemetery { Name = c });
        }

        if (!await db.Mosques.AnyAsync())
        {
            foreach (var m in new[] { "Kadirli Merkez Cami", "Alacami", "Savrun Cami", "Yenimahalle Cami" })
                db.Mosques.Add(new Mosque { Name = m });
        }

        await db.SaveChangesAsync();

        // Faz 10.5: ilan kategori ağacı (alt kategoriler) + kategoriye özel alanlar.
        // Ana bloklardan SONRA koşar çünkü ebeveyn kategorilerin Id'leri kaydedilmiş olmalı
        // (dev DB'de ad_categories dolu olduğundan üstteki blok çalışmaz; bu bloklar kendi
        // tablolarına/koşullarına göre ayrıca idempotenttir).
        await SeedAdCategoryTreeAsync(db);
    }

    private static async Task SeedAdCategoryTreeAsync(AppDbContext db)
    {
        if (!await db.AdCategories.AnyAsync(c => c.ParentId != null))
        {
            var subs = new (string ParentSlug, string[] Children)[]
            {
                ("araclar", new[] { "Otomobil", "Motosiklet", "Ticari Araç" }),
                ("emlak", new[] { "Satılık Konut", "Kiralık Konut", "Arsa" })
            };
            foreach (var (parentSlug, children) in subs)
            {
                var parent = await db.AdCategories.FirstOrDefaultAsync(c => c.Slug == parentSlug && c.ParentId == null);
                if (parent is null) continue;
                var order = 0;
                foreach (var name in children)
                    db.AdCategories.Add(new AdCategory { Name = name, Slug = Slugify(name), ParentId = parent.Id, DisplayOrder = order++, IsActive = true });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.CategoryProperties.AnyAsync())
        {
            var otomobil = await db.AdCategories.FirstOrDefaultAsync(c => c.Slug == "otomobil");
            if (otomobil is not null)
            {
                db.CategoryProperties.AddRange(
                    SelectProperty(otomobil.Id, "Yakıt Tipi", true, 0, "Benzin", "Dizel", "LPG", "Hibrit", "Elektrik"),
                    SelectProperty(otomobil.Id, "Vites", true, 1, "Manuel", "Otomatik", "Yarı Otomatik"),
                    new CategoryProperty { CategoryId = otomobil.Id, PropertyName = "Model Yılı", PropertyType = PropertyType.Number, IsRequired = true, DisplayOrder = 2 },
                    new CategoryProperty { CategoryId = otomobil.Id, PropertyName = "Kilometre", PropertyType = PropertyType.Number, DisplayOrder = 3 },
                    new CategoryProperty { CategoryId = otomobil.Id, PropertyName = "Renk", PropertyType = PropertyType.Text, DisplayOrder = 4 });
            }

            var satilikKonut = await db.AdCategories.FirstOrDefaultAsync(c => c.Slug == "satilik-konut");
            if (satilikKonut is not null)
            {
                db.CategoryProperties.AddRange(
                    SelectProperty(satilikKonut.Id, "Oda Sayısı", true, 0, "1+0", "1+1", "2+1", "3+1", "4+1", "5 ve üzeri"),
                    new CategoryProperty { CategoryId = satilikKonut.Id, PropertyName = "Metrekare", PropertyType = PropertyType.Number, IsRequired = true, DisplayOrder = 1 },
                    SelectProperty(satilikKonut.Id, "Isınma", false, 2, "Doğalgaz", "Soba", "Klima", "Merkezi"),
                    new CategoryProperty { CategoryId = satilikKonut.Id, PropertyName = "Bina Yaşı", PropertyType = PropertyType.Number, DisplayOrder = 3 });
            }

            await db.SaveChangesAsync();
        }
    }

    private static CategoryProperty SelectProperty(Guid categoryId, string name, bool required, int order, params string[] options)
    {
        var property = new CategoryProperty
        {
            CategoryId = categoryId,
            PropertyName = name,
            PropertyType = PropertyType.Select,
            IsRequired = required,
            DisplayOrder = order
        };
        for (var i = 0; i < options.Length; i++)
            property.Options.Add(new PropertyOption { OptionValue = options[i], DisplayOrder = i });
        return property;
    }

    internal static string Slugify(string value)
    {
        var map = new Dictionary<char, string>
        {
            ['ç'] = "c", ['ğ'] = "g", ['ı'] = "i", ['ö'] = "o", ['ş'] = "s", ['ü'] = "u",
            ['Ç'] = "c", ['Ğ'] = "g", ['İ'] = "i", ['Ö'] = "o", ['Ş'] = "s", ['Ü'] = "u"
        };
        var chars = value.Trim().ToLowerInvariant()
            .Select(ch => map.TryGetValue(ch, out var r) ? r : ch.ToString())
            .Select(s => s.Length == 1 && !char.IsLetterOrDigit(s[0]) ? "-" : s);
        var slug = string.Concat(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }
}
