using System;

namespace KadirliApp.Application.Features.Deaths.Dtos;

public record QueryDeathNoticeDto(
    DateTime? Date,
    string? Search,
    string? Status,
    int Page = 1,
    int Limit = 20,

    /// <summary>
    /// Faz 11.18 — panel sütun sıralaması. Additive alan (ARCHITECTURE.md §5): boş
    /// geldiğinde 11.18 öncesindeki sıra (cenaze tarihi azalan) birebir korunur.
    /// Anahtarlar <c>DeathNoticeSorts</c>'ta; bilinmeyen anahtar varsayılana düşer.
    /// </summary>
    string? Sort = null
);
