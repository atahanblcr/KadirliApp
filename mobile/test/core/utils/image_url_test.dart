import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/utils/utils.dart';

/// API_CONTRACT §7 — göreli URL'e istemci origin ekler, mutlak URL'e dokunmaz.
void main() {
  const base = 'http://localhost:5005';

  test('göreli yola origin eklenir', () {
    expect(
      AppImage.url('/uploads/abc_kapak.png', baseUrl: base),
      'http://localhost:5005/uploads/abc_kapak.png',
    );
  });

  test('baştaki eğik çizgi yoksa eklenir', () {
    expect(AppImage.url('uploads/a.png', baseUrl: base), 'http://localhost:5005/uploads/a.png');
  });

  test('mutlak URL (prod FileStorage:BaseUrl) olduğu gibi kalır', () {
    expect(
      AppImage.url('https://cdn.kadirli.app/uploads/a.png', baseUrl: base),
      'https://cdn.kadirli.app/uploads/a.png',
    );
  });

  test('null / boş değer null döner', () {
    expect(AppImage.url(null, baseUrl: base), isNull);
    expect(AppImage.url('   ', baseUrl: base), isNull);
  });

  test('galeri listesinde boşlar elenir', () {
    expect(AppImage.urls(['/uploads/a.png', null, '', '/uploads/b.png'], baseUrl: base), [
      'http://localhost:5005/uploads/a.png',
      'http://localhost:5005/uploads/b.png',
    ]);
  });
}
