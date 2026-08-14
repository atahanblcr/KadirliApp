using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

/// <summary>Faz 12.16 — <c>legal_document_versions</c> (metnin belirli bir hâli).</summary>
public class LegalDocumentVersionConfiguration : IEntityTypeConfiguration<LegalDocumentVersion>
{
    public void Configure(EntityTypeBuilder<LegalDocumentVersion> b)
    {
        b.ToTable("legal_document_versions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        // Metin tavanlanmıyor: hukuki bir metnin uzunluğu bizim varsayımımız olamaz
        // (§7 madde 54'ün "kolon tavanı" dersi — dar tutmak bir gün tek bir kaydı düşürür).
        b.Property(x => x.Body).HasColumnType("text").IsRequired();
        b.Property(x => x.Summary).HasMaxLength(500);

        // Belge içinde sürüm numarası benzersiz — "v3" iki farklı metni gösteremez.
        b.HasIndex(x => new { x.DocumentId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ix_legal_document_versions_document_id_version_number");

        // 🔴 AYNI ANDA EN FAZLA BİR YAYINDA SÜRÜM (§7 madde 72'nin ikinci ayağı).
        //
        // Kural neden kodda DEĞİL burada: yayınlama iki adımdır (eskiyi `Supersede`, yeniyi
        // `Publish`) ve iki eşzamanlı istek ikisini de "eski yayında sürüm yok" hâlinde
        // görebilir. Kod sırası doğru olsa bile yarışı yakalayamaz; veritabanı yakalar
        // (12.15'in `push_campaigns` kısmi indeksiyle birebir aynı gerekçe).
        //
        // İki yayında sürüm doğsaydı belirti sinsi olurdu: kayıt ekranı hangisini
        // soracağını bilemez, iki kullanıcı FARKLI metne rıza verir ve ikisi de "yayında"
        // görünürdü — hiçbir hata, hiçbir log.
        //
        // ⚠️ Kolon kümesi yukarıdakinden farklı, yani EF'in "aynı indeks" tuzağına
        // (§7 madde 64, 12.15 bulgusu) düşmez; yine de ikisi de ADLANDIRILDI.
        b.HasIndex(x => x.DocumentId)
            .IsUnique()
            .HasFilter("published_at IS NOT NULL AND superseded_at IS NULL")
            .HasDatabaseName("ix_legal_document_versions_one_live_per_document");
    }
}
