// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'duty_schedule.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_DutySchedule _$DutyScheduleFromJson(Map<String, dynamic> json) =>
    _DutySchedule(
      id: json['id'] as String,
      dutyDate: DateTime.parse(json['dutyDate'] as String),
      startTime: json['startTime'] as String? ?? '',
      endTime: json['endTime'] as String? ?? '',
      pharmacyId: json['pharmacyId'] as String,
      pharmacyName: json['pharmacyName'] as String,
      source: json['source'] as String?,
    );

Map<String, dynamic> _$DutyScheduleToJson(_DutySchedule instance) =>
    <String, dynamic>{
      'id': instance.id,
      'dutyDate': instance.dutyDate.toIso8601String(),
      'startTime': instance.startTime,
      'endTime': instance.endTime,
      'pharmacyId': instance.pharmacyId,
      'pharmacyName': instance.pharmacyName,
      'source': instance.source,
    };
