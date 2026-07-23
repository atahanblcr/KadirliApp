using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Body).IsRequired();

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // ix_notif_user_read ON notifications (user_id, is_read, created_at DESC);
        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt }).IsDescending(false, false, true).HasDatabaseName("ix_notif_user_read");
    }
}
