import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/complaint.dart';

/// Şikayet/istek uçları (API_CONTRACT §10).
class ComplaintsRepository {
  ComplaintsRepository(this._api);

  final ApiClient _api;

  /// `POST /v1/complaints` — **anonim de gönderebilir**; oturum varsa sunucu
  /// `user_id` claim'ini kendisi bağlar (istemci kullanıcı kimliği yollamaz).
  /// Yanıt oluşan kaydın kimliği.
  Future<String> create({
    required String subject,
    required String message,
    String? type,
    String? relatedModule,
    String? relatedId,
  }) async {
    final data = await _api.post(
      '/v1/complaints',
      body: {
        'subject': subject.trim(),
        'message': message.trim(),
        'type': type,
        'relatedModule': relatedModule,
        'relatedId': relatedId,
      },
    );
    if (data is String && data.isNotEmpty) return data;
    // Uç yalnız Guid döndürüyor; şekil değişirse sessizce boş id dönmesin.
    throw ApiException.unexpectedResponse(cause: data);
  }

  /// `GET /v1/complaints/my` `[A]` — yalnız oturum sahibinin kayıtları.
  Future<PagedResult<Complaint>> mine({int page = 1, int limit = 20}) =>
      _api.getPaged(
        '/v1/complaints/my',
        Complaint.fromJson,
        page: page,
        limit: limit,
      );
}

final complaintsRepositoryProvider = Provider<ComplaintsRepository>(
  (ref) => ComplaintsRepository(ref.watch(apiClientProvider)),
);
