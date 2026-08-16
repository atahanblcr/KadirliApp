using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using MediatR;

namespace KadirliApp.Application.Features.Dashboard.Commands;

/// <summary>
/// Faz 12.19a — panelin "Paneli Test Verileriyle Doldur" aksiyonunun komut karşılığı.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Üç şeyi birden düzeltmek için doğdu</b> (14 Ağu 2026 denetimi):
/// </para>
/// <list type="number">
///   <item><b>Ortam kapısı yoktu</b> → <see cref="IDevelopmentOnlyCommand"/>; kapıyı
///   <c>DevelopmentOnlyBehavior</c> tutar, yani kural bu dosyada <i>yazılı değil</i>,
///   <b>tipten türer</b>.</item>
///   <item><b>Denetim izi düşmüyordu</b> → <see cref="IAuditableCommand"/>. Canlıda sahte
///   içerik basabilen tek aksiyonun kim tarafından çalıştırıldığı hiçbir yerde
///   yazmıyordu.</item>
///   <item><b><c>AppDbContext</c> controller'a enjekte ediliyordu</b> → iş
///   <see cref="IMockDataSeeder"/>'a indi.</item>
/// </list>
/// <para>
/// ⚠️ <b>Modül anahtarı <c>"system"</c> ve izin matrisinde KARŞILIĞI YOK</b> — bu bilinçli.
/// <c>DashboardController</c> <c>[PanelPermission]</c> taşımaz (iniş sayfası herkese açıktır,
/// aksiyonun kendisi rol kapısıyla admin'e kısılıdır). Matriste bir "system" satırı
/// belirseydi yöneticiye <i>dağıtabileceği ama asla çalışmayacak</i> bir yetki görünürdü —
/// 12.2'de <c>StaffAdmin</c>'de tam olarak bu hata bulunmuştu. Anahtarın Türkçe karşılığı
/// <c>PanelDisplay.ModuleLabel</c>'a eklendi, yoksa denetim izi ekranı ham <c>system</c>
/// basardı (Değişmez Kural #6).
/// </para>
/// </remarks>
public record SeedMockDataCommand : IRequest<MockDataSeedResult>, IDevelopmentOnlyCommand, IAuditableCommand
{
    public string AuditModule => "system";
    public string AuditAction => "seed";
}

public class SeedMockDataCommandHandler : IRequestHandler<SeedMockDataCommand, MockDataSeedResult>
{
    private readonly IMockDataSeeder _seeder;

    public SeedMockDataCommandHandler(IMockDataSeeder seeder) => _seeder = seeder;

    public Task<MockDataSeedResult> Handle(SeedMockDataCommand request, CancellationToken cancellationToken)
        => _seeder.SeedAsync(cancellationToken);
}
