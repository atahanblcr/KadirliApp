using System;
using System.Collections.Generic;
using System.Linq;
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
/// Faz 12.12 — haberi uygulamadan <b>geri alınabilir</b> biçimde kaldırır.
/// </summary>
/// <remarks>
/// 🔑 <b>Bu modülde moderasyon bilinçli olarak YOK.</b> Kaynak bizim ve günde ~5 haber
/// geliyor; her haberi tek tek onaylatmak yayını saatlerce geciktirir, bir "onay kuyruğu"
/// da kimsenin bakmadığı bir ekrana dönerdi. Onay yerine <b>otomatik yayın + geri alınabilir
/// gizleme</b> seçildi.
///
/// ⚠️ <b>Dosya adı bilerek <c>Archive*</c>, <c>Approve*</c> DEĞİL.</b>
/// <c>ModerationSingleOwnerTests.ModeratedModules()</c> moderasyonlu modül kümesini
/// <c>Features/&lt;M&gt;/</c> altında <b><c>Approve*.cs</c> dosyası var mı</b> diye türetiyor;
/// buraya o adla bir dosya konduğu an panel controller'ı, <c>_ModerationStatusField</c>,
/// <c>ModerationStatusGuard</c> çağrısı ve beş moderasyon alanının <c>init</c> olması
/// <b>zorunlu hâle gelir</b> — hepsi bu modülde karşılığı olmayan şeyler.
/// </remarks>
public class ArchiveNewsArticleCommand : IRequest<ApiResponse<bool>>, IAuditableCommand, ICacheInvalidator
{
    public Guid Id { get; set; }
    public string? Reason { get; set; }

    /// <summary>Butona basan yönetici (panel 12.13'te dolduracak).</summary>
    public Guid? AdminId { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "archive";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(NewsArticle);

    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.News };
}

public class ArchiveNewsArticleCommandHandler : IRequestHandler<ArchiveNewsArticleCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public ArchiveNewsArticleCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(ArchiveNewsArticleCommand request, CancellationToken ct)
    {
        // ⚠️ Query() varsayılan olarak AsNoTracking döner (§ checklist, 12.3 canlı bulgusu):
        // tracking istenmezse yazılan alanlar SaveChanges'e hiç ulaşmaz ve hiçbir hata olmaz.
        var article = await _uow.Repository<NewsArticle>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (article is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Haber bulunamadı.");

        article.Archive(request.Reason, request.AdminId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
