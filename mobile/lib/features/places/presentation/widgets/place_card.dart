import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/places_providers.dart';
import '../../data/models/place.dart';

/// Mekan kartı: kapak görseli + ad + kategori + merkeze uzaklık/giriş ücreti.
class PlaceCard extends ConsumerWidget {
  const PlaceCard({super.key, required this.place, this.onTap});

  final Place place;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final category = ref.watch(placeCategoryByIdProvider(place.categoryId));

    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.zero,
      semanticLabel: place.name,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            height: 148,
            child: AppNetworkImage(
              url: place.coverImageUrl,
              fit: BoxFit.cover,
              // Kartın kendisi zaten `Clip.antiAlias` ile kırpıyor; görselin
              // ayrıca köşe yuvarlaması iç içe iki yarıçap gibi görünüyordu.
              borderRadius: BorderRadius.zero,
              // Görseli olmayan mekan çok — nötr bir sembol, kırık ikon değil.
              fallbackIcon: category?.materialIcon ?? Icons.place_rounded,
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  place.name,
                  style: theme.textTheme.titleMedium,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                if ((place.description ?? '').trim().isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Text(
                    place.description!.trim(),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: palette.muted,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
                AppSpacing.gapMd,
                // ⚠️ `Wrap`: dar ekran + büyük yazı ölçeğinde tek satırlık
                // `Row` bu projede dört kez taştı.
                Wrap(
                  spacing: AppSpacing.md,
                  runSpacing: AppSpacing.xs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    if (category != null)
                      _Meta(
                        icon: category.materialIcon,
                        label: category.name,
                        emphasized: true,
                      ),
                    if (place.distanceLabel != null)
                      _Meta(
                        icon: Icons.near_me_outlined,
                        label: 'Merkeze ${place.distanceLabel}',
                      ),
                    if (place.feeLabel != null)
                      _Meta(
                        icon: Icons.confirmation_number_outlined,
                        label: place.feeLabel!,
                      ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({
    required this.icon,
    required this.label,
    this.emphasized = false,
  });

  final IconData icon;
  final String label;
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = emphasized ? theme.colorScheme.primary : theme.palette.muted;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 15, color: color),
        AppSpacing.wGapXs,
        // `Wrap` çocuğuna sınırlı genişlik verir → `Flexible` + ellipsis şart.
        Flexible(
          child: Text(
            label,
            style: theme.textTheme.labelMedium?.copyWith(color: color),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
