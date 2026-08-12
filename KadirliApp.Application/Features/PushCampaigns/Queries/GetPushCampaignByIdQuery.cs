using System.Text.Json;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.PushCampaigns.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.PushCampaigns.Queries;

/// <summary>
/// Faz 12.2b — kampanya ayrıntısı: sayaçlar + <b>hata kırılımı</b>.
/// </summary>
/// <remarks>
/// 🔑 Kırılım olmadan pano "188 başarısız" der ve yönetici hiçbir şey yapamaz. Oysa
/// <c>UNREGISTERED</c> (kullanıcı uygulamayı sildi — yapılacak bir şey yok, token zaten
/// temizlendi) ile <c>SENDER_ID_MISMATCH</c> (yapılandırma yanlış — <b>her</b> gönderim
/// başarısız oluyor) tamamen farklı iki durumdur ve ikisi de aynı sayıya düşer.
///
/// ⚠️ Kırılım burada <c>GROUP BY</c> ile hesaplanır ve bu bilinçli: <b>tek kampanya</b>
/// için, <b>talep üzerine</b> açılan bir ekranda. Listedeki toplam sayaçların kolonda
/// tutulmasının sebebi (her açılışta tam tarama) burada geçerli değil.
/// </remarks>
public record GetPushCampaignByIdQuery(Guid Id) : IRequest<PushCampaignDetailDto?>;

public class GetPushCampaignByIdQueryHandler : IRequestHandler<GetPushCampaignByIdQuery, PushCampaignDetailDto?>
{
    private readonly IUnitOfWork _uow;

    public GetPushCampaignByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PushCampaignDetailDto?> Handle(GetPushCampaignByIdQuery request, CancellationToken ct)
    {
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        var x = await _uow.Repository<PushCampaign>().Query()
            .Where(c => c.Id == request.Id)
            .Select(c => new
            {
                c.Id, c.Title, c.Body, c.TargetType, c.TargetNeighborhoods,
                c.Source, c.SourceId,
                CreatedByName = users.Where(u => u.Id == c.CreatedBy).Select(u => u.Username).FirstOrDefault(),
                c.RecipientCount, c.SentCount, c.FailedCount, c.InvalidTokenCount,
                c.CreatedAt, c.CompletedAt, c.CancelledAt
            })
            .FirstOrDefaultAsync(ct);

        if (x is null) return null;

        var breakdown = await _uow.Repository<Notification>().Query()
            .Where(n => n.CampaignId == request.Id && n.FcmError != null)
            .GroupBy(n => n.FcmError!)
            .Select(g => new PushErrorBreakdownDto { Error = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Error)
            .ToListAsync(ct);

        var ids = ParseIds(x.TargetNeighborhoods);
        var names = ids.Count == 0
            ? new Dictionary<Guid, string>()
            : await _uow.Repository<Neighborhood>().Query()
                .Where(n => ids.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name, ct);

        return new PushCampaignDetailDto
        {
            Id = x.Id,
            Title = x.Title,
            Body = x.Body,
            TargetType = x.TargetType,
            TargetNeighborhoodNames = ids
                .Select(id => names.TryGetValue(id, out var name) ? name : "(silinmiş mahalle)")
                .ToList(),
            Source = x.Source,
            SourceId = x.SourceId,
            CreatedByName = x.CreatedByName,
            RecipientCount = x.RecipientCount,
            SentCount = x.SentCount,
            FailedCount = x.FailedCount,
            InvalidTokenCount = x.InvalidTokenCount,
            PendingCount = PushCampaignStatus.Pending(x.RecipientCount, x.SentCount, x.FailedCount),
            Status = PushCampaignStatus.Of(x.RecipientCount, x.SentCount, x.FailedCount, x.CompletedAt, x.CancelledAt),
            CreatedAt = x.CreatedAt,
            CompletedAt = x.CompletedAt,
            CancelledAt = x.CancelledAt,
            CanCancel = x.CancelledAt is null &&
                        PushCampaignStatus.Pending(x.RecipientCount, x.SentCount, x.FailedCount) > 0,
            ErrorBreakdown = breakdown
        };
    }

    private static IReadOnlyList<Guid> ParseIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<Guid>();
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid>();
        }
    }
}

/// <summary>
/// Panel formunun "kaç kişiye gidecek?" önizlemesi.
/// </summary>
/// <remarks>
/// 🔑 <c>INotificationDispatcher</c>'a devredilir — yani <b>gönderimin kendisiyle aynı
/// süzgeç</b>. Ayrı bir sayım yazılsaydı panel "342 kişiye gidecek" der, gönderim 280 satır
/// yazardı ve aradaki fark hiçbir yerde görünmezdi.
/// </remarks>
public record EstimatePushRecipientsQuery(string TargetType, IReadOnlyList<Guid>? NeighborhoodIds)
    : IRequest<int>;

public class EstimatePushRecipientsQueryHandler : IRequestHandler<EstimatePushRecipientsQuery, int>
{
    private readonly INotificationDispatcher _dispatcher;

    public EstimatePushRecipientsQueryHandler(INotificationDispatcher dispatcher) => _dispatcher = dispatcher;

    public Task<int> Handle(EstimatePushRecipientsQuery request, CancellationToken ct)
        // ⚠️ Faz 12.15b — kaynak AÇIKÇA veriliyor. Bu sorgu panelin **elle gönderim**
        // formunun önizlemesi; kaynağı söylemeseydi varsayılana düşerdi ve bugün doğru
        // olurdu — ama yarın form başka bir kaynağı da desteklediğinde önizleme ile
        // gönderim sessizce ayrışırdı (§7 madde 38).
        => _dispatcher.EstimateRecipientsAsync(
            request.TargetType, request.NeighborhoodIds, PushCampaignSources.Manual, ct);
}

/// <summary>
/// Dashboard satırı: "son gönderim: N/M teslim".
/// 12.1'in hata rozeti ve 12.2'nin şüpheli giriş rozetiyle aynı desen ve aynı gerekçe —
/// kimse panoya "acaba gitti mi" diye günde üç kez bakmaz; sayı iniş sayfasında olmalı.
/// </summary>
public record GetLastPushCampaignQuery : IRequest<PushCampaignResponseDto?>;

public class GetLastPushCampaignQueryHandler : IRequestHandler<GetLastPushCampaignQuery, PushCampaignResponseDto?>
{
    private readonly ISender _sender;

    public GetLastPushCampaignQueryHandler(ISender sender) => _sender = sender;

    public async Task<PushCampaignResponseDto?> Handle(GetLastPushCampaignQuery request, CancellationToken ct)
    {
        // Liste sorgusuna delege: ikinci bir projeksiyon yazılsaydı dashboard ile pano
        // aynı kampanya için farklı sayı gösterebilirdi (görünmez sözleşme #23 sınıfı).
        var page = await _sender.Send(new GetPushCampaignsQuery(new QueryPushCampaignDto
        {
            Page = 1,
            Limit = 1
        }), ct);

        return page.Items.FirstOrDefault();
    }
}
