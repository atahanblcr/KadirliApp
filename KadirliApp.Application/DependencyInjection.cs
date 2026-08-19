using KadirliApp.Application.Common.Behaviors;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Notifications.Services;
using KadirliApp.Application.Features.PowerOutages.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KadirliApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            // 🔴 Faz 12.19a: ortam kapısı EN BAŞTA. Sıra bir tercih değil, kuralın kendisi —
            // AuditBehavior izi handler DÖNDÜKTEN sonra yazar, yani kapı ondan sonra
            // dursaydı reddedilen komut çoktan koşmuş olurdu. Kapsam tipten türer
            // (IDevelopmentOnlyCommand), elle liste yok.
            cfg.AddOpenBehavior(typeof(DevelopmentOnlyBehavior<,>));
            // 🔑 Faz 12.22a: ölçüm İKİNCİ sırada — yani CachingBehavior'ı SARAR. Ölçülen
            // şey "handler ne kadar sürdü" değil "çağıran ne kadar bekledi"; cache HIT'te
            // handler hiç koşmaz ama bekleyen yine bekler. Halka cache'in içine konsaydı
            // sıcak uçların p95'i sistematik olarak İYİ görünürdü (PerformanceBehavior).
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            // Faz 9.4: sıra önemli — önce cache'e bakılır (hit'te handler hiç çalışmaz),
            // invalidation ise handler başarıyla bittikten sonra devreye girer.
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
            // Faz 10.9(i): IAuditableCommand başarıyla bitince audit_logs'a iz yazar.
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        // Faz 12.2b: hedeflemenin TEK sahibi. Duyuru üreticisi de, panelin manuel gönderim
        // komutu da bunu kullanır — ikinci bir hedefleme gerçeklemesi yazılırsa aynı
        // mahalleye farklı kişi kümesi gider ve kimse hata almaz.
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        // Faz 10.10: duyuru yayınında bildirim üretimi — hem announcement command'leri
        // hem PublishScheduledAnnouncementsJob (Infrastructure) bu servisi kullanır.
        services.AddScoped<IAnnouncementNotificationGenerator, AnnouncementNotificationGenerator>();

        // Faz 12.3: kesinti ↔ duyuru bağının TEK sahibi. Üç komut da (oluştur/güncelle/sil)
        // buradan geçer — ikinci bir gerçekleme "güncelleme ikinci duyuru üretti" ya da
        // "silinen kesintinin bildirimleri ayakta kaldı" sınıfından sessiz hata doğurur.
        services.AddScoped<IPowerOutageAnnouncementWriter, PowerOutageAnnouncementWriter>();

        return services;
    }
}
