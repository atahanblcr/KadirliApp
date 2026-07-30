// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'power_outage.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$PowerOutage {

 String get id; String? get neighborhood; DateTime get startTime; DateTime get endTime; String? get reason;
/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$PowerOutageCopyWith<PowerOutage> get copyWith => _$PowerOutageCopyWithImpl<PowerOutage>(this as PowerOutage, _$identity);

  /// Serializes this PowerOutage to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is PowerOutage&&(identical(other.id, id) || other.id == id)&&(identical(other.neighborhood, neighborhood) || other.neighborhood == neighborhood)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.reason, reason) || other.reason == reason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,neighborhood,startTime,endTime,reason);

@override
String toString() {
  return 'PowerOutage(id: $id, neighborhood: $neighborhood, startTime: $startTime, endTime: $endTime, reason: $reason)';
}


}

/// @nodoc
abstract mixin class $PowerOutageCopyWith<$Res>  {
  factory $PowerOutageCopyWith(PowerOutage value, $Res Function(PowerOutage) _then) = _$PowerOutageCopyWithImpl;
@useResult
$Res call({
 String id, String? neighborhood, DateTime startTime, DateTime endTime, String? reason
});




}
/// @nodoc
class _$PowerOutageCopyWithImpl<$Res>
    implements $PowerOutageCopyWith<$Res> {
  _$PowerOutageCopyWithImpl(this._self, this._then);

  final PowerOutage _self;
  final $Res Function(PowerOutage) _then;

/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? neighborhood = freezed,Object? startTime = null,Object? endTime = null,Object? reason = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,neighborhood: freezed == neighborhood ? _self.neighborhood : neighborhood // ignore: cast_nullable_to_non_nullable
as String?,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as DateTime,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as DateTime,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [PowerOutage].
extension PowerOutagePatterns on PowerOutage {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _PowerOutage value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _PowerOutage value)  $default,){
final _that = this;
switch (_that) {
case _PowerOutage():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _PowerOutage value)?  $default,){
final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String? neighborhood,  DateTime startTime,  DateTime endTime,  String? reason)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
return $default(_that.id,_that.neighborhood,_that.startTime,_that.endTime,_that.reason);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String? neighborhood,  DateTime startTime,  DateTime endTime,  String? reason)  $default,) {final _that = this;
switch (_that) {
case _PowerOutage():
return $default(_that.id,_that.neighborhood,_that.startTime,_that.endTime,_that.reason);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String? neighborhood,  DateTime startTime,  DateTime endTime,  String? reason)?  $default,) {final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
return $default(_that.id,_that.neighborhood,_that.startTime,_that.endTime,_that.reason);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _PowerOutage extends PowerOutage {
  const _PowerOutage({required this.id, this.neighborhood, required this.startTime, required this.endTime, this.reason}): super._();
  factory _PowerOutage.fromJson(Map<String, dynamic> json) => _$PowerOutageFromJson(json);

@override final  String id;
@override final  String? neighborhood;
@override final  DateTime startTime;
@override final  DateTime endTime;
@override final  String? reason;

/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$PowerOutageCopyWith<_PowerOutage> get copyWith => __$PowerOutageCopyWithImpl<_PowerOutage>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$PowerOutageToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _PowerOutage&&(identical(other.id, id) || other.id == id)&&(identical(other.neighborhood, neighborhood) || other.neighborhood == neighborhood)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.reason, reason) || other.reason == reason));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,neighborhood,startTime,endTime,reason);

@override
String toString() {
  return 'PowerOutage(id: $id, neighborhood: $neighborhood, startTime: $startTime, endTime: $endTime, reason: $reason)';
}


}

/// @nodoc
abstract mixin class _$PowerOutageCopyWith<$Res> implements $PowerOutageCopyWith<$Res> {
  factory _$PowerOutageCopyWith(_PowerOutage value, $Res Function(_PowerOutage) _then) = __$PowerOutageCopyWithImpl;
@override @useResult
$Res call({
 String id, String? neighborhood, DateTime startTime, DateTime endTime, String? reason
});




}
/// @nodoc
class __$PowerOutageCopyWithImpl<$Res>
    implements _$PowerOutageCopyWith<$Res> {
  __$PowerOutageCopyWithImpl(this._self, this._then);

  final _PowerOutage _self;
  final $Res Function(_PowerOutage) _then;

/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? neighborhood = freezed,Object? startTime = null,Object? endTime = null,Object? reason = freezed,}) {
  return _then(_PowerOutage(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,neighborhood: freezed == neighborhood ? _self.neighborhood : neighborhood // ignore: cast_nullable_to_non_nullable
as String?,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as DateTime,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as DateTime,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on
