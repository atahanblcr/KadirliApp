using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> b)
    {
        b.ToTable("login_attempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        // Maskeli değer kısadır; tavan yine de cömert — maskeleyici değişirse kolon
        // sessizce kırpmasın (Postgres varchar taşmasında HATA verir, kırpmaz: doğrusu bu).
        b.Property(x => x.Identifier).HasMaxLength(120).IsRequired();
        b.Property(x => x.FailureReason).HasMaxLength(30);
        b.Property(x => x.SuspicionRule).HasMaxLength(10);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.IpAddress).HasColumnType("inet");

        // ⚠️ users'a FK YOK ve bu bilinçli: hesap silinse bile giriş denemesi kalmalı.
        // FK + cascade olsaydı "hesabı silerek izini de silmek" mümkün olurdu; bu tablo
        // tam olarak onu engellemek için var. (AuditLog'daki aynı karar.)

        // R1/R4 — "bu hesapta son 15 dakikada ne oldu".
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        // R2 — "bu IP'den son 15 dakikada ne oldu".
        b.HasIndex(x => new { x.IpAddress, x.CreatedAt });
        // Panelin "yalnız şüpheli" süzgeci + SecurityAlertJob'ın işlenmemiş kayıt taraması.
        b.HasIndex(x => new { x.IsSuspicious, x.CreatedAt }).IsDescending(false, true);
        // R3 — "bu kullanıcı bu IP'den daha önce BAŞARIYLA girmiş mi".
        b.HasIndex(x => new { x.UserId, x.Succeeded, x.IpAddress });
        // PurgeLoginAttemptsJob başarı/başarısızlığa göre farklı süre uyguluyor.
        b.HasIndex(x => new { x.Succeeded, x.CreatedAt });
        // 🔑 Maskeli kimlik ARANIR ve SÜZÜLÜR: kullanıcı/personel detayındaki "son giriş
        // denemeleri" kutusu hatalı OTP satırlarını yalnız bu kolondan bulabiliyor
        // (o dalda UserId bilerek boş). İndekssiz bırakılsaydı her kullanıcı sayfası
        // tablonun tamamını tarardı ve yavaşlama ancak tablo büyüdükçe fark edilirdi.
        b.HasIndex(x => new { x.Identifier, x.CreatedAt });
    }
}
