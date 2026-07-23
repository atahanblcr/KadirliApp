using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class IntracityRouteConfiguration : IEntityTypeConfiguration<IntracityRoute>
{
    public void Configure(EntityTypeBuilder<IntracityRoute> builder)
    {
        builder.ToTable("intracity_routes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.RouteNumber).IsRequired();
        builder.Property(x => x.RouteName).IsRequired();
    }
}
