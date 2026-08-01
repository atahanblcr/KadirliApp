// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'named_lookup.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_NamedLookup _$NamedLookupFromJson(Map<String, dynamic> json) => _NamedLookup(
  id: json['id'] as String,
  name: json['name'] as String,
  address: json['address'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble(),
  longitude: (json['longitude'] as num?)?.toDouble(),
);

Map<String, dynamic> _$NamedLookupToJson(_NamedLookup instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'address': instance.address,
      'latitude': instance.latitude,
      'longitude': instance.longitude,
    };
