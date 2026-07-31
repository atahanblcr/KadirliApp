// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'ad_detail.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$AdDetail {

 String get id; String get title; String get description; double? get price; String get status; String get categoryId; String get categoryName; String get userId; String? get sellerName; String get contactPhone; int get viewCount; DateTime get createdAt; DateTime get expiresAt; List<AdImage> get images; List<AdPropertyValue> get properties;
/// Create a copy of AdDetail
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AdDetailCopyWith<AdDetail> get copyWith => _$AdDetailCopyWithImpl<AdDetail>(this as AdDetail, _$identity);

  /// Serializes this AdDetail to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AdDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.userId, userId) || other.userId == userId)&&(identical(other.sellerName, sellerName) || other.sellerName == sellerName)&&(identical(other.contactPhone, contactPhone) || other.contactPhone == contactPhone)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&const DeepCollectionEquality().equals(other.images, images)&&const DeepCollectionEquality().equals(other.properties, properties));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,description,price,status,categoryId,categoryName,userId,sellerName,contactPhone,viewCount,createdAt,expiresAt,const DeepCollectionEquality().hash(images),const DeepCollectionEquality().hash(properties));

@override
String toString() {
  return 'AdDetail(id: $id, title: $title, description: $description, price: $price, status: $status, categoryId: $categoryId, categoryName: $categoryName, userId: $userId, sellerName: $sellerName, contactPhone: $contactPhone, viewCount: $viewCount, createdAt: $createdAt, expiresAt: $expiresAt, images: $images, properties: $properties)';
}


}

/// @nodoc
abstract mixin class $AdDetailCopyWith<$Res>  {
  factory $AdDetailCopyWith(AdDetail value, $Res Function(AdDetail) _then) = _$AdDetailCopyWithImpl;
@useResult
$Res call({
 String id, String title, String description, double? price, String status, String categoryId, String categoryName, String userId, String? sellerName, String contactPhone, int viewCount, DateTime createdAt, DateTime expiresAt, List<AdImage> images, List<AdPropertyValue> properties
});




}
/// @nodoc
class _$AdDetailCopyWithImpl<$Res>
    implements $AdDetailCopyWith<$Res> {
  _$AdDetailCopyWithImpl(this._self, this._then);

  final AdDetail _self;
  final $Res Function(AdDetail) _then;

/// Create a copy of AdDetail
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? description = null,Object? price = freezed,Object? status = null,Object? categoryId = null,Object? categoryName = null,Object? userId = null,Object? sellerName = freezed,Object? contactPhone = null,Object? viewCount = null,Object? createdAt = null,Object? expiresAt = null,Object? images = null,Object? properties = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: null == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,categoryId: null == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String,categoryName: null == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String,userId: null == userId ? _self.userId : userId // ignore: cast_nullable_to_non_nullable
as String,sellerName: freezed == sellerName ? _self.sellerName : sellerName // ignore: cast_nullable_to_non_nullable
as String?,contactPhone: null == contactPhone ? _self.contactPhone : contactPhone // ignore: cast_nullable_to_non_nullable
as String,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,images: null == images ? _self.images : images // ignore: cast_nullable_to_non_nullable
as List<AdImage>,properties: null == properties ? _self.properties : properties // ignore: cast_nullable_to_non_nullable
as List<AdPropertyValue>,
  ));
}

}


/// Adds pattern-matching-related methods to [AdDetail].
extension AdDetailPatterns on AdDetail {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AdDetail value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AdDetail() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AdDetail value)  $default,){
final _that = this;
switch (_that) {
case _AdDetail():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AdDetail value)?  $default,){
final _that = this;
switch (_that) {
case _AdDetail() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String description,  double? price,  String status,  String categoryId,  String categoryName,  String userId,  String? sellerName,  String contactPhone,  int viewCount,  DateTime createdAt,  DateTime expiresAt,  List<AdImage> images,  List<AdPropertyValue> properties)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AdDetail() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.categoryId,_that.categoryName,_that.userId,_that.sellerName,_that.contactPhone,_that.viewCount,_that.createdAt,_that.expiresAt,_that.images,_that.properties);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String description,  double? price,  String status,  String categoryId,  String categoryName,  String userId,  String? sellerName,  String contactPhone,  int viewCount,  DateTime createdAt,  DateTime expiresAt,  List<AdImage> images,  List<AdPropertyValue> properties)  $default,) {final _that = this;
switch (_that) {
case _AdDetail():
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.categoryId,_that.categoryName,_that.userId,_that.sellerName,_that.contactPhone,_that.viewCount,_that.createdAt,_that.expiresAt,_that.images,_that.properties);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String description,  double? price,  String status,  String categoryId,  String categoryName,  String userId,  String? sellerName,  String contactPhone,  int viewCount,  DateTime createdAt,  DateTime expiresAt,  List<AdImage> images,  List<AdPropertyValue> properties)?  $default,) {final _that = this;
switch (_that) {
case _AdDetail() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.categoryId,_that.categoryName,_that.userId,_that.sellerName,_that.contactPhone,_that.viewCount,_that.createdAt,_that.expiresAt,_that.images,_that.properties);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AdDetail extends AdDetail {
  const _AdDetail({required this.id, required this.title, this.description = '', this.price, this.status = 'approved', required this.categoryId, this.categoryName = '', this.userId = '', this.sellerName, this.contactPhone = '', this.viewCount = 0, required this.createdAt, required this.expiresAt, final  List<AdImage> images = const <AdImage>[], final  List<AdPropertyValue> properties = const <AdPropertyValue>[]}): _images = images,_properties = properties,super._();
  factory _AdDetail.fromJson(Map<String, dynamic> json) => _$AdDetailFromJson(json);

@override final  String id;
@override final  String title;
@override@JsonKey() final  String description;
@override final  double? price;
@override@JsonKey() final  String status;
@override final  String categoryId;
@override@JsonKey() final  String categoryName;
@override@JsonKey() final  String userId;
@override final  String? sellerName;
@override@JsonKey() final  String contactPhone;
@override@JsonKey() final  int viewCount;
@override final  DateTime createdAt;
@override final  DateTime expiresAt;
 final  List<AdImage> _images;
@override@JsonKey() List<AdImage> get images {
  if (_images is EqualUnmodifiableListView) return _images;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_images);
}

 final  List<AdPropertyValue> _properties;
@override@JsonKey() List<AdPropertyValue> get properties {
  if (_properties is EqualUnmodifiableListView) return _properties;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_properties);
}


/// Create a copy of AdDetail
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AdDetailCopyWith<_AdDetail> get copyWith => __$AdDetailCopyWithImpl<_AdDetail>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AdDetailToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AdDetail&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.userId, userId) || other.userId == userId)&&(identical(other.sellerName, sellerName) || other.sellerName == sellerName)&&(identical(other.contactPhone, contactPhone) || other.contactPhone == contactPhone)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&const DeepCollectionEquality().equals(other._images, _images)&&const DeepCollectionEquality().equals(other._properties, _properties));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,description,price,status,categoryId,categoryName,userId,sellerName,contactPhone,viewCount,createdAt,expiresAt,const DeepCollectionEquality().hash(_images),const DeepCollectionEquality().hash(_properties));

@override
String toString() {
  return 'AdDetail(id: $id, title: $title, description: $description, price: $price, status: $status, categoryId: $categoryId, categoryName: $categoryName, userId: $userId, sellerName: $sellerName, contactPhone: $contactPhone, viewCount: $viewCount, createdAt: $createdAt, expiresAt: $expiresAt, images: $images, properties: $properties)';
}


}

/// @nodoc
abstract mixin class _$AdDetailCopyWith<$Res> implements $AdDetailCopyWith<$Res> {
  factory _$AdDetailCopyWith(_AdDetail value, $Res Function(_AdDetail) _then) = __$AdDetailCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String description, double? price, String status, String categoryId, String categoryName, String userId, String? sellerName, String contactPhone, int viewCount, DateTime createdAt, DateTime expiresAt, List<AdImage> images, List<AdPropertyValue> properties
});




}
/// @nodoc
class __$AdDetailCopyWithImpl<$Res>
    implements _$AdDetailCopyWith<$Res> {
  __$AdDetailCopyWithImpl(this._self, this._then);

  final _AdDetail _self;
  final $Res Function(_AdDetail) _then;

/// Create a copy of AdDetail
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? description = null,Object? price = freezed,Object? status = null,Object? categoryId = null,Object? categoryName = null,Object? userId = null,Object? sellerName = freezed,Object? contactPhone = null,Object? viewCount = null,Object? createdAt = null,Object? expiresAt = null,Object? images = null,Object? properties = null,}) {
  return _then(_AdDetail(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: null == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,categoryId: null == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String,categoryName: null == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String,userId: null == userId ? _self.userId : userId // ignore: cast_nullable_to_non_nullable
as String,sellerName: freezed == sellerName ? _self.sellerName : sellerName // ignore: cast_nullable_to_non_nullable
as String?,contactPhone: null == contactPhone ? _self.contactPhone : contactPhone // ignore: cast_nullable_to_non_nullable
as String,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,images: null == images ? _self._images : images // ignore: cast_nullable_to_non_nullable
as List<AdImage>,properties: null == properties ? _self._properties : properties // ignore: cast_nullable_to_non_nullable
as List<AdPropertyValue>,
  ));
}


}


/// @nodoc
mixin _$AdImage {

 String get id; String get fileId; String? get url; bool get isCover; int get displayOrder;
/// Create a copy of AdImage
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AdImageCopyWith<AdImage> get copyWith => _$AdImageCopyWithImpl<AdImage>(this as AdImage, _$identity);

  /// Serializes this AdImage to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AdImage&&(identical(other.id, id) || other.id == id)&&(identical(other.fileId, fileId) || other.fileId == fileId)&&(identical(other.url, url) || other.url == url)&&(identical(other.isCover, isCover) || other.isCover == isCover)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,fileId,url,isCover,displayOrder);

@override
String toString() {
  return 'AdImage(id: $id, fileId: $fileId, url: $url, isCover: $isCover, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class $AdImageCopyWith<$Res>  {
  factory $AdImageCopyWith(AdImage value, $Res Function(AdImage) _then) = _$AdImageCopyWithImpl;
@useResult
$Res call({
 String id, String fileId, String? url, bool isCover, int displayOrder
});




}
/// @nodoc
class _$AdImageCopyWithImpl<$Res>
    implements $AdImageCopyWith<$Res> {
  _$AdImageCopyWithImpl(this._self, this._then);

  final AdImage _self;
  final $Res Function(AdImage) _then;

/// Create a copy of AdImage
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? fileId = null,Object? url = freezed,Object? isCover = null,Object? displayOrder = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,fileId: null == fileId ? _self.fileId : fileId // ignore: cast_nullable_to_non_nullable
as String,url: freezed == url ? _self.url : url // ignore: cast_nullable_to_non_nullable
as String?,isCover: null == isCover ? _self.isCover : isCover // ignore: cast_nullable_to_non_nullable
as bool,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}

}


/// Adds pattern-matching-related methods to [AdImage].
extension AdImagePatterns on AdImage {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AdImage value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AdImage() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AdImage value)  $default,){
final _that = this;
switch (_that) {
case _AdImage():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AdImage value)?  $default,){
final _that = this;
switch (_that) {
case _AdImage() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String fileId,  String? url,  bool isCover,  int displayOrder)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AdImage() when $default != null:
return $default(_that.id,_that.fileId,_that.url,_that.isCover,_that.displayOrder);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String fileId,  String? url,  bool isCover,  int displayOrder)  $default,) {final _that = this;
switch (_that) {
case _AdImage():
return $default(_that.id,_that.fileId,_that.url,_that.isCover,_that.displayOrder);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String fileId,  String? url,  bool isCover,  int displayOrder)?  $default,) {final _that = this;
switch (_that) {
case _AdImage() when $default != null:
return $default(_that.id,_that.fileId,_that.url,_that.isCover,_that.displayOrder);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AdImage extends AdImage {
  const _AdImage({required this.id, this.fileId = '', this.url, this.isCover = false, this.displayOrder = 0}): super._();
  factory _AdImage.fromJson(Map<String, dynamic> json) => _$AdImageFromJson(json);

@override final  String id;
@override@JsonKey() final  String fileId;
@override final  String? url;
@override@JsonKey() final  bool isCover;
@override@JsonKey() final  int displayOrder;

/// Create a copy of AdImage
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AdImageCopyWith<_AdImage> get copyWith => __$AdImageCopyWithImpl<_AdImage>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AdImageToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AdImage&&(identical(other.id, id) || other.id == id)&&(identical(other.fileId, fileId) || other.fileId == fileId)&&(identical(other.url, url) || other.url == url)&&(identical(other.isCover, isCover) || other.isCover == isCover)&&(identical(other.displayOrder, displayOrder) || other.displayOrder == displayOrder));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,fileId,url,isCover,displayOrder);

@override
String toString() {
  return 'AdImage(id: $id, fileId: $fileId, url: $url, isCover: $isCover, displayOrder: $displayOrder)';
}


}

/// @nodoc
abstract mixin class _$AdImageCopyWith<$Res> implements $AdImageCopyWith<$Res> {
  factory _$AdImageCopyWith(_AdImage value, $Res Function(_AdImage) _then) = __$AdImageCopyWithImpl;
@override @useResult
$Res call({
 String id, String fileId, String? url, bool isCover, int displayOrder
});




}
/// @nodoc
class __$AdImageCopyWithImpl<$Res>
    implements _$AdImageCopyWith<$Res> {
  __$AdImageCopyWithImpl(this._self, this._then);

  final _AdImage _self;
  final $Res Function(_AdImage) _then;

/// Create a copy of AdImage
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? fileId = null,Object? url = freezed,Object? isCover = null,Object? displayOrder = null,}) {
  return _then(_AdImage(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,fileId: null == fileId ? _self.fileId : fileId // ignore: cast_nullable_to_non_nullable
as String,url: freezed == url ? _self.url : url // ignore: cast_nullable_to_non_nullable
as String?,isCover: null == isCover ? _self.isCover : isCover // ignore: cast_nullable_to_non_nullable
as bool,displayOrder: null == displayOrder ? _self.displayOrder : displayOrder // ignore: cast_nullable_to_non_nullable
as int,
  ));
}


}


/// @nodoc
mixin _$AdPropertyValue {

 String get propertyId; String get propertyName; String get propertyType; String get value;
/// Create a copy of AdPropertyValue
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AdPropertyValueCopyWith<AdPropertyValue> get copyWith => _$AdPropertyValueCopyWithImpl<AdPropertyValue>(this as AdPropertyValue, _$identity);

  /// Serializes this AdPropertyValue to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AdPropertyValue&&(identical(other.propertyId, propertyId) || other.propertyId == propertyId)&&(identical(other.propertyName, propertyName) || other.propertyName == propertyName)&&(identical(other.propertyType, propertyType) || other.propertyType == propertyType)&&(identical(other.value, value) || other.value == value));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,propertyId,propertyName,propertyType,value);

@override
String toString() {
  return 'AdPropertyValue(propertyId: $propertyId, propertyName: $propertyName, propertyType: $propertyType, value: $value)';
}


}

/// @nodoc
abstract mixin class $AdPropertyValueCopyWith<$Res>  {
  factory $AdPropertyValueCopyWith(AdPropertyValue value, $Res Function(AdPropertyValue) _then) = _$AdPropertyValueCopyWithImpl;
@useResult
$Res call({
 String propertyId, String propertyName, String propertyType, String value
});




}
/// @nodoc
class _$AdPropertyValueCopyWithImpl<$Res>
    implements $AdPropertyValueCopyWith<$Res> {
  _$AdPropertyValueCopyWithImpl(this._self, this._then);

  final AdPropertyValue _self;
  final $Res Function(AdPropertyValue) _then;

/// Create a copy of AdPropertyValue
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? propertyId = null,Object? propertyName = null,Object? propertyType = null,Object? value = null,}) {
  return _then(_self.copyWith(
propertyId: null == propertyId ? _self.propertyId : propertyId // ignore: cast_nullable_to_non_nullable
as String,propertyName: null == propertyName ? _self.propertyName : propertyName // ignore: cast_nullable_to_non_nullable
as String,propertyType: null == propertyType ? _self.propertyType : propertyType // ignore: cast_nullable_to_non_nullable
as String,value: null == value ? _self.value : value // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [AdPropertyValue].
extension AdPropertyValuePatterns on AdPropertyValue {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AdPropertyValue value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AdPropertyValue() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AdPropertyValue value)  $default,){
final _that = this;
switch (_that) {
case _AdPropertyValue():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AdPropertyValue value)?  $default,){
final _that = this;
switch (_that) {
case _AdPropertyValue() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String propertyId,  String propertyName,  String propertyType,  String value)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AdPropertyValue() when $default != null:
return $default(_that.propertyId,_that.propertyName,_that.propertyType,_that.value);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String propertyId,  String propertyName,  String propertyType,  String value)  $default,) {final _that = this;
switch (_that) {
case _AdPropertyValue():
return $default(_that.propertyId,_that.propertyName,_that.propertyType,_that.value);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String propertyId,  String propertyName,  String propertyType,  String value)?  $default,) {final _that = this;
switch (_that) {
case _AdPropertyValue() when $default != null:
return $default(_that.propertyId,_that.propertyName,_that.propertyType,_that.value);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AdPropertyValue extends AdPropertyValue {
  const _AdPropertyValue({required this.propertyId, this.propertyName = '', this.propertyType = 'Text', this.value = ''}): super._();
  factory _AdPropertyValue.fromJson(Map<String, dynamic> json) => _$AdPropertyValueFromJson(json);

@override final  String propertyId;
@override@JsonKey() final  String propertyName;
@override@JsonKey() final  String propertyType;
@override@JsonKey() final  String value;

/// Create a copy of AdPropertyValue
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AdPropertyValueCopyWith<_AdPropertyValue> get copyWith => __$AdPropertyValueCopyWithImpl<_AdPropertyValue>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AdPropertyValueToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AdPropertyValue&&(identical(other.propertyId, propertyId) || other.propertyId == propertyId)&&(identical(other.propertyName, propertyName) || other.propertyName == propertyName)&&(identical(other.propertyType, propertyType) || other.propertyType == propertyType)&&(identical(other.value, value) || other.value == value));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,propertyId,propertyName,propertyType,value);

@override
String toString() {
  return 'AdPropertyValue(propertyId: $propertyId, propertyName: $propertyName, propertyType: $propertyType, value: $value)';
}


}

/// @nodoc
abstract mixin class _$AdPropertyValueCopyWith<$Res> implements $AdPropertyValueCopyWith<$Res> {
  factory _$AdPropertyValueCopyWith(_AdPropertyValue value, $Res Function(_AdPropertyValue) _then) = __$AdPropertyValueCopyWithImpl;
@override @useResult
$Res call({
 String propertyId, String propertyName, String propertyType, String value
});




}
/// @nodoc
class __$AdPropertyValueCopyWithImpl<$Res>
    implements _$AdPropertyValueCopyWith<$Res> {
  __$AdPropertyValueCopyWithImpl(this._self, this._then);

  final _AdPropertyValue _self;
  final $Res Function(_AdPropertyValue) _then;

/// Create a copy of AdPropertyValue
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? propertyId = null,Object? propertyName = null,Object? propertyType = null,Object? value = null,}) {
  return _then(_AdPropertyValue(
propertyId: null == propertyId ? _self.propertyId : propertyId // ignore: cast_nullable_to_non_nullable
as String,propertyName: null == propertyName ? _self.propertyName : propertyName // ignore: cast_nullable_to_non_nullable
as String,propertyType: null == propertyType ? _self.propertyType : propertyType // ignore: cast_nullable_to_non_nullable
as String,value: null == value ? _self.value : value // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}

// dart format on
