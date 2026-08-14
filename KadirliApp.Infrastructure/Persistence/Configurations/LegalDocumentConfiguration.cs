using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

/// <summary>Faz 12.16 — <c>legal_documents</c> (hukuki belgenin kimliği; metin sürümlerde).</summary>
public class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> b)
    {
        b.ToTable("legal_documents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Type).HasMaxLength(40).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();

        // 🔴 Bir türden yalnız BİR belge: iki "açık rıza metni" olsaydı kayıt ekranı
        // hangisini soracağını bilemez, ikisini birden sorsa kullanıcı aynı şeyi iki kez
        // onaylardı. Kural kodda değil BURADA yaşasın ki bir migration/seed hatası da
        // INSERT'te reddedilsin.
        b.HasIndex(x => x.Type).IsUnique().HasDatabaseName("ix_legal_documents_type");

        b.HasMany(x => x.Versions)
            .WithOne(v => v.Document)
            .HasForeignKey(v => v.DocumentId)
            // ⚠️ Cascade DEĞİL: belge silinemez (silme yok, yalnız `IsActive`) ve bir gün
            // silinmeye kalkılsa bile sürümler + onlara bağlı rıza kayıtları KANITTIR
            // (§7 madde 74). Restrict, o silmeyi sessiz bir veri kaybı yerine BİR HATAYA çevirir.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
