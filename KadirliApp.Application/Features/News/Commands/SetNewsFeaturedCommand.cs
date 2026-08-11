using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Commands;

/// <summary>
/// Faz 12.12 — haberi <b>öne çıkarır</b> (manşet).
/// </summary>
/// <remarks>
/// 🔑 <see cref="FeaturedUntil"/> bilinçli: süresiz bir manşet, unutulduğu gün <b>bayat</b>
/// bir haberi aylarca en üstte tutar ve bunu kimse fark etmez (bu bloğun 1 numaralı hasar
/// sınıfının küçük kardeşi). Süre dolduğunda haber sessizce sıradan bir habere döner —
/// ⚠️ ama süzgeç <b>sunucuda</b> uygulanır (<c>NewsVisibility.Featured</c>), istemcide değil:
/// istemcide olsaydı mağazadaki eski sürümler süresi dolmuş manşeti göstermeye devam ederdi.
/// </remarks>
public class SetNewsFeaturedCommand : IRequest<ApiResponse<bool>>, IAuditableCommand, ICacheInvalidator
{
    public Guid Id { get; set; }
    public bool IsFeatured { get; set; }

    /// <summary>Bitiş anı (UTC); <c>null</c> = süresiz.</summary>
    public DateTime? FeaturedUntil { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "feature";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(NewsArticle);

    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.News };
}

public class SetNewsFeaturedCommandHandler : IRequestHandler<SetNewsFeaturedCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public SetNewsFeaturedCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(SetNewsFeaturedCommand request, CancellationToken ct)
    {
        if (request.IsFeatured && request.FeaturedUntil is { } until && until <= DateTime.UtcNow)
            return ApiResponse<bool>.FailureResponse("VALIDATION",
                "Öne çıkarma bitiş zamanı gelecekte olmalı; geçmiş bir tarih haberi hiç öne çıkarmaz.");

        var article = await _uow.Repository<NewsArticle>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (article is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Haber bulunamadı.");

        article.SetFeatured(request.IsFeatured, request.FeaturedUntil);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
