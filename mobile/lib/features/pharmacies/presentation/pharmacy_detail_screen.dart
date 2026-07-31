import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/pharmacies_providers.dart';
import '../data/models/duty_schedule.dart';
import '../data/models/pharmacy.dart';

/// Eczane detayı (11.7) — 11.13 push deep-link hedefi.
class PharmacyDetailScreen extends ConsumerWidget {
  const PharmacyDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(pharmacyDetailProvider(id));

    return AppScaffold(
      title: 'Eczane',
      actions: [
        if (state case AsyncData(value: final pharmacy))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                pharmacyShareText(pharmacy),
                subject: pharmacy.name,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(pharmacyDetailProvider(id)),
      body: switch (state) {
        AsyncData(value: final pharmacy) => _Content(pharmacy: pharmacy),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(pharmacyDetailProvider(id)),
        ),
        _ => const LoadingView(itemCount: 2, hasImage: false),
      },
    );
  }
}

/// Paylaşım metni — adres + telefon, WhatsApp'ta tek bakışta okunur.
@visibleForTesting
String pharmacyShareText(Pharmacy pharmacy) {
  final buffer = StringBuffer('💊 ${pharmacy.name}');
  final address = pharmacy.address?.trim();
  if (address != null && address.isNotEmpty) buffer.write('\n📍 $address');
  final phone = pharmacy.phone?.trim();
  if (phone != null && phone.isNotEmpty) buffer.write('\n📞 $phone');
  final hours = pharmacy.workingHours?.trim();
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
        title: 'Eczane bulunamadı',
        message: 'Bu kayıt kaldırılmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Eczane bilgisi alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends ConsumerWidget {
  const _Content({required this.pharmacy});

  final Pharmacy pharmacy;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
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
        Text(pharmacy.name, style: theme.textTheme.headlineSmall),
        if ((pharmacy.pharmacistName ?? '').trim().isNotEmpty) ...[
          AppSpacing.gapXs,
          Text(
            pharmacy.pharmacistName!.trim(),
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
          ),
        ],
        if (!pharmacy.isActive) ...[
          AppSpacing.gapLg,
          const InfoBanner(
            tone: InfoBannerTone.warning,
            message: 'Bu eczane şu anda hizmet vermiyor olabilir.',
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
              if ((pharmacy.address ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.place_rounded,
                  label: 'Adres',
                  value: pharmacy.address!.trim(),
                ),
              if ((pharmacy.phone ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.call_rounded,
                  label: 'Telefon',
                  value: pharmacy.phone!.trim(),
                  onTap: () => AppLinks.call(pharmacy.phone!),
                ),
              if ((pharmacy.workingHours ?? '').trim().isNotEmpty)
                InfoRow(
                  icon: Icons.schedule_rounded,
                  label: 'Çalışma saatleri',
                  value: pharmacy.workingHours!.trim(),
                ),
            ],
          ),
        ),

        AppSpacing.gapLg,
        ContactActions(
          phone: pharmacy.phone,
          latitude: pharmacy.latitude,
          longitude: pharmacy.longitude,
          mapLabel: pharmacy.name,
          address: pharmacy.address,
          callLabel: 'Eczaneyi ara',
        ),

        AppSpacing.gapXl,
        _DutyDaysSection(pharmacyId: pharmacy.id),
      ],
    );
  }
}

/// "Bu ay nöbetçi olduğu günler" — **ek uç yok**, takvim sekmesinin zaten
/// çektiği aylık liste süzülüyor (aynı provider, aynı önbellek).
class _DutyDaysSection extends ConsumerWidget {
  const _DutyDaysSection({required this.pharmacyId});

  final String pharmacyId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final month = ref.watch(dutyMonthProvider);
    final schedule = ref.watch(dutyScheduleProvider(month));

    return switch (schedule) {
      AsyncData(value: final entries) => _list(
        context,
        theme,
        dutyDaysOf(entries, pharmacyId),
      ),
      // Takvim alınamadıysa detay ekranını hataya düşürmeye değmez.
      _ => const SizedBox.shrink(),
    };
  }

  Widget _list(BuildContext context, ThemeData theme, List<DutySchedule> days) {
    if (days.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const SectionHeader(title: 'Bu ayki nöbet günleri'),
        AppCard(
          child: Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: [
              for (final day in days)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.md,
                    vertical: AppSpacing.sm,
                  ),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.primaryContainer,
                    borderRadius: AppRadius.rPill,
                  ),
                  child: Text(
                    day.hours == null
                        ? AppDate.dateShort(day.dutyDate)
                        : '${AppDate.dateShort(day.dutyDate)} · ${day.hours}',
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.onPrimaryContainer,
                    ),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
