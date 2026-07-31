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
  });

  final AdSummary ad;
  final VoidCallback onTap;
  final bool isFavorite;

  /// Null ise kalp hiç çizilmez (favori özelliğinin kapalı olduğu bağlamlar).
  final VoidCallback? onFavoriteTap;

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
            child: SizedBox(
              height: _imageSize,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
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
                  const Spacer(),
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
                  Row(
                    children: [
                      Icon(
                        Icons.schedule_rounded,
                        size: 13,
                        color: palette.muted,
                      ),
                      AppSpacing.wGapXs,
                      // Taşmayı Expanded engelliyor: "12 Ağustos 2026" + büyük
                      // yazı ölçeğinde satır dolabiliyor (11.7'de PharmacyTile
                      // aynı hatadan RenderFlex taşması vermişti).
                      Expanded(
                        child: Text(
                          AppDate.relative(ad.createdAt),
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: palette.muted,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (ad.viewCount > 0) ...[
                        AppSpacing.wGapSm,
                        Icon(
                          Icons.visibility_outlined,
                          size: 13,
                          color: palette.muted,
                        ),
                        AppSpacing.wGapXs,
                        Text(
                          '${ad.viewCount}',
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: palette.muted,
                          ),
                        ),
                      ],
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
