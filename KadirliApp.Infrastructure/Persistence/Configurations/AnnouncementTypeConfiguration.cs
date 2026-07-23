using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AnnouncementTypeConfiguration : IEntityTypeConfiguration<AnnouncementType>
{
    public void Configure(EntityTypeBuilder<AnnouncementType> b)
    {
        b.ToTable("announcement_types");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(50);
        b.Property(x => x.Color).HasMaxLength(20);
        
        b.HasIndex(x => x.Name).IsUnique();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
