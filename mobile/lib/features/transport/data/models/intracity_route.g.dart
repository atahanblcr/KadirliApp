// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'intracity_route.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_IntracityRoute _$IntracityRouteFromJson(Map<String, dynamic> json) =>
    _IntracityRoute(
      id: json['id'] as String,
      routeNumber: json['routeNumber'] as String? ?? '',
      routeName: json['routeName'] as String? ?? '',
      firstDeparture: json['firstDeparture'] as String?,
      lastDeparture: json['lastDeparture'] as String?,
      frequencyMinutes: (json['frequencyMinutes'] as num?)?.toInt(),
      isActive: json['isActive'] as bool? ?? true,
      stops:
          (json['stops'] as List<dynamic>?)
              ?.map((e) => IntracityStop.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <IntracityStop>[],
    );

Map<String, dynamic> _$IntracityRouteToJson(_IntracityRoute instance) =>
    <String, dynamic>{
      'id': instance.id,
      'routeNumber': instance.routeNumber,
      'routeName': instance.routeName,
      'firstDeparture': instance.firstDeparture,
      'lastDeparture': instance.lastDeparture,
      'frequencyMinutes': instance.frequencyMinutes,
      'isActive': instance.isActive,
      'stops': instance.stops,
    };

_IntracityStop _$IntracityStopFromJson(Map<String, dynamic> json) =>
    _IntracityStop(
      id: json['id'] as String,
      stopName: json['stopName'] as String? ?? '',
      stopOrder: (json['stopOrder'] as num?)?.toInt() ?? 0,
      timeFromStart: (json['timeFromStart'] as num?)?.toInt(),
    );

Map<String, dynamic> _$IntracityStopToJson(_IntracityStop instance) =>
    <String, dynamic>{
      'id': instance.id,
      'stopName': instance.stopName,
      'stopOrder': instance.stopOrder,
      'timeFromStart': instance.timeFromStart,
    };
