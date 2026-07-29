import 'package:flutter/material.dart';

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
    this.offline = false,
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

  final bool offline;

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
      content = RefreshIndicator(
        onRefresh: onRefresh!,
        color: Theme.of(context).colorScheme.primary,
        child: content,
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
            OfflineBanner(visible: offline),
            Expanded(child: content),
          ],
        ),
      ),
      bottomNavigationBar: bottomNavigationBar,
      floatingActionButton: floatingActionButton,
    );
  }
}
