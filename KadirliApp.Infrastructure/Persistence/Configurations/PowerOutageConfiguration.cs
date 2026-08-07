using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PowerOutageConfiguration : IEntityTypeConfiguration<PowerOutage>
{
    public void Configure(EntityTypeBuilder<PowerOutage> b)
    {
        b.ToTable("power_outages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Neighborhood).HasMaxLength(200);
        b.Property(x => x.AreaDetail).HasMaxLength(300);
        b.Property(x => x.Source).HasMaxLength(100);

        b.HasOne(x => x.Announcement)
         .WithMany()
         .HasForeignKey(x => x.AnnouncementId)
         .OnDelete(DeleteBehavior.SetNull);

        // Faz 12.3: mahalle sözlüğüne bağ. `SetNull` bilinçli — bir mahalle sözlükten
        // kalkarsa kesinti kaydı silinmez, yalnız hedeflenemez hâle gelir (ve panel onu
        // "mahallesi eşleşmemiş" şeridinde gösterir). Cascade olsaydı bir lookup temizliği
        // geçmiş kesinti kayıtlarını sessizce yok ederdi.
        b.HasOne(x => x.NeighborhoodRef)
         .WithMany()
         .HasForeignKey(x => x.NeighborhoodId)
         .OnDelete(DeleteBehavior.SetNull);

        // Mobil "sadece mahallem" ve panelin mahalle süzgeci bu kolondan gidiyor.
        b.HasIndex(x => x.NeighborhoodId);
    }
}
