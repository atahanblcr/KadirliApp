// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'paged_result.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_PagedResult<T> _$PagedResultFromJson<T>(
  Map<String, dynamic> json,
  T Function(Object? json) fromJsonT,
) => _PagedResult<T>(
  items:
      (json['items'] as List<dynamic>?)?.map(fromJsonT).toList() ??
      const <Never>[],
  totalCount: (json['totalCount'] as num?)?.toInt() ?? 0,
  pageSize: (json['pageSize'] as num?)?.toInt() ?? 0,
  currentPage: (json['currentPage'] as num?)?.toInt() ?? 1,
  totalPages: (json['totalPages'] as num?)?.toInt() ?? 0,
);

Map<String, dynamic> _$PagedResultToJson<T>(
  _PagedResult<T> instance,
  Object? Function(T value) toJsonT,
) => <String, dynamic>{
  'items': instance.items.map(toJsonT).toList(),
  'totalCount': instance.totalCount,
  'pageSize': instance.pageSize,
  'currentPage': instance.currentPage,
  'totalPages': instance.totalPages,
};
