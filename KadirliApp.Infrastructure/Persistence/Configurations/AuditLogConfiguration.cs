using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        
        b.Property(x => x.Action).HasMaxLength(50).IsRequired();
        b.Property(x => x.Module).HasMaxLength(50).IsRequired();
        b.Property(x => x.AffectedType).HasMaxLength(50);
        b.Property(x => x.IpAddress).HasColumnType("inet");
        b.Property(x => x.Details).HasColumnType("jsonb");
        
        b.HasIndex(x => new { x.UserId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.Module, x.CreatedAt }).IsDescending(false, true);
    }
}
