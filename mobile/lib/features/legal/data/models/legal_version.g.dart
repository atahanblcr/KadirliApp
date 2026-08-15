// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'legal_version.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_LegalVersion _$LegalVersionFromJson(Map<String, dynamic> json) =>
    _LegalVersion(
      id: json['id'] as String,
      documentType: json['documentType'] as String,
      documentTitle: json['documentTitle'] as String,
      versionNumber: (json['versionNumber'] as num?)?.toInt() ?? 1,
      summary: json['summary'] as String?,
      body: json['body'] as String? ?? '',
      effectiveFrom: json['effectiveFrom'] == null
          ? null
          : DateTime.parse(json['effectiveFrom'] as String),
      publishedAt: json['publishedAt'] == null
          ? null
          : DateTime.parse(json['publishedAt'] as String),
      isLive: json['isLive'] as bool? ?? false,
      supersededAt: json['supersededAt'] == null
          ? null
          : DateTime.parse(json['supersededAt'] as String),
    );

Map<String, dynamic> _$LegalVersionToJson(_LegalVersion instance) =>
    <String, dynamic>{
      'id': instance.id,
      'documentType': instance.documentType,
      'documentTitle': instance.documentTitle,
      'versionNumber': instance.versionNumber,
      'summary': instance.summary,
      'body': instance.body,
      'effectiveFrom': instance.effectiveFrom?.toIso8601String(),
      'publishedAt': instance.publishedAt?.toIso8601String(),
      'isLive': instance.isLive,
      'supersededAt': instance.supersededAt?.toIso8601String(),
    };
