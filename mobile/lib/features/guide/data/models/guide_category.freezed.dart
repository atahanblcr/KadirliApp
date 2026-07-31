// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'guide_category.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$GuideCategory {

 String get id; String get name; String get slug; String? get parentId; String? get icon; String? get color; int get displayOrder;
/// Create a copy of GuideCategory
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$GuideCategoryCopyWith<GuideCategory> get copyWith => _$GuideCategoryCopyWithImpl<GuideCategory>(this as GuideCategory, _$identity);

  /// Serializes this GuideCategory to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is GuideCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.parentId, parentId) || other.parentId == parentId)&&(identical(other.icon, icon) || other.icon == icon)&&(identical(other.color, color) || other.color == color)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,parentId,icon,color,displayOrder);

@override
String toString() {
  return 'GuideCategory(id: $id, name: $name, slug: $slug, parentId: $parentId, icon: $icon, color: $color, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class $GuideCategoryCopyWith<$Res>  {
  factory $GuideCategoryCopyWith(GuideCategory value, $Res Function(GuideCategory) _then) = _$GuideCategoryCopyWithImpl;
@useResult
$Res call({
 String id, String name, String slug, String? parentId, String? icon, String? color, int displayOrder
});




}
/// @nodoc
class _$GuideCategoryCopyWithImpl<$Res>
    implements $GuideCategoryCopyWith<$Res> {
  _$GuideCategoryCopyWithImpl(this._self, this._then);

  final GuideCategory _self;
  final $Res Function(GuideCategory) _then;

/// Create a copy of GuideCategory
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? parentId = freezed,Object? icon = freezed,Object? color = freezed,Object? displayOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,icon: freezed == icon ? _self.icon : icon // ignore: cast_nullable_to_non_nullable
as String?,color: freezed == color ? _self.color : color // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [GuideCategory].
extension GuideCategoryPatterns on GuideCategory {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _GuideCategory value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _GuideCategory() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _GuideCategory value)  $default,){
final _that = this;
switch (_that) {
case _GuideCategory():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _GuideCategory value)?  $default,){
final _that = this;
switch (_that) {
case _GuideCategory() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? parentId,  String? icon,  String? color,  int displayOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _GuideCategory() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.parentId,_that.icon,_that.color,_that.displayOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? parentId,  String? icon,  String? color,  int displayOrder)  $default,) {final _that = this;
switch (_that) {
case _GuideCategory():
return $default(_that.id,_that.name,_that.slug,_that.parentId,_that.icon,_that.color,_that.displayOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String slug,  String? parentId,  String? icon,  String? color,  int displayOrder)?  $default,) {final _that = this;
switch (_that) {
case _GuideCategory() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.parentId,_that.icon,_that.color,_that.displayOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _GuideCategory extends GuideCategory {
  const _GuideCategory({required this.id, required this.name, this.slug = '', this.parentId, this.icon, this.color, this.displayOrder = 0}): super._();
  factory _GuideCategory.fromJson(Map<String, dynamic> json) => _$GuideCategoryFromJson(json);

@override final  String id;
@override final  String name;
@override@JsonKey() final  String slug;
@override final  String? parentId;
@override final  String? icon;
@override final  String? color;
@override@JsonKey() final  int displayOrder;

/// Create a copy of GuideCategory
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$GuideCategoryCopyWith<_GuideCategory> get copyWith => __$GuideCategoryCopyWithImpl<_GuideCategory>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$GuideCategoryToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _GuideCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.parentId, parentId) || other.parentId == parentId)&&(identical(other.icon, icon) || other.icon == icon)&&(identical(other.color, color) || other.color == color)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,parentId,icon,color,displayOrder);

@override
String toString() {
  return 'GuideCategory(id: $id, name: $name, slug: $slug, parentId: $parentId, icon: $icon, color: $color, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class _$GuideCategoryCopyWith<$Res> implements $GuideCategoryCopyWith<$Res> {
  factory _$GuideCategoryCopyWith(_GuideCategory value, $Res Function(_GuideCategory) _then) = __$GuideCategoryCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String slug, String? parentId, String? icon, String? color, int displayOrder
});




}
/// @nodoc
class __$GuideCategoryCopyWithImpl<$Res>
    implements _$GuideCategoryCopyWith<$Res> {
  __$GuideCategoryCopyWithImpl(this._self, this._then);

  final _GuideCategory _self;
  final $Res Function(_GuideCategory) _then;

/// Create a copy of GuideCategory
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? parentId = freezed,Object? icon = freezed,Object? color = freezed,Object? displayOrder = null,}) {
  return _then(_GuideCategory(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,icon: freezed == icon ? _self.icon : icon // ignore: cast_nullable_to_non_nullable
as String?,color: freezed == color ? _self.color : color // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
