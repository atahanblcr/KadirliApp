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
        b.Property(x => x.Source).HasMaxLength(100);
        
        b.HasOne(x => x.Announcement)
         .WithMany()
         .HasForeignKey(x => x.AnnouncementId)
         .OnDelete(DeleteBehavior.SetNull);
    }
}
