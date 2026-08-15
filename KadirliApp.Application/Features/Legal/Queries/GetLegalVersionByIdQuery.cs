using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal.Queries;

/// <summary>
/// Faz 12.17 (plan dışı ek) — <c>GET /v1/legal/versions/{id}</c> (<b>anonim</b>):
/// <b>belirli bir sürümün</b> metni.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Ucun varlık sebebi:</b> 12.16 rızayı sürüme bağladı ve rıza kaydı o sürümün
/// kimliğini taşıyor; ama o kimlikten <b>metne</b> giden bir yol yoktu. Sonucu: v2'yi
/// onaylayan bir vatandaş, yönetici v3'ü yayınladığı an <b>neyi kabul ettiğini bir daha
/// hiç göremiyordu</b>. Bloğun açılış cümlesi (<i>"kayıt duruyor, metin ortada yok"</i>)
/// vatandaş tarafında hâlâ geçerliydi — bu uç onu kapatıyor.
/// </para>
/// <para>
/// 🔴 <b>Taslak DÖNMEZ</b> (<c>PublishedAt == null</c> → <c>null</c> → uçta <c>404</c>).
/// İki sebep: henüz yayınlanmamış bir hukuki metin, kimliğini eline geçiren herkese
/// açılırdı <b>ve</b> kullanıcı onu yürürlükteki metin sanabilirdi. Rıza zaten yalnız
/// yayında sürüme yazılabildiği için (§7 madde 71) bu kısıt hiçbir meşru kullanımı
/// engellemiyor.
/// </para>
/// <para>
/// ⚠️ <b>Yürürlükten kalkmış sürüm BİLEREK dönüyor</b> — ucun bütün amacı o. Ayrım
/// <see cref="LegalVersionDto.IsLive"/> ile <b>veride</b> taşınıyor, çünkü ekranın
/// "bu metin artık yürürlükte değil" diyebilmesi bu bilgiye bağlı; istemci onu
/// <c>SupersededAt</c>'ten kendi türetseydi (§7 madde 43'ün sınıfı) iki sahip doğardı.
/// </para>
/// <para>
/// ⚠️ Uç <b>önbelleklenmiyor</b> — kardeşleriyle (<c>GET /v1/legal/documents</c>) aynı
/// gerekçe (12.16, karar 4).
/// </para>
/// <para>
/// ⚠️ Belgenin <c>IsActive</c>'ine <b>bilerek BAKILMIYOR</b> (kardeş uçların
/// <c>LegalConsentRules.Available</c> süzgecinin tersi): bu uç "bugün ne soruluyor?"u değil
/// <b>"ben neyi onaylamıştım?"</b>u cevaplıyor. Yönetici belgeyi pasifleştirdiğinde geçmişte
/// verilmiş rızanın metni <b>okunamaz hâle gelseydi</b>, kanıt tam da onu isteyen kişi için
/// kaybolurdu — üstelik tek bir panel anahtarıyla ve hiçbir uyarı olmadan.
/// </para>
/// </remarks>
public record GetLegalVersionByIdQuery(Guid VersionId) : IRequest<LegalVersionDto?>;

public class GetLegalVersionByIdQueryHandler
    : IRequestHandler<GetLegalVersionByIdQuery, LegalVersionDto?>
{
    private readonly IUnitOfWork _uow;

    public GetLegalVersionByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<LegalVersionDto?> Handle(GetLegalVersionByIdQuery request, CancellationToken ct)
    {
        if (request.VersionId == Guid.Empty) return null;

        var version = await _uow.Repository<LegalDocumentVersion>().Query()
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.Id == request.VersionId, ct);

        // 🔴 Taslak "bulunamadı"dır: var olduğunu bile söylemiyoruz.
        if (version?.Document is null || version.PublishedAt is null) return null;

        return new LegalVersionDto
        {
            Id = version.Id,
            DocumentType = version.Document.Type,
            DocumentTitle = version.Document.Title,
            VersionNumber = version.VersionNumber,
            Summary = version.Summary,
            Body = version.Body,
            EffectiveFrom = version.EffectiveFrom,
            PublishedAt = version.PublishedAt.Value,
            IsLive = version.IsLive,
            SupersededAt = version.SupersededAt
        };
    }
}
