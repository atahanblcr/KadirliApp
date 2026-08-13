namespace KadirliApp.Application.Features.Notifications;

/// <summary>
/// 🔑 Push <c>data</c> sözlüğünün anahtar adlarının <b>tek sahibi</b> (§7 madde 16).
///
/// <para>
/// Bu adlar bir <b>kontrattır</b>: mağazadaki mobil sürümler onları
/// <c>mobile/lib/core/push/push_messaging.dart</c> → <c>PushPayload.fromData</c> içinde
/// birebir okuyor. Biri yeniden adlandırılırsa <b>deep-link sessizce ölür</b>: kullanıcı
/// bildirime dokunur, hiçbir yere gitmez, hata da almaz (§7 madde 18).
/// </para>
///
/// <para>
/// 🔴 <b>Faz 0 denetiminin bulgusu (B1):</b> 12.15b'ye kadar bu adlar
/// <c>SendPushNotificationsJob.BuildData</c> içinde satır içi dizelerdi ve tek test
/// <c>notificationId</c>'nin <i>varlığını</i> soruyordu — <c>relatedType</c> yeniden
/// adlandırılsa ne backend ne mobil süiti kırmızıya dönerdi. Adlar buraya alındı ve
/// <c>PushDataContractTests</c> onları <b>düz metin olarak</b> iddia ediyor:
/// sabiti yeniden adlandırmak testi kırar, çünkü test sabite değil <b>dizenin kendisine</b>
/// bakıyor. Sabiti ve testi aynı anda değiştirmek ancak <i>bilinçli</i> yapılabilir —
/// kazayla değil.
/// </para>
/// </summary>
public static class PushDataKeys
{
    /// <summary>Her mesajda bulunur — istemci bildirimi "okundu" işaretlemek için kullanır.</summary>
    public const string NotificationId = "notificationId";

    /// <summary>Bildirimin türü (ör. <c>announcement</c>); yalnız kayıtta doluysa yazılır.</summary>
    public const string Type = "type";

    /// <summary>Hedef kaydın kimliği; <see cref="RelatedType"/> ile birlikte rotayı üretir.</summary>
    public const string RelatedId = "relatedId";

    /// <summary>Hedef kaydın türü — mobilde rotaya çevrilir (§7 madde 18).</summary>
    public const string RelatedType = "relatedType";

    /// <summary>Sözlükte görünebilecek anahtarların tamamı. Beşincisi eklenirse mobil onu yok sayar.</summary>
    public static readonly IReadOnlyList<string> All = new[] { NotificationId, Type, RelatedId, RelatedType };
}
