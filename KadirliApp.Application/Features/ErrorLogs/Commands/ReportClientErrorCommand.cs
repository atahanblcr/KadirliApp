using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Observability;
using KadirliApp.Application.Features.ErrorLogs.Dtos;
using MediatR;

namespace KadirliApp.Application.Features.ErrorLogs.Commands;

/// <summary>
/// Faz 12.1 — mobil istemcinin bildirdiği hatayı kuyruğa koyar.
///
/// 🔴 <b>Kaynak sunucuda sabitlenir.</b> DTO'da <c>Source</c> alanı yok ve olmayacak:
/// istemci <c>api</c> diyebilseydi kendi çökmesini sunucu hatası gibi gösterip
/// "sunucumuzda kaç hata var?" sorusunun cevabını zehirlerdi. Aynı sebeple
/// <c>TraceId</c>, <c>IpAddress</c> ve <c>UserId</c> de istekten değil <b>bağlamdan</b> gelir.
///
/// ⚠️ Uç anonim (çökme oturum açılmadan da olur) → gövde tavanları DTO'da zorunlu ve
/// aşımda <b>reddedilir</b>: sessiz kırpma yığını yarıda keser, kesilen yığın farklı bir
/// parmak izi üretir ve aynı hata iki ayrı kayda düşer (tekilleştirme sessizce bozulur).
/// </summary>
public sealed record ReportClientErrorCommand(
    ReportClientErrorDto Dto,
    Guid? UserId,
    string? IpAddress,
    string? UserAgent,
    string? TraceId) : IRequest<bool>;

public sealed class ReportClientErrorCommandHandler : IRequestHandler<ReportClientErrorCommand, bool>
{
    private readonly IErrorLogSink _sink;

    public ReportClientErrorCommandHandler(IErrorLogSink sink) => _sink = sink;

    public Task<bool> Handle(ReportClientErrorCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        // Tanınmayan seviye sessizce "error"a düşer — istemcinin yazım hatası kaydı
        // düşürmesin, ama uydurma bir seviye de tabloya girmesin.
        var level = ErrorLogLevels.All.Contains(dto.Level ?? string.Empty)
            ? dto.Level!
            : ErrorLogLevels.Error;

        var accepted = _sink.TryWrite(new ErrorLogEntry(
            Source: ErrorLogSources.Mobile,
            Level: level,
            Code: dto.Code.Trim(),
            Message: dto.Message.Trim(),
            StackTrace: dto.StackTrace,
            Path: SensitiveDataMasker.MaskPath(dto.Path),
            Method: null,
            StatusCode: null,
            TraceId: request.TraceId,
            UserId: request.UserId,
            IpAddress: request.IpAddress,
            UserAgent: request.UserAgent,
            AppVersion: dto.AppVersion,
            Platform: dto.Platform,
            OsVersion: dto.OsVersion));

        return Task.FromResult(accepted);
    }
}
