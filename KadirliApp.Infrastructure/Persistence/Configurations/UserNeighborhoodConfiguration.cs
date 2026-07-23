using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KadirliApp.Infrastructure.Persistence.Configurations;

public class UserNeighborhoodConfiguration : IEntityTypeConfiguration<UserNeighborhood>
{
    public void Configure(EntityTypeBuilder<UserNeighborhood> b)
    {
        b.ToTable("user_neighborhoods");
        b.HasKey(x => new { x.UserId, x.NeighborhoodId });
        
        b.HasOne(x => x.User)
         .WithMany(u => u.Neighborhoods)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
         
        b.HasOne(x => x.Neighborhood)
         .WithMany(n => n.UserNeighborhoods)
         .HasForeignKey(x => x.NeighborhoodId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
