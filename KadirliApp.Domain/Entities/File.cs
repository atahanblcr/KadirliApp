using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class File : BaseEntity, ISoftDeletable
{
    public string OriginalName { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string? MimeType { get; set; }
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;
    public string? CdnUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ModuleType { get; set; }
    public Guid? ModuleId { get; set; }
    public Guid? UploadedBy { get; set; }
    public string? Metadata { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User? Uploader { get; set; }
}
