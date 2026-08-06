using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class PushCampaignConfiguration : IEntityTypeConfiguration<PushCampaign>
{
    public void Configure(EntityTypeBuilder<PushCampaign> b)
    {
        b.ToTable("push_campaigns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).IsRequired();               // text — tavan komutta (500)
        b.Property(x => x.TargetType).HasMaxLength(20).IsRequired();
        b.Property(x => x.TargetNeighborhoods).HasColumnType("jsonb");
        b.Property(x => x.Source).HasMaxLength(20).IsRequired();

        // ⚠️ users'a FK YOK (CreatedBy): personel hesabı silinse bile gönderim tarihçesi
        // kalmalı. AuditLog ve LoginAttempt'teki aynı karar — "kim yolladı" bilgisi tam da
        // en çok gerektiğinde isimsizleşmemeli.

        // Panelin varsayılan sırası: en yeni gönderim önce.
        b.HasIndex(x => x.CreatedAt).IsDescending();
        // "Bu duyurunun kampanyası hangisi?" — duyuru ekranından gönderim panosuna geçiş
        // ve idempotency kontrolü aynı indeksten okur.
        b.HasIndex(x => new { x.Source, x.SourceId });
        // Panonun "tamamlanmamış kampanyalar" süzgeci + job'ın tamamlama kontrolü.
        b.HasIndex(x => x.CompletedAt);
    }
}
