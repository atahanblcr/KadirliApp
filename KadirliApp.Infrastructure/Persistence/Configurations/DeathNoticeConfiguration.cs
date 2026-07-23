using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class DeathNoticeConfiguration : IEntityTypeConfiguration<DeathNotice>
{
    public void Configure(EntityTypeBuilder<DeathNotice> builder)
    {
        builder.ToTable("death_notices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.DeceasedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(x => x.CondolenceLatitude).HasColumnType("numeric(10,7)");
        builder.Property(x => x.CondolenceLongitude).HasColumnType("numeric(10,7)");

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Cemetery)
            .WithMany()
            .HasForeignKey(x => x.CemeteryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Mosque)
            .WithMany()
            .HasForeignKey(x => x.MosqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Status, x.FuneralDate }).HasDatabaseName("ix_deaths_status_funeral").HasFilter("deleted_at IS NULL").IsDescending(false, true);
        builder.HasIndex(x => x.AutoArchiveAt).HasDatabaseName("ix_deaths_archive").HasFilter("status = 'approved'");
        builder.HasIndex(x => x.NeighborhoodId).HasDatabaseName("ix_deaths_neighborhood");
    }
}
