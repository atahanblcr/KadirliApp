import '../data/legal_repository.dart';
import '../data/models/legal_document.dart';

/// Rıza kutularının **tek karar sahibi** (Faz 12.17): *"devam edebilir mi,
/// edemiyorsa neden?"*
///
/// Saf: `BuildContext` görmez, ağ görmez, `Widget` döndürmez. Kayıt ekranı,
/// yeniden onay ekranı ve ayarlar ekranı **aynı** sınıftan geçer — ayrı
/// yazılsalardı bu projenin en sık tekrarlayan hasar sınıfı doğardı
/// (§7 madde 23/38/65): bir ekran kutuyu zorunlu sayar, diğeri saymaz.
///
/// 🔴 **Hiçbir kutu ÖN İŞARETLİ başlamaz** ([ConsentSelection.initial] boş bir
/// küme ile başlar): ön işaretli kutu KVKK'da rıza sayılmaz. Bu, sınıfın
/// var olma sebeplerinden biri ve testle kilitli.
class ConsentSelection {
  const ConsentSelection({
    required this.documents,
    required this.grantedVersionIds,
  });

  /// 🔴 Ön işaretsiz başlangıç — kutuların hiçbiri seçili değil.
  factory ConsentSelection.initial(List<LegalDocument> documents) =>
      ConsentSelection(documents: documents, grantedVersionIds: const {});

  final List<LegalDocument> documents;
  final Set<String> grantedVersionIds;

  bool isGranted(LegalDocument document) =>
      grantedVersionIds.contains(document.versionId);

  ConsentSelection toggle(LegalDocument document, bool granted) {
    final next = Set<String>.from(grantedVersionIds);
    if (granted) {
      next.add(document.versionId);
    } else {
      next.remove(document.versionId);
    }
    return ConsentSelection(documents: documents, grantedVersionIds: next);
  }

  /// İşaretlenmemiş **ilk zorunlu** belge (yoksa `null`).
  ///
  /// ⚠️ Sıra `sortOrder`'a değil, **listenin geliş sırasına** güveniyor:
  /// sunucu zaten `sortOrder` + başlık sırasıyla döndürüyor ve ikinci bir
  /// sıralama yazmak, ekranda görünen sırayla mesajda söylenen belgeyi
  /// ayrıştırabilirdi.
  LegalDocument? get blockingDocument {
    for (final document in documents) {
      if (document.isMandatory && !isGranted(document)) return document;
    }
    return null;
  }

  bool get canSubmit => blockingDocument == null;

  /// 🔑 Buton kapalıysa **sebebini söyler** (§7 madde 42'nin kuralı).
  /// Kapalı ve sebepsiz bir buton, kullanıcının çözemeyeceği bir duvardır.
  String? get blockingReason {
    final document = blockingDocument;
    if (document == null) return null;
    return 'Devam etmek için "${document.title}" onayı gerekli.';
  }

  /// Sunucuya gidecek kararlar — **her belge için bir satır**.
  ///
  /// ⚠️ Yalnız işaretlenenler gönderilseydi *"sormadık"* ile *"sorduk, hayır
  /// dedi"* farkı hiçbir yerde durmazdı: isteğe bağlı bir izni bilerek
  /// reddeden kullanıcı, sunucu tarafında hiç sorulmamış görünürdü.
  List<ConsentDecision> get decisions => documents
      .map(
        (document) => ConsentDecision(
          versionId: document.versionId,
          granted: isGranted(document),
        ),
      )
      .toList(growable: false);
}
