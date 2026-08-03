using System;

namespace KadirliApp.Application.Features.Trash.Dtos;

/// <summary>
/// Faz 11.17: çöp kutusundaki tek satır. Soft delete her modülde vardı, panelde karşılığı
/// yoktu — yanlışlıkla silinen bir duyuru/ilan ancak <c>psql</c> ile geri getirilebiliyordu.
/// </summary>
public class TrashItemDto
{
    /// <summary>Panel modül anahtarı ("ads", "announcements"…) — menü ve izinlerle aynı sözlük.</summary>
    public string Module { get; set; } = default!;
    public Guid Id { get; set; }

    /// <summary>Kaydın tanınabilir adı (ilan başlığı, merhumun adı, sürücü adı).</summary>
    public string Title { get; set; } = default!;

    public DateTime DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class QueryTrashDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 25;

    /// <summary>Boşsa tüm modüller karışık, silinme tarihine göre sıralı gelir.</summary>
    public string? Module { get; set; }
}
