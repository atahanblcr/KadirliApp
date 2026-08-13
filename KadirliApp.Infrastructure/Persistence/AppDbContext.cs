using KadirliApp.Domain.Common;
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<AdCategory> AdCategories => Set<AdCategory>();
    public DbSet<AdExtension> AdExtensions => Set<AdExtension>();
    public DbSet<AdFavorite> AdFavorites => Set<AdFavorite>();
    public DbSet<AdImage> AdImages => Set<AdImage>();
    public DbSet<AdPropertyValue> AdPropertyValues => Set<AdPropertyValue>();
    public DbSet<AdminPermission> AdminPermissions => Set<AdminPermission>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementType> AnnouncementTypes => Set<AnnouncementType>();
    public DbSet<AnnouncementView> AnnouncementViews => Set<AnnouncementView>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    // Faz 12.1 — AuditLog'un tamamlayıcısı: denetim izi BAŞARILI yazma eylemlerini tutar,
    // bu tablo BAŞARISIZ olanı ("vatandaş ne gördü").
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    // Faz 12.2 — üçüncü kardeş: kimlik doğrulamanın sonucu ("kim girmeye çalıştı").
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessCategory> BusinessCategories => Set<BusinessCategory>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignCodeView> CampaignCodeViews => Set<CampaignCodeView>();
    public DbSet<CampaignImage> CampaignImages => Set<CampaignImage>();
    public DbSet<CategoryProperty> CategoryProperties => Set<CategoryProperty>();
    public DbSet<Cemetery> Cemeteries => Set<Cemetery>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<DeathNotice> DeathNotices => Set<DeathNotice>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();
    public DbSet<EventImage> EventImages => Set<EventImage>();
    public DbSet<KadirliApp.Domain.Entities.File> Files => Set<KadirliApp.Domain.Entities.File>();
    public DbSet<GuideCategory> GuideCategories => Set<GuideCategory>();
    public DbSet<GuideItem> GuideItems => Set<GuideItem>();
    public DbSet<IntercityRoute> IntercityRoutes => Set<IntercityRoute>();
    public DbSet<IntercitySchedule> IntercitySchedules => Set<IntercitySchedule>();
    public DbSet<IntracityRoute> IntracityRoutes => Set<IntracityRoute>();
    public DbSet<IntracityStop> IntracityStops => Set<IntracityStop>();
    public DbSet<TransportDeparturePoint> TransportDeparturePoints => Set<TransportDeparturePoint>();
    public DbSet<Mosque> Mosques => Set<Mosque>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    // Faz 12.12 — projedeki ilk DIŞ KAYNAK entegrasyonu (FCM/SMS dışında): haberler
    // WordPress'ten alınıp buraya iner. Mobil kaynağa asla bağlanmaz.
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<NewsSyncRun> NewsSyncRuns => Set<NewsSyncRun>();
    public DbSet<NewsSyncState> NewsSyncStates => Set<NewsSyncState>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();
    public DbSet<PharmacySchedule> PharmacySchedules => Set<PharmacySchedule>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceCategory> PlaceCategories => Set<PlaceCategory>();
    public DbSet<PlaceImage> PlaceImages => Set<PlaceImage>();
    public DbSet<PowerOutage> PowerOutages => Set<PowerOutage>();
    public DbSet<PropertyOption> PropertyOptions => Set<PropertyOption>();
    public DbSet<PushCampaign> PushCampaigns => Set<PushCampaign>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<TaxiCall> TaxiCalls => Set<TaxiCall>();
    public DbSet<TaxiDriver> TaxiDrivers => Set<TaxiDriver>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserNeighborhood> UserNeighborhoods => Set<UserNeighborhood>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added)   e.Entity.CreatedAt = DateTime.UtcNow;
            if (e.State is EntityState.Added or EntityState.Modified)
                e.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
