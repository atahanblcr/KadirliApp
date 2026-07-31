import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/guide_providers.dart';
import '../data/models/guide_item.dart';

/// Rehber kaydı detayı (11.7) — 11.13 push deep-link hedefi.
class GuideItemDetailScreen extends ConsumerWidget {
  const GuideItemDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(guideItemProvider(id));

    return AppScaffold(
      title: 'Rehber',
      actions: [
        if (state case AsyncData(value: final item))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                guideShareText(item),
                subject: item.name,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(guideItemProvider(id)),
      body: switch (state) {
        AsyncData(value: final item) => _Content(item: item),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(guideItemProvider(id)),
        ),
        _ => const LoadingView(itemCount: 2, hasImage: false),
      },
    );
  }
}

/// Paylaşım metni — "numarayı gönder" en sık kullanılacak paylaşım.
@visibleForTesting
String guideShareText(GuideItem item) {
  final buffer = StringBuffer('📒 ${item.name}');
  final category = item.categoryName?.trim();
  if (category != null && category.isNotEmpty) buffer.write(' ($category)');
  final phone = item.phone?.trim();
  if (phone != null && phone.isNotEmpty) buffer.write('\n📞 $phone');
  final address = item.address?.trim();
  if (address != null && address.isNotEmpty) buffer.write('\n📍 $address');
  final hours = item.workingHours?.trim();
  if (hours != null && hours.isNotEmpty) buffer.write('\n🕗 $hours');
  buffer.write('\n\n— Kadirli uygulaması');
  return buffer.toString();
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
        title: 'Kayıt bulunamadı',
        message: 'Bu rehber kaydı kaldırılmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Rehber kaydı alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends StatelessWidget {
  const _Content({required this.item});

  final GuideItem item;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final description = item.description?.trim();

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if ((item.categoryName ?? '').trim().isNotEmpty) ...[
          Text(
            item.categoryName!.trim(),
            style: theme.textTheme.labelLarge?.copyWith(
              color: theme.colorScheme.primary,
            ),
          ),
          AppSpacing.gapXs,
        ],
        Text(item.name, style: theme.textTheme.headlineSmall),

        if (description != null && description.isNotEmpty) ...[
          AppSpacing.gapMd,
          Text(
            description,
            style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
          ),
        ],

        AppSpacing.gapLg,
        AppCard(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          ),
          child: Column(
            children: [
              if (item.hasPhone)
                InfoRow(
                  icon: Icons.call_rounded,
                  label: 'Telefon',
                  value: item.phone!.trim(),
                  onTap: () => AppLinks.call(item.phone!),
                ),
              if ((item.address ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.place_rounded,
                  label: 'Adres',
                  value: item.address!.trim(),
                ),
              if ((item.workingHours ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.schedule_rounded,
                  label: 'Çalışma saatleri',
                  value: item.workingHours!.trim(),
                ),
              if ((item.email ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.mail_outline_rounded,
                  label: 'E-posta',
                  value: item.email!.trim(),
                  onTap: () => AppLinks.email(item.email!),
                ),
              if ((item.websiteUrl ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.language_rounded,
                  label: 'Web sitesi',
                  value: item.websiteUrl!.trim(),
                  onTap: () => AppLinks.web(item.websiteUrl!),
                ),
            ],
          ),
        ),

        AppSpacing.gapLg,
        ContactActions(
          phone: item.phone,
          latitude: item.latitude,
          longitude: item.longitude,
          mapLabel: item.name,
          address: item.address,
          website: item.websiteUrl,
          email: item.email,
        ),

        if (!item.isActive) ...[
          AppSpacing.gapLg,
          InfoBanner(
            tone: InfoBannerTone.warning,
            message: 'Bu kayıt güncel olmayabilir.',
            icon: Icons.info_outline_rounded,
          ),
          AppSpacing.gapSm,
          Text(
            'Bilgilerde hata varsa Ayarlar → Şikayet/İstek üzerinden bize '
            'bildirebilirsiniz.',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ],
      ],
    );
  }
}
