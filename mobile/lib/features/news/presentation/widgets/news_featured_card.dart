import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/news_article.dart';

/// **Manşet kartı** — panelde "öne çıkar" işaretlenmiş haber (plan dışı ek).
///
/// Liste kartından farkı bilinçli: burada görsel **tam genişlik** ve kaynağın
/// `full` boyutu (650×368) kullanılıyor. 12.14 planının notu birebir geçerli:
/// *"⭐ Öne çıkan haber tam genişlik olacaksa `full` kullanılmalı"* — ve bu bile
/// 3x telefonda yukarı ölçekleniyor, **kabul edilen bir yumuşaklık**. Kartın
/// oranı (16:9) kaynağınkine (650×368 ≈ 16:9.06) bilerek yakın seçildi: farklı
/// bir oran, kaynağın zaten sınırlı görselini ayrıca **kırpardı**.
///
/// Şerit yatay kaydırılabilir olduğu için kart **sabit genişlikte**; ekran
/// genişliğinin %78'i, yani sağdan bir sonraki kartın kenarı görünür ve
/// "kaydırılabilir" olduğu kendini anlatır.
class NewsFeaturedCard extends StatelessWidget {
  const NewsFeaturedCard({
    super.key,
    required this.article,
    required this.width,
    this.onTap,
    this.now,
  });

  final NewsArticle article;
  final double width;
  final VoidCallback? onTap;

  /// Golden/test için sabitlenebilir "şimdi" (bkz. [NewsCard.now]).
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final published = article.publishedLabel(now: now);

    return SizedBox(
      width: width,
      child: AppCard(
        onTap: onTap,
        padding: EdgeInsets.zero,
        semanticLabel: 'Öne çıkan haber: ${article.title}',
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Görsel yoksa kart yine çizilir; başlık tek başına manşet olur.
            if (article.imageUrl != null)
              AspectRatio(
                aspectRatio: 16 / 9,
                child: AppNetworkImage(
                  url: article.imageUrl,
                  borderRadius: const BorderRadius.vertical(
                    top: Radius.circular(AppRadius.md),
                  ),
                  fallbackIcon: Icons.newspaper_outlined,
                ),
              ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        Icons.star_rounded,
                        size: 14,
                        color: theme.colorScheme.primary,
                      ),
                      AppSpacing.wGapXs,
                      // ⚠️ `Flexible`: kategori adı uzun olduğunda (ör.
                      // "Bilim ve Teknoloji") 1.4 yazı ölçeğinde satır taşardı.
                      Flexible(
                        child: Text(
                          article.primaryCategory ?? 'Öne çıkan',
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: theme.colorScheme.primary,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  AppSpacing.gapXs,
                  Text(
                    article.title,
                    style: theme.textTheme.titleSmall,
                    maxLines: 3,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (published != null) ...[
                    AppSpacing.gapXs,
                    Text(
                      published,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: palette.muted,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
