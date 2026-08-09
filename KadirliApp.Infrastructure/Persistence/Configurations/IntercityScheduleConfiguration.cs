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

        // Faz 12.5: 7 bitlik gün maskesi. 🔴 DB varsayılanı 127 ("her gün") — 12.5 öncesindeki
        // örtük varsayımın ta kendisi, yani göç eden satırların davranışı değişmiyor.
        builder.Property(x => x.OperatingDays)
               .IsRequired()
               .HasDefaultValue(Domain.Enums.OperatingDays.Daily);

        builder.HasOne(x => x.Route)
               .WithMany(x => x.Schedules)
               .HasForeignKey(x => x.RouteId)
               .OnDelete(DeleteBehavior.Cascade);

        // ix_intercity_sched_route ON intercity_schedules (route_id);
        builder.HasIndex(x => x.RouteId).HasDatabaseName("ix_intercity_sched_route");
    }
}
