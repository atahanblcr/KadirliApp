using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Staff.Queries;

/// <summary>
/// Faz 12.2 — oturumu açık panel kullanıcısının kayıtlı e-posta adresi.
///
/// Tek kullanıcısı <c>LoginAttemptsAdmin/SendTestAlert</c>: uyarı kanalını denerken
/// alıcı <b>her zaman kişinin kendisidir</b>. Serbest bir alıcı alanı olsaydı panel bir
/// spam aracına dönerdi; bu sorgu o kısıtı <b>kod düzeyinde</b> zorunlu kılıyor —
/// controller'ın başka bir adres seçme imkânı yok.
/// </summary>
/// <param name="UserIdClaim">Çerezden gelen ham kimlik değeri; ayrıştırılamazsa <c>null</c> döner.</param>
public record GetCurrentPanelUserEmailQuery(string? UserIdClaim) : IRequest<string?>;

public class GetCurrentPanelUserEmailQueryHandler : IRequestHandler<GetCurrentPanelUserEmailQuery, string?>
{
    private readonly IUnitOfWork _uow;

    public GetCurrentPanelUserEmailQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<string?> Handle(GetCurrentPanelUserEmailQuery request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.UserIdClaim, out var userId))
            return null;

        return await _uow.Repository<User>().Query()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(ct);
    }
}
