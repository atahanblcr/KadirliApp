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
import 'package:kadirli_app/features/notifications/data/models/app_notification.dart';
import 'package:kadirli_app/features/notifications/presentation/widgets/notification_tile.dart';
import 'package:kadirli_app/features/pharmacies/presentation/widgets/pharmacy_tile.dart';

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
}
