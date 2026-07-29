// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'current_user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_CurrentUser _$CurrentUserFromJson(Map<String, dynamic> json) => _CurrentUser(
  id: json['id'] as String,
  phone: json['phone'] as String,
  username: json['username'] as String?,
  email: json['email'] as String?,
  age: (json['age'] as num?)?.toInt(),
  role: json['role'] as String? ?? 'user',
  primaryNeighborhoodId: json['primaryNeighborhoodId'] as String?,
  primaryNeighborhoodName: json['primaryNeighborhoodName'] as String?,
  profilePhotoUrl: json['profilePhotoUrl'] as String?,
  createdAt: json['createdAt'] == null
      ? null
      : DateTime.parse(json['createdAt'] as String),
);

Map<String, dynamic> _$CurrentUserToJson(_CurrentUser instance) =>
    <String, dynamic>{
      'id': instance.id,
      'phone': instance.phone,
      'username': instance.username,
      'email': instance.email,
      'age': instance.age,
      'role': instance.role,
      'primaryNeighborhoodId': instance.primaryNeighborhoodId,
      'primaryNeighborhoodName': instance.primaryNeighborhoodName,
      'profilePhotoUrl': instance.profilePhotoUrl,
      'createdAt': instance.createdAt?.toIso8601String(),
    };
