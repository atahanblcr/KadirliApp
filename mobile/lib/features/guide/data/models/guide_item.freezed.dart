// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'guide_item.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$GuideItem {

 String get id; String get name; String? get categoryId; String? get categoryName; String? get categoryIcon; String? get categoryColor; String? get phone; String? get address; String? get email; String? get websiteUrl; String? get workingHours; double? get latitude; double? get longitude; bool get hasLocation; String? get description; bool get isActive;
/// Create a copy of GuideItem
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$GuideItemCopyWith<GuideItem> get copyWith => _$GuideItemCopyWithImpl<GuideItem>(this as GuideItem, _$identity);

  /// Serializes this GuideItem to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is GuideItem&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.categoryIcon, categoryIcon) || other.categoryIcon == categoryIcon)&&(identical(other.categoryColor, categoryColor) || other.categoryColor == categoryColor)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.address, address) || other.address == address)&&(identical(other.email, email) || other.email == email)&&(identical(other.websiteUrl, websiteUrl) || other.websiteUrl == websiteUrl)&&(identical(other.workingHours, workingHours) || other.workingHours == workingHours)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.hasLocation, hasLocation) || other.hasLocation == hasLocation)&&(identical(other.description, description) || other.description == description)&&(identical(other.isActive, isActive) || other.isActive == isActive));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,categoryId,categoryName,categoryIcon,categoryColor,phone,address,email,websiteUrl,workingHours,latitude,longitude,hasLocation,description,isActive);

@override
String toString() {
  return 'GuideItem(id: $id, name: $name, categoryId: $categoryId, categoryName: $categoryName, categoryIcon: $categoryIcon, categoryColor: $categoryColor, phone: $phone, address: $address, email: $email, websiteUrl: $websiteUrl, workingHours: $workingHours, latitude: $latitude, longitude: $longitude, hasLocation: $hasLocation, description: $description, isActive: $isActive)';
}


}

/// @nodoc
abstract mixin class $GuideItemCopyWith<$Res>  {
  factory $GuideItemCopyWith(GuideItem value, $Res Function(GuideItem) _then) = _$GuideItemCopyWithImpl;
@useResult
$Res call({
 String id, String name, String? categoryId, String? categoryName, String? categoryIcon, String? categoryColor, String? phone, String? address, String? email, String? websiteUrl, String? workingHours, double? latitude, double? longitude, bool hasLocation, String? description, bool isActive
});




}
/// @nodoc
class _$GuideItemCopyWithImpl<$Res>
    implements $GuideItemCopyWith<$Res> {
  _$GuideItemCopyWithImpl(this._self, this._then);

  final GuideItem _self;
  final $Res Function(GuideItem) _then;

/// Create a copy of GuideItem
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? categoryId = freezed,Object? categoryName = freezed,Object? categoryIcon = freezed,Object? categoryColor = freezed,Object? phone = freezed,Object? address = freezed,Object? email = freezed,Object? websiteUrl = freezed,Object? workingHours = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? hasLocation = null,Object? description = freezed,Object? isActive = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,categoryId: freezed == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,categoryIcon: freezed == categoryIcon ? _self.categoryIcon : categoryIcon // ignore: cast_nullable_to_non_nullable
as String?,categoryColor: freezed == categoryColor ? _self.categoryColor : categoryColor // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,websiteUrl: freezed == websiteUrl ? _self.websiteUrl : websiteUrl // ignore: cast_nullable_to_non_nullable
as String?,workingHours: freezed == workingHours ? _self.workingHours : workingHours // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,hasLocation: null == hasLocation ? _self.hasLocation : hasLocation // ignore: cast_nullable_to_non_nullable
as bool,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [GuideItem].
extension GuideItemPatterns on GuideItem {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _GuideItem value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _GuideItem() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _GuideItem value)  $default,){
final _that = this;
switch (_that) {
case _GuideItem():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _GuideItem value)?  $default,){
final _that = this;
switch (_that) {
case _GuideItem() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String? categoryId,  String? categoryName,  String? categoryIcon,  String? categoryColor,  String? phone,  String? address,  String? email,  String? websiteUrl,  String? workingHours,  double? latitude,  double? longitude,  bool hasLocation,  String? description,  bool isActive)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _GuideItem() when $default != null:
return $default(_that.id,_that.name,_that.categoryId,_that.categoryName,_that.categoryIcon,_that.categoryColor,_that.phone,_that.address,_that.email,_that.websiteUrl,_that.workingHours,_that.latitude,_that.longitude,_that.hasLocation,_that.description,_that.isActive);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String? categoryId,  String? categoryName,  String? categoryIcon,  String? categoryColor,  String? phone,  String? address,  String? email,  String? websiteUrl,  String? workingHours,  double? latitude,  double? longitude,  bool hasLocation,  String? description,  bool isActive)  $default,) {final _that = this;
switch (_that) {
case _GuideItem():
return $default(_that.id,_that.name,_that.categoryId,_that.categoryName,_that.categoryIcon,_that.categoryColor,_that.phone,_that.address,_that.email,_that.websiteUrl,_that.workingHours,_that.latitude,_that.longitude,_that.hasLocation,_that.description,_that.isActive);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String? categoryId,  String? categoryName,  String? categoryIcon,  String? categoryColor,  String? phone,  String? address,  String? email,  String? websiteUrl,  String? workingHours,  double? latitude,  double? longitude,  bool hasLocation,  String? description,  bool isActive)?  $default,) {final _that = this;
switch (_that) {
case _GuideItem() when $default != null:
return $default(_that.id,_that.name,_that.categoryId,_that.categoryName,_that.categoryIcon,_that.categoryColor,_that.phone,_that.address,_that.email,_that.websiteUrl,_that.workingHours,_that.latitude,_that.longitude,_that.hasLocation,_that.description,_that.isActive);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _GuideItem extends GuideItem {
  const _GuideItem({required this.id, required this.name, this.categoryId, this.categoryName, this.categoryIcon, this.categoryColor, this.phone, this.address, this.email, this.websiteUrl, this.workingHours, this.latitude, this.longitude, this.hasLocation = false, this.description, this.isActive = true}): super._();
  factory _GuideItem.fromJson(Map<String, dynamic> json) => _$GuideItemFromJson(json);

@override final  String id;
@override final  String name;
@override final  String? categoryId;
@override final  String? categoryName;
@override final  String? categoryIcon;
@override final  String? categoryColor;
@override final  String? phone;
@override final  String? address;
@override final  String? email;
@override final  String? websiteUrl;
@override final  String? workingHours;
@override final  double? latitude;
@override final  double? longitude;
@override@JsonKey() final  bool hasLocation;
@override final  String? description;
@override@JsonKey() final  bool isActive;

/// Create a copy of GuideItem
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$GuideItemCopyWith<_GuideItem> get copyWith => __$GuideItemCopyWithImpl<_GuideItem>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$GuideItemToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _GuideItem&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.categoryIcon, categoryIcon) || other.categoryIcon == categoryIcon)&&(identical(other.categoryColor, categoryColor) || other.categoryColor == categoryColor)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.address, address) || other.address == address)&&(identical(other.email, email) || other.email == email)&&(identical(other.websiteUrl, websiteUrl) || other.websiteUrl == websiteUrl)&&(identical(other.workingHours, workingHours) || other.workingHours == workingHours)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.hasLocation, hasLocation) || other.hasLocation == hasLocation)&&(identical(other.description, description) || other.description == description)&&(identical(other.isActive, isActive) || other.isActive == isActive));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,categoryId,categoryName,categoryIcon,categoryColor,phone,address,email,websiteUrl,workingHours,latitude,longitude,hasLocation,description,isActive);

@override
String toString() {
  return 'GuideItem(id: $id, name: $name, categoryId: $categoryId, categoryName: $categoryName, categoryIcon: $categoryIcon, categoryColor: $categoryColor, phone: $phone, address: $address, email: $email, websiteUrl: $websiteUrl, workingHours: $workingHours, latitude: $latitude, longitude: $longitude, hasLocation: $hasLocation, description: $description, isActive: $isActive)';
}


}

/// @nodoc
abstract mixin class _$GuideItemCopyWith<$Res> implements $GuideItemCopyWith<$Res> {
  factory _$GuideItemCopyWith(_GuideItem value, $Res Function(_GuideItem) _then) = __$GuideItemCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String? categoryId, String? categoryName, String? categoryIcon, String? categoryColor, String? phone, String? address, String? email, String? websiteUrl, String? workingHours, double? latitude, double? longitude, bool hasLocation, String? description, bool isActive
});




}
/// @nodoc
class __$GuideItemCopyWithImpl<$Res>
    implements _$GuideItemCopyWith<$Res> {
  __$GuideItemCopyWithImpl(this._self, this._then);

  final _GuideItem _self;
  final $Res Function(_GuideItem) _then;

/// Create a copy of GuideItem
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? categoryId = freezed,Object? categoryName = freezed,Object? categoryIcon = freezed,Object? categoryColor = freezed,Object? phone = freezed,Object? address = freezed,Object? email = freezed,Object? websiteUrl = freezed,Object? workingHours = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? hasLocation = null,Object? description = freezed,Object? isActive = null,}) {
  return _then(_GuideItem(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,categoryId: freezed == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String?,categoryName: freezed == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String?,categoryIcon: freezed == categoryIcon ? _self.categoryIcon : categoryIcon // ignore: cast_nullable_to_non_nullable
as String?,categoryColor: freezed == categoryColor ? _self.categoryColor : categoryColor // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,email: freezed == email ? _self.email : email // ignore: cast_nullable_to_non_nullable
as String?,websiteUrl: freezed == websiteUrl ? _self.websiteUrl : websiteUrl // ignore: cast_nullable_to_non_nullable
as String?,workingHours: freezed == workingHours ? _self.workingHours : workingHours // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,hasLocation: null == hasLocation ? _self.hasLocation : hasLocation // ignore: cast_nullable_to_non_nullable
as bool,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
