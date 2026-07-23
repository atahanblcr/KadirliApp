using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Subject).IsRequired();
        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Resolver)
               .WithMany()
               .HasForeignKey(x => x.ResolvedBy)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
