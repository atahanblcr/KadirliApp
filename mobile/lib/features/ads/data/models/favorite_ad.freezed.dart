// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'favorite_ad.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$FavoriteAd {

 String get adId; String get title; double? get price; String get status; bool get isAvailable; int get viewCount; DateTime get favoritedAt; List<String> get imageUrls;
/// Create a copy of FavoriteAd
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$FavoriteAdCopyWith<FavoriteAd> get copyWith => _$FavoriteAdCopyWithImpl<FavoriteAd>(this as FavoriteAd, _$identity);

  /// Serializes this FavoriteAd to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is FavoriteAd&&(identical(other.adId, adId) || other.adId == adId)&&(identical(other.title, title) || other.title == title)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.isAvailable, isAvailable) || other.isAvailable == isAvailable)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.favoritedAt, favoritedAt) || other.favoritedAt == favoritedAt)&&const DeepCollectionEquality().equals(other.imageUrls, imageUrls));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,adId,title,price,status,isAvailable,viewCount,favoritedAt,const DeepCollectionEquality().hash(imageUrls));

@override
String toString() {
  return 'FavoriteAd(adId: $adId, title: $title, price: $price, status: $status, isAvailable: $isAvailable, viewCount: $viewCount, favoritedAt: $favoritedAt, imageUrls: $imageUrls)';
}


}

/// @nodoc
abstract mixin class $FavoriteAdCopyWith<$Res>  {
  factory $FavoriteAdCopyWith(FavoriteAd value, $Res Function(FavoriteAd) _then) = _$FavoriteAdCopyWithImpl;
@useResult
$Res call({
 String adId, String title, double? price, String status, bool isAvailable, int viewCount, DateTime favoritedAt, List<String> imageUrls
});




}
/// @nodoc
class _$FavoriteAdCopyWithImpl<$Res>
    implements $FavoriteAdCopyWith<$Res> {
  _$FavoriteAdCopyWithImpl(this._self, this._then);

  final FavoriteAd _self;
  final $Res Function(FavoriteAd) _then;

/// Create a copy of FavoriteAd
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? adId = null,Object? title = null,Object? price = freezed,Object? status = null,Object? isAvailable = null,Object? viewCount = null,Object? favoritedAt = null,Object? imageUrls = null,}) {
  return _then(_self.copyWith(
adId: null == adId ? _self.adId : adId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,isAvailable: null == isAvailable ? _self.isAvailable : isAvailable // ignore: cast_nullable_to_non_nullable
as bool,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,favoritedAt: null == favoritedAt ? _self.favoritedAt : favoritedAt // ignore: cast_nullable_to_non_nullable
as DateTime,imageUrls: null == imageUrls ? _self.imageUrls : imageUrls // ignore: cast_nullable_to_non_nullable
as List<String>,
  ));
}

}


/// Adds pattern-matching-related methods to [FavoriteAd].
extension FavoriteAdPatterns on FavoriteAd {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _FavoriteAd value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _FavoriteAd() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _FavoriteAd value)  $default,){
final _that = this;
switch (_that) {
case _FavoriteAd():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _FavoriteAd value)?  $default,){
final _that = this;
switch (_that) {
case _FavoriteAd() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String adId,  String title,  double? price,  String status,  bool isAvailable,  int viewCount,  DateTime favoritedAt,  List<String> imageUrls)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _FavoriteAd() when $default != null:
return $default(_that.adId,_that.title,_that.price,_that.status,_that.isAvailable,_that.viewCount,_that.favoritedAt,_that.imageUrls);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String adId,  String title,  double? price,  String status,  bool isAvailable,  int viewCount,  DateTime favoritedAt,  List<String> imageUrls)  $default,) {final _that = this;
switch (_that) {
case _FavoriteAd():
return $default(_that.adId,_that.title,_that.price,_that.status,_that.isAvailable,_that.viewCount,_that.favoritedAt,_that.imageUrls);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String adId,  String title,  double? price,  String status,  bool isAvailable,  int viewCount,  DateTime favoritedAt,  List<String> imageUrls)?  $default,) {final _that = this;
switch (_that) {
case _FavoriteAd() when $default != null:
return $default(_that.adId,_that.title,_that.price,_that.status,_that.isAvailable,_that.viewCount,_that.favoritedAt,_that.imageUrls);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _FavoriteAd extends FavoriteAd {
  const _FavoriteAd({required this.adId, this.title = '', this.price, this.status = '', this.isAvailable = true, this.viewCount = 0, required this.favoritedAt, final  List<String> imageUrls = const <String>[]}): _imageUrls = imageUrls,super._();
  factory _FavoriteAd.fromJson(Map<String, dynamic> json) => _$FavoriteAdFromJson(json);

@override final  String adId;
@override@JsonKey() final  String title;
@override final  double? price;
@override@JsonKey() final  String status;
@override@JsonKey() final  bool isAvailable;
@override@JsonKey() final  int viewCount;
@override final  DateTime favoritedAt;
 final  List<String> _imageUrls;
@override@JsonKey() List<String> get imageUrls {
  if (_imageUrls is EqualUnmodifiableListView) return _imageUrls;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_imageUrls);
}


/// Create a copy of FavoriteAd
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$FavoriteAdCopyWith<_FavoriteAd> get copyWith => __$FavoriteAdCopyWithImpl<_FavoriteAd>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$FavoriteAdToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _FavoriteAd&&(identical(other.adId, adId) || other.adId == adId)&&(identical(other.title, title) || other.title == title)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.isAvailable, isAvailable) || other.isAvailable == isAvailable)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.favoritedAt, favoritedAt) || other.favoritedAt == favoritedAt)&&const DeepCollectionEquality().equals(other._imageUrls, _imageUrls));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,adId,title,price,status,isAvailable,viewCount,favoritedAt,const DeepCollectionEquality().hash(_imageUrls));

@override
String toString() {
  return 'FavoriteAd(adId: $adId, title: $title, price: $price, status: $status, isAvailable: $isAvailable, viewCount: $viewCount, favoritedAt: $favoritedAt, imageUrls: $imageUrls)';
}


}

/// @nodoc
abstract mixin class _$FavoriteAdCopyWith<$Res> implements $FavoriteAdCopyWith<$Res> {
  factory _$FavoriteAdCopyWith(_FavoriteAd value, $Res Function(_FavoriteAd) _then) = __$FavoriteAdCopyWithImpl;
@override @useResult
$Res call({
 String adId, String title, double? price, String status, bool isAvailable, int viewCount, DateTime favoritedAt, List<String> imageUrls
});




}
/// @nodoc
class __$FavoriteAdCopyWithImpl<$Res>
    implements _$FavoriteAdCopyWith<$Res> {
  __$FavoriteAdCopyWithImpl(this._self, this._then);

  final _FavoriteAd _self;
  final $Res Function(_FavoriteAd) _then;

/// Create a copy of FavoriteAd
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? adId = null,Object? title = null,Object? price = freezed,Object? status = null,Object? isAvailable = null,Object? viewCount = null,Object? favoritedAt = null,Object? imageUrls = null,}) {
  return _then(_FavoriteAd(
adId: null == adId ? _self.adId : adId // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,isAvailable: null == isAvailable ? _self.isAvailable : isAvailable // ignore: cast_nullable_to_non_nullable
as bool,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,favoritedAt: null == favoritedAt ? _self.favoritedAt : favoritedAt // ignore: cast_nullable_to_non_nullable
as DateTime,imageUrls: null == imageUrls ? _self._imageUrls : imageUrls // ignore: cast_nullable_to_non_nullable
as List<String>,
  ));
}


}

// dart format on
