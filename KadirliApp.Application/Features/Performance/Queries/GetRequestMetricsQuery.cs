using KadirliApp.Application.Common.Performance;
using MediatR;

namespace KadirliApp.Application.Features.Performance.Queries;

/// <summary>
/// Faz 12.22a — panelin performans tablosunu besleyen sorgu.
/// </summary>
/// <remarks>
/// 📌 Veritabanına hiç dokunmaz: ölçüm Redis'te ve süreç belleğinde yaşar. Yine de bir
/// MediatR sorgusu, çünkü panelin okuma yolu <b>tek</b> olmalı — controller'ın doğrudan
/// <c>IRequestMetricsReader</c> alması, boru hattının (yetki/denetim/ölçüm) dışına çıkan
/// ikinci bir yol açardı.
/// ⚠️ <b>Bu sorgu bilerek cache'lenmiyor:</b> cache'lenmiş bir ölçüm ekranı, ölçtüğü
/// sistemin şu anki hâlini değil <i>bir dakika önceki</i> hâlini gösterirdi ve tam da
/// "yavaşladı mı?" diye bakılan anda yanıltırdı.
/// </remarks>
public sealed record GetRequestMetricsQuery : IRequest<RequestMetricsSnapshot>;

public sealed class GetRequestMetricsQueryHandler
    : IRequestHandler<GetRequestMetricsQuery, RequestMetricsSnapshot>
{
    private readonly IRequestMetricsReader _reader;

    public GetRequestMetricsQueryHandler(IRequestMetricsReader reader) => _reader = reader;

    public Task<RequestMetricsSnapshot> Handle(GetRequestMetricsQuery request, CancellationToken ct)
        => _reader.ReadAsync(ct);
}
