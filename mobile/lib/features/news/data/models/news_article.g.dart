// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'news_article.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_NewsArticle _$NewsArticleFromJson(Map<String, dynamic> json) => _NewsArticle(
  id: json['id'] as String,
  title: json['title'] as String? ?? '',
  excerpt: json['excerpt'] as String? ?? '',
  contentHtml: json['contentHtml'] as String?,
  imageUrl: json['imageUrl'] as String?,
  imageWidth: (json['imageWidth'] as num?)?.toInt(),
  imageHeight: (json['imageHeight'] as num?)?.toInt(),
  sourceUrl: json['sourceUrl'] as String?,
  publishedAt: json['publishedAt'] == null
      ? null
      : DateTime.parse(json['publishedAt'] as String),
  modifiedAt: json['modifiedAt'] == null
      ? null
      : DateTime.parse(json['modifiedAt'] as String),
  readingMinutes: (json['readingMinutes'] as num?)?.toInt() ?? 1,
  isFeatured: json['isFeatured'] as bool? ?? false,
  categories:
      (json['categories'] as List<dynamic>?)
          ?.map((e) => NewsCategory.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <NewsCategory>[],
);

Map<String, dynamic> _$NewsArticleToJson(_NewsArticle instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'excerpt': instance.excerpt,
      'contentHtml': instance.contentHtml,
      'imageUrl': instance.imageUrl,
      'imageWidth': instance.imageWidth,
      'imageHeight': instance.imageHeight,
      'sourceUrl': instance.sourceUrl,
      'publishedAt': instance.publishedAt?.toIso8601String(),
      'modifiedAt': instance.modifiedAt?.toIso8601String(),
      'readingMinutes': instance.readingMinutes,
      'isFeatured': instance.isFeatured,
      'categories': instance.categories,
    };
