using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads.Commands.TrackAdContact;

public enum AdContactChannel
{
    Phone,
    Whatsapp
}

/// <summary>
/// Faz 10.6: ilan iletişim tıklama sayacı (masterclass §13.1 track-phone/track-whatsapp — anonim).
/// GetAdById'nin view_count deseni: tracked entity değil tek atomik ExecuteUpdate — yarışta kayıp artış olmaz,
/// hiçbir cache grubunu da invalide etmez (public liste cache'siz, sayaç değişimi listeyi bozmaz).
/// </summary>
public record TrackAdContactCommand(Guid AdId, AdContactChannel Channel) : IRequest<bool>;

public class TrackAdContactCommandHandler : IRequestHandler<TrackAdContactCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public TrackAdContactCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(TrackAdContactCommand request, CancellationToken cancellationToken)
    {
        // Yalnız yayında görünür ilanlar: liste/detay kuralıyla aynı (approved + süresi geçmemiş) —
        // iletişim bilgisi yalnız onlarda istemciye gider, sayaç da yalnız onlar için anlamlı
        // (soft-delete query filter zaten aktif; ExpiresAt kontrolü job gecikmesine karşı).
        var query = _uow.Repository<Ad>().Query()
            .Where(a => a.Id == request.AdId && a.Status == "approved" && a.ExpiresAt > DateTime.UtcNow);

        var affected = request.Channel == AdContactChannel.Phone
            ? await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.PhoneClickCount, a => a.PhoneClickCount + 1), cancellationToken)
            : await query.ExecuteUpdateAsync(s => s.SetProperty(a => a.WhatsappClickCount, a => a.WhatsappClickCount + 1), cancellationToken);

        if (affected == 0)
            throw new NotFoundException(nameof(Ad), request.AdId);

        return true;
    }
}
