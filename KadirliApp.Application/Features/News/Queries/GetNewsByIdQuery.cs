using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Caching;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.News.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.News.Queries;

/// <summary>Faz 12.12 — <c>GET /v1/news/{id}</c>. Gövde <b>yalnız burada</b> döner.</summary>
/// <remarks>
/// 🔴 <b>Bilinçli olarak ÖNBELLEKLENMEZ</b> (12.12 sonrası denetim, bulgu 6). İlk yazımda
/// <c>news:detail:{id}</c> anahtarıyla önbellekleniyordu ve bu, diğer modüllerde görülmeyen
/// bir ölçek sorunu üretiyordu: anahtar sayısı <b>haber sayısı kadar</b>, yani 27k'ya kadar
/// büyüyor ve hepsi tek bir gruba (<c>news</c>) yazılıyordu. Grup kümesi her senkronda
/// (15 dk'da bir) baştan sona dolaşılıp siliniyor — yani önbellek, korumaya çalıştığı işten
/// <b>daha pahalı</b> hâle geliyordu. Diğer modüllerde kayıt sayısı küçük olduğu için aynı
/// desen bugüne kadar sorun çıkarmadı; burada çıkarır.
/// <para>
/// ⚠️ Bedeli ölçülü: detay sorgusu birincil anahtar üzerinden tek satır okur ve gövde zaten
/// yalnız burada taşınıyor. Liste (<c>news:list:*</c>) ve kategoriler önbellekli kalıyor —
/// yani <b>sayfa başına 20 kaydı</b> koruyan önbellek duruyor, tekil kaydınki kalkıyor.
/// </para>
/// </remarks>
public record GetNewsByIdQuery(Guid Id) : IRequest<NewsArticleDto?>;

public class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, NewsArticleDto?>
{
    private readonly IUnitOfWork _uow;

    public GetNewsByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<NewsArticleDto?> Handle(GetNewsByIdQuery request, CancellationToken ct)
    {
        // Görünürlük detayda da zorlanır: arşivlenmiş ya da kaynaktan kalkmış bir haberin
        // bağlantısı elde kalmış olabilir (paylaşılmış link, eski push bildirimi).
        // Proje deseni (EventsController): sorgu null döner, controller 404'e çevirir.
        return await NewsVisibility
            .Published(_uow.Repository<NewsArticle>().Query())
            .Where(x => x.Id == request.Id)
            .Select(NewsProjection.Select(includeContent: true))
            .FirstOrDefaultAsync(ct);
    }
}
