import 'package:flutter/services.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/utils/phone.dart';

void main() {
  group('AppPhone.national', () {
    test('her yaygın biçimi 10 haneye indirir', () {
      expect(AppPhone.national('5321110001'), '5321110001');
      expect(AppPhone.national('532 111 00 01'), '5321110001');
      expect(AppPhone.national('0532 111 00 01'), '5321110001');
      expect(AppPhone.national('+90 532 111 00 01'), '5321110001');
      expect(AppPhone.national('905321110001'), '5321110001');
      expect(AppPhone.national('(532) 111-00-01'), '5321110001');
    });

    test('yazarken oluşan yarım girdileri bozmaz', () {
      expect(AppPhone.national('5'), '5');
      expect(AppPhone.national('532'), '532');
      expect(AppPhone.national('0'), ''); // sıfır önek olarak atılır
    });
  });

  group('AppPhone.isValid / toE164', () {
    test('sunucunun beklediği E.164 biçimini üretir', () {
      expect(AppPhone.toE164('0532 111 00 01'), '+905321110001');
      expect(AppPhone.toE164('532 111 00 01'), '+905321110001');
    });

    test('eksik hane ya da 5 ile başlamayan numara geçersiz', () {
      expect(AppPhone.isValid('532 111 00 0'), isFalse);
      expect(AppPhone.isValid('328 714 10 01'), isFalse); // sabit hat
      expect(AppPhone.toE164('532 111 00 0'), isNull);
    });
  });

  group('AppPhone gösterim', () {
    test('gruplama 3-3-2-2', () {
      expect(AppPhone.formatNational('5321110001'), '532 111 00 01');
      expect(AppPhone.formatNational('53211'), '532 11');
      expect(AppPhone.display('5321110001'), '+90 532 111 00 01');
      expect(AppPhone.display(''), '');
    });

    test('maskede yalnız son 2 hane görünür', () {
      expect(AppPhone.masked('5321110001'), '+90 532 ••• •• 01');
    });
  });

  group('PhoneInputFormatter', () {
    const formatter = PhoneInputFormatter();

    TextEditingValue type(String text) => formatter.formatEditUpdate(
      TextEditingValue.empty,
      TextEditingValue(text: text, selection: TextSelection.collapsed(offset: text.length)),
    );

    test('yazarken maske uygular ve imleci sonda tutar', () {
      final value = type('532111');
      expect(value.text, '532 111');
      expect(value.selection.baseOffset, '532 111'.length);
    });

    test('10 hane doluyken yeni hane yok sayılır', () {
      const full = TextEditingValue(
        text: '532 111 00 01',
        selection: TextSelection.collapsed(offset: 13),
      );

      final result = formatter.formatEditUpdate(
        full,
        const TextEditingValue(
          text: '532 111 00 015',
          selection: TextSelection.collapsed(offset: 14),
        ),
      );

      expect(result.text, '532 111 00 01');
      expect(AppPhone.digitsOf(result.text).length, 10);
    });

    test('yapıştırılan +90 / 0 önekli numara temizlenir', () {
      expect(type('+905321110001').text, '532 111 00 01');
      expect(type('05321110001').text, '532 111 00 01');
    });
  });
}
