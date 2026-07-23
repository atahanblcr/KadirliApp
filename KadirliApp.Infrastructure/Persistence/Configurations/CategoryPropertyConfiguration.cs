using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class CategoryPropertyConfiguration : IEntityTypeConfiguration<CategoryProperty>
{
    public void Configure(EntityTypeBuilder<CategoryProperty> builder)
    {
        builder.ToTable("category_properties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.PropertyName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PropertyType).HasConversion<string>();

        builder.HasIndex(x => new { x.CategoryId, x.PropertyName }).IsUnique();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Properties)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
