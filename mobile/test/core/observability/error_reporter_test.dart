import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/core/observability/error_reporter.dart';
import 'package:package_info_plus/package_info_plus.dart';

import '../network/fake_http_adapter.dart';

/// Faz 12.1 — mobil hata raporlayıcısı.
///
/// 12.1 öncesinde mobilde oluşan hata **hiçbir yere** akmıyordu: Crashlytics yok, uç yok.
/// Kullanıcının gördüğü çökme yalnız kullanıcının telefonundaydı.
///
/// 🔑 Bu testlerin çoğu "rapor gitti mi" değil, **"gitmeyi denerken zarar vermiyor mu"**
/// sorusuna bakıyor. Bir gözlem katmanının en kötü hâli, gözlemlediği sistemi bozmasıdır.
void main() {
  const path = '/v1/client-errors';

  setUp(() {
    // package_info_plus platform kanalı testte yok — sahte değer verilir,
    // yoksa raporlayıcı gövdeyi hiç kuramaz.
    PackageInfo.setMockInitialValues(
      appName: 'KadirliApp',
      packageName: 'app.kadirli',
      version: '1.0.0',
      buildNumber: '7',
      buildSignature: '',
      installerStore: null,
    );
  });

  ({ProviderContainer container, FakeHttpAdapter adapter}) makeContainer({
    Future<dynamic> Function(dynamic options)? handler,
  }) {
    final adapter = FakeHttpAdapter(
      handler == null
          ? (_) async => jsonResponse(successEnvelope({'accepted': true}))
          : (options) async => await handler(options),
    );

    final container = ProviderContainer(
      overrides: [
        tokenStoreProvider.overrideWithValue(InMemoryTokenStore()),
        dioProvider.overrideWith(
          (ref) => DioClient.create(
            tokenStore: ref.watch(tokenStoreProvider),
            baseUrl: 'http://localhost:5005',
            adapter: adapter,
          ),
        ),
      ],
    );
    addTearDown(container.dispose);
    return (container: container, adapter: adapter);
  }

  Map<String, dynamic> bodyOf(FakeHttpAdapter adapter) {
    final data = adapter.lastOf(path)!.data;
    return data is String ? jsonDecode(data) as Map<String, dynamic> : data as Map<String, dynamic>;
  }

  test('yakalanmamış hata uca bildirilir', () async {
    final env = makeContainer();

    env.container.read(errorReporterProvider).reportUncaught(StateError('patladı'), StackTrace.current);
    await Future<void>.delayed(const Duration(milliseconds: 50));

    expect(env.adapter.countOf(path), 1);

    final body = bodyOf(env.adapter);
    expect(body['message'], contains('patladı'));
    expect(body['code'], 'StateError');
    expect(body['level'], 'fatal', reason: 'yakalanmamış eşzamansız hata çökme sınıfıdır');
    expect(body['stackTrace'], isNotEmpty);
    expect(body['appVersion'], '1.0.0+7');
  });

  test('çatı hatası "error" seviyesiyle gider (çökme değil)', () async {
    final env = makeContainer();

    env.container.read(errorReporterProvider).reportFlutterError(
      FlutterErrorDetails(exception: ArgumentError('kötü argüman'), stack: StackTrace.current),
    );
    await Future<void>.delayed(const Duration(milliseconds: 50));

    expect(bodyOf(env.adapter)['level'], 'error');
  });

  /// 🔴 `source` sunucuda sabitleniyor. İstemci gönderebilseydi kendi çökmesini
  /// sunucu hatası gibi gösterip "sunucumuzda kaç hata var?" sorusunu zehirlerdi.
  test('gövdede source alanı YOKTUR — kaynak sunucuda sabitlenir', () async {
    final env = makeContainer();

    env.container.read(errorReporterProvider).reportUncaught(StateError('x'), StackTrace.current);
    await Future<void>.delayed(const Duration(milliseconds: 50));

    expect(bodyOf(env.adapter).containsKey('source'), isFalse);
  });

  /// 🔴 Kural 2: gönderim başarısız olursa **sessizce yutulur**. Yutulmazsa
  /// ağ yokken: rapor gönder → başarısız → onu raporla → başarısız… sonsuz döngü.
  test('uç hata dönerse raporlayıcı fırlatmaz', () async {
    final env = makeContainer(
      handler: (_) async => jsonResponse(errorEnvelope('INTERNAL_ERROR', 'sunucu patladı'), statusCode: 500),
    );

    final reporter = env.container.read(errorReporterProvider);

    // Fırlatırsa test burada kırılır.
    reporter.reportUncaught(StateError('x'), StackTrace.current);
    await Future<void>.delayed(const Duration(milliseconds: 100));

    expect(env.adapter.countOf(path), 1, reason: 'denedi ama patlamadı');
  });

  /// Bir build döngüsündeki hata saniyede onlarca kez tetiklenebiliyor.
  /// Sunucu zaten tekilleştiriyor; bu kısma yalnız ağ trafiğini koruyor.
  test('aynı hata art arda gönderilirse kısılır', () async {
    final env = makeContainer();
    final reporter = env.container.read(errorReporterProvider);

    for (var i = 0; i < 5; i++) {
      reporter.reportUncaught(StateError('aynı hata'), StackTrace.current);
    }
    await Future<void>.delayed(const Duration(milliseconds: 100));

    expect(env.adapter.countOf(path), 1);
  });

  test('farklı hatalar ayrı ayrı gider (kısma yanlış hatayı yutmaz)', () async {
    final env = makeContainer();
    final reporter = env.container.read(errorReporterProvider);

    reporter.reportUncaught(StateError('birinci'), StackTrace.current);
    await Future<void>.delayed(const Duration(milliseconds: 60));
    reporter.reportUncaught(ArgumentError('ikinci'), StackTrace.current);
    await Future<void>.delayed(const Duration(milliseconds: 60));

    expect(env.adapter.countOf(path), 2);
  });

  /// Sunucu tavanı aşan gövdeyi **reddediyor** (kırpmıyor) — istemci kendi tarafında
  /// kırpar ki rapor hiç gitmemektense kısaltılmış gitsin.
  test('çok uzun mesaj ve yığın sunucu tavanına göre kırpılır', () async {
    final env = makeContainer();

    env.container.read(errorReporterProvider).reportUncaught(
      StateError('x' * 5000),
      StackTrace.fromString('y' * 40000),
    );
    await Future<void>.delayed(const Duration(milliseconds: 50));

    final body = bodyOf(env.adapter);
    expect((body['message'] as String).length, lessThanOrEqualTo(2000));
    expect((body['stackTrace'] as String).length, lessThanOrEqualTo(16000));
  });
}
