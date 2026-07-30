// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'announcement.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Announcement _$AnnouncementFromJson(Map<String, dynamic> json) =>
    _Announcement(
      id: json['id'] as String,
      title: json['title'] as String,
      body: json['body'] as String? ?? '',
      typeId: json['typeId'] as String?,
      typeName: json['typeName'] as String?,
      priority: (json['priority'] as num?)?.toInt() ?? 0,
      status: json['status'] as String? ?? '',
      sentAt: json['sentAt'] == null
          ? null
          : DateTime.parse(json['sentAt'] as String),
      scheduledFor: json['scheduledFor'] == null
          ? null
          : DateTime.parse(json['scheduledFor'] as String),
      visibleUntil: json['visibleUntil'] == null
          ? null
          : DateTime.parse(json['visibleUntil'] as String),
      createdAt: json['createdAt'] == null
          ? null
          : DateTime.parse(json['createdAt'] as String),
      imageUrl: json['imageUrl'] as String?,
      source: json['source'] as String?,
      sourceUrl: json['sourceUrl'] as String?,
      hasLink: json['hasLink'] as bool? ?? false,
      externalLink: json['externalLink'] as String?,
      hasLocation: json['hasLocation'] as bool? ?? false,
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      locationName: json['locationName'] as String?,
    );

Map<String, dynamic> _$AnnouncementToJson(_Announcement instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'body': instance.body,
      'typeId': instance.typeId,
      'typeName': instance.typeName,
      'priority': instance.priority,
      'status': instance.status,
      'sentAt': instance.sentAt?.toIso8601String(),
      'scheduledFor': instance.scheduledFor?.toIso8601String(),
      'visibleUntil': instance.visibleUntil?.toIso8601String(),
      'createdAt': instance.createdAt?.toIso8601String(),
      'imageUrl': instance.imageUrl,
      'source': instance.source,
      'sourceUrl': instance.sourceUrl,
      'hasLink': instance.hasLink,
      'externalLink': instance.externalLink,
      'hasLocation': instance.hasLocation,
      'latitude': instance.latitude,
      'longitude': instance.longitude,
      'locationName': instance.locationName,
    };
