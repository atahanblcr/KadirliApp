using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Role).HasConversion<string>().HasColumnType("varchar(20)");
        
        b.HasIndex(x => new { x.Role, x.PermissionId }).IsUnique();
        
        b.HasOne(x => x.Permission)
         .WithMany()
         .HasForeignKey(x => x.PermissionId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
