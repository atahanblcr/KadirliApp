// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'ad_extend_result.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_AdExtendResult _$AdExtendResultFromJson(Map<String, dynamic> json) =>
    _AdExtendResult(
      adId: json['adId'] as String,
      status: json['status'] as String? ?? 'approved',
      expiresAt: DateTime.parse(json['expiresAt'] as String),
      extensionCount: (json['extensionCount'] as num?)?.toInt() ?? 0,
      maxExtensions: (json['maxExtensions'] as num?)?.toInt() ?? 0,
      remainingExtensions: (json['remainingExtensions'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$AdExtendResultToJson(_AdExtendResult instance) =>
    <String, dynamic>{
      'adId': instance.adId,
      'status': instance.status,
      'expiresAt': instance.expiresAt.toIso8601String(),
      'extensionCount': instance.extensionCount,
      'maxExtensions': instance.maxExtensions,
      'remainingExtensions': instance.remainingExtensions,
    };
