using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Features.Files.Dtos;
using MediatR;
using File = KadirliApp.Domain.Entities.File;

namespace KadirliApp.Application.Features.Files.Commands.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileResponseDto>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UploadFileCommandHandler(IFileStorageService fileStorageService, IUnitOfWork unitOfWork)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<FileResponseDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        // 1. Save physical file
        var fileUrl = await _fileStorageService.UploadFileAsync(request.FileStream, request.FileName, request.ContentType, cancellationToken);
        
        // 2. Create DB record
        var fileEntity = new File
        {
            OriginalName = request.FileName,
            // fileUrl göreli ("/uploads/x.png") ya da mutlak olabilir; Uri.LocalPath göreli URI'de fırlatır
            FileName = Path.GetFileName(fileUrl),
            MimeType = request.ContentType,
            SizeBytes = request.SizeBytes,
            StoragePath = fileUrl,
            CdnUrl = fileUrl,
            ModuleType = request.ModuleType,
            ModuleId = request.ModuleId,
            UploadedBy = request.UploadedBy
        };

        var repo = _unitOfWork.Repository<File>();
        await repo.AddAsync(fileEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Return response
        return new FileResponseDto(fileEntity.Id, fileEntity.CdnUrl, fileEntity.OriginalName);
    }
}
