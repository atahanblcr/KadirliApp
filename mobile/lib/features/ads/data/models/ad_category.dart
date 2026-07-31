import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'ad_category.freezed.dart';
part 'ad_category.g.dart';

/// `GET /v1/ads/categories[?parentId=]` öğesi (`AdCategoryDto`).
///
/// Kategoriler **iki katmanlı bir ağaç**: parametresiz çağrı kök kategorileri,
/// `?parentId=` o kökün alt kategorilerini döndürür ([subCategoryCount] alt
/// kategori var mı sorusunu ek istek atmadan cevaplar).
///
/// ⚠️ **Sunucu filtresi TAM EŞLEŞMEDİR:** `?categoryId=<Araçlar>` yalnız
/// doğrudan "Araçlar"a yazılmış ilanları döndürür, "Otomobil"dekileri değil.
/// Bu yüzden ekran hangi kategorinin seçili olduğunu net göstermek zorunda
/// (bkz. `ads_screen.dart` iki katmanlı chip şeridi).
///
/// `icon` alanı panelden doldurulabiliyor ama seed veride boş → ikon
/// [materialIcon] içinde **slug'dan** türetiliyor (rehber kategorileriyle
/// aynı desen).
@freezed
abstract class AdCategory with _$AdCategory {
  const factory AdCategory({
    required String id,
    required String name,
    @Default('') String slug,
    String? parentId,
    String? icon,
    @Default(0) int displayOrder,
    @Default(0) int subCategoryCount,
  }) = _AdCategory;

  const AdCategory._();

  factory AdCategory.fromJson(Map<String, dynamic> json) =>
      _$AdCategoryFromJson(json);

  bool get hasSubCategories => subCategoryCount > 0;

  /// Slug → Material ikonu; bilinmeyen kategori nötr ikona düşer.
  IconData get materialIcon => switch (slug) {
    'araclar' => Icons.directions_car_rounded,
    'otomobil' => Icons.directions_car_filled_rounded,
    'motosiklet' => Icons.two_wheeler_rounded,
    'ticari-arac' => Icons.local_shipping_rounded,
    'emlak' => Icons.home_work_rounded,
    'satilik-konut' => Icons.house_rounded,
    'kiralik-konut' => Icons.vpn_key_rounded,
    'arsa' => Icons.landscape_rounded,
    'elektronik' => Icons.devices_rounded,
    'ev-esyasi' => Icons.chair_rounded,
    'giyim' => Icons.checkroom_rounded,
    'hayvanlar' => Icons.pets_rounded,
    'is-makineleri' => Icons.agriculture_rounded,
    'diger' => Icons.category_rounded,
    _ => Icons.label_rounded,
  };
}
