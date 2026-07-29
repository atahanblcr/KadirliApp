// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'verify_otp_result.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$VerifyOtpResult {

 bool get isNewUser;/// Yalnız yeni kullanıcıda: 30 dk ömürlü kayıt token'ı. **Saklanmaz** —
/// kayıt ekranı bitene kadar bellekte taşınır.
 String? get tempToken; String? get accessToken; String? get refreshToken; int? get expiresIn;
/// Create a copy of VerifyOtpResult
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$VerifyOtpResultCopyWith<VerifyOtpResult> get copyWith => _$VerifyOtpResultCopyWithImpl<VerifyOtpResult>(this as VerifyOtpResult, _$identity);

  /// Serializes this VerifyOtpResult to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is VerifyOtpResult&&(identical(other.isNewUser, isNewUser) || other.isNewUser == isNewUser)&&(identical(other.tempToken, tempToken) || other.tempToken == tempToken)&&(identical(other.accessToken, accessToken) || other.accessToken == accessToken)&&(identical(other.refreshToken, refreshToken) || other.refreshToken == refreshToken)&&(identical(other.expiresIn, expiresIn) || other.expiresIn == expiresIn));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,isNewUser,tempToken,accessToken,refreshToken,expiresIn);

@override
String toString() {
  return 'VerifyOtpResult(isNewUser: $isNewUser, tempToken: $tempToken, accessToken: $accessToken, refreshToken: $refreshToken, expiresIn: $expiresIn)';
}


}

/// @nodoc
abstract mixin class $VerifyOtpResultCopyWith<$Res>  {
  factory $VerifyOtpResultCopyWith(VerifyOtpResult value, $Res Function(VerifyOtpResult) _then) = _$VerifyOtpResultCopyWithImpl;
@useResult
$Res call({
 bool isNewUser, String? tempToken, String? accessToken, String? refreshToken, int? expiresIn
});




}
/// @nodoc
class _$VerifyOtpResultCopyWithImpl<$Res>
    implements $VerifyOtpResultCopyWith<$Res> {
  _$VerifyOtpResultCopyWithImpl(this._self, this._then);

  final VerifyOtpResult _self;
  final $Res Function(VerifyOtpResult) _then;

/// Create a copy of VerifyOtpResult
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? isNewUser = null,Object? tempToken = freezed,Object? accessToken = freezed,Object? refreshToken = freezed,Object? expiresIn = freezed,}) {
  return _then(_self.copyWith(
isNewUser: null == isNewUser ? _self.isNewUser : isNewUser // ignore: cast_nullable_to_non_nullable
as bool,tempToken: freezed == tempToken ? _self.tempToken : tempToken // ignore: cast_nullable_to_non_nullable
as String?,accessToken: freezed == accessToken ? _self.accessToken : accessToken // ignore: cast_nullable_to_non_nullable
as String?,refreshToken: freezed == refreshToken ? _self.refreshToken : refreshToken // ignore: cast_nullable_to_non_nullable
as String?,expiresIn: freezed == expiresIn ? _self.expiresIn : expiresIn // ignore: cast_nullable_to_non_nullable
as int?,
  ));
}

}


/// Adds pattern-matching-related methods to [VerifyOtpResult].
extension VerifyOtpResultPatterns on VerifyOtpResult {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _VerifyOtpResult value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _VerifyOtpResult() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _VerifyOtpResult value)  $default,){
final _that = this;
switch (_that) {
case _VerifyOtpResult():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _VerifyOtpResult value)?  $default,){
final _that = this;
switch (_that) {
case _VerifyOtpResult() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( bool isNewUser,  String? tempToken,  String? accessToken,  String? refreshToken,  int? expiresIn)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _VerifyOtpResult() when $default != null:
return $default(_that.isNewUser,_that.tempToken,_that.accessToken,_that.refreshToken,_that.expiresIn);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( bool isNewUser,  String? tempToken,  String? accessToken,  String? refreshToken,  int? expiresIn)  $default,) {final _that = this;
switch (_that) {
case _VerifyOtpResult():
return $default(_that.isNewUser,_that.tempToken,_that.accessToken,_that.refreshToken,_that.expiresIn);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( bool isNewUser,  String? tempToken,  String? accessToken,  String? refreshToken,  int? expiresIn)?  $default,) {final _that = this;
switch (_that) {
case _VerifyOtpResult() when $default != null:
return $default(_that.isNewUser,_that.tempToken,_that.accessToken,_that.refreshToken,_that.expiresIn);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _VerifyOtpResult extends VerifyOtpResult {
  const _VerifyOtpResult({this.isNewUser = false, this.tempToken, this.accessToken, this.refreshToken, this.expiresIn}): super._();
  factory _VerifyOtpResult.fromJson(Map<String, dynamic> json) => _$VerifyOtpResultFromJson(json);

@override@JsonKey() final  bool isNewUser;
/// Yalnız yeni kullanıcıda: 30 dk ömürlü kayıt token'ı. **Saklanmaz** —
/// kayıt ekranı bitene kadar bellekte taşınır.
@override final  String? tempToken;
@override final  String? accessToken;
@override final  String? refreshToken;
@override final  int? expiresIn;

/// Create a copy of VerifyOtpResult
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$VerifyOtpResultCopyWith<_VerifyOtpResult> get copyWith => __$VerifyOtpResultCopyWithImpl<_VerifyOtpResult>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$VerifyOtpResultToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _VerifyOtpResult&&(identical(other.isNewUser, isNewUser) || other.isNewUser == isNewUser)&&(identical(other.tempToken, tempToken) || other.tempToken == tempToken)&&(identical(other.accessToken, accessToken) || other.accessToken == accessToken)&&(identical(other.refreshToken, refreshToken) || other.refreshToken == refreshToken)&&(identical(other.expiresIn, expiresIn) || other.expiresIn == expiresIn));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,isNewUser,tempToken,accessToken,refreshToken,expiresIn);

@override
String toString() {
  return 'VerifyOtpResult(isNewUser: $isNewUser, tempToken: $tempToken, accessToken: $accessToken, refreshToken: $refreshToken, expiresIn: $expiresIn)';
}


}

/// @nodoc
abstract mixin class _$VerifyOtpResultCopyWith<$Res> implements $VerifyOtpResultCopyWith<$Res> {
  factory _$VerifyOtpResultCopyWith(_VerifyOtpResult value, $Res Function(_VerifyOtpResult) _then) = __$VerifyOtpResultCopyWithImpl;
@override @useResult
$Res call({
 bool isNewUser, String? tempToken, String? accessToken, String? refreshToken, int? expiresIn
});




}
/// @nodoc
class __$VerifyOtpResultCopyWithImpl<$Res>
    implements _$VerifyOtpResultCopyWith<$Res> {
  __$VerifyOtpResultCopyWithImpl(this._self, this._then);

  final _VerifyOtpResult _self;
  final $Res Function(_VerifyOtpResult) _then;

/// Create a copy of VerifyOtpResult
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? isNewUser = null,Object? tempToken = freezed,Object? accessToken = freezed,Object? refreshToken = freezed,Object? expiresIn = freezed,}) {
  return _then(_VerifyOtpResult(
isNewUser: null == isNewUser ? _self.isNewUser : isNewUser // ignore: cast_nullable_to_non_nullable
as bool,tempToken: freezed == tempToken ? _self.tempToken : tempToken // ignore: cast_nullable_to_non_nullable
as String?,accessToken: freezed == accessToken ? _self.accessToken : accessToken // ignore: cast_nullable_to_non_nullable
as String?,refreshToken: freezed == refreshToken ? _self.refreshToken : refreshToken // ignore: cast_nullable_to_non_nullable
as String?,expiresIn: freezed == expiresIn ? _self.expiresIn : expiresIn // ignore: cast_nullable_to_non_nullable
as int?,
  ));
}


}

// dart format on
