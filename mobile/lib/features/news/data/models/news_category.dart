import 'package:freezed_annotation/freezed_annotation.dart';

part 'news_category.freezed.dart';
part 'news_category.g.dart';

/// `GET /v1/news/categories` öğesi (`NewsCategoryResponseDto`).
///
/// Sözlük WordPress'ten geliyor ama **görünürlüğü panel belirliyor** (12.13):
/// uç yalnız *dışlanmamış* kategorileri döndürür, [showInFilterStrip] ise
/// "şeritte göster" tercihidir.
///
/// ⚠️ [articleCount] kaynağın sayısı **değil**, bizde görünen kayıt sayısıdır
/// (API_CONTRACT "Haberler" bölümü). Kaynakta 366 E-Gazete haberi olabilir ama
/// bizde 0 tanesi görünüyorsa burada 0 yazar.
@freezed
abstract class NewsCategory with _$NewsCategory {
  const factory NewsCategory({
    required String id,
    @Default('') String name,
    @Default('') String slug,
    @Default(0) int articleCount,
    @Default(true) bool showInFilterStrip,
    @Default(0) int displayOrder,
  }) = _NewsCategory;

  const NewsCategory._();

  factory NewsCategory.fromJson(Map<String, dynamic> json) =>
      _$NewsCategoryFromJson(json);

  /// Şeritte gösterilecek etiket — sayı **parantez içinde** eklenmez.
  ///
  /// Sayıyı etikete yazmak sezgisel geliyor ama iki sebeple yapılmadı:
  /// dar telefonda chip'i şişiriyor ve sayı her senkronda değiştiği için
  /// golden'ı sebepsiz kırardı.
  String get label => name.trim().isEmpty ? 'Kategori' : name.trim();

  /// Bu kategoride **bizde** görünen haber var mı.
  ///
  /// ⚠️ Şeritten **elenmez** — yalnız bilgi. Sunucu kategoriyi döndürüyorsa
  /// (dışlanmamışsa) istemci onu gizlemez: sayı bir anlık görüntüdür ve
  /// senkron bir dakika sonra kayıt getirebilir. "Şüphede kalınca göster"
  /// (§7 madde 49'un sınıfı).
  bool get hasArticles => articleCount > 0;
}
