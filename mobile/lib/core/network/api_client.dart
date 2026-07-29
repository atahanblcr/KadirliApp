import 'package:dio/dio.dart';

import 'api_exception.dart';
import 'interceptors/auth_interceptor.dart';
import 'interceptors/envelope_interceptor.dart';
import 'models/api_meta.dart';
import 'models/paged_result.dart';

/// Repository'lerin kullandığı ince API sarmalayıcısı.
///
/// Zarf açma ve hata çevirisi interceptor'larda olduğu için buradaki metotlar
/// **doğrudan `data` gövdesini** döndürür ve hata olarak yalnız [ApiException]
/// fırlatır. Tip dönüşümü çağırana bırakılmaz: beklenen şekil gelmezse
/// `UNEXPECTED_RESPONSE` hatası üretilir (sessiz null yerine görünür hata).
class ApiClient {
  ApiClient(this._dio);

  final Dio _dio;

  Dio get dio => _dio;

  // --- Ham (şekil dönüşümü olmadan) ---

  Future<dynamic> get(
    String path, {
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
    bool skipAuth = false,
  }) => _send('GET', path, query: query, cancelToken: cancelToken, skipAuth: skipAuth);

  Future<dynamic> post(
    String path, {
    Object? body,
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
    bool skipAuth = false,
  }) => _send('POST', path, body: body, query: query, cancelToken: cancelToken, skipAuth: skipAuth);

  Future<dynamic> put(
    String path, {
    Object? body,
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
  }) => _send('PUT', path, body: body, query: query, cancelToken: cancelToken);

  Future<dynamic> patch(
    String path, {
    Object? body,
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
  }) => _send('PATCH', path, body: body, query: query, cancelToken: cancelToken);

  Future<dynamic> delete(
    String path, {
    Object? body,
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
  }) => _send('DELETE', path, body: body, query: query, cancelToken: cancelToken);

  // --- Tipli yardımcılar ---

  /// Tek nesne döndüren uçlar (`GET /v1/ads/{id}` gibi).
  Future<T> getObject<T>(
    String path,
    T Function(Map<String, dynamic> json) parse, {
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
  }) async {
    final data = await get(path, query: query, cancelToken: cancelToken);
    return _parseObject(data, parse);
  }

  /// Düz liste döndüren uçlar (`GET /v1/neighborhoods` gibi — sayfasız).
  Future<List<T>> getList<T>(
    String path,
    T Function(Map<String, dynamic> json) parse, {
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
  }) async {
    final data = await get(path, query: query, cancelToken: cancelToken);
    if (data is! List) throw ApiException.unexpectedResponse(cause: data);
    return data.map((item) => _parseObject(item, parse)).toList(growable: false);
  }

  /// Sayfalı uçlar (API_CONTRACT §5). `page`/`limit` verilmezse sunucu
  /// varsayılanları kullanılır; public uçlarda `limit` 50'ye kırpılır.
  Future<PagedResult<T>> getPaged<T>(
    String path,
    T Function(Map<String, dynamic> json) parse, {
    int? page,
    int? limit,
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
  }) async {
    final data = await get(
      path,
      query: {
        'page': ?page,
        'limit': ?limit,
        ...?query,
      },
      cancelToken: cancelToken,
    );
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return PagedResult<T>.fromJson(
      Map<String, dynamic>.from(data),
      (item) => _parseObject(item, parse),
    );
  }

  Future<dynamic> _send(
    String method,
    String path, {
    Object? body,
    Map<String, dynamic>? query,
    CancelToken? cancelToken,
    bool skipAuth = false,
  }) async {
    try {
      final response = await _dio.request<dynamic>(
        path,
        data: body,
        queryParameters: query,
        cancelToken: cancelToken,
        options: Options(
          method: method,
          extra: skipAuth ? {AuthInterceptor.skipAuthExtraKey: true} : null,
        ),
      );
      return response.data;
    } on DioException catch (error) {
      throw ApiException.of(error);
    }
  }

  static T _parseObject<T>(Object? data, T Function(Map<String, dynamic> json) parse) {
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    try {
      return parse(Map<String, dynamic>.from(data));
    } on ApiException {
      rethrow;
    } catch (error) {
      // Alan eksik/tip uyuşmuyor: kontrat ile model ayrışmış demektir.
      throw ApiException.unexpectedResponse(cause: error);
    }
  }
}

/// Yanıtın `meta` bloğu (traceId için) — nadiren gerekir, o yüzden ayrı.
extension ApiMetaAccess on Response<dynamic> {
  ApiMeta? get apiMeta {
    final meta = extra[EnvelopeInterceptor.metaExtraKey];
    return meta is ApiMeta ? meta : null;
  }
}
