using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class TaxiDriver : BaseEntity, ISoftDeletable
{
    public Guid? UserId { get; set; }
    public string Name { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? Plaka { get; set; }
    public string? VehicleInfo { get; set; }
    public Guid? LicenseFileId { get; set; }
    public Guid? RegistrationFileId { get; set; }
    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int TotalCalls { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User? User { get; set; }
    public File? LicenseFile { get; set; }
    public File? RegistrationFile { get; set; }
}
