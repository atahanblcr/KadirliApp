import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../widgets/app_button.dart';
import '../widgets/app_card.dart';
import '../widgets/state_views.dart';
import 'paged_feed.dart';

/// Sonsuz kaydırmalı listelerin **son satırı** — üç hâli tek yerde:
///
/// 1. sonraki sayfa yükleniyor → küçük gösterge,
/// 2. sonraki sayfa patladı → **sebebi yazan** kart + "Devamını yükle",
/// 3. liste bitti → "Toplam N ilan" (ya da kayıt yoksa "Hepsi bu kadar").
///
/// **Neden ortak bileşen (11.15):** bu altbilgi 11 ekranda **birebir aynı**
/// kopyalanmıştı ve kopyalar ayrışmıştı — 10'u sayfa hatasının *sebebini*
/// göstermiyordu (kullanıcı boş bir "Devamını yükle" düğmesi görüyordu),
/// eczane ekranında ise altbilgi **hiç yoktu**: 2. sayfa patlarsa liste
/// sessizce eksik kalıyor, kullanıcı "hepsi bu kadarmış" sanıyordu.
///
/// Ortaklaştırma aynı zamanda "liste sonu" sözleşmesini test edilebilir
/// kılıyor: tek bir bileşenin davranışı doğrulanınca 14 liste kapsanır.
class PagedListFooter<T, F> extends StatelessWidget {
  const PagedListFooter({
    super.key,
    required this.state,
    required this.onLoadMore,
    required this.itemNoun,
  });

  final PagedFeedState<T, F> state;
  final VoidCallback onLoadMore;

  /// Bitiş satırındaki sayılabilir ad: "ilan", "duyuru", "eczane"…
  /// → "Toplam 12 **eczane**".
  final String itemNoun;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (state.isLoadingMore) {
      return const Padding(
        padding: EdgeInsets.only(top: AppSpacing.md),
        child: LoadingView.compact(),
      );
    }

    // Sonraki sayfa patladı: okunan kayıtlar ekranda kalır (tüm ekranı hataya
    // düşürmek kullanıcının okuduğunu silerdi), yalnız devamı tekrar denenir.
    if (state.loadMoreError != null) {
      return Padding(
        padding: const EdgeInsets.only(top: AppSpacing.lg),
        child: AppCard(
          child: Column(
            children: [
              Text(
                state.loadMoreError!.message,
                style: theme.textTheme.bodyMedium,
                textAlign: TextAlign.center,
              ),
              AppSpacing.gapMd,
              AppButton.ghost(
                label: 'Devamını yükle',
                icon: Icons.refresh_rounded,
                size: AppButtonSize.small,
                expand: true,
                onPressed: onLoadMore,
              ),
            ],
          ),
        ),
      );
    }

    // Daha fazlası var: kaydırma devam edecek, yalnız nefes payı bırak.
    if (state.hasMore) return const SizedBox(height: AppSpacing.lg);

    // Liste bitti — "daha var mı?" sorusunu kapatan sakin bir satır.
    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.lg),
      child: Text(
        state.totalCount > 0
            ? 'Toplam ${state.totalCount} $itemNoun'
            : 'Hepsi bu kadar',
        textAlign: TextAlign.center,
        style: theme.textTheme.labelSmall?.copyWith(color: theme.palette.muted),
      ),
    );
  }
}
