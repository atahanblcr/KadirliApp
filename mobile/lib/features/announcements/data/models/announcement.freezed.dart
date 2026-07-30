// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'announcement.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Announcement {

 String get id; String get title; String get body; String? get typeId; String? get typeName; int get priority; String get status;/// Yayına çıkış anı — liste sıralaması ve "2 saat önce" bunun üzerinden.
 DateTime? get sentAt; DateTime? get scheduledFor; DateTime? get visibleUntil; DateTime? get createdAt;/// Göreli olabilir → `AppImage.url` ile mutlaklaştırılır.
 String? get imageUrl; String? get source; String? get sourceUrl; bool get hasLink; String? get externalLink; bool get hasLocation; double? get latitude; double? get longitude; String? get locationName;
/// Create a copy of Announcement
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AnnouncementCopyWith<Announcement> get copyWith => _$AnnouncementCopyWithImpl<Announcement>(this as Announcement, _$identity);

  /// Serializes this Announcement to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Announcement&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.body, body) || other.body == body)&&(identical(other.typeId, typeId) || other.typeId == typeId)&&(identical(other.typeName, typeName) || other.typeName == typeName)&&(identical(other.priority, priority) || other.priority == priority)&&(identical(other.status, status) || other.status == status)&&(identical(other.sentAt, sentAt) || other.sentAt == sentAt)&&(identical(other.scheduledFor, scheduledFor) || other.scheduledFor == scheduledFor)&&(identical(other.visibleUntil, visibleUntil) || other.visibleUntil == visibleUntil)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.imageUrl, imageUrl) || other.imageUrl == imageUrl)&&(identical(other.source, source) || other.source == source)&&(identical(other.sourceUrl, sourceUrl) || other.sourceUrl == sourceUrl)&&(identical(other.hasLink, hasLink) || other.hasLink == hasLink)&&(identical(other.externalLink, externalLink) || other.externalLink == externalLink)&&(identical(other.hasLocation, hasLocation) || other.hasLocation == hasLocation)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.locationName, locationName) || other.locationName == locationName));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,title,body,typeId,typeName,priority,status,sentAt,scheduledFor,visibleUntil,createdAt,imageUrl,source,sourceUrl,hasLink,externalLink,hasLocation,latitude,longitude,locationName]);

@override
String toString() {
  return 'Announcement(id: $id, title: $title, body: $body, typeId: $typeId, typeName: $typeName, priority: $priority, status: $status, sentAt: $sentAt, scheduledFor: $scheduledFor, visibleUntil: $visibleUntil, createdAt: $createdAt, imageUrl: $imageUrl, source: $source, sourceUrl: $sourceUrl, hasLink: $hasLink, externalLink: $externalLink, hasLocation: $hasLocation, latitude: $latitude, longitude: $longitude, locationName: $locationName)';
}


}

/// @nodoc
abstract mixin class $AnnouncementCopyWith<$Res>  {
  factory $AnnouncementCopyWith(Announcement value, $Res Function(Announcement) _then) = _$AnnouncementCopyWithImpl;
@useResult
$Res call({
 String id, String title, String body, String? typeId, String? typeName, int priority, String status, DateTime? sentAt, DateTime? scheduledFor, DateTime? visibleUntil, DateTime? createdAt, String? imageUrl, String? source, String? sourceUrl, bool hasLink, String? externalLink, bool hasLocation, double? latitude, double? longitude, String? locationName
});




}
/// @nodoc
class _$AnnouncementCopyWithImpl<$Res>
    implements $AnnouncementCopyWith<$Res> {
  _$AnnouncementCopyWithImpl(this._self, this._then);

  final Announcement _self;
  final $Res Function(Announcement) _then;

/// Create a copy of Announcement
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? body = null,Object? typeId = freezed,Object? typeName = freezed,Object? priority = null,Object? status = null,Object? sentAt = freezed,Object? scheduledFor = freezed,Object? visibleUntil = freezed,Object? createdAt = freezed,Object? imageUrl = freezed,Object? source = freezed,Object? sourceUrl = freezed,Object? hasLink = null,Object? externalLink = freezed,Object? hasLocation = null,Object? latitude = freezed,Object? longitude = freezed,Object? locationName = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,typeId: freezed == typeId ? _self.typeId : typeId // ignore: cast_nullable_to_non_nullable
as String?,typeName: freezed == typeName ? _self.typeName : typeName // ignore: cast_nullable_to_non_nullable
as String?,priority: null == priority ? _self.priority : priority // ignore: cast_nullable_to_non_nullable
as int,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,sentAt: freezed == sentAt ? _self.sentAt : sentAt // ignore: cast_nullable_to_non_nullable
as DateTime?,scheduledFor: freezed == scheduledFor ? _self.scheduledFor : scheduledFor // ignore: cast_nullable_to_non_nullable
as DateTime?,visibleUntil: freezed == visibleUntil ? _self.visibleUntil : visibleUntil // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,imageUrl: freezed == imageUrl ? _self.imageUrl : imageUrl // ignore: cast_nullable_to_non_nullable
as String?,source: freezed == source ? _self.source : source // ignore: cast_nullable_to_non_nullable
as String?,sourceUrl: freezed == sourceUrl ? _self.sourceUrl : sourceUrl // ignore: cast_nullable_to_non_nullable
as String?,hasLink: null == hasLink ? _self.hasLink : hasLink // ignore: cast_nullable_to_non_nullable
as bool,externalLink: freezed == externalLink ? _self.externalLink : externalLink // ignore: cast_nullable_to_non_nullable
as String?,hasLocation: null == hasLocation ? _self.hasLocation : hasLocation // ignore: cast_nullable_to_non_nullable
as bool,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,locationName: freezed == locationName ? _self.locationName : locationName // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [Announcement].
extension AnnouncementPatterns on Announcement {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Announcement value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Announcement() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Announcement value)  $default,){
final _that = this;
switch (_that) {
case _Announcement():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Announcement value)?  $default,){
final _that = this;
switch (_that) {
case _Announcement() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String body,  String? typeId,  String? typeName,  int priority,  String status,  DateTime? sentAt,  DateTime? scheduledFor,  DateTime? visibleUntil,  DateTime? createdAt,  String? imageUrl,  String? source,  String? sourceUrl,  bool hasLink,  String? externalLink,  bool hasLocation,  double? latitude,  double? longitude,  String? locationName)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Announcement() when $default != null:
return $default(_that.id,_that.title,_that.body,_that.typeId,_that.typeName,_that.priority,_that.status,_that.sentAt,_that.scheduledFor,_that.visibleUntil,_that.createdAt,_that.imageUrl,_that.source,_that.sourceUrl,_that.hasLink,_that.externalLink,_that.hasLocation,_that.latitude,_that.longitude,_that.locationName);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String body,  String? typeId,  String? typeName,  int priority,  String status,  DateTime? sentAt,  DateTime? scheduledFor,  DateTime? visibleUntil,  DateTime? createdAt,  String? imageUrl,  String? source,  String? sourceUrl,  bool hasLink,  String? externalLink,  bool hasLocation,  double? latitude,  double? longitude,  String? locationName)  $default,) {final _that = this;
switch (_that) {
case _Announcement():
return $default(_that.id,_that.title,_that.body,_that.typeId,_that.typeName,_that.priority,_that.status,_that.sentAt,_that.scheduledFor,_that.visibleUntil,_that.createdAt,_that.imageUrl,_that.source,_that.sourceUrl,_that.hasLink,_that.externalLink,_that.hasLocation,_that.latitude,_that.longitude,_that.locationName);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String body,  String? typeId,  String? typeName,  int priority,  String status,  DateTime? sentAt,  DateTime? scheduledFor,  DateTime? visibleUntil,  DateTime? createdAt,  String? imageUrl,  String? source,  String? sourceUrl,  bool hasLink,  String? externalLink,  bool hasLocation,  double? latitude,  double? longitude,  String? locationName)?  $default,) {final _that = this;
switch (_that) {
case _Announcement() when $default != null:
return $default(_that.id,_that.title,_that.body,_that.typeId,_that.typeName,_that.priority,_that.status,_that.sentAt,_that.scheduledFor,_that.visibleUntil,_that.createdAt,_that.imageUrl,_that.source,_that.sourceUrl,_that.hasLink,_that.externalLink,_that.hasLocation,_that.latitude,_that.longitude,_that.locationName);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Announcement extends Announcement {
  const _Announcement({required this.id, required this.title, this.body = '', this.typeId, this.typeName, this.priority = 0, this.status = '', this.sentAt, this.scheduledFor, this.visibleUntil, this.createdAt, this.imageUrl, this.source, this.sourceUrl, this.hasLink = false, this.externalLink, this.hasLocation = false, this.latitude, this.longitude, this.locationName}): super._();
  factory _Announcement.fromJson(Map<String, dynamic> json) => _$AnnouncementFromJson(json);

@override final  String id;
@override final  String title;
@override@JsonKey() final  String body;
@override final  String? typeId;
@override final  String? typeName;
@override@JsonKey() final  int priority;
@override@JsonKey() final  String status;
/// Yayına çıkış anı — liste sıralaması ve "2 saat önce" bunun üzerinden.
@override final  DateTime? sentAt;
@override final  DateTime? scheduledFor;
@override final  DateTime? visibleUntil;
@override final  DateTime? createdAt;
/// Göreli olabilir → `AppImage.url` ile mutlaklaştırılır.
@override final  String? imageUrl;
@override final  String? source;
@override final  String? sourceUrl;
@override@JsonKey() final  bool hasLink;
@override final  String? externalLink;
@override@JsonKey() final  bool hasLocation;
@override final  double? latitude;
@override final  double? longitude;
@override final  String? locationName;

/// Create a copy of Announcement
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AnnouncementCopyWith<_Announcement> get copyWith => __$AnnouncementCopyWithImpl<_Announcement>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AnnouncementToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Announcement&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.body, body) || other.body == body)&&(identical(other.typeId, typeId) || other.typeId == typeId)&&(identical(other.typeName, typeName) || other.typeName == typeName)&&(identical(other.priority, priority) || other.priority == priority)&&(identical(other.status, status) || other.status == status)&&(identical(other.sentAt, sentAt) || other.sentAt == sentAt)&&(identical(other.scheduledFor, scheduledFor) || other.scheduledFor == scheduledFor)&&(identical(other.visibleUntil, visibleUntil) || other.visibleUntil == visibleUntil)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.imageUrl, imageUrl) || other.imageUrl == imageUrl)&&(identical(other.source, source) || other.source == source)&&(identical(other.sourceUrl, sourceUrl) || other.sourceUrl == sourceUrl)&&(identical(other.hasLink, hasLink) || other.hasLink == hasLink)&&(identical(other.externalLink, externalLink) || other.externalLink == externalLink)&&(identical(other.hasLocation, hasLocation) || other.hasLocation == hasLocation)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.locationName, locationName) || other.locationName == locationName));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hashAll([runtimeType,id,title,body,typeId,typeName,priority,status,sentAt,scheduledFor,visibleUntil,createdAt,imageUrl,source,sourceUrl,hasLink,externalLink,hasLocation,latitude,longitude,locationName]);

@override
String toString() {
  return 'Announcement(id: $id, title: $title, body: $body, typeId: $typeId, typeName: $typeName, priority: $priority, status: $status, sentAt: $sentAt, scheduledFor: $scheduledFor, visibleUntil: $visibleUntil, createdAt: $createdAt, imageUrl: $imageUrl, source: $source, sourceUrl: $sourceUrl, hasLink: $hasLink, externalLink: $externalLink, hasLocation: $hasLocation, latitude: $latitude, longitude: $longitude, locationName: $locationName)';
}


}

/// @nodoc
abstract mixin class _$AnnouncementCopyWith<$Res> implements $AnnouncementCopyWith<$Res> {
  factory _$AnnouncementCopyWith(_Announcement value, $Res Function(_Announcement) _then) = __$AnnouncementCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String body, String? typeId, String? typeName, int priority, String status, DateTime? sentAt, DateTime? scheduledFor, DateTime? visibleUntil, DateTime? createdAt, String? imageUrl, String? source, String? sourceUrl, bool hasLink, String? externalLink, bool hasLocation, double? latitude, double? longitude, String? locationName
});




}
/// @nodoc
class __$AnnouncementCopyWithImpl<$Res>
    implements _$AnnouncementCopyWith<$Res> {
  __$AnnouncementCopyWithImpl(this._self, this._then);

  final _Announcement _self;
  final $Res Function(_Announcement) _then;

/// Create a copy of Announcement
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? body = null,Object? typeId = freezed,Object? typeName = freezed,Object? priority = null,Object? status = null,Object? sentAt = freezed,Object? scheduledFor = freezed,Object? visibleUntil = freezed,Object? createdAt = freezed,Object? imageUrl = freezed,Object? source = freezed,Object? sourceUrl = freezed,Object? hasLink = null,Object? externalLink = freezed,Object? hasLocation = null,Object? latitude = freezed,Object? longitude = freezed,Object? locationName = freezed,}) {
  return _then(_Announcement(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,typeId: freezed == typeId ? _self.typeId : typeId // ignore: cast_nullable_to_non_nullable
as String?,typeName: freezed == typeName ? _self.typeName : typeName // ignore: cast_nullable_to_non_nullable
as String?,priority: null == priority ? _self.priority : priority // ignore: cast_nullable_to_non_nullable
as int,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,sentAt: freezed == sentAt ? _self.sentAt : sentAt // ignore: cast_nullable_to_non_nullable
as DateTime?,scheduledFor: freezed == scheduledFor ? _self.scheduledFor : scheduledFor // ignore: cast_nullable_to_non_nullable
as DateTime?,visibleUntil: freezed == visibleUntil ? _self.visibleUntil : visibleUntil // ignore: cast_nullable_to_non_nullable
as DateTime?,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,imageUrl: freezed == imageUrl ? _self.imageUrl : imageUrl // ignore: cast_nullable_to_non_nullable
as String?,source: freezed == source ? _self.source : source // ignore: cast_nullable_to_non_nullable
as String?,sourceUrl: freezed == sourceUrl ? _self.sourceUrl : sourceUrl // ignore: cast_nullable_to_non_nullable
as String?,hasLink: null == hasLink ? _self.hasLink : hasLink // ignore: cast_nullable_to_non_nullable
as bool,externalLink: freezed == externalLink ? _self.externalLink : externalLink // ignore: cast_nullable_to_non_nullable
as String?,hasLocation: null == hasLocation ? _self.hasLocation : hasLocation // ignore: cast_nullable_to_non_nullable
as bool,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,locationName: freezed == locationName ? _self.locationName : locationName // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on
