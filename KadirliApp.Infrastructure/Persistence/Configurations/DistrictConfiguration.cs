using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class DistrictConfiguration : IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> b)
    {
        b.ToTable("districts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.Property(x => x.ProvinceName).HasMaxLength(100).IsRequired();

        // Slug il+ilçeden türediği için benzersizdir; her ilin "Merkez"i ayrı satır.
        b.HasIndex(x => x.Slug).IsUnique();

        // Panelin ve mobilin "il" gruplaması bu kolondan gider.
        b.HasIndex(x => new { x.ProvinceName, x.DisplayOrder });
    }
}
