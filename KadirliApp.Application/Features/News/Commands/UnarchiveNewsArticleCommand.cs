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
/// Faz 12.12 — arşivlenmiş haberi geri yayına alır.
/// </summary>
/// <remarks>
/// ⚠️ Geri alma <b>kaynağın durumuna dokunmaz</b>: kaynakta kalkmış (<c>gone</c>) bir haber
/// arşivden çıkarılsa bile public uçta görünmez (<c>NewsVisibility</c>). Aksi olsaydı panel
/// "yayına aldım" der, vatandaş hiçbir şey görmez ve kimse hata almazdı — 12.10'un
/// "geri getirme, yayına alma değildir" kuralının (§7 madde 28) bu modüldeki karşılığı.
/// </remarks>
public class UnarchiveNewsArticleCommand : IRequest<ApiResponse<bool>>, IAuditableCommand, ICacheInvalidator
{
    public Guid Id { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "unarchive";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(NewsArticle);

    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.News };
}

public class UnarchiveNewsArticleCommandHandler : IRequestHandler<UnarchiveNewsArticleCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public UnarchiveNewsArticleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(UnarchiveNewsArticleCommand request, CancellationToken ct)
    {
        var article = await _uow.Repository<NewsArticle>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (article is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Haber bulunamadı.");

        article.Unarchive();
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
