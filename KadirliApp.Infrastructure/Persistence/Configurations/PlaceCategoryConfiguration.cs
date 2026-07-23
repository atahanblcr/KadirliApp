using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PlaceCategoryConfiguration : IEntityTypeConfiguration<PlaceCategory>
{
    public void Configure(EntityTypeBuilder<PlaceCategory> builder)
    {
        builder.ToTable("place_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
