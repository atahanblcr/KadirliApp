using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> b)
    {
        b.ToTable("error_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Source).HasMaxLength(20).IsRequired();
        b.Property(x => x.Level).HasMaxLength(20).IsRequired();
        b.Property(x => x.Code).HasMaxLength(120).IsRequired();
        b.Property(x => x.Message).IsRequired();          // text — mesaj uzunluğu tavanı uçta
        b.Property(x => x.Path).HasMaxLength(500);
        b.Property(x => x.Method).HasMaxLength(10);
        b.Property(x => x.TraceId).HasMaxLength(120);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.AppVersion).HasMaxLength(40);
        b.Property(x => x.Platform).HasMaxLength(40);
        b.Property(x => x.OsVersion).HasMaxLength(80);
        b.Property(x => x.ResolvedNote).HasMaxLength(1000);
        b.Property(x => x.IpAddress).HasColumnType("inet");

        b.Property(x => x.Fingerprint).HasMaxLength(32).IsRequired();

        // 🔑 Tekilleştirmenin kapısı VERİTABANINDA. Yazıcı "önce ara, yoksa ekle" yapıyor,
        // ama Api ve Web ayrı süreçler — ikisi aynı anda aynı yeni hatayı görebilir ve
        // ikisi de "yok" diye ekler. Benzersiz indeks o yarışı kaybedeni patlatır, yazıcı
        // da onu yakalayıp güncellemeye çevirir. İndeks olmasaydı yarış SESSİZCE mükerrer
        // satır üretirdi ve tekilleştirme "çoğu zaman çalışan" bir şey olurdu.
        b.HasIndex(x => x.Fingerprint).IsUnique();

        // Panelin varsayılan sırası (son görülen önce) ve en sık süzgeçler.
        b.HasIndex(x => x.LastSeenAt).IsDescending();
        b.HasIndex(x => new { x.Source, x.Level });
        b.HasIndex(x => x.IsResolved);
    }
}
