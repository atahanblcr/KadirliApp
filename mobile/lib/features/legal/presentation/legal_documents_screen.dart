import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../application/consent_controller.dart';
import '../application/legal_providers.dart';
import '../data/models/legal_document.dart';
import '../data/models/my_consent.dart';

/// `/yasal` — **Ayarlar › Yasal metinler.**
///
/// Üç işi birden yapar ve üçü de plandaki bitti-kriterinde:
/// 1. Yayında olan metinleri okuma,
/// 2. *"onayladığınız sürüm: v3, 12.08.2026"*,
/// 3. **isteğe bağlı rızayı geri alma** (zorunlu olanınki hesap silmedir).
///
/// ⚠️ Misafir kullanıcı da bu ekranı açabilir — metinler anonim okunur; yalnız
/// rıza satırları görünmez. Ekranı oturuma kapatmak, mağazanın "gizlilik
/// politikasına uygulama içinden erişilebilmeli" şartını kırardı.
class LegalDocumentsScreen extends ConsumerWidget {
  const LegalDocumentsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final documents = ref.watch(legalDocumentsProvider);
    final consents = ref.watch(myConsentsProvider);
    final writeState = ref.watch(consentControllerProvider);
    final signedIn = ref.watch(currentUserProvider) != null;

    return AppScaffold(
      title: 'Yasal metinler',
      onRefresh: () async {
        ref.invalidate(legalDocumentsProvider);
        ref.invalidate(myConsentsProvider);
      },
      body: switch (documents) {
        AsyncData(value: final items) when items.isEmpty => ScrollableStateBody(
          child: EmptyView(
            icon: Icons.gavel_rounded,
            title: 'Yayında metin yok',
            // 🔑 Boş liste **sebebini söyler**: "henüz yayınlanmadı" ile
            // "yüklenemedi" aynı ekran değildir.
            message:
                'Uygulamanın hukuki metinleri henüz yayınlanmadı. '
                'Gizlilik politikasını web sitemizden okuyabilirsiniz.',
            actionLabel: 'Web sitesinde aç',
            onAction: () => AppLinks.web(Env.privacyPolicyUrl),
          ),
        ),
        AsyncData(value: final items) => ListView(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.lg,
            AppSpacing.lg,
            AppSpacing.xxl,
          ),
          children: [
            if (writeState.error != null) ...[
              InfoBanner(
                tone: InfoBannerTone.danger,
                message: writeState.error!,
                onClose: () =>
                    ref.read(consentControllerProvider.notifier).clearError(),
              ),
              AppSpacing.gapLg,
            ],
            if (!signedIn) ...[
              const InfoBanner(
                message:
                    'Onay durumunuzu görmek ve izinlerinizi yönetmek için '
                    'giriş yapın.',
              ),
              AppSpacing.gapLg,
            ],
            for (final document in items) ...[
              _DocumentCard(
                document: document,
                consent: _consentFor(consents.value, document.type),
                showConsent: signedIn,
                pending: writeState.isPending(document.type),
              ),
              AppSpacing.gapMd,
            ],
          ],
        ),
        AsyncError(:final error) => ScrollableStateBody(
          child: ErrorView(
            title: 'Metinler açılamadı',
            message: error is ApiException
                ? error.message
                : 'Hukuki metinler yüklenemedi.',
            traceId: error is ApiException ? error.traceId : null,
            onRetry: () => ref.invalidate(legalDocumentsProvider),
          ),
        ),
        _ => const LoadingView(hasImage: false),
      },
    );
  }

  static MyConsent? _consentFor(List<MyConsent>? consents, String type) {
    if (consents == null) return null;
    for (final consent in consents) {
      if (consent.type == type) return consent;
    }
    return null;
  }
}

class _DocumentCard extends ConsumerWidget {
  const _DocumentCard({
    required this.document,
    required this.consent,
    required this.showConsent,
    required this.pending,
  });

  final LegalDocument document;
  final MyConsent? consent;
  final bool showConsent;
  final bool pending;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(document.title, style: theme.textTheme.titleSmall),
              ),
              AppSpacing.wGapSm,
              if (document.isMandatory)
                Text(
                  'Zorunlu',
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: palette.muted,
                  ),
                ),
            ],
          ),
          if (document.summary != null && document.summary!.trim().isNotEmpty) ...[
            AppSpacing.gapXs,
            Text(
              document.summary!,
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            ),
          ],
          AppSpacing.gapSm,
          Text(
            'Yayındaki sürüm: v${document.versionNumber}',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
          if (showConsent) ...[
            AppSpacing.gapSm,
            _ConsentSummary(consent: consent),
          ],
          AppSpacing.gapLg,
          AppButton.ghost(
            label: 'Metni oku',
            icon: Icons.description_outlined,
            expand: true,
            onPressed: () =>
                context.push(AppRoutes.legalDocument(document.type)),
          ),
          // 🔑 "Onayladığınız metin" yalnız **eski bir sürümü** onayladıysa
          // ayrı bir buton: aynı metinse iki buton aynı yere giderdi.
          if (showConsent &&
              consent?.consentedVersionId != null &&
              consent!.consentedVersionId != document.versionId) ...[
            AppSpacing.gapSm,
            AppButton.ghost(
              label: 'Onayladığınız metni oku (v${consent!.consentedVersionNumber})',
              icon: Icons.history_rounded,
              expand: true,
              onPressed: () => context.push(
                AppRoutes.legalVersion(consent!.consentedVersionId!),
              ),
            ),
          ],
          // 🔴 Geri alma **yalnız isteğe bağlı ve onaylanmış** rızada çizilir.
          // Zorunlu rızada anahtarı kapalı çizmek işlevsiz buton olurdu; onun
          // karşılığı hesap silmedir ve aşağıda **söyleniyor**.
          if (showConsent && (consent?.canRevoke ?? false)) ...[
            AppSpacing.gapSm,
            AppButton.ghost(
              label: 'İzni geri al',
              icon: Icons.block_rounded,
              expand: true,
              loading: pending,
              onPressed: pending ? null : () => _revoke(context, ref),
            ),
          ],
          if (showConsent &&
              !(consent?.granted ?? false) &&
              !document.isMandatory) ...[
            AppSpacing.gapSm,
            AppButton.ghost(
              label: 'İzin ver',
              icon: Icons.check_rounded,
              expand: true,
              loading: pending,
              onPressed: pending
                  ? null
                  : () => ref
                        .read(consentControllerProvider.notifier)
                        .decide(
                          type: document.type,
                          versionId: document.versionId,
                          granted: true,
                        ),
            ),
          ],
          if (showConsent && document.isMandatory && (consent?.granted ?? false)) ...[
            AppSpacing.gapSm,
            Text(
              'Bu onay hizmetin verilebilmesi için zorunludur; geri almak '
              'istiyorsanız Ayarlar › Hesabı sil adımını kullanabilirsiniz.',
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _revoke(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('İzin geri alınsın mı?'),
        // ⚠️ Onay penceresi **neyi** geri aldığını yazar (11.15c kuralı).
        content: Text(
          '"${document.title}" için verdiğiniz izin geri alınacak. '
          'Dilediğiniz zaman yeniden verebilirsiniz.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Geri al'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    // Geri alma da **bir karardır** ve sürüme yazılır: kayıt silinmez,
    // `granted=false` satırı yazılır ("sormadık" ≠ "sorduk, hayır dedi").
    await ref
        .read(consentControllerProvider.notifier)
        .decide(
          type: document.type,
          versionId: consent?.consentedVersionId ?? document.versionId,
          granted: false,
        );
  }
}

/// "Onayınız: v3 · 12 Ağustos 2026" satırı — ya da hiç karar verilmediyse
/// **onu söyleyen** satır.
class _ConsentSummary extends StatelessWidget {
  const _ConsentSummary({required this.consent});

  final MyConsent? consent;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final value = consent;

    final (icon, color, text) = switch (value) {
      null => (Icons.hourglass_empty_rounded, palette.muted, 'Onay durumu yükleniyor…'),
      MyConsent(hasDecision: false) => (
        Icons.remove_circle_outline_rounded,
        palette.muted,
        'Henüz karar vermediniz.',
      ),
      MyConsent(granted: false) => (
        Icons.cancel_outlined,
        palette.danger,
        value.revokedAt != null
            ? 'Geri aldınız · ${AppDate.date(value.revokedAt!)}'
            : 'Onaylamadınız${value.decidedAt == null ? '' : ' · ${AppDate.date(value.decidedAt!)}'}',
      ),
      _ => (
        value.needsReconsent
            ? Icons.info_outline_rounded
            : Icons.check_circle_outline_rounded,
        value.needsReconsent ? palette.warning : palette.success,
        value.needsReconsent
            ? 'Onayınız: v${value.consentedVersionNumber} — metin güncellendi, '
                  'yeniden onayınız bekleniyor.'
            : 'Onayınız: v${value.consentedVersionNumber}'
                  '${value.decidedAt == null ? '' : ' · ${AppDate.date(value.decidedAt!)}'}',
      ),
    };

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 16, color: color),
        AppSpacing.wGapSm,
        // 🔑 `Expanded`: 1.4 yazı ölçeğinde "yeniden onayınız bekleniyor"
        // satırı 360dp'de taşardı.
        Expanded(
          child: Text(
            text,
            style: theme.textTheme.bodySmall?.copyWith(color: color),
          ),
        ),
      ],
    );
  }
}
