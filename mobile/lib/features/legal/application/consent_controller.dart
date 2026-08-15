import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../data/legal_repository.dart';
import 'legal_providers.dart';

/// Rıza yazma işleminin geçici (UI) durumu.
///
/// **Rızaların kendisi burada tutulmaz** — tek kaynak sunucudur
/// (`myConsentsProvider`). Burada yalnız "hangi belge şu an yazılıyor" ve
/// "son hata" bilgisi var; bu, `NotificationPreferencesController`'ın
/// deseninin birebir aynısı.
class ConsentWriteState {
  const ConsentWriteState({this.pending = const {}, this.error});

  /// İsteği süren belge türleri (satır kilitlenir).
  final Set<String> pending;

  /// Son başarısız yazma denemesinin mesajı (gösterilince temizlenir).
  final String? error;

  bool isPending(String type) => pending.contains(type);

  ConsentWriteState copyWith({
    Set<String>? pending,
    String? error,
    bool clearError = false,
  }) => ConsentWriteState(
    pending: pending ?? this.pending,
    error: clearError ? null : (error ?? this.error),
  );
}

final consentControllerProvider =
    NotifierProvider<ConsentController, ConsentWriteState>(
      ConsentController.new,
    );

/// İsteğe bağlı rızayı ver/geri al ve yeniden onay akışını tamamla.
///
/// 🔴 **İyimser güncelleme YOK** (bildirim anahtarlarının bilinçli tersi).
/// Orada bedel bir bildirimin gelmemesiydi; burada ekranda "onaylandı" yazıp
/// sunucuda yazılmamış bir rıza, **var olmayan bir kanıt** demektir — ve
/// kullanıcı onayladığını sanır. Bu yüzden satır isteği bekler, sonucu
/// sunucudan gelen liste belirler.
class ConsentController extends Notifier<ConsentWriteState> {
  @override
  ConsentWriteState build() => const ConsentWriteState();

  /// Tek bir belge için kararı yazar.
  ///
  /// [type] yalnız satırı kilitlemek için; sunucuya giden şey **sürüm
  /// kimliğidir** (§7 madde 71 — rıza belgeye değil, kullanıcının gördüğü
  /// sürüme verilir).
  Future<bool> decide({
    required String type,
    required String versionId,
    required bool granted,
  }) => _write(
    lockKeys: {type},
    decisions: [ConsentDecision(versionId: versionId, granted: granted)],
    isReconsent: false,
  );

  /// Yeniden onay akışı — birden çok belge tek istekte gider.
  ///
  /// ⚠️ `isReconsent` bir **istemci beyanıdır**; sunucu onu `ConsentSources`
  /// değerine çevirir (serbest metin kabul edilseydi defterdeki "nasıl alındı"
  /// sütunu istemcinin yazdığı her şeyi taşırdı).
  Future<bool> submitReconsent(List<ConsentDecision> decisions) => _write(
    lockKeys: decisions.map((d) => d.versionId).toSet(),
    decisions: decisions,
    isReconsent: true,
  );

  void clearError() => state = state.copyWith(clearError: true);

  Future<bool> _write({
    required Set<String> lockKeys,
    required List<ConsentDecision> decisions,
    required bool isReconsent,
  }) async {
    if (decisions.isEmpty) return true;
    if (lockKeys.any(state.isPending)) return false;

    state = state.copyWith(
      pending: {...state.pending, ...lockKeys},
      clearError: true,
    );

    try {
      await ref
          .read(legalRepositoryProvider)
          .saveConsents(decisions, isReconsent: isReconsent);
      // Sunucudaki hâl tek kaynak: yazma başarılıysa listeyi **yeniden**
      // okuyoruz. Yerel olarak güncellemek, `needsReconsent` gibi sunucuda
      // türetilen alanları istemcide yeniden hesaplamak olurdu.
      ref.invalidate(myConsentsProvider);
      state = state.copyWith(pending: _without(lockKeys));
      return true;
    } on ApiException catch (error) {
      state = state.copyWith(
        pending: _without(lockKeys),
        error: error.message,
      );
      return false;
    }
  }

  Set<String> _without(Set<String> keys) =>
      {...state.pending}..removeAll(keys);
}
