using KadirliApp.Domain.Entities;
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
    }
}
