import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'place_category.freezed.dart';
part 'place_category.g.dart';

/// `GET /v1/places/categories` öğesi (lookup: `{id, name, slug}`).
///
/// 🔑 Bu uç **11.11'de eklendi** (backend'e tek additive dokunuş): `PlaceResponseDto`
/// yalnız `categoryId` taşıyor, public bir kategori ucu yoktu → mobil ne kartta
/// kategori adını yazabiliyor ne de filtre chip'i çizebiliyordu.
///
/// İkon slug'dan türetiliyor (11.6/11.7/11.10 kararı: sunucunun ikon alanı boş,
/// yeni ikon paketi eklenmiyor).
@freezed
abstract class PlaceCategory with _$PlaceCategory {
  const factory PlaceCategory({
    required String id,
    required String name,
    @Default('') String slug,
  }) = _PlaceCategory;

  const PlaceCategory._();

  factory PlaceCategory.fromJson(Map<String, dynamic> json) =>
      _$PlaceCategoryFromJson(json);

  IconData get materialIcon => switch (slug) {
    'doga-yayla' => Icons.forest_rounded,
    'tarihi-yerler' => Icons.account_balance_rounded,
    'piknik-alanlari' => Icons.outdoor_grill_rounded,
    'muzeler' => Icons.museum_rounded,
    'parklar' => Icons.park_rounded,
    'sehir-merkezi' => Icons.location_city_rounded,
    'mesire-alanlari' => Icons.nature_people_rounded,
    _ => Icons.place_rounded,
  };
}
