using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.ExtendMyAd;

/// <summary>
/// Faz 10.6: ilan süresi uzatma. Ücretli/reklamlı senaryo henüz yok — süre handler'da sabit 30 gün
/// (10.3'teki username değişim kuralı emsali: config yerine const). AdsWatched, mobilin "reklam izle,
/// süre uzat" akışı için entity'de duran alan; şimdilik istemcinin bildirdiği değer kaydedilir.
/// </summary>
public record ExtendMyAdCommand(Guid AdId, Guid UserId, int AdsWatched = 0) : IRequest<ExtendAdResultDto>;

public record ExtendAdResultDto(
    Guid AdId,
    string Status,
    DateTime ExpiresAt,
    int ExtensionCount,
    int MaxExtensions,
    int RemainingExtensions
);

public class ExtendMyAdCommandHandler : IRequestHandler<ExtendMyAdCommand, ExtendAdResultDto>
{
    /// <summary>Her uzatmanın eklediği gün; ilk yayın süresiyle (30 gün) aynı.</summary>
    private const int ExtensionDays = 30;

    private readonly IUnitOfWork _uow;

    public ExtendMyAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ExtendAdResultDto> Handle(ExtendMyAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.AdId, cancellationToken);
        if (ad == null)
            throw new NotFoundException(nameof(Ad), request.AdId);
        if (ad.UserId != request.UserId)
            throw new ForbiddenException("Bu ilan üzerinde işlem yetkiniz yok.");

        // pending/rejected uzatılamaz: henüz yayınlanmamış ya da reddedilmiş içeriğin süresini uzatmak anlamsız.
        // expired uzatılabilir ve yeniden approved olur — içerik zaten moderasyondan geçmişti, yalnızca süresi doldu.
        if (ad.Status != "approved" && ad.Status != "expired")
            throw new ValidationException("Yalnız yayındaki veya süresi dolmuş ilanlar uzatılabilir.");

        if (ad.ExtensionCount >= ad.MaxExtensions)
            throw new ConflictException($"Uzatma hakkınız doldu (en fazla {ad.MaxExtensions} uzatma).");

        var now = DateTime.UtcNow;

        // 🔴 Faz 12.11: bu blok eskiden burada yazılıydı ve son satırı `ad.Status = "approved"`
        // idi — yani moderasyon durumunu yazan BEŞİNCİ yol. 12.10'un yapısal testi onu
        // görmüyordu (yalnız Update*/Approve*/Reject*/Archive* dosyalarını tarıyor,
        // ExtendMyAd* hiçbirine uymuyor): kayıt bozulmuyordu ama koruma tesadüfen çalışıyordu.
        // Geçiş `Ad.Extend`'e taşındı ve alan `init` olduğu için aynı satır artık DERLENMEZ.
        ad.Extend(ExtensionDays, now);

        await _uow.Repository<AdExtension>().AddAsync(new AdExtension
        {
            AdId = ad.Id,
            UserId = request.UserId,
            AdsWatched = Math.Max(0, request.AdsWatched),
            DaysExtended = ExtensionDays,
            ExtendedAt = now,
            CreatedAt = now
        }, cancellationToken);

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ExtendAdResultDto(
            ad.Id,
            ad.Status,
            ad.ExpiresAt,
            ad.ExtensionCount,
            ad.MaxExtensions,
            ad.MaxExtensions - ad.ExtensionCount
        );
    }
}
