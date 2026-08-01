import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/campaign.dart';
import 'models/campaign_code.dart';

/// Kampanya uçları (API_CONTRACT §10).
///
/// Public uç yalnız onaylı + tarihi geçerli kampanyaları döndürür; kategori
/// filtresi (`?categoryId=`) **işletme kategorisine** bakıyor ama public bir
/// işletme-kategori lookup ucu olmadığı için mobilde yalnız arama kullanılıyor.
class CampaignsRepository {
  CampaignsRepository(this._api);

  final ApiClient _api;

  /// Sayfalı liste — `search` kampanya başlığı **ve işletme adında** arar.
  Future<PagedResult<Campaign>> list({
    int page = 1,
    int limit = 20,
    String? search,
  }) => _api.getPaged(
    '/v1/campaigns',
    Campaign.fromJson,
    page: page,
    limit: limit,
    query: {'search': ?_blankToNull(search)},
  );

  Future<Campaign> detail(String id) =>
      _api.getObject('/v1/campaigns/$id', Campaign.fromJson);

  /// İndirim kodunu açar ve `campaign_code_views`'a iz düşer (oturum zorunlu).
  ///
  /// Kodu olmayan kampanyada sunucu **400 VALIDATION_ERROR** döner → çağıran
  /// [ApiException] mesajını gösterir.
  Future<CampaignCode> viewCode(String id) async {
    final data = await _api.post('/v1/campaigns/$id/view-code');
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return CampaignCode.fromJson(Map<String, dynamic>.from(data));
  }

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final campaignsRepositoryProvider = Provider<CampaignsRepository>(
  (ref) => CampaignsRepository(ref.watch(apiClientProvider)),
);
