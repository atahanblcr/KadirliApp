import 'package:flutter/widgets.dart';
import 'package:share_plus/share_plus.dart';

/// İçerik paylaşımı (cihazın kendi paylaşım sayfası).
///
/// **Neden var:** Kadirli'de bilgi WhatsApp aile/mahalle gruplarından yayılıyor.
/// "Yarın 09:00-15:00 elektrik kesintisi var" bilgisini kullanıcı ekran görüntüsü
/// alıp göndermek yerine tek dokunuşla paylaşabilmeli — uygulamanın topluluk
/// tonunun (MOBILE_UX_PLAN §0) doğrudan karşılığı.
///
/// Uygulama henüz mağazada olmadığı için metne bağlantı eklenmiyor; yayından
/// sonra (11.15) buraya tek satırda mağaza/deep-link kuyruğu eklenecek.
abstract final class AppShare {
  /// [origin] paylaşım sayfasını iPad'de konumlandırmak için gerekir
  /// (iPhone/Android'de yok sayılır); çağıran ekran butonun context'ini verir.
  static Future<void> text(
    String value, {
    String? subject,
    Rect? origin,
  }) async {
    final trimmed = value.trim();
    if (trimmed.isEmpty) return;
    await SharePlus.instance.share(
      ShareParams(text: trimmed, subject: subject, sharePositionOrigin: origin),
    );
  }

  /// Dokunulan widget'ın ekran üzerindeki dikdörtgeni (iPad popover çapası).
  static Rect? originOf(BuildContext context) {
    final box = context.findRenderObject();
    if (box is! RenderBox || !box.hasSize) return null;
    return box.localToGlobal(Offset.zero) & box.size;
  }
}
