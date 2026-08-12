import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';
import 'news_category.dart';

part 'news_article.freezed.dart';
part 'news_article.g.dart';

/// `GET /v1/news` ve `/v1/news/{id}` gövdesi (`NewsResponseDto`).
///
/// 🔑 **Alanlar "etkin" değerdir:** yönetici panelden bir override yazdıysa
/// (12.13) sunucu onu döndürür, yoksa kaynağınkini. İstemci `title` ile
/// `titleOverride`'ı **birleştirmez** — böyle bir alan çifti zaten gelmiyor;
/// birleştiren bir istemci yazılsaydı mağazadaki eski sürümler panel
/// düzeltmesini hiç görmezdi (API_CONTRACT "Haberler").
///
/// ⚠️ [contentHtml] **yalnız detayda dolu**, listede `null`: 27 bin kayıtlık bir
/// modülde sayfa başına 20 gövde taşımak hiç okunmayacak ~40 KB demek. Liste
/// kartı [excerpt] kullanır.
@freezed
abstract class NewsArticle with _$NewsArticle {
  const factory NewsArticle({
    required String id,
    @Default('') String title,
    @Default('') String excerpt,

    /// Temizlenmiş HTML gövde — **yalnız detayda** dolu.
    ///
    /// Temizlik alım anında sunucuda yapıldı (12.12, `NewsHtmlPolicy` beyaz
    /// listesi). İstemci **ikinci bir beyaz liste yazmaz**: iki sahipli bir
    /// güvenlik kuralı, ayrıştıkları anda hangisinin doğru olduğu
    /// bilinemeyen iki gerçeklik üretir.
    String? contentHtml,

    /// Aynalanmış kapak görseli — **göreli** (`/uploads/…`, §7 madde 9).
    String? imageUrl,

    /// Kaynak görselinin ölçüleri; yöneticinin koyduğu kapakta **null** gelir
    /// (boyutu istemci ölçer).
    int? imageWidth,
    int? imageHeight,

    /// Haberin gazetedeki adresi ("Kaynakta oku" + paylaşım metni).
    String? sourceUrl,
    DateTime? publishedAt,
    DateTime? modifiedAt,

    /// Sunucuda üretilen okuma süresi (200 kelime/dk, en az 1).
    ///
    /// ⚠️ İstemcide **hesaplanmaz**: liste ucu gövdeyi zaten taşımıyor, yani
    /// hesaplanabilseydi bile listede yanlış sonuç verirdi.
    @Default(1) int readingMinutes,
    @Default(false) bool isFeatured,
    @Default(<NewsCategory>[]) List<NewsCategory> categories,
  }) = _NewsArticle;

  const NewsArticle._();

  factory NewsArticle.fromJson(Map<String, dynamic> json) =>
      _$NewsArticleFromJson(json);

  /// Kartta gösterilen tek kategori adı — çoklu kategoride **ilki**.
  ///
  /// Kaynakta bir haber birden çok kategoride olabiliyor (`[49,51,52]` ölçüldü,
  /// 12.12 planı). Kartta hepsini basmak dar telefonda satırı şişirir; detayda
  /// tamamı gösteriliyor.
  String? get primaryCategory {
    for (final category in categories) {
      final name = category.name.trim();
      if (name.isNotEmpty) return name;
    }
    return null;
  }

  /// "5 dk okuma" — süre her zaman en az 1, yani rozet hep anlamlı.
  String get readingLabel => '${readingMinutes < 1 ? 1 : readingMinutes} dk okuma';

  /// Kartın zaman etiketi: "3 saat önce" / "12 Ağustos 2026".
  ///
  /// ⚠️ [now] enjekte edilebilir olmak **zorunda**: göreli tarih gösteren bir
  /// kart gerçek saate bakarsa golden referansı **her gün** kırılır ve insan
  /// `--update-goldens`'ı refleks hâline getirir (bu projede 4 kez yaşandı,
  /// `CODE_REVIEW_CHECKLIST` §5).
  String? publishedLabel({DateTime? now}) {
    final published = publishedAt;
    if (published == null) return null;
    return AppDate.relative(published, now: now);
  }

  /// Kaynak yayımladıktan **sonra** anlamlı biçimde güncellendi mi.
  ///
  /// Senkron her koşuda `modified`'ı tazeliyor ve saniyelik farklar oluşuyor
  /// (canlıda `publishedAt` 14:40:59 ↔ `modifiedAt` 14:41:00 görüldü) →
  /// eşik olmadan **her haber** "güncellendi" rozeti alırdı.
  bool get wasUpdated {
    final published = publishedAt;
    final modified = modifiedAt;
    if (published == null || modified == null) return false;
    return modified.difference(published) >= const Duration(minutes: 5);
  }

  /// Paylaşım metni — bağlantı **kaynağa** gider (uygulama henüz mağazada yok).
  String shareText() {
    final buffer = StringBuffer('📰 ${title.trim()}');
    final summary = excerpt.trim();
    if (summary.isNotEmpty) buffer.write('\n\n$summary');
    final url = sourceUrl?.trim();
    if (url != null && url.isNotEmpty) buffer.write('\n\n$url');
    buffer.write('\n\n— Kadirli uygulaması');
    return buffer.toString();
  }
}
