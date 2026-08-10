using System;

namespace KadirliApp.Application.Features.Deaths.Dtos;

public record UpdateDeathNoticeDto(
    string DeceasedName,
    Guid? PhotoFileId,
    DateTime FuneralDate,
    TimeSpan FuneralTime,
    Guid? CemeteryId,
    Guid? MosqueId,
    Guid? NeighborhoodId,
    string? CondolenceAddress,
    decimal? CondolenceLatitude,
    decimal? CondolenceLongitude,
    /// <summary>
    /// ☠️ Faz 12.10'dan beri <b>yazılamaz</b> — moderasyon durumunun tek sahibi
    /// <c>ApproveDeathNoticeCommand</c>/<c>RejectDeathNoticeCommand</c>/
    /// <c>ArchiveDeathNoticeCommand</c> (görünmez sözleşme #52). Alan DTO'da duruyor (§5),
    /// ama farklı bir değer gelirse komut reddeder (<c>ModerationStatusGuard</c>).
    /// <para>
    /// ⚠️ <b>Nullable yapıldı ve bu bir gevşetme</b> (§5: doğrulama gevşetmek güvenlidir,
    /// sıkılaştırmak kırıcıdır). Sebep MVC'nin sessiz bir davranışı: non-nullable bir
    /// referans tipi <b>örtük olarak zorunludur</b>, yani alanı formdan kaldırdığımız anda
    /// <c>ModelState</c> "Status alanı gereklidir" diye kırılırdı ve düzenleme formu hiç
    /// kaydedilemezdi.
    /// </para>
    /// </summary>
    string? Status
);
