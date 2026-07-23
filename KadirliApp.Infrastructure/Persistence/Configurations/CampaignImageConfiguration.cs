using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class CampaignImageConfiguration : IEntityTypeConfiguration<CampaignImage>
{
    public void Configure(EntityTypeBuilder<CampaignImage> builder)
    {
        builder.ToTable("campaign_images");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.HasOne(x => x.File)
               .WithMany()
               .HasForeignKey(x => x.FileId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
