using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.DiscountPercentage).HasPrecision(5, 2);
        builder.Property(x => x.MinimumAmount).HasPrecision(12, 2);
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Business)
               .WithMany(x => x.Campaigns)
               .HasForeignKey(x => x.BusinessId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(x => x.CoverImage)
               .WithMany()
               .HasForeignKey(x => x.CoverImageId)
               .OnDelete(DeleteBehavior.SetNull);
               
        builder.HasMany(x => x.Images)
               .WithOne(x => x.Campaign)
               .HasForeignKey(x => x.CampaignId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasMany(x => x.CodeViews)
               .WithOne(x => x.Campaign)
               .HasForeignKey(x => x.CampaignId)
               .OnDelete(DeleteBehavior.Cascade);

        // Index from requirements
        // ix_campaigns_status_dates ON campaigns (status, start_date, end_date) WHERE deleted_at IS NULL;
        builder.HasIndex(x => new { x.Status, x.StartDate, x.EndDate }); // Filter will be applied in SQL via partial index, we can just declare the index here. EF Core 8 supports filter.
        // Actually, we can add filter like: builder.HasIndex(...).HasFilter("deleted_at IS NULL");
        builder.HasIndex(x => new { x.Status, x.StartDate, x.EndDate }).HasFilter("deleted_at IS NULL").HasDatabaseName("ix_campaigns_status_dates");
        
        // ix_campaigns_business ON campaigns (business_id);
        builder.HasIndex(x => x.BusinessId).HasDatabaseName("ix_campaigns_business");
    }
}
