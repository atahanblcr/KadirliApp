import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/power_outages_providers.dart';
import '../data/models/power_outage.dart';

/// Kesinti detayı (11.6) — 11.13 push deep-link hedefi.
class PowerOutageDetailScreen extends ConsumerStatefulWidget {
  const PowerOutageDetailScreen({super.key, required this.id});

  final String id;

  @override
  ConsumerState<PowerOutageDetailScreen> createState() =>
      _PowerOutageDetailScreenState();
}

class _PowerOutageDetailScreenState
    extends ConsumerState<PowerOutageDetailScreen> {
  Timer? _ticker;
  DateTime _now = DateTime.now();

  @override
  void initState() {
    super.initState();
    _ticker = Timer.periodic(const Duration(seconds: 30), (_) {
      if (mounted) setState(() => _now = DateTime.now());
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(powerOutageDetailProvider(widget.id));

    return AppScaffold(
      title: 'Kesinti',
      actions: [
        if (state case AsyncData(value: final outage))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                shareTextOf(outage),
                subject: 'Elektrik kesintisi — ${outage.placeLabel}',
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async =>
          ref.invalidate(powerOutageDetailProvider(widget.id)),
      body: switch (state) {
        AsyncData(value: final outage) => _Content(outage: outage, now: _now),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(powerOutageDetailProvider(widget.id)),
        ),
        _ => const LoadingView(itemCount: 2, hasImage: false),
      },
    );
  }

}

/// Paylaşım metni — WhatsApp'ta tek bakışta okunacak biçimde (mahalle, tarih
/// aralığı, süre, sebep). Görünür for test: ekran testleri metni doğrular.
@visibleForTesting
String shareTextOf(PowerOutage outage) {
  final buffer = StringBuffer('⚡ Elektrik kesintisi — ${outage.placeLabel}')
    ..write('\n🗓 ${AppDate.range(outage.startTime, outage.endTime)}')
    ..write('\n⏳ Süre: ${AppDate.duration(outage.duration)}');
  final reason = outage.reason?.trim();
  if (reason != null && reason.isNotEmpty) {
    buffer.write('\nSebep: $reason');
  }
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

    // ⚠️ Bu uç da bulunamayan kaydı 200 + success:false ile döndürüyor;
    // zarf interceptor'ı NOT_FOUND'a çevirdiği için burada tek kontrol yeter.
    if (api != null && api.isNotFound) {
      return const EmptyView(
        icon: Icons.search_off_rounded,
        title: 'Kesinti kaydı bulunamadı',
        message: 'Bu kesinti kaldırılmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Kesinti bilgisi alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends StatelessWidget {
  const _Content({required this.outage, required this.now});

  final PowerOutage outage;
  final DateTime now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final status = outage.status(now: now);
    final remaining = outage.remaining(now: now);

    final (bannerTone, bannerTitle, bannerMessage) = switch (status) {
      PowerOutageStatus.active => (
        InfoBannerTone.danger,
        'Kesinti şu an sürüyor',
        remaining == null
            ? 'Elektrikler kesik.'
            : 'Elektriklerin gelmesine yaklaşık ${AppDate.duration(remaining)} var.',
      ),
      PowerOutageStatus.upcoming => (
        InfoBannerTone.warning,
        'Planlanan kesinti',
        remaining == null
            ? 'Kesinti planlandı.'
            : 'Kesintiye yaklaşık ${AppDate.duration(remaining)} kaldı.',
      ),
      PowerOutageStatus.past => (
        InfoBannerTone.success,
        'Kesinti sona erdi',
        'Bu kesinti tamamlandı, kayıt arşivde tutuluyor.',
      ),
    };

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        InfoBanner(
          tone: bannerTone,
          title: bannerTitle,
          message: bannerMessage,
        ),
        AppSpacing.gapXl,

        Text(outage.placeLabel, style: theme.textTheme.headlineSmall),
        AppSpacing.gapLg,

        AppCard(
          child: Column(
            children: [
              _DetailRow(
                icon: Icons.play_circle_outline_rounded,
                label: 'Başlangıç',
                value: AppDate.dateTime(outage.startTime),
              ),
              Divider(height: AppSpacing.xl, color: palette.border),
              _DetailRow(
                icon: Icons.stop_circle_outlined,
                label: 'Bitiş',
                value: AppDate.dateTime(outage.endTime),
              ),
              Divider(height: AppSpacing.xl, color: palette.border),
              _DetailRow(
                icon: Icons.hourglass_bottom_rounded,
                label: 'Süre',
                value: AppDate.duration(outage.duration),
              ),
            ],
          ),
        ),

        if ((outage.reason ?? '').trim().isNotEmpty) ...[
          AppSpacing.gapXl,
          const SectionHeader(title: 'Sebep'),
          AppCard(
            child: Text(
              outage.reason!.trim(),
              style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
            ),
          ),
        ],

        AppSpacing.gapXl,
        // Kesinti bilgisi paylaşılmak için var: mahalle/aile gruplarına
        // gönderilmesi bu modülün asıl faydası.
        Builder(
          builder: (context) => AppButton.ghost(
            label: 'Kesintiyi paylaş',
            icon: Icons.ios_share_rounded,
            expand: true,
            onPressed: () => AppShare.text(
              shareTextOf(outage),
              subject: 'Elektrik kesintisi — ${outage.placeLabel}',
              origin: AppShare.originOf(context),
            ),
          ),
        ),
      ],
    );
  }
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(icon, size: 20, color: theme.colorScheme.primary),
        AppSpacing.wGapMd,
        Expanded(
          child: Text(
            label,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: theme.palette.muted,
            ),
          ),
        ),
        Text(
          value,
          style: theme.textTheme.bodyMedium?.copyWith(
            fontWeight: FontWeight.w700,
          ),
        ),
      ],
    );
  }
}
