// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'duty_schedule.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$DutySchedule {

 String get id; DateTime get dutyDate; String get startTime; String get endTime; String get pharmacyId; String get pharmacyName; String? get source;
/// Create a copy of DutySchedule
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DutyScheduleCopyWith<DutySchedule> get copyWith => _$DutyScheduleCopyWithImpl<DutySchedule>(this as DutySchedule, _$identity);

  /// Serializes this DutySchedule to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DutySchedule&&(identical(other.id, id) || other.id == id)&&(identical(other.dutyDate, dutyDate) || other.dutyDate == dutyDate)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.pharmacyId, pharmacyId) || other.pharmacyId == pharmacyId)&&(identical(other.pharmacyName, pharmacyName) || other.pharmacyName == pharmacyName)&&(identical(other.source, source) || other.source == source));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,dutyDate,startTime,endTime,pharmacyId,pharmacyName,source);

@override
String toString() {
  return 'DutySchedule(id: $id, dutyDate: $dutyDate, startTime: $startTime, endTime: $endTime, pharmacyId: $pharmacyId, pharmacyName: $pharmacyName, source: $source)';
}


}

/// @nodoc
abstract mixin class $DutyScheduleCopyWith<$Res>  {
  factory $DutyScheduleCopyWith(DutySchedule value, $Res Function(DutySchedule) _then) = _$DutyScheduleCopyWithImpl;
@useResult
$Res call({
 String id, DateTime dutyDate, String startTime, String endTime, String pharmacyId, String pharmacyName, String? source
});




}
/// @nodoc
class _$DutyScheduleCopyWithImpl<$Res>
    implements $DutyScheduleCopyWith<$Res> {
  _$DutyScheduleCopyWithImpl(this._self, this._then);

  final DutySchedule _self;
  final $Res Function(DutySchedule) _then;

/// Create a copy of DutySchedule
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? dutyDate = null,Object? startTime = null,Object? endTime = null,Object? pharmacyId = null,Object? pharmacyName = null,Object? source = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,dutyDate: null == dutyDate ? _self.dutyDate : dutyDate // ignore: cast_nullable_to_non_nullable
as DateTime,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as String,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as String,pharmacyId: null == pharmacyId ? _self.pharmacyId : pharmacyId // ignore: cast_nullable_to_non_nullable
as String,pharmacyName: null == pharmacyName ? _self.pharmacyName : pharmacyName // ignore: cast_nullable_to_non_nullable
as String,source: freezed == source ? _self.source : source // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [DutySchedule].
extension DutySchedulePatterns on DutySchedule {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DutySchedule value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DutySchedule() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DutySchedule value)  $default,){
final _that = this;
switch (_that) {
case _DutySchedule():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DutySchedule value)?  $default,){
final _that = this;
switch (_that) {
case _DutySchedule() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  DateTime dutyDate,  String startTime,  String endTime,  String pharmacyId,  String pharmacyName,  String? source)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DutySchedule() when $default != null:
return $default(_that.id,_that.dutyDate,_that.startTime,_that.endTime,_that.pharmacyId,_that.pharmacyName,_that.source);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  DateTime dutyDate,  String startTime,  String endTime,  String pharmacyId,  String pharmacyName,  String? source)  $default,) {final _that = this;
switch (_that) {
case _DutySchedule():
return $default(_that.id,_that.dutyDate,_that.startTime,_that.endTime,_that.pharmacyId,_that.pharmacyName,_that.source);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  DateTime dutyDate,  String startTime,  String endTime,  String pharmacyId,  String pharmacyName,  String? source)?  $default,) {final _that = this;
switch (_that) {
case _DutySchedule() when $default != null:
return $default(_that.id,_that.dutyDate,_that.startTime,_that.endTime,_that.pharmacyId,_that.pharmacyName,_that.source);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DutySchedule extends DutySchedule {
  const _DutySchedule({required this.id, required this.dutyDate, this.startTime = '', this.endTime = '', required this.pharmacyId, required this.pharmacyName, this.source}): super._();
  factory _DutySchedule.fromJson(Map<String, dynamic> json) => _$DutyScheduleFromJson(json);

@override final  String id;
@override final  DateTime dutyDate;
@override@JsonKey() final  String startTime;
@override@JsonKey() final  String endTime;
@override final  String pharmacyId;
@override final  String pharmacyName;
@override final  String? source;

/// Create a copy of DutySchedule
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DutyScheduleCopyWith<_DutySchedule> get copyWith => __$DutyScheduleCopyWithImpl<_DutySchedule>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DutyScheduleToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _DutySchedule&&(identical(other.id, id) || other.id == id)&&(identical(other.dutyDate, dutyDate) || other.dutyDate == dutyDate)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.pharmacyId, pharmacyId) || other.pharmacyId == pharmacyId)&&(identical(other.pharmacyName, pharmacyName) || other.pharmacyName == pharmacyName)&&(identical(other.source, source) || other.source == source));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,dutyDate,startTime,endTime,pharmacyId,pharmacyName,source);

@override
String toString() {
  return 'DutySchedule(id: $id, dutyDate: $dutyDate, startTime: $startTime, endTime: $endTime, pharmacyId: $pharmacyId, pharmacyName: $pharmacyName, source: $source)';
}


}

/// @nodoc
abstract mixin class _$DutyScheduleCopyWith<$Res> implements $DutyScheduleCopyWith<$Res> {
  factory _$DutyScheduleCopyWith(_DutySchedule value, $Res Function(_DutySchedule) _then) = __$DutyScheduleCopyWithImpl;
@override @useResult
$Res call({
 String id, DateTime dutyDate, String startTime, String endTime, String pharmacyId, String pharmacyName, String? source
});




}
/// @nodoc
class __$DutyScheduleCopyWithImpl<$Res>
    implements _$DutyScheduleCopyWith<$Res> {
  __$DutyScheduleCopyWithImpl(this._self, this._then);

  final _DutySchedule _self;
  final $Res Function(_DutySchedule) _then;

/// Create a copy of DutySchedule
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? dutyDate = null,Object? startTime = null,Object? endTime = null,Object? pharmacyId = null,Object? pharmacyName = null,Object? source = freezed,}) {
  return _then(_DutySchedule(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,dutyDate: null == dutyDate ? _self.dutyDate : dutyDate // ignore: cast_nullable_to_non_nullable
as DateTime,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as String,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as String,pharmacyId: null == pharmacyId ? _self.pharmacyId : pharmacyId // ignore: cast_nullable_to_non_nullable
as String,pharmacyName: null == pharmacyName ? _self.pharmacyName : pharmacyName // ignore: cast_nullable_to_non_nullable
as String,source: freezed == source ? _self.source : source // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on
