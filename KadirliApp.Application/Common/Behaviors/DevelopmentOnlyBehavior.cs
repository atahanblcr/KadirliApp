using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KadirliApp.Application.Common.Behaviors;

/// <summary>
/// Faz 12.19a — <see cref="IDevelopmentOnlyCommand"/> işaretli komutları geliştirme ortamı
/// dışında <b>çalıştırmaz</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 <b>Boru hattındaki yeri en başta ve bu şart.</b> <c>AuditBehavior</c> izi handler
/// döndükten <i>sonra</i> yazar; kapı ondan sonra dursaydı iş zaten yapılmış olurdu.
/// <c>CachingBehavior</c>'dan da önce olmalı: önbellekten dönen bir yanıt "komut koştu"
/// demek değil ama kapının <b>koşulmamış</b> bir komutu reddetmesi de yanlış olmazdı;
/// yine de en baştaki konum, kuralın hiçbir davranışa bağlı kalmadan tutmasını sağlar.
/// </para>
/// <para>
/// ⚠️ <b>Neden <see cref="ForbiddenException"/>:</b> bu bir <i>yetki</i> reddi değil ortam
/// reddi, ama dışarıya aynı biçimde görünmeli — <c>500</c> dönseydi panelin hata kaydı
/// (<c>error_logs</c>) her denemede bir "işlenmemiş hata" satırıyla kirlenir ve gerçek
/// hatalar arasında kaybolurdu. Mesaj Türkçe (Değişmez Kural #6) ve <b>ortam adını söyler</b>:
/// "neden çalışmadı?" sorusunun cevabı, bu kapıyı ilk kez gören geliştirici için
/// başka hiçbir yerde yazmıyor.
/// </para>
/// <para>
/// 📌 Log seviyesi <c>Warning</c>: geliştirme dışı bir ortamda bu komutun <i>denenmiş</i>
/// olması başlı başına bir olaydır — sessizce reddedilirse denemenin kendisi kaybolur.
/// </para>
/// </remarks>
public class DevelopmentOnlyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IAppEnvironment _env;
    private readonly ILogger<DevelopmentOnlyBehavior<TRequest, TResponse>> _log;

    public DevelopmentOnlyBehavior(
        IAppEnvironment env,
        ILogger<DevelopmentOnlyBehavior<TRequest, TResponse>> log)
        => (_env, _log) = (env, log);

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not IDevelopmentOnlyCommand || _env.IsDevelopment)
            return next(ct);

        _log.LogWarning(
            "Yalnız geliştirmeye açık komut '{Command}' '{Environment}' ortamında denendi ve reddedildi.",
            typeof(TRequest).Name, _env.Name);

        throw new ForbiddenException(
            $"Bu işlem yalnız geliştirme ortamında çalışır (şu anki ortam: {_env.Name}).");
    }
}
