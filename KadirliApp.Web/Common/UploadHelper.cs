using System;
using System.Threading.Tasks;
using KadirliApp.Application.Features.Files.Commands.UploadFile;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace KadirliApp.Web.Common;

/// <summary>Admin formlarından gelen dosyaları Files modülü üzerinden kaydeder.</summary>
public static class UploadHelper
{
    public static async Task<Guid?> UploadAsync(ISender sender, IFormFile? file, string moduleType, Guid? uploadedBy = null)
    {
        if (file == null || file.Length == 0) return null;

        using var stream = file.OpenReadStream();
        var result = await sender.Send(new UploadFileCommand(
            stream, file.FileName, file.ContentType, file.Length, moduleType, null, uploadedBy));

        return result.Id;
    }
}
