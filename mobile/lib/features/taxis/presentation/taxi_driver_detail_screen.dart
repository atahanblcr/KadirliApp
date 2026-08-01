import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/taxis_providers.dart';
import '../data/models/taxi_driver.dart';
import 'widgets/taxi_call_button.dart';

/// Taksi sürücüsü detayı (11.11) — `/taksi/<id>`.
class TaxiDriverDetailScreen extends ConsumerWidget {
  const TaxiDriverDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(taxiDriverProvider(id));

    return AppScaffold(
      title: 'Taksi',
      actions: [
        if (state case AsyncData(value: final driver))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                driver.shareText,
                subject: driver.name,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(taxiDriverProvider(id)),
      body: switch (state) {
        AsyncData(value: final driver) => _Content(driver: driver),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(taxiDriverProvider(id)),
        ),
        _ => const LoadingView(itemCount: 2, hasImage: false),
      },
    );
  }
}

class _DetailError extends StatelessWidget {
  const _DetailError({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final api = error is ApiException ? error as ApiException : null;

    if (api != null && api.isNotFound) {
      return const EmptyView(
        icon: Icons.search_off_rounded,
        title: 'Sürücü bulunamadı',
        message:
            'Bu sürücü listeden kaldırılmış ya da kaydı pasife alınmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Sürücü bilgisi alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends StatelessWidget {
  const _Content({required this.driver});

  final TaxiDriver driver;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        Row(
          children: [
            Container(
              width: 56,
              height: 56,
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer,
                borderRadius: AppRadius.rMd,
              ),
              child: Icon(
                Icons.local_taxi_rounded,
                size: 28,
                color: theme.colorScheme.onPrimaryContainer,
              ),
            ),
            AppSpacing.wGapLg,
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(driver.name, style: theme.textTheme.headlineSmall),
                  if (driver.isVerified) ...[
                    AppSpacing.gapXs,
                    Row(
                      children: [
                        Icon(
                          Icons.verified_rounded,
                          size: 16,
                          color: palette.success,
                        ),
                        AppSpacing.wGapXs,
                        Flexible(
                          child: Text(
                            'Doğrulanmış sürücü',
                            style: theme.textTheme.bodySmall?.copyWith(
                              color: palette.success,
                              fontWeight: FontWeight.w600,
                            ),
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
        AppSpacing.gapXl,

        AppCard(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          ),
          child: Column(
            children: [
              if (driver.plateLabel != null)
                InfoRow(
                  icon: Icons.pin_rounded,
                  label: 'Plaka',
                  value: driver.plateLabel!,
                ),
              if (driver.vehicleLabel != null)
                InfoRow(
                  icon: Icons.directions_car_rounded,
                  label: 'Araç',
                  value: driver.vehicleLabel!,
                ),
              if (driver.hasPhone)
                InfoRow(
                  icon: Icons.call_rounded,
                  label: 'Telefon',
                  value: driver.phone.trim(),
                ),
            ],
          ),
        ),
        AppSpacing.gapXl,

        if (driver.hasPhone)
          TaxiCallButton(driver: driver, label: 'Taksiyi ara', expand: true)
        else
          const InfoBanner(
            tone: InfoBannerTone.warning,
            message: 'Bu sürücü için telefon numarası kayıtlı değil.',
          ),

        AppSpacing.gapLg,
        Text(
          'Arama başlattığınızda telefon uygulamanız açılır. Ücret ve güzergâh '
          'konusunda sürücüyle önceden anlaşmanızı öneririz.',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }
}
