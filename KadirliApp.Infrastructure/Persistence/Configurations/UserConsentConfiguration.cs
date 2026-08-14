using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

/// <summary>Faz 12.16 — <c>user_consents</c> (kimin, hangi sürüme, ne zaman rıza verdiği).</summary>
public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> b)
    {
        b.ToTable("user_consents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Source).HasMaxLength(20).IsRequired();
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.IpAddress).HasColumnType("inet"); // LoginAttempt ile aynı tip (12.2)

        // Bir kullanıcı bir sürüme yalnız BİR karar verir; fikir değiştirmek satırı günceller
        // (`Grant`/`Revoke`), ikinci bir satır açmaz — yoksa "hangisi geçerli?" sorusunun
        // cevabı zamana bağlı bir tahmine dönerdi.
        b.HasIndex(x => new { x.UserId, x.DocumentVersionId })
            .IsUnique()
            .HasDatabaseName("ix_user_consents_user_id_document_version_id");

        // Panelin "kaç kişi onayladı" sayacı ve rıza defteri bu yönden okuyor.
        b.HasIndex(x => new { x.DocumentVersionId, x.Granted })
            .HasDatabaseName("ix_user_consents_document_version_id_granted");

        b.HasOne(x => x.User)
            .WithMany(u => u.Consents)
            .HasForeignKey(x => x.UserId)
            // 🔴 CASCADE DEĞİL — ve bu, 12.7'nin `user_identities` kararının BİLİNÇLİ TERSİ
            // (§7 madde 74). Hesap silme bu projede zaten fiziksel silme değil
            // ANONİMLEŞTİRMEDİR (10.8), yani satır anonim kullanıcıya bağlı kalır ve
            // işlemenin hukuki dayanağı korunur. Cascade yazılsaydı bugün hiçbir şey
            // olmazdı (silme soft) ama yarın biri gerçek bir DELETE yazdığında bütün
            // kanıt sessizce giderdi.
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.DocumentVersion)
            .WithMany()
            .HasForeignKey(x => x.DocumentVersionId)
            // Sürüm silinemez: ona bağlı rıza kayıtları onu işaret ediyor ve o metin
            // kanıtın kendisidir. Silme denemesi sessiz veri kaybı değil HATA olmalı.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
