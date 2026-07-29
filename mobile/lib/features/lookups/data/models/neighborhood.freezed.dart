// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'neighborhood.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Neighborhood {

 String get id; String get name; String get slug;/// `mahalle` / `köy` gibi tür etiketi — boş olabilir.
 String? get type; double? get latitude; double? get longitude; int get displayOrder;
/// Create a copy of Neighborhood
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NeighborhoodCopyWith<Neighborhood> get copyWith => _$NeighborhoodCopyWithImpl<Neighborhood>(this as Neighborhood, _$identity);

  /// Serializes this Neighborhood to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Neighborhood&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.type, type) || other.type == type)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,type,latitude,longitude,displayOrder);

@override
String toString() {
  return 'Neighborhood(id: $id, name: $name, slug: $slug, type: $type, latitude: $latitude, longitude: $longitude, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class $NeighborhoodCopyWith<$Res>  {
  factory $NeighborhoodCopyWith(Neighborhood value, $Res Function(Neighborhood) _then) = _$NeighborhoodCopyWithImpl;
@useResult
$Res call({
 String id, String name, String slug, String? type, double? latitude, double? longitude, int displayOrder
});




}
/// @nodoc
class _$NeighborhoodCopyWithImpl<$Res>
    implements $NeighborhoodCopyWith<$Res> {
  _$NeighborhoodCopyWithImpl(this._self, this._then);

  final Neighborhood _self;
  final $Res Function(Neighborhood) _then;

/// Create a copy of Neighborhood
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? type = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? displayOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,type: freezed == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [Neighborhood].
extension NeighborhoodPatterns on Neighborhood {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Neighborhood value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Neighborhood() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Neighborhood value)  $default,){
final _that = this;
switch (_that) {
case _Neighborhood():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Neighborhood value)?  $default,){
final _that = this;
switch (_that) {
case _Neighborhood() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? type,  double? latitude,  double? longitude,  int displayOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Neighborhood() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.type,_that.latitude,_that.longitude,_that.displayOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? type,  double? latitude,  double? longitude,  int displayOrder)  $default,) {final _that = this;
switch (_that) {
case _Neighborhood():
return $default(_that.id,_that.name,_that.slug,_that.type,_that.latitude,_that.longitude,_that.displayOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String slug,  String? type,  double? latitude,  double? longitude,  int displayOrder)?  $default,) {final _that = this;
switch (_that) {
case _Neighborhood() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.type,_that.latitude,_that.longitude,_that.displayOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Neighborhood extends Neighborhood {
  const _Neighborhood({required this.id, required this.name, this.slug = '', this.type, this.latitude, this.longitude, this.displayOrder = 0}): super._();
  factory _Neighborhood.fromJson(Map<String, dynamic> json) => _$NeighborhoodFromJson(json);

@override final  String id;
@override final  String name;
@override@JsonKey() final  String slug;
/// `mahalle` / `köy` gibi tür etiketi — boş olabilir.
@override final  String? type;
@override final  double? latitude;
@override final  double? longitude;
@override@JsonKey() final  int displayOrder;

/// Create a copy of Neighborhood
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NeighborhoodCopyWith<_Neighborhood> get copyWith => __$NeighborhoodCopyWithImpl<_Neighborhood>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NeighborhoodToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Neighborhood&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.type, type) || other.type == type)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,type,latitude,longitude,displayOrder);

@override
String toString() {
  return 'Neighborhood(id: $id, name: $name, slug: $slug, type: $type, latitude: $latitude, longitude: $longitude, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class _$NeighborhoodCopyWith<$Res> implements $NeighborhoodCopyWith<$Res> {
  factory _$NeighborhoodCopyWith(_Neighborhood value, $Res Function(_Neighborhood) _then) = __$NeighborhoodCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String slug, String? type, double? latitude, double? longitude, int displayOrder
});




}
/// @nodoc
class __$NeighborhoodCopyWithImpl<$Res>
    implements _$NeighborhoodCopyWith<$Res> {
  __$NeighborhoodCopyWithImpl(this._self, this._then);

  final _Neighborhood _self;
  final $Res Function(_Neighborhood) _then;

/// Create a copy of Neighborhood
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? type = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? displayOrder = null,}) {
  return _then(_Neighborhood(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,type: freezed == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
