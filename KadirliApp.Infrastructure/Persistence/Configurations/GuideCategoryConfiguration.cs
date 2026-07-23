using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class GuideCategoryConfiguration : IEntityTypeConfiguration<GuideCategory>
{
    public void Configure(EntityTypeBuilder<GuideCategory> builder)
    {
        builder.ToTable("guide_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Slug).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne(x => x.Parent)
               .WithMany(x => x.SubCategories)
               .HasForeignKey(x => x.ParentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
