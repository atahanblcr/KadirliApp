// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'place_category.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_PlaceCategory _$PlaceCategoryFromJson(Map<String, dynamic> json) =>
    _PlaceCategory(
      id: json['id'] as String,
      name: json['name'] as String,
      slug: json['slug'] as String? ?? '',
    );

Map<String, dynamic> _$PlaceCategoryToJson(_PlaceCategory instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'slug': instance.slug,
    };
