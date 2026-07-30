import 'package:flutter/material.dart';

import '../../../core/navigation/app_modules.dart';
import '../../common/presentation/module_placeholder_screen.dart';

/// İlanlar sekmesi.
///
/// 11.8 gerçek listeyi (arama/sıralama/kategori + sonsuz kaydırma) buraya
/// yazacak; şimdilik modül kaydındaki bilgiyle "yakında" ekranı gösterilir —
/// sekme boş beyaz kalmaz.
class AdsScreen extends StatelessWidget {
  const AdsScreen({super.key});

  @override
  Widget build(BuildContext context) => ModulePlaceholderScreen(
    module: moduleById('ads'),
    showBackButton: false,
  );
}
