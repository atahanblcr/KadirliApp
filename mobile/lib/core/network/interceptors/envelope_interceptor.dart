import 'package:dio/dio.dart';

import '../api_exception.dart';
import '../models/api_meta.dart';

/// `{success, data, meta}` zarfını açar (API_CONTRACT §2).
///
/// - `success:true` → `response.data` yerine **zarfın `data`'sı** konur;
///   `meta` [metaExtraKey] altında `response.extra`'da taşınır (traceId lazım
///   olduğunda okunur).
/// - `success:false` → HTTP 200 olsa bile [ApiException] fırlatılır.
///   ⚠️ `GET /v1/announcements/{id}` bulunamayınca tam olarak böyle davranır
///   (200 + `success:false` + `NOT_FOUND`) — bu quirk burada normalleşir,
///   üst katmanlar gerçek 404'ten ayırt etmek zorunda kalmaz.
/// - Zarfsız yanıtlar (`/health` gibi) olduğu gibi geçer.
///
/// Hata yolunda da tüm `DioException`'lar [ApiException]'a çevrilir, böylece
/// ağ katmanının dışına tek bir hata tipi çıkar.
class EnvelopeInterceptor extends Interceptor {
  static const metaExtraKey = 'api.meta';

  @override
  void onResponse(Response<dynamic> response, ResponseInterceptorHandler handler) {
    final body = response.data;
    if (body is! Map || !body.containsKey('success')) {
      handler.next(response); // zarfsız uç (ör. /health)
      return;
    }

    final map = Map<String, dynamic>.from(body);
    if (map['success'] != true) {
      handler.reject(
        ApiException.fromEnvelope(map, statusCode: response.statusCode)
            .toDio(response.requestOptions, response: response),
        true,
      );
      return;
    }

    response.extra = {...response.extra, metaExtraKey: _parseMeta(map['meta'])};
    response.data = map['data'];
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (err.error is ApiException) {
      handler.next(err); // zaten çevrilmiş (ör. yukarıdaki reject)
      return;
    }
    handler.next(
      ApiException.fromDio(err).toDio(err.requestOptions, response: err.response),
    );
  }

  static ApiMeta? _parseMeta(Object? raw) {
    if (raw is! Map) return null;
    try {
      return ApiMeta.fromJson(Map<String, dynamic>.from(raw));
    } catch (_) {
      return null; // meta ayrıştırılamazsa istek yine de başarılıdır
    }
  }
}
