using System;

namespace KadirliApp.Application.Features.Files.Dtos;

public record FileResponseDto(Guid Id, string CdnUrl, string OriginalName);
