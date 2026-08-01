import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/campaign.dart';

/// Kampanya listesi kartı.
///
/// Kapak görseli üstte geniş: kampanya görseli esnafın vitrinidir ve çoğu
/// kampanya görsel olmadan da anlaşılsın diye indirim oranı **metin rozetiyle**
/// tekrarlanır (renk tek başına anlam taşımaz).
class CampaignCard extends StatelessWidget {
  const CampaignCard({super.key, required this.campaign, this.onTap, this.now});

  final Campaign campaign;
  final VoidCallback? onTap;

  /// Testlerde "bugün"ü sabitlemek için.
  final DateTime? now;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final discount = campaign.discountLabel;
    final urgency = campaign.urgencyLabel(now: now);
    final business = (campaign.businessName ?? '').trim();

    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.zero,
      semanticLabel: '${campaign.title}${business.isEmpty ? '' : ', $business'}',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (campaign.coverImageUrl != null)
            AppNetworkImage(
              url: campaign.coverImageUrl,
              height: 140,
              borderRadius: const BorderRadius.only(
                topLeft: Radius.circular(AppRadius.md),
                topRight: Radius.circular(AppRadius.md),
              ),
              fallbackIcon: Icons.local_offer_outlined,
            ),
          Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (business.isNotEmpty)
                  Row(
                    children: [
                      Icon(
                        Icons.storefront_rounded,
                        size: 14,
                        color: theme.colorScheme.primary,
                      ),
                      AppSpacing.wGapXs,
                      // ⚠️ Dar sütunda çıplak Text taşar (11.7-11.9 tuzağı).
                      Flexible(
                        child: Text(
                          business,
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
                  campaign.title,
                  style: theme.textTheme.titleSmall,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                AppSpacing.gapSm,
                Wrap(
                  spacing: AppSpacing.sm,
                  runSpacing: AppSpacing.xs,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    if (discount != null)
                      _Badge(
                        label: '$discount indirim',
                        color: palette.success,
                        // ⚠️ `percent` ikonu değil: etiket zaten "%25" ile
                        // başlıyor, ikonla birlikte "% %25 indirim" gibi çift
                        // yüzde okunuyordu (canlı kontrolde görüldü).
                        icon: Icons.local_offer_rounded,
                      ),
                    if (campaign.hasCode)
                      _Badge(
                        label: 'İndirim kodu',
                        color: theme.colorScheme.primary,
                        icon: Icons.confirmation_number_rounded,
                      ),
                    if (urgency != null)
                      _Badge(
                        label: urgency,
                        color: palette.warning,
                        icon: Icons.timer_outlined,
                      ),
                  ],
                ),
                AppSpacing.gapSm,
                Text(
                  campaign.validityLabel,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: palette.muted,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({required this.label, required this.color, required this.icon});

  final String label;
  final Color color;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 3,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: AppRadius.rPill,
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: color),
          AppSpacing.wGapXs,
          // ⚠️ `Wrap` çocuğuna sınırlı genişlik verir → 1.4 yazı ölçeğinde
          // "%25 indirim" rozeti 49 px taşıyordu (test yakaladı). Bu projenin
          // dördüncü kez tekrarlayan tuzağı: Row içindeki çıplak Text.
          Flexible(
            child: Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: theme.textTheme.labelSmall?.copyWith(
                color: color,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
