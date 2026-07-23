using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AdPropertyValueConfiguration : IEntityTypeConfiguration<AdPropertyValue>
{
    public void Configure(EntityTypeBuilder<AdPropertyValue> builder)
    {
        builder.ToTable("ad_property_values");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(x => new { x.AdId, x.PropertyId }).IsUnique();
        builder.HasIndex(x => x.AdId).HasDatabaseName("ix_ad_prop_values_ad");

        builder.HasOne(x => x.Ad)
            .WithMany(x => x.PropertyValues)
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
