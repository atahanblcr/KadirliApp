import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/router/app_router.dart';
import 'package:kadirli_app/features/news/application/news_text_scale.dart';

import '../../core/network/fake_http_adapter.dart';
import '../../helpers/pump_app.dart';

/// Haber detayı (12.14): gövde, kaynağa gidiş, kaydetme ve "bulunamadı".
void main() {
  const guest = {'auth.guestChoice': true};
  const detailPath = '/v1/news/n1';

  Map<String, dynamic> article({
    String id = 'n1',
    String? contentHtml =
        '<p>Kadirli Belediyesi açık hava sineması etkinliklerine devam ediyor.</p>'
        '<h2>Etkinlik programı</h2>'
        '<p>Gösterimler her cuma <strong>21.00</strong>’de başlıyor.</p>',
    String publishedAt = '2026-08-11T14:40:59Z',
    String modifiedAt = '2026-08-11T14:41:00Z',
    List<Map<String, dynamic>> categories = const [
      {'id': 'cat-gundem', 'name': 'Gündem', 'slug': 'gundem'},
      {'id': 'cat-yerel', 'name': 'Yerel Haberler', 'slug': 'yerel-haberler'},
    ],
  }) => {
    'id': id,
    'title': 'Kadirli’de yaz akşamları sinema keyfiyle renkleniyor',
    'excerpt': 'Kadirli Belediyesi açık hava sineması etkinliklerine devam ediyor.',
    'contentHtml': contentHtml,
    // ⚠️ Görsel bilinçli olarak yok: `CachedNetworkImage`'in yer tutucusu
    // sonsuz shimmer çalıştırıyor ve `pumpAndSettle` kilitleniyor
    // (ARCHITECTURE §8 "bilinen test tuzakları").
    'imageUrl': null,
    'sourceUrl': 'https://www.silagazetesi.com.tr/kadirlide-yaz-aksamlari/',
    'publishedAt': publishedAt,
    'modifiedAt': modifiedAt,
    'readingMinutes': 2,
    'isFeatured': false,
    'categories': categories,
  };

  Map<String, dynamic> pagedBody(List<Map<String, dynamic>> items) =>
      successEnvelope({
        'items': items,
        'totalCount': items.length,
        'pageSize': 20,
        'currentPage': 1,
        'totalPages': items.isEmpty ? 0 : 1,
      });

  Future<FakeHttpAdapter> openDetail(
    WidgetTester tester, {
    Map<String, Future<ResponseBody> Function(RequestOptions)> routes = const {},
    Map<String, Object> prefs = guest,
    String location = '/haberler/n1',
  }) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    final adapter = routedAdapter({
      ...homeStubs(),
      '/v1/news/categories': (_) async => jsonResponse(successEnvelope(const [])),
      '/v1/news': (_) async => jsonResponse(pagedBody(const [])),
      detailPath: (_) async => jsonResponse(successEnvelope(article())),
      ...routes,
    });
    final container = await pumpApp(tester, prefs: prefs, adapter: adapter);
    container.read(routerProvider).go(location);
    await tester.pumpAndSettle();
    return adapter;
  }

  testWidgets('başlık, meta ve HTML gövde biçimlenerek çizilir', (tester) async {
    await openDetail(tester);

    expect(
      find.text('Kadirli’de yaz akşamları sinema keyfiyle renkleniyor'),
      findsOneWidget,
    );
    expect(find.text('2 dk okuma'), findsOneWidget);
    // Gövde HTML olarak render ediliyor → ham etiket ekranda görünmemeli.
    expect(find.textContaining('<p>'), findsNothing);
    expect(find.textContaining('Etkinlik programı'), findsWidgets);
  });

  testWidgets('detayda TÜM kategoriler görünür (kartta yalnız ilki)', (
    tester,
  ) async {
    await openDetail(tester);

    expect(find.text('Gündem'), findsWidgets);
    expect(find.text('Yerel Haberler'), findsWidgets);
  });

  testWidgets('senkronun saniyelik farkı "Güncellendi" rozeti üretmez', (
    tester,
  ) async {
    // 🐛 Canlıda ölçüldü: publishedAt 14:40:59 ↔ modifiedAt 14:41:00. Eşik
    // olmasaydı **her haber** güncellenmiş görünürdü.
    await openDetail(tester);

    expect(find.textContaining('Güncellendi'), findsNothing);
  });

  testWidgets('gerçek bir düzeltmede "Güncellendi" yazar', (tester) async {
    await openDetail(
      tester,
      routes: {
        detailPath: (_) async => jsonResponse(
          successEnvelope(article(modifiedAt: '2026-08-11T17:10:00Z')),
        ),
      },
    );

    expect(find.textContaining('Güncellendi'), findsOneWidget);
  });

  testWidgets('gövde boşsa özet gösterilir (ekran boş kalmaz)', (tester) async {
    await openDetail(
      tester,
      routes: {
        detailPath: (_) async =>
            jsonResponse(successEnvelope(article(contentHtml: null))),
      },
    );

    expect(
      find.textContaining('açık hava sineması etkinliklerine devam ediyor'),
      findsWidgets,
    );
  });

  testWidgets('kaynağa giden buton her zaman var', (tester) async {
    await openDetail(tester);

    expect(find.text('Kaynakta oku'), findsOneWidget);
    expect(
      find.text('Haber içerikleri Sıla Gazetesi kaynaklıdır.'),
      findsOneWidget,
    );
  });

  testWidgets('kaldırılmış haber "bulunamadı" der, "Tekrar dene" DEMEZ', (
    tester,
  ) async {
    // Kaynakta yayından kalkan (12.12'nin `gone` durumu) ya da panelden
    // gizlenen kayıt 404 döner. "Tekrar dene" anlamsız: tekrar denemek de
    // bulmayacak.
    await openDetail(
      tester,
      routes: {
        detailPath: (_) async => jsonResponse(
          errorEnvelope('NOT_FOUND', 'Haber bulunamadı.'),
          statusCode: 404,
        ),
      },
    );

    expect(find.text('Haber bulunamadı'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsNothing);
  });

  testWidgets('yükleme hatasında traceId ile "Tekrar dene" çıkar', (
    tester,
  ) async {
    await openDetail(
      tester,
      routes: {
        detailPath: (_) async => jsonResponse(
          errorEnvelope('CONFLICT', 'Haber yüklenemedi.'),
          statusCode: 409,
        ),
      },
    );

    expect(find.text('Haber yüklenemedi.'), findsOneWidget);
    expect(find.text('Tekrar dene'), findsOneWidget);
  });

  group('ilgili haberler (plan dışı ek)', () {
    /// ⚠️ `/haberler/:id` bir **alt rota** (detaydan geri liste konumuna dönsün
    /// diye) ve go_router alt rotada üst ekranı da kurar → `/v1/news`e liste
    /// ekranı da istek atar. İki çağrıyı ayıran şey `categoryId`: ilgili
    /// haberler sorgusu onu taşır, liste ekranınınki taşımaz.
    Future<ResponseBody> Function(RequestOptions) relatedRoute(
      List<Map<String, dynamic>> related,
    ) => (options) async => jsonResponse(
      pagedBody(
        options.queryParameters.containsKey('categoryId') ? related : const [],
      ),
    );

    RequestOptions? relatedRequest(FakeHttpAdapter adapter) => adapter.requests
        .where(
          (request) =>
              request.path == '/v1/news' &&
              request.queryParameters.containsKey('categoryId'),
        )
        .lastOrNull;

    testWidgets('aynı kategoriden haberler UÇTAN gelir ve haberin kendisi elenir', (
      tester,
    ) async {
      final adapter = await openDetail(
        tester,
        routes: {
          '/v1/news': relatedRoute([
            // Okunan haberin kendisi listede: elenmezse kullanıcı zaten açık
            // olan habere geri dönen bir kart görürdü.
            article(id: 'n1'),
            {...article(id: 'n2'), 'title': 'İlgili başka haber'},
          ]),
        },
      );

      expect(find.text('Bu kategoriden'), findsOneWidget);
      expect(find.text('İlgili başka haber'), findsOneWidget);
      expect(
        relatedRequest(adapter)?.queryParameters['categoryId'],
        'cat-gundem',
      );
      expect(
        find.text('Kadirli’de yaz akşamları sinema keyfiyle renkleniyor'),
        findsOneWidget,
        reason: 'yalnız detayın kendi başlığı — ilgili şeritte tekrar etmemeli',
      );
    });

    testWidgets('ilgili haber yoksa bölüm hiç çizilmez', (tester) async {
      await openDetail(tester);

      expect(find.text('Bu kategoriden'), findsNothing);
    });

    testWidgets('kategorisiz haberde ilgili haber SORGUSU hiç atılmaz', (
      tester,
    ) async {
      final adapter = await openDetail(
        tester,
        routes: {
          detailPath: (_) async =>
              jsonResponse(successEnvelope(article(categories: const []))),
          '/v1/news': relatedRoute([
            {...article(id: 'n2'), 'title': 'İlgili başka haber'},
          ]),
        },
      );

      expect(find.text('Bu kategoriden'), findsNothing);
      expect(relatedRequest(adapter), isNull);
    });
  });

  group('kaydetme (plan dışı ek)', () {
    testWidgets('yer imine dokunmak kaydeder ve sonucu SÖYLER', (tester) async {
      // Sessizce çalışan bir yer imi butonu, çalışmayan bir butondan ayırt
      // edilemez ("işlevsiz buton yok" kuralının aynası).
      await openDetail(tester);

      await tester.tap(find.byIcon(Icons.bookmark_border_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Haber kaydedildi.'), findsOneWidget);
      expect(find.byIcon(Icons.bookmark_rounded), findsOneWidget);
    });

    testWidgets('ikinci dokunuş kaydı kaldırır', (tester) async {
      await openDetail(tester);

      await tester.tap(find.byIcon(Icons.bookmark_border_rounded));
      await tester.pumpAndSettle();
      await tester.tap(find.byIcon(Icons.bookmark_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Haber kayıtlardan çıkarıldı.'), findsOneWidget);
      expect(find.byIcon(Icons.bookmark_border_rounded), findsOneWidget);
    });

    testWidgets(
      'kaydedilmiş haber kaynakta kalkmışsa "Kaynakta oku" yine çalışır',
      (tester) async {
        // 🔑 Anlık görüntü saklamanın asıl kazancı: kayıt 404 verse bile
        // kullanıcı neyi kaydettiğini görür ve kaynağa gidebilir.
        await openDetail(
          tester,
          prefs: {
            ...guest,
            'news.saved': [
              '{"id":"n1","title":"Kaydedilmiş haber",'
                  '"sourceUrl":"https://www.silagazetesi.com.tr/x/"}',
            ],
          },
          routes: {
            detailPath: (_) async => jsonResponse(
              errorEnvelope('NOT_FOUND', 'Haber bulunamadı.'),
              statusCode: 404,
            ),
          },
        );

        expect(find.text('Haber bulunamadı'), findsOneWidget);
        expect(find.textContaining('Kaydedilmiş haber'), findsOneWidget);
        expect(find.text('Kaynakta oku'), findsOneWidget);
      },
    );
  });

  group('yazı boyutu (plan dışı ek)', () {
    testWidgets('denetim açılır ve dört boyutu sunar', (tester) async {
      await openDetail(tester);

      await tester.tap(find.byIcon(Icons.format_size_rounded));
      await tester.pumpAndSettle();

      expect(find.text('Okuma boyutu'), findsOneWidget);
      for (final scale in NewsTextScale.values) {
        expect(find.text(scale.label), findsOneWidget);
      }
      // Ayarın yalnız haber metnini büyüttüğü kullanıcıya SÖYLENİR: aksi hâlde
      // "neden bütün uygulama büyümedi?" sorusu doğar.
      expect(find.textContaining('Yalnız haber metnini'), findsOneWidget);
    });

    testWidgets('en büyük boyut + 1.4 sistem ölçeğinde ekran taşmıyor', (
      tester,
    ) async {
      // 🔴 Çarpım tavanı (1.6) burada kilitleniyor: tavansız 1.4 × 1.3 = 1.82 olurdu ve
      // ekranın en dar yerleri hiç denenmemiş bir ölçekte çizilirdi.
      await openDetail(
        tester,
        prefs: {...guest, 'news.textScale': 'huge'},
      );

      tester.platformDispatcher.textScaleFactorTestValue = 1.4;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
    });
  });
}
