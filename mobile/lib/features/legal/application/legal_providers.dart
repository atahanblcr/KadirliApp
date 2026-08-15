import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../auth/application/auth_controller.dart';
import '../data/legal_repository.dart';
import '../data/models/legal_document.dart';
import '../data/models/legal_version.dart';
import '../data/models/my_consent.dart';

/// **Kayıt ekranının** soracağı belgeler (`?registrationOnly=true`).
///
/// 🔴 **Hata durumunda kayıt AÇILMAZ** ve bu, projedeki varsayılan yönün
/// (§5 *"şüphede kalınca göster"*) **bilinçli tersi**: metni gösteremiyorken
/// rıza almak, rıza almamaktır. Bu yüzden burada "boş listeye düş" gibi bir
/// zarif düşüş **yok** — hata çağırana ulaşır, ekran onu söyler.
///
/// ⚠️ `autoDispose`: kayıt akışı bittikten sonra bellekte kalmasının anlamı yok
/// ve yeniden girildiğinde metin **taze** çekilmeli (yönetici arada yeni sürüm
/// yayınlamış olabilir — §7 madde 71).
final registrationLegalDocumentsProvider =
    FutureProvider.autoDispose<List<LegalDocument>>(
      (ref) => ref.watch(legalRepositoryProvider).documents(registrationOnly: true),
      retry: apiRetry,
    );

/// Ayarlar → "Yasal metinler": yayında olan **her** belge.
final legalDocumentsProvider = FutureProvider.autoDispose<List<LegalDocument>>(
  (ref) => ref.watch(legalRepositoryProvider).documents(),
  retry: apiRetry,
);

/// Tek belge (`/yasal/:type`).
final legalDocumentProvider =
    FutureProvider.autoDispose.family<LegalDocument, String>(
      (ref, type) => ref.watch(legalRepositoryProvider).documentByType(type),
      retry: apiRetry,
    );

/// **Onaylanan sürümün metni** (`/yasal/surum/:id`, 12.17 eki).
final legalVersionProvider =
    FutureProvider.autoDispose.family<LegalVersion, String>(
      (ref, versionId) => ref.watch(legalRepositoryProvider).version(versionId),
      retry: apiRetry,
    );

/// Oturum sahibinin rıza durumu.
///
/// ⚠️ `currentUserProvider`'ı **izliyor**: çıkış/giriş sonrası liste
/// kendiliğinden tazelenir. İzlemeseydi çıkış yapan kullanıcının rıza satırları
/// bir sonraki kullanıcının ekranında görünebilirdi.
final myConsentsProvider = FutureProvider.autoDispose<List<MyConsent>>((ref) {
  final user = ref.watch(currentUserProvider);
  if (user == null) return Future.value(const <MyConsent>[]);
  return ref.watch(legalRepositoryProvider).myConsents();
}, retry: apiRetry);

/// 🔑 **Yeniden onay gereken belgeler** — açılış kapısının tek ölçütü.
///
/// Ölçüt sunucuda türetiliyor (`needsReconsent`); istemci onu **yeniden
/// hesaplamaz** (§7 madde 43'ün "tek sahip" kuralı), yalnız süzer.
final pendingReconsentsProvider = Provider.autoDispose<List<MyConsent>>((ref) {
  final consents = ref.watch(myConsentsProvider).value;
  if (consents == null) return const [];
  return consents.where((consent) => consent.needsReconsent).toList();
});
