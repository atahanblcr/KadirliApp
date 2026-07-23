using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("ads");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.ContactPhone).HasMaxLength(15).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");
        builder.Property(x => x.MaxExtensions).HasDefaultValue(3);

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indices
        // builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasFilter("deleted_at IS NULL"); // Done in Migration usually but we can specify it
        // Wait, "CREATE INDEX ix_ads_status_created ON ads (status, created_at DESC) WHERE deleted_at IS NULL;"
        // We can just define normal indices here, the complex ones will be in migrations. But the instruction says:
        // "Apply the exact snake_case table names, column lengths, required fields, indices, and soft delete query filters as specified in the master document."
        builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("ix_ads_status_created").HasFilter("deleted_at IS NULL").IsDescending(false, true);
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("ix_ads_category").HasFilter("deleted_at IS NULL");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_ads_user").HasFilter("deleted_at IS NULL");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_ads_expires").HasFilter("status = 'approved'");
        builder.HasIndex(x => x.Price).HasDatabaseName("ix_ads_price").HasFilter("deleted_at IS NULL");
    }
}
