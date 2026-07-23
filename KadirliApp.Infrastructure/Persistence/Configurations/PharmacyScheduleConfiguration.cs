using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PharmacyScheduleConfiguration : IEntityTypeConfiguration<PharmacySchedule>
{
    public void Configure(EntityTypeBuilder<PharmacySchedule> builder)
    {
        builder.ToTable("pharmacy_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(x => x.Pharmacy)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.PharmacyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DutyDate).HasDatabaseName("ix_pharm_sched_date");
    }
}
