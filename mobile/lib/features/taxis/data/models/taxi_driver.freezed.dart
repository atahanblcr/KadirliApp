// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'taxi_driver.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$TaxiDriver {

 String get id; String? get userId; String get name; String get phone; String? get plaka; String? get vehicleInfo; bool get isVerified; bool get isActive;
/// Create a copy of TaxiDriver
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$TaxiDriverCopyWith<TaxiDriver> get copyWith => _$TaxiDriverCopyWithImpl<TaxiDriver>(this as TaxiDriver, _$identity);

  /// Serializes this TaxiDriver to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is TaxiDriver&&(identical(other.id, id) || other.id == id)&&(identical(other.userId, userId) || other.userId == userId)&&(identical(other.name, name) || other.name == name)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.plaka, plaka) || other.plaka == plaka)&&(identical(other.vehicleInfo, vehicleInfo) || other.vehicleInfo == vehicleInfo)&&(identical(other.isVerified, isVerified) || other.isVerified == isVerified)&&(identical(other.isActive, isActive) || other.isActive == isActive));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,userId,name,phone,plaka,vehicleInfo,isVerified,isActive);

@override
String toString() {
  return 'TaxiDriver(id: $id, userId: $userId, name: $name, phone: $phone, plaka: $plaka, vehicleInfo: $vehicleInfo, isVerified: $isVerified, isActive: $isActive)';
}


}

/// @nodoc
abstract mixin class $TaxiDriverCopyWith<$Res>  {
  factory $TaxiDriverCopyWith(TaxiDriver value, $Res Function(TaxiDriver) _then) = _$TaxiDriverCopyWithImpl;
@useResult
$Res call({
 String id, String? userId, String name, String phone, String? plaka, String? vehicleInfo, bool isVerified, bool isActive
});




}
/// @nodoc
class _$TaxiDriverCopyWithImpl<$Res>
    implements $TaxiDriverCopyWith<$Res> {
  _$TaxiDriverCopyWithImpl(this._self, this._then);

  final TaxiDriver _self;
  final $Res Function(TaxiDriver) _then;

/// Create a copy of TaxiDriver
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? userId = freezed,Object? name = null,Object? phone = null,Object? plaka = freezed,Object? vehicleInfo = freezed,Object? isVerified = null,Object? isActive = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,userId: freezed == userId ? _self.userId : userId // ignore: cast_nullable_to_non_nullable
as String?,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,phone: null == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String,plaka: freezed == plaka ? _self.plaka : plaka // ignore: cast_nullable_to_non_nullable
as String?,vehicleInfo: freezed == vehicleInfo ? _self.vehicleInfo : vehicleInfo // ignore: cast_nullable_to_non_nullable
as String?,isVerified: null == isVerified ? _self.isVerified : isVerified // ignore: cast_nullable_to_non_nullable
as bool,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [TaxiDriver].
extension TaxiDriverPatterns on TaxiDriver {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _TaxiDriver value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _TaxiDriver() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _TaxiDriver value)  $default,){
final _that = this;
switch (_that) {
case _TaxiDriver():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _TaxiDriver value)?  $default,){
final _that = this;
switch (_that) {
case _TaxiDriver() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String? userId,  String name,  String phone,  String? plaka,  String? vehicleInfo,  bool isVerified,  bool isActive)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _TaxiDriver() when $default != null:
return $default(_that.id,_that.userId,_that.name,_that.phone,_that.plaka,_that.vehicleInfo,_that.isVerified,_that.isActive);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String? userId,  String name,  String phone,  String? plaka,  String? vehicleInfo,  bool isVerified,  bool isActive)  $default,) {final _that = this;
switch (_that) {
case _TaxiDriver():
return $default(_that.id,_that.userId,_that.name,_that.phone,_that.plaka,_that.vehicleInfo,_that.isVerified,_that.isActive);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String? userId,  String name,  String phone,  String? plaka,  String? vehicleInfo,  bool isVerified,  bool isActive)?  $default,) {final _that = this;
switch (_that) {
case _TaxiDriver() when $default != null:
return $default(_that.id,_that.userId,_that.name,_that.phone,_that.plaka,_that.vehicleInfo,_that.isVerified,_that.isActive);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _TaxiDriver extends TaxiDriver {
  const _TaxiDriver({required this.id, this.userId, required this.name, this.phone = '', this.plaka, this.vehicleInfo, this.isVerified = true, this.isActive = true}): super._();
  factory _TaxiDriver.fromJson(Map<String, dynamic> json) => _$TaxiDriverFromJson(json);

@override final  String id;
@override final  String? userId;
@override final  String name;
@override@JsonKey() final  String phone;
@override final  String? plaka;
@override final  String? vehicleInfo;
@override@JsonKey() final  bool isVerified;
@override@JsonKey() final  bool isActive;

/// Create a copy of TaxiDriver
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$TaxiDriverCopyWith<_TaxiDriver> get copyWith => __$TaxiDriverCopyWithImpl<_TaxiDriver>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$TaxiDriverToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _TaxiDriver&&(identical(other.id, id) || other.id == id)&&(identical(other.userId, userId) || other.userId == userId)&&(identical(other.name, name) || other.name == name)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.plaka, plaka) || other.plaka == plaka)&&(identical(other.vehicleInfo, vehicleInfo) || other.vehicleInfo == vehicleInfo)&&(identical(other.isVerified, isVerified) || other.isVerified == isVerified)&&(identical(other.isActive, isActive) || other.isActive == isActive));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,userId,name,phone,plaka,vehicleInfo,isVerified,isActive);

@override
String toString() {
  return 'TaxiDriver(id: $id, userId: $userId, name: $name, phone: $phone, plaka: $plaka, vehicleInfo: $vehicleInfo, isVerified: $isVerified, isActive: $isActive)';
}


}

/// @nodoc
abstract mixin class _$TaxiDriverCopyWith<$Res> implements $TaxiDriverCopyWith<$Res> {
  factory _$TaxiDriverCopyWith(_TaxiDriver value, $Res Function(_TaxiDriver) _then) = __$TaxiDriverCopyWithImpl;
@override @useResult
$Res call({
 String id, String? userId, String name, String phone, String? plaka, String? vehicleInfo, bool isVerified, bool isActive
});




}
/// @nodoc
class __$TaxiDriverCopyWithImpl<$Res>
    implements _$TaxiDriverCopyWith<$Res> {
  __$TaxiDriverCopyWithImpl(this._self, this._then);

  final _TaxiDriver _self;
  final $Res Function(_TaxiDriver) _then;

/// Create a copy of TaxiDriver
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? userId = freezed,Object? name = null,Object? phone = null,Object? plaka = freezed,Object? vehicleInfo = freezed,Object? isVerified = null,Object? isActive = null,}) {
  return _then(_TaxiDriver(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,userId: freezed == userId ? _self.userId : userId // ignore: cast_nullable_to_non_nullable
as String?,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,phone: null == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String,plaka: freezed == plaka ? _self.plaka : plaka // ignore: cast_nullable_to_non_nullable
as String?,vehicleInfo: freezed == vehicleInfo ? _self.vehicleInfo : vehicleInfo // ignore: cast_nullable_to_non_nullable
as String?,isVerified: null == isVerified ? _self.isVerified : isVerified // ignore: cast_nullable_to_non_nullable
as bool,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
