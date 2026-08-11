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
using File = KadirliApp.Domain.Entities.File;

namespace KadirliApp.Application.Features.News.Commands;

/// <summary>
/// Faz 12.12 — yöneticinin özelleştirmeleri (başlık · özet · kapak görseli).
/// </summary>
/// <remarks>
/// 🔴 <b>Bu komut kaynağın alanlarına YAZAMAZ — derleyici engeller.</b> <c>Source*</c> alanları
/// <c>init</c> ve yalnız <c>NewsArticle.ApplySourceSnapshot</c> onlara dokunuyor; buradan
/// <c>article.SourceTitle = …</c> yazmayı denemek <b>CS8852</b> derleme hatasıdır.
/// Simetrik olarak senkron da bu alanları göremez.
///
/// 🔑 <b>Boş değer = override'ı kaldır.</b> "Boş başlık" diye bir override yok; alan
/// temizlendiğinde kayıt <b>deterministik biçimde kaynağa döner</b> (kilit bayrağı
/// tasarımının "geri alınca ne olacağı belirsiz" sorunu tam da bu yüzden reddedilmişti).
///
/// ⚠️ Gövde override'ı <b>bilinçli olarak yok</b> (ikinci sürüm adayı): tam gövde override'ı,
/// kaynaktaki bir düzeltmeyi sonsuza kadar görmezden gelen bir kopya üretirdi. Yönetici
/// gövdeyi düzeltmek istiyorsa doğru yer <b>kaynağın kendisi</b> — o da bizim.
/// </remarks>
public class UpdateNewsOverridesCommand : IRequest<ApiResponse<bool>>, IAuditableCommand, ICacheInvalidator
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Excerpt { get; set; }
    public Guid? CoverImageFileId { get; set; }
    public Guid? AdminId { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "update";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(NewsArticle);

    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.News };
}

public class UpdateNewsOverridesCommandHandler : IRequestHandler<UpdateNewsOverridesCommand, ApiResponse<bool>>
{
    /// <summary>Başlık override'ı için üst sınır — kaynağın başlıkları 60–120 karakter bandında.</summary>
    public const int MaxTitleLength = 250;
    public const int MaxExcerptLength = 600;

    private readonly IUnitOfWork _uow;

    public UpdateNewsOverridesCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(UpdateNewsOverridesCommand request, CancellationToken ct)
    {
        if (request.Title is { Length: > MaxTitleLength })
            return ApiResponse<bool>.FailureResponse("VALIDATION", $"Başlık en fazla {MaxTitleLength} karakter olabilir.");

        if (request.Excerpt is { Length: > MaxExcerptLength })
            return ApiResponse<bool>.FailureResponse("VALIDATION", $"Özet en fazla {MaxExcerptLength} karakter olabilir.");

        var article = await _uow.Repository<NewsArticle>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (article is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Haber bulunamadı.");

        if (request.CoverImageFileId.HasValue)
        {
            // Var olmayan bir dosya kimliği FK ihlaliyle 500 üretirdi; kullanıcıya Türkçe
            // sebep söylemek (Değişmez Kural #6) ve kaydı ezmemek gerekiyor.
            var exists = await _uow.Repository<File>().Query()
                .AnyAsync(f => f.Id == request.CoverImageFileId.Value, ct);

            if (!exists)
                return ApiResponse<bool>.FailureResponse("VALIDATION", "Seçilen kapak görseli bulunamadı.");
        }

        article.SetOverrides(request.Title, request.Excerpt, request.CoverImageFileId, request.AdminId, DateTime.UtcNow);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
