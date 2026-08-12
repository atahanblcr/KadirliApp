import 'dart:convert';

import 'package:shared_preferences/shared_preferences.dart';

import 'models/news_article.dart';

/// "Kaydedilenler" listesinin **yerel** deposu (plan dışı ek, 12.14).
///
/// ## Neden sunucuda değil
/// Sunucuda tutmak `[Authorize]` demek olurdu; bu uygulamada **misafir gezinme
/// birinci sınıf** (11.3 kararı) ve "sonra okurum" en çok misafirin ihtiyacı.
/// Ayrıca yeni bir uç + tablo + izin + panel ekranı zinciri açardı. Yerel liste
/// tek cihaza bağlı kalır — bilinen ve kabul edilen sınır.
///
/// ## Neden kaydın **anlık görüntüsü** saklanıyor (yalnız kimliği değil)
/// Yalnız `id` saklansaydı liste her açılışta N istek atardı ve **kaynakta
/// yayından kalkan** (12.12'nin `gone` durumu) bir haber listede
/// *"bulunamadı"* satırına dönüşürdü: kullanıcı neyi kaydettiğini bile
/// göremezdi. Anlık görüntüyle başlık, özet ve kaynak adresi elde kalır;
/// detay 404 verse bile "Kaynakta oku" çalışmaya devam eder.
///
/// ⚠️ Gövde (`contentHtml`) **saklanmaz**: tek haber 11 KB'a kadar çıkıyor
/// (12.12 ölçümü) ve `SharedPreferences` bütün dosyayı belleğe alır.
/// ⚠️ Liste [maxItems] ile **tavanlı**: sınırsız büyüyen bir depo, sorunu
/// yıllar sonra fark edilen sınıftandır (`CODE_REVIEW_CHECKLIST` §11).
class SavedNewsStore {
  const SavedNewsStore(this._prefs);

  static const String prefsKey = 'news.saved';

  /// En yeni kayıt başta; tavanı aşınca **en eski** düşer.
  static const int maxItems = 100;

  final SharedPreferences _prefs;

  List<NewsArticle> read() {
    final raw = _prefs.getStringList(prefsKey);
    if (raw == null) return const [];

    final items = <NewsArticle>[];
    for (final entry in raw) {
      final article = _decode(entry);
      // Bozuk bir satır bütün listeyi düşürmemeli: kullanıcının 40 kaydı,
      // sürüm geçişinde bozulan tek bir JSON yüzünden kaybolmaz.
      if (article != null) items.add(article);
    }
    return items;
  }

  Future<List<NewsArticle>> toggle(NewsArticle article) async {
    final current = read();
    final exists = current.any((item) => item.id == article.id);
    final next = exists
        ? current.where((item) => item.id != article.id).toList()
        : [_snapshot(article), ...current.where((item) => item.id != article.id)]
              .take(maxItems)
              .toList();
    await _write(next);
    return next;
  }

  Future<List<NewsArticle>> clear() async {
    await _prefs.remove(prefsKey);
    return const [];
  }

  Future<void> _write(List<NewsArticle> items) => _prefs.setStringList(
    prefsKey,
    items.map((item) => jsonEncode(item.toJson())).toList(),
  );

  /// Depoya yazılan biçim — gövde bilinçli olarak düşürülür (yukarıda).
  static NewsArticle _snapshot(NewsArticle article) =>
      article.copyWith(contentHtml: null);

  static NewsArticle? _decode(String entry) {
    try {
      final decoded = jsonDecode(entry);
      if (decoded is! Map<String, dynamic>) return null;
      return NewsArticle.fromJson(decoded);
    } on FormatException {
      return null;
    } on TypeError {
      // Alan tipi değişmiş eski bir kayıt (sürüm geçişi) — sessizce atlanır.
      return null;
    }
  }
}
