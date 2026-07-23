using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PropertyOptionConfiguration : IEntityTypeConfiguration<PropertyOption>
{
    public void Configure(EntityTypeBuilder<PropertyOption> builder)
    {
        builder.ToTable("property_options");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OptionValue).HasMaxLength(200).IsRequired();

        builder.HasOne(x => x.Property)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
