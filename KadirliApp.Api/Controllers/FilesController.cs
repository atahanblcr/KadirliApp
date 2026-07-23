using System;
using System.IO;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Exceptions;
using KadirliApp.Application.Features.Files.Commands.DeleteMyFile;
using KadirliApp.Application.Features.Files.Commands.UploadFile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KadirliApp.Api.Controllers;

// Faz 10.1: Anonim 100 MB upload kapatıldı — [Authorize] + 10 MB limit + magic-byte doğrulaması
// (yalnız jpeg/png/webp; Content-Type başlığına ve uzantıya GÜVENİLMEZ, dosyanın ilk baytlarına bakılır).
[Authorize]
public class FilesController : ApiControllerBase
{
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    [HttpPost("upload")]
    [RequestSizeLimit(MaxSizeBytes)]
    [EnableRateLimiting("public-write")] // Faz 10.7: maliyetli uç — disk doldurma koruması
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string? moduleType,
        [FromForm] Guid? moduleId)
    {
        if (file == null || file.Length == 0)
            throw new AppException("Dosya gönderilmedi.", "FILE_REQUIRED");

        if (file.Length > MaxSizeBytes)
            throw new AppException("Dosya boyutu 10 MB'ı aşamaz.", "FILE_TOO_LARGE");

        using var stream = file.OpenReadStream();
        var (detectedMime, canonicalExtension) = DetectImageType(stream)
            ?? throw new AppException("Yalnızca JPEG, PNG veya WebP görselleri yüklenebilir.", "UNSUPPORTED_FILE_TYPE");

        // Path traversal'a karşı yol ayıklanır; uzantı istemcinin beyanına değil tespit edilen türe göre yazılır
        var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(file.FileName));
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "image";
        var safeFileName = $"{baseName}{canonicalExtension}";

        var command = new UploadFileCommand(
            stream,
            safeFileName,
            detectedMime,
            file.Length,
            moduleType,
            moduleId,
            CurrentUserId);

        var result = await Sender.Send(command);

        return Success(result);
    }

    /// <summary>
    /// Faz 10.8: kullanıcı kendi yüklediği (henüz hiçbir kayda bağlanmamış) dosyayı siler.
    /// Sahiplik değilse 403; bir kayıtta kullanılıyorsa 409 CONFLICT.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => Success(await Sender.Send(new DeleteMyFileCommand(id, CurrentUserId!.Value)));

    /// <summary>İlk 12 bayttan görsel türünü tespit eder (magic bytes); desteklenmeyen türde null döner.</summary>
    private static (string Mime, string Extension)? DetectImageType(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        var read = 0;
        while (read < header.Length)
        {
            var n = stream.Read(header[read..]);
            if (n == 0) break;
            read += n;
        }
        stream.Seek(0, SeekOrigin.Begin);

        // JPEG: FF D8 FF
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return ("image/jpeg", ".jpg");

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return ("image/png", ".png");

        // WebP: "RIFF" .... "WEBP"
        if (read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
            return ("image/webp", ".webp");

        return null;
    }
}
