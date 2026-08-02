import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_exception.dart';

/// Uygulamanın "çevrimdışıyım" sinyali.
///
/// **Neden bir bağlantı paketi (`connectivity_plus`) kullanılmadı:** o paketler
/// cihazın *arayüz* durumunu söyler (Wi-Fi'a bağlı mı) — kullanıcı için asıl
/// soru ise **sunucuya ulaşılıyor mu**. Otel Wi-Fi'ına bağlı ama internete
/// çıkamayan telefon "bağlı" görünür ve şerit hiç çıkmaz; tersine bir paket
/// bağımlılığı, iki platform izni ve bir kanal daha eklemek gerekir.
///
/// Bu yüzden sinyal **gerçek isteklerden** türetiliyor: ağ katmanı bir bağlantı
/// hatası gördüğünde [goOffline], herhangi bir yanıt geldiğinde [goOnline]
/// çağrılır. Yani şerit "internetin yok" değil, **"şu an sunucuya
/// ulaşamıyoruz"** demektir — kullanıcının ekranda gördüğü şey de zaten bu.
///
/// ⚠️ Yalnız **bağlantı/zaman aşımı** hataları çevrimdışı sayılır; 404/400/500
/// sayılmaz — sunucuya ulaşıldığı hâlde şerit göstermek yanıltıcı olur.
class ConnectivityStatus extends Notifier<bool> {
  /// `true` = son ağ denemesi bağlantı hatasıyla döndü.
  @override
  bool build() => false;

  void goOffline() {
    if (!state) state = true;
  }

  void goOnline() {
    if (state) state = false;
  }
}

final connectivityStatusProvider =
    NotifierProvider<ConnectivityStatus, bool>(ConnectivityStatus.new);

/// Dio hata/yanıt yolundan [ConnectivityStatus]'ü besleyen ince interceptor.
///
/// Zincirin **en sonunda** durur: `EnvelopeInterceptor` ham `DioException`'ı
/// çoktan [ApiException]'a çevirmiş olur, burada yalnız sınıflandırma yapılır.
class ConnectivityInterceptor extends Interceptor {
  ConnectivityInterceptor({required this.onOnline, required this.onOffline});

  final void Function() onOnline;
  final void Function() onOffline;

  @override
  void onResponse(Response<dynamic> response, ResponseInterceptorHandler handler) {
    // Sunucudan yanıt geldi → ulaşılabiliyoruz.
    onOnline();
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final error = err.error;
    final isConnectionProblem = error is ApiException
        ? error.isConnectionProblem
        : err.type == DioExceptionType.connectionError ||
              err.type == DioExceptionType.connectionTimeout ||
              err.type == DioExceptionType.receiveTimeout ||
              err.type == DioExceptionType.sendTimeout;

    // Sunucu hata döndürdüyse (404/500…) bağlantı **var** demektir.
    if (isConnectionProblem) {
      onOffline();
    } else if (err.response != null) {
      onOnline();
    }
    handler.next(err);
  }
}
