using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

/// <summary>Faz 12.12 — <c>news_sync_runs</c> (koşu defteri).</summary>
public class NewsSyncRunConfiguration : IEntityTypeConfiguration<NewsSyncRun>
{
    public void Configure(EntityTypeBuilder<NewsSyncRun> b)
    {
        b.ToTable("news_sync_runs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Trigger).HasMaxLength(20).IsRequired();
        b.Property(x => x.Mode).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);

        // Panel "son koşular" listesi ve bayatlık kutusu bu sıradan gidiyor.
        b.HasIndex(x => x.StartedAt).IsDescending();
    }
}

/// <summary>Faz 12.12 — <c>news_sync_state</c> (tek satır: iki imleç).</summary>
public class NewsSyncStateConfiguration : IEntityTypeConfiguration<NewsSyncState>
{
    public void Configure(EntityTypeBuilder<NewsSyncState> b)
    {
        b.ToTable("news_sync_state");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
    }
}
