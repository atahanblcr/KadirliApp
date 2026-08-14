using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal.Queries;

/// <summary>
/// Faz 12.16 — <c>GET /v1/users/me/consents</c>: "neyi onayladım, ne zaman, hangi sürümü?"
/// </summary>
/// <remarks>
/// 🔑 Liste <b>yayında olan her belgeyi</b> içerir — kullanıcının hiç karar vermediklerini de
/// (<c>ConsentedVersionId = null</c>). Yalnız rıza satırı olanlar dönseydi ayarlar ekranı,
/// kullanıcının <b>hiç sorulmamış</b> bir izni (ör. ticari ileti) göremez ve onu verme
/// imkânı da olmazdı: "işlevsiz buton yok" kuralının tersi — <b>hiç var olmayan buton</b>.
/// </remarks>
public record GetMyConsentsQuery(Guid UserId) : IRequest<List<MyConsentDto>>;

public class GetMyConsentsQueryHandler : IRequestHandler<GetMyConsentsQuery, List<MyConsentDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMyConsentsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<MyConsentDto>> Handle(GetMyConsentsQuery request, CancellationToken ct)
    {
        var documents = await LegalConsentRules
            .Available(_uow.Repository<LegalDocument>().Query().Include(d => d.Versions))
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Title)
            .ToListAsync(ct);

        var versionIds = documents.SelectMany(d => d.Versions).Select(v => v.Id).ToList();

        var consents = await _uow.Repository<UserConsent>().Query()
            .Where(c => c.UserId == request.UserId && versionIds.Contains(c.DocumentVersionId))
            .ToListAsync(ct);

        var result = new List<MyConsentDto>();

        foreach (var document in documents)
        {
            var live = LegalConsentRules.LiveVersionOf(document)!;

            // ⚠️ Kullanıcı eski bir sürüme onay vermiş olabilir; "en güncel kararı" bulmak
            // için sürüm numarasına göre en yükseği alıyoruz. Yalnız yayındaki sürüme
            // bakılsaydı, v1'i onaylamış bir kullanıcı v2 yayınlandığı an ekranda
            // "hiç karar vermemiş" görünürdü — ve verdiği rıza yok sayılmış gibi olurdu.
            var decision = consents
                .Where(c => document.Versions.Any(v => v.Id == c.DocumentVersionId))
                .OrderByDescending(c => document.Versions.First(v => v.Id == c.DocumentVersionId).VersionNumber)
                .FirstOrDefault();

            var decidedVersion = decision is null
                ? null
                : document.Versions.First(v => v.Id == decision.DocumentVersionId);

            result.Add(new MyConsentDto
            {
                Type = document.Type,
                Title = document.Title,
                IsMandatory = document.IsMandatory,
                CurrentVersionId = live.Id,
                CurrentVersionNumber = live.VersionNumber,
                ConsentedVersionId = decidedVersion?.Id,
                ConsentedVersionNumber = decidedVersion?.VersionNumber,
                Granted = decision?.Granted ?? false,
                DecidedAt = decision?.DecidedAt,
                RevokedAt = decision?.RevokedAt,
                NeedsReconsent = LegalProjection.NeedsReconsent(
                    document, decidedVersion?.VersionNumber, decision?.Granted ?? false)
            });
        }

        return result;
    }
}
