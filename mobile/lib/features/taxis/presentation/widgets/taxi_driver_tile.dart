import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/taxi_driver.dart';
import 'taxi_call_button.dart';

/// Taksi sürücüsü kartı — **listede doğrudan "Ara" düğmesi** (11.7 Rehber
/// kararının aynısı: bu ekranın işi "numarayı bul, ara"; detaya girmeye zorlamak
/// gereksiz bir dokunuş).
class TaxiDriverTile extends StatelessWidget {
  const TaxiDriverTile({super.key, required this.driver, this.onTap});

  final TaxiDriver driver;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppCard(
      onTap: onTap,
      semanticLabel: 'Taksi sürücüsü ${driver.name}',
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              borderRadius: AppRadius.rSm,
            ),
            child: Icon(
              Icons.local_taxi_rounded,
              color: theme.colorScheme.onPrimaryContainer,
            ),
          ),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  driver.name,
                  style: theme.textTheme.titleSmall,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                AppSpacing.gapXs,
                // ⚠️ Dar sütun + `Row` içinde çıplak `Text` bu projenin tekrar
                // eden taşma tuzağı → `Wrap` + `Flexible`.
                Wrap(
                  spacing: AppSpacing.sm,
                  runSpacing: AppSpacing.xxs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    if (driver.plateLabel != null) _Plate(text: driver.plateLabel!),
                    if (driver.vehicleLabel != null)
                      ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 190),
                        child: Text(
                          driver.vehicleLabel!,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: palette.muted,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
          AppSpacing.wGapSm,
          if (driver.hasPhone)
            TaxiCallButton(driver: driver, size: AppButtonSize.small),
        ],
      ),
    );
  }
}

/// Plaka rozeti — sarı zemin yerine tema kenarlığıyla, koyu temada da okunur.
class _Plate extends StatelessWidget {
  const _Plate({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xxs,
      ),
      decoration: BoxDecoration(
        borderRadius: AppRadius.rSm,
        border: Border.all(color: theme.palette.border),
      ),
      child: Text(
        text,
        style: theme.textTheme.labelSmall?.copyWith(
          fontWeight: FontWeight.w700,
          letterSpacing: 0.4,
        ),
      ),
    );
  }
}
