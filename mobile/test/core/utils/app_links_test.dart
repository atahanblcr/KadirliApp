import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/utils/utils.dart';

/// Dış uygulama yönlendirmeleri (`AppLinks`).
///
/// **Neden test ediliyor:** üretilen URL sessizce yanlış olabilir — hatalı
/// normalleştirilmiş bir numara **yanlış kişinin WhatsApp sohbetini** açar ve
/// kullanıcı bunu ancak karşı taraf cevap verince anlar. Numara biçimleri
/// (`0532…`, `+90532…`, `532…`) Kadirli'de üçü de elle giriliyor.
///
/// `url_launcher` platform kanalı testte sahtelenip **açılan URL yakalanıyor**.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  const channel = MethodChannel('plugins.flutter.io/url_launcher');
  late List<String> launched;
  late Set<String> canLaunchFalseFor;

  setUp(() {
    launched = [];
    canLaunchFalseFor = {};

    TestDefaultBinaryMessengerBinding.instance.defaultBinaryMessenger
        .setMockMethodCallHandler(channel, (call) async {
          final url = (call.arguments as Map?)?['url'] as String? ?? '';
          switch (call.method) {
            case 'canLaunch':
              return !canLaunchFalseFor.any(url.startsWith);
            case 'launch':
              if (canLaunchFalseFor.any(url.startsWith)) return false;
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

  group('telefon', () {
    test('boşluklu numara tel: şemasında sadeleşir', () async {
      expect(await AppLinks.call('0532 111 00 01'), isTrue);
      expect(launched.single, 'tel:05321110001');
    });

    test('+90 önekli numara korunur', () async {
      await AppLinks.call('+90 532 111 00 01');
      expect(launched.single, 'tel:+905321110001');
    });

    test('rakamsız değerde arama denenmez', () async {
      expect(await AppLinks.call('  -  '), isFalse);
      expect(launched, isEmpty);
    });
  });

  group('WhatsApp', () {
    test('0 ile başlayan numara 90 önekine çevrilir', () async {
      expect(await AppLinks.whatsapp('0532 111 00 01'), isTrue);
      expect(launched.single, startsWith('https://wa.me/905321110001'));
    });

    test('+90 önekli numara iki kez öneklenmez', () async {
      await AppLinks.whatsapp('+905321110001');
      expect(launched.single, startsWith('https://wa.me/905321110001'));
    });

    test('öneksiz 10 hane 90 ile tamamlanır', () async {
      await AppLinks.whatsapp('5321110001');
      expect(launched.single, startsWith('https://wa.me/905321110001'));
    });

    test('mesaj text parametresi olarak eklenir', () async {
      await AppLinks.whatsapp('05321110001', message: 'Merhaba "Egea" ilanı');
      expect(launched.single, contains('text=Merhaba'));
      expect(launched.single, contains('Egea'));
    });

    test('mesaj yoksa boş text parametresi eklenmez', () async {
      await AppLinks.whatsapp('05321110001');
      expect(launched.single, isNot(contains('text=')));
    });

    test('numara boşsa WhatsApp açılmaz', () async {
      expect(await AppLinks.whatsapp('abc'), isFalse);
      expect(launched, isEmpty);
    });
  });

  group('harita', () {
    test('koordinat varsa önce geo: şeması denenir', () async {
      await AppLinks.map(latitude: 37.3708, longitude: 36.0961, label: 'Eczane');
      expect(launched.single, startsWith('geo:37.3708,36.0961'));
      expect(launched.single, contains('Eczane'));
    });

    test('geo: desteklenmiyorsa Google Haritalar bağlantısına düşer', () async {
      canLaunchFalseFor = {'geo:'};
      await AppLinks.map(latitude: 37.3708, longitude: 36.0961);
      expect(launched.single, startsWith('https://www.google.com/maps/search/'));
      expect(launched.single, contains('37.3708%2C36.0961'));
    });

    test('koordinat yoksa adres metniyle aranır', () async {
      await AppLinks.mapSearch('Cumhuriyet Mah. Kadirli');
      expect(launched.single, startsWith('https://www.google.com/maps/search/'));
      expect(launched.single, contains('Kadirli'));
    });

    test('boş adresle harita açılmaz', () async {
      expect(await AppLinks.mapSearch('   '), isFalse);
      expect(launched, isEmpty);
    });
  });

  group('web ve e-posta', () {
    test('şemasız adrese https eklenir', () async {
      await AppLinks.web('kadirli.bel.tr');
      expect(launched.single, 'https://kadirli.bel.tr');
    });

    test('http ile başlayan adres olduğu gibi açılır', () async {
      await AppLinks.web('http://kadirli.bel.tr/duyuru');
      expect(launched.single, 'http://kadirli.bel.tr/duyuru');
    });

    test('e-posta mailto şemasıyla açılır', () async {
      await AppLinks.email('bilgi@kadirli.bel.tr', subject: 'İlan hakkında');
      expect(launched.single, startsWith('mailto:bilgi@kadirli.bel.tr'));
      expect(launched.single, contains('subject='));
    });
  });

  test('platform açamazsa sessizce false döner (çağıran şerit gösterir)',
      () async {
    canLaunchFalseFor = {'tel:'};
    expect(await AppLinks.call('05321110001'), isFalse);
  });
}
