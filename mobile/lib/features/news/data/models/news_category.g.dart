// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'news_category.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_NewsCategory _$NewsCategoryFromJson(Map<String, dynamic> json) =>
    _NewsCategory(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      slug: json['slug'] as String? ?? '',
      articleCount: (json['articleCount'] as num?)?.toInt() ?? 0,
      showInFilterStrip: json['showInFilterStrip'] as bool? ?? true,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$NewsCategoryToJson(_NewsCategory instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'slug': instance.slug,
      'articleCount': instance.articleCount,
      'showInFilterStrip': instance.showInFilterStrip,
      'displayOrder': instance.displayOrder,
    };
