import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:package_info_plus/package_info_plus.dart';

/// Uygulama kimliği: mağaza sürümü + build numarası (Ayarlar → Hakkında).
///
/// `pubspec.yaml`'daki `version: 1.0.0+1` değeri **derleme zamanında** platform
/// meta verisine gömülür; buradan okumak elle güncellenen bir sabit tutmaktan
/// güvenlidir (11.15'te sürüm artınca ekran kendiliğinden doğru olur).
class AppInfo {
  const AppInfo({required this.version, required this.buildNumber});

  final String version;
  final String buildNumber;

  /// "1.0.0 (1)" — build numarası yoksa yalnız sürüm.
  String get display => buildNumber.isEmpty ? version : '$version ($buildNumber)';

  /// Platform kanalı yoksa (widget testleri) kullanılan nötr değer.
  static const unknown = AppInfo(version: '—', buildNumber: '');
}

/// Sürüm bilgisi. Platform kanalı cevap vermezse (test ortamı) uygulama
/// patlamaz, "—" gösterilir — sürüm satırı hiçbir zaman ekranı düşürmemeli.
final appInfoProvider = FutureProvider<AppInfo>((ref) async {
  try {
    final info = await PackageInfo.fromPlatform();
    return AppInfo(version: info.version, buildNumber: info.buildNumber);
  } catch (error) {
    debugPrint('Sürüm bilgisi okunamadı: $error');
    return AppInfo.unknown;
  }
});
