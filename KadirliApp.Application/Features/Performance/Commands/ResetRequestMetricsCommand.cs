using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Performance;
using MediatR;

namespace KadirliApp.Application.Features.Performance.Commands;

/// <summary>
/// Faz 12.22a — bütün ölçüm sayaçlarını sıfırlar (<b>temiz sayfa</b>).
/// </summary>
/// <remarks>
/// 🔑 <b>Var olma sebebi taban çizgisidir.</b> 12.22'nin başarı ölçütü bir cümledir:
/// <i>"en sıcak beş ucun p95'i şudur."</i> O cümle ancak ölçümün <b>ne zaman
/// başladığı</b> biliniyorsa kurulabilir; açılıştan beri biriken sayaçlar, aradaki
/// migration'ı ve ilk istekteki JIT ısınmasını da p95'e karıştırır.
/// 🔴 <b>Denetim izine düşer</b> (<c>IAuditableCommand</c>): ölçüm silmek geri alınamaz
/// ve "dün p95 şuydu" diyen birine "kim sıfırladı?" sorusunun cevabı gerekir.
/// ⚠️ <c>AuditModule</c> matris dışı bir anahtardır — Türkçe karşılığı
/// <c>PanelDisplay.NonMatrixModules</c>'ta olmak zorunda, yoksa denetim izi ekranı modül
/// sütununa ham İngilizce basar (Değişmez Kural #6).
/// </remarks>
public sealed record ResetRequestMetricsCommand : IRequest<bool>, IAuditableCommand
{
    public string AuditModule => PerformanceAudit.Module;
    public string AuditAction => "reset";
}

public sealed class ResetRequestMetricsCommandHandler : IRequestHandler<ResetRequestMetricsCommand, bool>
{
    private readonly IRequestMetricsReader _reader;

    public ResetRequestMetricsCommandHandler(IRequestMetricsReader reader) => _reader = reader;

    public async Task<bool> Handle(ResetRequestMetricsCommand request, CancellationToken ct)
    {
        await _reader.ResetAsync(ct);
        return true;
    }
}
