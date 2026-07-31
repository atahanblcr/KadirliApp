import 'package:freezed_annotation/freezed_annotation.dart';

part 'category_property.freezed.dart';
part 'category_property.g.dart';

/// Kategoriye özel form alanının **tipi** (sunucu `PropertyType` enum'u,
/// `ToString()` ile metin geliyor).
///
/// İlan verme formu bu tipe göre widget üretir; bilinmeyen bir tip gelirse
/// (backend yeni tip eklerse) form patlamaz, **metin alanına** düşer.
enum AdPropertyKind {
  text,
  number,
  boolean,
  select,
  multiSelect;

  static AdPropertyKind parse(String? raw) => switch (raw?.toLowerCase()) {
    'number' => AdPropertyKind.number,
    'boolean' => AdPropertyKind.boolean,
    'select' => AdPropertyKind.select,
    'multiselect' => AdPropertyKind.multiSelect,
    _ => AdPropertyKind.text,
  };
}

/// `GET /v1/ads/categories/{id}/properties` satırı (`CategoryPropertyDto`).
///
/// Kategori seçildikten sonra çekilir ve **ilan verme formunun dinamik
/// bölümünü** üretir. Sunucu zorunlu alanları `isRequired` ile bildiriyor ve
/// eksik gönderimde 400 veriyor (`AdSubmissionRules`), bu yüzden istemci aynı
/// denetimi önden yapar.
@freezed
abstract class CategoryProperty with _$CategoryProperty {
  const factory CategoryProperty({
    required String id,
    @Default('') String propertyName,
    @Default('Text') String propertyType,
    @Default(false) bool isRequired,
    String? defaultValue,
    @Default(0) int displayOrder,
    @Default(<PropertyOption>[]) List<PropertyOption> options,
  }) = _CategoryProperty;

  const CategoryProperty._();

  factory CategoryProperty.fromJson(Map<String, dynamic> json) =>
      _$CategoryPropertyFromJson(json);

  AdPropertyKind get kind => AdPropertyKind.parse(propertyType);

  /// Seçenek listesi boş bir `select` alanı çizilemez (kullanıcı hiçbir şey
  /// seçemez, zorunluysa formu kilitler) → böyle bir alan hiç gösterilmez.
  bool get isUsable =>
      switch (kind) {
        AdPropertyKind.select ||
        AdPropertyKind.multiSelect => options.isNotEmpty,
        _ => true,
      } &&
      propertyName.trim().isNotEmpty;

  List<PropertyOption> get sortedOptions =>
      [...options]..sort((a, b) => a.displayOrder.compareTo(b.displayOrder));
}

/// `select`/`multiSelect` alanının seçeneği (`PropertyOptionDto`).
///
/// ⚠️ Sunucuya **`optionValue` metni** gönderilir (seçenek id'si değil):
/// `AdSubmissionRules` gelen değeri `Options.Contains(value)` ile doğruluyor.
@freezed
abstract class PropertyOption with _$PropertyOption {
  const factory PropertyOption({
    required String id,
    @Default('') String optionValue,
    @Default(0) int displayOrder,
  }) = _PropertyOption;

  factory PropertyOption.fromJson(Map<String, dynamic> json) =>
      _$PropertyOptionFromJson(json);
}
