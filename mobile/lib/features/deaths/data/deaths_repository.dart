import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/utils/utils.dart';
import 'models/death_notice.dart';

/// Vefat ilanı uçları (API_CONTRACT §10).
///
/// Public liste **yalnız `approved`** kayıtları döndürür; istemcinin `?status=`
/// parametresi sunucuda yok sayılır. `POST /v1/deaths` `[A]` kaydı `pending`
/// yazar (moderasyona düşer) ve **yeni kaydın kimliğini** döndürür.
class DeathsRepository {
  DeathsRepository(this._api);

  final ApiClient _api;

  /// Sayfalı liste. [search] merhumun adında arar, [date] tek bir cenaze gününe
  /// süzer (sunucu `funeral_date` gününe bakar).
  Future<PagedResult<DeathNotice>> list({
    int page = 1,
    int limit = 20,
    String? search,
    DateTime? date,
  }) => _api.getPaged(
    '/v1/deaths',
    DeathNotice.fromJson,
    page: page,
    limit: limit,
    query: {
      'search': ?_blankToNull(search),
      'date': ?(date == null ? null : AppDate.isoDay(date)),
    },
  );

  Future<DeathNotice> detail(String id) =>
      _api.getObject('/v1/deaths/$id', DeathNotice.fromJson);

  /// Vefat bildirimi gönderir; dönen değer yeni kaydın kimliği.
  ///
  /// ⚠️ [funeralDate] **Türkiye günü, 00:00 UTC** olarak gönderilir (sunucunun
  /// `funeral_date` konvansiyonu); [funeralTime] `"HH:mm:00"` (sunucu tarafı
  /// `TimeSpan`).
  Future<String> create({
    required String deceasedName,
    required DateTime funeralDate,
    required String funeralTime,
    String? photoFileId,
    String? cemeteryId,
    String? mosqueId,
    String? neighborhoodId,
    String? condolenceAddress,
    double? condolenceLatitude,
    double? condolenceLongitude,
  }) async {
    final data = await _api.post(
      '/v1/deaths',
      body: {
        'deceasedName': deceasedName.trim(),
        'funeralDate': _utcDayIso(funeralDate),
        'funeralTime': funeralTime,
        'photoFileId': photoFileId,
        'cemeteryId': cemeteryId,
        'mosqueId': mosqueId,
        'neighborhoodId': neighborhoodId,
        'condolenceAddress': _blankToNull(condolenceAddress),
        'condolenceLatitude': condolenceLatitude,
        'condolenceLongitude': condolenceLongitude,
      },
    );
    if (data is String && data.isNotEmpty) return data;
    // Uç yalnız Guid döndürüyor; şekil değişirse sessizce boş id dönmesin.
    throw ApiException.unexpectedResponse(cause: data);
  }

  /// `2026-08-05` → `2026-08-05T00:00:00.000Z` (gün kaymadan).
  static String _utcDayIso(DateTime day) =>
      DateTime.utc(day.year, day.month, day.day).toIso8601String();

  static String? _blankToNull(String? value) {
    final trimmed = value?.trim();
    return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
  }
}

final deathsRepositoryProvider = Provider<DeathsRepository>(
  (ref) => DeathsRepository(ref.watch(apiClientProvider)),
);
