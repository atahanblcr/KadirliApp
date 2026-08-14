using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Legal.Dtos;
using KadirliApp.Domain.Enums;
using MediatR;

namespace KadirliApp.Application.Features.Legal.Commands;

/// <summary>
/// Faz 12.16 — <c>POST /v1/users/me/consents</c>: isteğe bağlı rızayı ver/geri al ve
/// yeniden onay akışını tamamla.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Denetim izi (<c>IAuditableCommand</c>) bilinçli olarak YOK.</b> <c>audit_logs</c>
/// bu projede <b>panel/yönetici</b> eylemlerinin defteridir ("kim ne yaptı" — personel);
/// vatandaşın kendi rızası zaten <c>user_consents</c>'te <b>zamanı, IP'si ve kaynağıyla</b>
/// duruyor. İkinci bir deftere yazmak aynı olayın iki sahibi olurdu (onaylanan planın
/// <i>"ikinci sahip yasağı"</i> kısıtı) ve denetim izi ekranı vatandaş trafiğiyle dolardı.
/// </para>
/// <para>
/// 🔴 <b>Zorunlu rıza buradan geri ALINAMAZ</b> — karşılığı var olan
/// <c>DELETE /v1/users/me</c>'dir (10.8). Yeni bir yol açılmadı: geri alınabilseydi
/// hesabı duran ama dayanağı olmayan bir kullanıcı doğardı.
/// </para>
/// </remarks>
public class SaveMyConsentsCommand : IRequest<ApiResponse<bool>>
{
    public Guid UserId { get; set; }
    public List<ConsentDecisionDto> Consents { get; set; } = new();

    /// <summary>⚠️ Sunucuda doldurulur (controller), istemciden gelmez.</summary>
    public IPAddress? IpAddress { get; set; }

    /// <summary>⚠️ Sunucuda doldurulur (controller), istemciden gelmez.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Yeniden onay akışından mı geliyor? ⚠️ İstemci bunu <b>söyleyebilir</b> ama
    /// <see cref="ConsentSources"/> değerine <b>çeviren sunucudur</b>: serbest metin kabul
    /// edilseydi defterdeki "nasıl alındı" sütunu istemcinin yazdığı her şeyi taşırdı.
    /// </summary>
    public bool IsReconsent { get; set; }
}

public class SaveMyConsentsCommandHandler : IRequestHandler<SaveMyConsentsCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public SaveMyConsentsCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(SaveMyConsentsCommand request, CancellationToken ct)
    {
        var context = new ConsentContext(
            request.IsReconsent ? ConsentSources.Reconsent : ConsentSources.Settings,
            request.IpAddress,
            request.UserAgent);

        var result = await LegalConsentWriter.ApplyAsync(
            _uow, request.UserId, request.Consents, context, DateTime.UtcNow, ct);

        if (!result.IsValid)
            return ApiResponse<bool>.FailureResponse(
                "MANDATORY_CONSENT",
                $"\"{result.MissingDocumentTitle}\" zorunludur ve buradan geri alınamaz. " +
                "Bu rızayı geri almak istiyorsanız hesabınızı silmeniz gerekir.");

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.SuccessResponse(true);
    }
}
