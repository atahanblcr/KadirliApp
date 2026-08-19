import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:shared_preferences_platform_interface/shared_preferences_platform_interface.dart';

/// Tercih deposunun (`shared_preferences`) **tek sahibi** — Faz 12.23.
///
/// Depoda bugün yedi anahtar var ve hepsi *küçük yerel durum*: tema
/// (`settings.themeMode`), okuma boyutu (`news.textScale`), misafir tercihi
/// (`auth.guestChoice`), profil önbelleği (`auth.cachedUser`), ilan taslağı
/// (`ads.draft`), son aranan taksiciler (`taxis.recentCalls`), kaydedilen
/// haberler (`news.saved`). **Oturum jetonları burada DEĞİL** —
/// `flutter_secure_storage` (bkz. `core/network/token_store.dart`).
///
/// 🔴 **Depo açılamazsa uygulama AÇILMAYA DEVAM EDER.** 12.23 öncesinde
/// `main()` doğrudan `SharedPreferences.getInstance()` bekliyordu ve o çağrı
/// hata verirse uygulama **siyah ekranda** kalıyordu; üstelik hata
/// yakalayıcıları 18 satır *sonra* kurulduğu için **tek bir rapor bile
/// düşmüyordu** — belirtisi sessiz bir açılmama, sebebi hiçbir yerde. Aynı
/// karar push tarafında zaten verilmişti (`FirebasePushMessaging.tryInitialize`
/// → `NoopPushMessaging`: *"uygulama hiçbir durumda push yüzünden açılamaz hâle
/// gelmez"*); burası onun tercih deposundaki aynası.
///
/// 🔑 **Bellek içine düşmek neden GÜVENLİ — yedi anahtarın yokluğu tek tek
/// sayıldı:** tema ve okuma boyutu varsayılana döner, `auth.guestChoice`
/// okunamayınca kullanıcı Giriş ekranını görür (misafire *sessizce* düşmez),
/// `auth.cachedUser` boş kalır ama **oturum düşmez** — jetonlar ayrı depoda ve
/// `AuthController.bootstrap` yine `hasSession()`'a bakar; taslak, son
/// taksiciler ve kaydedilenler boş görünür. Hiçbiri bir şeyi *yanlış* yapmaz,
/// yalnız *unutur*. Buna karşılık açılmayan bir uygulamanın kullanıcıya
/// söyleyeceği hiçbir şey yoktur.
///
/// 🔴 **Ama sessizce düşmez.** Bellek içi depoda yazma **başarılı görünür ve
/// uygulama kapanınca kaybolur**: kullanıcı bir haberi kaydeder, yer imi dolar,
/// ertesi gün liste boştur. Bu yüzden durum [preferencesDegradedProvider] ile
/// taşınır ve Ayarlar ekranı bunu **yazar** (12.21b'nin dersi: blokaj doğruydu,
/// eksik olan dürüstlüktü).
final sharedPreferencesProvider = Provider<SharedPreferences>(
  (ref) => throw UnimplementedError('sharedPreferencesProvider override edilmeli'),
);

/// Tercih deposu açılamadı mı (yani tercihler **bu oturumda kalıcı değil**)?
///
/// `main()` içinde gerçek değerle override edilir. Varsayılanı `false`:
/// override etmeyi unutan bir test/ortam "bozuk" damgası yemez — o damga
/// kullanıcıya gösterildiği için yanlış pozitifi yanlış negatiften kötüdür.
final preferencesDegradedProvider = Provider<bool>((ref) => false);

/// [PreferencesBootstrap.open] sonucu: depo + bozulma durumu + sebebi.
@immutable
class PreferencesBootstrap {
  const PreferencesBootstrap._({
    required this.preferences,
    required this.isDegraded,
    this.error,
    this.stackTrace,
  });

  /// Her koşulda **kullanılabilir** bir depo (gerçek ya da bellek içi).
  final SharedPreferences preferences;

  /// `true` ise yazılanlar uygulama kapanınca kaybolur.
  final bool isDegraded;

  /// Bozulmanın sebebi — `main()` bunu **yakalayıcılar kurulduktan sonra**
  /// raporlar. Açılıştan önce raporlanamaz: raporlayıcının kendisi henüz yok.
  final Object? error;
  final StackTrace? stackTrace;

  /// Depoyu açar; açılamazsa **bellek içi** bir depoya düşer ve sebebi taşır.
  ///
  /// ⚠️ `SharedPreferences.setMockInitialValues` bilinçli olarak
  /// **kullanılmıyor**: paket onu `@visibleForTesting` işaretlemiş
  /// (`shared_preferences_legacy.dart`), yani üretim kodunda çağrılması
  /// `invalid_use_of_visible_for_testing_member` üretir ve `flutter analyze`
  /// kırmızıya döner. Platform arayüzünün `InMemorySharedPreferencesStore`'u
  /// ise **public** ve `PlatformInterface` belirtecini taşıyor (yani
  /// `instance` setter'ının doğrulamasından geçer).
  ///
  /// ⚠️ İkinci `getInstance()` çağrısı **çalışır**: paket ilk hatada dahilî
  /// `_completer`'ı `null`'a çeker — yeniden deneme paketin *tasarımıdır*,
  /// bizim şansımız değil.
  static Future<PreferencesBootstrap> open() async {
    try {
      return PreferencesBootstrap._(
        preferences: await SharedPreferences.getInstance(),
        isDegraded: false,
      );
    } catch (error, stackTrace) {
      SharedPreferencesStorePlatform.instance =
          InMemorySharedPreferencesStore.empty();
      return PreferencesBootstrap._(
        preferences: await SharedPreferences.getInstance(),
        isDegraded: true,
        error: error,
        stackTrace: stackTrace,
      );
    }
  }
}
