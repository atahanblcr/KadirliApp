using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Infrastructure.Persistence;

/// <summary>
/// Panel testleri için idempotent örnek veri: her blok yalnızca kendi tablosu boşsa
/// çalışır. DbSeeder'ın lookup verileri (kategori, mahalle, mezarlık vb.) ve admin
/// kullanıcısı ekli olmalıdır (Program.cs açılışında zaten çalışıyor).
/// </summary>
public static class MockDataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin);
        if (admin == null)
            throw new InvalidOperationException("MockDataSeeder: super_admin bulunamadı; önce DbSeeder çalışmalı.");

        // --- Normal kullanıcılar ---
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.User))
        {
            var neighborhood = await db.Neighborhoods.OrderBy(n => n.DisplayOrder).FirstOrDefaultAsync();
            var users = new[]
            {
                new User { Phone = "+905321110001", Username = "ahmetk", Email = "ahmet@example.com", Age = 34 },
                new User { Phone = "+905321110002", Username = "aysedmr", Email = "ayse@example.com", Age = 28 },
                new User { Phone = "+905321110003", Username = "mehmetc", Age = 45 },
                new User { Phone = "+905321110004", Username = "fatmay", Email = "fatma@example.com", Age = 39 }
            };
            foreach (var u in users)
            {
                u.Role = UserRole.User;
                u.IsActive = true;
                u.PrimaryNeighborhoodId = neighborhood?.Id;
                db.Users.Add(u);
            }
            await db.SaveChangesAsync();
        }

        var mockUsers = await db.Users.Where(u => u.Role == UserRole.User).OrderBy(u => u.Phone).ToListAsync();
        var user1 = mockUsers.ElementAtOrDefault(0) ?? admin;
        var user2 = mockUsers.ElementAtOrDefault(1) ?? admin;

        // --- İlanlar (2 pending + 3 approved) ---
        if (!await db.Ads.AnyAsync())
        {
            var cats = await db.AdCategories.OrderBy(c => c.DisplayOrder).ToListAsync();
            Guid CatId(string name) => cats.FirstOrDefault(c => c.Name == name)?.Id ?? cats.First().Id;

            var ads = new[]
            {
                new Ad { Title = "Sahibinden Temiz Fiat Egea", Description = "2021 model, 45.000 km, boyasız değişensiz.", Price = 750000, CategoryId = CatId("Araçlar"), UserId = user1.Id, SellerName = "Ahmet K.", ContactPhone = user1.Phone, Status = "pending" },
                new Ad { Title = "Merkez Konumda 3+1 Kiralık Daire", Description = "Savrun mahallesinde, yeni binada, doğalgazlı.", Price = 9000, CategoryId = CatId("Emlak"), UserId = user2.Id, SellerName = "Ayşe D.", ContactPhone = user2.Phone, Status = "pending" },
                new Ad { Title = "Az Kullanılmış Çamaşır Makinesi", Description = "Arçelik 9 kg, 2 yıllık, sorunsuz çalışıyor.", Price = 8500, CategoryId = CatId("Ev Eşyası"), UserId = user1.Id, SellerName = "Ahmet K.", ContactPhone = user1.Phone, Status = "approved", ApprovedBy = admin.Id, ApprovedAt = now.AddDays(-2) },
                new Ad { Title = "iPhone 13 128 GB", Description = "Kutulu faturalı, batarya sağlığı %89.", Price = 28000, CategoryId = CatId("Elektronik"), UserId = user2.Id, SellerName = "Ayşe D.", ContactPhone = user2.Phone, Status = "approved", ApprovedBy = admin.Id, ApprovedAt = now.AddDays(-1) },
                new Ad { Title = "Simental Düve", Description = "14 aylık, aşıları tam, sağlıklı.", Price = 95000, CategoryId = CatId("Hayvanlar"), UserId = user1.Id, SellerName = "Ahmet K.", ContactPhone = user1.Phone, Status = "approved", ApprovedBy = admin.Id, ApprovedAt = now.AddHours(-6) }
            };
            foreach (var ad in ads)
            {
                ad.ExpiresAt = now.AddDays(30);
                db.Ads.Add(ad);
            }
            await db.SaveChangesAsync();
        }

        // --- Duyurular ---
        if (!await db.Announcements.AnyAsync())
        {
            var types = await db.AnnouncementTypes.OrderBy(t => t.DisplayOrder).ToListAsync();
            Guid TypeId(string name) => types.FirstOrDefault(t => t.Name == name)?.Id ?? types.First().Id;

            db.Announcements.AddRange(
                new Announcement { TypeId = TypeId("Belediye Duyurusu"), Title = "Pazar Yeri Taşınıyor", Body = "Cumartesi pazarı bu haftadan itibaren kapalı pazar alanında kurulacaktır.", Priority = 1, Status = "active", CreatedBy = admin.Id, SentAt = now.AddDays(-3) },
                new Announcement { TypeId = TypeId("Su Kesintisi"), Title = "Savrun Mahallesinde Su Kesintisi", Body = "Ana hat bakımı nedeniyle yarın 09:00-15:00 arası su verilemeyecektir.", Priority = 2, Status = "active", CreatedBy = admin.Id, SentAt = now.AddDays(-1) },
                new Announcement { TypeId = TypeId("Genel Duyuru"), Title = "Kan Bağışı Kampanyası", Body = "Kızılay kan bağış aracı Cuma günü hükümet konağı önünde olacaktır.", Priority = 0, Status = "active", CreatedBy = admin.Id, SentAt = now.AddHours(-8) }
            );
            await db.SaveChangesAsync();
        }

        // --- Vefat ilanları ---
        if (!await db.DeathNotices.AnyAsync())
        {
            var cemetery = await db.Cemeteries.FirstOrDefaultAsync();
            var mosque = await db.Mosques.FirstOrDefaultAsync();
            var neighborhood = await db.Neighborhoods.OrderBy(n => n.DisplayOrder).FirstOrDefaultAsync();

            var funeral1 = now.Date.AddDays(1);
            var funeral2 = now.Date;
            db.DeathNotices.AddRange(
                new DeathNotice
                {
                    DeceasedName = "Hüseyin Yıldırım",
                    FuneralDate = DateTime.SpecifyKind(funeral1, DateTimeKind.Utc), FuneralTime = new TimeSpan(13, 30, 0),
                    CemeteryId = cemetery?.Id, MosqueId = mosque?.Id, NeighborhoodId = neighborhood?.Id,
                    CondolenceAddress = "Cengiz Topel Mah. aile evi", AddedBy = admin.Id,
                    Status = "approved", ApprovedBy = admin.Id, ApprovedAt = now,
                    AutoArchiveAt = DateTime.SpecifyKind(funeral1.AddDays(7), DateTimeKind.Utc)
                },
                new DeathNotice
                {
                    DeceasedName = "Emine Kaya",
                    FuneralDate = DateTime.SpecifyKind(funeral2, DateTimeKind.Utc), FuneralTime = new TimeSpan(11, 0, 0),
                    CemeteryId = cemetery?.Id, MosqueId = mosque?.Id, NeighborhoodId = neighborhood?.Id,
                    AddedBy = admin.Id, Status = "pending",
                    AutoArchiveAt = DateTime.SpecifyKind(funeral2.AddDays(7), DateTimeKind.Utc)
                }
            );
            await db.SaveChangesAsync();
        }

        // --- Eczaneler + bugünün nöbeti ---
        if (!await db.Pharmacies.AnyAsync())
        {
            var p1 = new Pharmacy { Name = "Merkez Eczanesi", Address = "Cumhuriyet Cad. No:12", Phone = "+903287141001", PharmacistName = "Ecz. Zeynep Aslan", WorkingHours = "08:30 - 19:00", IsActive = true };
            var p2 = new Pharmacy { Name = "Savrun Eczanesi", Address = "Savrun Mah. Okul Sok. No:3", Phone = "+903287141002", PharmacistName = "Ecz. Murat Demir", WorkingHours = "08:30 - 19:00", IsActive = true };
            var p3 = new Pharmacy { Name = "Şifa Eczanesi", Address = "Fatih Mah. Hastane Cad. No:8", Phone = "+903287141003", PharmacistName = "Ecz. Elif Şahin", WorkingHours = "08:30 - 19:00", IsActive = true };
            db.Pharmacies.AddRange(p1, p2, p3);
            await db.SaveChangesAsync();

            db.PharmacySchedules.Add(new PharmacySchedule
            {
                PharmacyId = p1.Id,
                DutyDate = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc),
                Source = "mock"
            });
            await db.SaveChangesAsync();
        }

        // --- Elektrik kesintileri ---
        if (!await db.PowerOutages.AnyAsync())
        {
            db.PowerOutages.AddRange(
                new PowerOutage { Neighborhood = "Karataş", StartTime = now.AddDays(1).Date.AddHours(9), EndTime = now.AddDays(1).Date.AddHours(14), Reason = "Trafo bakımı", Source = "Toroslar EDAŞ" },
                new PowerOutage { Neighborhood = "Yenimahalle", StartTime = now.AddDays(2).Date.AddHours(10), EndTime = now.AddDays(2).Date.AddHours(12), Reason = "Hat yenileme çalışması", Source = "Toroslar EDAŞ" }
            );
            await db.SaveChangesAsync();
        }

        // --- Ulaşım hatları ---
        if (!await db.IntracityRoutes.AnyAsync())
        {
            db.IntracityRoutes.AddRange(
                new IntracityRoute { RouteNumber = "1", RouteName = "Merkez - Devlet Hastanesi", FirstDeparture = new TimeSpan(6, 30, 0), LastDeparture = new TimeSpan(22, 0, 0), FrequencyMinutes = 20, IsActive = true },
                new IntracityRoute { RouteNumber = "2", RouteName = "Merkez - Sanayi Sitesi", FirstDeparture = new TimeSpan(7, 0, 0), LastDeparture = new TimeSpan(21, 0, 0), FrequencyMinutes = 30, IsActive = true }
            );
        }

        if (!await db.IntercityRoutes.AnyAsync())
        {
            db.IntercityRoutes.AddRange(
                new IntercityRoute { Destination = "Adana", Price = 220, DurationMinutes = 105, Company = "Kadirli Seyahat", IsActive = true },
                new IntercityRoute { Destination = "Osmaniye", Price = 120, DurationMinutes = 50, Company = "Kadirli Birlik", IsActive = true }
            );
        }
        await db.SaveChangesAsync();

        // Faz 10.8: kalkış saatleri + duraklar — hatlar zaten dolu olan dev DB'de de çalışır
        // (mevcut hatlar DB'den okunur, yalnız boş tablolar doldurulur).
        if (!await db.IntercitySchedules.AnyAsync())
        {
            var intercity = await db.IntercityRoutes.ToListAsync();
            foreach (var route in intercity)
            {
                var times = route.Destination == "Adana"
                    ? new[] { new TimeSpan(7, 0, 0), new TimeSpan(10, 30, 0), new TimeSpan(14, 0, 0), new TimeSpan(17, 30, 0) }
                    : new[] { new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), new TimeSpan(18, 0, 0) };
                foreach (var t in times)
                    db.IntercitySchedules.Add(new IntercitySchedule { RouteId = route.Id, DepartureTime = t, IsActive = true });
            }
        }

        if (!await db.IntracityStops.AnyAsync())
        {
            var intracity = await db.IntracityRoutes.ToListAsync();
            foreach (var route in intracity)
            {
                var stops = route.RouteNumber == "1"
                    ? new[] { "Cumhuriyet Meydanı", "Belediye", "Kız Meslek Lisesi", "Devlet Hastanesi" }
                    : new[] { "Cumhuriyet Meydanı", "Otogar", "Küçük Sanayi Girişi", "Sanayi Sitesi" };
                for (var i = 0; i < stops.Length; i++)
                    db.IntracityStops.Add(new IntracityStop { RouteId = route.Id, StopName = stops[i], StopOrder = i + 1, TimeFromStart = i * 7 });
            }
        }
        await db.SaveChangesAsync();

        // --- İşletmeler + kampanyalar ---
        if (!await db.Businesses.AnyAsync())
        {
            var bizCats = await db.BusinessCategories.ToListAsync();
            Guid BizCatId(string name) => bizCats.FirstOrDefault(c => c.Name == name)?.Id ?? bizCats.First().Id;

            var b1 = new Business { BusinessName = "Savrun Cafe", CategoryId = BizCatId("Kafe & Restoran"), Address = "Cumhuriyet Cad. No:45", Phone = "+903287142001", IsVerified = true, VerifiedBy = admin.Id, VerifiedAt = now };
            var b2 = new Business { BusinessName = "Kadirli Teknoloji Market", CategoryId = BizCatId("Elektronik"), Address = "Fatih Mah. Çarşı içi No:7", Phone = "+903287142002" };
            db.Businesses.AddRange(b1, b2);
            await db.SaveChangesAsync();

            if (!await db.Campaigns.AnyAsync())
            {
                db.Campaigns.AddRange(
                    new Campaign
                    {
                        BusinessId = b1.Id, Title = "Kahve Yanına Tatlı %20 İndirimli",
                        Description = "Hafta içi 14:00-17:00 arası tüm tatlılarda geçerlidir.",
                        DiscountPercentage = 20, DiscountCode = "TATLI20", Terms = "Başka kampanyayla birleştirilemez.",
                        StartDate = now.AddDays(-3), EndDate = now.AddDays(14),
                        Status = "approved", ApprovedBy = admin.Id, ApprovedAt = now.AddDays(-3)
                    },
                    new Campaign
                    {
                        BusinessId = b2.Id, Title = "Kulaklıklarda Sezon Sonu İndirimi",
                        Description = "Seçili kulaklık modellerinde %15 indirim.",
                        DiscountPercentage = 15, DiscountCode = "SES15",
                        StartDate = now, EndDate = now.AddDays(21),
                        Status = "pending"
                    }
                );
                await db.SaveChangesAsync();
            }
        }

        // --- Etkinlikler (1 pending + 2 approved) ---
        if (!await db.Events.AnyAsync())
        {
            var evCats = await db.EventCategories.ToListAsync();
            Guid EvCatId(string name) => evCats.FirstOrDefault(c => c.Name == name)?.Id ?? evCats.First().Id;

            db.Events.AddRange(
                new Event
                {
                    Title = "Kadirli Yaz Konseri", Description = "Belediye meydanında ücretsiz halk konseri.",
                    CategoryId = EvCatId("Konser"), EventDate = DateTime.SpecifyKind(now.Date.AddDays(10), DateTimeKind.Utc), EventTime = new TimeSpan(20, 30, 0),
                    VenueName = "Belediye Meydanı", Organizer = "Kadirli Belediyesi", IsFree = true, IsLocal = true,
                    Status = "approved", CreatedBy = admin.Id
                },
                new Event
                {
                    Title = "Karakucak Güreş Festivali", Description = "Geleneksel karakucak güreşleri ve yöresel etkinlikler.",
                    CategoryId = EvCatId("Festival"), EventDate = DateTime.SpecifyKind(now.Date.AddDays(20), DateTimeKind.Utc), EventTime = new TimeSpan(10, 0, 0),
                    VenueName = "Şehir Stadyumu", Organizer = "Kadirli Kaymakamlığı", IsFree = true, IsLocal = true,
                    Status = "approved", CreatedBy = admin.Id
                },
                new Event
                {
                    Title = "Amatör Tiyatro Gecesi", Description = "Yerel tiyatro topluluğundan tek perdelik oyun.",
                    CategoryId = EvCatId("Tiyatro"), EventDate = DateTime.SpecifyKind(now.Date.AddDays(5), DateTimeKind.Utc), EventTime = new TimeSpan(19, 0, 0),
                    VenueName = "Kültür Merkezi", Organizer = "Kadirli Sanat Derneği", TicketPrice = 50, IsFree = false, IsLocal = true,
                    Status = "pending", CreatedBy = user1.Id
                }
            );
            await db.SaveChangesAsync();
        }

        // --- Mekanlar ---
        if (!await db.Places.AnyAsync())
        {
            var placeCats = await db.PlaceCategories.OrderBy(c => c.DisplayOrder).ToListAsync();
            Guid PlaceCatId(string name) => placeCats.FirstOrDefault(c => c.Name == name)?.Id ?? placeCats.First().Id;

            db.Places.AddRange(
                new Place { Name = "Savrun Kanyonu", Description = "Doğa yürüyüşü ve piknik için ideal kanyon.", CategoryId = PlaceCatId("Doğa & Yayla"), Latitude = 37.4520m, Longitude = 36.0410m, IsFree = true, BestSeason = "İlkbahar-Yaz", DistanceFromCenter = 12.5m, CreatedBy = admin.Id },
                new Place { Name = "Ala Cami", Description = "Roma dönemi temelli, tarihi taş cami.", CategoryId = PlaceCatId("Tarihi Yerler"), Latitude = 37.3735m, Longitude = 36.0961m, IsFree = true, Address = "Merkez", DistanceFromCenter = 0.5m, CreatedBy = admin.Id },
                new Place { Name = "Maksutoğlu Yaylası", Description = "Serin havası ve çam ormanlarıyla yayla turizmi.", CategoryId = PlaceCatId("Doğa & Yayla"), Latitude = 37.5810m, Longitude = 36.2120m, IsFree = true, BestSeason = "Yaz", DistanceFromCenter = 38m, CreatedBy = admin.Id }
            );
            await db.SaveChangesAsync();
        }

        // --- Taksiciler (1'i doğrulanmamış) ---
        if (!await db.TaxiDrivers.AnyAsync())
        {
            db.TaxiDrivers.AddRange(
                new TaxiDriver { Name = "Osman Kılıç", Phone = "+905331230001", Plaka = "80 T 0101", VehicleInfo = "Fiat Egea, Sarı, 2022", IsVerified = true, VerifiedBy = admin.Id, VerifiedAt = now, IsActive = true },
                new TaxiDriver { Name = "Ramazan Özdemir", Phone = "+905331230002", Plaka = "80 T 0202", VehicleInfo = "Toyota Corolla, Sarı, 2021", IsVerified = true, VerifiedBy = admin.Id, VerifiedAt = now, IsActive = true },
                new TaxiDriver { Name = "İsmail Aydın", Phone = "+905331230003", Plaka = "80 T 0303", VehicleInfo = "Renault Symbol, Sarı, 2019", IsVerified = false, IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        // --- Şikayetler ---
        if (!await db.Complaints.AnyAsync())
        {
            db.Complaints.AddRange(
                new Complaint { UserId = user1.Id, Type = "content", RelatedModule = "ads", Subject = "Yanıltıcı ilan", Message = "İlandaki araç fotoğrafları başka bir araca ait görünüyor.", Status = "pending" },
                new Complaint { UserId = user2.Id, Type = "app", Subject = "Bildirim gelmiyor", Message = "Duyuru bildirimleri açık olmasına rağmen telefonuma bildirim düşmüyor.", Status = "pending" }
            );
            await db.SaveChangesAsync();
        }

        // --- Şehir Rehberi kayıtları ---
        if (!await db.GuideItems.AnyAsync())
        {
            var guideCats = await db.GuideCategories.ToListAsync();
            Guid GuideCatId(string namePart) =>
                (guideCats.FirstOrDefault(c => c.Name.Contains(namePart)) ?? guideCats.First()).Id;

            if (guideCats.Any())
            {
                db.GuideItems.AddRange(
                    new GuideItem { Name = "Kadirli Devlet Hastanesi", CategoryId = GuideCatId("Sağlık"), Phone = "0328 718 10 26", Address = "Şehit Sedat Nezih Özok Cad.", WorkingHours = "7/24 Açık", Latitude = 37.3689m, Longitude = 36.1042m, IsActive = true },
                    new GuideItem { Name = "Kadirli Belediyesi", CategoryId = GuideCatId("Resmi"), Phone = "0328 718 10 40", Address = "Cumhuriyet Mah. Merkez", WorkingHours = "08:00 - 17:00", Latitude = 37.3708m, Longitude = 36.0961m, IsActive = true },
                    new GuideItem { Name = "Kadirli Kaymakamlığı", CategoryId = GuideCatId("Resmi"), Phone = "0328 718 10 01", Address = "Hükümet Konağı", WorkingHours = "08:00 - 17:00", IsActive = true },
                    new GuideItem { Name = "İtfaiye", CategoryId = GuideCatId("Acil"), Phone = "110", WorkingHours = "7/24 Açık", IsActive = true },
                    new GuideItem { Name = "Elektrik Arıza (Toroslar EDAŞ)", CategoryId = GuideCatId("Acil"), Phone = "186", WorkingHours = "7/24 Açık", IsActive = true }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
