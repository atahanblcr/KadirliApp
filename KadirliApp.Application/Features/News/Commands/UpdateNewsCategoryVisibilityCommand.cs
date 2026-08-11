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
/// Faz 12.12 — kategori görünürlüğü (dışlama · şeritte göster · sıra).
/// </summary>
/// <remarks>
/// 🔴 <b>Dışlama geriye dönüktür ve anında etkilidir:</b> "E-Gazete"yi dışlayan yönetici,
/// o kategorideki <b>366 eski haberin</b> de listeden düşmesini bekler — kayıt kayıt
/// arşivlemek zorunda kalmamalı. Bu yüzden süzgeç sorguda (<c>NewsVisibility</c>) yaşıyor,
/// kayıtlara yazılan bir bayrakta değil.
///
/// 📌 <b>Silme yok</b> (<c>LookupsAdmin</c> kuralı): kaynak kategoriyi kaldırsa bile satır
/// kalır — ona bağlı haberlerin geçmişi kaybolmasın.
/// </remarks>
public class UpdateNewsCategoryVisibilityCommand : IRequest<ApiResponse<bool>>, IAuditableCommand, ICacheInvalidator
{
    public Guid Id { get; set; }
    public bool IsExcluded { get; set; }
    public bool ShowInFilterStrip { get; set; } = true;
    public int DisplayOrder { get; set; }

    public string AuditModule => NewsAudit.Module;
    public string AuditAction => "update";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(NewsCategory);

    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.News };
}

public class UpdateNewsCategoryVisibilityCommandHandler
    : IRequestHandler<UpdateNewsCategoryVisibilityCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateNewsCategoryVisibilityCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(UpdateNewsCategoryVisibilityCommand request, CancellationToken ct)
    {
        var category = await _uow.Repository<NewsCategory>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (category is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Kategori bulunamadı.");

        category.SetVisibility(request.IsExcluded, request.ShowInFilterStrip, request.DisplayOrder);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
