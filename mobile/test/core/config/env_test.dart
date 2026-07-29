import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/config/env.dart';

void main() {
  test('varsayılan flavor dev', () {
    expect(Env.flavor, AppFlavor.dev);
    expect(Env.isDev, isTrue);
  });

  test('base URL sonunda / yok ve /v1 içermez (uçlar kendi ekler)', () {
    expect(Env.apiBaseUrl.endsWith('/'), isFalse);
    expect(Env.apiBaseUrl.contains('/v1'), isFalse);
  });

  test('dev base URL masaüstü/simülatörde localhost:5005', () {
    // Testler host makinede (Android değil) koşar → localhost beklenir.
    // Android emülatöründe 10.0.2.2'ye döner (bkz. Env._devBaseUrl).
    expect(Env.apiBaseUrl, 'http://localhost:${Env.apiPort}');
  });
}
