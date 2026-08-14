using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// Faz 12.2 — <b>panel süper admin parolasının yerel, git'e girmeyen kaynağı.</b>
    /// </summary>
    /// <remarks>
    /// 🐛 <b>Neden eklendi:</b> panel parolası 11.18'de değiştirildi ve o günden sonra her
    /// oturumda "parola ne?" sorusu yeniden doğdu — kaynaktaki sabit artık yalan söylüyordu,
    /// doğrusu ise <b>hiçbir yerde yazılı değildi</b> (ve yazılamazdı: depo herkese açık,
    /// 11.18'de tam bu yüzden gerçek bir sızıntı yaşandı).
    ///
    /// 🔑 Çözüm: parola <c>secrets/</c> altında durur — klasör <c>.gitignore</c>'da
    /// (<c>secrets/*</c>, yalnız README hariç), yani <b>commit edilmesi imkânsız</b>.
    /// Dosya varsa seed onu kullanır; yoksa hiçbir şey değişmez (eski davranış aynen sürer).
    ///
    /// ⚠️ <b>Yalnız Development.</b> Production'da parolayı bir dosyadan sessizce ezmek,
    /// eski/kopyalanmış bir dosyanın canlı yönetici parolasını geri alması demektir.
    /// </remarks>
    public const string PanelPasswordConfigKey = "Panel:SuperAdmin:Password";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var cfg = scope.ServiceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
        var env = scope.ServiceProvider.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();

        await db.Database.MigrateAsync();

        // Yapılandırılmış parola (secrets/panel-admin.json ya da ortam değişkeni).
        // Boşsa akış 12.2 öncesiyle birebir aynı kalır.
        var configuredPassword = cfg?[PanelPasswordConfigKey];
        var canApplyConfigured =
            !string.IsNullOrWhiteSpace(configuredPassword) &&
            string.Equals(env?.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
        {
            db.Users.Add(new User
            {
                Phone = AdminPhone,
                Username = AdminUsername,
                Email = "admin@kadirli.app",
                Password = hasher.HashPassword(canApplyConfigured ? configuredPassword! : AdminPassword),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                // 🔑 Faz 11.18: varsayılan parola KAYNAKTA yazılı ve herkese açık bir depoda
                // duruyor. 11.15c giriş ekranındaki sızıntıyı kapattı ama parolanın kendisi
                // zayıf kalmıştı. Bu bayrak sayesinde varsayılan parola artık yalnızca
                // "parolanı değiştir" ekranını açan bir anahtar — panelde başka hiçbir
                // kapıyı açmıyor. Bayrağı `ChangeMyPasswordCommand` temizler.
                //
                // 🔑 Faz 12.2: parolayı yönetici KENDİ dosyasında belirlediyse bayrak
                // gerekmez — 11.18'in kuralı "parolayı sahibi değil BAŞKASI belirlediyse
                // değiştirmeye zorla"dır ve burada belirleyen sahibin ta kendisidir.
                MustChangePassword = !canApplyConfigured
            });
        }
        else
        {
            // 🔑 Faz 11.18 — **zaten kurulmuş** sistemlerin boşluğu. Yukarıdaki blok yalnız
            // hiç super_admin yokken çalışır; bugüne kadar kurulmuş her panelde admin
            // varsayılan parolayla yaşamaya devam ederdi ve bayrağı hiç almazdı.
            //
            // ⚠️ Ölçüt "super_admin'dir" DEĞİL, **"hâlâ varsayılan parolayı kullanıyor"**:
            // parolasını çoktan güçlü bir şeyle değiştirmiş bir yöneticiyi her açılışta
            // parola ekranına düşürmek, kapatmaya çalıştığımız riski kapatmadan yalnızca
            // gürültü üretirdi. Doğrulama hash üzerinden yapılır — parola karşılaştırması
            // düz metinle yapılmaz.
            var admins = await db.Users
                .Where(u => u.Role == UserRole.SuperAdmin && !u.MustChangePassword && u.Password != null)
                .ToListAsync();

            foreach (var admin in admins.Where(a => hasher.VerifyPassword(AdminPassword, a.Password!)))
                admin.MustChangePassword = true;

            // 🔑 Faz 12.2 — **parolayı yapılandırılmış değere hizala** (yalnız Development).
            //
            // Bu, "her oturumda parola ne?" sorusunu kalıcı olarak kapatan satır: dosya
            // artık tek doğruluk kaynağı. Parola zaten dosyadakiyle aynıysa hiçbir şey
            // yazılmaz — aksi hâlde her açılışta `PasswordChangedAt` tazelenir ve
            // `OnValidatePrincipal` yöneticiyi KENDİ oturumundan atardı (11.18 dersi).
            if (canApplyConfigured)
            {
                var byUsername = await db.Users
                    .Where(u => u.Role == UserRole.SuperAdmin && u.Username == AdminUsername && u.Password != null)
                    .ToListAsync();

                foreach (var admin in byUsername.Where(a => !hasher.VerifyPassword(configuredPassword!, a.Password!)))
                {
                    admin.Password = hasher.HashPassword(configuredPassword!);
                    admin.MustChangePassword = false;
                    // ⚠️ Kilit de temizlenir: parolayı unutup kilitlenmiş bir yöneticinin
                    // dosyayı düzeltip yeniden başlatması, 15 dakika beklemesinden iyidir.
                    admin.FailedLoginAttempts = 0;
                    admin.LockedOutUntil = null;
                }
            }
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

        // Faz 12.3: kesinti bildirimi bir DUYURUDUR ve o duyurunun türü olmak zorunda.
        // ⚠️ Üstteki blok `AnnouncementTypes` tablosu BOŞSA çalışıyor — yani 12.3'ten önce
        // ayağa kalkmış her veritabanında (dev dâhil) tür listesi doludur ve o blok bir daha
        // hiç koşmaz. Türün varlığını ayrıca ve tür bazında garantilemezsek kesinti bildirimi
        // canlıda "duyuru türü bulunamadı" ile patlardı — üstelik yalnız eski kurulumlarda,
        // yani geliştiricinin makinesinde görünmeyen bir hata olarak.
        await EnsurePowerOutageAnnouncementTypeAsync(db);

        // Faz 12.3: serbest metin mahalleleri sözlüğe bağla (idempotent, yalnız FK'sı boş
        // satırlara dokunur). Raporu panel "mahallesi eşleşmemiş kesinti" şeridinde gösterir.
        await PowerOutageNeighborhoodBackfill.RunAsync(
            db, scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(PowerOutageNeighborhoodBackfill)));

        // Faz 12.4: il/ilçe sözlüğü + etkinliklerin geri doldurulması.
        await EnsureDistrictsAsync(db);
        await EventDistrictBackfill.RunAsync(
            db, scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(EventDistrictBackfill)));

        // Faz 12.5: kalkış noktası sözlüğü (satır bazında idempotent — aşağıdaki nota bak).
        await EnsureDeparturePointsAsync(db);

        // Faz 12.16: KVKK belge KABUKLARI (metin YOK — bkz. metodun notu).
        await EnsureLegalDocumentsAsync(db);
    }

    /// <summary>
    /// Faz 12.16 — hukuki belgelerin <b>kabukları</b>, tür bazında idempotent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>METİN BİLİNÇLİ OLARAK SEED EDİLMİYOR</b> — yalnız belgenin kimliği (tür, başlık,
    /// zorunluluk, sıra) açılıyor, <c>legal_document_versions</c>'a <b>tek satır bile</b>
    /// yazılmıyor. Sebep: seed edilmiş bir "örnek KVKK metni" er ya da geç <b>yayına çıkar</b>
    /// ve o an vatandaş, hiçbir hukukçunun okumadığı bir metne rıza vermiş olur. Kayıt
    /// ekranının bir belgeyi sorabilmesi için <b>yayında bir sürümü</b> olmak zorunda, yani
    /// bu seed tek başına hiçbir şeyi zorunlu kılmaz: metni yönetici panelden yazar ve
    /// yayınlar. 🔑 Bu, projenin "tahmini koordinat yazma" kuralının (12.5 kalkış noktaları)
    /// hukuki metin hâli — <b>yanlış doldurmak boş bırakmaktan kötüdür.</b>
    /// </para>
    /// <para>
    /// ⚠️ Var olan satır <b>ezilmez</b>: başlığı ya da <c>IsMandatory</c>'yi panelden
    /// değiştiren yönetici, bir sonraki açılışta kararını geri alınmış bulmamalı.
    /// </para>
    /// </remarks>
    private static async Task EnsureLegalDocumentsAsync(AppDbContext db)
    {
        // (Tür, Başlık, Zorunlu mu, Kayıt ekranında görünsün mü, Sıra).
        // 🔴 Zorunlu ↔ isteğe bağlı AYRI BELGELERDİR (KVKK'nın en sık ihlal edilen kuralı):
        // "hizmet için gerekli işleme" ile "ticari elektronik ileti"yi tek kutuda toplamak
        // rızayı GEÇERSİZ kılar. Sıra da anlamlı: önce aydınlatma, sonra rıza.
        var seed = new (string Type, string Title, bool Mandatory, bool AtRegistration, int Order)[]
        {
            (LegalDocumentTypes.Kvkk, "KVKK Aydınlatma Metni", true, true, 0),
            (LegalDocumentTypes.ExplicitConsent, "Açık Rıza Metni", true, true, 1),
            (LegalDocumentTypes.TermsOfUse, "Kullanım Koşulları", true, true, 2),
            (LegalDocumentTypes.PrivacyPolicy, "Gizlilik Politikası", false, false, 3),
            (LegalDocumentTypes.CommercialMessage, "Ticari Elektronik İleti İzni", false, true, 4)
        };

        var known = new HashSet<string>(
            await db.LegalDocuments.Select(d => d.Type).ToListAsync(), StringComparer.Ordinal);
        var added = false;

        foreach (var (type, title, mandatory, atRegistration, order) in seed)
        {
            if (!known.Add(type)) continue;

            db.LegalDocuments.Add(new LegalDocument
            {
                Type = type,
                Title = title,
                IsMandatory = mandatory,
                ShowAtRegistration = atRegistration,
                SortOrder = order,
                IsActive = true
            });
            added = true;
        }

        if (added) await db.SaveChangesAsync();
    }

    /// <summary>
    /// Faz 12.5 — kalkış noktası sözlüğü, <b>satır bazında</b> idempotent
    /// (<see cref="EnsureDistrictsAsync"/> ile aynı gerekçe: "tablo boşsa" bloğu, listeye
    /// sonradan eklenen bir satırı ayakta olan hiçbir veritabanına sokmaz).
    /// </summary>
    /// <remarks>
    /// ⚠️ Var olan satır <b>ezilmez</b>: koordinatı panelden düzelten yönetici, bir sonraki
    /// açılışta düzeltmesini geri alınmış bulmamalı. Koordinatlar burada bilerek <c>null</c>:
    /// tahmini bir koordinat, "Yol tarifi" butonunu <b>yanlış yere</b> götürür — yanlış
    /// bağlamak hiç bağlamamaktan kötüdür (12.3'ün mahalle dersi).
    /// </remarks>
    private static async Task EnsureDeparturePointsAsync(AppDbContext db)
    {
        var seed = new (string Name, string? Address, int Order)[]
        {
            ("Kadirli Otogarı", "Kadirli / Osmaniye", 0),
            ("Minibüs Garajı", "Kadirli / Osmaniye", 1)
        };

        var known = new HashSet<string>(
            await db.TransportDeparturePoints.Select(p => p.Slug).ToListAsync(), StringComparer.Ordinal);
        var added = false;

        foreach (var (name, address, order) in seed)
        {
            // 🔴 Slug'ın tek sahibi SlugHelper (görünmez sözleşme #21) — ikinci bir küçültme yok.
            var slug = Application.Common.Utils.SlugHelper.Slugify(name);
            if (!known.Add(slug)) continue;

            db.TransportDeparturePoints.Add(new TransportDeparturePoint
            {
                Name = name,
                Slug = slug,
                Address = address,
                DisplayOrder = order,
                IsActive = true
            });
            added = true;
        }

        if (added) await db.SaveChangesAsync();
    }

    /// <summary>
    /// Faz 12.4 — il/ilçe sözlüğü, <b>satır bazında</b> idempotent.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bilerek "tablo boşsa" bloğu <b>değil</b>: 12.3'te tam bu tuzağa düşüldü — koşullu blok,
    /// listeye <i>sonradan</i> eklenen bir satırı zaten ayakta olan hiçbir veritabanına sokmaz
    /// ve hata yalnız <b>eski kurulumlarda</b> çıkar (geliştiricinin makinesinde görünmez).
    /// Buraya yarın "Ceyhan" eklenirse mevcut kurulumlar da onu alır.
    ///
    /// ⚠️ Var olan satırın adı/sırası <b>ezilmez</b>: sözlük panelden yönetilebiliyor, yöneticinin
    /// düzeltmesi her açılışta geri alınamaz.
    /// </remarks>
    private static async Task EnsureDistrictsAsync(AppDbContext db)
    {
        // (İl, İlçe, Merkez mi, Sıra). Kadirli başta: ev ilçesi listede önce görünmeli.
        var seed = new (string Province, string Name, bool IsCenter, int Order)[]
        {
            ("Osmaniye", "Kadirli", false, 0),
            ("Osmaniye", "Merkez", true, 1),
            ("Osmaniye", "Düziçi", false, 2),
            ("Osmaniye", "Bahçe", false, 3),
            ("Osmaniye", "Hasanbeyli", false, 4),
            ("Osmaniye", "Sumbas", false, 5),
            ("Osmaniye", "Toprakkale", false, 6),
            // Çevre il merkezleri — Kadirli'ye günübirlik gidilen yerler.
            ("Adana", "Merkez", true, 10),
            ("Hatay", "Merkez", true, 11),
            ("Kahramanmaraş", "Merkez", true, 12),
            ("Gaziantep", "Merkez", true, 13)
        };

        var existing = await db.Districts.Select(d => d.Slug).ToListAsync();
        var known = new HashSet<string>(existing, StringComparer.Ordinal);
        var added = false;

        foreach (var (province, name, isCenter, order) in seed)
        {
            var slug = Application.Features.Lookups.DistrictDefaults.SlugFor(province, name);
            if (!known.Add(slug)) continue;

            db.Districts.Add(new District
            {
                Name = name,
                Slug = slug,
                ProvinceName = province,
                IsCenter = isCenter,
                DisplayOrder = order,
                IsActive = true
            });
            added = true;
        }

        if (added) await db.SaveChangesAsync();
    }

    /// <summary>
    /// Faz 12.3 — "Elektrik Kesintisi" duyuru türü, <b>tür bazında</b> idempotent.
    /// Slug üzerinden aranır: ad panelden değiştirilse bile aynı satır bulunur.
    /// </summary>
    public const string PowerOutageAnnouncementTypeSlug = "elektrik-kesintisi";

    private static async Task EnsurePowerOutageAnnouncementTypeAsync(AppDbContext db)
    {
        if (await db.AnnouncementTypes.AnyAsync(t => t.Slug == PowerOutageAnnouncementTypeSlug))
            return;

        var order = await db.AnnouncementTypes.AnyAsync()
            ? await db.AnnouncementTypes.MaxAsync(t => t.DisplayOrder) + 1
            : 0;

        db.AnnouncementTypes.Add(new AnnouncementType
        {
            Name = "Elektrik Kesintisi",
            Slug = PowerOutageAnnouncementTypeSlug,
            Icon = "fa-bolt",
            Color = "#F59E0B",
            DisplayOrder = order
        });

        await db.SaveChangesAsync();
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

    /// <summary>
    /// Faz 11.15b: buradaki kopya gerçekleme kaldırıldı. Seeder ile çalışma zamanı **aynı**
    /// slug kuralını kullanmalı; ayrıştıklarında (10.9-11.15b arası olduğu gibi) seed'lenen
    /// kayıtla panelden eklenen kayıt farklı slug alır ve fark hiçbir yerde görünmez.
    /// Kuralın tek sahibi: <see cref="KadirliApp.Application.Common.Utils.SlugHelper"/>.
    /// </summary>
    internal static string Slugify(string value)
        => KadirliApp.Application.Common.Utils.SlugHelper.Slugify(value);
}
