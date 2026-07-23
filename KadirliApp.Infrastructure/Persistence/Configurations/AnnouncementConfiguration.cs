using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> b)
    {
        b.ToTable("announcements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("draft");
        b.Property(x => x.TargetNeighborhoods).HasColumnType("jsonb");
        b.Property(x => x.TargetUserIds).HasColumnType("jsonb");
        
        b.Property(x => x.LocationName).HasMaxLength(300);
        b.Property(x => x.Latitude).HasColumnType("numeric(10,7)");
        b.Property(x => x.Longitude).HasColumnType("numeric(10,7)");

        b.HasOne(x => x.Type)
         .WithMany()
         .HasForeignKey(x => x.TypeId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ImageFile)
         .WithMany()
         .HasForeignKey(x => x.ImageFileId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => x.DeletedAt == null);

        b.HasIndex(x => new { x.Status, x.CreatedAt }).IsDescending(false, true).HasFilter("deleted_at IS NULL");
        b.HasIndex(x => x.TypeId);
        b.HasIndex(x => x.ScheduledFor).HasFilter("status = 'scheduled'");
    }
}
