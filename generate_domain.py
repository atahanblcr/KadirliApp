import os

def create_file(path, content):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as f:
        f.write(content.strip() + "\n")

# Base paths
domain_path = "KadirliApp.Domain/Entities"
enums_path = "KadirliApp.Domain/Enums"
config_path = "KadirliApp.Infrastructure/Persistence/Configurations"

# --- ENUMS ---
create_file(f"{enums_path}/AdStatus.cs", """
namespace KadirliApp.Domain.Enums;

public enum AdStatus
{
    Pending,
    Approved,
    Rejected,
    Expired
}
""")

create_file(f"{enums_path}/DeathStatus.cs", """
namespace KadirliApp.Domain.Enums;

public enum DeathStatus
{
    Pending,
    Approved,
    Rejected,
    Archived
}
""")

create_file(f"{enums_path}/EventStatus.cs", """
namespace KadirliApp.Domain.Enums;

public enum EventStatus
{
    Pending,
    Approved,
    Rejected,
    Canceled
}
""")

create_file(f"{enums_path}/PropertyType.cs", """
namespace KadirliApp.Domain.Enums;

public enum PropertyType
{
    Text,
    Number,
    Boolean,
    Select,
    MultiSelect
}
""")

# --- ADS MODULE ENTITIES ---
create_file(f"{domain_path}/AdCategory.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public Guid? ParentId { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public AdCategory? Parent { get; set; }
    public ICollection<AdCategory> SubCategories { get; set; } = new List<AdCategory>();
    public ICollection<CategoryProperty> Properties { get; set; } = new List<CategoryProperty>();
}
""")

create_file(f"{domain_path}/CategoryProperty.cs", """
using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class CategoryProperty : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string PropertyName { get; set; } = default!;
    public PropertyType PropertyType { get; set; } = PropertyType.Text;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int DisplayOrder { get; set; }

    public AdCategory Category { get; set; } = default!;
    public ICollection<PropertyOption> Options { get; set; } = new List<PropertyOption>();
}
""")

create_file(f"{domain_path}/PropertyOption.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PropertyOption : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string OptionValue { get; set; } = default!;
    public int DisplayOrder { get; set; }

    public CategoryProperty Property { get; set; } = default!;
}
""")

create_file(f"{domain_path}/Ad.cs", """
using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class Ad : BaseEntity, ISoftDeletable
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public Guid UserId { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExtensionCount { get; set; }
    public int MaxExtensions { get; set; } = 3;
    public int ViewCount { get; set; }
    public int PhoneClickCount { get; set; }
    public int WhatsappClickCount { get; set; }
    public DateTime? DeletedAt { get; set; }

    public AdCategory Category { get; set; } = default!;
    public ICollection<AdImage> Images { get; set; } = new List<AdImage>();
    public ICollection<AdPropertyValue> PropertyValues { get; set; } = new List<AdPropertyValue>();
    public ICollection<AdFavorite> Favorites { get; set; } = new List<AdFavorite>();
    public ICollection<AdExtension> Extensions { get; set; } = new List<AdExtension>();
}
""")

create_file(f"{domain_path}/AdImage.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdImage : BaseEntity
{
    public Guid AdId { get; set; }
    public Guid FileId { get; set; }
    public bool IsCover { get; set; }
    public int DisplayOrder { get; set; }

    public Ad Ad { get; set; } = default!;
}
""")

create_file(f"{domain_path}/AdPropertyValue.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdPropertyValue : BaseEntity
{
    public Guid AdId { get; set; }
    public Guid PropertyId { get; set; }
    public string Value { get; set; } = default!;

    public Ad Ad { get; set; } = default!;
    public CategoryProperty Property { get; set; } = default!;
}
""")

create_file(f"{domain_path}/AdFavorite.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdFavorite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid AdId { get; set; }

    public Ad Ad { get; set; } = default!;
}
""")

create_file(f"{domain_path}/AdExtension.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class AdExtension : BaseEntity
{
    public Guid AdId { get; set; }
    public Guid UserId { get; set; }
    public int AdsWatched { get; set; }
    public int DaysExtended { get; set; }
    public DateTime ExtendedAt { get; set; }

    public Ad Ad { get; set; } = default!;
}
""")

# --- DEATHS MODULE ENTITIES ---
create_file(f"{domain_path}/Cemetery.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Cemetery : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
""")

create_file(f"{domain_path}/Mosque.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Mosque : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
""")

create_file(f"{domain_path}/DeathNotice.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class DeathNotice : BaseEntity, ISoftDeletable
{
    public string DeceasedName { get; set; } = default!;
    public int? Age { get; set; }
    public Guid? PhotoFileId { get; set; }
    public DateTime FuneralDate { get; set; }
    public TimeSpan FuneralTime { get; set; }
    public Guid? CemeteryId { get; set; }
    public Guid? MosqueId { get; set; }
    public Guid? NeighborhoodId { get; set; }
    public string? CondolenceAddress { get; set; }
    public Guid AddedBy { get; set; }
    public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? AutoArchiveAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Cemetery? Cemetery { get; set; }
    public Mosque? Mosque { get; set; }
}
""")

# --- PHARMACY MODULE ENTITIES ---
create_file(f"{domain_path}/Pharmacy.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Pharmacy : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? WorkingHours { get; set; }
    public string? PharmacistName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PharmacySchedule> Schedules { get; set; } = new List<PharmacySchedule>();
}
""")

create_file(f"{domain_path}/PharmacySchedule.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class PharmacySchedule : BaseEntity
{
    public Guid PharmacyId { get; set; }
    public DateTime DutyDate { get; set; }
    public TimeSpan StartTime { get; set; } = new TimeSpan(19, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(9, 0, 0);
    public string? Source { get; set; }

    public Pharmacy Pharmacy { get; set; } = default!;
}
""")

# --- EVENTS MODULE ENTITIES ---
create_file(f"{domain_path}/EventCategory.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class EventCategory : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
""")

create_file(f"{domain_path}/Event.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Event : BaseEntity, ISoftDeletable
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public DateTime EventDate { get; set; }
    public TimeSpan EventTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Organizer { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool IsFree { get; set; }
    public int? AgeRestriction { get; set; }
    public int? Capacity { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? TicketUrl { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrencePattern { get; set; }
    public bool IsLocal { get; set; }
    public string Status { get; set; } = "pending";
    public Guid CreatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public EventCategory Category { get; set; } = default!;
    public ICollection<EventImage> Images { get; set; } = new List<EventImage>();
}
""")

create_file(f"{domain_path}/EventImage.cs", """
using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class EventImage : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid FileId { get; set; }
    public int DisplayOrder { get; set; }

    public Event Event { get; set; } = default!;
}
""")

print("Domain Entities and Enums created successfully.")
