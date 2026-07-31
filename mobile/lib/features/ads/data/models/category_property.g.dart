// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'category_property.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_CategoryProperty _$CategoryPropertyFromJson(Map<String, dynamic> json) =>
    _CategoryProperty(
      id: json['id'] as String,
      propertyName: json['propertyName'] as String? ?? '',
      propertyType: json['propertyType'] as String? ?? 'Text',
      isRequired: json['isRequired'] as bool? ?? false,
      defaultValue: json['defaultValue'] as String?,
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
      options:
          (json['options'] as List<dynamic>?)
              ?.map((e) => PropertyOption.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const <PropertyOption>[],
    );

Map<String, dynamic> _$CategoryPropertyToJson(_CategoryProperty instance) =>
    <String, dynamic>{
      'id': instance.id,
      'propertyName': instance.propertyName,
      'propertyType': instance.propertyType,
      'isRequired': instance.isRequired,
      'defaultValue': instance.defaultValue,
      'displayOrder': instance.displayOrder,
      'options': instance.options,
    };

_PropertyOption _$PropertyOptionFromJson(Map<String, dynamic> json) =>
    _PropertyOption(
      id: json['id'] as String,
      optionValue: json['optionValue'] as String? ?? '',
      displayOrder: (json['displayOrder'] as num?)?.toInt() ?? 0,
    );

Map<String, dynamic> _$PropertyOptionToJson(_PropertyOption instance) =>
    <String, dynamic>{
      'id': instance.id,
      'optionValue': instance.optionValue,
      'displayOrder': instance.displayOrder,
    };
