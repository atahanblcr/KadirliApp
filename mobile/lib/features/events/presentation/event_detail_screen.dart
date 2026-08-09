import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/events_providers.dart';
import '../data/models/event.dart';

/// Etkinlik detayı (11.10) — `/etkinlikler/:id`.
///
/// Sıralama kullanıcının sorduğu sıraya göre: **ne zaman → nerede → ne kadar →
/// ne anlatıyor → kim düzenliyor.** Sayaç ucu yok (etkinlik uçları görüntülenme
/// saymıyor), bu yüzden ekran tamamen okuma odaklı.
class EventDetailScreen extends ConsumerWidget {
  const EventDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(eventDetailProvider(id));

    return AppScaffold(
      title: 'Etkinlik',
      actions: [
        if (state case AsyncData(value: final event))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                _shareText(event),
                subject: event.title,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(eventDetailProvider(id)),
      body: switch (state) {
        AsyncData(value: final event) => _Content(event: event),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(eventDetailProvider(id)),
        ),
        _ => const LoadingView(itemCount: 3),
      },
    );
  }

  /// WhatsApp'a yapıştırılacak metin — Kadirli'de duyurular gruplarda dolaşıyor.
  static String _shareText(Event event) {
    final buffer = StringBuffer('🎉 ${event.title}');
    buffer.write('\n🗓 ${AppDate.date(event.eventDate)} · ${event.timeLabel}');
    // Faz 12.4: paylaşılan metinde konum da var — WhatsApp'ta dolaşan bir
    // etkinlik duyurusunda "nerede" sorusu en çok sorulan ikinci şey.
    final place = [
      (event.venueName ?? '').trim(),
      event.locationBadge ?? '',
    ].where((value) => value.isNotEmpty).join(' · ');
    if (place.isNotEmpty) buffer.write('\n📍 $place');
    final price = event.priceLabel;
    if (price != null) buffer.write('\n🎟 $price');
    if (event.description.trim().isNotEmpty) {
      buffer.write('\n\n${event.description.trim()}');
    }
    buffer.write('\n\n— Kadirli uygulaması');
    return buffer.toString();
  }
}

class _DetailError extends StatelessWidget {
  const _DetailError({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final api = error is ApiException ? error as ApiException : null;

    // "Bulunamadı" ile "yüklenemedi" ayrı: yayından kaldırılmış etkinliğe
    // "Tekrar dene" göstermek kullanıcıyı boşuna uğraştırır.
    if (api != null && api.isNotFound) {
      return const EmptyView(
        icon: Icons.event_busy_rounded,
        title: 'Etkinlik bulunamadı',
        message: 'Bu etkinlik kaldırılmış ya da yayından çıkarılmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Etkinlik yüklenemedi.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends StatelessWidget {
  const _Content({required this.event});

  final Event event;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final countdown = event.countdownLabel();
    final isPast = event.isPast();
    final price = event.priceLabel;
    final venue = (event.venueName ?? '').trim();
    final address = (event.address ?? '').trim();
    final organizer = (event.organizer ?? '').trim();

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (event.coverImageUrl != null) ...[
          AppNetworkImage(
            url: event.coverImageUrl,
            height: 200,
            borderRadius: AppRadius.rMd,
            fallbackIcon: Icons.celebration_outlined,
          ),
          AppSpacing.gapLg,
        ],

        if (isPast) ...[
          const InfoBanner(
            icon: Icons.history_rounded,
            message: 'Bu etkinlik geçmişte kaldı.',
          ),
          AppSpacing.gapLg,
        ],

        if (event.categoryName != null) ...[
          Row(
            children: [
              Icon(
                Icons.local_activity_rounded,
                size: 16,
                color: theme.colorScheme.primary,
              ),
              AppSpacing.wGapSm,
              Expanded(
                child: Text(
                  event.categoryName!,
                  style: theme.textTheme.labelLarge?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
              ),
            ],
          ),
          AppSpacing.gapSm,
        ],

        Text(event.title, style: theme.textTheme.headlineSmall),
        AppSpacing.gapLg,

        // --- Ne zaman ---
        AppCard(
          color: theme.colorScheme.primaryContainer,
          borderColor: theme.colorScheme.primary.withValues(alpha: 0.22),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.event_rounded,
                color: theme.colorScheme.onPrimaryContainer,
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      AppDate.dayWithWeekday(event.eventDate),
                      style: theme.textTheme.titleMedium,
                    ),
                    AppSpacing.gapXs,
                    Text(
                      'Saat ${event.timeLabel}',
                      style: theme.textTheme.bodyMedium,
                    ),
                    if (countdown != null) ...[
                      AppSpacing.gapSm,
                      Text(
                        countdown,
                        style: theme.textTheme.labelLarge?.copyWith(
                          color: theme.colorScheme.primary,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),

        // --- Nerede ---
        if (venue.isNotEmpty ||
            address.isNotEmpty ||
            event.canOpenMap ||
            event.locationBadge != null) ...[
          AppSpacing.gapLg,
          AppCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Faz 12.4 — ilçe. Mekan adının ÜSTÜNDE: "Adana" bilgisi olmadan
                // "Kültür Merkezi" yazan bir satır kullanıcıyı yanıltıyordu.
                if (event.locationBadge case final location?)
                  InfoRow(
                    icon: Icons.location_city_rounded,
                    label: 'İlçe',
                    value: location,
                  ),
                if (venue.isNotEmpty)
                  InfoRow(
                    icon: Icons.place_rounded,
                    label: 'Mekan',
                    value: venue,
                  ),
                if (address.isNotEmpty)
                  InfoRow(
                    icon: Icons.map_rounded,
                    label: 'Adres',
                    value: address,
                  ),
                AppSpacing.gapSm,
                // Koordinat yoksa mekan adı + adresle harita araması yapılır
                // (`ContactActions` kuralı: veri yoksa buton hiç çizilmez).
                ContactActions(
                  latitude: event.latitude,
                  longitude: event.longitude,
                  mapLabel: venue.isEmpty ? event.title : venue,
                  address: event.mapQuery,
                ),
              ],
            ),
          ),
        ],

        // --- Ne kadar ---
        if (price != null) ...[
          AppSpacing.gapLg,
          AppCard(
            child: Row(
              children: [
                Icon(
                  Icons.confirmation_number_rounded,
                  color: event.isFree ? palette.success : palette.muted,
                ),
                AppSpacing.wGapMd,
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Bilet',
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: palette.muted,
                        ),
                      ),
                      AppSpacing.gapXs,
                      Text(price, style: theme.textTheme.titleMedium),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],

        if (event.description.trim().isNotEmpty) ...[
          AppSpacing.gapXl,
          const SectionHeader(title: 'Etkinlik hakkında'),
          SelectableText(
            event.description.trim(),
            style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
          ),
        ],

        if (organizer.isNotEmpty) ...[
          AppSpacing.gapXl,
          Row(
            children: [
              Icon(Icons.groups_rounded, size: 16, color: palette.muted),
              AppSpacing.wGapSm,
              Flexible(
                child: Text(
                  'Düzenleyen: $organizer',
                  style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                ),
              ),
            ],
          ),
        ],
      ],
    );
  }
}
