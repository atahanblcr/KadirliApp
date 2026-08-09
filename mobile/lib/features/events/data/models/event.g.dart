// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'event.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Event _$EventFromJson(Map<String, dynamic> json) => _Event(
  id: json['id'] as String,
  title: json['title'] as String,
  description: json['description'] as String? ?? '',
  categoryId: json['categoryId'] as String?,
  categoryName: json['categoryName'] as String?,
  eventDate: DateTime.parse(json['eventDate'] as String),
  eventTime: json['eventTime'] as String? ?? '00:00:00',
  venueName: json['venueName'] as String?,
  address: json['address'] as String?,
  districtId: json['districtId'] as String?,
  districtName: json['districtName'] as String?,
  provinceName: json['provinceName'] as String?,
  locationLabel: json['locationLabel'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble(),
  longitude: (json['longitude'] as num?)?.toDouble(),
  hasLocation: json['hasLocation'] as bool? ?? false,
  organizer: json['organizer'] as String?,
  ticketPrice: (json['ticketPrice'] as num?)?.toDouble(),
  isFree: json['isFree'] as bool? ?? false,
  isLocal: json['isLocal'] as bool? ?? true,
  coverImageId: json['coverImageId'] as String?,
  coverImageUrl: json['coverImageUrl'] as String?,
  status: json['status'] as String? ?? 'approved',
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$EventToJson(_Event instance) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'description': instance.description,
  'categoryId': instance.categoryId,
  'categoryName': instance.categoryName,
  'eventDate': instance.eventDate.toIso8601String(),
  'eventTime': instance.eventTime,
  'venueName': instance.venueName,
  'address': instance.address,
  'districtId': instance.districtId,
  'districtName': instance.districtName,
  'provinceName': instance.provinceName,
  'locationLabel': instance.locationLabel,
  'latitude': instance.latitude,
  'longitude': instance.longitude,
  'hasLocation': instance.hasLocation,
  'organizer': instance.organizer,
  'ticketPrice': instance.ticketPrice,
  'isFree': instance.isFree,
  'isLocal': instance.isLocal,
  'coverImageId': instance.coverImageId,
  'coverImageUrl': instance.coverImageUrl,
  'status': instance.status,
  'createdAt': instance.createdAt?.toIso8601String(),
};
