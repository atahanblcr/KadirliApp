import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import 'models/legal_document.dart';
import 'models/legal_version.dart';
import 'models/my_consent.dart';

/// Hukuki metin / rıza uçlarının tek sarmalayıcısı (API_CONTRACT "KVKK").
///
/// 🔴 **Belge uçları anonim** (`skipAuth`): bu ucu çağıran henüz kayıtlı
/// **değildir** — rızayı vermeden önce metni okuması gerekiyor. Rıza uçları
/// (`/v1/users/me/consents`) ise oturum ister.
class LegalRepository {
  LegalRepository(this._api);

  final ApiClient _api;

  /// Yayında olan belgeler, **metinleriyle birlikte**.
  ///
  /// [registrationOnly] `true` ise yalnız kayıt ekranında sorulacaklar gelir.
  Future<List<LegalDocument>> documents({bool registrationOnly = false}) =>
      _api.getList(
        '/v1/legal/documents',
        LegalDocument.fromJson,
        query: registrationOnly ? {'registrationOnly': true} : null,
      );

  /// Tek belge — ayarlardan bir metne dokunulduğunda.
  ///
  /// ⚠️ Tanınmayan tür sunucuda **404**'tür (varsayılana düşmez): yanlış hukuki
  /// metni göstermek, kullanıcıya okumadığı bir belgeyi onaylatmanın en sessiz
  /// yoludur.
  Future<LegalDocument> documentByType(String type) =>
      _api.getObject('/v1/legal/documents/$type', LegalDocument.fromJson);

  /// **Belirli bir sürümün** metni (12.17 eki) — "ben neyi onaylamıştım?".
  ///
  /// ⚠️ Taslak sürüm **404**; yürürlükten kalkmış sürüm **döner**
  /// (`isLive: false` ile).
  Future<LegalVersion> version(String versionId) =>
      _api.getObject('/v1/legal/versions/$versionId', LegalVersion.fromJson);

  /// Oturum sahibinin rıza durumu — yayında olan **her** belge için bir satır.
  Future<List<MyConsent>> myConsents() =>
      _api.getList('/v1/users/me/consents', MyConsent.fromJson);

  /// İsteğe bağlı rızayı ver/geri al ve yeniden onay akışını tamamla.
  ///
  /// 🔴 Zorunlu rıza buradan **geri alınamaz** — sunucu `MANDATORY_CONSENT`
  /// döner ve karşılığın hesap silme olduğunu söyler.
  ///
  /// ⚠️ Kaynak (`registration`/`settings`/`reconsent`) **sunucuda** sabitlenir;
  /// istemci yalnız "yeniden onay akışından geliyorum" diyebilir.
  Future<void> saveConsents(
    List<ConsentDecision> decisions, {
    bool isReconsent = false,
  }) => _api.post(
    '/v1/users/me/consents',
    body: {
      'consents': decisions.map((d) => d.toJson()).toList(),
      'isReconsent': isReconsent,
    },
  );
}

/// Kullanıcının **gördüğü sürüme** verdiği tek karar (`ConsentDecisionDto`).
///
/// ⚠️ `granted: false` de **gönderilir ve kaydedilir**: "sormadık" ile
/// "sorduk, hayır dedi" KVKK'da farklı şeylerdir ve yalnız `true` yollanırsa
/// bu fark **hiçbir yerde durmaz**.
class ConsentDecision {
  const ConsentDecision({required this.versionId, required this.granted});

  final String versionId;
  final bool granted;

  Map<String, dynamic> toJson() => {'versionId': versionId, 'granted': granted};

  @override
  bool operator ==(Object other) =>
      other is ConsentDecision &&
      other.versionId == versionId &&
      other.granted == granted;

  @override
  int get hashCode => Object.hash(versionId, granted);

  @override
  String toString() => 'ConsentDecision($versionId, granted: $granted)';
}

final legalRepositoryProvider = Provider<LegalRepository>(
  (ref) => LegalRepository(ref.watch(apiClientProvider)),
);
