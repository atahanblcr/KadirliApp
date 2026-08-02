// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'intercity_route.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_IntercityRoute _$IntercityRouteFromJson(Map<String, dynamic> json) =>
    _IntercityRoute(
      id: json['id'] as String,
      destination: json['destination'] as String,
      price: (json['price'] as num?)?.toDouble(),
      durationMinutes: (json['durationMinutes'] as num?)?.toInt(),
      company: json['company'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      schedules:
          (json['schedules'] as List<dynamic>?)
              ?.map(
                (e) => IntercityDeparture.fromJson(e as Map<String, dynamic>),
              )
              .toList() ??
          const <IntercityDeparture>[],
    );

Map<String, dynamic> _$IntercityRouteToJson(_IntercityRoute instance) =>
    <String, dynamic>{
      'id': instance.id,
      'destination': instance.destination,
      'price': instance.price,
      'durationMinutes': instance.durationMinutes,
      'company': instance.company,
      'isActive': instance.isActive,
      'schedules': instance.schedules,
    };

_IntercityDeparture _$IntercityDepartureFromJson(Map<String, dynamic> json) =>
    _IntercityDeparture(
      id: json['id'] as String,
      departureTime: json['departureTime'] as String? ?? '',
    );

Map<String, dynamic> _$IntercityDepartureToJson(_IntercityDeparture instance) =>
    <String, dynamic>{
      'id': instance.id,
      'departureTime': instance.departureTime,
    };
