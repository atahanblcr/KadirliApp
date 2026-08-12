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

        // 🔴 Faz 12.15 — "aynı haber ikinci kez gönderilemez" kuralının SON kapısı.
        //
        // Kural üç katmanda yaşıyor ve üçü de gerekli: panel butonu (görünüm), komutun
        // `NotificationSent` denetimi (sunucu) ve bu indeks (veritabanı). İlk ikisi bir
        // YARIŞI yakalayamaz: gönderim ile işaretleme aynı `SaveChanges` içinde değil
        // (kampanya kimliği ancak dispatcher yazdıktan sonra doğuyor), yani iki eşzamanlı
        // istek ikisi de "gönderilmemiş" görüp **şehre iki push** atabilirdi.
        //
        // ⚠️ Kapsam bilerek DAR (`source = 'news'`): duyuru/kesinti kampanyalarında aynı
        // kaynağa ikinci bir gönderim MEŞRU (yeniden gönderim yeni kampanya açar, §7 madde
        // 37) — genel bir unique indeks o yolu sessizce kapatırdı.
        // ⚠️ 12.13'ün "koruma ile kurtarma birlikte yazılır" dersi burada GEREKMİYOR ve
        // sebebi önemli: senkron kilidinin aksine bu satır **terminal** — yarıda kalmış bir
        // durumu yok, dolayısıyla "takılmış kaydı kurtar" adımı da yok.
        //
        // 🐛 <b>ADLI aşırı yükleme şart.</b> İlk yazımda `HasIndex(x => new { Source, SourceId })`
        // ikinci kez çağrıldı ve EF ikisini **aynı indeks** sayıp üsttekini SESSİZCE ezdi:
        // üretilen migration, duyuru idempotency'sinin dayandığı `ix_push_campaigns_source_
        // source_id`'yi **DROP** ediyordu. Ne derleyici ne test söylerdi — yalnız
        // `AnnouncementNotificationGenerator`ın "bu duyurunun kampanyası var mı?" sorgusu
        // büyüyen bir tabloyu tam taramaya başlardı. (Üretilen SQL'i okuma kuralı, checklist §6.)
        // ⚠️ İkinci ad (`HasDatabaseName`) de şart: adlı aşırı yüklemenin ilk parametresi
        // MODEL adıdır; snake_case adlandırma eklentisi veritabanı adını yine kendisi türetir
        // ve `…_source_source_id1` gibi bir ad bırakır.
        b.HasIndex(new[] { nameof(PushCampaign.Source), nameof(PushCampaign.SourceId) },
                   "NewsSourceUnique")
         .IsUnique()
         .HasFilter("source = 'news' AND source_id IS NOT NULL")
         .HasDatabaseName("ix_push_campaigns_news_source_id_unique");
    }
}
