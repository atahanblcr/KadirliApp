import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/consent_controller.dart';
import '../application/consent_selection.dart';
import '../application/legal_providers.dart';
import '../data/models/legal_document.dart';
import 'widgets/consent_check_tile.dart';

/// `/yasal-onay` — **yeniden onay** akışı (12.17).
///
/// Yönetici *esaslı* bir değişiklik yayınladığında (`requiresReconsent`) açılan
/// tek seferlik ekran. Ölçüt sunucuda türetiliyor (`needsReconsent`); istemci
/// onu **yeniden hesaplamaz**, yalnız gösterir.
///
/// 🔴 **Zorunlu belge varsa ekran kapatılamaz** — ama **kapana da dönmez**:
/// çıkış ve hesap silme yolları burada duruyor. Kapatılamayan ve çıkışı olmayan
/// bir ekran, kullanıcıyı hesabından kilitler; 12.7'nin *"son sosyal bağlantı da
/// çözülebilmeli"* kararının aynı gerekçesi.
///
/// 🔴 Kutular **ön işaretsiz** başlar (`ConsentSelection.initial`): kullanıcının
/// eski sürüme verdiği onayı yeni sürüme taşımak, ekranın var olma sebebini
/// ortadan kaldırırdı.
class ReconsentScreen extends ConsumerStatefulWidget {
  const ReconsentScreen({super.key});

  @override
  ConsumerState<ReconsentScreen> createState() => _ReconsentScreenState();
}

class _ReconsentScreenState extends ConsumerState<ReconsentScreen> {
  ConsentSelection? _selection;
  bool _submitting = false;

  @override
  Widget build(BuildContext context) {
    final documents = ref.watch(legalDocumentsProvider);
    final pending = ref.watch(pendingReconsentsProvider);
    final writeState = ref.watch(consentControllerProvider);

    return switch (documents) {
      AsyncData(value: final items) => _build(
        context,
        _documentsNeedingReconsent(items, pending.map((c) => c.type).toSet()),
        writeState.error,
      ),
      AsyncError(:final error) => AppScaffold(
        title: 'Güncellenen metinler',
        body: ScrollableStateBody(
          child: ErrorView(
            message: error is ApiException
                ? error.message
                : 'Metinler yüklenemedi.',
            onRetry: () => ref.invalidate(legalDocumentsProvider),
          ),
        ),
      ),
      _ => const AppScaffold(
        title: 'Güncellenen metinler',
        body: LoadingView(hasImage: false),
      ),
    };
  }

  /// Yeniden onay bekleyen belgeler — **sunucunun listesiyle** kesişim.
  ///
  /// ⚠️ Kesişim şart: `legalDocumentsProvider` yayında olan **her** belgeyi
  /// döndürüyor; süzmeseydik kullanıcı zaten güncel onayladığı metinleri de
  /// yeniden onaylamak zorunda kalırdı.
  static List<LegalDocument> _documentsNeedingReconsent(
    List<LegalDocument> documents,
    Set<String> types,
  ) => documents.where((d) => types.contains(d.type)).toList(growable: false);

  Widget _build(
    BuildContext context,
    List<LegalDocument> documents,
    String? error,
  ) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    // Liste boşaldıysa (kullanıcı onayladı ya da başka cihazdan hallettiyse)
    // ekranı **kendiliğinden** kapatıyoruz: "onayla" dedikten sonra hâlâ açık
    // duran bir ekran, işlemin başarısız olduğu izlenimi verir.
    // ⚠️ `addPostFrameCallback` içinde (checklist §5): build sırasında
    // gezinmek router redirect'inin üstünde asılı bir ekran bırakır.
    if (documents.isEmpty) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && context.canPop()) context.pop();
      });
      return const AppScaffold(
        title: 'Güncellenen metinler',
        body: LoadingView(hasImage: false),
      );
    }

    final selection = _selection ??= ConsentSelection.initial(documents);
    final hasMandatory = documents.any((d) => d.isMandatory);

    return PopScope(
      // 🔴 Zorunlu belge varsa geri tuşu ekranı kapatmaz.
      canPop: !hasMandatory,
      child: AppScaffold(
        title: 'Güncellenen metinler',
        showBackButton: !hasMandatory,
        body: ListView(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.lg,
            AppSpacing.lg,
            AppSpacing.lg,
            AppSpacing.xxl,
          ),
          children: [
            InfoBanner(
              icon: Icons.update_rounded,
              title: 'Metinlerimiz güncellendi',
              message: hasMandatory
                  ? 'Uygulamayı kullanmaya devam edebilmek için güncellenen '
                        'metni okuyup onaylamanız gerekiyor.'
                  : 'Güncellenen metni okuyup kararınızı yenileyebilirsiniz.',
            ),
            AppSpacing.gapLg,
            if (error != null) ...[
              InfoBanner(
                tone: InfoBannerTone.danger,
                message: error,
                onClose: () =>
                    ref.read(consentControllerProvider.notifier).clearError(),
              ),
              AppSpacing.gapLg,
            ],
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final document in documents)
                    ConsentCheckTile(
                      document: document,
                      granted: selection.isGranted(document),
                      enabled: !_submitting,
                      onChanged: (value) => setState(
                        () => _selection = selection.toggle(document, value),
                      ),
                      onRead: () =>
                          context.push(AppRoutes.legalDocument(document.type)),
                    ),
                ],
              ),
            ),
            AppSpacing.gapLg,
            // 🔑 Buton kapalıysa **sebebini söyler** (§7 madde 42).
            if (selection.blockingReason != null) ...[
              Text(
                selection.blockingReason!,
                style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                textAlign: TextAlign.center,
              ),
              AppSpacing.gapSm,
            ],
            AppButton(
              label: 'Onayla ve devam et',
              icon: Icons.check_rounded,
              expand: true,
              loading: _submitting,
              onPressed: selection.canSubmit && !_submitting ? _submit : null,
            ),
            AppSpacing.gapMd,
            if (!hasMandatory)
              Center(
                child: TextButton(
                  onPressed: _submitting ? null : () => context.pop(),
                  child: const Text('Şimdi değil'),
                ),
              )
            else
              // 🔴 Kapatılamayan ekranın **çıkışı olmak zorunda**: kabul
              // etmeyen kullanıcının yolu hesap silmedir (12.16'nın "zorunlu
              // rızanın geri alınması = hesap silme" kararı).
              Column(
                children: [
                  Text(
                    'Onaylamak istemiyorsanız hesabınızı silebilirsiniz.',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: palette.muted,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  AppSpacing.gapSm,
                  Center(
                    child: TextButton(
                      onPressed: _submitting
                          ? null
                          : () => context.push(AppRoutes.accountDelete),
                      child: const Text('Hesabı sil'),
                    ),
                  ),
                ],
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    final selection = _selection;
    if (selection == null || !selection.canSubmit) return;

    setState(() => _submitting = true);
    final ok = await ref
        .read(consentControllerProvider.notifier)
        .submitReconsent(selection.decisions);
    if (!mounted) return;
    setState(() => _submitting = false);

    // Başarıda ekran, `pendingReconsents` boşaldığı için yukarıdaki daldan
    // kapanır; hata mesajı zaten `consentControllerProvider`'da ve yukarıda
    // basılıyor — yani hiçbir yol **sessiz** değil.
    if (ok) ref.invalidate(myConsentsProvider);
  }
}
