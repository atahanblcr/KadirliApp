import 'dart:async';

import 'package:flutter/foundation.dart';

/// Bir push mesajının **istemcinin umursadığı** kısmı.
///
/// FCM `data` sözlüğü her zaman `Map<String, String>`; sunucunun yazdığı
/// anahtarlar `SendPushNotificationsJob.BuildData` ile sabit:
/// `notificationId` (her zaman), `type`, `relatedId`, `relatedType`.
@immutable
class PushPayload {
  const PushPayload({
    this.notificationId,
    this.type,
    this.relatedId,
    this.relatedType,
    this.title,
    this.body,
  });

  final String? notificationId;
  final String? type;
  final String? relatedId;
  final String? relatedType;
  final String? title;
  final String? body;

  /// Sunucunun `data` sözlüğünden okur. Eksik/yabancı anahtarlar yok sayılır.
  factory PushPayload.fromData(
    Map<String, dynamic> data, {
    String? title,
    String? body,
  }) {
    String? read(String key) {
      final value = data[key];
      if (value == null) return null;
      final text = value.toString().trim();
      return text.isEmpty ? null : text;
    }

    return PushPayload(
      notificationId: read('notificationId'),
      type: read('type'),
      relatedId: read('relatedId'),
      relatedType: read('relatedType'),
      title: title,
      body: body,
    );
  }

  @override
  bool operator ==(Object other) =>
      other is PushPayload &&
      other.notificationId == notificationId &&
      other.type == type &&
      other.relatedId == relatedId &&
      other.relatedType == relatedType &&
      other.title == title &&
      other.body == body;

  @override
  int get hashCode =>
      Object.hash(notificationId, type, relatedId, relatedType, title, body);

  @override
  String toString() =>
      'PushPayload(notificationId: $notificationId, relatedType: $relatedType, relatedId: $relatedId)';
}

/// Push altyapısının **arayüzü**.
///
/// 🔑 Neden soyutlama var: (1) widget testleri Firebase kanalı olmadan koşmalı,
/// (2) Firebase yapılandırması olmayan bir derlemede uygulama **çökmemeli** —
/// [NoopPushMessaging]'e düşer. Bu, backend'deki `Fcm:Provider=None`
/// kararının birebir istemci aynası: push yoksa uygulama sessizce push'suz
/// çalışır, kullanıcıya hata gösterilmez.
///
/// 📌 **11.14 dersi burada bilinçli olarak uygulandı:** "yapılandırma
/// bayrağıyla kapatılan kod yolu hiç test edilmiyor demektir" — bu yüzden
/// hem no-op hem gerçek yol için testler var, ve gerçek yolun tüm mantığı
/// (izin, token kaydı, deep-link) Firebase'e değil bu arayüze bağlı.
abstract interface class PushMessaging {
  /// Sağlayıcı gerçekten çalışıyor mu (Firebase kuruldu mu)?
  bool get isAvailable;

  /// Kullanıcıdan bildirim izni ister. Zaten verilmişse tekrar sormaz.
  Future<PushPermission> requestPermission();

  /// Mevcut izin durumu — istek atmadan.
  Future<PushPermission> currentPermission();

  /// Cihaz token'ı (izin yoksa ya da sağlayıcı yoksa `null`).
  Future<String?> getToken();

  /// Token döndüğünde (FCM zaman zaman yeniler) yeni değeri yayar.
  Stream<String> get onTokenRefresh;

  /// Uygulama **ön plandayken** gelen mesaj — sistem bildirimi gösterilmez.
  Stream<PushPayload> get onForegroundMessage;

  /// Kullanıcı **arka plandaki** bildirime dokunup uygulamayı öne getirdi.
  Stream<PushPayload> get onMessageOpenedApp;

  /// Uygulama **kapalıyken** bildirime dokunularak açıldıysa o mesaj.
  /// Bir kez okunur; ikinci çağrıda `null` döner.
  Future<PushPayload?> initialMessage();
}

enum PushPermission {
  granted,
  denied,

  /// Kullanıcı henüz sorulmadı (iOS'ta ilk açılış).
  notDetermined,

  /// Sağlayıcı yok → izin kavramı da yok.
  unavailable,
}

/// Firebase yapılandırılmamışken kullanılan sağlayıcı: hiçbir şey yapmaz.
///
/// Uygulamanın geri kalanı bunu bilmez — bildirim **listesi** yine çalışır
/// (o sunucudan geliyor), yalnız cihaza push düşmez.
class NoopPushMessaging implements PushMessaging {
  const NoopPushMessaging();

  @override
  bool get isAvailable => false;

  @override
  Future<PushPermission> requestPermission() async => PushPermission.unavailable;

  @override
  Future<PushPermission> currentPermission() async => PushPermission.unavailable;

  @override
  Future<String?> getToken() async => null;

  @override
  Stream<String> get onTokenRefresh => const Stream.empty();

  @override
  Stream<PushPayload> get onForegroundMessage => const Stream.empty();

  @override
  Stream<PushPayload> get onMessageOpenedApp => const Stream.empty();

  @override
  Future<PushPayload?> initialMessage() async => null;
}
