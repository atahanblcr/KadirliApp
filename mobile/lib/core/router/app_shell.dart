import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/notifications/application/unread_count_provider.dart';
import '../theme/app_colors.dart';

/// Alt sekme kabuğu (11.4).
///
/// `StatefulShellRoute.indexedStack` her sekmenin kendi `Navigator`'ını ve
/// kaydırma konumunu korur — İlanlar'da 40 kart aşağıdayken Bildirimler'e
/// bakıp dönünce aynı yerde kalınır.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _destinations = <_TabSpec>[
    _TabSpec('Ana Sayfa', Icons.home_outlined, Icons.home_rounded),
    _TabSpec('İlanlar', Icons.sell_outlined, Icons.sell_rounded),
    _TabSpec('Bildirim', Icons.notifications_outlined, Icons.notifications_rounded),
    _TabSpec('Profil', Icons.person_outline_rounded, Icons.person_rounded),
  ];

  /// Rozetli sekmenin indeksi (bildirimler).
  static const _badgeIndex = 2;

  void _onTap(int index) {
    // Aynı sekmeye tekrar dokunmak o sekmenin köküne döndürür (Material deseni).
    navigationShell.goBranch(index, initialLocation: index == navigationShell.currentIndex);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final unread = ref.watch(unreadNotificationCountProvider).value ?? 0;

    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: DecoratedBox(
        decoration: BoxDecoration(
          border: Border(top: BorderSide(color: theme.palette.border)),
        ),
        child: NavigationBar(
          selectedIndex: navigationShell.currentIndex,
          onDestinationSelected: _onTap,
          height: 68,
          labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
          destinations: [
            for (var i = 0; i < _destinations.length; i++)
              NavigationDestination(
                icon: _maybeBadge(i, Icon(_destinations[i].icon), unread, palette.accent),
                selectedIcon: _maybeBadge(
                  i,
                  Icon(_destinations[i].selectedIcon, color: theme.colorScheme.primary),
                  unread,
                  palette.accent,
                ),
                label: _destinations[i].label,
                tooltip: _destinations[i].label,
              ),
          ],
        ),
      ),
    );
  }

  Widget _maybeBadge(int index, Widget icon, int unread, Color badgeColor) {
    if (index != _badgeIndex || unread <= 0) return icon;
    return Badge(
      label: Text(unread > 99 ? '99+' : '$unread'),
      // Okunmamış sayısı dikkat çekmeli ama alarm değil → accent (turuncu).
      backgroundColor: badgeColor,
      textColor: Colors.white,
      child: Semantics(label: '$unread okunmamış bildirim', child: icon),
    );
  }
}

@immutable
class _TabSpec {
  const _TabSpec(this.label, this.icon, this.selectedIcon);

  final String label;
  final IconData icon;
  final IconData selectedIcon;
}
