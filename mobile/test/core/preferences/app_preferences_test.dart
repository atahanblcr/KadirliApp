import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/preferences/app_preferences.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:shared_preferences_platform_interface/shared_preferences_platform_interface.dart';
import 'package:shared_preferences_platform_interface/types.dart';

/// Faz 12.23 — tercih deposu **açılışı öldüremez** (§7 madde 85).
///
/// Bu dosyanın kilitlediği şey bir davranış değil bir **yokluk**: depo hata
/// verdiğinde uygulamanın *açılmaya devam etmesi*. 12.23 öncesi `main()`
/// çıplak bir `SharedPreferences.getInstance()` bekliyordu; o çağrı atarsa
/// belirti **siyah ekran**, sebep ise **hiçbir yerde** oluyordu (hata
/// yakalayıcıları 20 satır sonra bağlanıyor).
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  tearDown(() {
    // Platform deposu süreç genelinde statik — bir sonraki teste sızmasın.
    SharedPreferencesStorePlatform.instance =
        InMemorySharedPreferencesStore.empty();
  });

  group('PreferencesBootstrap.open', () {
    test('depo açılıyorsa gerçek değerleri okur ve BOZUK DEĞİLDİR', () async {
      SharedPreferences.setMockInitialValues({'settings.themeMode': 'dark'});

      final result = await PreferencesBootstrap.open();

      expect(result.isDegraded, isFalse);
      expect(result.error, isNull);
      expect(result.preferences.getString('settings.themeMode'), 'dark');
    });

    test('depo AÇILAMAZSA uygulama yine de bir depo alır (siyah ekran yok)', () async {
      SharedPreferences.setMockInitialValues({});
      SharedPreferencesStorePlatform.instance = _ThrowingStore();

      final result = await PreferencesBootstrap.open();

      // Asıl iddia: `open()` ATMADI ve kullanılabilir bir depo döndü.
      expect(result.isDegraded, isTrue);
      expect(result.preferences.getString('settings.themeMode'), isNull);
      await result.preferences.setString('settings.themeMode', 'dark');
      expect(
        result.preferences.getString('settings.themeMode'),
        'dark',
        reason: 'Bellek içi depo yazılabilir olmalı — yoksa uygulama açılır '
            'ama her ayar dokunuşu sessizce hata verir.',
      );
    });

    test('bozulmanın SEBEBİ taşınır (main() onu raporlayabilsin diye)', () async {
      SharedPreferences.setMockInitialValues({});
      SharedPreferencesStorePlatform.instance = _ThrowingStore();

      final result = await PreferencesBootstrap.open();

      // Sebep yutulsaydı hasar 12.23 öncesiyle aynı sınıfa düşerdi: uygulama
      // açılır, tercihler sessizce kaybolur, hiçbir rapor düşmez.
      expect(result.error, isA<_StoreFailure>());
      expect(result.stackTrace, isNotNull);
    });

    test('sağlıklı açılışta hata alanları BOŞ (yanlış pozitif uyarı yok)', () async {
      SharedPreferences.setMockInitialValues({});

      final result = await PreferencesBootstrap.open();

      // İki yönlü kilit: yalnız "bozukta bozuk der" yazılsaydı, HER açılışta
      // bozuk diyen bir gerçekleme de yeşil kalırdı — ve kullanıcı her açılışta
      // "tercihleriniz kaydedilemiyor" uyarısı görürdü.
      expect(result.isDegraded, isFalse);
      expect(result.error, isNull);
      expect(result.stackTrace, isNull);
    });
  });

  group('preferencesDegradedProvider', () {
    test('varsayılanı FALSE — override edilmeyen ortam "bozuk" damgası yemez', () {
      // Damga kullanıcıya gösteriliyor: yanlış pozitif, yanlış negatiften kötü.
      final container = ProviderContainer();
      addTearDown(container.dispose);
      expect(container.read(preferencesDegradedProvider), isFalse);
    });
  });
}

/// Her okumada patlayan depo — gerçek dünyada bozuk XML / dolu disk / kayıp
/// platform kanalı. Hangi sebep olduğu önemli değil, `open()` için hepsi aynı.
class _ThrowingStore extends SharedPreferencesStorePlatform {
  @override
  Future<bool> clear() async => throw const _StoreFailure();

  @override
  Future<bool> clearWithParameters(ClearParameters parameters) async =>
      throw const _StoreFailure();

  @override
  Future<Map<String, Object>> getAll() async => throw const _StoreFailure();

  @override
  Future<Map<String, Object>> getAllWithParameters(
    GetAllParameters parameters,
  ) async => throw const _StoreFailure();

  @override
  Future<bool> remove(String key) async => throw const _StoreFailure();

  @override
  Future<bool> setValue(String valueType, String key, Object value) async =>
      throw const _StoreFailure();
}

class _StoreFailure implements Exception {
  const _StoreFailure();
  @override
  String toString() => 'Tercih deposu okunamadı (test)';
}
