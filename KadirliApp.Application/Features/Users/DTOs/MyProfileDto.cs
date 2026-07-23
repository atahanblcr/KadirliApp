using System;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Application.Features.Users.DTOs;

/// <summary>Faz 10.3: GET/PATCH /v1/users/me yanıtı — profil + mahalle adı + bildirim tercihleri.</summary>
public class MyProfileDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public int? Age { get; set; }
    public string Role { get; set; } = default!;
    public Guid? PrimaryNeighborhoodId { get; set; }
    public string? PrimaryNeighborhoodName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public NotificationPreferencesDto NotificationPreferences { get; set; } = new();
    public DateTime? UsernameLastChangedAt { get; set; }
    public DateTime? NeighborhoodLastChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static MyProfileDto FromUser(User user, string? neighborhoodName)
    {
        return new MyProfileDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Email = user.Email,
            Username = user.Username,
            Age = user.Age,
            Role = user.Role.ToRoleString(),
            PrimaryNeighborhoodId = user.PrimaryNeighborhoodId,
            PrimaryNeighborhoodName = neighborhoodName,
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            NotificationPreferences = NotificationPreferencesDto.FromEntity(user.NotificationPreferences),
            UsernameLastChangedAt = user.UsernameLastChangedAt,
            NeighborhoodLastChangedAt = user.NeighborhoodLastChangedAt,
            CreatedAt = user.CreatedAt
        };
    }
}

public class NotificationPreferencesDto
{
    public bool Announcements { get; set; } = true;
    public bool Deaths { get; set; } = true;
    public bool Pharmacy { get; set; } = true;
    public bool Events { get; set; } = true;
    public bool Ads { get; set; }
    public bool Campaigns { get; set; }

    public static NotificationPreferencesDto FromEntity(NotificationPreferences p) => new()
    {
        Announcements = p.Announcements,
        Deaths = p.Deaths,
        Pharmacy = p.Pharmacy,
        Events = p.Events,
        Ads = p.Ads,
        Campaigns = p.Campaigns
    };
}
