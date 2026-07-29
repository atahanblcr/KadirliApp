// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'verify_otp_result.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_VerifyOtpResult _$VerifyOtpResultFromJson(Map<String, dynamic> json) =>
    _VerifyOtpResult(
      isNewUser: json['isNewUser'] as bool? ?? false,
      tempToken: json['tempToken'] as String?,
      accessToken: json['accessToken'] as String?,
      refreshToken: json['refreshToken'] as String?,
      expiresIn: (json['expiresIn'] as num?)?.toInt(),
    );

Map<String, dynamic> _$VerifyOtpResultToJson(_VerifyOtpResult instance) =>
    <String, dynamic>{
      'isNewUser': instance.isNewUser,
      'tempToken': instance.tempToken,
      'accessToken': instance.accessToken,
      'refreshToken': instance.refreshToken,
      'expiresIn': instance.expiresIn,
    };
