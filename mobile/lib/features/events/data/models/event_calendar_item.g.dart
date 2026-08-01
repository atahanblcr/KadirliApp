// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'event_calendar_item.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_EventCalendarItem _$EventCalendarItemFromJson(Map<String, dynamic> json) =>
    _EventCalendarItem(
      id: json['id'] as String,
      title: json['title'] as String,
      eventDate: DateTime.parse(json['eventDate'] as String),
      eventTime: json['eventTime'] as String? ?? '00:00:00',
      venueName: json['venueName'] as String?,
      categoryName: json['categoryName'] as String?,
      status: json['status'] as String? ?? 'approved',
    );

Map<String, dynamic> _$EventCalendarItemToJson(_EventCalendarItem instance) =>
    <String, dynamic>{
      'id': instance.id,
      'title': instance.title,
      'eventDate': instance.eventDate.toIso8601String(),
      'eventTime': instance.eventTime,
      'venueName': instance.venueName,
      'categoryName': instance.categoryName,
      'status': instance.status,
    };
