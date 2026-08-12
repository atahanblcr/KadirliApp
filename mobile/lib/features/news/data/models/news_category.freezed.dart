// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'news_category.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$NewsCategory {

 String get id; String get name; String get slug; int get articleCount; bool get showInFilterStrip; int get displayOrder;
/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NewsCategoryCopyWith<NewsCategory> get copyWith => _$NewsCategoryCopyWithImpl<NewsCategory>(this as NewsCategory, _$identity);

  /// Serializes this NewsCategory to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NewsCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.articleCount, articleCount) || other.articleCount == articleCount)&&(identical(other.showInFilterStrip, showInFilterStrip) || other.showInFilterStrip == showInFilterStrip)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,articleCount,showInFilterStrip,displayOrder);

@override
String toString() {
  return 'NewsCategory(id: $id, name: $name, slug: $slug, articleCount: $articleCount, showInFilterStrip: $showInFilterStrip, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class $NewsCategoryCopyWith<$Res>  {
  factory $NewsCategoryCopyWith(NewsCategory value, $Res Function(NewsCategory) _then) = _$NewsCategoryCopyWithImpl;
@useResult
$Res call({
 String id, String name, String slug, int articleCount, bool showInFilterStrip, int displayOrder
});




}
/// @nodoc
class _$NewsCategoryCopyWithImpl<$Res>
    implements $NewsCategoryCopyWith<$Res> {
  _$NewsCategoryCopyWithImpl(this._self, this._then);

  final NewsCategory _self;
  final $Res Function(NewsCategory) _then;

/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? articleCount = null,Object? showInFilterStrip = null,Object? displayOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,articleCount: null == articleCount ? _self.articleCount : articleCount // ignore: cast_nullable_to_non_nullable
as int,showInFilterStrip: null == showInFilterStrip ? _self.showInFilterStrip : showInFilterStrip // ignore: cast_nullable_to_non_nullable
as bool,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [NewsCategory].
extension NewsCategoryPatterns on NewsCategory {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NewsCategory value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NewsCategory value)  $default,){
final _that = this;
switch (_that) {
case _NewsCategory():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NewsCategory value)?  $default,){
final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  int articleCount,  bool showInFilterStrip,  int displayOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.articleCount,_that.showInFilterStrip,_that.displayOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String slug,  int articleCount,  bool showInFilterStrip,  int displayOrder)  $default,) {final _that = this;
switch (_that) {
case _NewsCategory():
return $default(_that.id,_that.name,_that.slug,_that.articleCount,_that.showInFilterStrip,_that.displayOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String slug,  int articleCount,  bool showInFilterStrip,  int displayOrder)?  $default,) {final _that = this;
switch (_that) {
case _NewsCategory() when $default != null:
return $default(_that.id,_that.name,_that.slug,_that.articleCount,_that.showInFilterStrip,_that.displayOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NewsCategory extends NewsCategory {
  const _NewsCategory({required this.id, this.name = '', this.slug = '', this.articleCount = 0, this.showInFilterStrip = true, this.displayOrder = 0}): super._();
  factory _NewsCategory.fromJson(Map<String, dynamic> json) => _$NewsCategoryFromJson(json);

@override final  String id;
@override@JsonKey() final  String name;
@override@JsonKey() final  String slug;
@override@JsonKey() final  int articleCount;
@override@JsonKey() final  bool showInFilterStrip;
@override@JsonKey() final  int displayOrder;

/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NewsCategoryCopyWith<_NewsCategory> get copyWith => __$NewsCategoryCopyWithImpl<_NewsCategory>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NewsCategoryToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _NewsCategory&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.slug, slug) || other.slug == slug)&&(identical(other.articleCount, articleCount) || other.articleCount == articleCount)&&(identical(other.showInFilterStrip, showInFilterStrip) || other.showInFilterStrip == showInFilterStrip)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,slug,articleCount,showInFilterStrip,displayOrder);

@override
String toString() {
  return 'NewsCategory(id: $id, name: $name, slug: $slug, articleCount: $articleCount, showInFilterStrip: $showInFilterStrip, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class _$NewsCategoryCopyWith<$Res> implements $NewsCategoryCopyWith<$Res> {
  factory _$NewsCategoryCopyWith(_NewsCategory value, $Res Function(_NewsCategory) _then) = __$NewsCategoryCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String slug, int articleCount, bool showInFilterStrip, int displayOrder
});




}
/// @nodoc
class __$NewsCategoryCopyWithImpl<$Res>
    implements _$NewsCategoryCopyWith<$Res> {
  __$NewsCategoryCopyWithImpl(this._self, this._then);

  final _NewsCategory _self;
  final $Res Function(_NewsCategory) _then;

/// Create a copy of NewsCategory
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? slug = null,Object? articleCount = null,Object? showInFilterStrip = null,Object? displayOrder = null,}) {
  return _then(_NewsCategory(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,slug: null == slug ? _self.slug : slug // ignore: cast_nullable_to_non_nullable
as String,articleCount: null == articleCount ? _self.articleCount : articleCount // ignore: cast_nullable_to_non_nullable
as int,showInFilterStrip: null == showInFilterStrip ? _self.showInFilterStrip : showInFilterStrip // ignore: cast_nullable_to_non_nullable
as bool,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
