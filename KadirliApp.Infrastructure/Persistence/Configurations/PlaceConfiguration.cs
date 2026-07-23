using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("places");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Amenities).HasColumnType("jsonb");
        builder.Property(x => x.Latitude).IsRequired();
        builder.Property(x => x.Longitude).IsRequired();

        builder.HasOne(x => x.Category)
               .WithMany()
               .HasForeignKey(x => x.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CoverImage)
               .WithMany()
               .HasForeignKey(x => x.CoverImageId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Creator)
               .WithMany()
               .HasForeignKey(x => x.CreatedBy)
               .OnDelete(DeleteBehavior.SetNull);
               
        builder.HasMany(x => x.Images)
               .WithOne(x => x.Place)
               .HasForeignKey(x => x.PlaceId)
               .OnDelete(DeleteBehavior.Cascade);

        // ix_places_category ON places (category_id);
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_places_category");
    }
}
