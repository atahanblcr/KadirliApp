// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'category_property.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$CategoryProperty {

 String get id; String get propertyName; String get propertyType; bool get isRequired; String? get defaultValue; int get displayOrder; List<PropertyOption> get options;
/// Create a copy of CategoryProperty
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CategoryPropertyCopyWith<CategoryProperty> get copyWith => _$CategoryPropertyCopyWithImpl<CategoryProperty>(this as CategoryProperty, _$identity);

  /// Serializes this CategoryProperty to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is CategoryProperty&&(identical(other.id, id) || other.id == id)&&(identical(other.propertyName, propertyName) || other.propertyName == propertyName)&&(identical(other.propertyType, propertyType) || other.propertyType == propertyType)&&(identical(other.isRequired, isRequired) || other.isRequired == isRequired)&&(identical(other.defaultValue, defaultValue) || other.defaultValue == defaultValue)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder)&&const DeepCollectionEquality().equals(other.options, options));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,propertyName,propertyType,isRequired,defaultValue,displayOrder,const DeepCollectionEquality().hash(options));

@override
String toString() {
  return 'CategoryProperty(id: $id, propertyName: $propertyName, propertyType: $propertyType, isRequired: $isRequired, defaultValue: $defaultValue, displayOrder: $displayOrder, options: $options)';
}


}

/// @nodoc
abstract mixin class $CategoryPropertyCopyWith<$Res>  {
  factory $CategoryPropertyCopyWith(CategoryProperty value, $Res Function(CategoryProperty) _then) = _$CategoryPropertyCopyWithImpl;
@useResult
$Res call({
 String id, String propertyName, String propertyType, bool isRequired, String? defaultValue, int displayOrder, List<PropertyOption> options
});




}
/// @nodoc
class _$CategoryPropertyCopyWithImpl<$Res>
    implements $CategoryPropertyCopyWith<$Res> {
  _$CategoryPropertyCopyWithImpl(this._self, this._then);

  final CategoryProperty _self;
  final $Res Function(CategoryProperty) _then;

/// Create a copy of CategoryProperty
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? propertyName = null,Object? propertyType = null,Object? isRequired = null,Object? defaultValue = freezed,Object? displayOrder = null,Object? options = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,propertyName: null == propertyName ? _self.propertyName : propertyName // ignore: cast_nullable_to_non_nullable
as String,propertyType: null == propertyType ? _self.propertyType : propertyType // ignore: cast_nullable_to_non_nullable
as String,isRequired: null == isRequired ? _self.isRequired : isRequired // ignore: cast_nullable_to_non_nullable
as bool,defaultValue: freezed == defaultValue ? _self.defaultValue : defaultValue // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,options: null == options ? _self.options : options // ignore: cast_nullable_to_non_nullable
as List<PropertyOption>,
  ));
}

}


/// Adds pattern-matching-related methods to [CategoryProperty].
extension CategoryPropertyPatterns on CategoryProperty {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _CategoryProperty value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _CategoryProperty() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _CategoryProperty value)  $default,){
final _that = this;
switch (_that) {
case _CategoryProperty():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _CategoryProperty value)?  $default,){
final _that = this;
switch (_that) {
case _CategoryProperty() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String propertyName,  String propertyType,  bool isRequired,  String? defaultValue,  int displayOrder,  List<PropertyOption> options)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _CategoryProperty() when $default != null:
return $default(_that.id,_that.propertyName,_that.propertyType,_that.isRequired,_that.defaultValue,_that.displayOrder,_that.options);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String propertyName,  String propertyType,  bool isRequired,  String? defaultValue,  int displayOrder,  List<PropertyOption> options)  $default,) {final _that = this;
switch (_that) {
case _CategoryProperty():
return $default(_that.id,_that.propertyName,_that.propertyType,_that.isRequired,_that.defaultValue,_that.displayOrder,_that.options);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String propertyName,  String propertyType,  bool isRequired,  String? defaultValue,  int displayOrder,  List<PropertyOption> options)?  $default,) {final _that = this;
switch (_that) {
case _CategoryProperty() when $default != null:
return $default(_that.id,_that.propertyName,_that.propertyType,_that.isRequired,_that.defaultValue,_that.displayOrder,_that.options);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _CategoryProperty extends CategoryProperty {
  const _CategoryProperty({required this.id, this.propertyName = '', this.propertyType = 'Text', this.isRequired = false, this.defaultValue, this.displayOrder = 0, final  List<PropertyOption> options = const <PropertyOption>[]}): _options = options,super._();
  factory _CategoryProperty.fromJson(Map<String, dynamic> json) => _$CategoryPropertyFromJson(json);

@override final  String id;
@override@JsonKey() final  String propertyName;
@override@JsonKey() final  String propertyType;
@override@JsonKey() final  bool isRequired;
@override final  String? defaultValue;
@override@JsonKey() final  int displayOrder;
 final  List<PropertyOption> _options;
@override@JsonKey() List<PropertyOption> get options {
  if (_options is EqualUnmodifiableListView) return _options;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_options);
}


/// Create a copy of CategoryProperty
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CategoryPropertyCopyWith<_CategoryProperty> get copyWith => __$CategoryPropertyCopyWithImpl<_CategoryProperty>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CategoryPropertyToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _CategoryProperty&&(identical(other.id, id) || other.id == id)&&(identical(other.propertyName, propertyName) || other.propertyName == propertyName)&&(identical(other.propertyType, propertyType) || other.propertyType == propertyType)&&(identical(other.isRequired, isRequired) || other.isRequired == isRequired)&&(identical(other.defaultValue, defaultValue) || other.defaultValue == defaultValue)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder)&&const DeepCollectionEquality().equals(other._options, _options));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,propertyName,propertyType,isRequired,defaultValue,displayOrder,const DeepCollectionEquality().hash(_options));

@override
String toString() {
  return 'CategoryProperty(id: $id, propertyName: $propertyName, propertyType: $propertyType, isRequired: $isRequired, defaultValue: $defaultValue, displayOrder: $displayOrder, options: $options)';
}


}

/// @nodoc
abstract mixin class _$CategoryPropertyCopyWith<$Res> implements $CategoryPropertyCopyWith<$Res> {
  factory _$CategoryPropertyCopyWith(_CategoryProperty value, $Res Function(_CategoryProperty) _then) = __$CategoryPropertyCopyWithImpl;
@override @useResult
$Res call({
 String id, String propertyName, String propertyType, bool isRequired, String? defaultValue, int displayOrder, List<PropertyOption> options
});




}
/// @nodoc
class __$CategoryPropertyCopyWithImpl<$Res>
    implements _$CategoryPropertyCopyWith<$Res> {
  __$CategoryPropertyCopyWithImpl(this._self, this._then);

  final _CategoryProperty _self;
  final $Res Function(_CategoryProperty) _then;

/// Create a copy of CategoryProperty
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? propertyName = null,Object? propertyType = null,Object? isRequired = null,Object? defaultValue = freezed,Object? displayOrder = null,Object? options = null,}) {
  return _then(_CategoryProperty(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,propertyName: null == propertyName ? _self.propertyName : propertyName // ignore: cast_nullable_to_non_nullable
as String,propertyType: null == propertyType ? _self.propertyType : propertyType // ignore: cast_nullable_to_non_nullable
as String,isRequired: null == isRequired ? _self.isRequired : isRequired // ignore: cast_nullable_to_non_nullable
as bool,defaultValue: freezed == defaultValue ? _self.defaultValue : defaultValue // ignore: cast_nullable_to_non_nullable
as String?,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,options: null == options ? _self._options : options // ignore: cast_nullable_to_non_nullable
as List<PropertyOption>,
  ));
}


}


/// @nodoc
mixin _$PropertyOption {

 String get id; String get optionValue; int get displayOrder;
/// Create a copy of PropertyOption
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$PropertyOptionCopyWith<PropertyOption> get copyWith => _$PropertyOptionCopyWithImpl<PropertyOption>(this as PropertyOption, _$identity);

  /// Serializes this PropertyOption to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is PropertyOption&&(identical(other.id, id) || other.id == id)&&(identical(other.optionValue, optionValue) || other.optionValue == optionValue)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,optionValue,displayOrder);

@override
String toString() {
  return 'PropertyOption(id: $id, optionValue: $optionValue, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class $PropertyOptionCopyWith<$Res>  {
  factory $PropertyOptionCopyWith(PropertyOption value, $Res Function(PropertyOption) _then) = _$PropertyOptionCopyWithImpl;
@useResult
$Res call({
 String id, String optionValue, int displayOrder
});




}
/// @nodoc
class _$PropertyOptionCopyWithImpl<$Res>
    implements $PropertyOptionCopyWith<$Res> {
  _$PropertyOptionCopyWithImpl(this._self, this._then);

  final PropertyOption _self;
  final $Res Function(PropertyOption) _then;

/// Create a copy of PropertyOption
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? optionValue = null,Object? displayOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,optionValue: null == optionValue ? _self.optionValue : optionValue // ignore: cast_nullable_to_non_nullable
as String,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [PropertyOption].
extension PropertyOptionPatterns on PropertyOption {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _PropertyOption value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _PropertyOption() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _PropertyOption value)  $default,){
final _that = this;
switch (_that) {
case _PropertyOption():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _PropertyOption value)?  $default,){
final _that = this;
switch (_that) {
case _PropertyOption() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String optionValue,  int displayOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _PropertyOption() when $default != null:
return $default(_that.id,_that.optionValue,_that.displayOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String optionValue,  int displayOrder)  $default,) {final _that = this;
switch (_that) {
case _PropertyOption():
return $default(_that.id,_that.optionValue,_that.displayOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String optionValue,  int displayOrder)?  $default,) {final _that = this;
switch (_that) {
case _PropertyOption() when $default != null:
return $default(_that.id,_that.optionValue,_that.displayOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _PropertyOption implements PropertyOption {
  const _PropertyOption({required this.id, this.optionValue = '', this.displayOrder = 0});
  factory _PropertyOption.fromJson(Map<String, dynamic> json) => _$PropertyOptionFromJson(json);

@override final  String id;
@override@JsonKey() final  String optionValue;
@override@JsonKey() final  int displayOrder;

/// Create a copy of PropertyOption
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$PropertyOptionCopyWith<_PropertyOption> get copyWith => __$PropertyOptionCopyWithImpl<_PropertyOption>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$PropertyOptionToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _PropertyOption&&(identical(other.id, id) || other.id == id)&&(identical(other.optionValue, optionValue) || other.optionValue == optionValue)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,optionValue,displayOrder);

@override
String toString() {
  return 'PropertyOption(id: $id, optionValue: $optionValue, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class _$PropertyOptionCopyWith<$Res> implements $PropertyOptionCopyWith<$Res> {
  factory _$PropertyOptionCopyWith(_PropertyOption value, $Res Function(_PropertyOption) _then) = __$PropertyOptionCopyWithImpl;
@override @useResult
$Res call({
 String id, String optionValue, int displayOrder
});




}
/// @nodoc
class __$PropertyOptionCopyWithImpl<$Res>
    implements _$PropertyOptionCopyWith<$Res> {
  __$PropertyOptionCopyWithImpl(this._self, this._then);

  final _PropertyOption _self;
  final $Res Function(_PropertyOption) _then;

/// Create a copy of PropertyOption
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? optionValue = null,Object? displayOrder = null,}) {
  return _then(_PropertyOption(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,optionValue: null == optionValue ? _self.optionValue : optionValue // ignore: cast_nullable_to_non_nullable
as String,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}

// dart format on
