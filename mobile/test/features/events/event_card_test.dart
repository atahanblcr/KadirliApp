import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/core/utils/app_date.dart';
import 'package:kadirli_app/features/campaigns/data/models/campaign.dart';
import 'package:kadirli_app/features/campaigns/presentation/widgets/campaign_card.dart';
import 'package:kadirli_app/features/events/data/models/event.dart';
import 'package:kadirli_app/features/events/presentation/widgets/event_card.dart';

/// Etkinlik ve kampanya kartlarının **taşma dayanıklılığı** (11.10).
///
/// Bu projede dar sütun + `Row` içindeki metin dört fazda üst üste `RenderFlex`
/// taşması üretti (11.7 `PharmacyTile`, 11.8 `AdCard`, 11.9 `_FavoriteTile`).
/// Yeni iki kart aynı kombinasyonu her koşuda deniyor: uzun başlık + uzun mekan
/// adı + 1.4 yazı ölçeği + dar ekran.
void main() {
  // Kart tarih biçimlemesi Türkçe ay adı kullanıyor (`d MMM`).
  setUpAll(() async => initializeDateFormatting('tr_TR'));

  Event event({
    String title = 'Karakucak Güreş Festivali',
    String? venue = 'Şehir Stadyumu',
    String? category = 'Spor',
    bool isFree = true,
    double? price,
    int inDays = 2,
  }) => Event(
    id: 'e1',
    title: title,
    // ⚠️ `eventDate` sunucuda "Türkiye günü, 00:00 UTC" olarak yazılır ve model
    // onu **kaydırmadan** okur. `DateTime.now().toUtc()` verilirse saat
    // 00:00-03:00 arasında gün bir geri kayıyor ve test yalnız gece patlıyordu.
    eventDate: _turkeyDay(inDays),
    eventTime: '10:00:00',
    venueName: venue,
    categoryName: category,
    isFree: isFree,
    ticketPrice: price,
  );

  Campaign campaign({
    String title = 'Yaz İndirimi',
    String? business = 'Kadirli Kırtasiye',
    double? discount = 25,
    String? code = 'YAZ25',
    int endsIn = 3,
  }) => Campaign(
    id: 'c1',
    businessId: 'b1',
    businessName: business,
    title: title,
    discountPercentage: discount,
    discountCode: code,
    startDate: DateTime.now().toUtc().subtract(const Duration(days: 3)),
    endDate: DateTime.now().toUtc().add(Duration(days: endsIn)),
  );

  Future<void> pumpCard(
    WidgetTester tester,
    Widget card, {
    double textScale = 1,
    Size surface = const Size(1080, 2400),
  }) async {
    tester.view.physicalSize = surface;
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light,
        home: MediaQuery(
          data: MediaQueryData(textScaler: TextScaler.linear(textScale)),
          child: Scaffold(
            body: ListView(padding: const EdgeInsets.all(16), children: [card]),
          ),
        ),
      ),
    );
    await tester.pump(const Duration(milliseconds: 100));
  }

  group('EventCard', () {
    testWidgets('tarih, saat, mekan ve ücretsiz rozeti gösterilir', (
      tester,
    ) async {
      await pumpCard(tester, EventCard(event: event()));

      expect(find.text('Karakucak Güreş Festivali'), findsOneWidget);
      expect(find.text('10:00'), findsOneWidget);
      expect(find.text('Şehir Stadyumu'), findsOneWidget);
      expect(find.text('Ücretsiz'), findsOneWidget);
      expect(find.text('Spor'), findsOneWidget);
    });

    testWidgets('bugünkü etkinlikte "Bugün" rozeti çıkar', (tester) async {
      await pumpCard(tester, EventCard(event: event(inDays: 0)));
      expect(find.text('Bugün'), findsOneWidget);
    });

    testWidgets('ücretli etkinlikte fiyat, fiyatsızda rozet yok', (
      tester,
    ) async {
      await pumpCard(
        tester,
        EventCard(event: event(isFree: false, price: 150)),
      );
      expect(find.text('150 ₺'), findsOneWidget);
      expect(find.text('Ücretsiz'), findsNothing);

      // Ücretli ama fiyatı girilmemiş etkinlikte "0 ₺" YAZILMAZ.
      await pumpCard(tester, EventCard(event: event(isFree: false)));
      expect(find.textContaining('₺'), findsNothing);
    });

    testWidgets('uzun başlık + uzun mekan + 1.4 ölçek dar ekranda taşmaz', (
      tester,
    ) async {
      await pumpCard(
        tester,
        EventCard(
          event: event(
            title:
                'Kadirli Belediyesi 30. Geleneksel Karakucak Güreş ve Kültür '
                'Festivali Kapanış Programı',
            venue: 'Kadirli Şehir Stadyumu Yanı Kültür Park Açık Hava Sahnesi',
            category: 'Festival ve Şenlikler',
          ),
        ),
        textScale: 1.4,
        surface: const Size(720, 1600), // 240 dp genişlik: en dar telefon
      );

      expect(tester.takeException(), isNull);
    });
  });

  group('CampaignCard', () {
    testWidgets('işletme, indirim ve kod rozetleri gösterilir', (tester) async {
      await pumpCard(tester, CampaignCard(campaign: campaign()));

      expect(find.text('Yaz İndirimi'), findsOneWidget);
      expect(find.text('Kadirli Kırtasiye'), findsOneWidget);
      expect(find.text('%25 indirim'), findsOneWidget);
      expect(find.text('İndirim kodu'), findsOneWidget);
      expect(find.text('3 gün kaldı'), findsOneWidget);
    });

    testWidgets('kodsuz/indirimsiz kampanyada boş rozet çizilmez', (
      tester,
    ) async {
      await pumpCard(
        tester,
        CampaignCard(campaign: campaign(discount: null, code: null, endsIn: 40)),
      );

      expect(find.text('İndirim kodu'), findsNothing);
      expect(find.textContaining('indirim'), findsNothing);
      expect(find.textContaining('gün kaldı'), findsNothing);
    });

    testWidgets('uzun başlık + uzun işletme adı + 1.4 ölçek dar ekranda taşmaz', (
      tester,
    ) async {
      await pumpCard(
        tester,
        CampaignCard(
          campaign: campaign(
            title:
                'Okula Dönüş Kampanyası: Tüm kırtasiye ürünlerinde geçerli '
                'büyük indirim fırsatı',
            business: 'Kadirli Merkez Okul Kırtasiye ve Fotokopi Merkezi',
          ),
        ),
        textScale: 1.4,
        surface: const Size(720, 1600),
      );

      expect(tester.takeException(), isNull);
    });
  });
}

/// Bugünden [inDays] gün sonrasının **Kadirli takvim günü**, 00:00 UTC olarak.
DateTime _turkeyDay(int inDays) {
  final day = AppDate.nowInTurkey.add(Duration(days: inDays));
  return DateTime.utc(day.year, day.month, day.day);
}
