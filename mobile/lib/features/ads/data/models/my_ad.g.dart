// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'my_ad.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_MyAd _$MyAdFromJson(Map<String, dynamic> json) => _MyAd(
  id: json['id'] as String,
  title: json['title'] as String,
  description: json['description'] as String?,
  price: (json['price'] as num?)?.toDouble(),
  status: json['status'] as String? ?? 'pending',
  categoryId: json['categoryId'] as String? ?? '',
  categoryName: json['categoryName'] as String? ?? '',
  contactPhone: json['contactPhone'] as String? ?? '',
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  phoneClickCount: (json['phoneClickCount'] as num?)?.toInt() ?? 0,
  whatsappClickCount: (json['whatsappClickCount'] as num?)?.toInt() ?? 0,
  favoriteCount: (json['favoriteCount'] as num?)?.toInt() ?? 0,
  extensionCount: (json['extensionCount'] as num?)?.toInt() ?? 0,
  maxExtensions: (json['maxExtensions'] as num?)?.toInt() ?? 0,
  rejectedReason: json['rejectedReason'] as String?,
  createdAt: DateTime.parse(json['createdAt'] as String),
  expiresAt: DateTime.parse(json['expiresAt'] as String),
  imageUrls:
      (json['imageUrls'] as List<dynamic>?)?.map((e) => e as String).toList() ??
      const <String>[],
);

Map<String, dynamic> _$MyAdToJson(_MyAd instance) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'description': instance.description,
  'price': instance.price,
  'status': instance.status,
  'categoryId': instance.categoryId,
  'categoryName': instance.categoryName,
  'contactPhone': instance.contactPhone,
  'viewCount': instance.viewCount,
  'phoneClickCount': instance.phoneClickCount,
  'whatsappClickCount': instance.whatsappClickCount,
  'favoriteCount': instance.favoriteCount,
  'extensionCount': instance.extensionCount,
  'maxExtensions': instance.maxExtensions,
  'rejectedReason': instance.rejectedReason,
  'createdAt': instance.createdAt.toIso8601String(),
  'expiresAt': instance.expiresAt.toIso8601String(),
  'imageUrls': instance.imageUrls,
};
