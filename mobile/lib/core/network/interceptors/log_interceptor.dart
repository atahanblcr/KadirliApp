import 'dart:developer' as developer;

import 'package:dio/dio.dart';

import '../api_exception.dart';

/// Kompakt istek günlüğü (yalnız dev + debug — `Env.enableNetworkLogs`).
///
/// Dio'nun kendi `LogInterceptor`'ı tüm gövdeyi basıyor; liste uçlarında
/// konsolu boğuyordu. Burada tek satır: yön, metot, yol, durum, süre.
class NetworkLogInterceptor extends Interceptor {
  static const _startedAtKey = 'log.startedAt';

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    options.extra[_startedAtKey] = DateTime.now();
    _log('→ ${options.method} ${options.uri}');
    handler.next(options);
  }

  @override
  void onResponse(Response<dynamic> response, ResponseInterceptorHandler handler) {
    _log(
      '← ${response.statusCode} ${response.requestOptions.method} '
      '${response.requestOptions.uri.path}${_elapsed(response.requestOptions)}',
    );
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final api = err.error is ApiException ? err.error as ApiException : null;
    _log(
      '✗ ${err.requestOptions.method} ${err.requestOptions.uri.path}'
      '${_elapsed(err.requestOptions)} — ${api ?? err.message ?? err.type.name}',
    );
    handler.next(err);
  }

  String _elapsed(RequestOptions options) {
    final startedAt = options.extra[_startedAtKey];
    if (startedAt is! DateTime) return '';
    return ' (${DateTime.now().difference(startedAt).inMilliseconds}ms)';
  }

  void _log(String message) => developer.log(message, name: 'api');
}
