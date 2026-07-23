using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class AnnouncementViewConfiguration : IEntityTypeConfiguration<AnnouncementView>
{
    public void Configure(EntityTypeBuilder<AnnouncementView> b)
    {
        b.ToTable("announcement_views");
        b.HasKey(x => new { x.AnnouncementId, x.UserId });
        
        b.HasOne(x => x.Announcement)
         .WithMany()
         .HasForeignKey(x => x.AnnouncementId)
         .OnDelete(DeleteBehavior.Cascade);
         
        b.HasOne(x => x.User)
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
