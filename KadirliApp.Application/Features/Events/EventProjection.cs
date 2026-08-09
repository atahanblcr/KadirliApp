using System;
using System.Linq.Expressions;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Events;

/// <summary>
/// Faz 12.4 — <c>Event</c> → <see cref="EventResponseDto"/> projeksiyonunun tek sahibi.
/// </summary>
/// <remarks>
/// 🔑 <b>Neden ortaklaştırıldı:</b> aynı <c>Select</c> bloğu liste ve detay sorgularında iki kez
/// yazılıydı; 12.4'te dört yeni alan eklenirken birine yazıp diğerine yazmamak, detayda konumun
/// <b>boş görünmesi</b> demekti — ve hiçbir test/derleyici bunu yakalamazdı.
///
/// 🔴 <b><see cref="EventResponseDto.LocationLabel"/> burada, tek yerde üretilir.</b> Etiket
/// <c>IsCenter</c>'a bakıyor ama o alan DTO'ya girmiyor (istemcinin işine yaramaz), bu yüzden
/// projeksiyon önce <see cref="Row"/> döner, etiket <see cref="Finish"/>'te yazılır.
/// EF'in çeviremeyeceği bir metot ifade ağacına konulamazdı zaten.
/// </remarks>
public static class EventProjection
{
    /// <param name="Dto">Etiketi henüz yazılmamış DTO.</param>
    /// <param name="DistrictIsCenter">Etiket kuralının ihtiyaç duyduğu, DTO'ya çıkmayan bilgi.</param>
    public sealed record Row(EventResponseDto Dto, bool DistrictIsCenter);

    public static readonly Expression<Func<Event, Row>> Select = x => new Row(
        new EventResponseDto
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            CategoryId = x.CategoryId,
            CategoryName = x.Category.Name,
            EventDate = x.EventDate,
            EventTime = x.EventTime,
            VenueName = x.VenueName,
            Address = x.Address,
            DistrictId = x.DistrictId,
            DistrictName = x.District != null ? x.District.Name : null,
            ProvinceName = x.District != null ? x.District.ProvinceName : null,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Organizer = x.Organizer,
            TicketPrice = x.TicketPrice,
            IsFree = x.IsFree,
            IsLocal = x.IsLocal,
            CoverImageId = x.CoverImageId,
            CoverImageUrl = x.CoverImage != null ? x.CoverImage.CdnUrl : null,
            Status = x.Status,
            CreatedAt = x.CreatedAt
        },
        x.District != null && x.District.IsCenter);

    public static EventResponseDto Finish(Row row)
    {
        row.Dto.LocationLabel = DistrictLabel.For(
            row.Dto.DistrictName, row.Dto.ProvinceName, row.DistrictIsCenter);
        return row.Dto;
    }
}
