// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'my_consent.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_MyConsent _$MyConsentFromJson(Map<String, dynamic> json) => _MyConsent(
  type: json['type'] as String,
  title: json['title'] as String,
  isMandatory: json['isMandatory'] as bool? ?? false,
  currentVersionId: json['currentVersionId'] as String,
  currentVersionNumber: (json['currentVersionNumber'] as num?)?.toInt() ?? 1,
  consentedVersionId: json['consentedVersionId'] as String?,
  consentedVersionNumber: (json['consentedVersionNumber'] as num?)?.toInt(),
  granted: json['granted'] as bool? ?? false,
  decidedAt: json['decidedAt'] == null
      ? null
      : DateTime.parse(json['decidedAt'] as String),
  revokedAt: json['revokedAt'] == null
      ? null
      : DateTime.parse(json['revokedAt'] as String),
  needsReconsent: json['needsReconsent'] as bool? ?? false,
);

Map<String, dynamic> _$MyConsentToJson(_MyConsent instance) =>
    <String, dynamic>{
      'type': instance.type,
      'title': instance.title,
      'isMandatory': instance.isMandatory,
      'currentVersionId': instance.currentVersionId,
      'currentVersionNumber': instance.currentVersionNumber,
      'consentedVersionId': instance.consentedVersionId,
      'consentedVersionNumber': instance.consentedVersionNumber,
      'granted': instance.granted,
      'decidedAt': instance.decidedAt?.toIso8601String(),
      'revokedAt': instance.revokedAt?.toIso8601String(),
      'needsReconsent': instance.needsReconsent,
    };
