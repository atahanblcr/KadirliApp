using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class MosqueConfiguration : IEntityTypeConfiguration<Mosque>
{
    public void Configure(EntityTypeBuilder<Mosque> builder)
    {
        builder.ToTable("mosques");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}
