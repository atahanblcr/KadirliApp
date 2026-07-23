using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<KadirliApp.Domain.Entities.File>
{
    public void Configure(EntityTypeBuilder<KadirliApp.Domain.Entities.File> builder)
    {
        builder.ToTable("files");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OriginalName).IsRequired();
        builder.Property(x => x.FileName).IsRequired();
        builder.Property(x => x.StoragePath).IsRequired();

        builder.HasIndex(x => x.FileName).IsUnique();

        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.Uploader)
               .WithMany()
               .HasForeignKey(x => x.UploadedBy)
               .OnDelete(DeleteBehavior.SetNull);
               
        // If PostgreSQL jsonb is supported, EF 8 handles string properties to jsonb mapping if specified.
        builder.Property(x => x.Metadata).HasColumnType("jsonb");
    }
}
