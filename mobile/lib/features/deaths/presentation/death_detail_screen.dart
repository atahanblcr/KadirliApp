import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../lookups/data/lookups_repository.dart';
import '../../lookups/data/models/named_lookup.dart';
import '../application/deaths_providers.dart';
import '../data/models/death_notice.dart';

/// Vefat ilanı detayı (11.11) — `/vefat/<id>`, 11.13 push deep-link hedefi.
///
/// Sıralama bilinçli: **merhumun adı → cenaze namazı zamanı → yer bilgileri →
/// taziye → başsağlığı dileği**. Sayaç, rozet ve dış bağlantı yok.
class DeathDetailScreen extends ConsumerWidget {
  const DeathDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(deathNoticeProvider(id));

    return AppScaffold(
      title: 'Vefat İlanı',
      actions: [
        if (state case AsyncData(value: final notice))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                notice.shareText(),
                subject: notice.deceasedName,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(deathNoticeProvider(id)),
      body: switch (state) {
        AsyncData(value: final notice) => _Content(notice: notice),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(deathNoticeProvider(id)),
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
        title: 'İlan bulunamadı',
        message:
            'Bu ilan kaldırılmış ya da arşivlenmiş olabilir. Vefat ilanları '
            'cenazeden bir hafta sonra arşivlenir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'İlan bilgisi alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends ConsumerWidget {
  const _Content({required this.notice});

  final DeathNotice notice;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final remaining = notice.timeUntilFuneral();

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        // Kaydı gönderen kullanıcı kendi bekleyen ilanını görebiliyor
        // (`GET /v1/deaths/{id}` RequesterId istisnası) — ne olduğunu yazalım.
        if (notice.isPending) ...[
          const InfoBanner(
            tone: InfoBannerTone.warning,
            message:
                'Bu bildirim henüz yayında değil. Görevliler kontrol ettikten '
                'sonra herkes tarafından görülebilecek.',
          ),
          AppSpacing.gapLg,
        ],

        if ((notice.photoUrl ?? '').trim().isNotEmpty) ...[
          Center(
            child: ClipRRect(
              borderRadius: AppRadius.rLg,
              child: SizedBox(
                width: 140,
                height: 170,
                child: AppNetworkImage(url: notice.photoUrl, fit: BoxFit.cover),
              ),
            ),
          ),
          AppSpacing.gapLg,
        ],

        Text(
          notice.deceasedName,
          style: theme.textTheme.headlineSmall,
          textAlign: TextAlign.center,
        ),
        AppSpacing.gapXs,
        Text(
          'vefat etmiştir',
          textAlign: TextAlign.center,
          style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapXl,

        // --- Cenaze namazı ---
        AppCard(
          accentStripe: notice.isToday() ? theme.colorScheme.primary : null,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Cenaze namazı',
                style: theme.textTheme.labelMedium?.copyWith(
                  color: palette.muted,
                ),
              ),
              AppSpacing.gapXs,
              Text(
                notice.funeralLabel(),
                style: theme.textTheme.titleLarge?.copyWith(
                  color: notice.isToday()
                      ? theme.colorScheme.primary
                      : theme.colorScheme.onSurface,
                ),
              ),
              if (!notice.isToday()) ...[
                AppSpacing.gapXs,
                Text(
                  AppDate.date(notice.funeralDate),
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: palette.muted,
                  ),
                ),
              ],
              if (remaining != null) ...[
                AppSpacing.gapSm,
                Text(
                  'Namaza ${AppDate.duration(remaining)} var.',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.primary,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
              if (notice.isPast()) ...[
                AppSpacing.gapSm,
                Text(
                  'Cenaze namazı kılınmıştır.',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: palette.muted,
                  ),
                ),
              ],
            ],
          ),
        ),
        AppSpacing.gapLg,

        // --- Yer bilgileri ---
        AppCard(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          ),
          child: Column(
            children: [
              if (notice.mosqueName != null)
                InfoRow(
                  icon: Icons.mosque_rounded,
                  label: 'Cami',
                  value: notice.mosqueName!,
                ),
              if (notice.cemeteryName != null)
                InfoRow(
                  icon: Icons.park_rounded,
                  label: 'Defnedileceği yer',
                  value: notice.cemeteryName!,
                ),
              if ((notice.condolenceAddress ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.home_outlined,
                  label: 'Taziye adresi',
                  value: notice.condolenceAddress!.trim(),
                ),
              if (notice.mosqueName == null &&
                  notice.cemeteryName == null &&
                  (notice.condolenceAddress ?? '').trim().isEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                  child: Text(
                    'Cami ve defin yeri bilgisi girilmemiş.',
                    style: theme.textTheme.bodyMedium?.copyWith(
                      color: palette.muted,
                    ),
                  ),
                ),
            ],
          ),
        ),

        // --- Yol tarifi (taziye yeri, yoksa cami/mezarlık koordinatı) ---
        _DirectionsSection(notice: notice),

        AppSpacing.gapXl,
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(AppSpacing.lg),
          decoration: BoxDecoration(
            color: theme.colorScheme.primaryContainer.withValues(alpha: 0.45),
            borderRadius: AppRadius.rMd,
          ),
          child: Text(
            'Merhuma Allah\'tan rahmet, ailesine ve sevenlerine '
            'başsağlığı dileriz.',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.colorScheme.onSurface,
              height: 1.5,
            ),
          ),
        ),
      ],
    );
  }
}

/// Harita butonu — önce taziye konumu/adresi, o yoksa cami ya da mezarlığın
/// lookup'taki koordinatı kullanılır.
///
/// ⚠️ Vefat gövdesi cami/mezarlık için **yalnız ad** taşıyor; koordinatlar
/// lookup uçlarında. Lookup gelmezse (ya da koordinat yoksa) buton **hiç
/// çizilmez** — `ContactActions`'ın kuralı.
class _DirectionsSection extends ConsumerWidget {
  const _DirectionsSection({required this.notice});

  final DeathNotice notice;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final address = notice.condolenceAddress?.trim();

    if (notice.hasCondolenceLocation || (address != null && address.isNotEmpty)) {
      return Padding(
        padding: const EdgeInsets.only(top: AppSpacing.lg),
        child: ContactActions(
          latitude: notice.condolenceLatitude,
          longitude: notice.condolenceLongitude,
          address: address,
          mapLabel: 'Taziye evi',
        ),
      );
    }

    final mosque = _lookupMatch(ref.watch(mosquesProvider).value, notice.mosqueId);
    final cemetery = _lookupMatch(
      ref.watch(cemeteriesProvider).value,
      notice.cemeteryId,
    );
    final target = (mosque?.hasLocation ?? false)
        ? mosque
        : ((cemetery?.hasLocation ?? false) ? cemetery : null);
    if (target == null) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.lg),
      child: ContactActions(
        latitude: target.latitude,
        longitude: target.longitude,
        mapLabel: target.name,
      ),
    );
  }

  static NamedLookup? _lookupMatch(List<NamedLookup>? items, String? id) {
    if (items == null || id == null) return null;
    for (final item in items) {
      if (item.id == id) return item;
    }
    return null;
  }
}
