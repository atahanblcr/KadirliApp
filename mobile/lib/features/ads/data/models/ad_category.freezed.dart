// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'ad_category.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$AdCategory {

 String get id; String get name; String get slug; String? get parentId; String? get icon; int get displayOrder; int get subCategoryCount;
/// Create a copy of AdCategory
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AdCategoryCopyWith<AdCategory> get copyWith => _$AdCategoryCopyWithImpl<AdCategory>(this as AdCategory, _$identity);

  /// Serializes this AdCategory to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AdCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.parentId, parentId) || other.parentId == parentId)&&(identical(other.icon, icon) || other.icon == icon)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder)&&(identical(other.subCategoryCount, subCategoryCount) || other.subCategoryCount == subCategoryCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,parentId,icon,displayOrder,subCategoryCount);

@override
String toString() {
  return 'AdCategory(id: $id, name: $name, slug: $slug, parentId: $parentId, icon: $icon, displayOrder: $displayOrder, subCategoryCount: $subCategoryCount)';
}


}

/// @nodoc
abstract mixin class $AdCategoryCopyWith<$Res>  {
  factory $AdCategoryCopyWith(AdCategory value, $Res Function(AdCategory) _then) = _$AdCategoryCopyWithImpl;
@useResult
$Res call({
 String id, String name, String slug, String? parentId, String? icon, int displayOrder, int subCategoryCount
});




}
/// @nodoc
class _$AdCategoryCopyWithImpl<$Res>
    implements $AdCategoryCopyWith<$Res> {
  _$AdCategoryCopyWithImpl(this._self, this._then);

  final AdCategory _self;
  final $Res Function(AdCategory) _then;

/// Create a copy of AdCategory
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? parentId = freezed,Object? icon = freezed,Object? displayOrder = null,Object? subCategoryCount = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,icon: freezed == icon ? _self.icon : icon // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,subCategoryCount: null == subCategoryCount ? _self.subCategoryCount : subCategoryCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [AdCategory].
extension AdCategoryPatterns on AdCategory {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AdCategory value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AdCategory() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AdCategory value)  $default,){
final _that = this;
switch (_that) {
case _AdCategory():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AdCategory value)?  $default,){
final _that = this;
switch (_that) {
case _AdCategory() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? parentId,  String? icon,  int displayOrder,  int subCategoryCount)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AdCategory() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.parentId,_that.icon,_that.displayOrder,_that.subCategoryCount);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  String? parentId,  String? icon,  int displayOrder,  int subCategoryCount)  $default,) {final _that = this;
switch (_that) {
case _AdCategory():
return $default(_that.id,_that.name,_that.slug,_that.parentId,_that.icon,_that.displayOrder,_that.subCategoryCount);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String slug,  String? parentId,  String? icon,  int displayOrder,  int subCategoryCount)?  $default,) {final _that = this;
switch (_that) {
case _AdCategory() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.parentId,_that.icon,_that.displayOrder,_that.subCategoryCount);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AdCategory extends AdCategory {
  const _AdCategory({required this.id, required this.name, this.slug = '', this.parentId, this.icon, this.displayOrder = 0, this.subCategoryCount = 0}): super._();
  factory _AdCategory.fromJson(Map<String, dynamic> json) => _$AdCategoryFromJson(json);

@override final  String id;
@override final  String name;
@override@JsonKey() final  String slug;
@override final  String? parentId;
@override final  String? icon;
@override@JsonKey() final  int displayOrder;
@override@JsonKey() final  int subCategoryCount;

/// Create a copy of AdCategory
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AdCategoryCopyWith<_AdCategory> get copyWith => __$AdCategoryCopyWithImpl<_AdCategory>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AdCategoryToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AdCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.parentId, parentId) || other.parentId == parentId)&&(identical(other.icon, icon) || other.icon == icon)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder)&&(identical(other.subCategoryCount, subCategoryCount) || other.subCategoryCount == subCategoryCount));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,parentId,icon,displayOrder,subCategoryCount);

@override
String toString() {
  return 'AdCategory(id: $id, name: $name, slug: $slug, parentId: $parentId, icon: $icon, displayOrder: $displayOrder, subCategoryCount: $subCategoryCount)';
}


}

/// @nodoc
abstract mixin class _$AdCategoryCopyWith<$Res> implements $AdCategoryCopyWith<$Res> {
  factory _$AdCategoryCopyWith(_AdCategory value, $Res Function(_AdCategory) _then) = __$AdCategoryCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String slug, String? parentId, String? icon, int displayOrder, int subCategoryCount
});




}
/// @nodoc
class __$AdCategoryCopyWithImpl<$Res>
    implements _$AdCategoryCopyWith<$Res> {
  __$AdCategoryCopyWithImpl(this._self, this._then);

  final _AdCategory _self;
  final $Res Function(_AdCategory) _then;

/// Create a copy of AdCategory
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? parentId = freezed,Object? icon = freezed,Object? displayOrder = null,Object? subCategoryCount = null,}) {
  return _then(_AdCategory(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,parentId: freezed == parentId ? _self.parentId : parentId // ignore: cast_nullable_to_non_nullable
as String?,icon: freezed == icon ? _self.icon : icon // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,subCategoryCount: null == subCategoryCount ? _self.subCategoryCount : subCategoryCount // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
