using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.ErrorLogs.Queries;

/// <summary>
/// Faz 12.1 — dashboard rozetinin sayacı: son <paramref name="Hours"/> saatte görülmüş,
/// <b>hâlâ açık</b> hata kaydı sayısı.
///
/// ⚠️ Neden <see cref="GetErrorLogsQuery"/> yeniden kullanılmadı: o sorgunun tarih süzgeci
/// <c>from.Date</c> ile <b>gün başına</b> yuvarlanıyor (panelde tarih seçici gün bazlı).
/// "Son 24 saat" için kullanılsaydı aslında 24–48 saatlik bir pencere sayardı ve rozet
/// sessizce yanlış olurdu.
///
/// ⚠️ Ölçüt <c>LastSeenAt</c>: tekilleştirme yüzünden kayıt aylar önce açılmış olabilir
/// ama bugün hâlâ tekrar ediyorsa "son 24 saatte" sayılmalıdır.
/// </summary>
public record GetOpenErrorCountQuery(int Hours = 24) : IRequest<int>;

public class GetOpenErrorCountQueryHandler : IRequestHandler<GetOpenErrorCountQuery, int>
{
    private readonly IUnitOfWork _uow;

    public GetOpenErrorCountQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(GetOpenErrorCountQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Abs(request.Hours));

        return await _uow.Repository<ErrorLog>().Query()
            .Where(x => !x.IsResolved && x.LastSeenAt >= since)
            .CountAsync(ct);
    }
}
