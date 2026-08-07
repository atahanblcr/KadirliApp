import 'package:flutter/widgets.dart';
import 'package:go_router/go_router.dart';

/// Kabuk (alt sekme) rotalarına **güvenli** gezinme.
///
/// ## Neden var — 12.2'den devralınan çökmenin kanıtlanmış kök nedeni (12.3'te bulundu)
///
/// `go_router` her sayfaya bir anahtar verir ve iki anahtar türü **farklı** davranır:
///
/// - `context.push` ile açılan sayfa → `ImperativeRouteMatch`, anahtarı **rastgele**
///   (32 karakter). Aynı rotayı iki kez `push` etmek bu yüzden çakışma üretmez.
/// - Kabuk (`StatefulShellRoute`) sayfası → `ShellRouteMatch`, anahtarı
///   **`route.hashCode`** — yani **deterministik ve tek**.
///
/// `RouteMatchList._createNewMatchUntilIncompatible`, `push` edilen bir kabuk rotasını
/// yığındaki kabukla **yalnız kabuk en üstteyse** birleştirir. Araya kabuk dışı bir
/// sayfa girmişse birleştirme yapılmaz ve listeye **aynı anahtarla ikinci bir
/// `ShellRouteMatch`** eklenir:
///
/// ```text
/// [ShellRouteMatch=114994750, ImperativeRouteMatch=…, ShellRouteMatch=114994750]
///                 ▲                                              ▲  aynı anahtar
/// → Navigator._debugCheckDuplicatedPageKeys  ('!keyReservation.contains(key)')
/// ```
///
/// 🔑 **Gerçek hayattaki tetikleyici 12.3 ile daha da yakınlaştı:** kesinti bildirimi
/// artık **kendiliğinden** gidiyor. Kullanıcı bir modül ekranındayken (kabuk dışı) gelen
/// bir push'a dokunduğunda `PushCoordinator` hedefe `push` ediyor — hedef `/ilanlar/:id`
/// gibi bir **kabuk** rotasıysa uygulama o anda çöküyordu.
///
/// ## Kural
///
/// **Kabuk rotası, kabuk en üstte değilken `push` EDİLMEZ — `go` edilir.**
/// `go` yığını yeniden kurduğu için mükerrer anahtar doğmaz; kullanıcı da zaten
/// sekmeye "geçmiş" olur (bir sekmenin üstüne başka bir sekme yığmak anlamlı değil).
///
/// ⚠️ Karar **elde tutulan bir rota listesinden değil, router'ın kendisinden** okunuyor:
/// hangi rotanın kabukta olduğu `RouteConfiguration`'a sorulur. Elle liste tutulsaydı
/// yeni bir sekme eklendiğinde liste çürür ve çökme sessizce geri gelirdi.
abstract final class AppNav {
  /// Hedefe gider: kabuk rotasıysa ve kabuk en üstte değilse `go`, aksi hâlde `push`.
  static void push(GoRouter router, String location) {
    if (_wouldDuplicateShellPage(router, location)) {
      router.go(location);
      return;
    }
    router.push(location);
  }

  /// Ekranlardan kullanım: `AppNav.of(context, AppRoutes.adDetail(id))`.
  static void of(BuildContext context, String location) =>
      push(GoRouter.of(context), location);

  /// Hedef bir kabuk sayfası mı **ve** yığının tepesi kabuk değil mi.
  static bool _wouldDuplicateShellPage(GoRouter router, String location) {
    if (!_targetLivesInShell(router, location)) return false;

    final current = router.routerDelegate.currentConfiguration.matches;
    // Tepe kabuksa go_router birleştirir (güvenli yol) — dokunma.
    return current.isEmpty || current.last is! ShellRouteMatch;
  }

  static bool _targetLivesInShell(GoRouter router, String location) {
    try {
      final match = router.configuration.findMatch(Uri.parse(location));
      return match.matches.isNotEmpty && match.matches.last is ShellRouteMatch;
    } on Object {
      // Çözülemeyen rota burada patlamamalı: gezinme kararı bir yan konudur,
      // hatayı asıl `push`/`go` çağrısı üretsin.
      return false;
    }
  }
}
