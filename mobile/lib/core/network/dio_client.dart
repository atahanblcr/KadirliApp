import 'package:dio/dio.dart';

import '../config/env.dart';
import 'connectivity_status.dart';
import 'interceptors/auth_interceptor.dart';
import 'interceptors/envelope_interceptor.dart';
import 'interceptors/log_interceptor.dart';
import 'token_store.dart';

/// Uygulamanın Dio örneklerini kuran fabrika.
///
/// İki istemci üretilir:
/// - **ana istemci:** `[AuthInterceptor, EnvelopeInterceptor, (log)]` —
///   sıralama önemli: hata yolunda `AuthInterceptor` ham 401'i ÖNCE görmeli
///   ki yenileme yapabilsin, `EnvelopeInterceptor` en sonda `ApiException`'a
///   çevirsin.
/// - **yardımcı istemci:** yalnız `EnvelopeInterceptor` — token yenileme ve
///   401 sonrası yeniden gönderim burada koşar (bkz. `AuthInterceptor`).
abstract final class DioClient {
  static const _connectTimeout = Duration(seconds: 15);
  static const _receiveTimeout = Duration(seconds: 20);
  static const _sendTimeout = Duration(seconds: 20);

  static Dio create({
    required TokenStore tokenStore,
    void Function()? onSessionExpired,
    // 11.15: offline şeridi bu iki sinyalden besleniyor (bkz.
    // `connectivity_status.dart` — ayrı bir bağlantı paketi kullanılmıyor).
    void Function()? onReachable,
    void Function()? onUnreachable,
    String? baseUrl,
    // Testlerde sahte HTTP katmanı bağlanabilsin diye (her iki istemciye de
    // uygulanır — yenileme akışı da sahte adaptörden geçmeli).
    HttpClientAdapter? adapter,
  }) {
    final resolvedBaseUrl = baseUrl ?? Env.apiBaseUrl;
    final refreshClient = _bare(resolvedBaseUrl)..interceptors.add(EnvelopeInterceptor());
    if (adapter != null) refreshClient.httpClientAdapter = adapter;

    final dio = _bare(resolvedBaseUrl);
    if (adapter != null) dio.httpClientAdapter = adapter;
    dio.interceptors.addAll([
      AuthInterceptor(
        tokenStore: tokenStore,
        refreshClient: refreshClient,
        onSessionExpired: onSessionExpired,
      ),
      EnvelopeInterceptor(),
      if (onReachable != null && onUnreachable != null)
        ConnectivityInterceptor(onOnline: onReachable, onOffline: onUnreachable),
      if (Env.enableNetworkLogs) NetworkLogInterceptor(),
    ]);
    return dio;
  }

  static Dio _bare(String baseUrl) => Dio(
    BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: _connectTimeout,
      receiveTimeout: _receiveTimeout,
      sendTimeout: _sendTimeout,
      responseType: ResponseType.json,
      headers: {
        Headers.acceptHeader: Headers.jsonContentType,
        Headers.contentTypeHeader: Headers.jsonContentType,
      },
    ),
  );
}
