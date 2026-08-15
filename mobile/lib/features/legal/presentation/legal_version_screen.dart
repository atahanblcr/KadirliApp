import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/legal_providers.dart';
import '../data/models/legal_version.dart';

/// `/yasal-surum/:id` — **"ben neyi onaylamıştım?"** (12.17 plan dışı eki).
///
/// 🔑 **Neden ayrı bir ekran:** 12.16 rızayı sürüme bağladı ve
/// `GET /v1/users/me/consents` onaylanan sürümün kimliğini söylüyordu — ama o
/// kimlikten **metne** giden bir yol yoktu. Yani yönetici yeni sürüm
/// yayınladığı an vatandaş, kabul ettiği metni bir daha **hiç göremiyordu**:
/// kanıt bizde vardı, **sahibinde** yoktu. Bloğun açılış cümlesinin
/// (*"kayıt duruyor, metin ortada yok"*) vatandaş tarafındaki yüzü buydu.
///
/// 🔴 Ekran, metnin **yürürlükte olup olmadığını söylemek zorunda**: söylemezse
/// kullanıcı yürürlükten kalkmış bir metni güncel sanar — ve bu, bu bloğun
/// savaştığı hasarın tersten hâli olurdu.
class LegalVersionScreen extends ConsumerWidget {
  const LegalVersionScreen({super.key, required this.versionId});

  final String versionId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final version = ref.watch(legalVersionProvider(versionId));

    return AppScaffold(
      title: version.value?.documentTitle ?? 'Onayladığınız metin',
      onRefresh: () async => ref.invalidate(legalVersionProvider(versionId)),
      body: switch (version) {
        AsyncData(value: final value) => _Body(version: value),
        AsyncError(:final error) => ScrollableStateBody(
          child: ErrorView(
            title: 'Metin açılamadı',
            message: error is ApiException && error.code == ApiErrorCodes.notFound
                ? 'Bu metin artık bulunamıyor.'
                : error is ApiException
                ? error.message
                : 'Metin yüklenemedi. Lütfen tekrar deneyin.',
            traceId: error is ApiException ? error.traceId : null,
            onRetry: () => ref.invalidate(legalVersionProvider(versionId)),
          ),
        ),
        _ => const LoadingView(hasImage: false),
      },
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({required this.version});

  final LegalVersion version;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final publishedAt = version.publishedAt;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        // 🔴 Yürürlük durumu **en üstte**: metni okumaya başlamadan önce
        // hangi hâle baktığını bilmeli.
        if (!version.isLive)
          Padding(
            padding: const EdgeInsets.only(bottom: AppSpacing.lg),
            child: InfoBanner(
              tone: InfoBannerTone.warning,
              icon: Icons.history_rounded,
              title: 'Bu metin artık yürürlükte değil',
              message: version.supersededAt == null
                  ? 'Onayladığınız sürüm bu. Güncel metin için "Yasal metinler" '
                        'ekranına dönebilirsiniz.'
                  : '${AppDate.date(version.supersededAt!)} tarihinde yerini yeni '
                        'bir sürüme bıraktı. Onayladığınız metin bu.',
            ),
          ),
        Text(version.documentTitle, style: theme.textTheme.headlineSmall),
        AppSpacing.gapXs,
        Text(
          publishedAt == null
              ? 'Sürüm ${version.versionNumber}'
              : 'Sürüm ${version.versionNumber} · '
                    '${AppDate.date(publishedAt)} tarihinde yayınlandı',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapLg,
        RichHtmlBody(html: version.body),
      ],
    );
  }
}
