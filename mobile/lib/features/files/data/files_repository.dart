import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/uploaded_file.dart';

/// Görsel yükleme (`POST /v1/files/upload`) — API_CONTRACT §8.
///
/// Sözleşme: `multipart/form-data`, alan adı **`file`**, `[Authorize]`,
/// ≤10 MB, yalnız jpeg/png/webp (sunucu **magic-byte** doğrular; uzantıya
/// güvenmez). Sunucu `public-write` limitine tabi (15/dk/IP) → 429 gelirse
/// `ApiException.retryAfter` dolu gelir.
///
/// 11.5 profil fotoğrafı için yazıldı; 11.9 (ilan görselleri) ve 11.11 (vefat
/// fotoğrafı) aynı repository'yi kullanacak — o yüzden `features/files/`
/// altında ortak duruyor.
class FilesRepository {
  FilesRepository(this._api);

  final ApiClient _api;

  /// Cihazdaki bir dosyayı yükler. [moduleType]/[moduleId] sunucuda dosyayı
  /// ilgili kayda etiketlemek için opsiyoneldir (profil fotoğrafında modül id
  /// yok — kayıt PATCH ile bağlanır).
  Future<UploadedFile> upload({
    required String filePath,
    String? moduleType,
    String? moduleId,
    CancelToken? cancelToken,
  }) async {
    final form = FormData.fromMap({
      'file': await MultipartFile.fromFile(filePath),
      'moduleType': ?moduleType,
      'moduleId': ?moduleId,
    });

    final data = await _api.post('/v1/files/upload', body: form, cancelToken: cancelToken);
    if (data is! Map) throw ApiException.unexpectedResponse(cause: data);
    return UploadedFile.fromJson(Map<String, dynamic>.from(data));
  }
}

final filesRepositoryProvider = Provider<FilesRepository>(
  (ref) => FilesRepository(ref.watch(apiClientProvider)),
);
