using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Legal.Commands;

/// <summary>Faz 12.16 — KVKK modülünün denetim izi anahtarı.</summary>
public static class LegalAudit
{
    /// <summary>⚠️ <c>PanelMenu.Items</c> · <c>[PanelPermission]</c> ile <b>aynı</b> anahtar (§7 madde 20).</summary>
    public const string Module = "legal";
}

/// <summary>
/// Faz 12.16 — <b>yeni sürüm aç</b> (taslak). Yayınlanmış sürüm düzenlenemediği için
/// metni değiştirmenin <b>tek yolu</b> budur (§7 madde 72).
/// </summary>
/// <remarks>
/// 🔑 Sürüm numarası <b>sunucuda</b> üretilir (mevcut en yüksek + 1); formdan gelseydi iki
/// yönetici aynı numarayı verir ve <c>(document_id, version_number)</c> benzersiz indeksi
/// ikincisini reddederdi — doğru ama anlaşılmaz bir hatayla.
/// </remarks>
public class CreateLegalVersionCommand : IRequest<ApiResponse<Guid>>, IAuditableCommand
{
    public Guid DocumentId { get; set; }
    public string Body { get; set; } = default!;
    public string? Summary { get; set; }
    public bool RequiresReconsent { get; set; }
    public DateTime? EffectiveFrom { get; set; }

    public string AuditModule => LegalAudit.Module;
    public string AuditAction => "create_legal_version";
    public string? AuditAffectedType => nameof(LegalDocumentVersion);
}

public class CreateLegalVersionCommandHandler : IRequestHandler<CreateLegalVersionCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _uow;

    public CreateLegalVersionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<Guid>> Handle(CreateLegalVersionCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return ApiResponse<Guid>.FailureResponse("VALIDATION_ERROR", "Metin boş olamaz.");

        var documents = _uow.Repository<LegalDocument>();
        var exists = await documents.Query().AnyAsync(d => d.Id == request.DocumentId, ct);
        if (!exists)
            return ApiResponse<Guid>.FailureResponse("NOT_FOUND", "Belge bulunamadı.");

        var versions = _uow.Repository<LegalDocumentVersion>();

        // Açık bir taslak varken ikincisi açılmaz: "hangi taslağı yayınlayacağım?" sorusu
        // panelde cevapsız kalır ve yönetici yanlışını fark etmeden eskisini yayınlayabilir.
        if (await versions.Query().AnyAsync(v => v.DocumentId == request.DocumentId && v.PublishedAt == null, ct))
            return ApiResponse<Guid>.FailureResponse(
                "CONFLICT", "Bu belgenin yayınlanmamış bir taslağı zaten var. Önce onu yayınlayın ya da düzenleyin.");

        var last = await versions.Query()
            .Where(v => v.DocumentId == request.DocumentId)
            .MaxAsync(v => (int?)v.VersionNumber, ct) ?? 0;

        var version = new LegalDocumentVersion
        {
            DocumentId = request.DocumentId,
            VersionNumber = last + 1,
            Body = request.Body,
            Summary = request.Summary,
            RequiresReconsent = request.RequiresReconsent,
            // 🐛 `SpecifyKind` şart ve bu satır CANLI DOĞRULAMADA doğdu: panelin
            // `<input type="date">` alanı `Kind=Unspecified` bir `DateTime` üretiyor,
            // Npgsql ise `timestamptz` kolonuna yalnız UTC yazıyor → yeni sürüm açmak
            // **hiç çalışmıyordu** (500). Tek sahip `LegalDates`.
            EffectiveFrom = LegalDates.FromPanel(request.EffectiveFrom, DateTime.UtcNow)
        };

        await versions.AddAsync(version, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<Guid>.SuccessResponse(version.Id);
    }
}

/// <summary>
/// Faz 12.16 — <b>taslağı</b> düzenle.
/// </summary>
/// <remarks>
/// 🔴 Yayınlanmış sürümde <b>hiçbir şey yapmaz ve sebebini söyler</b> (§7 madde 72).
/// Sessizce yutmak, bu bloğun kapatmaya çalıştığı hasarın ta kendisi olurdu: yönetici
/// "düzelttim" sanır, metin eski kalır ve rıza kayıtları da hiçbir şey söylemez.
/// ⚠️ Kapı üç katmanlı: panelin formu yayınlanmış sürümü <b>salt-okunur</b> gösterir,
/// bu komut <b>reddeder</b>, alanlar <c>init</c> olduğu için üçüncü bir yol
/// <b><c>CS8852</c></b>'dir.
/// </remarks>
public class UpdateLegalVersionCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public Guid Id { get; set; }
    public string Body { get; set; } = default!;
    public string? Summary { get; set; }
    public bool RequiresReconsent { get; set; }
    public DateTime? EffectiveFrom { get; set; }

    public string AuditModule => LegalAudit.Module;
    public string AuditAction => "update_legal_version";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(LegalDocumentVersion);
}

public class UpdateLegalVersionCommandHandler : IRequestHandler<UpdateLegalVersionCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateLegalVersionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(UpdateLegalVersionCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
            return ApiResponse<bool>.FailureResponse("VALIDATION_ERROR", "Metin boş olamaz.");

        // ⚠️ Query() varsayılan olarak AsNoTracking döner (12.3 canlı bulgusu).
        var version = await _uow.Repository<LegalDocumentVersion>().Query(tracking: true)
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (version is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Sürüm bulunamadı.");

        // 🐛 Aynı tuzak burada da vardı (bkz. `LegalDates`): taslağı düzenlerken formdan
        // gelen tarih `Kind=Unspecified` olduğu için kayıt 500 veriyordu.
        if (!version.TryRevise(request.Body, request.Summary, request.RequiresReconsent,
                LegalDates.FromPanel(request.EffectiveFrom, version.EffectiveFrom)))
            return ApiResponse<bool>.FailureResponse(
                "CONFLICT",
                "Yayınlanmış bir metin değiştirilemez — kullanıcıların onayı bu metne verildi. " +
                "Değişiklik için yeni bir sürüm açın.");

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.SuccessResponse(true);
    }
}

/// <summary>
/// Faz 12.16 — taslağı <b>yayına al</b>: bu andan sonra kayıt ekranı bu metni sorar.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>İzin: <c>Publish</c> öneki <c>PanelPermissionFilter.ActionFor</c>'a ELLE eklendi</b>
/// → <c>approve</c>. §7 madde 19'un <b>yedinci</b> tekrarı (BulkApprove 11.18 · Archive 12.10 ·
/// Unarchive 12.13 · SendNotification 12.15 · ResetOverrides + Feature Faz 0 · <b>Publish
/// 12.16</b>). Eklenmeseydi POST olduğu için sessizce <c>update</c>'e düşerdi ve sonuç
/// listedeki en ağırlarından olurdu: <i>yalnız başlık düzeltme yetkisi olan bir moderatör,
/// şehrin tamamının onayladığı hukuki metni değiştirebilirdi.</i>
/// </para>
/// <para>
/// 🔴 <b>Eskiyi yürürlükten kaldırma ile yeniyi yayınlama AYNI İŞLEMDE</b> olmak zorunda —
/// ama <b>aynı <c>SaveChanges</c>'te DEĞİL.</b> Ayrım 12.16'nın bozma turunda ölçülerek
/// öğrenildi (8 koşudan 5'i <c>23505</c>): kısmi unique indeks <b>deyim başına</b>
/// denetlendiği ve EF'in UPDATE sırası <b>rastgele</b> olduğu için ikisi tek
/// <c>SaveChanges</c>'e sığmıyor. İkiye bölünüyor, <b>tek işlemde</b>
/// (<c>IUnitOfWork.ExecuteInTransactionAsync</c>) — ayrı işlemler olsaydı araya düşen bir
/// hata belgeyi <b>hiç yayında sürümü olmadan</b> bırakırdı ve o an zorunlu belge kayıt
/// akışından <b>sessizce düşerdi</b>. Ayrıntı gövdedeki notta.
/// </para>
/// </remarks>
public class PublishLegalVersionCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public Guid Id { get; set; }

    /// <summary>Yayınlayan yönetici — panel doldurur.</summary>
    public Guid? AdminId { get; set; }

    public string AuditModule => LegalAudit.Module;
    public string AuditAction => "publish_legal_version";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(LegalDocumentVersion);
}

public class PublishLegalVersionCommandHandler : IRequestHandler<PublishLegalVersionCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public PublishLegalVersionCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(PublishLegalVersionCommand request, CancellationToken ct)
    {
        var repo = _uow.Repository<LegalDocumentVersion>();

        var version = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(v => v.Id == request.Id, ct);

        if (version is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Sürüm bulunamadı.");

        if (!version.IsDraft)
            return ApiResponse<bool>.FailureResponse("CONFLICT", "Bu sürüm zaten yayınlanmış.");

        var now = DateTime.UtcNow;

        var current = await repo.Query(tracking: true)
            .FirstOrDefaultAsync(v => v.DocumentId == version.DocumentId
                                      && v.PublishedAt != null
                                      && v.SupersededAt == null, ct);

        // 🐛 SIRA ZORLANMAK ZORUNDA — ve bu, 12.16'nın bozma turunda ÖLÇÜLEREK bulundu:
        // aynı senaryo 8 kez koşuldu, **5'i düştü** (`23505: duplicate key value violates
        // unique constraint "ix_legal_document_versions_one_live_per_document"`).
        //
        // Sebep: kısmi unique indeks (`published_at IS NOT NULL AND superseded_at IS NULL`)
        // Postgres'te **deyim başına** denetlenir ve ertelenemez — `DEFERRABLE` yalnız
        // *kısıtlarda* var, kısmi unique indeks ise kısıt olamaz (UNIQUE constraint `WHERE`
        // kabul etmez). İki satır tek `SaveChanges`'e bırakılınca EF, aynı tablonun
        // UPDATE'lerini **birincil anahtar sırasına** göre gönderiyor; anahtarlar
        // `gen_random_uuid()` olduğu için sıra **rastgele**. Yeni sürüm önce yazıldığında
        // iki satır bir an için indeksin koşulunu sağlıyor ve yazma reddediliyor.
        //
        // 🔑 Belirtinin sinsiliği buradan: hata **her seferinde değil**, yayınlanan sürümün
        // GUID'i eskisinden küçük geldiğinde çıkıyor — yani "bende çalışıyor" diyen bir
        // geliştirici tamamen haklı olabilir. İlk yazımda testler üst üste **üç kez yeşil**
        // koştu; hata ancak **bozma turunda** göründü.
        //
        // ⚠️ Atomiklik KAYBOLMUYOR: iki `SaveChanges` tek veritabanı işleminin içinde.
        // Ayrı işlemler olsaydı araya düşen bir hata belgeyi **hiç yayında sürümü olmadan**
        // bırakırdı ve o an zorunlu belge kayıt akışından *sessizce* düşerdi
        // (`LegalConsentRules`: metni olmayan belge zorunlu tutulamaz).
        await _uow.ExecuteInTransactionAsync(async () =>
        {
            if (current is not null)
            {
                current.Supersede(now);
                await _uow.SaveChangesAsync(ct);   // önce boşalt
            }

            version.Publish(request.AdminId ?? Guid.Empty, now);
            await _uow.SaveChangesAsync(ct);       // sonra doldur
        }, ct);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}

/// <summary>Faz 12.16 — belgenin kimlik ayarları (başlık, zorunluluk, sıra, aktiflik).</summary>
/// <remarks>
/// ⚠️ <b>Metin buradan değiştirilemez</b> — o <see cref="CreateLegalVersionCommand"/>'in işi.
/// Ayrımı korumak şart: tek bir forma toplansaydı "başlığı düzelttim" diyen yönetici,
/// farkında olmadan metni de değiştirmiş olabilirdi.
/// </remarks>
public class UpdateLegalDocumentCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public bool IsMandatory { get; set; }
    public bool ShowAtRegistration { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    public string AuditModule => LegalAudit.Module;
    public string AuditAction => "update_legal_document";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => nameof(LegalDocument);
}

public class UpdateLegalDocumentCommandHandler : IRequestHandler<UpdateLegalDocumentCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateLegalDocumentCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ApiResponse<bool>> Handle(UpdateLegalDocumentCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ApiResponse<bool>.FailureResponse("VALIDATION_ERROR", "Başlık boş olamaz.");

        var document = await _uow.Repository<LegalDocument>().Query(tracking: true)
            .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

        if (document is null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Belge bulunamadı.");

        document.Title = request.Title.Trim();
        document.IsMandatory = request.IsMandatory;
        document.ShowAtRegistration = request.ShowAtRegistration;
        document.IsActive = request.IsActive;
        document.SortOrder = request.SortOrder;

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.SuccessResponse(true);
    }
}
