import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/announcements_providers.dart';
import '../data/announcements_repository.dart';
import '../data/models/announcement.dart';

/// Duyuru detayı (11.6).
///
/// **Sayaçlar:** ekran açılıp duyuru *gerçekten yüklendiğinde* bir kez
/// `POST /{id}/view`; içerideki dış bağlantıya dokunulduğunda
/// `POST /{id}/click` (10.12'de eklenen etkileşim sayaçları). İkisi de
/// sessizdir: hata olursa kullanıcıya yansımaz, ekran çalışmaya devam eder.
class AnnouncementDetailScreen extends ConsumerStatefulWidget {
  const AnnouncementDetailScreen({super.key, required this.id});

  final String id;

  @override
  ConsumerState<AnnouncementDetailScreen> createState() =>
      _AnnouncementDetailScreenState();
}

class _AnnouncementDetailScreenState
    extends ConsumerState<AnnouncementDetailScreen> {
  bool _viewTracked = false;

  /// Görüntülenme yalnız **başarılı yüklemeden sonra** sayılır: silinmiş bir
  /// duyurunun bağlantısına tıklanması istatistiği kirletmemeli.
  void _trackViewOnce() {
    if (_viewTracked) return;
    _viewTracked = true;
    ref
        .read(announcementsRepositoryProvider)
        .trackView(widget.id)
        .catchError((Object _) {});
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(announcementDetailProvider(widget.id));

    // Bir kare sonra: `build` sırasında ağ isteği başlatmak provider'ı
    // build içinde değiştirmek olurdu. Çağrı zaten idempotent.
    if (state case AsyncData()) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) _trackViewOnce();
      });
    }

    return AppScaffold(
      title: 'Duyuru',
      actions: [
        if (state case AsyncData(value: final announcement))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                _shareText(announcement),
                subject: announcement.title,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async =>
          ref.invalidate(announcementDetailProvider(widget.id)),
      body: switch (state) {
        AsyncData(value: final announcement) => _Content(
          announcement: announcement,
        ),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(announcementDetailProvider(widget.id)),
        ),
        _ => const LoadingView(itemCount: 3),
      },
    );
  }

  static String _shareText(Announcement announcement) {
    final buffer = StringBuffer('📢 ${announcement.title}');
    if (announcement.body.trim().isNotEmpty) {
      buffer.write('\n\n${announcement.body.trim()}');
    }
    final published = announcement.publishedAt;
    if (published != null) {
      buffer.write('\n\n🗓 ${AppDate.dateTime(published)}');
    }
    buffer.write('\n\n— Kadirli uygulaması');
    return buffer.toString();
  }
}

/// Hata ekranı: "bulunamadı" ile "yüklenemedi" bilinçli olarak ayrılır.
///
/// Silinmiş/süresi geçmiş duyuruya (ör. eski bir bildirimden) gelen kullanıcıya
/// "Tekrar dene" göstermek anlamsız — tekrar denemek de bulmayacak.
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
        title: 'Duyuru bulunamadı',
        message: 'Bu duyuru kaldırılmış ya da yayından çıkarılmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Duyuru yüklenemedi.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends ConsumerWidget {
  const _Content({required this.announcement});

  final Announcement announcement;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final published = announcement.publishedAt;

    final priority = announcement.priorityLevel;
    final (priorityLabel, priorityColor) = switch (priority) {
      AnnouncementPriority.urgent => ('Acil duyuru', palette.danger),
      AnnouncementPriority.high => ('Önemli duyuru', palette.warning),
      AnnouncementPriority.normal => (null, palette.muted),
    };

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (announcement.imageUrl != null) ...[
          AppNetworkImage(
            url: announcement.imageUrl,
            height: 200,
            borderRadius: AppRadius.rMd,
          ),
          AppSpacing.gapLg,
        ],

        if (priorityLabel != null) ...[
          InfoBanner(
            tone: priority == AnnouncementPriority.urgent
                ? InfoBannerTone.danger
                : InfoBannerTone.warning,
            message: priorityLabel,
          ),
          AppSpacing.gapLg,
        ],

        if (announcement.typeName != null) ...[
          Row(
            children: [
              Icon(Icons.label_rounded, size: 16, color: theme.colorScheme.primary),
              AppSpacing.wGapSm,
              Expanded(
                child: Text(
                  announcement.typeName!,
                  style: theme.textTheme.labelLarge?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
              ),
            ],
          ),
          AppSpacing.gapSm,
        ],

        Text(announcement.title, style: theme.textTheme.headlineSmall),

        if (published != null) ...[
          AppSpacing.gapSm,
          Text(
            '${AppDate.dateTime(published)} · ${AppDate.relative(published)}',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ],

        if (announcement.body.trim().isNotEmpty) ...[
          AppSpacing.gapLg,
          SelectableText(
            announcement.body.trim(),
            style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
          ),
        ],

        if (announcement.visibleUntil != null) ...[
          AppSpacing.gapLg,
          _MetaRow(
            icon: Icons.schedule_rounded,
            text:
                '${AppDate.dateTime(announcement.visibleUntil!)} tarihine kadar geçerli',
          ),
        ],

        if ((announcement.source ?? '').trim().isNotEmpty) ...[
          AppSpacing.gapSm,
          _MetaRow(
            icon: Icons.account_balance_rounded,
            text: 'Kaynak: ${announcement.source!.trim()}',
          ),
        ],

        if (announcement.hasLocation &&
            announcement.latitude != null &&
            announcement.longitude != null) ...[
          AppSpacing.gapXl,
          _LocationCard(announcement: announcement),
        ],

        if (announcement.hasLink &&
            (announcement.externalLink ?? '').trim().isNotEmpty) ...[
          AppSpacing.gapXl,
          _LinkButton(
            announcement: announcement,
            url: announcement.externalLink!.trim(),
          ),
        ] else if ((announcement.sourceUrl ?? '').trim().isNotEmpty) ...[
          // Dış bağlantı yok ama kaynak adresi var → sayaçsız aç
          // (`click` ucu duyurunun kendi bağlantısı içindir).
          AppSpacing.gapXl,
          AppButton.ghost(
            label: 'Kaynağa git',
            icon: Icons.open_in_new_rounded,
            expand: true,
            onPressed: () => AppLinks.web(announcement.sourceUrl!.trim()),
          ),
        ],
      ],
    );
  }
}

/// Dış bağlantı butonu — açmadan **önce** tıklama sayacını tetikler.
class _LinkButton extends ConsumerStatefulWidget {
  const _LinkButton({required this.announcement, required this.url});

  final Announcement announcement;
  final String url;

  @override
  ConsumerState<_LinkButton> createState() => _LinkButtonState();
}

class _LinkButtonState extends ConsumerState<_LinkButton> {
  bool _busy = false;

  Future<void> _open() async {
    setState(() => _busy = true);
    // Sayaç bağlantıyı bekletmemeli: hata olsa da sayfa açılır.
    unawaited(
      ref
          .read(announcementsRepositoryProvider)
          .trackClick(widget.announcement.id)
          .catchError((Object _) {}),
    );

    final opened = await AppLinks.web(widget.url);
    if (!mounted) return;
    setState(() => _busy = false);
    if (!opened) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Bağlantı açılamadı.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) => AppButton(
    label: 'Bağlantıyı aç',
    icon: Icons.open_in_new_rounded,
    expand: true,
    loading: _busy,
    onPressed: _open,
  );
}

class _LocationCard extends StatelessWidget {
  const _LocationCard({required this.announcement});

  final Announcement announcement;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final label = (announcement.locationName ?? '').trim();

    return AppCard(
      onTap: () => AppLinks.map(
        latitude: announcement.latitude!,
        longitude: announcement.longitude!,
        label: label.isEmpty ? announcement.title : label,
      ),
      child: Row(
        children: [
          Icon(Icons.place_rounded, color: theme.colorScheme.primary),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label.isEmpty ? 'Konum' : label,
                  style: theme.textTheme.titleSmall,
                ),
                Text(
                  'Haritada aç',
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.palette.muted,
                  ),
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_right_rounded, color: theme.palette.muted),
        ],
      ),
    );
  }
}

class _MetaRow extends StatelessWidget {
  const _MetaRow({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 16, color: palette.muted),
        AppSpacing.wGapSm,
        Expanded(
          child: Text(
            text,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ),
      ],
    );
  }
}
