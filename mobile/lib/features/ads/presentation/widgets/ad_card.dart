import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/ad_summary.dart';

/// İlan listesi kartı: kapak görseli · başlık · fiyat · tarih + görüntülenme
/// · favori kalbi.
///
/// **Neden mahalle yok:** liste DTO'su (`AdResponseDto`) kategori ya da mahalle
/// taşımıyor ve `Ad` varlığında mahalle alanı hiç yok (backend Faz 10'da
/// donduruldu). Boş bir "mahalle" satırı çizmek yerine kullanıcının gerçekten
/// karar verirken baktığı iki bilgi konuyor: **ne zaman ilan verilmiş** ve
/// **kaç kişi bakmış**.
class AdCard extends StatelessWidget {
  const AdCard({
    super.key,
    required this.ad,
    required this.onTap,
    this.isFavorite = false,
    this.onFavoriteTap,
    this.now,
  });

  final AdSummary ad;
  final VoidCallback onTap;
  final bool isFavorite;

  /// Null ise kalp hiç çizilmez (favori özelliğinin kapalı olduğu bağlamlar).
  final VoidCallback? onFavoriteTap;

  /// Testlerde "şimdi"yi sabitlemek için — **golden testinin şartı.**
  ///
  /// ⚠️ `AppDate.relative` `now` verilmezse **gerçek saate** bakar; kart bunu
  /// iletmediği sürece golden referansı zamanla kendiliğinden çürür: fixture
  /// "2 gün önce" diye üretilir, aylar sonra aynı fixture "1 Ağustos 2026"
  /// basar ve test, kodda hiçbir şey değişmeden kırmızıya döner. 12.4'te tam
  /// bu yaşandı — `AnnouncementTile`/`ComplaintCard`/`NotificationTile` daha
  /// önce aynı sebeple düzeltilmişti, `AdCard` atlanmıştı.
  final DateTime? now;

  static const double _imageSize = 104;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      onTap: onTap,
      semanticLabel: '${ad.title}, ${AppMoney.price(ad.price)}',
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AppNetworkImage(
            url: ad.coverImageUrl,
            width: _imageSize,
            height: _imageSize,
            fallbackIcon: Icons.image_not_supported_outlined,
          ),
          AppSpacing.wGapMd,
          Expanded(
            // 🐛 Testin yakaladığı hata: burada **sabit** `SizedBox(height: 104)`
            // vardı; yazı ölçeği 1.4'e çıkınca içerik sığmayıp `RenderFlex`
            // dikey taşması veriyordu. Artık 104 yalnız **alt sınır**: kart
            // görselden kısa olmaz ama gerekirse uzar (11.7'deki `PharmacyTile`
            // taşmasının aynı sınıfı).
            child: ConstrainedBox(
              constraints: const BoxConstraints(minHeight: _imageSize),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Text(
                          ad.title,
                          style: theme.textTheme.titleSmall,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (onFavoriteTap != null)
                        _FavoriteButton(
                          isFavorite: isFavorite,
                          title: ad.title,
                          onTap: onFavoriteTap!,
                        ),
                    ],
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      AppSpacing.gapSm,
                      Text(
                        AppMoney.price(ad.price),
                        style: theme.textTheme.titleMedium?.copyWith(
                          color: ad.price == null
                              ? palette.muted
                              : theme.colorScheme.primary,
                          fontWeight: FontWeight.w700,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      AppSpacing.gapXs,
                      // 🐛 Testin yakaladığı ikinci hata: tarih + görüntülenme
                      // tek `Row`'daydı ve dar ekran + 1.4 ölçekte yatay taşma
                      // veriyordu. `Wrap` ile görüntülenme gerekirse alt satıra
                      // iner — bilgi kesilmez, düzen bozulmaz.
                      Wrap(
                        spacing: AppSpacing.sm,
                        runSpacing: AppSpacing.xxs,
                        children: [
                          _MetaBit(
                            icon: Icons.schedule_rounded,
                            label: AppDate.relative(ad.createdAt, now: now),
                          ),
                          if (ad.viewCount > 0)
                            _MetaBit(
                              icon: Icons.visibility_outlined,
                              label: '${ad.viewCount}',
                            ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Kartın alt satırındaki küçük bilgi (ikon + metin).
///
/// `Wrap` içinde yaşadığı için metin **esnek**: sığmazsa kısalır, satırı
/// taşırmaz.
class _MetaBit extends StatelessWidget {
  const _MetaBit({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final style = theme.textTheme.labelSmall?.copyWith(
      color: theme.palette.muted,
    );

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: theme.palette.muted),
        AppSpacing.wGapXs,
        Flexible(
          child: Text(label, style: style, maxLines: 1, overflow: TextOverflow.ellipsis),
        ),
      ],
    );
  }
}

class _FavoriteButton extends StatelessWidget {
  const _FavoriteButton({
    required this.isFavorite,
    required this.title,
    required this.onTap,
  });

  final bool isFavorite;
  final String title;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Semantics(
      button: true,
      selected: isFavorite,
      label: isFavorite
          ? '$title favorilerden çıkar'
          : '$title favorilere ekle',
      child: SizedBox(
        width: 36,
        height: 36,
        child: IconButton(
          padding: EdgeInsets.zero,
          visualDensity: VisualDensity.compact,
          iconSize: 20,
          // Semantics zaten etiketi veriyor; tooltip ekran okuyucuda
          // etiketi ikinci kez okutur.
          icon: Icon(
            isFavorite ? Icons.favorite_rounded : Icons.favorite_border_rounded,
            color: isFavorite ? theme.palette.danger : theme.palette.muted,
          ),
          onPressed: onTap,
        ),
      ),
    );
  }
}
