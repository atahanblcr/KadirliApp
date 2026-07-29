// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'api_meta.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$ApiMeta {

 DateTime? get timestamp; String? get path; String? get traceId;
/// Create a copy of ApiMeta
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$ApiMetaCopyWith<ApiMeta> get copyWith => _$ApiMetaCopyWithImpl<ApiMeta>(this as ApiMeta, _$identity);

  /// Serializes this ApiMeta to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is ApiMeta&&(identical(other.timestamp, timestamp) || other.timestamp == timestamp)&&(identical(other.path, path) || other.path == path)&&(identical(other.traceId, traceId) || other.traceId == traceId));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,timestamp,path,traceId);

@override
String toString() {
  return 'ApiMeta(timestamp: $timestamp, path: $path, traceId: $traceId)';
}


}

/// @nodoc
abstract mixin class $ApiMetaCopyWith<$Res>  {
  factory $ApiMetaCopyWith(ApiMeta value, $Res Function(ApiMeta) _then) = _$ApiMetaCopyWithImpl;
@useResult
$Res call({
 DateTime? timestamp, String? path, String? traceId
});




}
/// @nodoc
class _$ApiMetaCopyWithImpl<$Res>
    implements $ApiMetaCopyWith<$Res> {
  _$ApiMetaCopyWithImpl(this._self, this._then);

  final ApiMeta _self;
  final $Res Function(ApiMeta) _then;

/// Create a copy of ApiMeta
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? timestamp = freezed,Object? path = freezed,Object? traceId = freezed,}) {
  return _then(_self.copyWith(
timestamp: freezed == timestamp ? _self.timestamp : timestamp // ignore: cast_nullable_to_non_nullable
as DateTime?,path: freezed == path ? _self.path : path // ignore: cast_nullable_to_non_nullable
as String?,traceId: freezed == traceId ? _self.traceId : traceId // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [ApiMeta].
extension ApiMetaPatterns on ApiMeta {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _ApiMeta value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _ApiMeta() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _ApiMeta value)  $default,){
final _that = this;
switch (_that) {
case _ApiMeta():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _ApiMeta value)?  $default,){
final _that = this;
switch (_that) {
case _ApiMeta() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( DateTime? timestamp,  String? path,  String? traceId)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _ApiMeta() when $default != null:
return $default(_that.timestamp,_that.path,_that.traceId);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( DateTime? timestamp,  String? path,  String? traceId)  $default,) {final _that = this;
switch (_that) {
case _ApiMeta():
return $default(_that.timestamp,_that.path,_that.traceId);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( DateTime? timestamp,  String? path,  String? traceId)?  $default,) {final _that = this;
switch (_that) {
case _ApiMeta() when $default != null:
return $default(_that.timestamp,_that.path,_that.traceId);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _ApiMeta implements ApiMeta {
  const _ApiMeta({this.timestamp, this.path, this.traceId});
  factory _ApiMeta.fromJson(Map<String, dynamic> json) => _$ApiMetaFromJson(json);

@override final  DateTime? timestamp;
@override final  String? path;
@override final  String? traceId;

/// Create a copy of ApiMeta
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$ApiMetaCopyWith<_ApiMeta> get copyWith => __$ApiMetaCopyWithImpl<_ApiMeta>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$ApiMetaToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _ApiMeta&&(identical(other.timestamp, timestamp) || other.timestamp == timestamp)&&(identical(other.path, path) || other.path == path)&&(identical(other.traceId, traceId) || other.traceId == traceId));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,timestamp,path,traceId);

@override
String toString() {
  return 'ApiMeta(timestamp: $timestamp, path: $path, traceId: $traceId)';
}


}

/// @nodoc
abstract mixin class _$ApiMetaCopyWith<$Res> implements $ApiMetaCopyWith<$Res> {
  factory _$ApiMetaCopyWith(_ApiMeta value, $Res Function(_ApiMeta) _then) = __$ApiMetaCopyWithImpl;
@override @useResult
$Res call({
 DateTime? timestamp, String? path, String? traceId
});




}
/// @nodoc
class __$ApiMetaCopyWithImpl<$Res>
    implements _$ApiMetaCopyWith<$Res> {
  __$ApiMetaCopyWithImpl(this._self, this._then);

  final _ApiMeta _self;
  final $Res Function(_ApiMeta) _then;

/// Create a copy of ApiMeta
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? timestamp = freezed,Object? path = freezed,Object? traceId = freezed,}) {
  return _then(_ApiMeta(
timestamp: freezed == timestamp ? _self.timestamp : timestamp // ignore: cast_nullable_to_non_nullable
as DateTime?,path: freezed == path ? _self.path : path // ignore: cast_nullable_to_non_nullable
as String?,traceId: freezed == traceId ? _self.traceId : traceId // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on
