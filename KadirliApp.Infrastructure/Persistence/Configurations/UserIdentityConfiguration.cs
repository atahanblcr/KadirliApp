using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

/// <summary>Faz 12.7 — <c>user_identities</c> (sosyal giriş bağlantıları).</summary>
public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> b)
    {
        b.ToTable("user_identities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Provider).HasMaxLength(20).IsRequired();

        // 255: Apple'ın `sub`'ı 44 karakter, Google'ınki 21 — ama uzunluk sağlayıcının
        // sözleşmesi değil, bizim varsayımımız olurdu. Geniş tutmak bedava; dar tutmak
        // ileride tek bir kaydın partisini düşürür (§7 madde 54'ün kolon tavanı dersi).
        b.Property(x => x.ProviderUserId).HasMaxLength(255).IsRequired();

        b.Property(x => x.Email).HasMaxLength(255);
        b.Property(x => x.DisplayName).HasMaxLength(150);

        // 🔴 Aynı sosyal hesap iki KadirliApp hesabına bağlanamaz (bkz. UserIdentity notları).
        b.HasIndex(x => new { x.Provider, x.ProviderUserId })
            .IsUnique()
            .HasDatabaseName("ix_user_identities_provider_provider_user_id");

        // 🔴 Bir kullanıcı bir sağlayıcıdan YALNIZ BİR hesap bağlayabilir — ve bu kural
        // rastgele değil, <b>ucun şeklinin dayattığı</b> bir kural:
        // `DELETE /v1/users/me/identities/{provider}` bağlantıyı SAĞLAYICI ADIYLA adresliyor.
        // İki Google bağlantısına izin verilseydi o uç "hangisini?" sorusunu cevaplayamaz,
        // sessizce birini silerdi. Kural burada, veritabanında yaşıyor: kodda unutulsa bile
        // ikinci satır INSERT'te reddedilir.
        // ⚠️ Kolon kümesi yukarıdakinden FARKLI olduğu için EF'in "aynı indeks" tuzağına
        // (§7 madde 64, 12.15 bulgusu) düşmez; yine de ikisi de adlandırıldı.
        b.HasIndex(x => new { x.UserId, x.Provider })
            .IsUnique()
            .HasDatabaseName("ix_user_identities_user_id_provider");

        b.HasOne(x => x.User)
            .WithMany(u => u.Identities)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
