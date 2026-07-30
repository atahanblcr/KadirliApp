// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'current_user.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$CurrentUser {

 String get id; String get phone; String? get username; String? get email; int? get age;/// `user` | `moderator` | `admin` | `super_admin`.
 String get role; String? get primaryNeighborhoodId; String? get primaryNeighborhoodName;/// Göreli URL (`/uploads/...`) — gösterirken `AppImage.url` ile origin eklenir.
 String? get profilePhotoUrl;/// Altı bildirim anahtarı (11.5 — Ayarlar ekranı).
 NotificationPreferences get notificationPreferences;/// Kullanıcı adının **en son** değiştirildiği an (kayıt anı sayılmaz →
/// ilk değişiklik serbest). 30 günlük kısıt bundan hesaplanır.
 DateTime? get usernameLastChangedAt;/// Birincil mahallenin en son değiştirildiği an (aynı kural).
 DateTime? get neighborhoodLastChangedAt; DateTime? get createdAt;
/// Create a copy of CurrentUser
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CurrentUserCopyWith<CurrentUser> get copyWith => _$CurrentUserCopyWithImpl<CurrentUser>(this as CurrentUser, _$identity);

  /// Serializes this CurrentUser to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CurrentUser&&(identical(other.id, id) || other.id == id)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.username, username) || other.username == username)&&(identical(other.email, email) || other.email == email)&&(identical(other.age, age) || other.age == age)&&(identical(other.role, role) || other.role == role)&&(identical(other.primaryNeighborhoodId, primaryNeighborhoodId) || other.primaryNeighborhoodId == primaryNeighborhoodId)&&(identical(other.primaryNeighborhoodName, primaryNeighborhoodName) || other.primaryNeighborhoodName == primaryNeighborhoodName)&&(identical(other.profilePhotoUrl, profilePhotoUrl) || other.profilePhotoUrl == profilePhotoUrl)&&(identical(other.notificationPreferences, notificationPreferences) || other.notificationPreferences == notificationPreferences)&&(identical(other.usernameLastChangedAt, usernameLastChangedAt) || other.usernameLastChangedAt == usernameLastChangedAt)&&(identical(other.neighborhoodLastChangedAt, neighborhoodLastChangedAt) || other.neighborhoodLastChangedAt == neighborhoodLastChangedAt)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,phone,username,email,age,role,primaryNeighborhoodId,primaryNeighborhoodName,profilePhotoUrl,notificationPreferences,usernameLastChangedAt,neighborhoodLastChangedAt,createdAt);

@override
String toString() {
  return 'CurrentUser(id: $id, phone: $phone, username: $username, email: $email, age: $age, role: $role, primaryNeighborhoodId: $primaryNeighborhoodId, primaryNeighborhoodName: $primaryNeighborhoodName, profilePhotoUrl: $profilePhotoUrl, notificationPreferences: $notificationPreferences, usernameLastChangedAt: $usernameLastChangedAt, neighborhoodLastChangedAt: $neighborhoodLastChangedAt, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $CurrentUserCopyWith<$Res>  {
  factory $CurrentUserCopyWith(CurrentUser value, $Res Function(CurrentUser) _then) = _$CurrentUserCopyWithImpl;
@useResult
$Res call({
 String id, String phone, String? username, String? email, int? age, String role, String? primaryNeighborhoodId, String? primaryNeighborhoodName, String? profilePhotoUrl, NotificationPreferences notificationPreferences, DateTime? usernameLastChangedAt, DateTime? neighborhoodLastChangedAt, DateTime? createdAt
});


$NotificationPreferencesCopyWith<$Res> get notificationPreferences;

}
/// @nodoc
class _$CurrentUserCopyWithImpl<$Res>
    implements $CurrentUserCopyWith<$Res> {
  _$CurrentUserCopyWithImpl(this._self, this._then);

  final CurrentUser _self;
  final $Res Function(CurrentUser) _then;

/// Create a copy of CurrentUser
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? phone = null,Object? username = freezed,Object? email = freezed,Object? age = freezed,Object? role = null,Object? primaryNeighborhoodId = freezed,Object? primaryNeighborhoodName = freezed,Object? profilePhotoUrl = freezed,Object? notificationPreferences = null,Object? usernameLastChangedAt = freezed,Object? neighborhoodLastChangedAt = freezed,Object? createdAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,phone: null == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String,username: freezed == username ? _self.username : username // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,age: freezed == age ? _self.age : age // ignore: cast_nullable_to_non_nullable
as int?,role: null == role ? _self.role : role // ignore: cast_nullable_to_non_nullable
as String,primaryNeighborhoodId: freezed == primaryNeighborhoodId ? _self.primaryNeighborhoodId : primaryNeighborhoodId // ignore: cast_nullable_to_non_nullable
as String?,primaryNeighborhoodName: freezed == primaryNeighborhoodName ? _self.primaryNeighborhoodName : primaryNeighborhoodName // ignore: cast_nullable_to_non_nullable
as String?,profilePhotoUrl: freezed == profilePhotoUrl ? _self.profilePhotoUrl : profilePhotoUrl // ignore: cast_nullable_to_non_nullable
as String?,notificationPreferences: null == notificationPreferences ? _self.notificationPreferences : notificationPreferences // ignore: cast_nullable_to_non_nullable
as NotificationPreferences,usernameLastChangedAt: freezed == usernameLastChangedAt ? _self.usernameLastChangedAt : usernameLastChangedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,neighborhoodLastChangedAt: freezed == neighborhoodLastChangedAt ? _self.neighborhoodLastChangedAt : neighborhoodLastChangedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}
/// Create a copy of CurrentUser
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$NotificationPreferencesCopyWith<$Res> get notificationPreferences {
  
  return $NotificationPreferencesCopyWith<$Res>(_self.notificationPreferences, (value) {
    return _then(_self.copyWith(notificationPreferences: value));
  });
}
}


/// Adds pattern-matching-related methods to [CurrentUser].
extension CurrentUserPatterns on CurrentUser {
/// A variant of `map` that fallback to returning `orElse`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CurrentUser value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CurrentUser() when $default != null:
return $default(_that);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// Callbacks receives the raw object, upcasted.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case final Subclass2 value:
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CurrentUser value)  $default,){
final _that = this;
switch (_that) {
case _CurrentUser():
return $default(_that);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `map` that fallback to returning `null`.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case final Subclass value:
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CurrentUser value)?  $default,){
final _that = this;
switch (_that) {
case _CurrentUser() when $default != null:
return $default(_that);case _:
  return null;

}
}
/// A variant of `when` that fallback to an `orElse` callback.
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return orElse();
/// }
/// ```

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String phone,  String? username,  String? email,  int? age,  String role,  String? primaryNeighborhoodId,  String? primaryNeighborhoodName,  String? profilePhotoUrl,  NotificationPreferences notificationPreferences,  DateTime? usernameLastChangedAt,  DateTime? neighborhoodLastChangedAt,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CurrentUser() when $default != null:
return $default(_that.id,_that.phone,_that.username,_that.email,_that.age,_that.role,_that.primaryNeighborhoodId,_that.primaryNeighborhoodName,_that.profilePhotoUrl,_that.notificationPreferences,_that.usernameLastChangedAt,_that.neighborhoodLastChangedAt,_that.createdAt);case _:
  return orElse();

}
}
/// A `switch`-like method, using callbacks.
///
/// As opposed to `map`, this offers destructuring.
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case Subclass2(:final field2):
///     return ...;
/// }
/// ```

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String phone,  String? username,  String? email,  int? age,  String role,  String? primaryNeighborhoodId,  String? primaryNeighborhoodName,  String? profilePhotoUrl,  NotificationPreferences notificationPreferences,  DateTime? usernameLastChangedAt,  DateTime? neighborhoodLastChangedAt,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _CurrentUser():
return $default(_that.id,_that.phone,_that.username,_that.email,_that.age,_that.role,_that.primaryNeighborhoodId,_that.primaryNeighborhoodName,_that.profilePhotoUrl,_that.notificationPreferences,_that.usernameLastChangedAt,_that.neighborhoodLastChangedAt,_that.createdAt);case _:
  throw StateError('Unexpected subclass');

}
}
/// A variant of `when` that fallback to returning `null`
///
/// It is equivalent to doing:
/// ```dart
/// switch (sealedClass) {
///   case Subclass(:final field):
///     return ...;
///   case _:
///     return null;
/// }
/// ```

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String phone,  String? username,  String? email,  int? age,  String role,  String? primaryNeighborhoodId,  String? primaryNeighborhoodName,  String? profilePhotoUrl,  NotificationPreferences notificationPreferences,  DateTime? usernameLastChangedAt,  DateTime? neighborhoodLastChangedAt,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _CurrentUser() when $default != null:
return $default(_that.id,_that.phone,_that.username,_that.email,_that.age,_that.role,_that.primaryNeighborhoodId,_that.primaryNeighborhoodName,_that.profilePhotoUrl,_that.notificationPreferences,_that.usernameLastChangedAt,_that.neighborhoodLastChangedAt,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CurrentUser extends CurrentUser {
  const _CurrentUser({required this.id, required this.phone, this.username, this.email, this.age, this.role = 'user', this.primaryNeighborhoodId, this.primaryNeighborhoodName, this.profilePhotoUrl, this.notificationPreferences = const NotificationPreferences(), this.usernameLastChangedAt, this.neighborhoodLastChangedAt, this.createdAt}): super._();
  factory _CurrentUser.fromJson(Map<String, dynamic> json) => _$CurrentUserFromJson(json);

@override final  String id;
@override final  String phone;
@override final  String? username;
@override final  String? email;
@override final  int? age;
/// `user` | `moderator` | `admin` | `super_admin`.
@override@JsonKey() final  String role;
@override final  String? primaryNeighborhoodId;
@override final  String? primaryNeighborhoodName;
/// Göreli URL (`/uploads/...`) — gösterirken `AppImage.url` ile origin eklenir.
@override final  String? profilePhotoUrl;
/// Altı bildirim anahtarı (11.5 — Ayarlar ekranı).
@override@JsonKey() final  NotificationPreferences notificationPreferences;
/// Kullanıcı adının **en son** değiştirildiği an (kayıt anı sayılmaz →
/// ilk değişiklik serbest). 30 günlük kısıt bundan hesaplanır.
@override final  DateTime? usernameLastChangedAt;
/// Birincil mahallenin en son değiştirildiği an (aynı kural).
@override final  DateTime? neighborhoodLastChangedAt;
@override final  DateTime? createdAt;

/// Create a copy of CurrentUser
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CurrentUserCopyWith<_CurrentUser> get copyWith => __$CurrentUserCopyWithImpl<_CurrentUser>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CurrentUserToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _CurrentUser&&(identical(other.id, id) || other.id == id)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.username, username) || other.username == username)&&(identical(other.email, email) || other.email == email)&&(identical(other.age, age) || other.age == age)&&(identical(other.role, role) || other.role == role)&&(identical(other.primaryNeighborhoodId, primaryNeighborhoodId) || other.primaryNeighborhoodId == primaryNeighborhoodId)&&(identical(other.primaryNeighborhoodName, primaryNeighborhoodName) || other.primaryNeighborhoodName == primaryNeighborhoodName)&&(identical(other.profilePhotoUrl, profilePhotoUrl) || other.profilePhotoUrl == profilePhotoUrl)&&(identical(other.notificationPreferences, notificationPreferences) || other.notificationPreferences == notificationPreferences)&&(identical(other.usernameLastChangedAt, usernameLastChangedAt) || other.usernameLastChangedAt == usernameLastChangedAt)&&(identical(other.neighborhoodLastChangedAt, neighborhoodLastChangedAt) || other.neighborhoodLastChangedAt == neighborhoodLastChangedAt)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,phone,username,email,age,role,primaryNeighborhoodId,primaryNeighborhoodName,profilePhotoUrl,notificationPreferences,usernameLastChangedAt,neighborhoodLastChangedAt,createdAt);

@override
String toString() {
  return 'CurrentUser(id: $id, phone: $phone, username: $username, email: $email, age: $age, role: $role, primaryNeighborhoodId: $primaryNeighborhoodId, primaryNeighborhoodName: $primaryNeighborhoodName, profilePhotoUrl: $profilePhotoUrl, notificationPreferences: $notificationPreferences, usernameLastChangedAt: $usernameLastChangedAt, neighborhoodLastChangedAt: $neighborhoodLastChangedAt, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$CurrentUserCopyWith<$Res> implements $CurrentUserCopyWith<$Res> {
  factory _$CurrentUserCopyWith(_CurrentUser value, $Res Function(_CurrentUser) _then) = __$CurrentUserCopyWithImpl;
@override @useResult
$Res call({
 String id, String phone, String? username, String? email, int? age, String role, String? primaryNeighborhoodId, String? primaryNeighborhoodName, String? profilePhotoUrl, NotificationPreferences notificationPreferences, DateTime? usernameLastChangedAt, DateTime? neighborhoodLastChangedAt, DateTime? createdAt
});


@override $NotificationPreferencesCopyWith<$Res> get notificationPreferences;

}
/// @nodoc
class __$CurrentUserCopyWithImpl<$Res>
    implements _$CurrentUserCopyWith<$Res> {
  __$CurrentUserCopyWithImpl(this._self, this._then);

  final _CurrentUser _self;
  final $Res Function(_CurrentUser) _then;

/// Create a copy of CurrentUser
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? phone = null,Object? username = freezed,Object? email = freezed,Object? age = freezed,Object? role = null,Object? primaryNeighborhoodId = freezed,Object? primaryNeighborhoodName = freezed,Object? profilePhotoUrl = freezed,Object? notificationPreferences = null,Object? usernameLastChangedAt = freezed,Object? neighborhoodLastChangedAt = freezed,Object? createdAt = freezed,}) {
  return _then(_CurrentUser(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,phone: null == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String,username: freezed == username ? _self.username : username // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,age: freezed == age ? _self.age : age // ignore: cast_nullable_to_non_nullable
as int?,role: null == role ? _self.role : role // ignore: cast_nullable_to_non_nullable
as String,primaryNeighborhoodId: freezed == primaryNeighborhoodId ? _self.primaryNeighborhoodId : primaryNeighborhoodId // ignore: cast_nullable_to_non_nullable
as String?,primaryNeighborhoodName: freezed == primaryNeighborhoodName ? _self.primaryNeighborhoodName : primaryNeighborhoodName // ignore: cast_nullable_to_non_nullable
as String?,profilePhotoUrl: freezed == profilePhotoUrl ? _self.profilePhotoUrl : profilePhotoUrl // ignore: cast_nullable_to_non_nullable
as String?,notificationPreferences: null == notificationPreferences ? _self.notificationPreferences : notificationPreferences // ignore: cast_nullable_to_non_nullable
as NotificationPreferences,usernameLastChangedAt: freezed == usernameLastChangedAt ? _self.usernameLastChangedAt : usernameLastChangedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,neighborhoodLastChangedAt: freezed == neighborhoodLastChangedAt ? _self.neighborhoodLastChangedAt : neighborhoodLastChangedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

/// Create a copy of CurrentUser
/// with the given fields replaced by the non-null parameter values.
@override
@pragma('vm:prefer-inline')
$NotificationPreferencesCopyWith<$Res> get notificationPreferences {
  
  return $NotificationPreferencesCopyWith<$Res>(_self.notificationPreferences, (value) {
    return _then(_self.copyWith(notificationPreferences: value));
  });
}
}

// dart format on
