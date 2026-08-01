// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'death_notice.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$DeathNotice {

 String get id; String get deceasedName; String? get photoFileId; String? get photoUrl; DateTime get funeralDate; String get funeralTime; String? get cemeteryId; String? get cemeteryName; String? get mosqueId; String? get mosqueName; String? get neighborhoodId; String? get condolenceAddress; double? get condolenceLatitude; double? get condolenceLongitude; bool get hasCondolenceLocation; String? get addedBy; String get status; DateTime? get createdAt;
/// Create a copy of DeathNotice
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$DeathNoticeCopyWith<DeathNotice> get copyWith => _$DeathNoticeCopyWithImpl<DeathNotice>(this as DeathNotice, _$identity);

  /// Serializes this DeathNotice to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is DeathNotice&&(identical(other.id, id) || other.id == id)&&(identical(other.deceasedName, deceasedName) || other.deceasedName == deceasedName)&&(identical(other.photoFileId, photoFileId) || other.photoFileId == photoFileId)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.funeralDate, funeralDate) || other.funeralDate == funeralDate)&&(identical(other.funeralTime, funeralTime) || other.funeralTime == funeralTime)&&(identical(other.cemeteryId, cemeteryId) || other.cemeteryId == cemeteryId)&&(identical(other.cemeteryName, cemeteryName) || other.cemeteryName == cemeteryName)&&(identical(other.mosqueId, mosqueId) || other.mosqueId == mosqueId)&&(identical(other.mosqueName, mosqueName) || other.mosqueName == mosqueName)&&(identical(other.neighborhoodId, neighborhoodId) || other.neighborhoodId == neighborhoodId)&&(identical(other.condolenceAddress, condolenceAddress) || other.condolenceAddress == condolenceAddress)&&(identical(other.condolenceLatitude, condolenceLatitude) || other.condolenceLatitude == condolenceLatitude)&&(identical(other.condolenceLongitude, condolenceLongitude) || other.condolenceLongitude == condolenceLongitude)&&(identical(other.hasCondolenceLocation, hasCondolenceLocation) || other.hasCondolenceLocation == hasCondolenceLocation)&&(identical(other.addedBy, addedBy) || other.addedBy == addedBy)&&(identical(other.status, status) || other.status == status)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,deceasedName,photoFileId,photoUrl,funeralDate,funeralTime,cemeteryId,cemeteryName,mosqueId,mosqueName,neighborhoodId,condolenceAddress,condolenceLatitude,condolenceLongitude,hasCondolenceLocation,addedBy,status,createdAt);

@override
String toString() {
  return 'DeathNotice(id: $id, deceasedName: $deceasedName, photoFileId: $photoFileId, photoUrl: $photoUrl, funeralDate: $funeralDate, funeralTime: $funeralTime, cemeteryId: $cemeteryId, cemeteryName: $cemeteryName, mosqueId: $mosqueId, mosqueName: $mosqueName, neighborhoodId: $neighborhoodId, condolenceAddress: $condolenceAddress, condolenceLatitude: $condolenceLatitude, condolenceLongitude: $condolenceLongitude, hasCondolenceLocation: $hasCondolenceLocation, addedBy: $addedBy, status: $status, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $DeathNoticeCopyWith<$Res>  {
  factory $DeathNoticeCopyWith(DeathNotice value, $Res Function(DeathNotice) _then) = _$DeathNoticeCopyWithImpl;
@useResult
$Res call({
 String id, String deceasedName, String? photoFileId, String? photoUrl, DateTime funeralDate, String funeralTime, String? cemeteryId, String? cemeteryName, String? mosqueId, String? mosqueName, String? neighborhoodId, String? condolenceAddress, double? condolenceLatitude, double? condolenceLongitude, bool hasCondolenceLocation, String? addedBy, String status, DateTime? createdAt
});




}
/// @nodoc
class _$DeathNoticeCopyWithImpl<$Res>
    implements $DeathNoticeCopyWith<$Res> {
  _$DeathNoticeCopyWithImpl(this._self, this._then);

  final DeathNotice _self;
  final $Res Function(DeathNotice) _then;

/// Create a copy of DeathNotice
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? deceasedName = null,Object? photoFileId = freezed,Object? photoUrl = freezed,Object? funeralDate = null,Object? funeralTime = null,Object? cemeteryId = freezed,Object? cemeteryName = freezed,Object? mosqueId = freezed,Object? mosqueName = freezed,Object? neighborhoodId = freezed,Object? condolenceAddress = freezed,Object? condolenceLatitude = freezed,Object? condolenceLongitude = freezed,Object? hasCondolenceLocation = null,Object? addedBy = freezed,Object? status = null,Object? createdAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,deceasedName: null == deceasedName ? _self.deceasedName : deceasedName // ignore: cast_nullable_to_non_nullable
as String,photoFileId: freezed == photoFileId ? _self.photoFileId : photoFileId // ignore: cast_nullable_to_non_nullable
as String?,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,funeralDate: null == funeralDate ? _self.funeralDate : funeralDate // ignore: cast_nullable_to_non_nullable
as DateTime,funeralTime: null == funeralTime ? _self.funeralTime : funeralTime // ignore: cast_nullable_to_non_nullable
as String,cemeteryId: freezed == cemeteryId ? _self.cemeteryId : cemeteryId // ignore: cast_nullable_to_non_nullable
as String?,cemeteryName: freezed == cemeteryName ? _self.cemeteryName : cemeteryName // ignore: cast_nullable_to_non_nullable
as String?,mosqueId: freezed == mosqueId ? _self.mosqueId : mosqueId // ignore: cast_nullable_to_non_nullable
as String?,mosqueName: freezed == mosqueName ? _self.mosqueName : mosqueName // ignore: cast_nullable_to_non_nullable
as String?,neighborhoodId: freezed == neighborhoodId ? _self.neighborhoodId : neighborhoodId // ignore: cast_nullable_to_non_nullable
as String?,condolenceAddress: freezed == condolenceAddress ? _self.condolenceAddress : condolenceAddress // ignore: cast_nullable_to_non_nullable
as String?,condolenceLatitude: freezed == condolenceLatitude ? _self.condolenceLatitude : condolenceLatitude // ignore: cast_nullable_to_non_nullable
as double?,condolenceLongitude: freezed == condolenceLongitude ? _self.condolenceLongitude : condolenceLongitude // ignore: cast_nullable_to_non_nullable
as double?,hasCondolenceLocation: null == hasCondolenceLocation ? _self.hasCondolenceLocation : hasCondolenceLocation // ignore: cast_nullable_to_non_nullable
as bool,addedBy: freezed == addedBy ? _self.addedBy : addedBy // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [DeathNotice].
extension DeathNoticePatterns on DeathNotice {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _DeathNotice value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _DeathNotice() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _DeathNotice value)  $default,){
final _that = this;
switch (_that) {
case _DeathNotice():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _DeathNotice value)?  $default,){
final _that = this;
switch (_that) {
case _DeathNotice() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String deceasedName,  String? photoFileId,  String? photoUrl,  DateTime funeralDate,  String funeralTime,  String? cemeteryId,  String? cemeteryName,  String? mosqueId,  String? mosqueName,  String? neighborhoodId,  String? condolenceAddress,  double? condolenceLatitude,  double? condolenceLongitude,  bool hasCondolenceLocation,  String? addedBy,  String status,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _DeathNotice() when $default != null:
return $default(_that.id,_that.deceasedName,_that.photoFileId,_that.photoUrl,_that.funeralDate,_that.funeralTime,_that.cemeteryId,_that.cemeteryName,_that.mosqueId,_that.mosqueName,_that.neighborhoodId,_that.condolenceAddress,_that.condolenceLatitude,_that.condolenceLongitude,_that.hasCondolenceLocation,_that.addedBy,_that.status,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String deceasedName,  String? photoFileId,  String? photoUrl,  DateTime funeralDate,  String funeralTime,  String? cemeteryId,  String? cemeteryName,  String? mosqueId,  String? mosqueName,  String? neighborhoodId,  String? condolenceAddress,  double? condolenceLatitude,  double? condolenceLongitude,  bool hasCondolenceLocation,  String? addedBy,  String status,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _DeathNotice():
return $default(_that.id,_that.deceasedName,_that.photoFileId,_that.photoUrl,_that.funeralDate,_that.funeralTime,_that.cemeteryId,_that.cemeteryName,_that.mosqueId,_that.mosqueName,_that.neighborhoodId,_that.condolenceAddress,_that.condolenceLatitude,_that.condolenceLongitude,_that.hasCondolenceLocation,_that.addedBy,_that.status,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String deceasedName,  String? photoFileId,  String? photoUrl,  DateTime funeralDate,  String funeralTime,  String? cemeteryId,  String? cemeteryName,  String? mosqueId,  String? mosqueName,  String? neighborhoodId,  String? condolenceAddress,  double? condolenceLatitude,  double? condolenceLongitude,  bool hasCondolenceLocation,  String? addedBy,  String status,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _DeathNotice() when $default != null:
return $default(_that.id,_that.deceasedName,_that.photoFileId,_that.photoUrl,_that.funeralDate,_that.funeralTime,_that.cemeteryId,_that.cemeteryName,_that.mosqueId,_that.mosqueName,_that.neighborhoodId,_that.condolenceAddress,_that.condolenceLatitude,_that.condolenceLongitude,_that.hasCondolenceLocation,_that.addedBy,_that.status,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _DeathNotice extends DeathNotice {
  const _DeathNotice({required this.id, required this.deceasedName, this.photoFileId, this.photoUrl, required this.funeralDate, this.funeralTime = '00:00:00', this.cemeteryId, this.cemeteryName, this.mosqueId, this.mosqueName, this.neighborhoodId, this.condolenceAddress, this.condolenceLatitude, this.condolenceLongitude, this.hasCondolenceLocation = false, this.addedBy, this.status = 'approved', this.createdAt}): super._();
  factory _DeathNotice.fromJson(Map<String, dynamic> json) => _$DeathNoticeFromJson(json);

@override final  String id;
@override final  String deceasedName;
@override final  String? photoFileId;
@override final  String? photoUrl;
@override final  DateTime funeralDate;
@override@JsonKey() final  String funeralTime;
@override final  String? cemeteryId;
@override final  String? cemeteryName;
@override final  String? mosqueId;
@override final  String? mosqueName;
@override final  String? neighborhoodId;
@override final  String? condolenceAddress;
@override final  double? condolenceLatitude;
@override final  double? condolenceLongitude;
@override@JsonKey() final  bool hasCondolenceLocation;
@override final  String? addedBy;
@override@JsonKey() final  String status;
@override final  DateTime? createdAt;

/// Create a copy of DeathNotice
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$DeathNoticeCopyWith<_DeathNotice> get copyWith => __$DeathNoticeCopyWithImpl<_DeathNotice>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$DeathNoticeToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _DeathNotice&&(identical(other.id, id) || other.id == id)&&(identical(other.deceasedName, deceasedName) || other.deceasedName == deceasedName)&&(identical(other.photoFileId, photoFileId) || other.photoFileId == photoFileId)&&(identical(other.photoUrl, photoUrl) || other.photoUrl == photoUrl)&&(identical(other.funeralDate, funeralDate) || other.funeralDate == funeralDate)&&(identical(other.funeralTime, funeralTime) || other.funeralTime == funeralTime)&&(identical(other.cemeteryId, cemeteryId) || other.cemeteryId == cemeteryId)&&(identical(other.cemeteryName, cemeteryName) || other.cemeteryName == cemeteryName)&&(identical(other.mosqueId, mosqueId) || other.mosqueId == mosqueId)&&(identical(other.mosqueName, mosqueName) || other.mosqueName == mosqueName)&&(identical(other.neighborhoodId, neighborhoodId) || other.neighborhoodId == neighborhoodId)&&(identical(other.condolenceAddress, condolenceAddress) || other.condolenceAddress == condolenceAddress)&&(identical(other.condolenceLatitude, condolenceLatitude) || other.condolenceLatitude == condolenceLatitude)&&(identical(other.condolenceLongitude, condolenceLongitude) || other.condolenceLongitude == condolenceLongitude)&&(identical(other.hasCondolenceLocation, hasCondolenceLocation) || other.hasCondolenceLocation == hasCondolenceLocation)&&(identical(other.addedBy, addedBy) || other.addedBy == addedBy)&&(identical(other.status, status) || other.status == status)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,deceasedName,photoFileId,photoUrl,funeralDate,funeralTime,cemeteryId,cemeteryName,mosqueId,mosqueName,neighborhoodId,condolenceAddress,condolenceLatitude,condolenceLongitude,hasCondolenceLocation,addedBy,status,createdAt);

@override
String toString() {
  return 'DeathNotice(id: $id, deceasedName: $deceasedName, photoFileId: $photoFileId, photoUrl: $photoUrl, funeralDate: $funeralDate, funeralTime: $funeralTime, cemeteryId: $cemeteryId, cemeteryName: $cemeteryName, mosqueId: $mosqueId, mosqueName: $mosqueName, neighborhoodId: $neighborhoodId, condolenceAddress: $condolenceAddress, condolenceLatitude: $condolenceLatitude, condolenceLongitude: $condolenceLongitude, hasCondolenceLocation: $hasCondolenceLocation, addedBy: $addedBy, status: $status, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$DeathNoticeCopyWith<$Res> implements $DeathNoticeCopyWith<$Res> {
  factory _$DeathNoticeCopyWith(_DeathNotice value, $Res Function(_DeathNotice) _then) = __$DeathNoticeCopyWithImpl;
@override @useResult
$Res call({
 String id, String deceasedName, String? photoFileId, String? photoUrl, DateTime funeralDate, String funeralTime, String? cemeteryId, String? cemeteryName, String? mosqueId, String? mosqueName, String? neighborhoodId, String? condolenceAddress, double? condolenceLatitude, double? condolenceLongitude, bool hasCondolenceLocation, String? addedBy, String status, DateTime? createdAt
});




}
/// @nodoc
class __$DeathNoticeCopyWithImpl<$Res>
    implements _$DeathNoticeCopyWith<$Res> {
  __$DeathNoticeCopyWithImpl(this._self, this._then);

  final _DeathNotice _self;
  final $Res Function(_DeathNotice) _then;

/// Create a copy of DeathNotice
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? deceasedName = null,Object? photoFileId = freezed,Object? photoUrl = freezed,Object? funeralDate = null,Object? funeralTime = null,Object? cemeteryId = freezed,Object? cemeteryName = freezed,Object? mosqueId = freezed,Object? mosqueName = freezed,Object? neighborhoodId = freezed,Object? condolenceAddress = freezed,Object? condolenceLatitude = freezed,Object? condolenceLongitude = freezed,Object? hasCondolenceLocation = null,Object? addedBy = freezed,Object? status = null,Object? createdAt = freezed,}) {
  return _then(_DeathNotice(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,deceasedName: null == deceasedName ? _self.deceasedName : deceasedName // ignore: cast_nullable_to_non_nullable
as String,photoFileId: freezed == photoFileId ? _self.photoFileId : photoFileId // ignore: cast_nullable_to_non_nullable
as String?,photoUrl: freezed == photoUrl ? _self.photoUrl : photoUrl // ignore: cast_nullable_to_non_nullable
as String?,funeralDate: null == funeralDate ? _self.funeralDate : funeralDate // ignore: cast_nullable_to_non_nullable
as DateTime,funeralTime: null == funeralTime ? _self.funeralTime : funeralTime // ignore: cast_nullable_to_non_nullable
as String,cemeteryId: freezed == cemeteryId ? _self.cemeteryId : cemeteryId // ignore: cast_nullable_to_non_nullable
as String?,cemeteryName: freezed == cemeteryName ? _self.cemeteryName : cemeteryName // ignore: cast_nullable_to_non_nullable
as String?,mosqueId: freezed == mosqueId ? _self.mosqueId : mosqueId // ignore: cast_nullable_to_non_nullable
as String?,mosqueName: freezed == mosqueName ? _self.mosqueName : mosqueName // ignore: cast_nullable_to_non_nullable
as String?,neighborhoodId: freezed == neighborhoodId ? _self.neighborhoodId : neighborhoodId // ignore: cast_nullable_to_non_nullable
as String?,condolenceAddress: freezed == condolenceAddress ? _self.condolenceAddress : condolenceAddress // ignore: cast_nullable_to_non_nullable
as String?,condolenceLatitude: freezed == condolenceLatitude ? _self.condolenceLatitude : condolenceLatitude // ignore: cast_nullable_to_non_nullable
as double?,condolenceLongitude: freezed == condolenceLongitude ? _self.condolenceLongitude : condolenceLongitude // ignore: cast_nullable_to_non_nullable
as double?,hasCondolenceLocation: null == hasCondolenceLocation ? _self.hasCondolenceLocation : hasCondolenceLocation // ignore: cast_nullable_to_non_nullable
as bool,addedBy: freezed == addedBy ? _self.addedBy : addedBy // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
