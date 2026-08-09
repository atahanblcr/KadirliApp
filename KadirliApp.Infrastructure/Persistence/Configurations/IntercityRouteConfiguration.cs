using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class IntercityRouteConfiguration : IEntityTypeConfiguration<IntercityRoute>
{
    public void Configure(EntityTypeBuilder<IntercityRoute> builder)
    {
        builder.ToTable("intercity_routes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Destination).IsRequired();
        builder.Property(x => x.Price).HasPrecision(12, 2);

        // Faz 12.5: araç tipi METİN olarak durur (enum sırası değil) — bkz. TransportVehicleTypes.
        // Varsayılan DB tarafında da yazılı: 12.5 öncesi satırlar ve dışarıdan INSERT edilen
        // kayıtlar "tipi olmayan hat" hâline düşmesin.
        builder.Property(x => x.VehicleType)
               .HasMaxLength(20)
               .IsRequired()
               .HasDefaultValue(TransportVehicleTypes.Default);

        builder.HasOne(x => x.DeparturePoint)
               .WithMany(x => x.Routes)
               .HasForeignKey(x => x.DeparturePointId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.VehicleType).HasDatabaseName("ix_intercity_routes_vehicle_type");
        builder.HasIndex(x => x.DeparturePointId).HasDatabaseName("ix_intercity_routes_departure_point");
    }
}
