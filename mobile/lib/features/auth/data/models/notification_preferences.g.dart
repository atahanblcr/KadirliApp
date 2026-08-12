// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'notification_preferences.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_NotificationPreferences _$NotificationPreferencesFromJson(
  Map<String, dynamic> json,
) => _NotificationPreferences(
  announcements: json['announcements'] as bool? ?? true,
  deaths: json['deaths'] as bool? ?? true,
  pharmacy: json['pharmacy'] as bool? ?? true,
  events: json['events'] as bool? ?? true,
  ads: json['ads'] as bool? ?? false,
  campaigns: json['campaigns'] as bool? ?? false,
  news: json['news'] as bool? ?? true,
);

Map<String, dynamic> _$NotificationPreferencesToJson(
  _NotificationPreferences instance,
) => <String, dynamic>{
  'announcements': instance.announcements,
  'deaths': instance.deaths,
  'pharmacy': instance.pharmacy,
  'events': instance.events,
  'ads': instance.ads,
  'campaigns': instance.campaigns,
  'news': instance.news,
};
