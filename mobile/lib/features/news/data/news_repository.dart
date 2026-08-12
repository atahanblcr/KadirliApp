import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/news_article.dart';
import 'models/news_category.dart';

/// Haber uçları (API_CONTRACT "Haberler", Faz 12.12–12.13).
///
/// 🔑 **Mobil WordPress'e ASLA bağlanmaz.** Zincir tek yönlü:
/// `WordPress → (Hangfire senkron) → bizim Postgres → /v1/news → mobil`.
/// Kaynağa bağlansaydık override, kategori görünürlüğü, arama ve önbellek
/// imkânsız olurdu; üstelik uygulama **başka birinin çalışma süresine**
/// bağımlı olurdu.
///
/// Görünürlük süzgeci **sunucuda**: arşivlenmiş, kaynağı kalkmış (`gone`) ve
/// dışlanmış kategorili kayıtlar hiç gelmez — istemcinin ayrıca süzmesi gerekmez.
class NewsRepository {
  NewsRepository(this._api);

  final ApiClient _api;

  /// Sayfalı liste.
  ///
  /// ⚠️ Arama parametresi **`search`** (`searchTerm` değil — §7 madde 4:
  /// yanlış ad 400 vermez, **sessizce yok sayılır**).
  /// ⚠️ Sunucu `search` için **en az 2 karakter** ister; altındaki değer
  /// süzgeci hiç uygulamaz (400 değil). İstemci de bu yüzden tek harfte
  /// istek atmaz — bkz. `NewsFilter.effectiveSearch`.
  Future<PagedResult<NewsArticle>> list({
    int page = 1,
    int limit = 20,
    String? search,
    String? categoryId,
    bool? featured,
  }) => _api.getPaged(
    '/v1/news',
    NewsArticle.fromJson,
    page: page,
    limit: limit,
    query: {
      'search': ?_blankToNull(search),
      'categoryId': ?_blankToNull(categoryId),
      'featured': ?featured,
    },
  );

  /// Tek haber (detay + 12.15 push deep-link hedefi).
  ///
  /// Gizlenen/kaldırılan kayıt **404** döner — ekran bunu "yüklenemedi"den
  /// ayırıp "bu haber yayından kaldırılmış" der.
  Future<NewsArticle> detail(String id) =>
      _api.getObject('/v1/news/$id', NewsArticle.fromJson);

  /// Filtre şeridi için kategoriler (sayfasız lookup).
  Future<List<NewsCategory>> categories() =>
      _api.getList('/v1/news/categories', NewsCategory.fromJson);

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final newsRepositoryProvider = Provider<NewsRepository>(
  (ref) => NewsRepository(ref.watch(apiClientProvider)),
);
