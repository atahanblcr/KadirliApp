using System;

namespace KadirliApp.Application.Features.Audit.Dtos;

/// <summary>
/// Faz 11.17: denetim izi satırı. <c>AuditBehavior</c> 10.9(i)'den beri yazıyordu ama
/// okuyan tek ekran/uç yoktu — "bu ilanı kim sildi?" ancak <c>psql</c> ile cevaplanıyordu.
/// </summary>
public class AuditLogResponseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid UserId { get; set; }
    /// <summary>Personelin görünen adı; kayıt silinmişse null (iz kalır, aktör kaybolabilir).</summary>
    public string? UserName { get; set; }
    public string? UserRole { get; set; }

    public string Module { get; set; } = default!;
    public string Action { get; set; } = default!;

    public Guid? AffectedId { get; set; }
    public string? AffectedType { get; set; }

    /// <summary>jsonb <c>details</c> kolonunun ham metni (komutun bilinçli seçilmiş alanları).</summary>
    public string? Details { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>Denetim izi filtresi. Boş bırakılan alan süzmez.</summary>
public class QueryAuditLogDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 25;

    public string? Module { get; set; }
    public string? Action { get; set; }
    public Guid? UserId { get; set; }

    /// <summary>Etkilenen kaydın kimliği — "bu ilana ne oldu?" sorusunun tek cevabı.</summary>
    public Guid? AffectedId { get; set; }

    /// <summary>Dâhil (gün başı). </summary>
    public DateTime? From { get; set; }
    /// <summary>Dâhil (gün sonu; handler günün sonuna kadar genişletir).</summary>
    public DateTime? To { get; set; }

    /// <summary>Personel adı / etkilenen tip / details içinde geçen metin.</summary>
    public string? Search { get; set; }
}
