using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AdminPermissionConfiguration : IEntityTypeConfiguration<AdminPermission>
{
    public void Configure(EntityTypeBuilder<AdminPermission> b)
    {
        b.ToTable("admin_permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Module).HasMaxLength(50).IsRequired();
        
        b.HasOne(x => x.User)
         .WithMany(u => u.AdminPermissions)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
         
        b.HasIndex(x => x.UserId);
    }
}
