import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/files/data/files_repository.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// `POST /v1/files/upload` — profil fotoğrafı (11.5) ve ileride ilan
/// görselleri (11.9) aynı yoldan geçiyor.
void main() {
  late Directory tempDir;
  late File imageFile;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('kadirli_upload_test');
    imageFile = File('${tempDir.path}/avatar.jpg');
    // İçerik önemli değil: sunucu magic-byte bakar, istemci yalnız gönderir.
    await imageFile.writeAsBytes(List<int>.filled(64, 7));
  });

  tearDown(() async => tempDir.delete(recursive: true));

  test('multipart gövde "file" alanıyla gider, yanıt id/url taşır', () async {
    final adapter = routedAdapter({
      '/v1/files/upload': (_) async => jsonResponse(
        successEnvelope(const {
          'id': '44444444-4444-4444-4444-444444444444',
          'cdnUrl': '/uploads/profile/avatar.jpg',
          'originalName': 'avatar.jpg',
        }),
      ),
    });

    final uploaded = await FilesRepository(testApiClient(adapter)).upload(
      filePath: imageFile.path,
      moduleType: 'profile',
    );

    expect(uploaded.id, '44444444-4444-4444-4444-444444444444');
    expect(uploaded.cdnUrl, '/uploads/profile/avatar.jpg');

    final request = adapter.lastOf('/v1/files/upload')!;
    expect(request.method, 'POST');
    final form = request.data as FormData;
    expect(form.files.map((entry) => entry.key), contains('file'));
    // MapEntry yapısal eşitlik taşımaz → anahtar/değer ayrı denetlenir.
    expect(
      {for (final field in form.fields) field.key: field.value},
      {'moduleType': 'profile'},
    );
  });

  test('desteklenmeyen tür sunucudan hata olarak döner', () async {
    final adapter = routedAdapter({
      '/v1/files/upload': (_) async => jsonResponse(
        errorEnvelope(
          'UNSUPPORTED_FILE_TYPE',
          'Yalnızca JPEG, PNG veya WebP görselleri yüklenebilir.',
        ),
        statusCode: 400,
      ),
    });

    await expectLater(
      FilesRepository(testApiClient(adapter)).upload(filePath: imageFile.path),
      throwsA(
        isA<ApiException>().having(
          (error) => error.message,
          'message',
          contains('JPEG'),
        ),
      ),
    );
  });
}
