// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'legal_document.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_LegalDocument _$LegalDocumentFromJson(Map<String, dynamic> json) =>
    _LegalDocument(
      id: json['id'] as String,
      type: json['type'] as String,
      title: json['title'] as String,
      versionId: json['versionId'] as String,
      versionNumber: (json['versionNumber'] as num?)?.toInt() ?? 1,
      summary: json['summary'] as String?,
      body: json['body'] as String? ?? '',
      isMandatory: json['isMandatory'] as bool? ?? false,
      showAtRegistration: json['showAtRegistration'] as bool? ?? false,
      sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
      effectiveFrom: json['effectiveFrom'] == null
          ? null
          : DateTime.parse(json['effectiveFrom'] as String),
      requiresReconsent: json['requiresReconsent'] as bool? ?? false,
    );

Map<String, dynamic> _$LegalDocumentToJson(_LegalDocument instance) =>
    <String, dynamic>{
      'id': instance.id,
      'type': instance.type,
      'title': instance.title,
      'versionId': instance.versionId,
      'versionNumber': instance.versionNumber,
      'summary': instance.summary,
      'body': instance.body,
      'isMandatory': instance.isMandatory,
      'showAtRegistration': instance.showAtRegistration,
      'sortOrder': instance.sortOrder,
      'effectiveFrom': instance.effectiveFrom?.toIso8601String(),
      'requiresReconsent': instance.requiresReconsent,
    };
