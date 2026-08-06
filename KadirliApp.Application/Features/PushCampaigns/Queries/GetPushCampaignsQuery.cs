using System.Text.Json;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Common.Sorting;
using KadirliApp.Application.Features.PushCampaigns.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.PushCampaigns.Queries;

/// <summary>
/// Faz 12.2b — teslim panosunun listesi.
///
/// ⚠️ <b>Sayaçlar tablodan OKUNUR, <c>COUNT</c> ile hesaplanmaz.</b> Her açılışta binlerce
/// bildirim satırını <c>GROUP BY</c> ile saymak, panelin en hızlı büyüyecek tablosunu tam
/// tarardı; job zaten elindeki değerleri yazıyor (görünmez sözleşme).
/// </summary>
public record GetPushCampaignsQuery(QueryPushCampaignDto QueryDto)
    : IRequest<PagedResult<PushCampaignResponseDto>>;

public class GetPushCampaignsQueryHandler
    : IRequestHandler<GetPushCampaignsQuery, PagedResult<PushCampaignResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetPushCampaignsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<PushCampaignResponseDto>> Handle(
        GetPushCampaignsQuery request, CancellationToken ct)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<PushCampaign>().Query();

        // Hesabını silmiş yöneticinin gönderimi de kalır — kayıt durur, kullanıcı gider
        // (denetim izi ve giriş denemelerindeki aynı karar).
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(dto.Source))
            query = query.Where(x => x.Source == dto.Source);

        if (!string.IsNullOrWhiteSpace(dto.TargetType))
            query = query.Where(x => x.TargetType == dto.TargetType);

        query = ApplyStatusFilter(query, dto.Status);

        if (dto.From is { } from)
            query = query.Where(x => x.CreatedAt >= from.Date);

        if (dto.To is { } to)
        {
            // "5 Ağustos"u seçen kişi o günün tamamını kasteder (12.1/12.2'deki aynı karar).
            var end = to.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var term = dto.Search.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var raw = await PanelSorts.PushCampaigns.Apply(query, dto.Sort)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new
            {
                x.Id, x.Title, x.Body, x.TargetType, x.TargetNeighborhoods,
                x.Source, x.SourceId,
                CreatedByName = users.Where(u => u.Id == x.CreatedBy).Select(u => u.Username).FirstOrDefault(),
                x.RecipientCount, x.SentCount, x.FailedCount, x.InvalidTokenCount,
                x.CreatedAt, x.CompletedAt, x.CancelledAt
            })
            .ToListAsync(ct);

        var names = await ResolveNeighborhoodNamesAsync(
            raw.Select(x => x.TargetNeighborhoods), ct);

        var items = raw.Select(x => new PushCampaignResponseDto
        {
            Id = x.Id,
            Title = x.Title,
            Body = x.Body,
            TargetType = x.TargetType,
            TargetNeighborhoodNames = NamesFor(x.TargetNeighborhoods, names),
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
                        PushCampaignStatus.Pending(x.RecipientCount, x.SentCount, x.FailedCount) > 0
        }).ToList();

        return new PagedResult<PushCampaignResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }

    /// <summary>
    /// Durum türetilmiş bir alan olduğu için süzgeç <b>aynı kuralı SQL'de tekrar tarif eder.</b>
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu bilinçli bir tekrar ve tek alternatifi listeyi belleğe çekip süzmekti — yani
    /// sayfalamayı bozmak. Tekrarın bedeli, ikisinin ayrışabilmesi; bedeli ödeyen de test:
    /// <c>PanelPushCampaignTests</c> her durum için "süzgeç ne getiriyorsa DTO'nun durumu
    /// da odur" iddiasını kurar.
    /// </remarks>
    private static IQueryable<PushCampaign> ApplyStatusFilter(IQueryable<PushCampaign> query, string? status)
        => status switch
        {
            PushCampaignStatuses.Cancelled => query.Where(x => x.CancelledAt != null),
            PushCampaignStatuses.Empty => query.Where(x => x.CancelledAt == null && x.RecipientCount == 0),
            PushCampaignStatuses.Completed => query.Where(x =>
                x.CancelledAt == null && x.RecipientCount > 0 && x.CompletedAt != null),
            PushCampaignStatuses.Sending => query.Where(x =>
                x.CancelledAt == null && x.RecipientCount > 0 && x.CompletedAt == null &&
                x.SentCount + x.FailedCount > 0),
            PushCampaignStatuses.Queued => query.Where(x =>
                x.CancelledAt == null && x.RecipientCount > 0 && x.CompletedAt == null &&
                x.SentCount + x.FailedCount == 0),
            _ => query
        };

    /// <summary>
    /// Sayfadaki tüm kampanyaların mahalle kimliklerini <b>tek sorguda</b> ada çevirir.
    /// Kampanya başına ayrı sorgu atmak 25 satırlık bir sayfada 25 gidiş-dönüş demekti.
    /// </summary>
    private async Task<Dictionary<Guid, string>> ResolveNeighborhoodNamesAsync(
        IEnumerable<string?> jsonValues, CancellationToken ct)
    {
        var ids = jsonValues.SelectMany(ParseIds).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        return await _uow.Repository<Neighborhood>().Query()
            .Where(n => ids.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Name, ct);
    }

    private static IReadOnlyList<string> NamesFor(string? json, IReadOnlyDictionary<Guid, string> names)
        => ParseIds(json)
            // ⚠️ Silinmiş bir mahallenin kimliği adsız kalır; satır yine de sayılmalı
            // ki "3 mahalle" derken ekranda 2 ad görünmesin.
            .Select(id => names.TryGetValue(id, out var name) ? name : "(silinmiş mahalle)")
            .ToList();

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
