using System;
using System.Collections.Generic;
using MediatR;

namespace KadirliApp.Application.Features.Ads.Commands.UpdateAd;

public class UpdateAdCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;

    /// <summary>
    /// ☠️ Faz 12.10'dan beri <b>yazılamaz</b> — moderasyon durumunun tek sahibi
    /// <c>ApproveAdCommand</c>/<c>RejectAdCommand</c> (görünmez sözleşme #52).
    /// Alan DTO'da <b>duruyor</b> (§5: silmek kırıcı olurdu), ama kaydın mevcut
    /// durumundan farklı bir değer gelirse komut <b>reddeder ve sebebini söyler</b>
    /// (<c>ModerationStatusGuard</c>) — sessizce yutmak, hiçbir şey yapmayan bir buton
    /// üretirdi.
    /// <para>
    /// ⚠️ <b>Nullable yapıldı ve bu bir gevşetme</b> (§5: gevşetmek güvenlidir). Sebep
    /// MVC'nin sessiz bir davranışı: non-nullable bir referans tipi <b>örtük olarak
    /// zorunludur</b>, yani alanı formdan kaldırdığımız anda <c>ModelState</c> "Status
    /// alanı gereklidir" diye kırılır ve düzenleme formu hiç kaydedilemezdi.
    /// </para>
    /// </summary>
    public string? Status { get; set; }

    /// <summary>Mevcut görsellere eklenecek yeni dosya id'leri; Web'de UploadHelper, API'de istemci doldurur.</summary>
    public List<Guid> NewImageFileIds { get; set; } = new();

    /// <summary>Silinecek ad_images kayıt id'leri (AdImage.Id — file id değil).</summary>
    public List<Guid> RemoveImageIds { get; set; } = new();
}
