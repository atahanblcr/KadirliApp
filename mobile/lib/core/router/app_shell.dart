import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/legal/presentation/widgets/reconsent_gate.dart';
import '../../features/notifications/application/unread_count_provider.dart';
import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

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
      // 12.17: yeniden onay kapısı. Kabuğu sarıyor çünkü `GoRouter.redirect`
      // eşzamanlı çalışıyor, rıza durumu ise bir ağ isteğinin sonucu —
      // redirect'e taşımak ya açılışı kilitler ya da ikinci bir sahip üretir.
      body: ReconsentGate(child: navigationShell),
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
    return _PopOnIncrease(
      value: unread,
      child: Badge(
        label: Text(unread > 99 ? '99+' : '$unread'),
        // Okunmamış sayısı dikkat çekmeli ama alarm değil → accent (turuncu).
        backgroundColor: badgeColor,
        textColor: Colors.white,
        child: Semantics(label: '$unread okunmamış bildirim', child: icon),
      ),
    );
  }
}

/// Sayı **arttığında** kısa bir "pop" ölçeği (MOBILE_UX_PLAN §4 "sayaç pop").
///
/// Push ile gelen yeni bildirim uygulama açıkken rozeti sessizce 1 artırıyordu;
/// göz alt çubuğa bakmıyorsa değişimi hiç fark etmiyor. Tek seferlik, 150 ms'lik
/// bir büyüme "yeni bir şey oldu" der ve biter.
///
/// ⚠️ Yalnız **artışta** oynar: okundu işaretleyip sayı düşerken zıplamak
/// kullanıcıyı yanlış yöne bakmaya iter.
/// ♿ "Hareketi azalt" açıkken hiç oynamaz.
class _PopOnIncrease extends StatefulWidget {
  const _PopOnIncrease({required this.value, required this.child});

  final int value;
  final Widget child;

  @override
  State<_PopOnIncrease> createState() => _PopOnIncreaseState();
}

class _PopOnIncreaseState extends State<_PopOnIncrease>
    with SingleTickerProviderStateMixin {
  // Büyü–küçül tek geçişte: `forward().then(reverse())` zincirlemek yerine
  // `TweenSequence` kullanmak animasyonu **atomik** yapıyor — yarıda kesilirse
  // rozet 1.25'te takılı kalmıyor.
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: AppDurations.pop * 2,
  );

  late final Animation<double> _scale = TweenSequence<double>([
    TweenSequenceItem(
      tween: Tween<double>(begin: 1, end: 1.25).chain(CurveTween(curve: Curves.easeOut)),
      weight: 1,
    ),
    TweenSequenceItem(
      tween: Tween<double>(begin: 1.25, end: 1).chain(CurveTween(curve: Curves.easeIn)),
      weight: 1,
    ),
  ]).animate(_controller);

  @override
  void didUpdateWidget(_PopOnIncrease oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.value > oldWidget.value && !MediaQuery.disableAnimationsOf(context)) {
      _controller.forward(from: 0);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => ScaleTransition(
    // Testin doğru `ScaleTransition`ı bulabilmesi için (ağaçta Material'ın
    // kendi geçiş animasyonları da var).
    key: const ValueKey('unreadBadgePop'),
    scale: _scale,
    child: widget.child,
  );
}

@immutable
class _TabSpec {
  const _TabSpec(this.label, this.icon, this.selectedIcon);

  final String label;
  final IconData icon;
  final IconData selectedIcon;
}
