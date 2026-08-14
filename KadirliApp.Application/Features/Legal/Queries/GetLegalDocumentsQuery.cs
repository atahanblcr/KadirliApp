using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal.Queries;

/// <summary>
/// Faz 12.16 — <c>GET /v1/legal/documents</c> (<b>anonim</b>).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Anonim olmak ZORUNDA:</b> bu ucu çağıran henüz kayıtlı değildir — rızayı vermeden
/// önce metni okuması gerekiyor. <c>EndpointAuthorizationSweepTests</c>'in anonim listesine
/// <b>bilinçli</b> eklendi.
/// </para>
/// <para>
/// ⚠️ <b>ÖNBELLEKLENMİYOR ve bu bilinçli bir sapma</b> (projede benzer sözlük uçları
/// önbellekli). Sebep: bu uç bir <i>hukuki metin</i> döndürüyor ve önbellek burada
/// §7 madde 22'nin hasarını en kötü biçimde üretirdi — yönetici yeni sürümü yayınlar,
/// vatandaş 15 dakika boyunca <b>yürürlükten kalkmış metni</b> okur ve <b>ona</b> rıza
/// verir. Rıza sürüme bağlandığı için kayıt "eski sürüme rıza" der: teknik olarak tutarlı,
/// hukuken yanlış. Uç kayıt akışında <b>bir kez</b> çağrılıyor; kazanç ihmal edilebilir,
/// bedel geri alınamaz. 📌 Bir grup açıp her mutasyona invalidator yazmak da bir sonraki
/// komutta unutulabilirdi — kural testi *"her grubun bir invalidator'ı var mı"*ya bakar,
/// *"her mutasyon invalidate ediyor mu"*ya değil.
/// </para>
/// </remarks>
public record GetLegalDocumentsQuery : IRequest<List<LegalDocumentDto>>
{
    /// <summary>
    /// Yalnız kayıt ekranında gösterilecekler mi? Ayarlar ekranı <c>false</c> ile çağırır
    /// (yayında olan <b>her</b> belgeyi okuyabilmeli).
    /// </summary>
    public bool RegistrationOnly { get; init; }
}

public class GetLegalDocumentsQueryHandler : IRequestHandler<GetLegalDocumentsQuery, List<LegalDocumentDto>>
{
    private readonly IUnitOfWork _uow;

    public GetLegalDocumentsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<List<LegalDocumentDto>> Handle(GetLegalDocumentsQuery request, CancellationToken ct)
    {
        var source = _uow.Repository<LegalDocument>().Query().Include(d => d.Versions);

        var documents = await (request.RegistrationOnly
                ? LegalConsentRules.AtRegistration(source)
                : LegalConsentRules.Available(source))
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Title)
            .ToListAsync(ct);

        return documents.Select(LegalProjection.ToDto).Where(d => d is not null).ToList()!;
    }
}

/// <summary>
/// Faz 12.16 — <c>GET /v1/legal/documents/{type}</c> (<b>anonim</b>): tek belge.
/// </summary>
/// <remarks>
/// ⚠️ Tanınmayan tür <b>varsayılana düşmez</b>, <c>null</c> döner (<c>LegalDocumentTypes</c>):
/// yanlış hukuki metni göstermek, kullanıcıya okumadığı bir belgeyi onaylatmanın en sessiz
/// yoludur.
/// </remarks>
public record GetLegalDocumentByTypeQuery(string Type) : IRequest<LegalDocumentDto?>;

public class GetLegalDocumentByTypeQueryHandler : IRequestHandler<GetLegalDocumentByTypeQuery, LegalDocumentDto?>
{
    private readonly IUnitOfWork _uow;

    public GetLegalDocumentByTypeQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<LegalDocumentDto?> Handle(GetLegalDocumentByTypeQuery request, CancellationToken ct)
    {
        var type = LegalDocumentTypes.Normalize(request.Type);
        if (type is null) return null;

        var document = await LegalConsentRules
            .Available(_uow.Repository<LegalDocument>().Query().Include(d => d.Versions))
            .FirstOrDefaultAsync(d => d.Type == type, ct);

        return document is null ? null : LegalProjection.ToDto(document);
    }
}
