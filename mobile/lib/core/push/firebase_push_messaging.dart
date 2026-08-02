import 'dart:async';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';

import 'push_messaging.dart';

/// Arka planda (uygulama kapalı/askıda) gelen mesajın giriş noktası.
///
/// ⚠️ **Üst düzey fonksiyon ve `@pragma('vm:entry-point')` şart** — Flutter bu
/// handler'ı ayrı bir isolate'te çağırıyor, sınıf üyesi olamaz ve tree-shake
/// edilmemeli. Burada **iş yapılmaz**: sistem bildirimi FCM tarafından zaten
/// gösteriliyor; kullanıcı dokununca `onMessageOpenedApp` akışı devreye girecek.
/// Yalnız hata ayıklama izi bırakılır.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  debugPrint('Arka planda push alındı: ${message.messageId}');
}

/// Gerçek FCM sağlayıcısı.
///
/// [tryInitialize] Firebase'i kurmayı dener; **yapılandırma dosyası yoksa ya da
/// bozuksa `null` döner** ve çağıran [NoopPushMessaging]'e düşer. Uygulama
/// hiçbir durumda push yüzünden açılamaz hâle gelmez.
class FirebasePushMessaging implements PushMessaging {
  FirebasePushMessaging._(this._messaging);

  final FirebaseMessaging _messaging;
  bool _initialMessageConsumed = false;

  static Future<PushMessaging> tryInitialize() async {
    try {
      await Firebase.initializeApp();
      FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
      return FirebasePushMessaging._(FirebaseMessaging.instance);
    } catch (error) {
      // Yapılandırma yok (google-services.json / GoogleService-Info.plist
      // depoda tutulmuyor), platform kanalı yok (widget testi) ya da anahtar
      // bozuk → push'suz devam.
      debugPrint('Firebase kurulamadı, push devre dışı: $error');
      return const NoopPushMessaging();
    }
  }

  @override
  bool get isAvailable => true;

  @override
  Future<PushPermission> requestPermission() async {
    final settings = await _messaging.requestPermission();
    return _map(settings.authorizationStatus);
  }

  @override
  Future<PushPermission> currentPermission() async {
    final settings = await _messaging.getNotificationSettings();
    return _map(settings.authorizationStatus);
  }

  @override
  Future<String?> getToken() => _messaging.getToken();

  @override
  Stream<String> get onTokenRefresh => _messaging.onTokenRefresh;

  @override
  Stream<PushPayload> get onForegroundMessage =>
      FirebaseMessaging.onMessage.map(_toPayload);

  @override
  Stream<PushPayload> get onMessageOpenedApp =>
      FirebaseMessaging.onMessageOpenedApp.map(_toPayload);

  @override
  Future<PushPayload?> initialMessage() async {
    // Yalnız bir kez tüketilir: aksi hâlde uygulama her öne geldiğinde aynı
    // bildirime tekrar tekrar deep-link yapılırdı.
    if (_initialMessageConsumed) return null;
    _initialMessageConsumed = true;

    final message = await _messaging.getInitialMessage();
    return message == null ? null : _toPayload(message);
  }

  static PushPayload _toPayload(RemoteMessage message) => PushPayload.fromData(
    message.data,
    title: message.notification?.title,
    body: message.notification?.body,
  );

  static PushPermission _map(AuthorizationStatus status) => switch (status) {
    AuthorizationStatus.authorized ||
    AuthorizationStatus.provisional => PushPermission.granted,
    AuthorizationStatus.denied => PushPermission.denied,
    AuthorizationStatus.notDetermined => PushPermission.notDetermined,
  };
}
