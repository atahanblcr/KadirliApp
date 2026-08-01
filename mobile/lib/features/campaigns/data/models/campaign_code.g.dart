// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'campaign_code.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_CampaignCode _$CampaignCodeFromJson(Map<String, dynamic> json) =>
    _CampaignCode(
      code: json['code'] as String,
      viewedAt: DateTime.parse(json['viewedAt'] as String),
    );

Map<String, dynamic> _$CampaignCodeToJson(_CampaignCode instance) =>
    <String, dynamic>{
      'code': instance.code,
      'viewedAt': instance.viewedAt.toIso8601String(),
    };
