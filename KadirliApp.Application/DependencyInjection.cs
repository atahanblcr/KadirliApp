using KadirliApp.Application.Common.Behaviors;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KadirliApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            // Faz 9.4: sıra önemli — önce cache'e bakılır (hit'te handler hiç çalışmaz),
            // invalidation ise handler başarıyla bittikten sonra devreye girer.
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
            // Faz 10.9(i): IAuditableCommand başarıyla bitince audit_logs'a iz yazar.
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        // Faz 10.10: duyuru yayınında bildirim üretimi — hem announcement command'leri
        // hem PublishScheduledAnnouncementsJob (Infrastructure) bu servisi kullanır.
        services.AddScoped<IAnnouncementNotificationGenerator, AnnouncementNotificationGenerator>();

        return services;
    }
}
