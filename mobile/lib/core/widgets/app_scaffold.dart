import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../network/connectivity_status.dart';
import '../theme/app_spacing.dart';
import 'state_views.dart';

/// Tüm ekranların ortak kabuğu.
///
/// - Başlık + geri butonu + sağ üst aksiyonlar (ör. Ana Sayfa'daki ⚙️).
/// - Gövdenin üstünde offline şeridi yeri.
/// - [onRefresh] verilirse pull-to-refresh sarmalar.
class AppScaffold extends StatelessWidget {
  const AppScaffold({
    super.key,
    required this.body,
    this.title,
    this.titleWidget,
    this.actions,
    this.leading,
    this.showBackButton = true,
    this.bottomNavigationBar,
    this.floatingActionButton,
    this.onRefresh,
    this.offline,
    this.padded = false,
    this.backgroundColor,
  });

  final Widget body;
  final String? title;
  final Widget? titleWidget;
  final List<Widget>? actions;
  final Widget? leading;
  final bool showBackButton;
  final Widget? bottomNavigationBar;
  final Widget? floatingActionButton;

  /// Pull-to-refresh geri çağrısı (liste ekranlarında zorunlu sayılır).
  final Future<void> Function()? onRefresh;

  /// Offline şeridi. **Varsayılan `null` = otomatik** — `AppScaffold` sinyali
  /// [connectivityStatusProvider]'dan kendisi okur, hiçbir ekranın bunu
  /// bağlaması gerekmez.
  ///
  /// 📌 11.15'e kadar bu alan `false` sabitiydi ve **hiçbir ekran değer
  /// geçmiyordu** → `OfflineBanner` 11.1'de yazılmış ama uygulamada hiç
  /// görünmemişti (yalnız stil kılavuzunda). Açık bir değer vermek yalnız
  /// testler ve `/gelistirici/tasarim` için anlamlı.
  final bool? offline;

  /// Gövdeye standart ekran yatay boşluğu uygula.
  final bool padded;

  final Color? backgroundColor;

  @override
  Widget build(BuildContext context) {
    final hasAppBar = title != null || titleWidget != null || actions != null || leading != null;

    Widget content = padded
        ? Padding(padding: AppSpacing.screenPadding, child: body)
        : body;

    if (onRefresh != null) {
      // 🐛 11.12'de canlıda yakalandı: içerik ekrana **sığdığında** Android'in
      // varsayılan `ClampingScrollPhysics`i taşmaya izin vermiyor, dolayısıyla
      // aşağı çekme jesti `RefreshIndicator`a hiç ulaşmıyordu — kısa listeli
      // her ekranda pull-to-refresh sessizce ölüydü (yalnız uzun listelerde
      // çalışıyordu, o yüzden fark edilmemişti).
      //
      // Düzeltme burada, çünkü `onRefresh` de burada: `ScrollConfiguration`
      // altındaki **tüm** kaydırılabilirlere uygulanır ve kendi `physics`ini
      // açıkça veren ekranlar (varsa) etkilenmez.
      //
      // ⚠️ Platform fiziği **korunmalı**: `AlwaysScrollableScrollPhysics()`
      // tek başına verilirse Android'in `ClampingScrollPhysics`i tümden
      // düşüyor (fling simülasyonu ve sınır davranışı değişiyor, uzun
      // listelerde tek hamlelik sürükleme çalışmaz oluyor) → `applyTo` ile
      // mevcut fiziğin ÜSTÜNE ekleniyor.
      final scrollBehavior = ScrollConfiguration.of(context);
      content = RefreshIndicator(
        onRefresh: onRefresh!,
        color: Theme.of(context).colorScheme.primary,
        child: ScrollConfiguration(
          behavior: scrollBehavior.copyWith(
            physics: const AlwaysScrollableScrollPhysics().applyTo(
              scrollBehavior.getScrollPhysics(context),
            ),
          ),
          child: content,
        ),
      );
    }

    return Scaffold(
      backgroundColor: backgroundColor,
      appBar: hasAppBar
          ? AppBar(
              title: titleWidget ?? (title != null ? Text(title!) : null),
              actions: actions,
              leading: leading,
              automaticallyImplyLeading: showBackButton,
              titleSpacing: leading == null && !showBackButton ? AppSpacing.lg : null,
            )
          : null,
      body: SafeArea(
        top: !hasAppBar,
        bottom: false,
        child: Column(
          children: [
            if (offline case final bool value)
              OfflineBanner(visible: value)
            else
              const _ConnectivityBanner(),
            Expanded(child: content),
          ],
        ),
      ),
      bottomNavigationBar: bottomNavigationBar,
      floatingActionButton: floatingActionButton,
    );
  }
}

/// Ağ katmanının çevrimdışı sinyalini dinleyen şerit.
///
/// Ayrı bir widget olması bilinçli: yalnız bu küçük parça yeniden çizilir,
/// bağlantı durumu değişince ekranın tamamı yeniden kurulmaz.
class _ConnectivityBanner extends ConsumerWidget {
  const _ConnectivityBanner();

  @override
  Widget build(BuildContext context, WidgetRef ref) =>
      OfflineBanner(visible: ref.watch(connectivityStatusProvider));
}
