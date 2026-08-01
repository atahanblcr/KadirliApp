import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/places_providers.dart';
import '../data/models/place.dart';

/// Mekan detayı (11.11) — `/mekanlar/<id>`, 11.13 push deep-link hedefi.
class PlaceDetailScreen extends ConsumerWidget {
  const PlaceDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(placeProvider(id));

    return AppScaffold(
      title: 'Mekan',
      actions: [
        if (state case AsyncData(value: final place))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                place.shareText(
                  categoryName: ref
                      .read(placeCategoryByIdProvider(place.categoryId))
                      ?.name,
                ),
                subject: place.name,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(placeProvider(id)),
      body: switch (state) {
        AsyncData(value: final place) => _Content(place: place),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(placeProvider(id)),
        ),
        _ => const LoadingView(itemCount: 2),
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
        title: 'Mekan bulunamadı',
        message: 'Bu kayıt kaldırılmış ya da yayından alınmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Mekan bilgisi alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends ConsumerWidget {
  const _Content({required this.place});

  final Place place;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final category = ref.watch(placeCategoryByIdProvider(place.categoryId));

    return ListView(
      padding: const EdgeInsets.only(bottom: AppSpacing.xxl),
      children: [
        if ((place.coverImageUrl ?? '').trim().isNotEmpty)
          SizedBox(
            height: 220,
            width: double.infinity,
            child: AppNetworkImage(
              url: place.coverImageUrl,
              fit: BoxFit.cover,
              borderRadius: BorderRadius.zero,
            ),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.lg,
            AppSpacing.lg,
            0,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (category != null) ...[
                Row(
                  children: [
                    Icon(
                      category.materialIcon,
                      size: 16,
                      color: theme.colorScheme.primary,
                    ),
                    AppSpacing.wGapXs,
                    Flexible(
                      child: Text(
                        category.name,
                        style: theme.textTheme.labelLarge?.copyWith(
                          color: theme.colorScheme.primary,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
                AppSpacing.gapSm,
              ],
              Text(place.name, style: theme.textTheme.headlineSmall),
              if ((place.description ?? '').trim().isNotEmpty) ...[
                AppSpacing.gapMd,
                Text(
                  place.description!.trim(),
                  style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
                ),
              ],
              AppSpacing.gapLg,

              ContactActions(
                latitude: place.hasLocation ? place.latitude : null,
                longitude: place.hasLocation ? place.longitude : null,
                mapLabel: place.name,
                address: place.address,
              ),
              AppSpacing.gapLg,

              AppCard(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.lg,
                  vertical: AppSpacing.sm,
                ),
                child: Column(
                  children: [
                    if ((place.address ?? '').trim().isNotEmpty)
                      InfoRow(
                        icon: Icons.place_rounded,
                        label: 'Adres',
                        value: place.address!.trim(),
                      ),
                    if (place.distanceLabel != null)
                      InfoRow(
                        icon: Icons.near_me_outlined,
                        label: 'Şehir merkezine uzaklık',
                        value: place.distanceLabel!,
                      ),
                    if ((place.openingHours ?? '').trim().isNotEmpty)
                      InfoRow(
                        icon: Icons.schedule_rounded,
                        label: 'Ziyaret saatleri',
                        value: place.openingHours!.trim(),
                      ),
                    if (place.feeLabel != null)
                      InfoRow(
                        icon: Icons.confirmation_number_outlined,
                        label: 'Giriş',
                        value: place.feeLabel!,
                      ),
                    if ((place.bestSeason ?? '').trim().isNotEmpty)
                      InfoRow(
                        icon: Icons.wb_sunny_outlined,
                        label: 'En uygun mevsim',
                        value: place.bestSeason!.trim(),
                      ),
                  ],
                ),
              ),

              if ((place.howToGetThere ?? '').trim().isNotEmpty) ...[
                AppSpacing.gapXl,
                const SectionHeader(title: 'Nasıl gidilir?'),
                AppCard(
                  child: Text(
                    place.howToGetThere!.trim(),
                    style: theme.textTheme.bodyMedium?.copyWith(height: 1.5),
                  ),
                ),
              ],

              _Amenities(place: place),

              AppSpacing.gapXl,
              Text(
                'Bilgilerde eksik ya da yanlış gördüyseniz Ayarlar → '
                'Şikayet/İstek üzerinden bize bildirebilirsiniz.',
                style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Olanaklar (WC / Wi-Fi / Klima…).
///
/// ⚠️ Panelin veri modelinde **anahtarda olmayan olanak "belirtilmemiş"**
/// demek, "yok" değil → yalnız açıkça `true`/`false` yazılanlar gösterilir ve
/// "yok" olanlar üstü çizili değil, soluk ve `close` ikonuyla ayrılır.
class _Amenities extends StatelessWidget {
  const _Amenities({required this.place});

  final Place place;

  @override
  Widget build(BuildContext context) {
    final available = place.availableAmenities;
    final missing = place.missingAmenities;
    if (available.isEmpty && missing.isEmpty) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final palette = theme.palette;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppSpacing.gapXl,
        const SectionHeader(title: 'Olanaklar'),
        Wrap(
          spacing: AppSpacing.sm,
          runSpacing: AppSpacing.sm,
          children: [
            for (final name in available)
              _AmenityChip(
                label: name,
                icon: Icons.check_rounded,
                color: palette.success,
              ),
            for (final name in missing)
              _AmenityChip(
                label: name,
                icon: Icons.close_rounded,
                color: palette.muted,
              ),
          ],
        ),
      ],
    );
  }
}

class _AmenityChip extends StatelessWidget {
  const _AmenityChip({
    required this.label,
    required this.icon,
    required this.color,
  });

  final String label;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        borderRadius: AppRadius.rPill,
        border: Border.all(color: theme.palette.border),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: color),
          AppSpacing.wGapXs,
          Text(
            label,
            style: theme.textTheme.labelMedium?.copyWith(color: color),
          ),
        ],
      ),
    );
  }
}
