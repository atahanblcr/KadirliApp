import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/app.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/theme/theme_controller.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../core/network/fake_http_adapter.dart';

/// Testlerde gerçek uygulamayı (router + tema + ağ zinciri) ayağa kaldırır.
///
/// `flutter_secure_storage` ve gerçek HTTP platform kanalı testte yok →
/// `tokenStoreProvider` bellek deposuyla, `dioProvider` sahte adaptörle
/// override edilir. Geri kalan her şey (interceptor'lar, redirect, provider'lar)
/// üretimdeki gibi koşar.
const testBaseUrl = 'http://localhost:5005';

Future<ProviderContainer> pumpApp(
  WidgetTester tester, {
  Map<String, Object> prefs = const {},
  TokenStore? tokenStore,
  FakeHttpAdapter? adapter,
}) async {
  SharedPreferences.setMockInitialValues(prefs);
  final preferences = await SharedPreferences.getInstance();
  final store = tokenStore ?? InMemoryTokenStore();

  final container = ProviderContainer(
    overrides: [
      sharedPreferencesProvider.overrideWithValue(preferences),
      tokenStoreProvider.overrideWithValue(store),
      if (adapter != null)
        dioProvider.overrideWith(
          (ref) => DioClient.create(
            tokenStore: ref.watch(tokenStoreProvider),
            onSessionExpired: () => ref.read(sessionExpiredProvider.notifier).signal(),
            baseUrl: testBaseUrl,
            adapter: adapter,
          ),
        ),
    ],
  );
  addTearDown(container.dispose);

  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: const KadirliApp(),
    ),
  );
  await settleApp(tester);
  return container;
}

/// Açılış akışını (splash → bootstrap → yönlendirme) tamamlar.
///
/// Doğrudan `pumpAndSettle` kullanılamaz: splash'teki `CircularProgressIndicator`
/// sonsuz animasyon olduğu için "settle" durumu hiç gelmez.
Future<void> settleApp(WidgetTester tester) async {
  for (var i = 0; i < 6; i++) {
    await tester.pump(const Duration(milliseconds: 16));
  }
  await tester.pump(const Duration(milliseconds: 400));
}

/// Bir koşul sağlanana kadar bekler (widget'sız provider testlerinde).
///
/// ⚠️ **Sabit `Future.delayed` kullanmayın:** tek dosya çalışırken yeten süre,
/// tüm süit paralel koştuğunda yetmiyor ve test **flaky** oluyor (11.8 test
/// gözden geçirmesinde yakalandı). Koşul beklemek hem hızlı hem kararlı.
/// ⚠️ Zaman aşımı **cömert** (15 sn): koşul sağlanır sağlanmaz dönüldüğü için
/// geçen testleri yavaşlatmaz, ama süit büyüdükçe (11.10'da 473 test) paralel
/// koşan isolate'ler zaman zaman 5 sn'yi aşıyor ve testler **flaky** oluyordu.
Future<void> waitUntil(
  bool Function() condition, {
  Duration timeout = const Duration(seconds: 15),
  String? reason,
}) async {
  final deadline = DateTime.now().add(timeout);
  while (!condition()) {
    if (DateTime.now().isAfter(deadline)) {
      throw StateError(
        'waitUntil zaman aşımı${reason == null ? '' : ' — $reason'}',
      );
    }
    await Future<void>.delayed(const Duration(milliseconds: 5));
  }
}

/// Widget'sız provider testleri için kapsayıcı (aynı override'lar).
Future<ProviderContainer> testContainer({
  Map<String, Object> prefs = const {},
  TokenStore? tokenStore,
  FakeHttpAdapter? adapter,
}) async {
  SharedPreferences.setMockInitialValues(prefs);
  final preferences = await SharedPreferences.getInstance();

  final container = ProviderContainer(
    overrides: [
      sharedPreferencesProvider.overrideWithValue(preferences),
      tokenStoreProvider.overrideWithValue(tokenStore ?? InMemoryTokenStore()),
      if (adapter != null)
        dioProvider.overrideWith(
          (ref) => DioClient.create(
            tokenStore: ref.watch(tokenStoreProvider),
            onSessionExpired: () => ref.read(sessionExpiredProvider.notifier).signal(),
            baseUrl: testBaseUrl,
            adapter: adapter,
          ),
        ),
    ],
  );
  addTearDown(container.dispose);
  return container;
}

/// Test API'si için ApiClient (widget'sız provider testlerinde).
ApiClient testApiClient(
  FakeHttpAdapter adapter, {
  TokenStore? tokenStore,
  void Function()? onSessionExpired,
}) => ApiClient(
  DioClient.create(
    tokenStore: tokenStore ?? InMemoryTokenStore(),
    baseUrl: testBaseUrl,
    adapter: adapter,
    onSessionExpired: onSessionExpired,
  ),
);

/// Ana Sayfa (11.4) açılışta üç uç sorgular (nöbetçi eczane / kesinti /
/// duyuru); oturum açıksa rozet için bir dördüncüsü.
///
/// Başka bir konuya odaklanan testler (auth akışı, tema) bu varsayılanları
/// serper — böylece Ana Sayfa 404 gürültüsü üretmez:
/// `routedAdapter({...homeStubs(), '/v1/…': …})`
///
/// 🔑 **12.17:** kayıt ekranı ve sekme kabuğu artık KVKK uçlarına da bakıyor
/// (`/v1/legal/documents`, `/v1/users/me/consents`). Varsayılanları **boş
/// liste**: bu, taze bir kurulumun gerçek hâli (12.16 kararı — metin seed
/// edilmez) ve o hâlde kayıt akışı **birebir 12.17 öncesi gibi** çalışır.
/// Rıza akışını ölçen testler bunları `legalStubs()` ile serper.
Map<String, Future<ResponseBody> Function(RequestOptions options)> homeStubs({
  List<Map<String, dynamic>> onDuty = const [],
  List<Map<String, dynamic>> outages = const [],
  List<Map<String, dynamic>> announcements = const [],
  int unreadCount = 0,
}) => {
  ...legalStubs(),
  '/v1/pharmacies/on-duty': (_) async => jsonResponse(successEnvelope(onDuty)),
  '/v1/power-outages': (_) async => jsonResponse(successEnvelope(outages)),
  '/v1/announcements': (_) async => jsonResponse(
    successEnvelope({
      'items': announcements,
      'totalCount': announcements.length,
      'pageSize': 3,
      'currentPage': 1,
      'totalPages': announcements.isEmpty ? 0 : 1,
    }),
  ),
  '/v1/notifications': (_) async => jsonResponse(
    successEnvelope({
      'unreadCount': unreadCount,
      'items': <Object>[],
      'totalCount': 0,
      'pageSize': 1,
      'currentPage': 1,
      'totalPages': 0,
    }),
  ),
};

/// KVKK / hukuki metin uçları (12.17).
///
/// Varsayılan **boş**: taze kurulumda hiçbir belge yayınlanmamıştır ve kayıt
/// akışı 12.17 öncesiyle aynı davranır. Rıza akışını ölçen testler
/// [documents] / [consents] vererek gerçek senaryoyu kurar.
Map<String, Future<ResponseBody> Function(RequestOptions options)> legalStubs({
  List<Map<String, dynamic>> documents = const [],
  List<Map<String, dynamic>> consents = const [],
}) => {
  '/v1/legal/documents': (_) async => jsonResponse(successEnvelope(documents)),
  '/v1/users/me/consents': (_) async => jsonResponse(successEnvelope(consents)),
};

/// Tek bir hukuki belge gövdesi (`LegalDocumentDto`) — testlerin ortak kalıbı.
Map<String, dynamic> legalDocumentBody({
  String id = '11111111-1111-1111-1111-111111111111',
  String type = 'acik_riza',
  String title = 'Açık Rıza Metni',
  String versionId = 'aaaaaaaa-1111-1111-1111-111111111111',
  int versionNumber = 1,
  String? summary = 'Kişisel verilerimin işlenmesini kabul ediyorum.',
  String body = '<p>Rıza metninin tam hâli.</p>',
  bool isMandatory = true,
  bool showAtRegistration = true,
  int sortOrder = 0,
  bool requiresReconsent = false,
}) => {
  'id': id,
  'type': type,
  'title': title,
  'versionId': versionId,
  'versionNumber': versionNumber,
  'summary': summary,
  'body': body,
  'isMandatory': isMandatory,
  'showAtRegistration': showAtRegistration,
  'sortOrder': sortOrder,
  'effectiveFrom': '2026-08-01T00:00:00Z',
  'requiresReconsent': requiresReconsent,
};

/// Sahte yanıt üreticisi: yol → gövde eşlemesi.
FakeHttpAdapter routedAdapter(
  Map<String, Future<ResponseBody> Function(RequestOptions options)> routes, {
  Future<ResponseBody> Function(RequestOptions options)? fallback,
}) {
  return FakeHttpAdapter((options) async {
    final handler = routes[options.path];
    if (handler != null) return handler(options);
    if (fallback != null) return fallback(options);
    return jsonResponse(
      errorEnvelope('NOT_FOUND', 'Test yolu tanımlı değil: ${options.path}'),
      statusCode: 404,
    );
  });
}
