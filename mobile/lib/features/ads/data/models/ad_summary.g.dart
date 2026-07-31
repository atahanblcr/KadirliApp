// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ad_summary.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_AdSummary _$AdSummaryFromJson(Map<String, dynamic> json) => _AdSummary(
  id: json['id'] as String,
  title: json['title'] as String,
  description: json['description'] as String?,
  price: (json['price'] as num?)?.toDouble(),
  status: json['status'] as String? ?? 'approved',
  contactPhone: json['contactPhone'] as String? ?? '',
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  createdAt: DateTime.parse(json['createdAt'] as String),
  imageUrls:
      (json['imageUrls'] as List<dynamic>?)?.map((e) => e as String).toList() ??
      const <String>[],
);

Map<String, dynamic> _$AdSummaryToJson(_AdSummary instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'description': instance.description,
      'price': instance.price,
      'status': instance.status,
      'contactPhone': instance.contactPhone,
      'viewCount': instance.viewCount,
      'createdAt': instance.createdAt.toIso8601String(),
      'imageUrls': instance.imageUrls,
    };
