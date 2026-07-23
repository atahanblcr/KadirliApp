using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Taxis.Commands.CallTaxiDriver;

public record TaxiCallResultDto(string Phone);

/// <summary>
/// Faz 10.12: POST /v1/taxis/drivers/{id}/call — taxi_calls'a iz + sürücünün total_calls sayacı,
/// yanıtta aranacak telefon. KARAR: [Authorize] (plandaki öneri izlendi — telefon numarası döndüğü
/// ve TaxiCall.PassengerId zorunlu olduğu için anonim çağrı yok). Her çağrı YENİ satırdır
/// (favori/view'ün aksine tekrarlanabilir eylem). Görünürlük public kuralla aynı:
/// yalnız doğrulanmış + aktif sürücü aranabilir, diğerine 404.
/// </summary>
public record CallTaxiDriverCommand(Guid DriverId, Guid PassengerId) : IRequest<TaxiCallResultDto>;

public class CallTaxiDriverCommandHandler : IRequestHandler<CallTaxiDriverCommand, TaxiCallResultDto>
{
    private readonly IUnitOfWork _uow;

    public CallTaxiDriverCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<TaxiCallResultDto> Handle(CallTaxiDriverCommand request, CancellationToken cancellationToken)
    {
        var drivers = _uow.Repository<TaxiDriver>();
        var driver = await drivers.Query()
            .Where(d => d.Id == request.DriverId && d.IsVerified && d.IsActive)
            .Select(d => new { d.Id, d.Phone })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(TaxiDriver), request.DriverId);

        await _uow.Repository<TaxiCall>().AddAsync(new TaxiCall
        {
            DriverId = request.DriverId,
            PassengerId = request.PassengerId,
            CalledAt = DateTime.UtcNow
        }, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Sayaç atomik ExecuteUpdate — yarışta kayıp artış yok; cache invalidation tetiklemez (taxis cache'siz).
        await drivers.Query()
            .Where(d => d.Id == request.DriverId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.TotalCalls, d => d.TotalCalls + 1), cancellationToken);

        return new TaxiCallResultDto(driver.Phone);
    }
}
