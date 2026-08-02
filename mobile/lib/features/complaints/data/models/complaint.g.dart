// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'complaint.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Complaint _$ComplaintFromJson(Map<String, dynamic> json) => _Complaint(
  id: json['id'] as String,
  type: json['type'] as String?,
  relatedModule: json['relatedModule'] as String?,
  relatedId: json['relatedId'] as String?,
  subject: json['subject'] as String? ?? '',
  message: json['message'] as String? ?? '',
  status: json['status'] as String? ?? 'pending',
  adminNotes: json['adminNotes'] as String?,
  resolvedAt: json['resolvedAt'] == null
      ? null
      : DateTime.parse(json['resolvedAt'] as String),
  createdAt: DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$ComplaintToJson(_Complaint instance) =>
    <String, dynamic>{
      'id': instance.id,
      'type': instance.type,
      'relatedModule': instance.relatedModule,
      'relatedId': instance.relatedId,
      'subject': instance.subject,
      'message': instance.message,
      'status': instance.status,
      'adminNotes': instance.adminNotes,
      'resolvedAt': instance.resolvedAt?.toIso8601String(),
      'createdAt': instance.createdAt.toIso8601String(),
    };
