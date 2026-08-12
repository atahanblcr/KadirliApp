using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

/// <summary>Faz 12.12 — <c>news_articles</c>.</summary>
public class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> b)
    {
        b.ToTable("news_articles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        // 🔑 Eşleştirmenin tek anahtarı. Unique indeks aynı zamanda iki koşunun (artımlı +
        // arşiv) yarışını da yakalar: aynı haberi aynı anda iki kez eklemek FK/unique
        // ihlaliyle patlar, sessizce mükerrer satır üretmez.
        b.HasIndex(x => x.WpId).IsUnique();

        b.Property(x => x.SourceTitle).HasMaxLength(500).IsRequired();
        b.Property(x => x.SourceExcerpt).HasMaxLength(1000);
        b.Property(x => x.SourceContentHtml).IsRequired();
        b.Property(x => x.SourcePlainText).IsRequired();
        b.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.SourceImageUrl).HasMaxLength(1000);
        b.Property(x => x.SourceChecksum).HasMaxLength(64).IsRequired();
        b.Property(x => x.SourceState).HasMaxLength(20).IsRequired();

        b.Property(x => x.TitleOverride).HasMaxLength(500);
        b.Property(x => x.ExcerptOverride).HasMaxLength(1000);
        b.Property(x => x.ArchivedReason).HasMaxLength(500);

        // Liste varsayılan sıralaması (yayın anı, tersten) — 27k kayıtta indekssiz her
        // sayfalama tam tarama demek.
        b.HasIndex(x => x.SourcePublishedAt).IsDescending();

        // Public görünürlük süzgecinin iki kolonu (NewsVisibility).
        b.HasIndex(x => new { x.IsArchived, x.SourceState });

        // Artımlı senkronun "en yeni değişiklik" sorgusu.
        b.HasIndex(x => x.SourceModifiedAt).IsDescending();

        // ⚠️ Bu btree indeksi **aramanın çıpası DEĞİL** — 12.12'de öyle yazılmıştı ve yorum
        // yanlıştı (12.12 sonrası denetim, bulgu 4): `lower(kolon) LIKE '%x%'` bir btree
        // indeksini kullanamaz (sorgunun `LIKE` üretmesi bunu değiştirmiyor; ölçüldü).
        // Buradaki indeksin gerçek işi panelin `title_asc` sıralaması.
        // Aramanın çıpası `AddNewsSearchIndexes` migration'ındaki üç **GIN/trigram ifade
        // indeksi**dir (`lower(source_title)` · `lower(title_override)` · `lower(source_plain_text)`);
        // EF `gin_trgm_ops` ifade indeksini modelleyemediği için ham SQL ile yazılırlar.
        b.HasIndex(x => x.SourceTitle);

        // Görsel aynasının tekilleştirme sorgusu (aynı URL başka haberde var mı?).
        b.HasIndex(x => x.SourceImageUrl);

        b.HasOne(x => x.SourceImage)
         .WithMany()
         .HasForeignKey(x => x.SourceImageFileId)
         // SetNull bilinçli: dosya silinirse haber kaybolmaz, yalnız görselsiz kalır.
         .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.CoverImageOverrideFile)
         .WithMany()
         .HasForeignKey(x => x.CoverImageFileIdOverride)
         .OnDelete(DeleteBehavior.SetNull);

        // Faz 12.15 — bildirimi açan kampanya. Gezinme özelliği YOK (panel yalnız kimliği
        // kullanıp bağlantı çiziyor); FK yine de kurulu ki kampanya kimliği uydurulamasın.
        // ⚠️ SetNull bilinçli: kampanya bir gün temizlenirse haber kaybolmaz. `sent_at`
        // damgası ayrı bir kolonda durduğu için "gönderildi mi?" sorusunun cevabı FK'ya
        // bağlı değil — bağlı olsaydı kampanyanın silinmesi haberi sessizce
        // "hiç gönderilmemiş" hâline döndürür ve panel ikinci bir push teklif ederdi.
        b.HasOne<PushCampaign>()
         .WithMany()
         .HasForeignKey(x => x.NotificationCampaignId)
         .OnDelete(DeleteBehavior.SetNull);

        // Koleksiyon salt-okunur bir özellik üzerinden veriliyor; EF alanı kullanmalı.
        b.Navigation(x => x.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.HasMany(x => x.Categories)
         .WithMany()
         .UsingEntity<Dictionary<string, object>>(
             "news_article_categories",
             j => j.HasOne<NewsCategory>().WithMany().HasForeignKey("news_category_id").OnDelete(DeleteBehavior.Cascade),
             j => j.HasOne<NewsArticle>().WithMany().HasForeignKey("news_article_id").OnDelete(DeleteBehavior.Cascade),
             j =>
             {
                 j.ToTable("news_article_categories");
                 j.HasKey("news_article_id", "news_category_id");
                 // Kategori bazlı liste süzgeci bu yönden gidiyor.
                 j.HasIndex("news_category_id");
             });
    }
}
