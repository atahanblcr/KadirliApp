// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'event_calendar_item.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$EventCalendarItem {

 String get id; String get title; DateTime get eventDate; String get eventTime; String? get venueName; String? get categoryName; String get status;
/// Create a copy of EventCalendarItem
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$EventCalendarItemCopyWith<EventCalendarItem> get copyWith => _$EventCalendarItemCopyWithImpl<EventCalendarItem>(this as EventCalendarItem, _$identity);

  /// Serializes this EventCalendarItem to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is EventCalendarItem&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.eventDate, eventDate) || other.eventDate == eventDate)&&(identical(other.eventTime, eventTime) || other.eventTime == eventTime)&&(identical(other.venueName, venueName) || other.venueName == venueName)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.status, status) || other.status == status));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,eventDate,eventTime,venueName,categoryName,status);

@override
String toString() {
  return 'EventCalendarItem(id: $id, title: $title, eventDate: $eventDate, eventTime: $eventTime, venueName: $venueName, categoryName: $categoryName, status: $status)';
}


}

/// @nodoc
abstract mixin class $EventCalendarItemCopyWith<$Res>  {
  factory $EventCalendarItemCopyWith(EventCalendarItem value, $Res Function(EventCalendarItem) _then) = _$EventCalendarItemCopyWithImpl;
@useResult
$Res call({
 String id, String title, DateTime eventDate, String eventTime, String? venueName, String? categoryName, String status
});




}
/// @nodoc
class _$EventCalendarItemCopyWithImpl<$Res>
    implements $EventCalendarItemCopyWith<$Res> {
  _$EventCalendarItemCopyWithImpl(this._self, this._then);

  final EventCalendarItem _self;
  final $Res Function(EventCalendarItem) _then;

/// Create a copy of EventCalendarItem
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? eventDate = null,Object? eventTime = null,Object? venueName = freezed,Object? categoryName = freezed,Object? status = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,eventDate: null == eventDate ? _self.eventDate : eventDate // ignore: cast_nullable_to_non_nullable
as DateTime,eventTime: null == eventTime ? _self.eventTime : eventTime // ignore: cast_nullable_to_non_nullable
as String,venueName: freezed == venueName ? _self.venueName : venueName // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [EventCalendarItem].
extension EventCalendarItemPatterns on EventCalendarItem {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _EventCalendarItem value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _EventCalendarItem() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _EventCalendarItem value)  $default,){
final _that = this;
switch (_that) {
case _EventCalendarItem():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _EventCalendarItem value)?  $default,){
final _that = this;
switch (_that) {
case _EventCalendarItem() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  DateTime eventDate,  String eventTime,  String? venueName,  String? categoryName,  String status)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _EventCalendarItem() when $default != null:
return $default(_that.id,_that.title,_that.eventDate,_that.eventTime,_that.venueName,_that.categoryName,_that.status);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  DateTime eventDate,  String eventTime,  String? venueName,  String? categoryName,  String status)  $default,) {final _that = this;
switch (_that) {
case _EventCalendarItem():
return $default(_that.id,_that.title,_that.eventDate,_that.eventTime,_that.venueName,_that.categoryName,_that.status);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  DateTime eventDate,  String eventTime,  String? venueName,  String? categoryName,  String status)?  $default,) {final _that = this;
switch (_that) {
case _EventCalendarItem() when $default != null:
return $default(_that.id,_that.title,_that.eventDate,_that.eventTime,_that.venueName,_that.categoryName,_that.status);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _EventCalendarItem extends EventCalendarItem {
  const _EventCalendarItem({required this.id, required this.title, required this.eventDate, this.eventTime = '00:00:00', this.venueName, this.categoryName, this.status = 'approved'}): super._();
  factory _EventCalendarItem.fromJson(Map<String, dynamic> json) => _$EventCalendarItemFromJson(json);

@override final  String id;
@override final  String title;
@override final  DateTime eventDate;
@override@JsonKey() final  String eventTime;
@override final  String? venueName;
@override final  String? categoryName;
@override@JsonKey() final  String status;

/// Create a copy of EventCalendarItem
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$EventCalendarItemCopyWith<_EventCalendarItem> get copyWith => __$EventCalendarItemCopyWithImpl<_EventCalendarItem>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$EventCalendarItemToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _EventCalendarItem&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.eventDate, eventDate) || other.eventDate == eventDate)&&(identical(other.eventTime, eventTime) || other.eventTime == eventTime)&&(identical(other.venueName, venueName) || other.venueName == venueName)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.status, status) || other.status == status));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,eventDate,eventTime,venueName,categoryName,status);

@override
String toString() {
  return 'EventCalendarItem(id: $id, title: $title, eventDate: $eventDate, eventTime: $eventTime, venueName: $venueName, categoryName: $categoryName, status: $status)';
}


}

/// @nodoc
abstract mixin class _$EventCalendarItemCopyWith<$Res> implements $EventCalendarItemCopyWith<$Res> {
  factory _$EventCalendarItemCopyWith(_EventCalendarItem value, $Res Function(_EventCalendarItem) _then) = __$EventCalendarItemCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, DateTime eventDate, String eventTime, String? venueName, String? categoryName, String status
});




}
/// @nodoc
class __$EventCalendarItemCopyWithImpl<$Res>
    implements _$EventCalendarItemCopyWith<$Res> {
  __$EventCalendarItemCopyWithImpl(this._self, this._then);

  final _EventCalendarItem _self;
  final $Res Function(_EventCalendarItem) _then;

/// Create a copy of EventCalendarItem
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? eventDate = null,Object? eventTime = null,Object? venueName = freezed,Object? categoryName = freezed,Object? status = null,}) {
  return _then(_EventCalendarItem(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,eventDate: null == eventDate ? _self.eventDate : eventDate // ignore: cast_nullable_to_non_nullable
as DateTime,eventTime: null == eventTime ? _self.eventTime : eventTime // ignore: cast_nullable_to_non_nullable
as String,venueName: freezed == venueName ? _self.venueName : venueName // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}

// dart format on
