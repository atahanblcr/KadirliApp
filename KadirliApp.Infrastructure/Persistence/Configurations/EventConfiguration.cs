using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CoverImage)
            .WithMany()
            .HasForeignKey(x => x.CoverImageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Faz 12.4: ilçe sözlüğüne bağ. `SetNull` bilinçli (12.3'teki mahalle kararının aynısı):
        // bir ilçe sözlükten kalkarsa geçmiş etkinlik silinmez, yalnız konumsuz kalır.
        // Cascade olsaydı bir lookup temizliği etkinlik arşivini sessizce yok ederdi.
        builder.HasOne(x => x.District)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.Status, x.EventDate }).HasDatabaseName("ix_events_status_date").HasFilter("deleted_at IS NULL");
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_events_category");

        // Panelin ilçe süzgeci ve mobilin "Kadirli / Osmaniye / Çevre iller" şeridi buradan gider.
        builder.HasIndex(x => x.DistrictId).HasDatabaseName("ix_events_district");
    }
}
