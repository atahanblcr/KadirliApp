import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/news_article.dart';

/// Haber listesi kartı (12.14).
///
/// **Neden küçük görsel, tam genişlik manşet değil:** kaynağın en büyük boyutu
/// ölçüldü — 40 haberin 39'unda `full` yalnız **650×368** (12.12 planı).
/// 360 dp'lik bir telefonda tam genişlik bir görsel 3x'te ~1080 px ister;
/// 650 px yukarı ölçeklenip **bulanık** görünürdü. Bu yüzden liste kartı
/// 96 dp'lik bir kapakla çiziliyor ve `medium` (300×170) bile fazlasıyla yetiyor.
///
/// ⚠️ Görsel yoksa kart **daralmaz, bozulmaz**: kapak alanı hiç çizilmez ve
/// metin tüm genişliği alır (haberlerin bir kısmında öne çıkan görsel yok).
class NewsCard extends StatelessWidget {
  const NewsCard({
    super.key,
    required this.article,
    this.onTap,
    this.now,
    this.isSaved = false,
  });

  final NewsArticle article;
  final VoidCallback? onTap;

  /// Testlerde/golden'da "şimdi"yi sabitlemek için.
  ///
  /// ⚠️ Bu projede göreli tarih gösteren dört kart, `now` enjekte edilemediği
  /// için golden'ı **her gün** kırdı (`CODE_REVIEW_CHECKLIST` §5).
  final DateTime? now;

  /// "Kaydedilenler"de mi (plan dışı ek) — kartta küçük bir yer imi ikonu.
  final bool isSaved;

  static const double _imageSize = 96;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final category = article.primaryCategory;
    final published = article.publishedLabel(now: now);
    final excerpt = article.excerpt.trim();

    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.all(AppSpacing.md),
      semanticLabel: [
        article.title,
        ?category,
        ?published,
        if (isSaved) 'kaydedildi',
      ].join(', '),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (category != null)
                  Text(
                    category,
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: theme.colorScheme.primary,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                Text(
                  article.title,
                  style: theme.textTheme.titleSmall,
                  maxLines: 3,
                  overflow: TextOverflow.ellipsis,
                ),
                if (excerpt.isNotEmpty) ...[
                  AppSpacing.gapXs,
                  Text(
                    excerpt,
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: palette.muted,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
                AppSpacing.gapSm,
                // ⚠️ `Wrap` + `_Meta` içi `Flexible`: dar sütunda `Row` içine
                // giren çıplak `Text` bu projede yedi kez taşma üretti.
                Wrap(
                  spacing: AppSpacing.md,
                  runSpacing: AppSpacing.xs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    if (published != null)
                      _Meta(icon: Icons.schedule_rounded, text: published),
                    _Meta(
                      icon: Icons.menu_book_rounded,
                      text: article.readingLabel,
                    ),
                    if (isSaved)
                      _Meta(
                        icon: Icons.bookmark_rounded,
                        text: 'Kaydedildi',
                        emphasized: true,
                      ),
                  ],
                ),
              ],
            ),
          ),
          if (article.imageUrl != null) ...[
            AppSpacing.wGapMd,
            AppNetworkImage(
              url: article.imageUrl,
              width: _imageSize,
              height: _imageSize,
              fallbackIcon: Icons.newspaper_outlined,
            ),
          ],
        ],
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({
    required this.icon,
    required this.text,
    this.emphasized = false,
  });

  final IconData icon;
  final String text;

  /// Renk **tek başına anlam taşımaz** — metin zaten orada (erişilebilirlik).
  final bool emphasized;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = emphasized ? theme.colorScheme.primary : theme.palette.muted;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color),
        AppSpacing.wGapXs,
        Flexible(
          child: Text(
            text,
            style: theme.textTheme.bodySmall?.copyWith(
              color: color,
              fontWeight: emphasized ? FontWeight.w600 : null,
            ),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
