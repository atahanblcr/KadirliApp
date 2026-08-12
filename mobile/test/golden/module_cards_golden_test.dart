// Golden'lar üretildikleri platformda karşılaştırılır (yazı tipi
// rasterleştirmesi işletim sistemine göre değişir) → CI'da ayrı bir
// macOS işinde koşarlar. Bkz. `.github/workflows/mobile.yml`.
@Tags(['golden'])
library;

import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/ads/data/models/ad_summary.dart';
import 'package:kadirli_app/features/ads/presentation/widgets/ad_card.dart';
import 'package:kadirli_app/features/announcements/data/models/announcement.dart';
import 'package:kadirli_app/features/announcements/presentation/widgets/announcement_tile.dart';
import 'package:kadirli_app/features/campaigns/data/models/campaign.dart';
import 'package:kadirli_app/features/campaigns/presentation/widgets/campaign_card.dart';
import 'package:kadirli_app/features/complaints/data/models/complaint.dart';
import 'package:kadirli_app/features/complaints/presentation/widgets/complaint_card.dart';
import 'package:kadirli_app/features/events/data/models/event.dart';
import 'package:kadirli_app/features/events/presentation/widgets/event_card.dart';
import 'package:kadirli_app/features/news/data/models/news_article.dart';
import 'package:kadirli_app/features/news/data/models/news_category.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_body.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_card.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_featured_card.dart';
import 'package:kadirli_app/features/notifications/data/models/app_notification.dart';
import 'package:kadirli_app/features/notifications/presentation/widgets/notification_tile.dart';
import 'package:kadirli_app/features/pharmacies/presentation/widgets/pharmacy_tile.dart';
import 'package:kadirli_app/features/transport/data/models/intercity_route.dart';
import 'package:kadirli_app/features/transport/presentation/widgets/intercity_route_card.dart';

import 'golden_harness.dart';

/// Modül liste kartlarının görsel regresyonu (11.15).
///
/// Bu kartların **altısı** geçmişte dar sütunda taşma verdi. Senaryolar bu
/// yüzden bilerek zorlayıcı: **uzun Türkçe başlık**, 7 haneli fiyat, uzun
/// rozet metni — kısa örneklerle hiçbir düzen hatası ortaya çıkmaz.
///
/// ⚠️ Tarih içeren kartlarda `now`/tarih **sabitlenir**: "3 saat önce" gibi
/// göreli metinler golden'ı saate bağımlı yapar ve gece yarısı kırar
/// (11.10/11.14'te iki kez yaşandı).
void main() {
  // Sabit bir "şimdi" — golden makinenin saatinden bağımsız olmalı.
  final now = DateTime.utc(2026, 8, 3, 12);

  testWidgets('AdCard — uzun başlık, 7 haneli fiyat, fiyatsız', (tester) async {
    AdSummary ad({required String title, double? price}) => AdSummary(
      id: 'ad-1',
      title: title,
      price: price,
      viewCount: 1234,
      createdAt: now.subtract(const Duration(days: 2)),
    );

    await expectGoldenSheet(
      tester,
      name: 'ad_card',
      height: 1400,
      scenarios: [
        GoldenScenario(
          'Uzun başlık + 7 haneli fiyat',
          AdCard(
            now: now,
            ad: ad(
              title: 'Sahibinden az kullanılmış, hatasız, tramer kaydı olmayan Fiat Egea',
              price: 1250000,
            ),
            isFavorite: true,
            onFavoriteTap: () {},
            onTap: () {},
          ),
        ),
        GoldenScenario(
          'Fiyatsız ilan',
          AdCard(
            now: now,
            ad: ad(title: 'Ücretsiz devren kitaplık', price: null),
            onFavoriteTap: () {},
            onTap: () {},
          ),
        ),
      ],
    );
  });

  testWidgets('AnnouncementTile — acil / önemli / normal', (tester) async {
    Announcement announcement({required String title, required int priority}) => Announcement(
      id: 'a-$priority',
      title: title,
      priority: priority,
      typeName: 'Belediye Duyurusu',
      sentAt: now.subtract(const Duration(hours: 5)),
    );

    await expectGoldenSheet(
      tester,
      name: 'announcement_tile',
      height: 1400,
      scenarios: [
        GoldenScenario(
          'Acil (uzun başlık)',
          AnnouncementTile(
            now: now,
            announcement: announcement(
              title: 'Savrun ve Cumhuriyet mahallelerinde su kesintisi yapılacaktır',
              priority: 3,
            ),
            onTap: () {},
          ),
        ),
        GoldenScenario(
          'Normal',
          AnnouncementTile(
            now: now,
            announcement: announcement(title: 'Pazar yeri düzenlemesi', priority: 1),
            onTap: () {},
          ),
        ),
      ],
    );
  });

  testWidgets('NotificationTile — okunmamış / okunmuş', (tester) async {
    AppNotification notification({required bool isRead}) => AppNotification(
      id: 'n-1',
      title: 'Yeni duyuru yayınlandı',
      body: 'Savrun mahallesinde planlı elektrik kesintisi 14:00-17:00 arasında sürecek.',
      relatedType: 'announcement',
      relatedId: '11111111-1111-1111-1111-111111111111',
      isRead: isRead,
      createdAt: now.subtract(const Duration(minutes: 20)),
    );

    await expectGoldenSheet(
      tester,
      name: 'notification_tile',
      height: 1300,
      scenarios: [
        GoldenScenario(
          'Okunmamış',
          NotificationTile(notification: notification(isRead: false), onTap: () {}, now: now),
        ),
        GoldenScenario(
          'Okunmuş',
          NotificationTile(notification: notification(isRead: true), onTap: () {}, now: now),
        ),
      ],
    );
  });

  testWidgets('PharmacyTile — rozetli, uzun ad + uzun adres (11.7 taşması)', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'pharmacy_tile',
      height: 1300,
      scenarios: [
        GoldenScenario(
          'Nöbetçi rozeti + uzun adres',
          PharmacyTile(
            name: 'Cumhuriyet Meydanı Merkez Eczanesi',
            address: 'Cumhuriyet Mahallesi, Atatürk Caddesi No: 145/A, Kadirli / Osmaniye',
            pharmacistName: 'Ecz. Ayşe Demir Yılmaz',
            workingHours: '19:00 - 09:00',
            badge: 'Bugün nöbetçi',
            onTap: () {},
          ),
        ),
        GoldenScenario(
          'Yalnız ad (eksik veri uydurulmaz)',
          PharmacyTile(name: 'Şifa Eczanesi', onTap: () {}),
        ),
      ],
    );
  });

  testWidgets('ComplaintCard — durum rozeti + yetkili yanıtı', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'complaint_card',
      height: 1500,
      scenarios: [
        GoldenScenario(
          'Çözüldü + yetkili yanıtı',
          ComplaintCard(
            now: now,
            complaint: Complaint(
              id: 'c-1',
              subject: 'Sokak lambaları akşam saatlerinde yanmıyor',
              message: 'Savrun mahallesi 4. sokakta lambalar bir haftadır yanmıyor.',
              type: 'complaint',
              status: 'resolved',
              adminNotes:
                  'Ekiplerimiz bölgeye yönlendirildi, arıza giderilmiştir. İlginiz için teşekkürler.',
              createdAt: now.subtract(const Duration(days: 3)),
              resolvedAt: now.subtract(const Duration(days: 1)),
            ),
          ),
        ),
        GoldenScenario(
          'Bekliyor',
          ComplaintCard(
            now: now,
            complaint: Complaint(
              id: 'c-2',
              subject: 'Pazar yerine bank talebi',
              message: 'Pazar yerinde oturacak yer yok.',
              type: 'request',
              status: 'pending',
              createdAt: now.subtract(const Duration(hours: 6)),
            ),
          ),
        ),
      ],
    );
  });

  testWidgets('EventCard — yaklaşan / geçmiş / ücretsiz', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'event_card',
      height: 1300,
      scenarios: [
        GoldenScenario(
          'Yaklaşan, ücretsiz, uzun başlık',
          EventCard(
            now: now,
            event: Event(
              id: 'e-1',
              title: 'Kadirli Belediyesi Geleneksel Yaz Konserleri — Açılış Gecesi',
              eventDate: DateTime.utc(2026, 8, 10),
              eventTime: '20:30:00',
              venueName: 'Kültür Merkezi',
              isFree: true,
              // Faz 12.4 — kendi ilçemiz: konum rozeti vurgulu çizilir.
              locationLabel: 'Kadirli',
            ),
            onTap: () {},
          ),
        ),
        GoldenScenario(
          'Ücretli',
          EventCard(
            now: now,
            event: Event(
              id: 'e-2',
              title: 'Tiyatro: Bir Yaz Gecesi',
              eventDate: DateTime.utc(2026, 8, 26),
              eventTime: '19:00:00',
              venueName: 'Halk Eğitim Salonu',
              ticketPrice: 150,
              locationLabel: 'Osmaniye / Merkez',
              isLocal: false,
            ),
            onTap: () {},
          ),
        ),
        // 🔴 Faz 12.4 — uzun Türkçe konum + uzun mekan adı aynı kartta. Kısa
        // örnek hiçbir düzen hatası göstermez; bu projede `Row`'a giren metin
        // yedi kez taşma üretti ve her seferinde kısa fixture'la gözden kaçtı.
        GoldenScenario(
          'Çevre il, uzun konum + uzun mekan',
          EventCard(
            now: now,
            event: Event(
              id: 'e-3',
              title: 'Uluslararası Kahramanmaraş Dondurma ve Kültür Festivali',
              eventDate: DateTime.utc(2026, 9, 2),
              eventTime: '18:00:00',
              venueName: 'Kahramanmaraş Büyükşehir Belediyesi Kongre Merkezi',
              locationLabel: 'Kahramanmaraş',
              isLocal: false,
              isFree: true,
            ),
            onTap: () {},
          ),
        ),
      ],
    );
  });

  testWidgets('CampaignCard — indirim + son gün rozeti (11.10 taşması)', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'campaign_card',
      height: 1300,
      scenarios: [
        GoldenScenario(
          'İndirim + aciliyet rozeti',
          CampaignCard(
            now: now,
            campaign: Campaign(
              id: 'k-1',
              businessId: 'b-1',
              businessName: 'Kadirli Mobilya ve Ev Tekstili Merkezi',
              title: 'Yaz sonu tüm oturma gruplarında büyük indirim',
              discountPercentage: 25,
              discountCode: 'YAZ25',
              startDate: now.subtract(const Duration(days: 10)),
              endDate: now.add(const Duration(days: 1)),
            ),
            onTap: () {},
          ),
        ),
      ],
    );
  });

  // 🔴 Faz 12.6 — açık kart: araç rozeti, kalkış noktası + "Yol tarifi",
  // gün rozetli saat hapları. `now` **sabit ve Pazartesi** (3 Ağustos 2026,
  // Kadirli 15:00): gün rozetli kart artık yalnız saate değil **haftanın
  // gününe** de bakıyor, yani enjekte edilmeyen bir `now` bu referansı
  // haftada bir değil **her gün** kırardı.
  testWidgets('IntercityRouteCard — araç tipi · kalkış noktası · gün rozetleri', (
    tester,
  ) async {
    IntercityRoute route({
      required String id,
      required String destination,
      String? company,
      String vehicleType = 'bus',
      String? departurePointName,
      String? departurePointAddress,
      double? latitude,
      double? longitude,
      required List<(String, List<String>)> schedules,
    }) => IntercityRoute(
      id: id,
      destination: destination,
      company: company,
      price: 220,
      durationMinutes: 105,
      vehicleType: vehicleType,
      departurePointName: departurePointName,
      departurePointAddress: departurePointAddress,
      departurePointLatitude: latitude,
      departurePointLongitude: longitude,
      schedules: [
        for (var i = 0; i < schedules.length; i++)
          IntercityDeparture(
            id: '$id-s$i',
            departureTime: schedules[i].$1,
            days: schedules[i].$2,
            runsDaily: schedules[i].$2.length == 7,
          ),
      ],
    );

    const daily = ['mon', 'tue', 'wed', 'thu', 'fri', 'sat', 'sun'];
    const weekdays = ['mon', 'tue', 'wed', 'thu', 'fri'];

    await expectGoldenSheet(
      tester,
      name: 'intercity_route_card',
      height: 2600,
      scenarios: [
        GoldenScenario(
          'Minibüs · uzun kalkış noktası · karışık günler',
          IntercityRouteCard(
            now: now,
            expanded: true,
            onToggle: () {},
            onShare: () {},
            route: route(
              id: 'ic-1',
              destination: 'Kahramanmaraş Elbistan',
              company: 'Kadirli Öz Seyahat Turizm Taşımacılık',
              vehicleType: 'minibus',
              departurePointName: 'Kadirli Şehirlerarası Otobüs Terminali',
              departurePointAddress:
                  'Cumhuriyet Mahallesi Otogar Caddesi No:1, Kadirli/Osmaniye',
              latitude: 37.3745,
              longitude: 36.0972,
              schedules: const [
                ('06:30', weekdays),
                ('09:15', ['mon', 'wed', 'fri']),
                ('13:00', ['sat', 'sun']),
                ('18:45', daily),
              ],
            ),
          ),
        ),
        GoldenScenario(
          'Otobüs · her gün · kalkış noktası girilmemiş',
          IntercityRouteCard(
            now: now,
            expanded: true,
            onToggle: () {},
            onShare: () {},
            route: route(
              id: 'ic-2',
              destination: 'Adana',
              company: 'Kadirli Seyahat',
              schedules: const [('07:00', daily), ('14:00', daily)],
            ),
          ),
        ),
      ],
    );
  });

  testWidgets('NewsCard — uzun başlık, görselsiz, kaydedilmiş', (tester) async {
    NewsArticle article({
      required String title,
      String excerpt =
          'Kadirli Belediyesi, mahallelerde sosyal hayatı ve komşuluk '
          'bağlarını güçlendirmek amacıyla düzenlediği açık hava '
          'etkinliklerine devam ediyor.',
      String categoryName = 'Yerel Haberler',
    }) => NewsArticle(
      id: 'news-1',
      title: title,
      excerpt: excerpt,
      // ⚠️ Golden'da görsel YOK: `CachedNetworkImage` yer tutucusu sonsuz
      // shimmer çalıştırıyor ve referans görüntü kararsız olurdu.
      publishedAt: now.subtract(const Duration(hours: 3)),
      modifiedAt: now.subtract(const Duration(hours: 3)),
      readingMinutes: 4,
      categories: [NewsCategory(id: 'c1', name: categoryName, slug: 'slug')],
    );

    await expectGoldenSheet(
      tester,
      name: 'news_card',
      height: 1600,
      scenarios: [
        GoldenScenario(
          'Uzun başlık + uzun kategori',
          NewsCard(
            now: now,
            article: article(
              title:
                  'Osmaniye’de kamyonette 89 kilo 550 gram uyuşturucu madde '
                  'ele geçirildi, bir kişi tutuklandı',
              categoryName: 'Bilim ve Teknoloji',
            ),
            onTap: () {},
          ),
        ),
        GoldenScenario(
          'Kaydedilmiş haber',
          NewsCard(
            now: now,
            isSaved: true,
            article: article(
              title: 'Kadirli’de yaz akşamları sinema keyfiyle renkleniyor',
            ),
            onTap: () {},
          ),
        ),
        GoldenScenario(
          'Manşet kartı',
          NewsFeaturedCard(
            now: now,
            width: 280,
            article: article(
              title:
                  'Sumbas’ta Yaz Kur’an Kursları Arası Bilgi Yarışması '
                  'heyecanı yaşandı',
            ),
            onTap: () {},
          ),
        ),
      ],
    );
  });

  testWidgets('NewsBody — haber gövdesi (paragraf, başlık, liste, alıntı)', (
    tester,
  ) async {
    // ⚠️ Gövde **bilerek kısa ve sabit**: `flutter_html`in çıktısı paket
    // sürümüyle birlikte kayabilir ve uzun bir metin referansı her yükseltmede
    // kırardı — insan da `--update-goldens`'ı refleks hâline getirirdi
    // (bu projenin dört kez tekrarlamış golden tuzağı). Uzun Türkçe metin
    // senaryosu **kartlarda** (yukarıda), burada değil.
    //
    // 🔑 Golden'ın asıl işi: tipografi token'larının gövdeye gerçekten
    // uygulandığını ve **koyu temada okunabilir** kaldığını kilitlemek.
    // Gövde rengi `body` stiline yazılmasaydı paket kendi siyahını basar ve
    // koyu temada metin **siyah üstüne siyah** olurdu — hata vermeyen,
    // yalnız okunamayan bir ekran.
    await expectGoldenSheet(
      tester,
      name: 'news_body',
      height: 1500,
      scenarios: const [
        GoldenScenario(
          'Paragraf + ara başlık + kalın/eğik',
          NewsBody(
            html:
                '<p>Osmaniye’de bir dönem Yer Fıstığı Müzesi olarak hizmet '
                'veren simgesel yapı, <strong>150 kişilik</strong> halk '
                'kütüphanesine dönüştürülüyor.</p>'
                '<h2>Çalışmalarda son durum</h2>'
                '<p>Projede <em>yüzde 95</em> seviyesine ulaşıldı.</p>',
          ),
        ),
        GoldenScenario(
          'Liste + alıntı',
          NewsBody(
            html:
                '<ul><li>Kaba inşaat tamamlandı</li>'
                '<li>İnce işler sürüyor</li></ul>'
                '<blockquote>Kısa sürede gençlerin kullanımına '
                'sunulacak.</blockquote>',
          ),
        ),
      ],
    );
  });
}
