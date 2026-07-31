// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'guide_item.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_GuideItem _$GuideItemFromJson(Map<String, dynamic> json) => _GuideItem(
  id: json['id'] as String,
  name: json['name'] as String,
  categoryId: json['categoryId'] as String?,
  categoryName: json['categoryName'] as String?,
  categoryIcon: json['categoryIcon'] as String?,
  categoryColor: json['categoryColor'] as String?,
  phone: json['phone'] as String?,
  address: json['address'] as String?,
  email: json['email'] as String?,
  websiteUrl: json['websiteUrl'] as String?,
  workingHours: json['workingHours'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble(),
  longitude: (json['longitude'] as num?)?.toDouble(),
  hasLocation: json['hasLocation'] as bool? ?? false,
  description: json['description'] as String?,
  isActive: json['isActive'] as bool? ?? true,
);

Map<String, dynamic> _$GuideItemToJson(_GuideItem instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'categoryId': instance.categoryId,
      'categoryName': instance.categoryName,
      'categoryIcon': instance.categoryIcon,
      'categoryColor': instance.categoryColor,
      'phone': instance.phone,
      'address': instance.address,
      'email': instance.email,
      'websiteUrl': instance.websiteUrl,
      'workingHours': instance.workingHours,
      'latitude': instance.latitude,
      'longitude': instance.longitude,
      'hasLocation': instance.hasLocation,
      'description': instance.description,
      'isActive': instance.isActive,
    };
