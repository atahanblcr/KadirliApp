import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/presentation/widgets/login_required_sheet.dart';
import '../application/campaigns_providers.dart';
import '../data/campaigns_repository.dart';
import '../data/models/campaign.dart';
import 'widgets/campaign_code_sheet.dart';

/// Kampanya detayı (11.10) — `/kampanyalar/:id`.
///
/// Ekranın tek gerçek aksiyonu **"Kodu göster"**: `POST /{id}/view-code`
/// oturum ister (`[A]`) ve esnafın ölçtüğü sayacı besler. Anonim kullanıcı
/// router'la giriş ekranına atılmaz — `ensureSignedIn` daveti gösterilir
/// (11.9'daki "İlan ver" kararının aynısı).
class CampaignDetailScreen extends ConsumerWidget {
  const CampaignDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(campaignDetailProvider(id));

    return AppScaffold(
      title: 'Kampanya',
      actions: [
        if (state case AsyncData(value: final campaign))
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                _shareText(campaign),
                subject: campaign.title,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
      ],
      onRefresh: () async => ref.invalidate(campaignDetailProvider(id)),
      body: switch (state) {
        AsyncData(value: final campaign) => _Content(campaign: campaign),
        AsyncError(:final error) => _DetailError(
          error: error,
          onRetry: () => ref.invalidate(campaignDetailProvider(id)),
        ),
        _ => const LoadingView(itemCount: 3),
      },
    );
  }

  /// Kod paylaşılan metne **konmaz**: kişiye özel açılıyor ve sayacı kişi
  /// bazında tutuluyor; kodu yayınlamak kampanyanın ölçümünü de bozar.
  static String _shareText(Campaign campaign) {
    final buffer = StringBuffer('🎟 ${campaign.title}');
    final business = (campaign.businessName ?? '').trim();
    if (business.isNotEmpty) buffer.write('\n🏪 $business');
    final discount = campaign.discountLabel;
    if (discount != null) buffer.write('\n💸 $discount indirim');
    buffer.write('\n🗓 ${campaign.validityLabel}');
    if (campaign.description.trim().isNotEmpty) {
      buffer.write('\n\n${campaign.description.trim()}');
    }
    buffer.write('\n\n— Kadirli uygulaması');
    return buffer.toString();
  }
}

class _DetailError extends StatelessWidget {
  const _DetailError({required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final api = error is ApiException ? error as ApiException : null;

    // ⚠️ Süresi dolan kampanya public uçtan **404** döner (uç `OnlyActive`):
    // "bulunamadı" burada çoğunlukla "kampanya bitti" demek.
    if (api != null && api.isNotFound) {
      return const EmptyView(
        icon: Icons.local_offer_outlined,
        title: 'Kampanya bulunamadı',
        message: 'Bu kampanya sona ermiş ya da yayından kaldırılmış olabilir.',
      );
    }

    return ErrorView(
      message: api?.message ?? 'Kampanya yüklenemedi.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends StatelessWidget {
  const _Content({required this.campaign});

  final Campaign campaign;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    final business = (campaign.businessName ?? '').trim();
    final discount = campaign.discountLabel;
    final urgency = campaign.urgencyLabel();
    final terms = (campaign.terms ?? '').trim();

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (campaign.coverImageUrl != null) ...[
          AppNetworkImage(
            url: campaign.coverImageUrl,
            height: 200,
            borderRadius: AppRadius.rMd,
            fallbackIcon: Icons.local_offer_outlined,
          ),
          AppSpacing.gapLg,
        ],

        if (business.isNotEmpty) ...[
          Row(
            children: [
              Icon(
                Icons.storefront_rounded,
                size: 16,
                color: theme.colorScheme.primary,
              ),
              AppSpacing.wGapSm,
              Expanded(
                child: Text(
                  business,
                  style: theme.textTheme.labelLarge?.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
              ),
            ],
          ),
          AppSpacing.gapSm,
        ],

        Text(campaign.title, style: theme.textTheme.headlineSmall),

        if (discount != null) ...[
          AppSpacing.gapMd,
          Text(
            '$discount indirim',
            style: theme.textTheme.headlineMedium?.copyWith(
              color: palette.success,
              fontWeight: FontWeight.w800,
            ),
          ),
        ],

        AppSpacing.gapLg,
        AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              InfoRow(
                icon: Icons.event_rounded,
                label: 'Geçerlilik',
                value: campaign.validityLabel,
              ),
              if (urgency != null) ...[
                AppSpacing.gapSm,
                InfoBanner(
                  tone: InfoBannerTone.warning,
                  icon: Icons.timer_outlined,
                  message: urgency,
                ),
              ],
            ],
          ),
        ),

        if (campaign.description.trim().isNotEmpty) ...[
          AppSpacing.gapXl,
          const SectionHeader(title: 'Kampanya detayı'),
          SelectableText(
            campaign.description.trim(),
            style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
          ),
        ],

        if (terms.isNotEmpty) ...[
          AppSpacing.gapXl,
          const SectionHeader(title: 'Koşullar'),
          Text(
            terms,
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
          ),
        ],

        AppSpacing.gapXl,
        if (campaign.hasCode)
          _ViewCodeButton(campaign: campaign)
        else
          // Kodsuz kampanyada uç 400 döndürüyor → buton hiç çizilmez
          // ("işlevsiz buton yok"), yerine ne yapılacağı yazılır.
          const InfoBanner(
            icon: Icons.storefront_rounded,
            message:
                'Bu kampanyada indirim kodu yok — işletmede kampanyadan '
                'söz etmeniz yeterli.',
          ),
      ],
    );
  }
}

/// "Kodu göster" — oturum kapısı + uç çağrısı + modal.
class _ViewCodeButton extends ConsumerStatefulWidget {
  const _ViewCodeButton({required this.campaign});

  final Campaign campaign;

  @override
  ConsumerState<_ViewCodeButton> createState() => _ViewCodeButtonState();
}

class _ViewCodeButtonState extends ConsumerState<_ViewCodeButton> {
  bool _busy = false;

  Future<void> _showCode() async {
    if (!await ensureSignedIn(
      context,
      ref,
      reason: 'İndirim kodunu görmek için giriş yapmanız gerekiyor.',
    )) {
      return;
    }
    if (!mounted) return;

    final messenger = ScaffoldMessenger.of(context);
    setState(() => _busy = true);
    try {
      final code = await ref
          .read(campaignsRepositoryProvider)
          .viewCode(widget.campaign.id);
      if (!mounted) return;
      // ⚠️ Yükleme göstergesi modal AÇILMADAN önce kapanmalı: `await` boyunca
      // buton sheet'in arkasında dönmeye devam ediyordu (testte "pumpAndSettle
      // timed out" olarak yakalandı; kullanıcı da sheet'i kapatınca bir an
      // dönen buton görürdü).
      setState(() => _busy = false);
      await showCampaignCodeSheet(
        context,
        campaign: widget.campaign,
        code: code,
      );
    } on ApiException catch (error) {
      messenger.showSnackBar(SnackBar(content: Text(error.message)));
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => AppButton(
    label: 'İndirim kodunu göster',
    icon: Icons.confirmation_number_rounded,
    expand: true,
    loading: _busy,
    onPressed: _showCode,
  );
}
