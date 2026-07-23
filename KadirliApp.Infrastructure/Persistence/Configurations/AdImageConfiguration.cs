using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AdImageConfiguration : IEntityTypeConfiguration<AdImage>
{
    public void Configure(EntityTypeBuilder<AdImage> builder)
    {
        builder.ToTable("ad_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(x => new { x.AdId, x.FileId }).IsUnique();
        builder.HasIndex(x => x.AdId).HasDatabaseName("ix_ad_images_ad");

        builder.HasOne(x => x.Ad)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
