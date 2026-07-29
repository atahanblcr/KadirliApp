import 'package:url_launcher/url_launcher.dart';

/// Dış uygulamalara yönlendirmeler: telefon, WhatsApp, harita, web.
///
/// Tümü `bool` döner — açılamazsa çağıran ekran kullanıcıya bilgi verir
/// (ör. WhatsApp kurulu değilse "WhatsApp açılamadı").
abstract final class AppLinks {
  static Future<bool> call(String phone) {
    final number = _digits(phone);
    if (number.isEmpty) return Future.value(false);
    return _open(Uri(scheme: 'tel', path: number));
  }

  /// WhatsApp sohbeti. Numara uluslararası biçime çevrilir (90…).
  static Future<bool> whatsapp(String phone, {String? message}) {
    final number = _internationalNumber(phone);
    if (number == null) return Future.value(false);
    return _open(
      Uri.https('wa.me', '/$number', {
        if (message != null && message.isNotEmpty) 'text': message,
      }),
    );
  }

  /// Konumu cihazın harita uygulamasında açar.
  ///
  /// `geo:` şeması Android'de doğal; iOS'ta karşılığı olmadığından Apple
  /// Haritalar/Google Haritalar web bağlantısına düşülür.
  static Future<bool> map({
    required double latitude,
    required double longitude,
    String? label,
  }) async {
    final coordinates = '$latitude,$longitude';
    final geoUri = Uri.parse(
      'geo:$coordinates?q=$coordinates${label == null ? '' : '(${Uri.encodeComponent(label)})'}',
    );
    if (await canLaunchUrl(geoUri)) return _open(geoUri);

    return _open(Uri.https('www.google.com', '/maps/search/', {'api': '1', 'query': coordinates}));
  }

  /// Adres metniyle harita araması (koordinat yoksa).
  static Future<bool> mapSearch(String query) {
    if (query.trim().isEmpty) return Future.value(false);
    return _open(Uri.https('www.google.com', '/maps/search/', {'api': '1', 'query': query}));
  }

  static Future<bool> web(String url) {
    final uri = Uri.tryParse(url.startsWith('http') ? url : 'https://$url');
    if (uri == null) return Future.value(false);
    return _open(uri);
  }

  static Future<bool> email(String address, {String? subject}) => _open(
    Uri(
      scheme: 'mailto',
      path: address,
      query: subject == null ? null : 'subject=${Uri.encodeComponent(subject)}',
    ),
  );

  static Future<bool> _open(Uri uri) async {
    try {
      return await launchUrl(uri, mode: LaunchMode.externalApplication);
    } catch (_) {
      return false; // uygulama yoksa / platform reddederse sessizce false
    }
  }

  static String _digits(String value) => value.replaceAll(RegExp(r'[^\d+]'), '');

  /// "0532 111 22 33" → "905321112233"; "+90532…" → "90532…".
  static String? _internationalNumber(String phone) {
    var digits = _digits(phone).replaceAll('+', '');
    if (digits.isEmpty) return null;
    if (digits.startsWith('90') && digits.length == 12) return digits;
    if (digits.startsWith('0')) digits = digits.substring(1);
    if (digits.length == 10) return '90$digits';
    return digits;
  }
}
