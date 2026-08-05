import 'dart:async';
import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../network/network_providers.dart';

/// Faz 12.1 — mobilde oluşan hatayı `POST /v1/client-errors` ucuna bildirir.
///
/// 12.1 öncesinde mobil hatalar **hiçbir yere** akmıyordu: Crashlytics yok, uç yok.
/// Kullanıcının gördüğü çökme yalnız kullanıcının telefonundaydı.
///
/// 🔑 Üç kural, üçü de sert:
/// 1. **Ateşle-unut.** Rapor gönderimi hiçbir zaman beklenmez ve hiçbir zaman
///    kullanıcıya hata göstermez. Zaten bir şey ters gitti; ikinci bir hata
///    diyaloğu kullanıcının işine yaramaz.
/// 2. **Kendi hatasını raporlamaz.** Gönderim sırasında oluşan hata sessizce yutulur.
///    Aksi hâlde ağ yokken: rapor gönder → başarısız → onu raporla → başarısız… sonsuz döngü.
/// 3. **Yeniden denemez** — `retry: apiRetry` bilinçli olarak YOK. Hata raporu
///    yeniden denenirse zaten sorunlu olan sistem daha çok yorulur ve kuyruk şişer.
///
/// ⚠️ Sunucu aynı hatayı **parmak izine göre tekilleştiriyor**, bu yüzden istemcinin
/// mükemmel bir kuyruk tutması gerekmiyor: aynı çökme yüz kez gönderilse bile panelde
/// tek satır, adet 100 olur.
class ErrorReporter {
  ErrorReporter(this._ref);

  final Ref _ref;

  /// 🔴 Yeniden giriş kilidi: gönderim sırasında doğan bir hata tekrar buraya gelirse
  /// döngü başlar. Bayrak, o zinciri ilk halkada keser.
  bool _sending = false;

  /// Aynı hatayı saniyede bir kereden fazla göndermemek için son imza + zaman.
  /// (Sunucu zaten tekilleştiriyor; bu yalnız ağ trafiğini kısar — bir build
  /// döngüsündeki hata saniyede 60 kez tetiklenebiliyor.)
  String? _lastSignature;
  DateTime? _lastSentAt;

  static const _throttle = Duration(seconds: 1);

  PackageInfo? _packageInfo;

  /// Flutter çatısının yakaladığı hata (build/layout/gesture). Uygulama genelde ayakta kalır.
  void reportFlutterError(FlutterErrorDetails details) {
    unawaited(_report(
      error: details.exception,
      stack: details.stack,
      level: 'error',
      context: details.context?.toDescription(),
    ));
  }

  /// Yakalanmamış eşzamansız hata. Buraya düşen şey kullanıcı için genelde "uygulama takıldı".
  void reportUncaught(Object error, StackTrace? stack) {
    unawaited(_report(error: error, stack: stack, level: 'fatal'));
  }

  Future<void> _report({
    required Object error,
    StackTrace? stack,
    required String level,
    String? context,
  }) async {
    if (_sending) return; // kural 2

    final code = error.runtimeType.toString();
    final message = context == null ? '$error' : '$error ($context)';
    final signature = '$code|$message';

    final now = DateTime.now();
    if (_lastSignature == signature &&
        _lastSentAt != null &&
        now.difference(_lastSentAt!) < _throttle) {
      return;
    }
    _lastSignature = signature;
    _lastSentAt = now;

    _sending = true;
    try {
      final info = _packageInfo ??= await PackageInfo.fromPlatform();

      await _ref.read(apiClientProvider).post(
        '/v1/client-errors',
        body: <String, dynamic>{
          // ⚠️ `source` GÖNDERİLMEZ — sunucuda sabitleniyor. İstemci "api" diyebilseydi
          // kendi çökmesini sunucu hatası gibi gösterirdi.
          'code': _clamp(code, 120),
          'message': _clamp(message, 2000),
          'level': level,
          if (stack != null) 'stackTrace': _clamp(stack.toString(), 16000),
          'appVersion': '${info.version}+${info.buildNumber}',
          'platform': Platform.operatingSystem,
          'osVersion': _clamp(Platform.operatingSystemVersion, 80),
        },
      );
    } catch (_) {
      // kural 2 — sessizce yut. Burada log bile basmıyoruz: hata zaten
      // debugPrint ile konsola düşmüş durumda.
    } finally {
      _sending = false;
    }
  }

  /// Sunucu tavanı aşan gövdeyi **reddediyor** (kırpmıyor) — istemci kendi tarafında
  /// kırpar ki rapor hiç gitmemektense kısaltılmış gitsin.
  static String _clamp(String value, int max) =>
      value.length <= max ? value : value.substring(0, max);
}

final errorReporterProvider = Provider<ErrorReporter>((ref) => ErrorReporter(ref));
