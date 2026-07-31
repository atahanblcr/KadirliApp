import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/features/announcements/data/models/announcement_type.dart';

/// Tür modeli — sunucu **panel için** üretilmiş sunum alanları gönderiyor
/// (FontAwesome sınıfı + hex renk); mobil bunları güvenle çevirebilmeli.
void main() {
  AnnouncementType type({String? icon, String? color}) => AnnouncementType(
    id: 'a',
    name: 'Elektrik Kesintisi',
    icon: icon,
    color: color,
  );

  group('accentColor', () {
    test('# ile ve # olmadan 6 haneli hex çözülür', () {
      expect(type(color: '#F59E0B').accentColor, const Color(0xFFF59E0B));
      expect(type(color: 'F59E0B').accentColor, const Color(0xFFF59E0B));
    });

    test('8 haneli hex alfa kanalıyla çözülür', () {
      expect(type(color: '#80FF0000').accentColor, const Color(0x80FF0000));
    });

    test('bozuk/boş renk null döner (ekran nötr renge düşer)', () {
      expect(type(color: null).accentColor, isNull);
      expect(type(color: '').accentColor, isNull);
      expect(type(color: 'mavi').accentColor, isNull);
      expect(type(color: '#12345').accentColor, isNull);
      expect(type(color: '#GGGGGG').accentColor, isNull);
    });
  });

  group('materialIcon', () {
    test('bilinen FontAwesome adları eşlenir', () {
      expect(type(icon: 'fa-bolt').materialIcon, Icons.bolt_rounded);
      expect(type(icon: 'fa-tint').materialIcon, Icons.water_drop_rounded);
      expect(
        type(icon: 'fa-landmark').materialIcon,
        Icons.account_balance_rounded,
      );
    });

    test('büyük harf/boşluk farkı yok sayılır', () {
      expect(type(icon: '  FA-BOLT ').materialIcon, Icons.bolt_rounded);
    });

    test('bilinmeyen ad nötr ikona düşer (ekran patlamaz)', () {
      expect(type(icon: 'fa-uzay-gemisi').materialIcon, Icons.label_rounded);
      expect(type(icon: null).materialIcon, Icons.label_rounded);
    });
  });

  test('JSON ayrıştırma: canlı uçtan gelen gövde', () {
    final parsed = AnnouncementType.fromJson(const {
      'id': '4db78365-18d0-4d10-b1ab-a64e11379e23',
      'name': 'Elektrik Kesintisi',
      'slug': 'elektrik-kesintisi',
      'icon': 'fa-bolt',
      'color': '#F59E0B',
      'displayOrder': 1,
    });

    expect(parsed.name, 'Elektrik Kesintisi');
    expect(parsed.slug, 'elektrik-kesintisi');
    expect(parsed.displayOrder, 1);
    expect(parsed.accentColor, const Color(0xFFF59E0B));
  });
}
