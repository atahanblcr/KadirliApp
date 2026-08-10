using System;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Events;

/// <summary>
/// Faz 12.10 — <b>bir etkinliğin moderasyon durumunu değiştirmenin tek yeri.</b>
/// Saf, container'sız test edilebilir.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Bu sınıf bilerek en yalın olanı.</b> <c>Event</c> varlığında
/// <c>ApprovedBy</c>/<c>ApprovedAt</c>/<c>RejectedReason</c> kolonları <b>yok</b>; onay izi
/// tümüyle <c>IAuditableCommand</c> üzerinden (<c>audit_logs</c>) tutuluyor. Kolon eklemek
/// bir migration demek olurdu ve 12.10'un kapsam sözü net: <b>şema değişikliği yok</b>.
/// </para>
/// <para>
/// 🔑 Sınıfın "tek satır yazıyor" olması onu gereksiz yapmıyor — <b>tek sahiplik</b> yapının
/// kendisi: yarın etkinliğe bir onay izi kolonu eklendiğinde dokunulacak yer burasıdır ve
/// yapısal test (<c>ModerationSingleOwnerTests</c>) başka bir yerde <c>.Status =</c>
/// yazılmasına zaten izin vermiyor.
/// </para>
/// <para>
/// 📌 <b>Diğer üç modülün aksine <c>adminId</c>/<c>now</c> almıyor.</b> Simetri için
/// kullanılmayan parametre taşımak, ilk okuyana "bir yere yazılıyor olmalı" dedirtir ve
/// yalan söyler — etkinlikte yazılacak kolon yok. Kolon eklendiği gün imza da eklenir.
/// </para>
/// </remarks>
public static class EventModeration
{
    /// <summary>Etkinliği yayına alır.</summary>
    public static void Approve(Event ev)
    {
        ev.Status = "approved";
    }

    /// <summary>Etkinliği reddeder.</summary>
    public static void Reject(Event ev)
    {
        ev.Status = "rejected";
    }
}
