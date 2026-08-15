import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/legal_providers.dart';
import '../data/models/legal_document.dart';

/// `/yasal/:type` — **yürürlükteki** hukuki metnin tam hâli.
///
/// 🔑 Metin **her açılışta sunucudan** gelir; yerel bir kopyası saklanmaz.
/// Saklansaydı kullanıcı, yönetici yeni sürüm yayınladıktan sonra da eski
/// metni okur ve **ona** rıza verirdi — 12.16'nın "public uçlar
/// önbelleklenmiyor" kararının istemci tarafı (§7 madde 71).
class LegalDocumentScreen extends ConsumerWidget {
  const LegalDocumentScreen({super.key, required this.type});

  final String type;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final document = ref.watch(legalDocumentProvider(type));

    return AppScaffold(
      title: document.value?.title ?? 'Hukuki metin',
      onRefresh: () async => ref.invalidate(legalDocumentProvider(type)),
      actions: [
        if (document.value != null)
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => _share(context, document.value!),
            ),
          ),
      ],
      body: switch (document) {
        AsyncData(value: final value) => _Body(document: value),
        AsyncError(:final error) => ScrollableStateBody(
          child: ErrorView(
            title: 'Metin açılamadı',
            message: error is ApiException
                ? error.message
                : 'Hukuki metin yüklenemedi. Lütfen tekrar deneyin.',
            traceId: error is ApiException ? error.traceId : null,
            onRetry: () => ref.invalidate(legalDocumentProvider(type)),
          ),
        ),
        _ => const LoadingView(hasImage: false),
      },
    );
  }

  /// Metni paylaşma — kullanıcının onayladığı şeyin bir kopyasını kendinde
  /// tutabilmesi KVKK'nın ruhuna uygun ve maliyeti sıfır.
  ///
  /// ⚠️ Gövde HTML olduğu için **paylaşılan şey metin değil, adrestir**:
  /// ham HTML'i bir mesaja yapıştırmak okunamaz bir duvar üretirdi.
  void _share(BuildContext context, LegalDocument document) {
    AppShare.text(
      '${document.title} (v${document.versionNumber})\n'
      'Kadirli uygulaması — Ayarlar › Yasal metinler',
      origin: AppShare.originOf(context),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.document});

  final LegalDocument document;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final effectiveFrom = document.effectiveFrom;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        Text(document.title, style: theme.textTheme.headlineSmall),
        AppSpacing.gapXs,
        // 🔑 Sürüm ve yürürlük tarihi **metnin bir parçası**: kullanıcının
        // hangi hâle rıza verdiğini bilmesi bu bloğun tamamının sebebi.
        Text(
          effectiveFrom == null
              ? 'Sürüm ${document.versionNumber}'
              : 'Sürüm ${document.versionNumber} · '
                    '${AppDate.date(effectiveFrom)} tarihinden itibaren geçerli',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapLg,
        if (document.body.trim().isEmpty)
          // Boş gövde sunucuda mümkün değil (komut boş metni reddediyor) ama
          // istemci "hiçbir şey çizmemek" ile "metin yok" arasındaki farkı
          // **söylemek** zorunda: bomboş bir ekran, yüklenmemiş bir ekrandan
          // ayırt edilemez.
          const InfoBanner(
            tone: InfoBannerTone.warning,
            message: 'Bu belgenin metni henüz yayınlanmamış.',
          )
        else
          RichHtmlBody(html: document.body),
      ],
    );
  }
}
