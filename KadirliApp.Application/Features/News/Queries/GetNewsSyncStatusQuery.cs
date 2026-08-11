using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>
/// Faz 12.13 — <b>"senkron çalışıyor mu?" sorusunun tek cevabı.</b>
/// </summary>
/// <remarks>
/// 🔴 Bu bloğun 1 numaralı hasar sınıfının ekran karşılığı: senkron sessizce durursa uygulama
/// <b>eski haberi göstermeye devam eder</b> — uçlar 200 döner, liste dolu görünür, log
/// temizdir ve <b>hiç kimse hata almaz</b>. Diğer 26 modülde veriyi biz giriyoruz ve
/// girilmediğini bilen bir insan var; burada yok. Tek gösterge
/// <c>news_sync_state.last_successful_run_at</c>.
///
/// 🔑 Hem senkron panosu hem Dashboard kutusu <b>bu sorgudan</b> okur. İki ekran ayrı
/// hesaplasaydı biri "taze" derken diğeri "durdu" derdi (§7 madde 35'in sınıfı) — eşiklerin
/// tek sahibi zaten <c>NewsSyncHealth</c>.
///
/// 📌 <c>/hangfire</c> panosu bu sorunun cevabı DEĞİL: "job koştu mu"yu gösterir, "kaç haber
/// geldi"yi göstermez — üstelik panoya erişimin kendisi <c>ARCHITECTURE.md</c> §3'te bir risk
/// olarak işaretli.
/// </remarks>
public record GetNewsSyncStatusQuery : IRequest<NewsSyncStatusDto>;

public class GetNewsSyncStatusQueryHandler : IRequestHandler<GetNewsSyncStatusQuery, NewsSyncStatusDto>
{
    private readonly IUnitOfWork _uow;
    private readonly NewsSyncOptions _options;

    public GetNewsSyncStatusQueryHandler(IUnitOfWork uow, NewsSyncOptions options)
    {
        _uow = uow;
        _options = options;
    }

    public async Task<NewsSyncStatusDto> Handle(GetNewsSyncStatusQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var articles = _uow.Repository<NewsArticle>().Query();

        var state = await _uow.Repository<NewsSyncState>().Query()
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var running = await _uow.Repository<NewsSyncRun>().Query()
            .Where(x => x.CompletedAt == null)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => (DateTime?)x.StartedAt)
            .FirstOrDefaultAsync(ct);

        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        var lastRun = await _uow.Repository<NewsSyncRun>().Query()
            .OrderByDescending(x => x.StartedAt)
            .ThenBy(x => x.Id)
            .Select(NewsSyncRunProjection.Select(users))
            .FirstOrDefaultAsync(ct);

        return new NewsSyncStatusDto
        {
            LastSuccessfulRunAt = state?.LastSuccessfulRunAt,
            Freshness = NewsSyncHealth.Evaluate(state?.LastSuccessfulRunAt, now),

            IsRunning = running is not null,
            RunningSince = running,

            TotalArticles = await articles.CountAsync(ct),
            VisibleArticles = await NewsVisibility.Published(articles).CountAsync(ct),
            ArchivedArticles = await articles.CountAsync(x => x.IsArchived, ct),
            GoneArticles = await articles.CountAsync(x => x.SourceState == NewsSourceStates.Gone, ct),
            StaleOverrides = await articles.CountAsync(NewsAdminProjection.StaleOverride, ct),

            TargetArticleCount = _options.MaxTotalPosts,
            ArchiveCompleted = state?.ArchiveCompleted ?? false,

            LastRun = lastRun
        };
    }
}

/// <summary>Faz 12.13 — senkron koşularının listesi (pano).</summary>
public record GetNewsSyncRunsQuery(string? Mode, string? Status, string? Trigger, int Page, int Limit)
    : IRequest<PagedResult<NewsSyncRunDto>>;

public class GetNewsSyncRunsQueryHandler : IRequestHandler<GetNewsSyncRunsQuery, PagedResult<NewsSyncRunDto>>
{
    private readonly IUnitOfWork _uow;

    public GetNewsSyncRunsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<NewsSyncRunDto>> Handle(GetNewsSyncRunsQuery request, CancellationToken ct)
    {
        var query = _uow.Repository<NewsSyncRun>().Query();

        // Bilinmeyen değer SÜZMEZ (§5) — bir yazım hatası panoyu boşaltmamalı.
        if (NewsSyncModes.All.Contains(request.Mode ?? string.Empty))
            query = query.Where(x => x.Mode == request.Mode);

        if (NewsSyncStatuses.All.Contains(request.Status ?? string.Empty))
            query = query.Where(x => x.Status == request.Status);

        if (NewsSyncTriggers.All.Contains(request.Trigger ?? string.Empty))
            query = query.Where(x => x.Trigger == request.Trigger);

        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        var totalCount = await query.CountAsync(ct);
        var (page, limit) = Pagination.Clamp(request.Page, request.Limit, Pagination.AdminMaxLimit);

        var items = await query
            // ⚠️ ThenBy(Id): günde 96 koşu, aynı saniyede başlayan iki koşu mümkün (§7 madde 30).
            .OrderByDescending(x => x.StartedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(NewsSyncRunProjection.Select(users))
            .ToListAsync(ct);

        return new PagedResult<NewsSyncRunDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}

/// <summary>Faz 12.13 — tek koşunun ayrıntısı (<b>aynı</b> projeksiyon).</summary>
public record GetNewsSyncRunByIdQuery(Guid Id) : IRequest<NewsSyncRunDto?>;

public class GetNewsSyncRunByIdQueryHandler : IRequestHandler<GetNewsSyncRunByIdQuery, NewsSyncRunDto?>
{
    private readonly IUnitOfWork _uow;

    public GetNewsSyncRunByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NewsSyncRunDto?> Handle(GetNewsSyncRunByIdQuery request, CancellationToken ct)
    {
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        return await _uow.Repository<NewsSyncRun>().Query()
            .Where(x => x.Id == request.Id)
            .Select(NewsSyncRunProjection.Select(users))
            .FirstOrDefaultAsync(ct);
    }
}

/// <summary>Koşu projeksiyonunun <b>tek sahibi</b> — liste, ayrıntı ve "son koşu" kutusu.</summary>
/// <remarks>
/// §7 madde 43: üç ayrı <c>Select</c> yazılsaydı yeni bir sayaç kolonu (12.12'de
/// <c>MarkedGone</c>/<c>Restored</c> eklendiği gibi) yalnız birine girer ve diğer ekranlar
/// <b>sessizce eksik</b> kalırdı.
/// </remarks>
public static class NewsSyncRunProjection
{
    public static System.Linq.Expressions.Expression<Func<NewsSyncRun, NewsSyncRunDto>> Select(
        IQueryable<User> users) => x => new NewsSyncRunDto
    {
        Id = x.Id,
        StartedAt = x.StartedAt,
        CompletedAt = x.CompletedAt,
        Mode = x.Mode,
        Trigger = x.Trigger,
        TriggeredByName = users.Where(u => u.Id == x.TriggeredBy).Select(u => u.Username).FirstOrDefault(),
        Status = x.Status,
        ErrorMessage = x.ErrorMessage,
        Fetched = x.Fetched,
        Created = x.Created,
        Updated = x.Updated,
        Skipped = x.Skipped,
        Failed = x.Failed,
        MarkedGone = x.MarkedGone,
        Restored = x.Restored,
        CursorFrom = x.CursorFrom,
        CursorTo = x.CursorTo
    };
}
