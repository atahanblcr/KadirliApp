// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'taxi_driver.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_TaxiDriver _$TaxiDriverFromJson(Map<String, dynamic> json) => _TaxiDriver(
  id: json['id'] as String,
  userId: json['userId'] as String?,
  name: json['name'] as String,
  phone: json['phone'] as String? ?? '',
  plaka: json['plaka'] as String?,
  vehicleInfo: json['vehicleInfo'] as String?,
  isVerified: json['isVerified'] as bool? ?? true,
  isActive: json['isActive'] as bool? ?? true,
);

Map<String, dynamic> _$TaxiDriverToJson(_TaxiDriver instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'name': instance.name,
      'phone': instance.phone,
      'plaka': instance.plaka,
      'vehicleInfo': instance.vehicleInfo,
      'isVerified': instance.isVerified,
      'isActive': instance.isActive,
    };
