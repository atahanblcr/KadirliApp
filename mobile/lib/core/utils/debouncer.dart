import 'dart:async';

import 'package:flutter/foundation.dart';

/// Hızlı tekrarlanan tetikleyicileri (arama kutusuna yazmak) tek çağrıya
/// indirger.
///
/// **Neden 11.8'de gerekti:** ilan araması sunucuda **başlık + açıklama**
/// üzerinde `LIKE` ile koşuyor (pg_trgm indeksi yalnız başlıkta — backend
/// notu) ve her tuş vuruşunda listeyi sıfırdan yüklemek hem gereksiz sorgu
/// hem de kullanıcının gözünde titreyen bir liste demek.
class Debouncer {
  Debouncer({this.delay = const Duration(milliseconds: 350)});

  final Duration delay;
  Timer? _timer;

  bool get isPending => _timer?.isActive ?? false;

  /// [action]'ı [delay] kadar erteler; bu arada gelen yeni çağrı öncekini iptal eder.
  void run(VoidCallback action) {
    _timer?.cancel();
    _timer = Timer(delay, action);
  }

  /// Beklemeyi atlayıp hemen çalıştırır (klavyedeki "Ara" tuşu).
  void flush(VoidCallback action) {
    _timer?.cancel();
    _timer = null;
    action();
  }

  /// Bekleyen çağrıyı iptal eder — `dispose` içinde çağrılmazsa ekran
  /// kapandıktan sonra provider'a dokunulur (testlerde "pending timer").
  void dispose() {
    _timer?.cancel();
    _timer = null;
  }
}
