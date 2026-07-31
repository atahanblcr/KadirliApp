import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/ads_providers.dart';
import '../application/favorite_ads_controller.dart';
import '../data/ads_repository.dart';
import '../data/models/ad_detail.dart';
import 'ads_screen.dart' show toggleAdFavorite;
import 'widgets/ad_gallery.dart';

/// İlan detayı (11.8) — `/ilanlar/<id>`; 11.13 push deep-link hedefi.
///
/// İletişim aksiyonları **alt çubukta sabit**: kullanıcı uzun açıklamayı
/// okurken "Ara"yı aramak zorunda kalmasın (pazaryeri deseni).
class AdDetailScreen extends ConsumerWidget {
  const AdDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(adDetailProvider(id));
    final isFavorite = ref.watch(isFavoriteProvider(id));

    return AppScaffold(
      title: 'İlan',
      actions: [
        if (state case AsyncData(value: final ad)) ...[
          IconButton(
            tooltip: isFavorite ? 'Favorilerden çıkar' : 'Favorilere ekle',
            icon: Icon(
              isFavorite
                  ? Icons.favorite_rounded
                  : Icons.favorite_border_rounded,
              color: isFavorite ? Theme.of(context).palette.danger : null,
            ),
            onPressed: () => toggleAdFavorite(context, ref, id),
          ),
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                adShareText(ad),
                subject: ad.title,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
        ],
      ],
      body: switch (state) {
        AsyncData(value: final ad) => _Content(ad: ad),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(adDetailProvider(id)),
        ),
        _ => const LoadingView(itemCount: 3),
      },
      bottomNavigationBar: switch (state) {
        AsyncData(value: final ad) when ad.hasPhone => _ContactBar(ad: ad),
        _ => null,
      },
    );
  }
}

/// Paylaşım metni — WhatsApp gruplarında "şuna bak" mesajı.
@visibleForTesting
String adShareText(AdDetail ad) {
  final buffer = StringBuffer('🏷️ ${ad.title}');
  buffer.write('\n💰 ${AppMoney.price(ad.price)}');
  if (ad.categoryName.trim().isNotEmpty) {
    buffer.write('\n📂 ${ad.categoryName.trim()}');
  }
  if (ad.hasPhone) buffer.write('\n📞 ${ad.contactPhone.trim()}');
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

    // 404 = silinmiş / yayından düşmüş / süresi geçmiş (backend üçünü de
    // ayırt etmeden 404 döner — varlık sızıntısı yok).
    if (api != null && api.isNotFound) {
      return const EmptyView(
        icon: Icons.search_off_rounded,
        title: 'İlan bulunamadı',
        message:
            'Bu ilan kaldırılmış, süresi dolmuş ya da yayından çıkarılmış '
            'olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'İlan alınamadı.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends StatelessWidget {
  const _Content({required this.ad});

  final AdDetail ad;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final description = ad.description.trim();
    final properties = ad.visibleProperties;

    return ListView(
      padding: const EdgeInsets.only(bottom: AppSpacing.xxl),
      children: [
        AdGallery(imageUrls: ad.imageUrls, title: ad.title),

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
              if (ad.categoryName.trim().isNotEmpty) ...[
                Text(
                  ad.categoryName.trim(),
                  style: theme.textTheme.labelLarge?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
                AppSpacing.gapXs,
              ],
              Text(ad.title, style: theme.textTheme.headlineSmall),
              AppSpacing.gapSm,
              Text(
                AppMoney.price(ad.price),
                style: theme.textTheme.headlineSmall?.copyWith(
                  color: ad.price == null
                      ? palette.muted
                      : theme.colorScheme.primary,
                  fontWeight: FontWeight.w700,
                ),
              ),
              AppSpacing.gapMd,
              Wrap(
                spacing: AppSpacing.lg,
                runSpacing: AppSpacing.xs,
                children: [
                  _MetaChip(
                    icon: Icons.schedule_rounded,
                    label: AppDate.relative(ad.createdAt),
                  ),
                  // Sunucu artıştan ÖNCEKİ sayıyı döndürüyor → "sen açmadan
                  // önce kaç kişi baktı". +1 eklemek yanıltıcı olurdu.
                  _MetaChip(
                    icon: Icons.visibility_outlined,
                    label: '${ad.viewCount} görüntülenme',
                  ),
                ],
              ),

              if (description.isNotEmpty) ...[
                AppSpacing.gapXl,
                Text('Açıklama', style: theme.textTheme.titleSmall),
                AppSpacing.gapSm,
                Text(
                  description,
                  style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
                ),
              ],

              if (properties.isNotEmpty) ...[
                AppSpacing.gapXl,
                Text('Özellikler', style: theme.textTheme.titleSmall),
                AppSpacing.gapSm,
                AppCard(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.lg,
                    vertical: AppSpacing.sm,
                  ),
                  child: Column(
                    children: [
                      for (final property in properties)
                        _PropertyRow(
                          name: property.propertyName,
                          value: property.displayValue!,
                          isLast: property == properties.last,
                        ),
                    ],
                  ),
                ),
              ],

              AppSpacing.gapXl,
              AppCard(
                child: Row(
                  children: [
                    CircleAvatar(
                      backgroundColor: theme.colorScheme.primaryContainer,
                      child: Icon(
                        Icons.person_rounded,
                        color: theme.colorScheme.onPrimaryContainer,
                      ),
                    ),
                    AppSpacing.wGapMd,
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'İlan sahibi',
                            style: theme.textTheme.labelSmall?.copyWith(
                              color: palette.muted,
                            ),
                          ),
                          AppSpacing.gapXs,
                          Text(
                            (ad.sellerName ?? '').trim().isEmpty
                                ? 'Belirtilmemiş'
                                : ad.sellerName!.trim(),
                            style: theme.textTheme.bodyLarge,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              AppSpacing.gapLg,
              Text(
                'Yayın süresi ${AppDate.date(ad.expiresAt)} tarihinde doluyor.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: palette.muted,
                ),
              ),
              AppSpacing.gapSm,
              Text(
                'Alışverişte dikkatli olun: parayı ürünü görmeden göndermeyin. '
                'Şüpheli ilanları Ayarlar → Şikayet/İstek üzerinden bildirin.',
                style: theme.textTheme.bodySmall?.copyWith(
                  color: palette.muted,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _MetaChip extends StatelessWidget {
  const _MetaChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 15, color: theme.palette.muted),
        AppSpacing.wGapXs,
        Text(
          label,
          style: theme.textTheme.labelMedium?.copyWith(
            color: theme.palette.muted,
          ),
        ),
      ],
    );
  }
}

class _PropertyRow extends StatelessWidget {
  const _PropertyRow({
    required this.name,
    required this.value,
    required this.isLast,
  });

  final String name;
  final String value;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
      decoration: isLast
          ? null
          : BoxDecoration(
              border: Border(bottom: BorderSide(color: theme.palette.border)),
            ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              name,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          ),
          AppSpacing.wGapMd,
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: theme.textTheme.bodyMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Sabit iletişim çubuğu: **Ara** ve **WhatsApp**.
///
/// İkisi de önce sayacı tetikler (`track-phone` / `track-whatsapp`), sonra dış
/// uygulamayı açar. Sayaç **beklenmez ve hatası yutulur** — sayaç yüzünden
/// kullanıcının araması gecikmemeli.
class _ContactBar extends ConsumerWidget {
  const _ContactBar({required this.ad});

  final AdDetail ad;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final repository = ref.read(adsRepositoryProvider);

    Future<void> open(
      Future<void> Function() track,
      Future<bool> Function() launch,
      String failureMessage,
    ) async {
      unawaitedTrack(track);
      final opened = await launch();
      if (!opened) {
        messenger.showSnackBar(SnackBar(content: Text(failureMessage)));
      }
    }

    return Container(
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        border: Border(top: BorderSide(color: theme.palette.border)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Row(
            children: [
              Expanded(
                child: AppButton(
                  label: 'Ara',
                  icon: Icons.call_rounded,
                  expand: true,
                  onPressed: () => open(
                    () => repository.trackPhone(ad.id),
                    () => AppLinks.call(ad.contactPhone),
                    'Arama başlatılamadı.',
                  ),
                ),
              ),
              AppSpacing.wGapSm,
              Expanded(
                child: AppButton.ghost(
                  label: 'WhatsApp',
                  icon: Icons.chat_rounded,
                  expand: true,
                  onPressed: () => open(
                    () => repository.trackWhatsapp(ad.id),
                    () => AppLinks.whatsapp(
                      ad.contactPhone,
                      message: whatsappMessage(ad),
                    ),
                    'WhatsApp açılamadı.',
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// WhatsApp'a hazır mesaj — kullanıcı "merhaba" yazmakla uğraşmasın,
/// satıcı da hangi ilan olduğunu bilsin.
@visibleForTesting
String whatsappMessage(AdDetail ad) =>
    'Merhaba, Kadirli uygulamasındaki "${ad.title}" ilanınız hakkında bilgi '
    'almak istiyorum.';

/// Sayaç isteğini bekletmeden gönderir; hatası kullanıcıya yansıtılmaz.
void unawaitedTrack(Future<void> Function() track) {
  track().catchError((_) {});
}
