import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/push/push_messaging.dart';
import '../application/push_controller.dart';

/// Cihazın push token'ını okuyan kaynak.
///
/// 11.3'te bilinçli olarak `null` dönüyordu (firebase_messaging kurulu değildi).
/// **11.13'te gerçeklendi ve o günkü söz tutuldu: yalnız BU provider değişti,
/// çağıran kodun (giriş sonrası `registerAfterLogin`) tek satırı bile değişmedi.**
///
/// Sıra önemli: token istemeden **önce izin** alınır. Android 13+ ve iOS'ta
/// izinsiz `getToken()` ya boş döner ya da bildirim cihaza hiç düşmez.
final deviceFcmTokenProvider = Provider<Future<String?> Function()>((ref) {
  return () async {
    final messaging = ref.read(pushMessagingProvider);
    if (!messaging.isAvailable) return null;

    final permission = await ref
        .read(pushCoordinatorProvider)
        .requestPermission();
    if (permission != PushPermission.granted) return null;

    return messaging.getToken();
  };
});

/// `POST /v1/notifications/fcm-token` — cihaz token'ını oturuma bağlar.
///
/// Sunucu aynı token'ı başka kullanıcıdan temizliyor (cihaz hesap değiştirme
/// senaryosu, Faz 10.3) → istemcinin ekstra bir şey yapması gerekmez.
class FcmTokenService {
  FcmTokenService({required this.api, required this.readDeviceToken});

  final ApiClient api;
  final Future<String?> Function() readDeviceToken;

  /// Giriş/kayıt başarısından sonra çağrılır. Token yoksa (izin verilmedi ya da
  /// Firebase yapılandırılmamış) sessizce çıkar.
  Future<void> registerAfterLogin() async {
    await registerToken(await readDeviceToken());
  }

  /// Token yenilendiğinde de aynı yol kullanılır — FCM token'ı zaman zaman
  /// kendiliğinden değişir ve yenisi kaydedilmezse **push sessizce kesilir**.
  ///
  /// **Hata yutulur:** push kaydı başarısız olsa da oturum açılmıştır,
  /// kullanıcıya hata göstermek yanıltıcı olur.
  Future<void> registerToken(String? token) async {
    if (token == null || token.isEmpty) return;
    try {
      await api.post('/v1/notifications/fcm-token', body: {'token': token});
    } on ApiException catch (error) {
      debugPrint('FCM token kaydı atlandı: ${error.code}');
    }
  }
}

final fcmTokenServiceProvider = Provider<FcmTokenService>(
  (ref) => FcmTokenService(
    api: ref.watch(apiClientProvider),
    readDeviceToken: ref.watch(deviceFcmTokenProvider),
  ),
);
