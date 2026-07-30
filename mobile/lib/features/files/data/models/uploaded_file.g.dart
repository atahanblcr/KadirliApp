// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'uploaded_file.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_UploadedFile _$UploadedFileFromJson(Map<String, dynamic> json) =>
    _UploadedFile(
      id: json['id'] as String,
      cdnUrl: json['cdnUrl'] as String,
      originalName: json['originalName'] as String? ?? '',
    );

Map<String, dynamic> _$UploadedFileToJson(_UploadedFile instance) =>
    <String, dynamic>{
      'id': instance.id,
      'cdnUrl': instance.cdnUrl,
      'originalName': instance.originalName,
    };
