using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AdExtensionConfiguration : IEntityTypeConfiguration<AdExtension>
{
    public void Configure(EntityTypeBuilder<AdExtension> builder)
    {
        builder.ToTable("ad_extensions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(x => x.Ad)
            .WithMany(x => x.Extensions)
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
