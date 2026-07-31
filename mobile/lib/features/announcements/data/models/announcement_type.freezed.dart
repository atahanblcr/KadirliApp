// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'announcement_type.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$AnnouncementType {

 String get id; String get name; String get slug; String? get icon; String? get color; int get displayOrder;
/// Create a copy of AnnouncementType
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AnnouncementTypeCopyWith<AnnouncementType> get copyWith => _$AnnouncementTypeCopyWithImpl<AnnouncementType>(this as AnnouncementType, _$identity);

  /// Serializes this AnnouncementType to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AnnouncementType&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.icon, icon) || other.icon == icon)&&(identical(other.color, color) || other.color == color)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,icon,color,displayOrder);

@override
String toString() {
  return 'AnnouncementType(id: $id, name: $name, slug: $slug, icon: $icon, color: $color, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class $AnnouncementTypeCopyWith<$Res>  {
  factory $AnnouncementTypeCopyWith(AnnouncementType value, $Res Function(AnnouncementType) _then) = _$AnnouncementTypeCopyWithImpl;
@useResult
$Res call({
 String id, String name, String slug, String? icon, String? color, int displayOrder
});




}
/// @nodoc
class _$AnnouncementTypeCopyWithImpl<$Res>
    implements $AnnouncementTypeCopyWith<$Res> {
  _$AnnouncementTypeCopyWithImpl(this._self, this._then);

  final AnnouncementType _self;
  final $Res Function(AnnouncementType) _then;

/// Create a copy of AnnouncementType
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? icon = freezed,Object? color = freezed,Object? displayOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,icon: freezed == icon ? _self.icon : icon // ignore: cast_nullable_to_non_nullable
as String?,color: freezed == color ? _self.color : color // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [AnnouncementType].
extension AnnouncementTypePatterns on AnnouncementType {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AnnouncementType value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AnnouncementType() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AnnouncementType value)  $default,){
final _that = this;
switch (_that) {
case _AnnouncementType():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AnnouncementType value)?  $default,){
final _that = this;
switch (_that) {
case _AnnouncementType() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? icon,  String? color,  int displayOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AnnouncementType() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.icon,_that.color,_that.displayOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? icon,  String? color,  int displayOrder)  $default,) {final _that = this;
switch (_that) {
case _AnnouncementType():
return $default(_that.id,_that.name,_that.slug,_that.icon,_that.color,_that.displayOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String slug,  String? icon,  String? color,  int displayOrder)?  $default,) {final _that = this;
switch (_that) {
case _AnnouncementType() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.icon,_that.color,_that.displayOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AnnouncementType extends AnnouncementType {
  const _AnnouncementType({required this.id, required this.name, this.slug = '', this.icon, this.color, this.displayOrder = 0}): super._();
  factory _AnnouncementType.fromJson(Map<String, dynamic> json) => _$AnnouncementTypeFromJson(json);

@override final  String id;
@override final  String name;
@override@JsonKey() final  String slug;
@override final  String? icon;
@override final  String? color;
@override@JsonKey() final  int displayOrder;

/// Create a copy of AnnouncementType
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AnnouncementTypeCopyWith<_AnnouncementType> get copyWith => __$AnnouncementTypeCopyWithImpl<_AnnouncementType>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AnnouncementTypeToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AnnouncementType&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.icon, icon) || other.icon == icon)&&(identical(other.color, color) || other.color == color)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,icon,color,displayOrder);

@override
String toString() {
  return 'AnnouncementType(id: $id, name: $name, slug: $slug, icon: $icon, color: $color, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class _$AnnouncementTypeCopyWith<$Res> implements $AnnouncementTypeCopyWith<$Res> {
  factory _$AnnouncementTypeCopyWith(_AnnouncementType value, $Res Function(_AnnouncementType) _then) = __$AnnouncementTypeCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String slug, String? icon, String? color, int displayOrder
});




}
/// @nodoc
class __$AnnouncementTypeCopyWithImpl<$Res>
    implements _$AnnouncementTypeCopyWith<$Res> {
  __$AnnouncementTypeCopyWithImpl(this._self, this._then);

  final _AnnouncementType _self;
  final $Res Function(_AnnouncementType) _then;

/// Create a copy of AnnouncementType
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? icon = freezed,Object? color = freezed,Object? displayOrder = null,}) {
  return _then(_AnnouncementType(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,icon: freezed == icon ? _self.icon : icon // ignore: cast_nullable_to_non_nullable
as String?,color: freezed == color ? _self.color : color // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
