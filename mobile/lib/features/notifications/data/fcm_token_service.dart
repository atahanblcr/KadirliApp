import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';

/// Cihazın push token'ını okuyan kaynak.
///
/// **11.3'te bilinçli olarak null döner** — `firebase_messaging` henüz kurulu
/// değil (11.13). Böylece kayıt akışının kendisi (giriş sonrası çağrı, hata
/// yutma, çıkışta temizlenme) bugünden yerinde ve test edilebilir; 11.13'te
/// yalnız bu provider `FirebaseMessaging.instance.getToken()` ile override
/// edilecek, çağıran kodda değişiklik olmayacak.
final deviceFcmTokenProvider = Provider<Future<String?> Function()>(
  (ref) => () async => null,
);

/// `POST /v1/notifications/fcm-token` — cihaz token'ını oturuma bağlar.
///
/// Sunucu aynı token'ı başka kullanıcıdan temizliyor (cihaz hesap değiştirme
/// senaryosu, Faz 10.3) → istemcinin ekstra bir şey yapması gerekmez.
class FcmTokenService {
  FcmTokenService({required this.api, required this.readDeviceToken});

  final ApiClient api;
  final Future<String?> Function() readDeviceToken;

  /// Giriş/kayıt başarısından sonra çağrılır. Token yoksa (11.3 durumu) sessizce
  /// çıkar. **Hata yutulur:** push kaydı başarısız olsa da oturum açılmıştır,
  /// kullanıcıya hata göstermek yanıltıcı olur.
  Future<void> registerAfterLogin() async {
    try {
      final token = await readDeviceToken();
      if (token == null || token.isEmpty) return;
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
