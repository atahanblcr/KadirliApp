using System;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Campaigns;

/// <summary>
/// Faz 12.10 — <b>bir kampanyanın moderasyon durumunu değiştirmenin tek yeri.</b>
/// Saf, container'sız test edilebilir.
/// </summary>
/// <remarks>
/// <para>
/// 📌 <b>"Süresi doldu" burada YOK ve bu bilinçli.</b> 12.10 öncesinde Düzenle formu
/// <c>expired</c> seçeneği sunuyordu, ama kampanyanın <i>yayında olup olmadığını</i>
/// belirleyen şey durum değil <b>tarih</b>: <c>GetCampaignsQuery</c> ve
/// <c>GetCampaignByIdQuery</c> <c>StartDate &lt;= now &lt;= EndDate</c> süzüyor ve hiçbir
/// arka plan işi kampanya durumunu <c>expired</c> yapmıyor (ilanlardaki
/// <c>ExpireAdsJob</c>'ın kampanya karşılığı yok). Yani elle <c>expired</c> yazmak
/// "kampanyayı erken bitir" gibi <i>görünen</i> ama aslında onu moderasyon dışı bir
/// duruma iten bir yoldu. Kampanyayı erken bitirmenin dürüst yolu <b>bitiş tarihini</b>
/// değiştirmektir ve o alan aynı formda duruyor.
/// </para>
/// <para>
/// ⚠️ <c>expired</c> yine de <b>okunabilir</b> bir durumdur (<c>PanelDisplay.Status</c>
/// onu Türkçeye çeviriyor): 12.10 öncesinde elle yazılmış satırlar duruyor ve ham
/// basılmamalı.
/// </para>
/// </remarks>
public static class CampaignModeration
{
    /// <summary>Kampanyayı yayına alır.</summary>
    /// <remarks>
    /// Faz 11.15b: reddedilmiş bir kampanya sonradan onaylanırsa bayat red gerekçesi
    /// kalmasın. Aynı düzeltme ilanlarda 10.14(1)'de yapılmış ama kampanyaya taşınmamıştı:
    /// panelde "Onaylandı" rozetiyle "Reddedilme sebebi: …" satırı yan yana görünüyor,
    /// işletme sahibi kampanyasının durumundan emin olamıyordu.
    /// </remarks>
    public static void Approve(Campaign campaign, Guid adminId, DateTime now)
    {
        campaign.Status = "approved";
        campaign.ApprovedBy = adminId;
        campaign.ApprovedAt = now;
        campaign.RejectedReason = null;
    }

    /// <summary>Kampanyayı reddeder.</summary>
    /// <remarks>
    /// 🐛 <b>12.10'da düzeltilen simetri hatası:</b> red, onay izlerini <b>temizlemiyordu</b>.
    /// İlanlarda 10.14(1)'de "bir kayıt aynı anda hem onaylı hem reddedilmiş olamaz" diye
    /// karar verilmiş ve <c>ApprovedBy</c>/<c>ApprovedAt</c> sıfırlanmıştı; kampanyada
    /// yapılmamıştı. Sonuç sessizdi: reddedilmiş bir kampanyanın kaydında hâlâ
    /// "onaylayan yönetici" duruyordu — denetim izi doğru, <b>kaydın kendisi yalan</b>.
    /// </remarks>
    public static void Reject(Campaign campaign, string? reason, DateTime now)
    {
        campaign.Status = "rejected";
        campaign.RejectedReason = reason;
        campaign.ApprovedBy = null;
        campaign.ApprovedAt = null;
    }
}
