import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/ads/data/models/ad_extend_result.dart';
import 'package:kadirli_app/features/ads/data/models/category_property.dart';
import 'package:kadirli_app/features/ads/data/models/my_ad.dart';

/// 11.9 modelleri — gövdeler backend DTO'larıyla birebir yazıldı
/// (`MyAdDto`, `CategoryPropertyDto`, `ExtendAdResultDto`); kontrat/model
/// ayrışması burada patlar.
void main() {
  Map<String, dynamic> myAdBody({
    String status = 'approved',
    String? rejectedReason,
    int extensionCount = 0,
    int maxExtensions = 3,
    DateTime? expiresAt,
  }) => {
    'id': 'ad-1',
    'title': 'Az kullanılmış bisiklet',
    'description': 'Temiz.',
    'price': 4500.00,
    'status': status,
    'categoryId': 'cat-1',
    'categoryName': 'Spor',
    'contactPhone': '+905321110001',
    'viewCount': 41,
    'phoneClickCount': 3,
    'whatsappClickCount': 2,
    'favoriteCount': 5,
    'extensionCount': extensionCount,
    'maxExtensions': maxExtensions,
    'rejectedReason': rejectedReason,
    'createdAt': '2026-07-01T09:00:00.0000000Z',
    'expiresAt':
        (expiresAt ?? DateTime.now().toUtc().add(const Duration(days: 20)))
            .toIso8601String(),
    'imageUrls': ['/uploads/a.png', '/uploads/b.png'],
  };

  group('MyAd', () {
    test('sunucu gövdesini eksiksiz okur', () {
      final ad = MyAd.fromJson(myAdBody());

      expect(ad.id, 'ad-1');
      expect(ad.price, 4500);
      expect(ad.statusKind, AdStatus.approved);
      expect(ad.viewCount, 41);
      expect(ad.contactCount, 5, reason: 'telefon + whatsapp');
      expect(ad.favoriteCount, 5);
      expect(ad.coverImageUrl, '/uploads/a.png');
    });

    test('bilinmeyen statü uygulamayı kırmaz', () {
      final ad = MyAd.fromJson(myAdBody(status: 'archived'));
      expect(ad.statusKind, AdStatus.unknown);
      expect(ad.canExtend, isFalse);
    });

    test('uzatma yalnız approved/expired ve hak kalmışken açıktır', () {
      expect(MyAd.fromJson(myAdBody(status: 'approved')).canExtend, isTrue);
      expect(MyAd.fromJson(myAdBody(status: 'expired')).canExtend, isTrue);
      // Sunucu (ExtendMyAdCommandHandler) bu ikisinde 400 veriyor →
      // butonun hiç açılmaması lazım ("işlevsiz buton yok").
      expect(MyAd.fromJson(myAdBody(status: 'pending')).canExtend, isFalse);
      expect(MyAd.fromJson(myAdBody(status: 'rejected')).canExtend, isFalse);
    });

    test('hak dolunca uzatma kapanır ve kalan negatife düşmez', () {
      final ad = MyAd.fromJson(
        myAdBody(extensionCount: 5, maxExtensions: 3),
      );
      expect(ad.remainingExtensions, 0);
      expect(ad.canExtend, isFalse);
    });

    test('süresi dolmuş ilanda kalan gün 0, "0 gün kaldı" yazılmaz', () {
      final ad = MyAd.fromJson(
        myAdBody(
          status: 'expired',
          expiresAt: DateTime.now().toUtc().subtract(const Duration(days: 2)),
        ),
      );
      expect(ad.daysUntilExpiry, 0);
      expect(ad.isExpiringSoon, isFalse, reason: 'expired zaten ayrı uyarı');
    });

    test('bir haftadan az kalan yayındaki ilan "yakında bitiyor" sayılır', () {
      final ad = MyAd.fromJson(
        myAdBody(
          expiresAt: DateTime.now().toUtc().add(const Duration(days: 3)),
        ),
      );
      expect(ad.isExpiringSoon, isTrue);
      expect(ad.daysUntilExpiry, 3);
    });
  });

  group('AdStatus', () {
    test('metin karşılıkları büyük/küçük harften bağımsız çözülür', () {
      expect(AdStatus.parse('APPROVED'), AdStatus.approved);
      expect(AdStatus.parse(' pending '), AdStatus.pending);
      expect(AdStatus.parse(null), AdStatus.unknown);
    });

    test('filtre şeridi dört gerçek statüyü taşır', () {
      expect(AdStatus.filterable, hasLength(4));
      expect(AdStatus.filterable.contains(AdStatus.unknown), isFalse);
    });
  });

  group('CategoryProperty', () {
    Map<String, dynamic> propertyBody({
      String type = 'Select',
      bool required = false,
      List<Map<String, dynamic>> options = const [],
    }) => {
      'id': 'prop-1',
      'propertyName': 'Yakıt',
      'propertyType': type,
      'isRequired': required,
      'defaultValue': null,
      'displayOrder': 1,
      'options': options,
    };

    test('sunucu enum metnini tipe çevirir, bilinmeyen tip metne düşer', () {
      expect(
        CategoryProperty.fromJson(propertyBody(type: 'Number')).kind,
        AdPropertyKind.number,
      );
      expect(
        CategoryProperty.fromJson(propertyBody(type: 'MultiSelect')).kind,
        AdPropertyKind.multiSelect,
      );
      expect(
        CategoryProperty.fromJson(propertyBody(type: 'Renk')).kind,
        AdPropertyKind.text,
        reason: 'backend yeni tip eklerse form patlamamalı',
      );
    });

    test('seçeneksiz select alanı çizilmez (kullanıcı hiçbir şey seçemez)', () {
      final property = CategoryProperty.fromJson(propertyBody());
      expect(property.isUsable, isFalse);
    });

    test('seçenekler displayOrder ile sıralanır', () {
      final property = CategoryProperty.fromJson(
        propertyBody(
          options: [
            {'id': 'o2', 'optionValue': 'Dizel', 'displayOrder': 2},
            {'id': 'o1', 'optionValue': 'Benzin', 'displayOrder': 1},
          ],
        ),
      );
      expect(property.isUsable, isTrue);
      expect(
        property.sortedOptions.map((option) => option.optionValue),
        ['Benzin', 'Dizel'],
      );
    });
  });

  test('AdExtendResult sunucu yanıtını okur', () {
    final result = AdExtendResult.fromJson({
      'adId': 'ad-1',
      'status': 'approved',
      'expiresAt': '2026-09-30T09:00:00.0000000Z',
      'extensionCount': 2,
      'maxExtensions': 3,
      'remainingExtensions': 1,
    });

    expect(result.adId, 'ad-1');
    expect(result.remainingExtensions, 1);
    expect(result.expiresAt.month, 9);
  });
}
