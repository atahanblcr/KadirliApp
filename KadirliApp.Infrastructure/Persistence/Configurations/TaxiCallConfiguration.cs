using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class TaxiCallConfiguration : IEntityTypeConfiguration<TaxiCall>
{
    public void Configure(EntityTypeBuilder<TaxiCall> builder)
    {
        builder.ToTable("taxi_calls");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(x => x.Passenger)
               .WithMany()
               .HasForeignKey(x => x.PassengerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Driver)
               .WithMany()
               .HasForeignKey(x => x.DriverId)
               .OnDelete(DeleteBehavior.Restrict);

        // ix_taxi_calls_driver ON taxi_calls (driver_id);
        builder.HasIndex(x => x.DriverId).HasDatabaseName("ix_taxi_calls_driver");
    }
}
