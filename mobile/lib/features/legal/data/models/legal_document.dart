import 'package:freezed_annotation/freezed_annotation.dart';

part 'legal_document.freezed.dart';
part 'legal_document.g.dart';

/// `GET /v1/legal/documents` öğesi (`LegalDocumentDto`, Faz 12.16).
///
/// 🔑 **Rıza [versionId]'ye verilir, [id]'ye değil** (§7 madde 71). Sunucu
/// "o anki yayında sürüm"ü kendi başına seçmez; kullanıcının **okuduğu**
/// sürümün kimliği geri gönderilir. Bu yüzden istemci [versionId]'yi asla
/// kendi başına türetmez, tek kaynağı bu gövdedir.
///
/// ⚠️ [body] **listede de dolu gelir** (haberlerin aksine): kayıt akışında
/// ikinci bir ağ turu, kullanıcıyı onay kutusunun önünde bekletirdi.
@freezed
abstract class LegalDocument with _$LegalDocument {
  const factory LegalDocument({
    required String id,

    /// ⚠️ **Kontrat** — `kvkk_aydinlatma` · `acik_riza` · `kullanim_kosullari` ·
    /// `gizlilik_politikasi` · `ticari_ileti`. Tanınmayan tür sunucuda
    /// **varsayılana düşmez, 404 olur**; istemci de bu değerleri yalnız
    /// **taşır**, yorumlamaz.
    required String type,
    required String title,

    /// 🔴 Rızanın bağlanacağı kimlik.
    required String versionId,
    @Default(1) int versionNumber,

    /// Onay kutusunun yanındaki tek cümle (boş olabilir → başlık kullanılır).
    String? summary,

    /// Metnin kendisi (HTML).
    @Default('') String body,

    /// 🔴 `true` ise bu kutu işaretlenmeden kayıt tamamlanmaz.
    @Default(false) bool isMandatory,

    /// Kayıt ekranında sorulsun mu (ayarlar ekranı hepsini gösterir).
    @Default(false) bool showAtRegistration,
    @Default(0) int sortOrder,
    DateTime? effectiveFrom,

    /// Bu sürüm yeniden onay gerektiriyor mu (yeniden onay ekranı bunu okur).
    @Default(false) bool requiresReconsent,
  }) = _LegalDocument;

  const LegalDocument._();

  factory LegalDocument.fromJson(Map<String, dynamic> json) =>
      _$LegalDocumentFromJson(json);

  /// Onay kutusunun yanında görünecek metin.
  ///
  /// ⚠️ Özet boşsa **başlığa düşer**: boş bir onay satırı, kullanıcının neyi
  /// kabul ettiğini söylemeyen bir kutu demektir.
  String get consentLabel {
    final value = summary?.trim();
    return value == null || value.isEmpty ? title : value;
  }
}
