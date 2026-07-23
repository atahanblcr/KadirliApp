using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Phone).HasMaxLength(15).IsRequired();
        b.HasIndex(x => x.Phone).IsUnique();
        
        b.Property(x => x.Email).HasMaxLength(100);
        b.HasIndex(x => x.Email).IsUnique();
        
        b.Property(x => x.Username).HasMaxLength(50);
        b.HasIndex(x => x.Username).IsUnique();

        b.OwnsOne(x => x.NotificationPreferences, o => o.ToJson());

        b.Property(x => x.Role).HasConversion<string>().HasColumnType("varchar(20)");
        b.Property(x => x.Password).HasColumnName("password");
        
        b.HasOne(x => x.PrimaryNeighborhood)
         .WithMany()
         .HasForeignKey(x => x.PrimaryNeighborhoodId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasQueryFilter(x => x.DeletedAt == null);

        b.HasIndex(x => x.Role).HasFilter("deleted_at IS NULL");
        b.HasIndex(x => x.PrimaryNeighborhoodId);
    }
}
