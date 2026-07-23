using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KadirliApp.Infrastructure.Files;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadDirectory;
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration configuration)
    {
        // Try getting from config or default to wwwroot/uploads
        _uploadDirectory = configuration["FileStorage:UploadDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        _baseUrl = configuration["FileStorage:BaseUrl"] ?? "";
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

        using var fileStreamOutput = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);

        var relativePath = $"/uploads/{uniqueFileName}";
        return string.IsNullOrEmpty(_baseUrl) ? relativePath : $"{_baseUrl.TrimEnd('/')}{relativePath}";
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

        // Faz 10.8 bug fix: göreli URL'de (`/uploads/...`) Uri.LocalPath fırlatıyordu — upload
        // tarafındaki 7 Tem düzeltmesinin aynısı (Path.GetFileName path traversal'ı da keser).
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_uploadDirectory, fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}
