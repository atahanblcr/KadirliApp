using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class User : BaseEntity, ISoftDeletable
{
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Username { get; set; }
    public int? Age { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public Guid? PrimaryNeighborhoodId { get; set; }
    public string? LocationType { get; set; }
    public NotificationPreferences NotificationPreferences { get; set; } = new();
    public string? FcmToken { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? UsernameLastChangedAt { get; set; }
    public DateTime? NeighborhoodLastChangedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public Guid? BannedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Neighborhood? PrimaryNeighborhood { get; set; }
    public ICollection<UserNeighborhood> Neighborhoods { get; set; } = new List<UserNeighborhood>();
    public ICollection<AdminPermission> AdminPermissions { get; set; } = new List<AdminPermission>();
}

public class NotificationPreferences
{
    public bool Announcements { get; set; } = true;
    public bool Deaths { get; set; } = true;
    public bool Pharmacy { get; set; } = true;
    public bool Events { get; set; } = true;
    public bool Ads { get; set; } = false;
    public bool Campaigns { get; set; } = false;
}
