// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'death_notice.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_DeathNotice _$DeathNoticeFromJson(Map<String, dynamic> json) => _DeathNotice(
  id: json['id'] as String,
  deceasedName: json['deceasedName'] as String,
  photoFileId: json['photoFileId'] as String?,
  photoUrl: json['photoUrl'] as String?,
  funeralDate: DateTime.parse(json['funeralDate'] as String),
  funeralTime: json['funeralTime'] as String? ?? '00:00:00',
  cemeteryId: json['cemeteryId'] as String?,
  cemeteryName: json['cemeteryName'] as String?,
  mosqueId: json['mosqueId'] as String?,
  mosqueName: json['mosqueName'] as String?,
  neighborhoodId: json['neighborhoodId'] as String?,
  condolenceAddress: json['condolenceAddress'] as String?,
  condolenceLatitude: (json['condolenceLatitude'] as num?)?.toDouble(),
  condolenceLongitude: (json['condolenceLongitude'] as num?)?.toDouble(),
  hasCondolenceLocation: json['hasCondolenceLocation'] as bool? ?? false,
  addedBy: json['addedBy'] as String?,
  status: json['status'] as String? ?? 'approved',
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$DeathNoticeToJson(_DeathNotice instance) =>
    <String, dynamic>{
      'id': instance.id,
      'deceasedName': instance.deceasedName,
      'photoFileId': instance.photoFileId,
      'photoUrl': instance.photoUrl,
      'funeralDate': instance.funeralDate.toIso8601String(),
      'funeralTime': instance.funeralTime,
      'cemeteryId': instance.cemeteryId,
      'cemeteryName': instance.cemeteryName,
      'mosqueId': instance.mosqueId,
      'mosqueName': instance.mosqueName,
      'neighborhoodId': instance.neighborhoodId,
      'condolenceAddress': instance.condolenceAddress,
      'condolenceLatitude': instance.condolenceLatitude,
      'condolenceLongitude': instance.condolenceLongitude,
      'hasCondolenceLocation': instance.hasCondolenceLocation,
      'addedBy': instance.addedBy,
      'status': instance.status,
      'createdAt': instance.createdAt?.toIso8601String(),
    };
