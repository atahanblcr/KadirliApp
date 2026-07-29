import '../config/env.dart';

/// Görsel/dosya URL'lerini kullanılabilir hale getirir (API_CONTRACT §7).
///
/// Sunucu URL'leri **göreli** döner (`/uploads/<guid>_ad.png`); istemci başına
/// API origin'ini ekler. Prod'da `FileStorage:BaseUrl` ayarlıysa URL zaten
/// mutlak gelir — o zaman dokunulmaz.
abstract final class AppImage {
  static String? url(String? raw, {String? baseUrl}) {
    final value = raw?.trim();
    if (value == null || value.isEmpty) return null;
    if (_isAbsolute(value)) return value;

    final origin = baseUrl ?? Env.apiBaseUrl;
    return value.startsWith('/') ? '$origin$value' : '$origin/$value';
  }

  /// Galeri listeleri için — boş/null girdiler elenir.
  static List<String> urls(Iterable<String?>? raw, {String? baseUrl}) {
    if (raw == null) return const [];
    return raw
        .map((item) => url(item, baseUrl: baseUrl))
        .whereType<String>()
        .toList(growable: false);
  }

  static bool _isAbsolute(String value) {
    final lower = value.toLowerCase();
    return lower.startsWith('http://') || lower.startsWith('https://');
  }
}
