import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'guide_category.freezed.dart';
part 'guide_category.g.dart';

/// `GET /v1/guide/categories` öğesi (`GuideCategoryDto`).
///
/// ⚠️ Uç **sayfalı** döner (`pageSize` varsayılanı 10) — kategori sayısı
/// büyürse `limit` verilmeli; ekran 50 ile çekiyor.
///
/// `icon`/`color` panelde doldurulabiliyor ama seed veride **boş**; bu yüzden
/// ikon [materialIcon] içinde **slug'dan** türetiliyor (Türkçe karakter sorunu
/// olmayan, sabit bir anahtar).
@freezed
abstract class GuideCategory with _$GuideCategory {
  const factory GuideCategory({
    required String id,
    required String name,
    @Default('') String slug,
    String? parentId,
    String? icon,
    String? color,
    @Default(0) int displayOrder,
  }) = _GuideCategory;

  const GuideCategory._();

  factory GuideCategory.fromJson(Map<String, dynamic> json) =>
      _$GuideCategoryFromJson(json);

  /// Acil numaralar kategorisi mi (ekranda öne çıkarılır)?
  bool get isEmergency => slug == 'acil-numaralar';

  /// Slug → Material ikonu; bilinmeyen kategori nötr ikona düşer.
  IconData get materialIcon => switch (slug) {
    'resmi-kurumlar' => Icons.account_balance_rounded,
    'saglik' => Icons.local_hospital_rounded,
    'egitim' => Icons.school_rounded,
    'ulasim' => Icons.directions_bus_rounded,
    'acil-numaralar' => Icons.emergency_rounded,
    'esnaf' => Icons.storefront_rounded,
    'bankalar' => Icons.account_balance_wallet_rounded,
    'oteller' || 'konaklama' => Icons.hotel_rounded,
    'restoranlar' => Icons.restaurant_rounded,
    _ => Icons.label_rounded,
  };
}
