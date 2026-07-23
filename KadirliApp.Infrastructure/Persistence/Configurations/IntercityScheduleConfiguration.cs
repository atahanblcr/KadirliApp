using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class IntercityScheduleConfiguration : IEntityTypeConfiguration<IntercitySchedule>
{
    public void Configure(EntityTypeBuilder<IntercitySchedule> builder)
    {
        builder.ToTable("intercity_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(x => x.Route)
               .WithMany(x => x.Schedules)
               .HasForeignKey(x => x.RouteId)
               .OnDelete(DeleteBehavior.Cascade);

        // ix_intercity_sched_route ON intercity_schedules (route_id);
        builder.HasIndex(x => x.RouteId).HasDatabaseName("ix_intercity_sched_route");
    }
}
