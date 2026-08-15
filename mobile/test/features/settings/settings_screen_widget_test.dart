import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/auth/data/models/notification_preferences.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/profile_fixtures.dart';
import '../../helpers/pump_app.dart';

/// Ayarlar/Kontrol ekranı: bölümlerin doğru kullanıcıya görünmesi ve
/// bildirim anahtarlarının uca yazması.
void main() {
  Future<FakeHttpAdapter> openSettings(
    WidgetTester tester, {
    bool signedIn = true,
    Map<String, dynamic>? profile,
  }) async {
    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/users/me': (_) async =>
          jsonResponse(successEnvelope(profile ?? profileBody())),
      '/v1/users/me/notifications': (_) async => jsonResponse(
        successEnvelope(const {
          'announcements': false,
          'deaths': true,
          'pharmacy': true,
          'events': true,
          'ads': false,
          'campaigns': false,
        }),
      ),
    });

    await pumpApp(
      tester,
      prefs: signedIn ? const {} : const {'auth.guestChoice': true},
      tokenStore: signedIn
          ? InMemoryTokenStore(accessToken: 'A', refreshToken: 'R')
          : InMemoryTokenStore(),
      adapter: adapter,
    );

    await tester.tap(find.byTooltip('Ayarlar'));
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('oturum açıkken hesap özeti + bütün bildirim anahtarları görünür', (
    tester,
  ) async {
    await openSettings(tester);

    expect(find.text('ahmetk'), findsOneWidget);
    expect(find.text('Profili düzenle'), findsOneWidget);

    for (final topic in NotificationTopic.values) {
      expect(find.text(topic.label), findsOneWidget, reason: topic.key);
    }
    // ⚠️ Sayı ELLE yazılmaz, listeden TÜRETİLİR: 12.15b'de yedinci anahtar (`news`)
    // eklendiğinde elle tutulan "6" bu testi ilgisiz bir sebeple kırdı. İddia "altı
    // anahtar var" değil, "listedeki HER anahtarın ekranda bir karşılığı var".
    expect(
      find.byType(SwitchListTile),
      findsNWidgets(NotificationTopic.values.length),
    );
  });

  testWidgets('anahtar kapatılınca yalnız o anahtar uca yazılır', (tester) async {
    final adapter = await openSettings(tester);

    await tester.tap(find.byType(SwitchListTile).first);
    await tester.pumpAndSettle();

    expect(adapter.lastOf('/v1/users/me/notifications')!.data, {
      'announcements': false,
    });
  });

  testWidgets('misafir bildirim bölümünü ve hesap işlemlerini görmez', (
    tester,
  ) async {
    await openSettings(tester, signedIn: false);

    expect(find.text('Misafir olarak geziyorsunuz'), findsOneWidget);
    expect(find.byType(SwitchListTile), findsNothing);
    expect(find.text('Hesabı sil'), findsNothing);
    expect(find.text('Çıkış yap'), findsNothing);
  });

  testWidgets('hesap işlemleri bölümünde çıkış ve hesap silme var', (tester) async {
    await openSettings(tester);

    await tester.scrollUntilVisible(find.text('Hesabı sil'), 200);
    await tester.pumpAndSettle();

    expect(find.text('Çıkış yap'), findsOneWidget);
    expect(find.text('Hesabı sil'), findsOneWidget);
    // Sürüm satırı her zaman var (test ortamında platform kanalı yok → "—").
    expect(find.textContaining('Sürüm'), findsOneWidget);
  });

  testWidgets('yasal metinler bağlantısı MİSAFİRE DE görünür', (tester) async {
    // Mağaza zorunluluğu (Faz 11.16): politikaya uygulamanın içinden
    // erişilebilmeli. ⚠️ Bilinçli olarak **misafir** oturumla test ediliyor:
    // bağlantı yanlışlıkla "Hesap işlemleri" gibi oturum gerektiren bir bloğa
    // konursa giriş yapmamış kullanıcı politikayı hiç göremez — ki mağazanın
    // istediği tam olarak o kullanıcının da görebilmesi.
    //
    // 🔑 12.17: hedef artık uygulama **içi** ekran (`/yasal`). Etiket
    // değiştiği için bu testin kırılması **doğruydu** — kilit bağlantının
    // varlığını değil, *misafirin ona ulaşabildiğini* tutuyor.
    await openSettings(tester, signedIn: false);

    await tester.scrollUntilVisible(find.text('Yasal metinler'), 200);
    await tester.pumpAndSettle();

    expect(find.text('Yasal metinler'), findsOneWidget);
  });
}
