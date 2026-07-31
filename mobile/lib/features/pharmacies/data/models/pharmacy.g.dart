// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'pharmacy.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Pharmacy _$PharmacyFromJson(Map<String, dynamic> json) => _Pharmacy(
  id: json['id'] as String,
  name: json['name'] as String,
  address: json['address'] as String?,
  phone: json['phone'] as String?,
  latitude: (json['latitude'] as num?)?.toDouble(),
  longitude: (json['longitude'] as num?)?.toDouble(),
  workingHours: json['workingHours'] as String?,
  pharmacistName: json['pharmacistName'] as String?,
  isActive: json['isActive'] as bool? ?? true,
);

Map<String, dynamic> _$PharmacyToJson(_Pharmacy instance) => <String, dynamic>{
  'id': instance.id,
  'name': instance.name,
  'address': instance.address,
  'phone': instance.phone,
  'latitude': instance.latitude,
  'longitude': instance.longitude,
  'workingHours': instance.workingHours,
  'pharmacistName': instance.pharmacistName,
  'isActive': instance.isActive,
};
