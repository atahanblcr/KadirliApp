// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'intercity_route.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_IntercityRoute _$IntercityRouteFromJson(
  Map<String, dynamic> json,
) => _IntercityRoute(
  id: json['id'] as String,
  destination: json['destination'] as String,
  price: (json['price'] as num?)?.toDouble(),
  durationMinutes: (json['durationMinutes'] as num?)?.toInt(),
  company: json['company'] as String?,
  isActive: json['isActive'] as bool? ?? true,
  vehicleType: json['vehicleType'] as String? ?? 'bus',
  departurePointName: json['departurePointName'] as String?,
  departurePointAddress: json['departurePointAddress'] as String?,
  departurePointLatitude: (json['departurePointLatitude'] as num?)?.toDouble(),
  departurePointLongitude: (json['departurePointLongitude'] as num?)
      ?.toDouble(),
  schedules:
      (json['schedules'] as List<dynamic>?)
          ?.map((e) => IntercityDeparture.fromJson(e as Map<String, dynamic>))
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
      'vehicleType': instance.vehicleType,
      'departurePointName': instance.departurePointName,
      'departurePointAddress': instance.departurePointAddress,
      'departurePointLatitude': instance.departurePointLatitude,
      'departurePointLongitude': instance.departurePointLongitude,
      'schedules': instance.schedules,
    };

_IntercityDeparture _$IntercityDepartureFromJson(Map<String, dynamic> json) =>
    _IntercityDeparture(
      id: json['id'] as String,
      departureTime: json['departureTime'] as String? ?? '',
      days:
          (json['days'] as List<dynamic>?)?.map((e) => e as String).toList() ??
          const <String>[],
      runsDaily: json['runsDaily'] as bool? ?? true,
    );

Map<String, dynamic> _$IntercityDepartureToJson(_IntercityDeparture instance) =>
    <String, dynamic>{
      'id': instance.id,
      'departureTime': instance.departureTime,
      'days': instance.days,
      'runsDaily': instance.runsDaily,
    };
