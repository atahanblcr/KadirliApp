// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'favorite_ad.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_FavoriteAd _$FavoriteAdFromJson(Map<String, dynamic> json) => _FavoriteAd(
  adId: json['adId'] as String,
  title: json['title'] as String? ?? '',
  price: (json['price'] as num?)?.toDouble(),
  status: json['status'] as String? ?? '',
  isAvailable: json['isAvailable'] as bool? ?? true,
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  favoritedAt: DateTime.parse(json['favoritedAt'] as String),
  imageUrls:
      (json['imageUrls'] as List<dynamic>?)?.map((e) => e as String).toList() ??
      const <String>[],
);

Map<String, dynamic> _$FavoriteAdToJson(_FavoriteAd instance) =>
    <String, dynamic>{
      'adId': instance.adId,
      'title': instance.title,
      'price': instance.price,
      'status': instance.status,
      'isAvailable': instance.isAvailable,
      'viewCount': instance.viewCount,
      'favoritedAt': instance.favoritedAt.toIso8601String(),
      'imageUrls': instance.imageUrls,
    };
