import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';

/// Uygulama ortamı (flavor). Derleme zamanında `--dart-define` ile seçilir:
/// `flutter run --dart-define=FLAVOR=prod`
enum AppFlavor { dev, prod }

/// Ortam yapılandırması — base URL, flavor, dev kolaylıkları.
///
/// ⚠️ Android emülatöründe `localhost` cihazın kendisidir; makinenin
/// localhost'una `10.0.2.2` ile erişilir. iOS simülatörü ise host ağını
/// paylaşır → `localhost` çalışır. Gerçek cihazda makinenin LAN IP'si
/// gerekir → `--dart-define=API_BASE_URL=http://192.168.x.x:5005`.
abstract final class Env {
  static const String _flavorName = String.fromEnvironment('FLAVOR', defaultValue: 'dev');

  /// `--dart-define=API_BASE_URL=...` verilirse her şeyi ezer (gerçek cihaz testi).
  static const String _baseUrlOverride = String.fromEnvironment('API_BASE_URL');

  /// Prod API kökü. Domain netleşince (11.15) burası güncellenir.
  static const String _prodBaseUrl = 'https://api.kadirli.app';

  static const int apiPort = 5005;

  static AppFlavor get flavor => _flavorName == 'prod' ? AppFlavor.prod : AppFlavor.dev;

  static bool get isDev => flavor == AppFlavor.dev;
  static bool get isProd => flavor == AppFlavor.prod;

  /// API kökü — sonunda `/` YOK. Tüm uçlar `/v1/...` ile başlar
  /// (API_CONTRACT §1) ve görsel URL'leri de bu köke eklenir (§7).
  static String get apiBaseUrl {
    if (_baseUrlOverride.isNotEmpty) return _stripTrailingSlash(_baseUrlOverride);
    if (isProd) return _prodBaseUrl;
    return _devBaseUrl;
  }

  static String get _devBaseUrl {
    // kIsWeb: Flutter Web hedefi opsiyonel; tarayıcı host'un localhost'unu görür.
    if (kIsWeb) return 'http://localhost:$apiPort';
    if (Platform.isAndroid) return 'http://10.0.2.2:$apiPort';
    return 'http://localhost:$apiPort';
  }

  /// Ağ isteklerini logla (yalnız dev + debug build).
  static bool get enableNetworkLogs => isDev && kDebugMode;

  /// Dev araçları (tasarım önizleme ekranı, OTP otomatik doldurma vb.).
  static bool get showDevTools => isDev && kDebugMode;

  static String _stripTrailingSlash(String url) =>
      url.endsWith('/') ? url.substring(0, url.length - 1) : url;
}
