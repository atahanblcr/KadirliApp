using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class GuideItemConfiguration : IEntityTypeConfiguration<GuideItem>
{
    public void Configure(EntityTypeBuilder<GuideItem> builder)
    {
        builder.ToTable("guide_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).IsRequired();
        
        builder.HasOne(x => x.Category)
               .WithMany()
               .HasForeignKey(x => x.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LogoFile)
               .WithMany()
               .HasForeignKey(x => x.LogoFileId)
               .OnDelete(DeleteBehavior.SetNull);

        // ix_guide_items_category ON guide_items (category_id);
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_guide_items_category");
    }
}
