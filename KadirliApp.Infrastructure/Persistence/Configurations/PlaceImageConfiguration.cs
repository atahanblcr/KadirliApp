using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PlaceImageConfiguration : IEntityTypeConfiguration<PlaceImage>
{
    public void Configure(EntityTypeBuilder<PlaceImage> builder)
    {
        builder.ToTable("place_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(x => x.File)
               .WithMany()
               .HasForeignKey(x => x.FileId)
               .OnDelete(DeleteBehavior.Cascade);

        // ix_place_images_place ON place_images (place_id);
        builder.HasIndex(x => x.PlaceId).HasDatabaseName("ix_place_images_place");
    }
}
