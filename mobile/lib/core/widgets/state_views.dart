import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import 'app_button.dart';
import 'skeleton.dart';

/// Yükleniyor durumu — varsayılan olarak skeleton (spinner değil).
///
/// Spinner yalnız "kısa ve belirsiz" işlerde ([compact] = true, ör. buton içi
/// veya sayfa sonu yükleme göstergesi).
class LoadingView extends StatelessWidget {
  const LoadingView({super.key, this.itemCount = 4, this.hasImage = true, this.compact = false});

  const LoadingView.compact({super.key})
    : itemCount = 0,
      hasImage = false,
      compact = true;

  final int itemCount;
  final bool hasImage;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    if (compact) {
      return const Padding(
        padding: EdgeInsets.all(AppSpacing.xl),
        child: Center(
          child: SizedBox(
            height: 24,
            width: 24,
            child: CircularProgressIndicator(strokeWidth: 2.4),
          ),
        ),
      );
    }
    return Semantics(
      label: 'İçerik yükleniyor',
      child: SkeletonCardList(itemCount: itemCount, hasImage: hasImage),
    );
  }
}

/// Boş durum — dostane ikon + mesaj + (varsa) aksiyon (MOBILE_UX_PLAN §7).
class EmptyView extends StatelessWidget {
  const EmptyView({
    super.key,
    this.title = 'Henüz kayıt yok',
    this.message,
    this.icon = Icons.inbox_rounded,
    this.actionLabel,
    this.onAction,
  });

  final String title;
  final String? message;
  final IconData icon;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(AppSpacing.lg),
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer,
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 32, color: theme.colorScheme.onPrimaryContainer),
            ),
            AppSpacing.gapLg,
            Text(title, style: theme.textTheme.titleMedium, textAlign: TextAlign.center),
            if (message != null) ...[
              AppSpacing.gapSm,
              Text(
                message!,
                style: theme.textTheme.bodyMedium?.copyWith(color: theme.palette.muted),
                textAlign: TextAlign.center,
              ),
            ],
            if (actionLabel != null && onAction != null) ...[
              AppSpacing.gapXl,
              AppButton(label: actionLabel!, onPressed: onAction),
            ],
          ],
        ),
      ),
    );
  }
}

/// Hata durumu — sıcak mesaj + "Tekrar dene". Teknik detay gösterilmez;
/// destek için yalnız [traceId] küçük punto ile verilir (API_CONTRACT §2).
class ErrorView extends StatelessWidget {
  const ErrorView({
    super.key,
    this.title = 'Bir şeyler ters gitti',
    this.message = 'İçerik yüklenemedi. Lütfen tekrar deneyin.',
    this.traceId,
    this.onRetry,
    this.retryLabel = 'Tekrar dene',
    this.icon = Icons.cloud_off_rounded,
  });

  final String title;
  final String message;
  final String? traceId;
  final VoidCallback? onRetry;
  final String retryLabel;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AppSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(AppSpacing.lg),
              decoration: BoxDecoration(
                color: palette.danger.withValues(alpha: 0.12),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 32, color: palette.danger),
            ),
            AppSpacing.gapLg,
            Text(title, style: theme.textTheme.titleMedium, textAlign: TextAlign.center),
            AppSpacing.gapSm,
            Text(
              message,
              style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
              textAlign: TextAlign.center,
            ),
            if (onRetry != null) ...[
              AppSpacing.gapXl,
              AppButton(label: retryLabel, icon: Icons.refresh_rounded, onPressed: onRetry),
            ],
            if (traceId != null) ...[
              AppSpacing.gapLg,
              Text(
                'Destek kodu: $traceId',
                style: theme.textTheme.labelSmall,
                textAlign: TextAlign.center,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// "İnternet yok" şeridi — ekranın üstünde ince bant (MOBILE_UX_PLAN §7).
///
/// Bağlantı dinleme 11.14'te bağlanır; şimdilik [visible] dışarıdan verilir.
class OfflineBanner extends StatelessWidget {
  const OfflineBanner({super.key, this.visible = true, this.message = 'İnternet bağlantısı yok'});

  final bool visible;
  final String message;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AnimatedSize(
      duration: AppDurations.medium,
      curve: Curves.easeOut,
      alignment: Alignment.topCenter,
      child: visible
          ? Container(
              width: double.infinity,
              color: palette.warning.withValues(alpha: 0.18),
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.lg,
                vertical: AppSpacing.sm,
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.wifi_off_rounded, size: 16, color: palette.warning),
                  AppSpacing.wGapSm,
                  Text(
                    message,
                    style: theme.textTheme.labelMedium?.copyWith(color: theme.colorScheme.onSurface),
                  ),
                ],
              ),
            )
          : const SizedBox(width: double.infinity),
    );
  }
}
