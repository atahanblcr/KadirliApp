using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Common.Sorting;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>
/// Faz 12.13 — panelin haber listesi.
/// </summary>
/// <remarks>
/// 🔴 <b>Görünürlük süzgeci burada YOK ve olmamalı:</b> panelin işi tam olarak vatandaşın
/// <i>göremediği</i> kayıtları göstermek (arşivlenmiş · kaynaktan kalkmış · dışlanmış
/// kategoride). Public uçtaki <c>NewsVisibility</c> ile karıştırılmamalı — ikisi <b>farklı
/// soruların</b> cevabı ve bu ayrım bilinçli.
///
/// ⚠️ Süzgeçler <b>sunucuda</b>: 20'lik sayfadan bellekte eleme yapmak <c>totalCount</c>'u ve
/// sayfalamayı yalancı yapar (checklist §5, 12.6'nın dersi).
/// 📌 Önbelleklenmez — panel her zaman <b>şu anki</b> gerçeği göstermek zorunda; 15 dk eski
/// bir liste, senkronun az önce ne yaptığını araştıran yöneticiye yalan söylerdi.
/// </remarks>
public record GetNewsAdminQuery(QueryNewsAdminDto QueryDto) : IRequest<PagedResult<NewsAdminDto>>;

public class GetNewsAdminQueryHandler : IRequestHandler<GetNewsAdminQuery, PagedResult<NewsAdminDto>>
{
    private readonly IUnitOfWork _uow;

    public GetNewsAdminQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<NewsAdminDto>> Handle(GetNewsAdminQuery request, CancellationToken ct)
    {
        var dto = request.QueryDto;
        var now = DateTime.UtcNow;

        var query = Filter(_uow.Repository<NewsArticle>().Query(), dto, now);

        // Hesabını silmiş yöneticinin düzeltmesi kayıtta durur (denetim izindeki aynı karar).
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        var totalCount = await query.CountAsync(ct);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var items = await PanelSorts.News.Apply(query, dto.Sort)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(NewsAdminProjection.Select(users, includeContent: false))
            .ToListAsync(ct);

        foreach (var item in items) NewsAdminProjection.Finish(item, now);

        return new PagedResult<NewsAdminDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }

    /// <summary>
    /// Süzgeçlerin <b>tek sahibi</b> — liste, CSV ve sayaçlar aynı metottan geçer.
    /// </summary>
    /// <remarks>
    /// ⚠️ Ayrı yazılsalardı ekranda 12 satır, dosyada 400 satır olurdu (11.16b'nin dersi).
    /// 📌 Bilinmeyen <c>State</c> değeri <b>süzmez</b> (§5): bir yazım hatası listeyi
    /// boşaltmamalı.
    /// </remarks>
    internal static IQueryable<NewsArticle> Filter(IQueryable<NewsArticle> query, QueryNewsAdminDto dto, DateTime now)
    {
        query = dto.State switch
        {
            NewsStates.Gone => query.Where(x => x.SourceState == NewsSourceStates.Gone),
            NewsStates.Archived => query.Where(x => x.IsArchived && x.SourceState != NewsSourceStates.Gone),
            NewsStates.Published => query.Where(x => !x.IsArchived && x.SourceState == NewsSourceStates.Published),
            _ => query
        };

        if (dto.CategoryId is { } categoryId)
            query = query.Where(x => x.Categories.Any(c => c.Id == categoryId));

        if (dto.Edited == true) query = query.Where(NewsAdminProjection.Edited);
        else if (dto.Edited == false) query = query.Where(Not(NewsAdminProjection.Edited));

        if (dto.SourceUpdated == true) query = query.Where(NewsAdminProjection.StaleOverride);

        if (dto.Featured == true) query = NewsVisibility.Featured(query, now);
        else if (dto.Featured == false) query = NewsVisibility.NotFeatured(query, now);

        if (dto.From is { } from)
            query = query.Where(x => x.SourcePublishedAt >= from.Date);

        if (dto.To is { } to)
        {
            // "11 Ağustos"u seçen kişi o günün tamamını kasteder (12.1/12.2'deki aynı karar).
            var end = to.Date.AddDays(1);
            query = query.Where(x => x.SourcePublishedAt < end);
        }

        // Arama public uçla AYNI kuralı kullanır (`NewsSearch`): panelde bulunamayan bir
        // haberin vatandaşta bulunabilmesi (ya da tersi) sessiz bir tutarsızlık olurdu.
        var pattern = NewsSearch.Pattern(dto.Search);
        if (pattern is not null)
        {
            query = query.Where(x =>
                (x.TitleOverride != null && EF.Functions.Like(x.TitleOverride.ToLower(), pattern)) ||
                EF.Functions.Like(x.SourceTitle.ToLower(), pattern) ||
                EF.Functions.Like(x.SourcePlainText.ToLower(), pattern));
        }

        return query;
    }

    private static System.Linq.Expressions.Expression<Func<NewsArticle, bool>> Not(
        System.Linq.Expressions.Expression<Func<NewsArticle, bool>> expression)
        => System.Linq.Expressions.Expression.Lambda<Func<NewsArticle, bool>>(
            System.Linq.Expressions.Expression.Not(expression.Body), expression.Parameters);
}

/// <summary>Faz 12.13 — panelin haber ayrıntısı (<b>aynı</b> projeksiyon, gövde açık).</summary>
public record GetNewsAdminByIdQuery(Guid Id) : IRequest<NewsAdminDto?>;

public class GetNewsAdminByIdQueryHandler : IRequestHandler<GetNewsAdminByIdQuery, NewsAdminDto?>
{
    private readonly IUnitOfWork _uow;

    public GetNewsAdminByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NewsAdminDto?> Handle(GetNewsAdminByIdQuery request, CancellationToken ct)
    {
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();
        var now = DateTime.UtcNow;

        var dto = await _uow.Repository<NewsArticle>().Query()
            .Where(x => x.Id == request.Id)
            .Select(NewsAdminProjection.Select(users, includeContent: true))
            .FirstOrDefaultAsync(ct);

        return dto is null ? null : NewsAdminProjection.Finish(dto, now);
    }
}
