import 'package:dio/dio.dart';

import 'api_error_codes.dart';
import 'error_messages.dart';

/// Ağ katmanının dışarı verdiği **tek** hata tipi.
///
/// Repository/provider katmanları `DioException` görmez: zarf açma ve hata
/// eşlemesi interceptor'larda yapılır, dışarı hep `ApiException` çıkar
/// (bkz. `EnvelopeInterceptor`).
class ApiException implements Exception {
  ApiException({
    required this.code,
    String? message,
    this.traceId,
    this.statusCode,
    this.retryAfter,
    this.cause,
    this.stackTrace,
  }) : message = ApiErrorMessages.resolve(code, serverMessage: message);

  /// API_CONTRACT §3 kodu ya da istemci kodu ([ApiErrorCodes]).
  final String code;

  /// Kullanıcıya gösterilebilir Türkçe mesaj.
  final String message;

  /// Destek/log eşlemesi için (`meta.traceId`).
  final String? traceId;

  /// HTTP durum kodu — bağlantı kurulamadıysa null.
  final int? statusCode;

  /// Yalnız 429'da: `Retry-After` header'ından okunan bekleme süresi.
  final Duration? retryAfter;

  final Object? cause;
  final StackTrace? stackTrace;

  bool get isUnauthorized => code == ApiErrorCodes.unauthorized;
  bool get isForbidden => code == ApiErrorCodes.forbidden;
  bool get isNotFound => code == ApiErrorCodes.notFound;
  bool get isConflict => code == ApiErrorCodes.conflict;
  bool get isValidation => code == ApiErrorCodes.validationError;
  bool get isRateLimited => code == ApiErrorCodes.rateLimited;
  bool get isCancelled => code == ApiErrorCodes.cancelled;

  /// Sunucuya hiç ulaşılamadı → "Tekrar dene" göstermek anlamlı.
  bool get isConnectionProblem =>
      code == ApiErrorCodes.networkError || code == ApiErrorCodes.timeout;

  /// Dio hatasını `ApiException`'a çevirir.
  ///
  /// Sunucu bir hata zarfı döndüyse (`{success:false, error:{code,message}}`)
  /// kod/mesaj/traceId oradan okunur; aksi halde `DioExceptionType`'a göre
  /// istemci kodu üretilir.
  factory ApiException.fromDio(DioException error) {
    final envelope = _envelopeOf(error.response?.data);
    if (envelope != null) {
      return ApiException(
        code: envelope.code,
        message: envelope.message,
        traceId: envelope.traceId,
        statusCode: error.response?.statusCode,
        retryAfter: _retryAfterOf(error.response),
        cause: error,
        stackTrace: error.stackTrace,
      );
    }

    final status = error.response?.statusCode;
    final code = switch (error.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout ||
      DioExceptionType.transformTimeout => ApiErrorCodes.timeout,
      DioExceptionType.connectionError => ApiErrorCodes.networkError,
      DioExceptionType.cancel => ApiErrorCodes.cancelled,
      DioExceptionType.badCertificate => ApiErrorCodes.networkError,
      DioExceptionType.badResponse => _codeForStatus(status),
      DioExceptionType.unknown =>
        error.error is FormatException ? ApiErrorCodes.unexpectedResponse : ApiErrorCodes.networkError,
    };

    return ApiException(
      code: code,
      traceId: _traceIdOf(error.response?.data),
      statusCode: status,
      retryAfter: _retryAfterOf(error.response),
      cause: error,
      stackTrace: error.stackTrace,
    );
  }

  /// HTTP 200 + `success:false` durumu (⚠️ announcements detay quirk'i,
  /// API_CONTRACT §3 istisnası) buradan geçer.
  factory ApiException.fromEnvelope(
    Map<String, dynamic> body, {
    int? statusCode,
    Duration? retryAfter,
  }) {
    final envelope = _envelopeOf(body);
    return ApiException(
      code: envelope?.code ?? ApiErrorCodes.unexpectedResponse,
      message: envelope?.message,
      traceId: envelope?.traceId ?? _traceIdOf(body),
      statusCode: statusCode,
      retryAfter: retryAfter,
    );
  }

  /// Zincirde zaten çevrilmişse onu döndürür, değilse çevirir — çağıran
  /// katmanların interceptor sırasına bağımlı kalmaması için.
  static ApiException of(DioException error) =>
      error.error is ApiException ? error.error! as ApiException : ApiException.fromDio(error);

  /// Zarf açıldı ama gövde beklenen tipte değil (ör. liste beklenirken nesne).
  factory ApiException.unexpectedResponse({String? traceId, Object? cause}) =>
      ApiException(
        code: ApiErrorCodes.unexpectedResponse,
        traceId: traceId,
        cause: cause,
      );

  /// `DioException` sarmalayıcısı — interceptor zincirinde taşınır.
  DioException toDio(RequestOptions options, {Response<dynamic>? response}) =>
      DioException(
        requestOptions: options,
        response: response,
        type: DioExceptionType.badResponse,
        error: this,
        message: message,
      );

  static _ErrorEnvelope? _envelopeOf(Object? data) {
    if (data is! Map) return null;
    final error = data['error'];
    if (error is! Map) return null;
    final code = error['code'];
    if (code is! String || code.isEmpty) return null;
    return _ErrorEnvelope(
      code: code,
      message: error['message'] is String ? error['message'] as String : null,
      traceId: _traceIdOf(data),
    );
  }

  static String? _traceIdOf(Object? data) {
    if (data is! Map) return null;
    final meta = data['meta'];
    if (meta is! Map) return null;
    final traceId = meta['traceId'];
    return traceId is String ? traceId : null;
  }

  static Duration? _retryAfterOf(Response<dynamic>? response) {
    final raw = response?.headers.value('retry-after');
    final seconds = raw == null ? null : int.tryParse(raw.trim());
    return seconds == null ? null : Duration(seconds: seconds);
  }

  static String _codeForStatus(int? status) => switch (status) {
    400 => ApiErrorCodes.validationError,
    401 => ApiErrorCodes.unauthorized,
    403 => ApiErrorCodes.forbidden,
    404 => ApiErrorCodes.notFound,
    409 => ApiErrorCodes.conflict,
    429 => ApiErrorCodes.rateLimited,
    _ => ApiErrorCodes.internalError,
  };

  @override
  String toString() =>
      'ApiException($code${statusCode == null ? '' : ' / HTTP $statusCode'}): '
      '$message${traceId == null ? '' : ' [trace: $traceId]'}';
}

class _ErrorEnvelope {
  const _ErrorEnvelope({required this.code, this.message, this.traceId});

  final String code;
  final String? message;
  final String? traceId;
}
