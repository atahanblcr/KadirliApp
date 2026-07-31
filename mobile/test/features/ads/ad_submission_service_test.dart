import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';
import 'package:kadirli_app/features/ads/application/ad_submission_service.dart';
import 'package:kadirli_app/features/ads/data/ads_repository.dart';
import 'package:kadirli_app/features/files/data/files_repository.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// İlan gönderme servisi — **görsel sıralama/kapak kararları** burada saf
/// mantık olarak test ediliyor (ekransız).
///
/// Sunucu sözleşmesi (`UpdateMyAdCommandHandler`): yeni görseller sona ve
/// `isCover=false` yazılır; kapak yalnız "hiç kapak kalmadıysa" en düşük
/// sıradakine verilir. Kapak değişimi bu yüzden **yeniden bağlama** ile
/// yapılıyor — bu testler o kuralı kilitliyor.
void main() {
  late FakeHttpAdapter adapter;
  late AdSubmissionService service;
  late Directory tempDir;
  var uploadCounter = 0;

  /// `MultipartFile.fromFile` gerçek bir dosya istiyor → geçici dosyalar.
  String tempImage(String name) {
    final file = File('${tempDir.path}/$name')..writeAsBytesSync([1, 2, 3]);
    return file.path;
  }

  /// Yüklenen her dosyaya sırayla `file-1`, `file-2` … kimliği verir.
  Future<ResponseBody> uploadHandler(RequestOptions options) async {
    uploadCounter++;
    return jsonResponse(
      successEnvelope({
        'id': 'file-$uploadCounter',
        'cdnUrl': '/uploads/file-$uploadCounter.png',
        'originalName': 'x.png',
      }),
    );
  }

  setUp(() {
    tempDir = Directory.systemTemp.createTempSync('ad-form-test');
    addTearDown(() => tempDir.deleteSync(recursive: true));
    uploadCounter = 0;
    adapter = routedAdapter({
      '/v1/files/upload': uploadHandler,
      '/v1/ads': (_) async =>
          jsonResponse(successEnvelope('new-ad-id'), statusCode: 201),
      '/v1/ads/ad-1': (_) async => jsonResponse(successEnvelope(true)),
    });
    final api = testApiClient(adapter);
    service = AdSubmissionService(AdsRepository(api), FilesRepository(api));
  });

  Map<String, dynamic> bodyOf(String path) =>
      Map<String, dynamic>.from(adapter.lastOf(path)!.data as Map);

  const values = AdFormValues(
    categoryId: 'cat-1',
    title: 'Bisiklet',
    description: 'Temiz',
    contactPhone: '+905321110001',
    price: 4500,
    sellerName: 'Ahmet',
    propertyValues: {'prop-1': 'Kırmızı'},
  );

  group('create', () {
    test('görseller sırayla yüklenir; ilk dosya kapak olur', () async {
      final id = await service.create(
        values: values,
        images: [
          AdFormImage.picked(tempImage('a.jpg')),
          AdFormImage.picked(tempImage('b.jpg')),
        ],
      );

      expect(id, 'new-ad-id');
      expect(adapter.countOf('/v1/files/upload'), 2);
      final body = bodyOf('/v1/ads');
      expect(body['imageFileIds'], ['file-1', 'file-2']);
      expect(body['categoryId'], 'cat-1');
      expect(body['price'], 4500);
      expect(body['propertyValues'], {'prop-1': 'Kırmızı'});
    });

    test('görselsiz ilan yüklemeye hiç girmez', () async {
      await service.create(values: values, images: const []);

      expect(adapter.countOf('/v1/files/upload'), 0);
      expect(bodyOf('/v1/ads')['imageFileIds'], isEmpty);
    });

    test('ilerleme geri çağrısı yüklenen/toplam bildirir', () async {
      final progress = <String>[];
      await service.create(
        values: values,
        images: [
          AdFormImage.picked(tempImage('a.jpg')),
          AdFormImage.picked(tempImage('b.jpg')),
        ],
        onProgress: (uploaded, total) => progress.add('$uploaded/$total'),
      );

      expect(progress, ['0/2', '1/2', '2/2']);
    });
  });

  group('update', () {
    const existingA = AdFormImage.existing(adImageId: 'img-a', fileId: 'file-a');
    const existingB = AdFormImage.existing(adImageId: 'img-b', fileId: 'file-b');

    test('sıra korunurken yalnız silinen ve eklenen görseller gönderilir', () async {
      await service.update(
        adId: 'ad-1',
        values: values,
        originalImages: const [existingA, existingB],
        images: [existingA, AdFormImage.picked(tempImage('c.jpg'))],
      );

      final body = bodyOf('/v1/ads/ad-1');
      expect(body['removeImageIds'], ['img-b']);
      expect(body['newImageFileIds'], ['file-1']);
    });

    test('hiçbir görsel değişmediyse ekleme/silme listeleri boş gider', () async {
      await service.update(
        adId: 'ad-1',
        values: values,
        originalImages: const [existingA, existingB],
        images: const [existingA, existingB],
      );

      final body = bodyOf('/v1/ads/ad-1');
      expect(body['removeImageIds'], isEmpty);
      expect(body['newImageFileIds'], isEmpty);
      expect(adapter.countOf('/v1/files/upload'), 0);
    });

    test('kapak değişince mevcut görseller yeni sırayla yeniden bağlanır', () async {
      // Kullanıcı ikinci fotoğrafı kapak yaptı → sunucuda "sıra" kavramı
      // ancak silip yeniden eklemekle ifade edilebiliyor.
      await service.update(
        adId: 'ad-1',
        values: values,
        originalImages: const [existingA, existingB],
        images: const [existingB, existingA],
      );

      final body = bodyOf('/v1/ads/ad-1');
      expect(body['removeImageIds'], ['img-a', 'img-b']);
      expect(
        body['newImageFileIds'],
        ['file-b', 'file-a'],
        reason: 'kapak (ilk sıradaki) kullanıcının seçtiği görsel olmalı',
      );
    });

    test('yeni görsel en öne alınırsa da yeniden bağlama yapılır', () async {
      await service.update(
        adId: 'ad-1',
        values: values,
        originalImages: const [existingA],
        images: [AdFormImage.picked(tempImage('c.jpg')), existingA],
      );

      final body = bodyOf('/v1/ads/ad-1');
      expect(body['removeImageIds'], ['img-a']);
      expect(body['newImageFileIds'], ['file-1', 'file-a']);
    });

    test('propertyValues null gönderilirse sunucu değerlere dokunmaz', () async {
      await service.update(
        adId: 'ad-1',
        values: values,
        originalImages: const [],
        images: const [],
        propertyValues: null,
      );

      expect(bodyOf('/v1/ads/ad-1').containsKey('propertyValues'), isTrue);
      expect(bodyOf('/v1/ads/ad-1')['propertyValues'], isNull);
    });
  });

  test('beklenmedik create yanıtı sessiz null değil hata üretir', () async {
    final badAdapter = routedAdapter({
      '/v1/ads': (_) async => jsonResponse(successEnvelope({'id': 'x'})),
    });
    final badService = AdSubmissionService(
      AdsRepository(testApiClient(badAdapter)),
      FilesRepository(testApiClient(badAdapter)),
    );

    expect(
      () => badService.create(values: values, images: const []),
      throwsA(
        isA<ApiException>().having(
          (error) => error.code,
          'code',
          ApiErrorCodes.unexpectedResponse,
        ),
      ),
    );
  });
}
