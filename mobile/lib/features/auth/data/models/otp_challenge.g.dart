// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'otp_challenge.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_OtpChallenge _$OtpChallengeFromJson(Map<String, dynamic> json) =>
    _OtpChallenge(
      message: json['message'] as String?,
      expiresIn: (json['expiresIn'] as num?)?.toInt() ?? 300,
      retryAfter: (json['retryAfter'] as num?)?.toInt() ?? 60,
      otp: json['otp'] as String?,
    );

Map<String, dynamic> _$OtpChallengeToJson(_OtpChallenge instance) =>
    <String, dynamic>{
      'message': instance.message,
      'expiresIn': instance.expiresIn,
      'retryAfter': instance.retryAfter,
      'otp': instance.otp,
    };
