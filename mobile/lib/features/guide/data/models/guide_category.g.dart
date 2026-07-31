// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'guide_category.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_GuideCategory _$GuideCategoryFromJson(Map<String, dynamic> json) =>
    _GuideCategory(
      id: json['id'] as String,
      name: json['name'] as String,
      slug: json['slug'] as String? ?? '',
      parentId: json['parentId'] as String?,
      icon: json['icon'] as String?,
      color: json['color'] as String?,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$GuideCategoryToJson(_GuideCategory instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'slug': instance.slug,
      'parentId': instance.parentId,
      'icon': instance.icon,
      'color': instance.color,
      'displayOrder': instance.displayOrder,
    };
