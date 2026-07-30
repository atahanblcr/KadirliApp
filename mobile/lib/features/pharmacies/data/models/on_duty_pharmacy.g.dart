// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'on_duty_pharmacy.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_OnDutyPharmacy _$OnDutyPharmacyFromJson(Map<String, dynamic> json) =>
    _OnDutyPharmacy(
      scheduleId: json['scheduleId'] as String,
      dutyDate: DateTime.parse(json['dutyDate'] as String),
      startTime: json['startTime'] as String? ?? '',
      endTime: json['endTime'] as String? ?? '',
      pharmacyId: json['pharmacyId'] as String,
      name: json['name'] as String,
      address: json['address'] as String?,
      phone: json['phone'] as String?,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      pharmacistName: json['pharmacistName'] as String?,
      workingHours: json['workingHours'] as String?,
    );

Map<String, dynamic> _$OnDutyPharmacyToJson(_OnDutyPharmacy instance) =>
    <String, dynamic>{
      'scheduleId': instance.scheduleId,
      'dutyDate': instance.dutyDate.toIso8601String(),
      'startTime': instance.startTime,
      'endTime': instance.endTime,
      'pharmacyId': instance.pharmacyId,
      'name': instance.name,
      'address': instance.address,
      'phone': instance.phone,
      'latitude': instance.latitude,
      'longitude': instance.longitude,
      'pharmacistName': instance.pharmacistName,
      'workingHours': instance.workingHours,
    };
