using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Files.Commands.DeleteMyFile;

/// <summary>
/// Faz 10.8: kullanıcı kendi yüklediği dosyayı siler (masterclass §13.2 kontratı).
/// Kurallar: sahiplik (uploaded_by == UserId, aksi 403); herhangi bir kayıtta referanslıysa 409 CONFLICT
/// (görsel bir ilana/duyuruya bağlıyken silinirse içerik kırılır — önce ilgili kayıttan çıkarılmalı).
/// KARAR: soft delete (files.deleted_at) + fiziksel dosya diskten kaldırılır (yetim byte bırakılmaz);
/// DB satırı iz için kalır. Fiziksel silme hatası isteği DÜŞÜRMEZ (kayıt soft-silinmiştir, disk artığı zararsız).
/// </summary>
public record DeleteMyFileCommand(Guid FileId, Guid UserId) : IRequest<bool>;

public class DeleteMyFileCommandHandler : IRequestHandler<DeleteMyFileCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _storage;

    public DeleteMyFileCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    {
        _uow = uow;
        _storage = storage;
    }

    public async Task<bool> Handle(DeleteMyFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _uow.Repository<Domain.Entities.File>().Query(tracking: true)
            .FirstOrDefaultAsync(x => x.Id == request.FileId && x.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.File), request.FileId);

        if (file.UploadedBy != request.UserId)
            throw new ForbiddenException("Yalnızca kendi yüklediğiniz dosyayı silebilirsiniz.");

        if (await IsReferencedAsync(request.FileId, file.CdnUrl, cancellationToken))
            throw new ConflictException("Dosya bir kayıtta kullanılıyor; önce ilgili kayıttan kaldırılmalı.");

        file.DeletedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(cancellationToken);

        // Fiziksel silme best-effort: kayıt soft-silindi, disk hatası isteği düşürmez (yetim byte zararsız).
        try
        {
            await _storage.DeleteFileAsync(file.CdnUrl ?? file.StoragePath, cancellationToken);
        }
        catch
        {
            // yut — dosya diskte kalırsa ileride yetim-dosya temizliği toplar (10.8 kararı: job şimdilik ertelendi)
        }

        return true;
    }

    /// <summary>Dosyaya id (veya profil fotoğrafında cdn_url) ile referans veren TÜM alanlar taranır.</summary>
    private async Task<bool> IsReferencedAsync(Guid fileId, string? cdnUrl, CancellationToken ct)
    {
        if (await _uow.Repository<AdImage>().Query().AnyAsync(x => x.FileId == fileId, ct)) return true;
        if (await _uow.Repository<DeathNotice>().Query().AnyAsync(x => x.PhotoFileId == fileId, ct)) return true;
        if (await _uow.Repository<Announcement>().Query().AnyAsync(x => x.ImageFileId == fileId || x.PdfFileId == fileId, ct)) return true;
        if (await _uow.Repository<Event>().Query().AnyAsync(x => x.CoverImageId == fileId, ct)) return true;
        if (await _uow.Repository<EventImage>().Query().AnyAsync(x => x.FileId == fileId, ct)) return true;
        if (await _uow.Repository<Campaign>().Query().AnyAsync(x => x.CoverImageId == fileId, ct)) return true;
        if (await _uow.Repository<CampaignImage>().Query().AnyAsync(x => x.FileId == fileId, ct)) return true;
        if (await _uow.Repository<Place>().Query().AnyAsync(x => x.CoverImageId == fileId, ct)) return true;
        if (await _uow.Repository<PlaceImage>().Query().AnyAsync(x => x.FileId == fileId, ct)) return true;
        if (await _uow.Repository<Business>().Query().AnyAsync(x => x.LogoFileId == fileId, ct)) return true;
        if (await _uow.Repository<GuideItem>().Query().AnyAsync(x => x.LogoFileId == fileId, ct)) return true;
        if (await _uow.Repository<TaxiDriver>().Query().AnyAsync(x => x.LicenseFileId == fileId || x.RegistrationFileId == fileId, ct)) return true;

        // User.ProfilePhotoUrl file id değil URL saklar (10.3 kararı) — cdn_url ile karşılaştırılır.
        if (!string.IsNullOrEmpty(cdnUrl) &&
            await _uow.Repository<User>().Query().AnyAsync(x => x.ProfilePhotoUrl == cdnUrl, ct)) return true;

        return false;
    }
}
