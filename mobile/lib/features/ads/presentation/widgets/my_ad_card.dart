import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/my_ad.dart';

/// "İlanlarım" listesindeki kart.
///
/// Public `AdCard`'dan farkı: **durum**, **red gerekçesi**, **performans
/// sayaçları** ve yönetim aksiyonları. Kullanıcı buraya "ilanım ne oldu?"
/// sorusuyla geliyor — cevabı kartın kendisi vermeli.
class MyAdCard extends StatelessWidget {
  const MyAdCard({
    super.key,
    required this.ad,
    required this.onTap,
    required this.onEdit,
    required this.onDelete,
    required this.onExtend,
    this.busy = false,
  });

  final MyAd ad;
  final VoidCallback onTap;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final VoidCallback onExtend;

  /// Bu ilan üzerinde bir istek sürüyor (uzat/sil) → butonlar kilitli.
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final status = ad.statusKind;

    return AppCard(
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          InkWell(
            onTap: onTap,
            borderRadius: AppRadius.rMd,
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AppNetworkImage(
                    url: ad.coverImageUrl,
                    width: 84,
                    height: 84,
                    borderRadius: AppRadius.rSm,
                  ),
                  AppSpacing.wGapMd,
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        _StatusChip(status: status),
                        AppSpacing.gapXs,
                        Text(
                          ad.title,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.titleSmall,
                        ),
                        AppSpacing.gapXs,
                        Text(
                          AppMoney.price(ad.price),
                          style: theme.textTheme.bodyLarge?.copyWith(
                            color: ad.price == null
                                ? palette.muted
                                : theme.colorScheme.primary,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),

          if (status == AdStatus.rejected && (ad.rejectedReason ?? '').trim().isNotEmpty)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                0,
                AppSpacing.md,
                AppSpacing.md,
              ),
              child: InfoBanner(
                tone: InfoBannerTone.danger,
                title: 'Yayınlanmama gerekçesi',
                message: ad.rejectedReason!.trim(),
              ),
            ),

          if (status == AdStatus.pending)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                0,
                AppSpacing.md,
                AppSpacing.md,
              ),
              child: Text(
                AdStatus.pending.description,
                style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              ),
            ),

          // Süre uyarısı: yayındaki ilanın bitimine az kaldıysa ya da süresi
          // dolduysa kullanıcıyı burada yakalamak lazım — listeden çıkınca
          // bir daha bakmıyor.
          if (status == AdStatus.expired || ad.isExpiringSoon)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.md,
                0,
                AppSpacing.md,
                AppSpacing.md,
              ),
              child: InfoBanner(
                tone: status == AdStatus.expired
                    ? InfoBannerTone.warning
                    : InfoBannerTone.info,
                message: status == AdStatus.expired
                    ? 'Yayın süresi ${AppDate.date(ad.expiresAt)} tarihinde doldu.'
                    : 'Yayın süresinin bitmesine ${ad.daysUntilExpiry} gün kaldı.',
              ),
            ),

          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.md,
              0,
              AppSpacing.md,
              AppSpacing.sm,
            ),
            child: Wrap(
              spacing: AppSpacing.lg,
              runSpacing: AppSpacing.xs,
              children: [
                _Stat(icon: Icons.visibility_outlined, value: ad.viewCount, label: 'görüntülenme'),
                _Stat(icon: Icons.call_outlined, value: ad.phoneClickCount, label: 'arama'),
                _Stat(icon: Icons.chat_outlined, value: ad.whatsappClickCount, label: 'WhatsApp'),
                _Stat(icon: Icons.favorite_outline_rounded, value: ad.favoriteCount, label: 'favori'),
              ],
            ),
          ),

          Divider(height: 1, thickness: 1, color: palette.border),
          Padding(
            padding: const EdgeInsets.all(AppSpacing.sm),
            child: Row(
              children: [
                Expanded(
                  child: AppButton.ghost(
                    label: 'Düzenle',
                    icon: Icons.edit_outlined,
                    size: AppButtonSize.small,
                    expand: true,
                    onPressed: busy ? null : onEdit,
                  ),
                ),
                AppSpacing.wGapSm,
                Expanded(
                  child: AppButton.ghost(
                    label: ad.canExtend
                        ? 'Uzat (${ad.remainingExtensions})'
                        : 'Uzat',
                    icon: Icons.update_rounded,
                    size: AppButtonSize.small,
                    expand: true,
                    // Hakkı bitmiş / uygun statüde olmayan ilanda buton
                    // devre dışı: "işlevsiz buton yok" kuralı (11.4).
                    onPressed: busy || !ad.canExtend ? null : onExtend,
                  ),
                ),
                AppSpacing.wGapSm,
                IconButton(
                  tooltip: 'İlanı sil',
                  onPressed: busy ? null : onDelete,
                  icon: Icon(Icons.delete_outline_rounded, color: palette.danger),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status});

  final AdStatus status;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final (color, icon) = switch (status) {
      AdStatus.approved => (palette.success, Icons.check_circle_outline_rounded),
      AdStatus.pending => (palette.warning, Icons.hourglass_empty_rounded),
      AdStatus.rejected => (palette.danger, Icons.cancel_outlined),
      AdStatus.expired => (palette.muted, Icons.schedule_rounded),
      AdStatus.unknown => (palette.muted, Icons.help_outline_rounded),
    };

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: AppRadius.rPill,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13, color: color),
          AppSpacing.wGapXs,
          // Renk tek başına yetmez (11.4 kararı) → durum her zaman METİNLE.
          Text(
            status.label,
            style: theme.textTheme.labelSmall?.copyWith(
              color: color,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.icon, required this.value, required this.label});

  final IconData icon;
  final int value;
  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Semantics(
      label: '$value $label',
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: theme.palette.muted),
          AppSpacing.wGapXs,
          Text(
            '$value',
            style: theme.textTheme.labelMedium?.copyWith(
              color: theme.palette.muted,
            ),
          ),
        ],
      ),
    );
  }
}
