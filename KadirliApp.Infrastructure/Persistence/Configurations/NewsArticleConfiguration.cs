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

        // Aramanın çıpası: 27k kayıtta indekssiz `LIKE` her tuş vuruşunda tam tarama.
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
