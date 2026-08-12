import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_body.dart';

/// **Görünmez sözleşme #61'in istemci ayağı** — haber gövdesinin tek çizim
/// sahibi `NewsBody` (12.14).
///
/// Üç ayrı sessiz hasar tek dosyada:
/// (a) `onLinkTap` bağlanmazsa gövdedeki `<a>` **çizilir ama hiçbir şey
///     yapmaz** — "işlevsiz buton yok" kuralının gövde içindeki karşılığı;
///     buton görünür, tıklanır, hiçbir yere gitmez ve log temizdir.
/// (b) `<img>` uzantısı kalkarsa paketin varsayılanı `Image.network`'e düşer:
///     önbelleklemez (aynı görsel her kaydırmada yeniden iner) ve **açılmayan
///     görselin yerine kırık bir kutu** çizer. Oysa metin arası görseller
///     aynalanmıyor ve %9'u **süreli** `fbcdn` linki (12.12 ölçümü) — yani
///     zamanla mutlaka kırılacaklar.
/// (c) İstemcide **ikinci bir beyaz liste yazılmaz**: temizlik alım anında
///     sunucuda yapıldı (`NewsHtmlPolicy`). İki sahipli bir güvenlik kuralı,
///     ayrıştıkları anda hangisinin doğru olduğu bilinemeyen iki gerçeklik
///     üretir — bu test istemcinin sunucudan geleni **kırpmadığını** kilitler.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const channel = MethodChannel('plugins.flutter.io/url_launcher');
  late List<String> launched;

  setUp(() {
    launched = [];
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          final url = (call.arguments as Map?)?['url'] as String? ?? '';
          switch (call.method) {
            case 'canLaunch':
              return true;
            case 'launch':
              launched.add(url);
              return true;
            default:
              return null;
          }
        });
  });

  tearDown(() {
    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, null);
  });

  Future<void> pumpBody(WidgetTester tester, String html) async {
    tester.view.physicalSize = const Size(1080, 2400);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: Scaffold(
          body: SingleChildScrollView(child: NewsBody(html: html)),
        ),
      ),
    );
    // ⚠️ `pumpAndSettle` DEĞİL: `CachedNetworkImage` yer tutucusu sonsuz
    // animasyon çalıştırabiliyor.
    await tester.pump();
  }

  testWidgets('paragraflar metin olarak çizilir, ham etiket görünmez', (
    tester,
  ) async {
    await pumpBody(
      tester,
      '<p>Kadirli Belediyesi açık hava sineması etkinliklerine devam ediyor.</p>'
      '<h2>Etkinlik programı</h2>',
    );

    expect(find.textContaining('açık hava sineması'), findsWidgets);
    expect(find.textContaining('<p>'), findsNothing);
    expect(find.textContaining('Etkinlik programı'), findsWidgets);
  });

  testWidgets('bağlantıya dokunmak tarayıcıya gider (ölü bağlantı yok)', (
    tester,
  ) async {
    await pumpBody(
      tester,
      '<p>Ayrıntılar <a href="https://www.silagazetesi.com.tr/detay/">burada</a>.</p>',
    );

    await tester.tap(find.textContaining('burada'));
    await tester.pump();

    expect(launched, ['https://www.silagazetesi.com.tr/detay/']);
  });

  testWidgets('href boşsa dokunuş sessizce yok sayılır (çökmez)', (
    tester,
  ) async {
    await pumpBody(tester, '<p>Bağlantısız <a href="">metin</a>.</p>');

    await tester.tap(find.textContaining('metin'));
    await tester.pump();

    expect(launched, isEmpty);
    expect(tester.takeException(), isNull);
  });

  testWidgets('gövde içi görsel ÖNBELLEKLİ bileşenle çizilir', (tester) async {
    await pumpBody(
      tester,
      '<figure><img src="https://www.silagazetesi.com.tr/wp-content/1.jpg">'
      '<figcaption>Fotoğraf: Sıla Gazetesi</figcaption></figure>',
    );

    expect(find.byType(CachedNetworkImage), findsOneWidget);
    expect(find.textContaining('Fotoğraf'), findsWidgets);
  });

  testWidgets('src\'siz görsel hiç yer kaplamaz (kırık kutu çizilmez)', (
    tester,
  ) async {
    await pumpBody(tester, '<p>Metin</p><img src="">');

    expect(find.byType(CachedNetworkImage), findsNothing);
    expect(tester.takeException(), isNull);
  });

  testWidgets('istemci sunucudan geleni KIRPMAZ (tek sahip sunucu)', (
    tester,
  ) async {
    // Sunucunun beyaz listesindeki etiketler (12.12 `NewsHtmlPolicy`) istemcide
    // ikinci bir süzgeçten geçmemeli; geçseydi gazetenin yarın kullanacağı bir
    // etiket **sessizce kaybolurdu**.
    await pumpBody(
      tester,
      '<blockquote>Alıntı metni</blockquote>'
      '<ul><li>Birinci madde</li><li>İkinci madde</li></ul>'
      '<p><strong>Kalın</strong> ve <em>eğik</em> metin.</p>',
    );

    expect(find.textContaining('Alıntı metni'), findsWidgets);
    expect(find.textContaining('Birinci madde'), findsWidgets);
    expect(find.textContaining('İkinci madde'), findsWidgets);
    expect(find.textContaining('Kalın'), findsWidgets);
  });
}
