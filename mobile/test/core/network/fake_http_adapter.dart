import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';

/// Testlerde gerçek ağ yerine geçen sahte HTTP katmanı.
///
/// `http_mock_adapter` gibi ek bir bağımlılık eklemeye gerek yok: Dio zaten
/// adaptörü değiştirilebilir yapıyor ve böylece tüm interceptor zinciri
/// (auth + zarf) gerçekte olduğu gibi koşuyor.
class FakeHttpAdapter implements HttpClientAdapter {
  FakeHttpAdapter(this.handler);

  final Future<ResponseBody> Function(RequestOptions options) handler;

  /// Gelen istekler — kaç kez çağrıldığı / hangi header'la gittiği denetimi.
  final List<RequestOptions> requests = [];

  int countOf(String path) => requests.where((r) => r.path == path).length;

  RequestOptions? lastOf(String path) =>
      requests.where((r) => r.path == path).isEmpty ? null : requests.lastWhere((r) => r.path == path);

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) {
    requests.add(options);
    return handler(options);
  }

  @override
  void close({bool force = false}) {}
}

/// JSON gövdeli sahte yanıt.
ResponseBody jsonResponse(Object body, {int statusCode = 200, Map<String, List<String>>? headers}) =>
    ResponseBody.fromString(
      jsonEncode(body),
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
        ...?headers,
      },
    );

/// `{success:true, data: ..., meta: {...}}` zarfı.
Map<String, dynamic> successEnvelope(Object? data, {String traceId = 'trace-ok'}) => {
  'success': true,
  'data': data,
  'meta': {
    'timestamp': '2026-07-29T08:00:00.0000000Z',
    'path': '/v1/test',
    'traceId': traceId,
  },
};

/// `{success:false, error:{code,message}, meta:{...}}` zarfı.
Map<String, dynamic> errorEnvelope(
  String code,
  String message, {
  String traceId = 'trace-err',
}) => {
  'success': false,
  'error': {'code': code, 'message': message},
  'meta': {
    'timestamp': '2026-07-29T08:00:00.0000000Z',
    'path': '/v1/test',
    'traceId': traceId,
  },
};
