// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'campaign.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_Campaign _$CampaignFromJson(Map<String, dynamic> json) => _Campaign(
  id: json['id'] as String,
  businessId: json['businessId'] as String,
  businessName: json['businessName'] as String?,
  title: json['title'] as String,
  description: json['description'] as String? ?? '',
  discountPercentage: (json['discountPercentage'] as num?)?.toDouble(),
  discountCode: json['discountCode'] as String?,
  terms: json['terms'] as String?,
  startDate: DateTime.parse(json['startDate'] as String),
  endDate: DateTime.parse(json['endDate'] as String),
  codeViewCount: (json['codeViewCount'] as num?)?.toInt() ?? 0,
  coverImageId: json['coverImageId'] as String?,
  coverImageUrl: json['coverImageUrl'] as String?,
  status: json['status'] as String? ?? 'approved',
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$CampaignToJson(_Campaign instance) => <String, dynamic>{
  'id': instance.id,
  'businessId': instance.businessId,
  'businessName': instance.businessName,
  'title': instance.title,
  'description': instance.description,
  'discountPercentage': instance.discountPercentage,
  'discountCode': instance.discountCode,
  'terms': instance.terms,
  'startDate': instance.startDate.toIso8601String(),
  'endDate': instance.endDate.toIso8601String(),
  'codeViewCount': instance.codeViewCount,
  'coverImageId': instance.coverImageId,
  'coverImageUrl': instance.coverImageUrl,
  'status': instance.status,
  'createdAt': instance.createdAt?.toIso8601String(),
};
