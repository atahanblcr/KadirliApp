import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';
import '../../../../core/widgets/widgets.dart';
import '../../data/models/campaign.dart';
import '../../data/models/campaign_code.dart';

/// İndirim kodu modalı.
///
/// Kod kasada okunacak: **büyük, seçilebilir, tek dokunuşla kopyalanabilir.**
/// `viewedAt` sunucudaki ilk görüntüleme anıdır (uç aynı kullanıcıya yeni satır
/// açmaz) → kodu daha önce alan kullanıcıya "ne zaman aldığı" hatırlatılır.
Future<void> showCampaignCodeSheet(
  BuildContext context, {
  required Campaign campaign,
  required CampaignCode code,
}) => showModalBottomSheet<void>(
  context: context,
  isScrollControlled: true,
  showDragHandle: true,
  builder: (context) => _CampaignCodeSheet(campaign: campaign, code: code),
);

class _CampaignCodeSheet extends StatelessWidget {
  const _CampaignCodeSheet({required this.campaign, required this.code});

  final Campaign campaign;
  final CampaignCode code;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final terms = (campaign.terms ?? '').trim();

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          0,
          AppSpacing.lg,
          AppSpacing.lg,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'İndirim kodunuz',
              style: theme.textTheme.titleMedium,
              textAlign: TextAlign.center,
            ),
            AppSpacing.gapSm,
            Text(
              campaign.title,
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              textAlign: TextAlign.center,
            ),
            AppSpacing.gapLg,

            Container(
              padding: const EdgeInsets.symmetric(
                vertical: AppSpacing.lg,
                horizontal: AppSpacing.md,
              ),
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer,
                borderRadius: AppRadius.rMd,
                border: Border.all(
                  color: theme.colorScheme.primary.withValues(alpha: 0.35),
                ),
              ),
              child: SelectableText(
                code.code,
                textAlign: TextAlign.center,
                style: theme.textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2,
                  color: theme.colorScheme.onPrimaryContainer,
                ),
              ),
            ),
            AppSpacing.gapMd,

            AppButton(
              label: 'Kodu kopyala',
              icon: Icons.copy_rounded,
              expand: true,
              onPressed: () async {
                await Clipboard.setData(ClipboardData(text: code.code));
                if (!context.mounted) return;
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('Kod kopyalandı'),
                    duration: Duration(seconds: 2),
                  ),
                );
              },
            ),

            AppSpacing.gapLg,
            _Note(
              icon: Icons.event_available_rounded,
              text: 'Kampanya ${AppDate.date(campaign.endDate)} tarihine kadar geçerli.',
            ),
            AppSpacing.gapSm,
            _Note(
              icon: Icons.history_rounded,
              text: 'Kodu ilk görüntüleme: ${AppDate.dateTime(code.viewedAt)}',
            ),

            if (terms.isNotEmpty) ...[
              AppSpacing.gapLg,
              Text(
                'Kampanya koşulları',
                style: theme.textTheme.labelLarge,
              ),
              AppSpacing.gapXs,
              Text(
                terms,
                style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Note extends StatelessWidget {
  const _Note({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 15, color: palette.muted),
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
