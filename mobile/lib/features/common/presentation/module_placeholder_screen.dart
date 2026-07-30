import 'package:flutter/material.dart';

import '../../../core/config/env.dart';
import '../../../core/navigation/app_modules.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';

/// Henüz yazılmamış modüllerin geçici ekranı.
///
/// **Neden var:** MOBILE_UX_PLAN'ın "işlevsiz buton yok" şartı. Ana Sayfa
/// ızgarası bugün 12 modülü gösteriyor ama yalnız birkaçı yazıldı; dokunulan
/// kart hiçbir şey yapmasın ya da grileşsin istemiyoruz — kullanıcı nereye
/// geldiğini, ne geleceğini görüyor. Modül gerçeklenince
/// `AppModule.ready = true` olur ve rota gerçek ekrana bağlanır.
///
/// Debug modunda ayrıca modülün bağlanacağı uçlar listelenir (geliştirme
/// sırasında "bu ekran neye bağlanacaktı" sorusunun tek cevabı).
class ModulePlaceholderScreen extends StatelessWidget {
  const ModulePlaceholderScreen({
    super.key,
    required this.module,
    this.showBackButton = true,
  });

  final AppModule module;

  /// Sekme olarak gösterildiğinde (İlanlar) geri butonu olmaz.
  final bool showBackButton;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppScaffold(
      title: module.label,
      showBackButton: showBackButton,
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Center(
            child: Container(
              padding: const EdgeInsets.all(AppSpacing.xl),
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer,
                shape: BoxShape.circle,
              ),
              child: Icon(
                module.icon,
                size: 40,
                color: theme.colorScheme.onPrimaryContainer,
              ),
            ),
          ),
          AppSpacing.gapXl,
          Text(
            '${module.label} yakında',
            style: theme.textTheme.titleLarge,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapSm,
          Text(
            module.summary,
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXl,
          InfoBanner(
            tone: InfoBannerTone.info,
            title: 'Hazırlanıyor',
            message:
                'Bu bölüm uygulamanın ${module.phase} sürümüyle açılacak. '
                'Kadirli uygulaması adım adım büyüyor.',
          ),
          if (Env.showDevTools) ...[
            AppSpacing.gapXl,
            const SectionHeader(title: 'Bağlanacağı uçlar'),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final endpoint in module.endpoints) ...[
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.api_rounded, size: 16, color: palette.muted),
                        AppSpacing.wGapSm,
                        Expanded(
                          child: Text(endpoint, style: theme.textTheme.bodySmall),
                        ),
                      ],
                    ),
                    if (endpoint != module.endpoints.last) AppSpacing.gapSm,
                  ],
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}
