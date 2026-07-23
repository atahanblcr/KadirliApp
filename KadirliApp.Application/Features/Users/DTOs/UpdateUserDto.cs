using System;

namespace KadirliApp.Application.Features.Users.DTOs;

public class UpdateUserDto
{
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public int? Age { get; set; }
    public int Role { get; set; }
    public Guid? PrimaryNeighborhoodId { get; set; }
    public string? LocationType { get; set; }
    public bool IsActive { get; set; } = true;
}
