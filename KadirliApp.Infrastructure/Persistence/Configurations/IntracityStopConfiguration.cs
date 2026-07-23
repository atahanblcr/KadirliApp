using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class IntracityStopConfiguration : IEntityTypeConfiguration<IntracityStop>
{
    public void Configure(EntityTypeBuilder<IntracityStop> builder)
    {
        builder.ToTable("intracity_stops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.StopName).IsRequired();

        builder.HasOne(x => x.Route)
               .WithMany(x => x.Stops)
               .HasForeignKey(x => x.RouteId)
               .OnDelete(DeleteBehavior.Cascade);

        // ix_intracity_stops_route ON intracity_stops (route_id, stop_order);
        builder.HasIndex(x => new { x.RouteId, x.StopOrder }).HasDatabaseName("ix_intracity_stops_route");
    }
}
