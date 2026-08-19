import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/preferences/app_preferences.dart';
import '../data/models/news_article.dart';
import '../data/saved_news_store.dart';

final savedNewsStoreProvider = Provider<SavedNewsStore>(
  (ref) => SavedNewsStore(ref.watch(sharedPreferencesProvider)),
);

/// "Kaydedilenler" listesi (plan dışı ek, 12.14).
///
/// Durum **senkron** kurulur: `SharedPreferences` zaten açılışta yüklenmiş
/// durumda (`main.dart` onu bekleyip override ediyor), yani yer imi ikonu
/// bir kare bile "yükleniyor" göstermez — dokunulan bir kalp ikonunun gecikmesi
/// kullanıcıya "çalışmadı" hissi verir.
class SavedNewsController extends Notifier<List<NewsArticle>> {
  @override
  List<NewsArticle> build() => ref.watch(savedNewsStoreProvider).read();

  bool contains(String id) => state.any((item) => item.id == id);

  /// Kaydeder ya da kaydı kaldırır; **yeni durumu** döndürür (ekran mesajı
  /// "Kaydedildi" mi "Kayıt kaldırıldı" mı diyeceğini buradan bilir).
  Future<bool> toggle(NewsArticle article) async {
    state = await ref.read(savedNewsStoreProvider).toggle(article);
    return contains(article.id);
  }

  Future<void> clear() async {
    state = await ref.read(savedNewsStoreProvider).clear();
  }
}

final savedNewsProvider =
    NotifierProvider<SavedNewsController, List<NewsArticle>>(
      SavedNewsController.new,
    );

/// Tek bir haberin kaydedilmiş olup olmadığı — ikonun `select`'i.
bool isNewsSaved(List<NewsArticle> saved, String id) =>
    saved.any((item) => item.id == id);
