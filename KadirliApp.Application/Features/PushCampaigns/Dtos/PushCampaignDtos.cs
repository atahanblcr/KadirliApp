using System.ComponentModel.DataAnnotations;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PushCampaigns.Dtos;

/// <summary>
/// Faz 12.2b — panonun gördüğü gönderim satırı.
///
/// ⚠️ <c>Status</c> ve <c>PendingCount</c> <b>türetilmiş</b> alanlardır
/// (<see cref="PushCampaignStatus"/>) — veritabanında karşılıkları yok ve olmamalı:
/// ayrı bir kolonda tutulsalardı sayaçlarla ayrışabilir ve pano sessizce yalan söylerdi.
/// </summary>
public class PushCampaignResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string TargetType { get; set; } = default!;

    /// <summary>Hedeflenen mahallelerin adları — kimlik değil, çünkü ekranda okunacak.</summary>
    public IReadOnlyList<string> TargetNeighborhoodNames { get; set; } = Array.Empty<string>();

    public string Source { get; set; } = default!;
    public Guid? SourceId { get; set; }

    /// <summary>Manuel gönderimde butona basan yönetici (kullanıcı adı). Otomatikte <c>null</c>.</summary>
    public string? CreatedByName { get; set; }

    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int InvalidTokenCount { get; set; }
    public int PendingCount { get; set; }

    /// <summary><see cref="PushCampaignStatuses"/> ham değeri — panelde Türkçeleştirilir.</summary>
    public string Status { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// İptal edilebilir mi — <b>sunucuda</b> karar verilir, görünüm yalnız uygular.
    /// </summary>
    /// <remarks>
    /// 🔑 Görünüm kendi koşulunu yazsaydı panel, komutun reddedeceği bir butonu çizerdi:
    /// "işlevsiz buton yok" kuralının panel karşılığı.
    ///
    /// 🐛 İlk yazımda koşul "tamamlanmamış" idi ve test onu kırdı: <b>"tamamlandı" ile
    /// "geri çekilecek bir şey kalmadı" aynı şey değil.</b> Kampanya, gönderilebilir
    /// bekleyen satır kalmadığında tamamlanır — ama token'ı olmayan alıcıların satırları
    /// hâlâ durur ve o kişiler yarın token kaydederse gönderilir. Doğru ölçüt
    /// <see cref="PendingCount"/>.
    /// </remarks>
    public bool CanCancel { get; set; }
}

/// <summary>Kampanya ayrıntısı — "188 başarısız"ın <b>neden</b>ini gösteren kırılımla.</summary>
public class PushCampaignDetailDto : PushCampaignResponseDto
{
    /// <summary>
    /// FCM hata kodu → adet. Kırılım olmadan pano "başarısız" der ve yönetici hiçbir şey
    /// yapamaz; <c>SENDER_ID_MISMATCH</c> ile <c>UNREGISTERED</c> tamamen farklı iki eylem gerektirir.
    /// </summary>
    public IReadOnlyList<PushErrorBreakdownDto> ErrorBreakdown { get; set; } = Array.Empty<PushErrorBreakdownDto>();
}

public class PushErrorBreakdownDto
{
    public string Error { get; set; } = default!;
    public int Count { get; set; }
}

public class QueryPushCampaignDto
{
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 25;

    public string? Source { get; set; }
    public string? TargetType { get; set; }

    /// <summary>Ham <see cref="PushCampaignStatuses"/> değeri. Türetilmiş alan olduğu için SQL'de tarif edilir.</summary>
    public string? Status { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    /// <summary>Başlıkta arar.</summary>
    public string? Search { get; set; }

    public string? Sort { get; set; }
}

/// <summary>Panelin "yeni bildirim gönder" formu.</summary>
public class SendPushCampaignDto
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Başlık 3-200 karakter olmalıdır.")]
    public string Title { get; set; } = default!;

    [Required(ErrorMessage = "Mesaj metni zorunludur.")]
    [StringLength(500, MinimumLength = 3, ErrorMessage = "Mesaj 3-500 karakter olmalıdır.")]
    public string Body { get; set; } = default!;

    [Required]
    public string TargetType { get; set; } = PushTargetTypes.All;

    public List<Guid> TargetNeighborhoodIds { get; set; } = new();
}
