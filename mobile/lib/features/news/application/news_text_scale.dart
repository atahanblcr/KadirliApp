import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/preferences/app_preferences.dart';

/// Haber okuma boyutu (plan dışı ek, 12.14).
///
/// ## Neden var (ve neden yalnız haberde)
/// Uygulama sistem yazı ölçeğine zaten saygı duyuyor (1.4'e kadar). Ama sistem ölçeğini
/// değiştirmek **bütün telefonu** değiştirmek demek; haber okuyan biri yalnız *o metni*
/// büyütmek ister ve bunun için ayarlara gidip geri gelmez. Gazete uygulamalarının bu
/// denetimi taşımasının sebebi bu.
///
/// ⚠️ Ölçek **yalnız gövde ve başlığa** uygulanır; rozetler, meta satırı ve şeritler
/// sistem ölçeğinde kalır — onlar düzenin taşıyıcısı ve bu projede `Row` içindeki metin
/// yedi kez taşma üretti.
enum NewsTextScale {
  small('Küçük', 0.9),
  normal('Normal', 1.0),
  large('Büyük', 1.15),
  huge('Çok büyük', 1.3);

  const NewsTextScale(this.label, this.factor);

  final String label;

  /// Sistem ölçeğinin **üstüne** çarpan. Toplam ölçek [effectiveScaler] ile sınırlanır.
  final double factor;

  static NewsTextScale fromName(String? name) =>
      values.firstWhere((v) => v.name == name, orElse: () => normal);
}

/// Sistem ölçeği × kullanıcı tercihi, **tavanlı**.
///
/// 🔴 Tavan zorunlu: sistem 1.4'te (uygulamanın üst sınırı) ve tercih 1.3'te iken çarpım
/// 1.82 olur. Tavan olmasaydı ekranın en dar yerleri (meta satırı, kategori hapları)
/// hiç denenmemiş bir ölçekte çizilirdi — bu projenin yedi kez tekrarlamış taşma sınıfına
/// yeni bir kapı açmak olurdu.
const double kNewsMaxTextScale = 1.6;

TextScaler effectiveNewsScaler(BuildContext context, NewsTextScale scale) {
  final system = MediaQuery.textScalerOf(context).scale(1);
  final combined = (system * scale.factor).clamp(0.8, kNewsMaxTextScale);
  return TextScaler.linear(combined);
}

class NewsTextScaleController extends Notifier<NewsTextScale> {
  static const _prefsKey = 'news.textScale';

  @override
  NewsTextScale build() => NewsTextScale.fromName(
    ref.watch(sharedPreferencesProvider).getString(_prefsKey),
  );

  /// Tercih **kalıcı**: her açılışta yeniden seçtirmek, ayarın kendisini işe yaramaz yapar.
  Future<void> select(NewsTextScale scale) async {
    state = scale;
    await ref.read(sharedPreferencesProvider).setString(_prefsKey, scale.name);
  }
}

final newsTextScaleProvider =
    NotifierProvider<NewsTextScaleController, NewsTextScale>(
      NewsTextScaleController.new,
    );
