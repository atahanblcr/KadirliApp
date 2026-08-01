// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'event.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Event {

 String get id; String get title; String get description; String? get categoryId; String? get categoryName; DateTime get eventDate; String get eventTime; String? get venueName; String? get address; double? get latitude; double? get longitude; bool get hasLocation; String? get organizer; double? get ticketPrice; bool get isFree; bool get isLocal; String? get coverImageId; String? get coverImageUrl; String get status; DateTime? get createdAt;
/// Create a copy of Event
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$EventCopyWith<Event> get copyWith => _$EventCopyWithImpl<Event>(this as Event, _$identity);

  /// Serializes this Event to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Event&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.eventDate, eventDate) || other.eventDate == eventDate)&&(identical(other.eventTime, eventTime) || other.eventTime == eventTime)&&(identical(other.venueName, venueName) || other.venueName == venueName)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.hasLocation, hasLocation) || other.hasLocation == hasLocation)&&(identical(other.organizer, organizer) || other.organizer == organizer)&&(identical(other.ticketPrice, ticketPrice) || other.ticketPrice == ticketPrice)&&(identical(other.isFree, isFree) || other.isFree == isFree)&&(identical(other.isLocal, isLocal) || other.isLocal == isLocal)&&(identical(other.coverImageId, coverImageId) || other.coverImageId == coverImageId)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.status, status) || other.status == status)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,title,description,categoryId,categoryName,eventDate,eventTime,venueName,address,latitude,longitude,hasLocation,organizer,ticketPrice,isFree,isLocal,coverImageId,coverImageUrl,status,createdAt]);

@override
String toString() {
  return 'Event(id: $id, title: $title, description: $description, categoryId: $categoryId, categoryName: $categoryName, eventDate: $eventDate, eventTime: $eventTime, venueName: $venueName, address: $address, latitude: $latitude, longitude: $longitude, hasLocation: $hasLocation, organizer: $organizer, ticketPrice: $ticketPrice, isFree: $isFree, isLocal: $isLocal, coverImageId: $coverImageId, coverImageUrl: $coverImageUrl, status: $status, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $EventCopyWith<$Res>  {
  factory $EventCopyWith(Event value, $Res Function(Event) _then) = _$EventCopyWithImpl;
@useResult
$Res call({
 String id, String title, String description, String? categoryId, String? categoryName, DateTime eventDate, String eventTime, String? venueName, String? address, double? latitude, double? longitude, bool hasLocation, String? organizer, double? ticketPrice, bool isFree, bool isLocal, String? coverImageId, String? coverImageUrl, String status, DateTime? createdAt
});




}
/// @nodoc
class _$EventCopyWithImpl<$Res>
    implements $EventCopyWith<$Res> {
  _$EventCopyWithImpl(this._self, this._then);

  final Event _self;
  final $Res Function(Event) _then;

/// Create a copy of Event
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? description = null,Object? categoryId = freezed,Object? categoryName = freezed,Object? eventDate = null,Object? eventTime = null,Object? venueName = freezed,Object? address = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? hasLocation = null,Object? organizer = freezed,Object? ticketPrice = freezed,Object? isFree = null,Object? isLocal = null,Object? coverImageId = freezed,Object? coverImageUrl = freezed,Object? status = null,Object? createdAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: null == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String,categoryId: freezed == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,eventDate: null == eventDate ? _self.eventDate : eventDate // ignore: cast_nullable_to_non_nullable
as DateTime,eventTime: null == eventTime ? _self.eventTime : eventTime // ignore: cast_nullable_to_non_nullable
as String,venueName: freezed == venueName ? _self.venueName : venueName // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,hasLocation: null == hasLocation ? _self.hasLocation : hasLocation // ignore: cast_nullable_to_non_nullable
as bool,organizer: freezed == organizer ? _self.organizer : organizer // ignore: cast_nullable_to_non_nullable
as String?,ticketPrice: freezed == ticketPrice ? _self.ticketPrice : ticketPrice // ignore: cast_nullable_to_non_nullable
as double?,isFree: null == isFree ? _self.isFree : isFree // ignore: cast_nullable_to_non_nullable
as bool,isLocal: null == isLocal ? _self.isLocal : isLocal // ignore: cast_nullable_to_non_nullable
as bool,coverImageId: freezed == coverImageId ? _self.coverImageId : coverImageId // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Event].
extension EventPatterns on Event {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Event value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Event() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Event value)  $default,){
final _that = this;
switch (_that) {
case _Event():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Event value)?  $default,){
final _that = this;
switch (_that) {
case _Event() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String description,  String? categoryId,  String? categoryName,  DateTime eventDate,  String eventTime,  String? venueName,  String? address,  double? latitude,  double? longitude,  bool hasLocation,  String? organizer,  double? ticketPrice,  bool isFree,  bool isLocal,  String? coverImageId,  String? coverImageUrl,  String status,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Event() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.categoryId,_that.categoryName,_that.eventDate,_that.eventTime,_that.venueName,_that.address,_that.latitude,_that.longitude,_that.hasLocation,_that.organizer,_that.ticketPrice,_that.isFree,_that.isLocal,_that.coverImageId,_that.coverImageUrl,_that.status,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String description,  String? categoryId,  String? categoryName,  DateTime eventDate,  String eventTime,  String? venueName,  String? address,  double? latitude,  double? longitude,  bool hasLocation,  String? organizer,  double? ticketPrice,  bool isFree,  bool isLocal,  String? coverImageId,  String? coverImageUrl,  String status,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _Event():
return $default(_that.id,_that.title,_that.description,_that.categoryId,_that.categoryName,_that.eventDate,_that.eventTime,_that.venueName,_that.address,_that.latitude,_that.longitude,_that.hasLocation,_that.organizer,_that.ticketPrice,_that.isFree,_that.isLocal,_that.coverImageId,_that.coverImageUrl,_that.status,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String description,  String? categoryId,  String? categoryName,  DateTime eventDate,  String eventTime,  String? venueName,  String? address,  double? latitude,  double? longitude,  bool hasLocation,  String? organizer,  double? ticketPrice,  bool isFree,  bool isLocal,  String? coverImageId,  String? coverImageUrl,  String status,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _Event() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.categoryId,_that.categoryName,_that.eventDate,_that.eventTime,_that.venueName,_that.address,_that.latitude,_that.longitude,_that.hasLocation,_that.organizer,_that.ticketPrice,_that.isFree,_that.isLocal,_that.coverImageId,_that.coverImageUrl,_that.status,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Event extends Event {
  const _Event({required this.id, required this.title, this.description = '', this.categoryId, this.categoryName, required this.eventDate, this.eventTime = '00:00:00', this.venueName, this.address, this.latitude, this.longitude, this.hasLocation = false, this.organizer, this.ticketPrice, this.isFree = false, this.isLocal = true, this.coverImageId, this.coverImageUrl, this.status = 'approved', this.createdAt}): super._();
  factory _Event.fromJson(Map<String, dynamic> json) => _$EventFromJson(json);

@override final  String id;
@override final  String title;
@override@JsonKey() final  String description;
@override final  String? categoryId;
@override final  String? categoryName;
@override final  DateTime eventDate;
@override@JsonKey() final  String eventTime;
@override final  String? venueName;
@override final  String? address;
@override final  double? latitude;
@override final  double? longitude;
@override@JsonKey() final  bool hasLocation;
@override final  String? organizer;
@override final  double? ticketPrice;
@override@JsonKey() final  bool isFree;
@override@JsonKey() final  bool isLocal;
@override final  String? coverImageId;
@override final  String? coverImageUrl;
@override@JsonKey() final  String status;
@override final  DateTime? createdAt;

/// Create a copy of Event
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$EventCopyWith<_Event> get copyWith => __$EventCopyWithImpl<_Event>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$EventToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Event&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.eventDate, eventDate) || other.eventDate == eventDate)&&(identical(other.eventTime, eventTime) || other.eventTime == eventTime)&&(identical(other.venueName, venueName) || other.venueName == venueName)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.hasLocation, hasLocation) || other.hasLocation == hasLocation)&&(identical(other.organizer, organizer) || other.organizer == organizer)&&(identical(other.ticketPrice, ticketPrice) || other.ticketPrice == ticketPrice)&&(identical(other.isFree, isFree) || other.isFree == isFree)&&(identical(other.isLocal, isLocal) || other.isLocal == isLocal)&&(identical(other.coverImageId, coverImageId) || other.coverImageId == coverImageId)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.status, status) || other.status == status)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,title,description,categoryId,categoryName,eventDate,eventTime,venueName,address,latitude,longitude,hasLocation,organizer,ticketPrice,isFree,isLocal,coverImageId,coverImageUrl,status,createdAt]);

@override
String toString() {
  return 'Event(id: $id, title: $title, description: $description, categoryId: $categoryId, categoryName: $categoryName, eventDate: $eventDate, eventTime: $eventTime, venueName: $venueName, address: $address, latitude: $latitude, longitude: $longitude, hasLocation: $hasLocation, organizer: $organizer, ticketPrice: $ticketPrice, isFree: $isFree, isLocal: $isLocal, coverImageId: $coverImageId, coverImageUrl: $coverImageUrl, status: $status, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$EventCopyWith<$Res> implements $EventCopyWith<$Res> {
  factory _$EventCopyWith(_Event value, $Res Function(_Event) _then) = __$EventCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String description, String? categoryId, String? categoryName, DateTime eventDate, String eventTime, String? venueName, String? address, double? latitude, double? longitude, bool hasLocation, String? organizer, double? ticketPrice, bool isFree, bool isLocal, String? coverImageId, String? coverImageUrl, String status, DateTime? createdAt
});




}
/// @nodoc
class __$EventCopyWithImpl<$Res>
    implements _$EventCopyWith<$Res> {
  __$EventCopyWithImpl(this._self, this._then);

  final _Event _self;
  final $Res Function(_Event) _then;

/// Create a copy of Event
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? description = null,Object? categoryId = freezed,Object? categoryName = freezed,Object? eventDate = null,Object? eventTime = null,Object? venueName = freezed,Object? address = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? hasLocation = null,Object? organizer = freezed,Object? ticketPrice = freezed,Object? isFree = null,Object? isLocal = null,Object? coverImageId = freezed,Object? coverImageUrl = freezed,Object? status = null,Object? createdAt = freezed,}) {
  return _then(_Event(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: null == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String,categoryId: freezed == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,eventDate: null == eventDate ? _self.eventDate : eventDate // ignore: cast_nullable_to_non_nullable
as DateTime,eventTime: null == eventTime ? _self.eventTime : eventTime // ignore: cast_nullable_to_non_nullable
as String,venueName: freezed == venueName ? _self.venueName : venueName // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,hasLocation: null == hasLocation ? _self.hasLocation : hasLocation // ignore: cast_nullable_to_non_nullable
as bool,organizer: freezed == organizer ? _self.organizer : organizer // ignore: cast_nullable_to_non_nullable
as String?,ticketPrice: freezed == ticketPrice ? _self.ticketPrice : ticketPrice // ignore: cast_nullable_to_non_nullable
as double?,isFree: null == isFree ? _self.isFree : isFree // ignore: cast_nullable_to_non_nullable
as bool,isLocal: null == isLocal ? _self.isLocal : isLocal // ignore: cast_nullable_to_non_nullable
as bool,coverImageId: freezed == coverImageId ? _self.coverImageId : coverImageId // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
