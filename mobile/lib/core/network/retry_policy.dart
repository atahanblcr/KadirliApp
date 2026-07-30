import 'api_exception.dart';

/// Riverpod 3, hata veren provider'ları **kendiliğinden** yeniden dener
/// (varsayılan: üstel bekleyişle, sınırsız). Bu, ağ hataları için iyi ama
/// "kayıt yok / yetkin yok / istek hatalı" gibi tekrarlanınca da aynı sonucu
/// verecek hatalarda boşuna istek üretir — hem pil hem sunucu israfı, hem de
/// testlerde asla sönmeyen zamanlayıcılar bırakır.
///
/// [apiRetry] sadece **geçici** sorunlarda yeniden dener:
/// bağlantı/zaman aşımı, 5xx ve 429. Kalıcı hatalarda (`404`, `401`, `400`…)
/// kullanıcıya gösterilen "Tekrar dene" düğmesi tek yeniden deneme yoludur.
///
/// Kullanım: `FutureProvider(..., retry: apiRetry)`.
Duration? apiRetry(int retryCount, Object error) {
  // En fazla 3 deneme (ilk istek + 2 tekrar).
  if (retryCount >= 2) return null;

  final transient = switch (error) {
    ApiException(:final isConnectionProblem, :final isRateLimited, :final statusCode) =>
      isConnectionProblem || isRateLimited || (statusCode != null && statusCode >= 500),
    // Beklenmeyen (kod hatası) — tekrar denemek düzeltmez.
    _ => false,
  };
  if (!transient) return null;

  // 429'da sunucu "ne zaman" dediyse ona uyulur.
  if (error is ApiException && error.retryAfter != null) return error.retryAfter;

  return Duration(milliseconds: 600 * (retryCount + 1));
}
