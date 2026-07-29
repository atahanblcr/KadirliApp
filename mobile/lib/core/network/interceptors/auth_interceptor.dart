import 'package:dio/dio.dart';

import '../api_exception.dart';
import '../token_store.dart';

/// Bearer token ekler ve 401'de oturumu **bir kez** yeniler (API_CONTRACT §4).
///
/// Akış: 401 → `POST /v1/auth/refresh` (jti rotasyonu: dönen YENİ refresh
/// saklanır) → başarısız istek aynı gövdeyle tekrarlanır. Refresh de
/// reddedilirse token'lar silinir ve [onSessionExpired] tetiklenir (11.3'te
/// go_router bunu dinleyip Giriş ekranına yönlendirecek).
///
/// **Eşzamanlı 401'ler tek refresh'e düşer** ([_pendingRefresh] kilidi) —
/// aksi halde rotasyon nedeniyle ilk yenileme dışındakiler geçersiz refresh
/// token'la sunucuya gider ve oturum boş yere düşerdi.
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required this.tokenStore,
    required this.refreshClient,
    this.onSessionExpired,
  });

  /// Yenileme ve yeniden gönderim için kullanılan yardımcı istemci:
  /// zarfı açar ama **auth interceptor'ı yoktur** — aksi halde 401 → refresh
  /// → 401 sonsuz döngüsüne girilirdi. Zarf açıldığı için hem refresh yanıtı
  /// hem de tekrarlanan isteğin yanıtı çağırana doğrudan `data` olarak döner.
  final Dio refreshClient;
  final TokenStore tokenStore;
  final void Function()? onSessionExpired;

  /// Bu isteğe token EKLEME (anonim uç zorlaması gerekirse).
  static const skipAuthExtraKey = 'auth.skip';
  static const _retriedExtraKey = 'auth.retried';

  /// Yenileme denemesi yapılmayacak uçlar — bunların 401'i "oturum bitti"
  /// değil, "kimlik bilgisi hatalı" demektir.
  static const _authPaths = {
    '/v1/auth/login',
    '/v1/auth/verify-otp',
    '/v1/auth/register',
    '/v1/auth/refresh',
  };

  Future<String?>? _pendingRefresh;

  @override
  Future<void> onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    if (options.extra[skipAuthExtraKey] == true || _isAuthPath(options.path)) {
      handler.next(options);
      return;
    }

    final token = await tokenStore.readAccessToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final options = err.requestOptions;
    final isUnauthorized =
        err.response?.statusCode == 401 ||
        (err.error is ApiException && (err.error as ApiException).isUnauthorized);

    if (!isUnauthorized ||
        options.extra[_retriedExtraKey] == true ||
        options.extra[skipAuthExtraKey] == true ||
        _isAuthPath(options.path)) {
      handler.next(err);
      return;
    }

    final newToken = await _refreshSession();
    if (newToken == null) {
      handler.next(err);
      return;
    }

    try {
      options.extra[_retriedExtraKey] = true;
      options.headers['Authorization'] = 'Bearer $newToken';
      final retried = await refreshClient.fetch<dynamic>(options);
      handler.resolve(retried);
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  /// Tek uçuşlu yenileme. Dönen değer yeni access token; null → oturum bitti.
  Future<String?> _refreshSession() {
    return _pendingRefresh ??= _performRefresh().whenComplete(() {
      _pendingRefresh = null;
    });
  }

  Future<String?> _performRefresh() async {
    final refreshToken = await tokenStore.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      await _endSession();
      return null;
    }

    try {
      final response = await refreshClient.post<dynamic>(
        '/v1/auth/refresh',
        data: {'refreshToken': refreshToken},
        options: Options(extra: {skipAuthExtraKey: true}),
      );

      final data = response.data;
      if (data is! Map) return _failRefresh();

      final access = data['accessToken'];
      final refresh = data['refreshToken'];
      if (access is! String || access.isEmpty || refresh is! String || refresh.isEmpty) {
        return _failRefresh();
      }

      await tokenStore.save(accessToken: access, refreshToken: refresh);
      return access;
    } on DioException catch (error) {
      final api = error.error is ApiException
          ? error.error as ApiException
          : ApiException.fromDio(error);
      // Bağlantı sorununda oturumu DÜŞÜRME — kullanıcı çevrimdışıyken
      // hesabından atılmamalı; yalnız sunucu reddettiyse temizle.
      if (api.isConnectionProblem) return null;
      return _failRefresh();
    }
  }

  Future<String?> _failRefresh() async {
    await _endSession();
    return null;
  }

  Future<void> _endSession() async {
    await tokenStore.clear();
    onSessionExpired?.call();
  }

  bool _isAuthPath(String path) {
    final normalized = Uri.tryParse(path)?.path ?? path;
    return _authPaths.any(normalized.endsWith);
  }
}
