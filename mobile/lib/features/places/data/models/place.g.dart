// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'place.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Place _$PlaceFromJson(Map<String, dynamic> json) => _Place(
  id: json['id'] as String,
  categoryId: json['categoryId'] as String,
  name: json['name'] as String,
  description: json['description'] as String?,
  address: json['address'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble() ?? 0,
  longitude: (json['longitude'] as num?)?.toDouble() ?? 0,
  entranceFee: (json['entranceFee'] as num?)?.toDouble(),
  isFree: json['isFree'] as bool? ?? false,
  openingHours: json['openingHours'] as String?,
  bestSeason: json['bestSeason'] as String?,
  howToGetThere: json['howToGetThere'] as String?,
  distanceFromCenter: (json['distanceFromCenter'] as num?)?.toDouble(),
  amenities: _rawAmenities(json['amenities']),
  coverImageId: json['coverImageId'] as String?,
  coverImageUrl: json['coverImageUrl'] as String?,
  isActive: json['isActive'] as bool? ?? true,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$PlaceToJson(_Place instance) => <String, dynamic>{
  'id': instance.id,
  'categoryId': instance.categoryId,
  'name': instance.name,
  'description': instance.description,
  'address': instance.address,
  'latitude': instance.latitude,
  'longitude': instance.longitude,
  'entranceFee': instance.entranceFee,
  'isFree': instance.isFree,
  'openingHours': instance.openingHours,
  'bestSeason': instance.bestSeason,
  'howToGetThere': instance.howToGetThere,
  'distanceFromCenter': instance.distanceFromCenter,
  'amenities': instance.amenities,
  'coverImageId': instance.coverImageId,
  'coverImageUrl': instance.coverImageUrl,
  'isActive': instance.isActive,
  'createdAt': instance.createdAt?.toIso8601String(),
};
