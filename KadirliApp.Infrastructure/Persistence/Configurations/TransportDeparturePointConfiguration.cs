using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class TransportDeparturePointConfiguration : IEntityTypeConfiguration<TransportDeparturePoint>
{
    public void Configure(EntityTypeBuilder<TransportDeparturePoint> b)
    {
        b.ToTable("transport_departure_points");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300);

        // Koordinat: mahalle/mezarlık/cami ile aynı hassasiyet (10.9(d) kararı).
        b.Property(x => x.Latitude).HasPrecision(10, 7);
        b.Property(x => x.Longitude).HasPrecision(10, 7);

        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.IsActive, x.DisplayOrder });
    }
}
