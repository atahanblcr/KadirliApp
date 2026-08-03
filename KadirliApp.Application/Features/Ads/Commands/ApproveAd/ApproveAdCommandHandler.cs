using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.ApproveAd;

public class ApproveAdCommandHandler : IRequestHandler<ApproveAdCommand, bool>
{
    /// <summary>
    /// Süresi dolmuş bir ilan onaylandığında verilen yeni yayın süresi.
    /// <c>CreateAdCommandHandler</c>'ın ilk yayın süresi ve <c>ExtendMyAdCommand</c>'in
    /// uzatma süresiyle aynı: 30 gün.
    /// </summary>
    private const int PublishDays = 30;

    private readonly IUnitOfWork _uow;

    public ApproveAdCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(ApproveAdCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<Ad>();
        var ad = await repo.GetByIdAsync(request.AdId, cancellationToken);

        if (ad == null) return false;

        // 🔴 Faz 11.15c: ONAY, İLANI GERÇEKTEN GÖRÜNÜR KILMALI.
        //
        // Canlı denetimde görülen çelişki: süresi geçmiş (expired) bir ilan panelden
        // onaylandığında "İlan başarıyla onaylandı." yazıyor, ama ExpiresAt geçmişte
        // kaldığı için mobil listede HİÇ görünmüyor (GetAdsQueryHandler:32) ve saatlik
        // ExpireAdsJob durumu sessizce yeniden "expired" yapıyor. Yönetici ile vatandaş
        // farklı gerçeklik görüyordu.
        //
        // Aynı sessiz hata expired olmayan ilanlarda da vardı: onay kuyruğunda 30 günden
        // fazla bekleyen "pending" bir ilan onaylandığı anda süresi dolmuş oluyordu.
        // Bu yüzden koşul duruma değil, TARİHE bakıyor.
        //
        // Karar: yayın penceresi ilanın gönderildiği an değil, GÖRÜNÜR OLDUĞU an başlar.
        var now = DateTime.UtcNow;
        if (ad.ExpiresAt <= now)
            ad.ExpiresAt = now.AddDays(PublishDays);

        // Faz 10.14(1) yan düzeltmesi: reddedilmiş bir ilan sonradan onaylanırsa bayat red gerekçesi kalmasın.
        ad.Status = "approved";
        ad.ApprovedBy = request.AdminId;
        ad.ApprovedAt = DateTime.UtcNow;
        ad.RejectedReason = null;
        ad.RejectedAt = null;

        repo.Update(ad);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
