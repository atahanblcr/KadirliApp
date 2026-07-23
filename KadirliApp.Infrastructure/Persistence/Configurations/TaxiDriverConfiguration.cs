using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class TaxiDriverConfiguration : IEntityTypeConfiguration<TaxiDriver>
{
    public void Configure(EntityTypeBuilder<TaxiDriver> builder)
    {
        builder.ToTable("taxi_drivers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Phone).IsRequired();
        
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.LicenseFile)
               .WithMany()
               .HasForeignKey(x => x.LicenseFileId)
               .OnDelete(DeleteBehavior.SetNull);
               
        builder.HasOne(x => x.RegistrationFile)
               .WithMany()
               .HasForeignKey(x => x.RegistrationFileId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
