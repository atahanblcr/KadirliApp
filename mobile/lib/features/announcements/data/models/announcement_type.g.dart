// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'announcement_type.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_AnnouncementType _$AnnouncementTypeFromJson(Map<String, dynamic> json) =>
    _AnnouncementType(
      id: json['id'] as String,
      name: json['name'] as String,
      slug: json['slug'] as String? ?? '',
      icon: json['icon'] as String?,
      color: json['color'] as String?,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$AnnouncementTypeToJson(_AnnouncementType instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'slug': instance.slug,
      'icon': instance.icon,
      'color': instance.color,
      'displayOrder': instance.displayOrder,
    };
