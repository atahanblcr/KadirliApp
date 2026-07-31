// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'ad_extend_result.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$AdExtendResult {

 String get adId; String get status; DateTime get expiresAt; int get extensionCount; int get maxExtensions; int get remainingExtensions;
/// Create a copy of AdExtendResult
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AdExtendResultCopyWith<AdExtendResult> get copyWith => _$AdExtendResultCopyWithImpl<AdExtendResult>(this as AdExtendResult, _$identity);

  /// Serializes this AdExtendResult to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AdExtendResult&&(identical(other.adId, adId) || other.adId == adId)&&(identical(other.status, status) || other.status == status)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&(identical(other.extensionCount, extensionCount) || other.extensionCount == extensionCount)&&(identical(other.maxExtensions, maxExtensions) || other.maxExtensions == maxExtensions)&&(identical(other.remainingExtensions, remainingExtensions) || other.remainingExtensions == remainingExtensions));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,adId,status,expiresAt,extensionCount,maxExtensions,remainingExtensions);

@override
String toString() {
  return 'AdExtendResult(adId: $adId, status: $status, expiresAt: $expiresAt, extensionCount: $extensionCount, maxExtensions: $maxExtensions, remainingExtensions: $remainingExtensions)';
}


}

/// @nodoc
abstract mixin class $AdExtendResultCopyWith<$Res>  {
  factory $AdExtendResultCopyWith(AdExtendResult value, $Res Function(AdExtendResult) _then) = _$AdExtendResultCopyWithImpl;
@useResult
$Res call({
 String adId, String status, DateTime expiresAt, int extensionCount, int maxExtensions, int remainingExtensions
});




}
/// @nodoc
class _$AdExtendResultCopyWithImpl<$Res>
    implements $AdExtendResultCopyWith<$Res> {
  _$AdExtendResultCopyWithImpl(this._self, this._then);

  final AdExtendResult _self;
  final $Res Function(AdExtendResult) _then;

/// Create a copy of AdExtendResult
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? adId = null,Object? status = null,Object? expiresAt = null,Object? extensionCount = null,Object? maxExtensions = null,Object? remainingExtensions = null,}) {
  return _then(_self.copyWith(
adId: null == adId ? _self.adId : adId // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,extensionCount: null == extensionCount ? _self.extensionCount : extensionCount // ignore: cast_nullable_to_non_nullable
as int,maxExtensions: null == maxExtensions ? _self.maxExtensions : maxExtensions // ignore: cast_nullable_to_non_nullable
as int,remainingExtensions: null == remainingExtensions ? _self.remainingExtensions : remainingExtensions // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [AdExtendResult].
extension AdExtendResultPatterns on AdExtendResult {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AdExtendResult value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AdExtendResult() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AdExtendResult value)  $default,){
final _that = this;
switch (_that) {
case _AdExtendResult():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AdExtendResult value)?  $default,){
final _that = this;
switch (_that) {
case _AdExtendResult() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String adId,  String status,  DateTime expiresAt,  int extensionCount,  int maxExtensions,  int remainingExtensions)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AdExtendResult() when $default != null:
return $default(_that.adId,_that.status,_that.expiresAt,_that.extensionCount,_that.maxExtensions,_that.remainingExtensions);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String adId,  String status,  DateTime expiresAt,  int extensionCount,  int maxExtensions,  int remainingExtensions)  $default,) {final _that = this;
switch (_that) {
case _AdExtendResult():
return $default(_that.adId,_that.status,_that.expiresAt,_that.extensionCount,_that.maxExtensions,_that.remainingExtensions);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String adId,  String status,  DateTime expiresAt,  int extensionCount,  int maxExtensions,  int remainingExtensions)?  $default,) {final _that = this;
switch (_that) {
case _AdExtendResult() when $default != null:
return $default(_that.adId,_that.status,_that.expiresAt,_that.extensionCount,_that.maxExtensions,_that.remainingExtensions);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AdExtendResult implements AdExtendResult {
  const _AdExtendResult({required this.adId, this.status = 'approved', required this.expiresAt, this.extensionCount = 0, this.maxExtensions = 0, this.remainingExtensions = 0});
  factory _AdExtendResult.fromJson(Map<String, dynamic> json) => _$AdExtendResultFromJson(json);

@override final  String adId;
@override@JsonKey() final  String status;
@override final  DateTime expiresAt;
@override@JsonKey() final  int extensionCount;
@override@JsonKey() final  int maxExtensions;
@override@JsonKey() final  int remainingExtensions;

/// Create a copy of AdExtendResult
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AdExtendResultCopyWith<_AdExtendResult> get copyWith => __$AdExtendResultCopyWithImpl<_AdExtendResult>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AdExtendResultToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AdExtendResult&&(identical(other.adId, adId) || other.adId == adId)&&(identical(other.status, status) || other.status == status)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&(identical(other.extensionCount, extensionCount) || other.extensionCount == extensionCount)&&(identical(other.maxExtensions, maxExtensions) || other.maxExtensions == maxExtensions)&&(identical(other.remainingExtensions, remainingExtensions) || other.remainingExtensions == remainingExtensions));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,adId,status,expiresAt,extensionCount,maxExtensions,remainingExtensions);

@override
String toString() {
  return 'AdExtendResult(adId: $adId, status: $status, expiresAt: $expiresAt, extensionCount: $extensionCount, maxExtensions: $maxExtensions, remainingExtensions: $remainingExtensions)';
}


}

/// @nodoc
abstract mixin class _$AdExtendResultCopyWith<$Res> implements $AdExtendResultCopyWith<$Res> {
  factory _$AdExtendResultCopyWith(_AdExtendResult value, $Res Function(_AdExtendResult) _then) = __$AdExtendResultCopyWithImpl;
@override @useResult
$Res call({
 String adId, String status, DateTime expiresAt, int extensionCount, int maxExtensions, int remainingExtensions
});




}
/// @nodoc
class __$AdExtendResultCopyWithImpl<$Res>
    implements _$AdExtendResultCopyWith<$Res> {
  __$AdExtendResultCopyWithImpl(this._self, this._then);

  final _AdExtendResult _self;
  final $Res Function(_AdExtendResult) _then;

/// Create a copy of AdExtendResult
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? adId = null,Object? status = null,Object? expiresAt = null,Object? extensionCount = null,Object? maxExtensions = null,Object? remainingExtensions = null,}) {
  return _then(_AdExtendResult(
adId: null == adId ? _self.adId : adId // ignore: cast_nullable_to_non_nullable
as String,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,extensionCount: null == extensionCount ? _self.extensionCount : extensionCount // ignore: cast_nullable_to_non_nullable
as int,maxExtensions: null == maxExtensions ? _self.maxExtensions : maxExtensions // ignore: cast_nullable_to_non_nullable
as int,remainingExtensions: null == remainingExtensions ? _self.remainingExtensions : remainingExtensions // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
