using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal.Queries;

/// <summary>
/// Faz 12.16 — <b>rıza defteri</b>: kim · hangi sürüm · ne zaman · nereden.
/// </summary>
/// <remarks>
/// ⚠️ <b>Yalnız admin</b> (<c>AdminOnlyControllers</c> deseni, §3): satırlar <b>IP adresi ve
/// tarayıcı imzası</b> taşıyor — "kim nereden onayladı" moderatöre dağıtılabilir bir yetki
/// değil (12.2'nin <c>LoginAttemptsAdmin</c> kararının birebir aynısı).
/// </remarks>
public class GetConsentLedgerQuery : IRequest<PagedResult<ConsentLedgerRowDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Belge türüne göre süz (<c>LegalDocumentTypes</c>).</summary>
    public string? Type { get; set; }

    /// <summary>Yalnız onaylayanlar / yalnız reddedenler. <c>null</c> = hepsi.</summary>
    public bool? Granted { get; set; }

    /// <summary>Tek bir kullanıcının rıza geçmişi (panelde kullanıcı satırından gelinir).</summary>
    public Guid? UserId { get; set; }
}

public class GetConsentLedgerQueryHandler : IRequestHandler<GetConsentLedgerQuery, PagedResult<ConsentLedgerRowDto>>
{
    private readonly IUnitOfWork _uow;

    public GetConsentLedgerQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<ConsentLedgerRowDto>> Handle(GetConsentLedgerQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var query = _uow.Repository<UserConsent>().Query()
            .Include(c => c.DocumentVersion!).ThenInclude(v => v.Document!)
            .Include(c => c.User!)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = Domain.Enums.LegalDocumentTypes.Normalize(request.Type);
            // Tanınmayan tür SÜZMEZ (§5: bir yazım hatası listeyi boşaltmamalı).
            if (type is not null)
                query = query.Where(c => c.DocumentVersion!.Document!.Type == type);
        }

        if (request.Granted is not null)
            query = query.Where(c => c.Granted == request.Granted);

        if (request.UserId is not null)
            query = query.Where(c => c.UserId == request.UserId);

        var total = await query.CountAsync(ct);

        // ⚠️ Sıralama BENZERSİZ bir ayraçla biter (§7 madde 30): aynı saniyede yazılmış
        // rıza satırları eşit değerlidir ve Postgres sırayı garanti etmez — sayfalı defterde
        // aynı satır iki sayfada görünüp bir başkası hiç görünmezdi.
        var items = await query
            .OrderByDescending(c => c.DecidedAt)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(c => new ConsentLedgerRowDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Username = c.User!.Username,
                Phone = c.User!.Phone,
                DocumentTitle = c.DocumentVersion!.Document!.Title,
                DocumentType = c.DocumentVersion!.Document!.Type,
                VersionNumber = c.DocumentVersion!.VersionNumber,
                Granted = c.Granted,
                DecidedAt = c.DecidedAt,
                RevokedAt = c.RevokedAt,
                IpAddress = c.IpAddress == null ? null : c.IpAddress.ToString(),
                UserAgent = c.UserAgent,
                Source = c.Source
            })
            .ToListAsync(ct);

        return new PagedResult<ConsentLedgerRowDto>
        {
            Items = items,
            TotalCount = total,
            PageSize = size,
            CurrentPage = page
        };
    }
}
