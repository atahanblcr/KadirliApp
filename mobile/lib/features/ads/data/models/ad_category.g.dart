// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ad_category.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_AdCategory _$AdCategoryFromJson(Map<String, dynamic> json) => _AdCategory(
  id: json['id'] as String,
  name: json['name'] as String,
  slug: json['slug'] as String? ?? '',
  parentId: json['parentId'] as String?,
  icon: json['icon'] as String?,
  displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
  subCategoryCount: (json['subCategoryCount'] as num?)?.toInt() ?? 0,
);

Map<String, dynamic> _$AdCategoryToJson(_AdCategory instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'slug': instance.slug,
      'parentId': instance.parentId,
      'icon': instance.icon,
      'displayOrder': instance.displayOrder,
      'subCategoryCount': instance.subCategoryCount,
    };
