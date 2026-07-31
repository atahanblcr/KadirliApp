// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'my_ad.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$MyAd {

 String get id; String get title; String? get description; double? get price; String get status; String get categoryId; String get categoryName; String get contactPhone; int get viewCount; int get phoneClickCount; int get whatsappClickCount; int get favoriteCount; int get extensionCount; int get maxExtensions; String? get rejectedReason; DateTime get createdAt; DateTime get expiresAt; List<String> get imageUrls;
/// Create a copy of MyAd
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$MyAdCopyWith<MyAd> get copyWith => _$MyAdCopyWithImpl<MyAd>(this as MyAd, _$identity);

  /// Serializes this MyAd to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is MyAd&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.contactPhone, contactPhone) || other.contactPhone == contactPhone)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.phoneClickCount, phoneClickCount) || other.phoneClickCount == phoneClickCount)&&(identical(other.whatsappClickCount, whatsappClickCount) || other.whatsappClickCount == whatsappClickCount)&&(identical(other.favoriteCount, favoriteCount) || other.favoriteCount == favoriteCount)&&(identical(other.extensionCount, extensionCount) || other.extensionCount == extensionCount)&&(identical(other.maxExtensions, maxExtensions) || other.maxExtensions == maxExtensions)&&(identical(other.rejectedReason, rejectedReason) || other.rejectedReason == rejectedReason)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&const DeepCollectionEquality().equals(other.imageUrls, imageUrls));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,description,price,status,categoryId,categoryName,contactPhone,viewCount,phoneClickCount,whatsappClickCount,favoriteCount,extensionCount,maxExtensions,rejectedReason,createdAt,expiresAt,const DeepCollectionEquality().hash(imageUrls));

@override
String toString() {
  return 'MyAd(id: $id, title: $title, description: $description, price: $price, status: $status, categoryId: $categoryId, categoryName: $categoryName, contactPhone: $contactPhone, viewCount: $viewCount, phoneClickCount: $phoneClickCount, whatsappClickCount: $whatsappClickCount, favoriteCount: $favoriteCount, extensionCount: $extensionCount, maxExtensions: $maxExtensions, rejectedReason: $rejectedReason, createdAt: $createdAt, expiresAt: $expiresAt, imageUrls: $imageUrls)';
}


}

/// @nodoc
abstract mixin class $MyAdCopyWith<$Res>  {
  factory $MyAdCopyWith(MyAd value, $Res Function(MyAd) _then) = _$MyAdCopyWithImpl;
@useResult
$Res call({
 String id, String title, String? description, double? price, String status, String categoryId, String categoryName, String contactPhone, int viewCount, int phoneClickCount, int whatsappClickCount, int favoriteCount, int extensionCount, int maxExtensions, String? rejectedReason, DateTime createdAt, DateTime expiresAt, List<String> imageUrls
});




}
/// @nodoc
class _$MyAdCopyWithImpl<$Res>
    implements $MyAdCopyWith<$Res> {
  _$MyAdCopyWithImpl(this._self, this._then);

  final MyAd _self;
  final $Res Function(MyAd) _then;

/// Create a copy of MyAd
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? description = freezed,Object? price = freezed,Object? status = null,Object? categoryId = null,Object? categoryName = null,Object? contactPhone = null,Object? viewCount = null,Object? phoneClickCount = null,Object? whatsappClickCount = null,Object? favoriteCount = null,Object? extensionCount = null,Object? maxExtensions = null,Object? rejectedReason = freezed,Object? createdAt = null,Object? expiresAt = null,Object? imageUrls = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,categoryId: null == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String,categoryName: null == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String,contactPhone: null == contactPhone ? _self.contactPhone : contactPhone // ignore: cast_nullable_to_non_nullable
as String,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,phoneClickCount: null == phoneClickCount ? _self.phoneClickCount : phoneClickCount // ignore: cast_nullable_to_non_nullable
as int,whatsappClickCount: null == whatsappClickCount ? _self.whatsappClickCount : whatsappClickCount // ignore: cast_nullable_to_non_nullable
as int,favoriteCount: null == favoriteCount ? _self.favoriteCount : favoriteCount // ignore: cast_nullable_to_non_nullable
as int,extensionCount: null == extensionCount ? _self.extensionCount : extensionCount // ignore: cast_nullable_to_non_nullable
as int,maxExtensions: null == maxExtensions ? _self.maxExtensions : maxExtensions // ignore: cast_nullable_to_non_nullable
as int,rejectedReason: freezed == rejectedReason ? _self.rejectedReason : rejectedReason // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,imageUrls: null == imageUrls ? _self.imageUrls : imageUrls // ignore: cast_nullable_to_non_nullable
as List<String>,
  ));
}

}


/// Adds pattern-matching-related methods to [MyAd].
extension MyAdPatterns on MyAd {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _MyAd value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _MyAd() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _MyAd value)  $default,){
final _that = this;
switch (_that) {
case _MyAd():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _MyAd value)?  $default,){
final _that = this;
switch (_that) {
case _MyAd() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String? description,  double? price,  String status,  String categoryId,  String categoryName,  String contactPhone,  int viewCount,  int phoneClickCount,  int whatsappClickCount,  int favoriteCount,  int extensionCount,  int maxExtensions,  String? rejectedReason,  DateTime createdAt,  DateTime expiresAt,  List<String> imageUrls)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _MyAd() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.categoryId,_that.categoryName,_that.contactPhone,_that.viewCount,_that.phoneClickCount,_that.whatsappClickCount,_that.favoriteCount,_that.extensionCount,_that.maxExtensions,_that.rejectedReason,_that.createdAt,_that.expiresAt,_that.imageUrls);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String? description,  double? price,  String status,  String categoryId,  String categoryName,  String contactPhone,  int viewCount,  int phoneClickCount,  int whatsappClickCount,  int favoriteCount,  int extensionCount,  int maxExtensions,  String? rejectedReason,  DateTime createdAt,  DateTime expiresAt,  List<String> imageUrls)  $default,) {final _that = this;
switch (_that) {
case _MyAd():
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.categoryId,_that.categoryName,_that.contactPhone,_that.viewCount,_that.phoneClickCount,_that.whatsappClickCount,_that.favoriteCount,_that.extensionCount,_that.maxExtensions,_that.rejectedReason,_that.createdAt,_that.expiresAt,_that.imageUrls);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String? description,  double? price,  String status,  String categoryId,  String categoryName,  String contactPhone,  int viewCount,  int phoneClickCount,  int whatsappClickCount,  int favoriteCount,  int extensionCount,  int maxExtensions,  String? rejectedReason,  DateTime createdAt,  DateTime expiresAt,  List<String> imageUrls)?  $default,) {final _that = this;
switch (_that) {
case _MyAd() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.categoryId,_that.categoryName,_that.contactPhone,_that.viewCount,_that.phoneClickCount,_that.whatsappClickCount,_that.favoriteCount,_that.extensionCount,_that.maxExtensions,_that.rejectedReason,_that.createdAt,_that.expiresAt,_that.imageUrls);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _MyAd extends MyAd {
  const _MyAd({required this.id, required this.title, this.description, this.price, this.status = 'pending', this.categoryId = '', this.categoryName = '', this.contactPhone = '', this.viewCount = 0, this.phoneClickCount = 0, this.whatsappClickCount = 0, this.favoriteCount = 0, this.extensionCount = 0, this.maxExtensions = 0, this.rejectedReason, required this.createdAt, required this.expiresAt, final  List<String> imageUrls = const <String>[]}): _imageUrls = imageUrls,super._();
  factory _MyAd.fromJson(Map<String, dynamic> json) => _$MyAdFromJson(json);

@override final  String id;
@override final  String title;
@override final  String? description;
@override final  double? price;
@override@JsonKey() final  String status;
@override@JsonKey() final  String categoryId;
@override@JsonKey() final  String categoryName;
@override@JsonKey() final  String contactPhone;
@override@JsonKey() final  int viewCount;
@override@JsonKey() final  int phoneClickCount;
@override@JsonKey() final  int whatsappClickCount;
@override@JsonKey() final  int favoriteCount;
@override@JsonKey() final  int extensionCount;
@override@JsonKey() final  int maxExtensions;
@override final  String? rejectedReason;
@override final  DateTime createdAt;
@override final  DateTime expiresAt;
 final  List<String> _imageUrls;
@override@JsonKey() List<String> get imageUrls {
  if (_imageUrls is EqualUnmodifiableListView) return _imageUrls;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_imageUrls);
}


/// Create a copy of MyAd
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$MyAdCopyWith<_MyAd> get copyWith => __$MyAdCopyWithImpl<_MyAd>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$MyAdToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _MyAd&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.categoryId, categoryId) || other.categoryId == categoryId)&&(identical(other.categoryName, categoryName) || other.categoryName == categoryName)&&(identical(other.contactPhone, contactPhone) || other.contactPhone == contactPhone)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.phoneClickCount, phoneClickCount) || other.phoneClickCount == phoneClickCount)&&(identical(other.whatsappClickCount, whatsappClickCount) || other.whatsappClickCount == whatsappClickCount)&&(identical(other.favoriteCount, favoriteCount) || other.favoriteCount == favoriteCount)&&(identical(other.extensionCount, extensionCount) || other.extensionCount == extensionCount)&&(identical(other.maxExtensions, maxExtensions) || other.maxExtensions == maxExtensions)&&(identical(other.rejectedReason, rejectedReason) || other.rejectedReason == rejectedReason)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&(identical(other.expiresAt, expiresAt) || other.expiresAt == expiresAt)&&const DeepCollectionEquality().equals(other._imageUrls, _imageUrls));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,description,price,status,categoryId,categoryName,contactPhone,viewCount,phoneClickCount,whatsappClickCount,favoriteCount,extensionCount,maxExtensions,rejectedReason,createdAt,expiresAt,const DeepCollectionEquality().hash(_imageUrls));

@override
String toString() {
  return 'MyAd(id: $id, title: $title, description: $description, price: $price, status: $status, categoryId: $categoryId, categoryName: $categoryName, contactPhone: $contactPhone, viewCount: $viewCount, phoneClickCount: $phoneClickCount, whatsappClickCount: $whatsappClickCount, favoriteCount: $favoriteCount, extensionCount: $extensionCount, maxExtensions: $maxExtensions, rejectedReason: $rejectedReason, createdAt: $createdAt, expiresAt: $expiresAt, imageUrls: $imageUrls)';
}


}

/// @nodoc
abstract mixin class _$MyAdCopyWith<$Res> implements $MyAdCopyWith<$Res> {
  factory _$MyAdCopyWith(_MyAd value, $Res Function(_MyAd) _then) = __$MyAdCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String? description, double? price, String status, String categoryId, String categoryName, String contactPhone, int viewCount, int phoneClickCount, int whatsappClickCount, int favoriteCount, int extensionCount, int maxExtensions, String? rejectedReason, DateTime createdAt, DateTime expiresAt, List<String> imageUrls
});




}
/// @nodoc
class __$MyAdCopyWithImpl<$Res>
    implements _$MyAdCopyWith<$Res> {
  __$MyAdCopyWithImpl(this._self, this._then);

  final _MyAd _self;
  final $Res Function(_MyAd) _then;

/// Create a copy of MyAd
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? description = freezed,Object? price = freezed,Object? status = null,Object? categoryId = null,Object? categoryName = null,Object? contactPhone = null,Object? viewCount = null,Object? phoneClickCount = null,Object? whatsappClickCount = null,Object? favoriteCount = null,Object? extensionCount = null,Object? maxExtensions = null,Object? rejectedReason = freezed,Object? createdAt = null,Object? expiresAt = null,Object? imageUrls = null,}) {
  return _then(_MyAd(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,categoryId: null == categoryId ? _self.categoryId : categoryId // ignore: cast_nullable_to_non_nullable
as String,categoryName: null == categoryName ? _self.categoryName : categoryName // ignore: cast_nullable_to_non_nullable
as String,contactPhone: null == contactPhone ? _self.contactPhone : contactPhone // ignore: cast_nullable_to_non_nullable
as String,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,phoneClickCount: null == phoneClickCount ? _self.phoneClickCount : phoneClickCount // ignore: cast_nullable_to_non_nullable
as int,whatsappClickCount: null == whatsappClickCount ? _self.whatsappClickCount : whatsappClickCount // ignore: cast_nullable_to_non_nullable
as int,favoriteCount: null == favoriteCount ? _self.favoriteCount : favoriteCount // ignore: cast_nullable_to_non_nullable
as int,extensionCount: null == extensionCount ? _self.extensionCount : extensionCount // ignore: cast_nullable_to_non_nullable
as int,maxExtensions: null == maxExtensions ? _self.maxExtensions : maxExtensions // ignore: cast_nullable_to_non_nullable
as int,rejectedReason: freezed == rejectedReason ? _self.rejectedReason : rejectedReason // ignore: cast_nullable_to_non_nullable
as String?,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,expiresAt: null == expiresAt ? _self.expiresAt : expiresAt // ignore: cast_nullable_to_non_nullable
as DateTime,imageUrls: null == imageUrls ? _self._imageUrls : imageUrls // ignore: cast_nullable_to_non_nullable
as List<String>,
  ));
}


}

// dart format on
