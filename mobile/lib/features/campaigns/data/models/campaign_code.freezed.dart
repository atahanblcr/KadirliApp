// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'campaign_code.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$CampaignCode {

 String get code; DateTime get viewedAt;
/// Create a copy of CampaignCode
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CampaignCodeCopyWith<CampaignCode> get copyWith => _$CampaignCodeCopyWithImpl<CampaignCode>(this as CampaignCode, _$identity);

  /// Serializes this CampaignCode to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CampaignCode&&(identical(other.code, code) || other.code == code)&&(identical(other.viewedAt, viewedAt) || other.viewedAt == viewedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,code,viewedAt);

@override
String toString() {
  return 'CampaignCode(code: $code, viewedAt: $viewedAt)';
}


}

/// @nodoc
abstract mixin class $CampaignCodeCopyWith<$Res>  {
  factory $CampaignCodeCopyWith(CampaignCode value, $Res Function(CampaignCode) _then) = _$CampaignCodeCopyWithImpl;
@useResult
$Res call({
 String code, DateTime viewedAt
});




}
/// @nodoc
class _$CampaignCodeCopyWithImpl<$Res>
    implements $CampaignCodeCopyWith<$Res> {
  _$CampaignCodeCopyWithImpl(this._self, this._then);

  final CampaignCode _self;
  final $Res Function(CampaignCode) _then;

/// Create a copy of CampaignCode
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? code = null,Object? viewedAt = null,}) {
  return _then(_self.copyWith(
code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,viewedAt: null == viewedAt ? _self.viewedAt : viewedAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}

}


/// Adds pattern-matching-related methods to [CampaignCode].
extension CampaignCodePatterns on CampaignCode {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CampaignCode value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CampaignCode() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CampaignCode value)  $default,){
final _that = this;
switch (_that) {
case _CampaignCode():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CampaignCode value)?  $default,){
final _that = this;
switch (_that) {
case _CampaignCode() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String code,  DateTime viewedAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CampaignCode() when $default != null:
return $default(_that.code,_that.viewedAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String code,  DateTime viewedAt)  $default,) {final _that = this;
switch (_that) {
case _CampaignCode():
return $default(_that.code,_that.viewedAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String code,  DateTime viewedAt)?  $default,) {final _that = this;
switch (_that) {
case _CampaignCode() when $default != null:
return $default(_that.code,_that.viewedAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CampaignCode implements CampaignCode {
  const _CampaignCode({required this.code, required this.viewedAt});
  factory _CampaignCode.fromJson(Map<String, dynamic> json) => _$CampaignCodeFromJson(json);

@override final  String code;
@override final  DateTime viewedAt;

/// Create a copy of CampaignCode
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CampaignCodeCopyWith<_CampaignCode> get copyWith => __$CampaignCodeCopyWithImpl<_CampaignCode>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CampaignCodeToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _CampaignCode&&(identical(other.code, code) || other.code == code)&&(identical(other.viewedAt, viewedAt) || other.viewedAt == viewedAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,code,viewedAt);

@override
String toString() {
  return 'CampaignCode(code: $code, viewedAt: $viewedAt)';
}


}

/// @nodoc
abstract mixin class _$CampaignCodeCopyWith<$Res> implements $CampaignCodeCopyWith<$Res> {
  factory _$CampaignCodeCopyWith(_CampaignCode value, $Res Function(_CampaignCode) _then) = __$CampaignCodeCopyWithImpl;
@override @useResult
$Res call({
 String code, DateTime viewedAt
});




}
/// @nodoc
class __$CampaignCodeCopyWithImpl<$Res>
    implements _$CampaignCodeCopyWith<$Res> {
  __$CampaignCodeCopyWithImpl(this._self, this._then);

  final _CampaignCode _self;
  final $Res Function(_CampaignCode) _then;

/// Create a copy of CampaignCode
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? code = null,Object? viewedAt = null,}) {
  return _then(_CampaignCode(
code: null == code ? _self.code : code // ignore: cast_nullable_to_non_nullable
as String,viewedAt: null == viewedAt ? _self.viewedAt : viewedAt // ignore: cast_nullable_to_non_nullable
as DateTime,
  ));
}


}

// dart format on
