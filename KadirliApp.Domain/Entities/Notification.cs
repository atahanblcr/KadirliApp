using KadirliApp.Domain.Common;

namespace KadirliApp.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? Type { get; set; }
    public Guid? RelatedId { get; set; }
    public string? RelatedType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool FcmSent { get; set; }
    public DateTime? FcmSentAt { get; set; }
    public string? FcmError { get; set; }

    /// <summary>
    /// Faz 12.2b — bu satırı üreten gönderim olayı (<see cref="PushCampaign"/>).
    /// </summary>
    /// <remarks>
    /// ⚠️ <b>Additive:</b> 12.2b öncesinde yazılmış satırlar <c>null</c> kalır ve öyle kalmalı.
    /// Geriye dönük kampanya uydurmak, olmayan bir teslim tarihçesi üretmek olurdu — pano
    /// "bilmiyorum" demeli, tahmin etmemeli. Panel bunu "kampanyasız" olarak gösterir.
    /// </remarks>
    public Guid? CampaignId { get; set; }

    public User User { get; set; } = default!;
    public PushCampaign? Campaign { get; set; }
}
