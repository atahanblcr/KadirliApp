using MediatR;
using System;
using System.IO;
using KadirliApp.Application.Features.Files.Dtos;

namespace KadirliApp.Application.Features.Files.Commands.UploadFile;

public record UploadFileCommand(
    Stream FileStream, 
    string FileName, 
    string ContentType, 
    long SizeBytes,
    string? ModuleType = null,
    Guid? ModuleId = null,
    Guid? UploadedBy = null) : IRequest<FileResponseDto>;
