import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';

/// Giriş akışının ortak marka başlığı: yeşil yaprak işareti + ad + slogan
/// (docs/mobile-mockup.html "6 Giriş (OTP)" ekranı).
class BrandHeader extends StatelessWidget {
  const BrandHeader({super.key, this.compact = false});

  /// Kod/kayıt ekranlarında daha küçük — form için yer kalsın.
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final size = compact ? 52.0 : 72.0;

    return Column(
      children: [
        Container(
          height: size,
          width: size,
          decoration: BoxDecoration(
            color: theme.colorScheme.primaryContainer,
            shape: BoxShape.circle,
          ),
          child: Center(
            child: Text('🌿', style: TextStyle(fontSize: size * 0.44)),
          ),
        ),
        AppSpacing.gapMd,
        Text(
          'Kadirli',
          style: compact ? theme.textTheme.headlineSmall : theme.textTheme.displaySmall,
        ),
        AppSpacing.gapXs,
        Text(
          'Şehrin cebinde',
          style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
        ),
      ],
    );
  }
}
