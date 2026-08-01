// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'place.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Place {

 String get id; String get categoryId; String get name; String? get description; String? get address; double get latitude; double get longitude; double? get entranceFee; bool get isFree; String? get openingHours; String? get bestSeason; String? get howToGetThere; double? get distanceFromCenter;/// Ham `jsonb` içeriği — çözümlenmiş hâli için [amenityMap].
@JsonKey(fromJson: _rawAmenities) String? get amenities; String? get coverImageId; String? get coverImageUrl; bool get isActive; DateTime? get createdAt;
/// Create a copy of Place
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$PlaceCopyWith<Place> get copyWith => _$PlaceCopyWithImpl<Place>(this as Place, _$identity);

  /// Serializes this Place to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Place&&(identical(other.id, id) || other.id == id)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.name, name) || other.name == name)&&(identical(other.description, description) || other.description == description)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.entranceFee, entranceFee) || other.entranceFee == entranceFee)&&(identical(other.isFree, isFree) || other.isFree == isFree)&&(identical(other.openingHours, openingHours) || other.openingHours == openingHours)&&(identical(other.bestSeason, bestSeason) || other.bestSeason == bestSeason)&&(identical(other.howToGetThere, howToGetThere) || other.howToGetThere == howToGetThere)&&(identical(other.distanceFromCenter, distanceFromCenter) || other.distanceFromCenter == distanceFromCenter)&&(identical(other.amenities, amenities) || other.amenities == amenities)&&(identical(other.coverImageId, coverImageId) || other.coverImageId == coverImageId)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.isActive, isActive) || other.isActive == isActive)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,categoryId,name,description,address,latitude,longitude,entranceFee,isFree,openingHours,bestSeason,howToGetThere,distanceFromCenter,amenities,coverImageId,coverImageUrl,isActive,createdAt);

@override
String toString() {
  return 'Place(id: $id, categoryId: $categoryId, name: $name, description: $description, address: $address, latitude: $latitude, longitude: $longitude, entranceFee: $entranceFee, isFree: $isFree, openingHours: $openingHours, bestSeason: $bestSeason, howToGetThere: $howToGetThere, distanceFromCenter: $distanceFromCenter, amenities: $amenities, coverImageId: $coverImageId, coverImageUrl: $coverImageUrl, isActive: $isActive, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $PlaceCopyWith<$Res>  {
  factory $PlaceCopyWith(Place value, $Res Function(Place) _then) = _$PlaceCopyWithImpl;
@useResult
$Res call({
 String id, String categoryId, String name, String? description, String? address, double latitude, double longitude, double? entranceFee, bool isFree, String? openingHours, String? bestSeason, String? howToGetThere, double? distanceFromCenter,@JsonKey(fromJson: _rawAmenities) String? amenities, String? coverImageId, String? coverImageUrl, bool isActive, DateTime? createdAt
});




}
/// @nodoc
class _$PlaceCopyWithImpl<$Res>
    implements $PlaceCopyWith<$Res> {
  _$PlaceCopyWithImpl(this._self, this._then);

  final Place _self;
  final $Res Function(Place) _then;

/// Create a copy of Place
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? categoryId = null,Object? name = null,Object? description = freezed,Object? address = freezed,Object? latitude = null,Object? longitude = null,Object? entranceFee = freezed,Object? isFree = null,Object? openingHours = freezed,Object? bestSeason = freezed,Object? howToGetThere = freezed,Object? distanceFromCenter = freezed,Object? amenities = freezed,Object? coverImageId = freezed,Object? coverImageUrl = freezed,Object? isActive = null,Object? createdAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,categoryId: null == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: null == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double,longitude: null == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double,entranceFee: freezed == entranceFee ? _self.entranceFee : entranceFee // ignore: cast_nullable_to_non_nullable
as double?,isFree: null == isFree ? _self.isFree : isFree // ignore: cast_nullable_to_non_nullable
as bool,openingHours: freezed == openingHours ? _self.openingHours : openingHours // ignore: cast_nullable_to_non_nullable
as String?,bestSeason: freezed == bestSeason ? _self.bestSeason : bestSeason // ignore: cast_nullable_to_non_nullable
as String?,howToGetThere: freezed == howToGetThere ? _self.howToGetThere : howToGetThere // ignore: cast_nullable_to_non_nullable
as String?,distanceFromCenter: freezed == distanceFromCenter ? _self.distanceFromCenter : distanceFromCenter // ignore: cast_nullable_to_non_nullable
as double?,amenities: freezed == amenities ? _self.amenities : amenities // ignore: cast_nullable_to_non_nullable
as String?,coverImageId: freezed == coverImageId ? _self.coverImageId : coverImageId // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Place].
extension PlacePatterns on Place {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Place value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Place() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Place value)  $default,){
final _that = this;
switch (_that) {
case _Place():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Place value)?  $default,){
final _that = this;
switch (_that) {
case _Place() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String categoryId,  String name,  String? description,  String? address,  double latitude,  double longitude,  double? entranceFee,  bool isFree,  String? openingHours,  String? bestSeason,  String? howToGetThere,  double? distanceFromCenter, @JsonKey(fromJson: _rawAmenities)  String? amenities,  String? coverImageId,  String? coverImageUrl,  bool isActive,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Place() when $default != null:
return $default(_that.id,_that.categoryId,_that.name,_that.description,_that.address,_that.latitude,_that.longitude,_that.entranceFee,_that.isFree,_that.openingHours,_that.bestSeason,_that.howToGetThere,_that.distanceFromCenter,_that.amenities,_that.coverImageId,_that.coverImageUrl,_that.isActive,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String categoryId,  String name,  String? description,  String? address,  double latitude,  double longitude,  double? entranceFee,  bool isFree,  String? openingHours,  String? bestSeason,  String? howToGetThere,  double? distanceFromCenter, @JsonKey(fromJson: _rawAmenities)  String? amenities,  String? coverImageId,  String? coverImageUrl,  bool isActive,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _Place():
return $default(_that.id,_that.categoryId,_that.name,_that.description,_that.address,_that.latitude,_that.longitude,_that.entranceFee,_that.isFree,_that.openingHours,_that.bestSeason,_that.howToGetThere,_that.distanceFromCenter,_that.amenities,_that.coverImageId,_that.coverImageUrl,_that.isActive,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String categoryId,  String name,  String? description,  String? address,  double latitude,  double longitude,  double? entranceFee,  bool isFree,  String? openingHours,  String? bestSeason,  String? howToGetThere,  double? distanceFromCenter, @JsonKey(fromJson: _rawAmenities)  String? amenities,  String? coverImageId,  String? coverImageUrl,  bool isActive,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _Place() when $default != null:
return $default(_that.id,_that.categoryId,_that.name,_that.description,_that.address,_that.latitude,_that.longitude,_that.entranceFee,_that.isFree,_that.openingHours,_that.bestSeason,_that.howToGetThere,_that.distanceFromCenter,_that.amenities,_that.coverImageId,_that.coverImageUrl,_that.isActive,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Place extends Place {
  const _Place({required this.id, required this.categoryId, required this.name, this.description, this.address, this.latitude = 0, this.longitude = 0, this.entranceFee, this.isFree = false, this.openingHours, this.bestSeason, this.howToGetThere, this.distanceFromCenter, @JsonKey(fromJson: _rawAmenities) this.amenities, this.coverImageId, this.coverImageUrl, this.isActive = true, this.createdAt}): super._();
  factory _Place.fromJson(Map<String, dynamic> json) => _$PlaceFromJson(json);

@override final  String id;
@override final  String categoryId;
@override final  String name;
@override final  String? description;
@override final  String? address;
@override@JsonKey() final  double latitude;
@override@JsonKey() final  double longitude;
@override final  double? entranceFee;
@override@JsonKey() final  bool isFree;
@override final  String? openingHours;
@override final  String? bestSeason;
@override final  String? howToGetThere;
@override final  double? distanceFromCenter;
/// Ham `jsonb` içeriği — çözümlenmiş hâli için [amenityMap].
@override@JsonKey(fromJson: _rawAmenities) final  String? amenities;
@override final  String? coverImageId;
@override final  String? coverImageUrl;
@override@JsonKey() final  bool isActive;
@override final  DateTime? createdAt;

/// Create a copy of Place
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$PlaceCopyWith<_Place> get copyWith => __$PlaceCopyWithImpl<_Place>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$PlaceToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Place&&(identical(other.id, id) || other.id == id)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.name, name) || other.name == name)&&(identical(other.description, description) || other.description == description)&&(identical(other.address, address) || other.address == address)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.entranceFee, entranceFee) || other.entranceFee == entranceFee)&&(identical(other.isFree, isFree) || other.isFree == isFree)&&(identical(other.openingHours, openingHours) || other.openingHours == openingHours)&&(identical(other.bestSeason, bestSeason) || other.bestSeason == bestSeason)&&(identical(other.howToGetThere, howToGetThere) || other.howToGetThere == howToGetThere)&&(identical(other.distanceFromCenter, distanceFromCenter) || other.distanceFromCenter == distanceFromCenter)&&(identical(other.amenities, amenities) || other.amenities == amenities)&&(identical(other.coverImageId, coverImageId) || other.coverImageId == coverImageId)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.isActive, isActive) || other.isActive == isActive)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,categoryId,name,description,address,latitude,longitude,entranceFee,isFree,openingHours,bestSeason,howToGetThere,distanceFromCenter,amenities,coverImageId,coverImageUrl,isActive,createdAt);

@override
String toString() {
  return 'Place(id: $id, categoryId: $categoryId, name: $name, description: $description, address: $address, latitude: $latitude, longitude: $longitude, entranceFee: $entranceFee, isFree: $isFree, openingHours: $openingHours, bestSeason: $bestSeason, howToGetThere: $howToGetThere, distanceFromCenter: $distanceFromCenter, amenities: $amenities, coverImageId: $coverImageId, coverImageUrl: $coverImageUrl, isActive: $isActive, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$PlaceCopyWith<$Res> implements $PlaceCopyWith<$Res> {
  factory _$PlaceCopyWith(_Place value, $Res Function(_Place) _then) = __$PlaceCopyWithImpl;
@override @useResult
$Res call({
 String id, String categoryId, String name, String? description, String? address, double latitude, double longitude, double? entranceFee, bool isFree, String? openingHours, String? bestSeason, String? howToGetThere, double? distanceFromCenter,@JsonKey(fromJson: _rawAmenities) String? amenities, String? coverImageId, String? coverImageUrl, bool isActive, DateTime? createdAt
});




}
/// @nodoc
class __$PlaceCopyWithImpl<$Res>
    implements _$PlaceCopyWith<$Res> {
  __$PlaceCopyWithImpl(this._self, this._then);

  final _Place _self;
  final $Res Function(_Place) _then;

/// Create a copy of Place
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? categoryId = null,Object? name = null,Object? description = freezed,Object? address = freezed,Object? latitude = null,Object? longitude = null,Object? entranceFee = freezed,Object? isFree = null,Object? openingHours = freezed,Object? bestSeason = freezed,Object? howToGetThere = freezed,Object? distanceFromCenter = freezed,Object? amenities = freezed,Object? coverImageId = freezed,Object? coverImageUrl = freezed,Object? isActive = null,Object? createdAt = freezed,}) {
  return _then(_Place(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,categoryId: null == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,latitude: null == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double,longitude: null == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double,entranceFee: freezed == entranceFee ? _self.entranceFee : entranceFee // ignore: cast_nullable_to_non_nullable
as double?,isFree: null == isFree ? _self.isFree : isFree // ignore: cast_nullable_to_non_nullable
as bool,openingHours: freezed == openingHours ? _self.openingHours : openingHours // ignore: cast_nullable_to_non_nullable
as String?,bestSeason: freezed == bestSeason ? _self.bestSeason : bestSeason // ignore: cast_nullable_to_non_nullable
as String?,howToGetThere: freezed == howToGetThere ? _self.howToGetThere : howToGetThere // ignore: cast_nullable_to_non_nullable
as String?,distanceFromCenter: freezed == distanceFromCenter ? _self.distanceFromCenter : distanceFromCenter // ignore: cast_nullable_to_non_nullable
as double?,amenities: freezed == amenities ? _self.amenities : amenities // ignore: cast_nullable_to_non_nullable
as String?,coverImageId: freezed == coverImageId ? _self.coverImageId : coverImageId // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
