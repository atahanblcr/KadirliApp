using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AdFavoriteConfiguration : IEntityTypeConfiguration<AdFavorite>
{
    public void Configure(EntityTypeBuilder<AdFavorite> builder)
    {
        builder.ToTable("ad_favorites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasIndex(x => new { x.UserId, x.AdId }).IsUnique();
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_ad_favorites_user");

        builder.HasOne(x => x.Ad)
            .WithMany(x => x.Favorites)
            .HasForeignKey(x => x.AdId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
