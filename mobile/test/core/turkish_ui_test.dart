import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/network/network.dart';

/// Türkçe arayüz sözleşmesi — Faz 11.15.
///
/// **Değişmez kural 6 (CLAUDE.md):** arayüz Türkçe, kod ve kimlikler İngilizce;
/// kullanıcıya **teknik/İngilizce hata mesajı gösterilmez**. Bugüne kadar bu
/// kural yalnız gözden geçirmeyle korunuyordu. Aşağıdaki testler onu kaynak
/// kodu tarayarak mekanikleştiriyor — yeni bir hata kodu ya da sızan bir
/// İngilizce mesaj **kendiliğinden** kırmızıya döner.
void main() {
  group('hata sözlüğü eksiksiz', () {
    test('ApiErrorCodes içindeki HER kodun Türkçe karşılığı var', () {
      // ⚠️ Elle liste tutulmuyor: kodlar kaynaktan okunuyor, böylece yeni bir
      // sabit eklendiğinde test onu kendiliğinden kapsar (liste çürümez).
      final source = File('lib/core/network/api_error_codes.dart').readAsStringSync();
      final codes = RegExp(r"static const \w+ = '([A-Z_]+)';")
          .allMatches(source)
          .map((m) => m.group(1)!)
          .toList();

      expect(codes, isNotEmpty, reason: 'Kodlar okunamadıysa test hiçbir şey denetlemez');

      final missing = codes.where((code) => ApiErrorMessages.forCode(code) == null).toList();
      expect(
        missing,
        isEmpty,
        reason: 'Şu kodların Türkçe mesajı yok: ${missing.join(", ")}',
      );
    });

    test('sözlükteki hiçbir mesaj İngilizce/teknik değil', () {
      final source = File('lib/core/network/api_error_codes.dart').readAsStringSync();
      final codes = RegExp(r"static const \w+ = '([A-Z_]+)';")
          .allMatches(source)
          .map((m) => m.group(1)!);

      for (final code in codes) {
        final message = ApiErrorMessages.forCode(code)!;
        // Türkçe bir cümle noktalama ile biter ve kod adını içermez.
        expect(message, isNot(contains(code)), reason: '$code: ham kod kullanıcıya sızıyor');
        expect(
          message,
          matches(RegExp(r'[.!?]$')),
          reason: '$code: mesaj tam cümle olmalı ("$message")',
        );
      }
    });

    test('her mesaj Türkçe karakter/sözcük taşıyor (kopyala-yapıştır İngilizce değil)', () {
      const englishGiveaways = [
        'error',
        'failed',
        'not found',
        'unauthorized',
        'forbidden',
        'invalid',
        'timeout',
        'request',
        'server error',
      ];

      final source = File('lib/core/network/api_error_codes.dart').readAsStringSync();
      for (final match in RegExp(r"static const \w+ = '([A-Z_]+)';").allMatches(source)) {
        final message = ApiErrorMessages.forCode(match.group(1)!)!.toLowerCase();
        for (final word in englishGiveaways) {
          expect(message, isNot(contains(word)), reason: '"$message" İngilizce sızıntısı içeriyor');
        }
      }
    });
  });

  group('sunucunun teknik mesajı kullanıcıya gösterilmez', () {
    test('genel NotFoundException kalıbı elenir, Türkçe mesaj kullanılır', () {
      // 📌 Backend `NotFoundException` İngilizce üretiyor (11.2'de canlıda
      // yakalandı). Kontrat dondurulmuş olduğu için filtre istemcide.
      final exception = ApiException(
        code: ApiErrorCodes.notFound,
        message: 'Entity "Ad" (3f2504e0-4f89-11d3-9a0c-0305e82c3301) was not found.',
      );

      expect(exception.message, 'Aradığınız kayıt bulunamadı.');
    });

    test('handlerların yazdığı ÖZEL Türkçe mesaj korunur', () {
      // Filtre fazla geniş olmamalı: sunucunun spesifik mesajı sözlükten iyidir.
      final exception = ApiException(
        code: ApiErrorCodes.notFound,
        message: 'Duyuru bulunamadı.',
      );

      expect(exception.message, 'Duyuru bulunamadı.');
    });

    test('boş sunucu mesajı sözlüğe düşer', () {
      final exception = ApiException(code: ApiErrorCodes.rateLimited, message: '   ');

      expect(exception.message, 'Çok fazla deneme yaptınız. Lütfen biraz bekleyin.');
    });

    test('bilinmeyen kod genel mesaja düşer, kodu ekrana basmaz', () {
      final exception = ApiException(code: 'SOME_NEW_SERVER_CODE');

      expect(exception.message, 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.');
      expect(exception.message, isNot(contains('SOME_NEW_SERVER_CODE')));
    });
  });
}
