// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'power_outage.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_PowerOutage _$PowerOutageFromJson(Map<String, dynamic> json) => _PowerOutage(
  id: json['id'] as String,
  neighborhood: json['neighborhood'] as String?,
  startTime: DateTime.parse(json['startTime'] as String),
  endTime: DateTime.parse(json['endTime'] as String),
  reason: json['reason'] as String?,
);

Map<String, dynamic> _$PowerOutageToJson(_PowerOutage instance) =>
    <String, dynamic>{
      'id': instance.id,
      'neighborhood': instance.neighborhood,
      'startTime': instance.startTime.toIso8601String(),
      'endTime': instance.endTime.toIso8601String(),
      'reason': instance.reason,
    };
