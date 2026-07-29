// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'api_meta.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_ApiMeta _$ApiMetaFromJson(Map<String, dynamic> json) => _ApiMeta(
  timestamp: json['timestamp'] == null
      ? null
      : DateTime.parse(json['timestamp'] as String),
  path: json['path'] as String?,
  traceId: json['traceId'] as String?,
);

Map<String, dynamic> _$ApiMetaToJson(_ApiMeta instance) => <String, dynamic>{
  'timestamp': instance.timestamp?.toIso8601String(),
  'path': instance.path,
  'traceId': instance.traceId,
};
