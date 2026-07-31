import 'package:freezed_annotation/freezed_annotation.dart';

part 'guide_item.freezed.dart';
part 'guide_item.g.dart';

/// `GET /v1/guide/items` ve `/v1/guide/items/{id}` gövdesi (`GuideItemDto`).
///
/// Kategori bilgisi satırın içinde geliyor (`categoryName`) — liste ekranı
/// ayrıca kategori çekmek zorunda değil.
@freezed
abstract class GuideItem with _$GuideItem {
  const factory GuideItem({
    required String id,
    required String name,
    String? categoryId,
    String? categoryName,
    String? categoryIcon,
    String? categoryColor,
    String? phone,
    String? address,
    String? email,
    String? websiteUrl,
    String? workingHours,
    double? latitude,
    double? longitude,
    @Default(false) bool hasLocation,
    String? description,
    @Default(true) bool isActive,
  }) = _GuideItem;

  const GuideItem._();

  factory GuideItem.fromJson(Map<String, dynamic> json) =>
      _$GuideItemFromJson(json);

  bool get hasPhone => (phone ?? '').trim().isNotEmpty;

  /// Haritada gösterilebilir mi — sunucunun `hasLocation` bayrağı ile
  /// koordinatların **ikisi de** doğru olmalı (bayrak true ama koordinat null
  /// gelen kayıtlara karşı savunma).
  bool get canOpenMap => latitude != null && longitude != null;
}
