// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ad_detail.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_AdDetail _$AdDetailFromJson(Map<String, dynamic> json) => _AdDetail(
  id: json['id'] as String,
  title: json['title'] as String,
  description: json['description'] as String? ?? '',
  price: (json['price'] as num?)?.toDouble(),
  status: json['status'] as String? ?? 'approved',
  categoryId: json['categoryId'] as String,
  categoryName: json['categoryName'] as String? ?? '',
  userId: json['userId'] as String? ?? '',
  sellerName: json['sellerName'] as String?,
  contactPhone: json['contactPhone'] as String? ?? '',
  viewCount: (json['viewCount'] as num?)?.toInt() ?? 0,
  createdAt: DateTime.parse(json['createdAt'] as String),
  expiresAt: DateTime.parse(json['expiresAt'] as String),
  images:
      (json['images'] as List<dynamic>?)
          ?.map((e) => AdImage.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <AdImage>[],
  properties:
      (json['properties'] as List<dynamic>?)
          ?.map((e) => AdPropertyValue.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const <AdPropertyValue>[],
);

Map<String, dynamic> _$AdDetailToJson(_AdDetail instance) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'description': instance.description,
  'price': instance.price,
  'status': instance.status,
  'categoryId': instance.categoryId,
  'categoryName': instance.categoryName,
  'userId': instance.userId,
  'sellerName': instance.sellerName,
  'contactPhone': instance.contactPhone,
  'viewCount': instance.viewCount,
  'createdAt': instance.createdAt.toIso8601String(),
  'expiresAt': instance.expiresAt.toIso8601String(),
  'images': instance.images,
  'properties': instance.properties,
};

_AdImage _$AdImageFromJson(Map<String, dynamic> json) => _AdImage(
  id: json['id'] as String,
  fileId: json['fileId'] as String? ?? '',
  url: json['url'] as String?,
  isCover: json['isCover'] as bool? ?? false,
  displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
);

Map<String, dynamic> _$AdImageToJson(_AdImage instance) => <String, dynamic>{
  'id': instance.id,
  'fileId': instance.fileId,
  'url': instance.url,
  'isCover': instance.isCover,
  'displayOrder': instance.displayOrder,
};

_AdPropertyValue _$AdPropertyValueFromJson(Map<String, dynamic> json) =>
    _AdPropertyValue(
      propertyId: json['propertyId'] as String,
      propertyName: json['propertyName'] as String? ?? '',
      propertyType: json['propertyType'] as String? ?? 'Text',
      value: json['value'] as String? ?? '',
    );

Map<String, dynamic> _$AdPropertyValueToJson(_AdPropertyValue instance) =>
    <String, dynamic>{
      'propertyId': instance.propertyId,
      'propertyName': instance.propertyName,
      'propertyType': instance.propertyType,
      'value': instance.value,
    };
