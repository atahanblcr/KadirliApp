// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'ad_summary.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$AdSummary {

 String get id; String get title; String? get description; double? get price; String get status; String get contactPhone; int get viewCount; DateTime get createdAt; List<String> get imageUrls;
/// Create a copy of AdSummary
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$AdSummaryCopyWith<AdSummary> get copyWith => _$AdSummaryCopyWithImpl<AdSummary>(this as AdSummary, _$identity);

  /// Serializes this AdSummary to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is AdSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.contactPhone, contactPhone) || other.contactPhone == contactPhone)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&const DeepCollectionEquality().equals(other.imageUrls, imageUrls));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,description,price,status,contactPhone,viewCount,createdAt,const DeepCollectionEquality().hash(imageUrls));

@override
String toString() {
  return 'AdSummary(id: $id, title: $title, description: $description, price: $price, status: $status, contactPhone: $contactPhone, viewCount: $viewCount, createdAt: $createdAt, imageUrls: $imageUrls)';
}


}

/// @nodoc
abstract mixin class $AdSummaryCopyWith<$Res>  {
  factory $AdSummaryCopyWith(AdSummary value, $Res Function(AdSummary) _then) = _$AdSummaryCopyWithImpl;
@useResult
$Res call({
 String id, String title, String? description, double? price, String status, String contactPhone, int viewCount, DateTime createdAt, List<String> imageUrls
});




}
/// @nodoc
class _$AdSummaryCopyWithImpl<$Res>
    implements $AdSummaryCopyWith<$Res> {
  _$AdSummaryCopyWithImpl(this._self, this._then);

  final AdSummary _self;
  final $Res Function(AdSummary) _then;

/// Create a copy of AdSummary
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? description = freezed,Object? price = freezed,Object? status = null,Object? contactPhone = null,Object? viewCount = null,Object? createdAt = null,Object? imageUrls = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,contactPhone: null == contactPhone ? _self.contactPhone : contactPhone // ignore: cast_nullable_to_non_nullable
as String,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,imageUrls: null == imageUrls ? _self.imageUrls : imageUrls // ignore: cast_nullable_to_non_nullable
as List<String>,
  ));
}

}


/// Adds pattern-matching-related methods to [AdSummary].
extension AdSummaryPatterns on AdSummary {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _AdSummary value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _AdSummary() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _AdSummary value)  $default,){
final _that = this;
switch (_that) {
case _AdSummary():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _AdSummary value)?  $default,){
final _that = this;
switch (_that) {
case _AdSummary() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String? description,  double? price,  String status,  String contactPhone,  int viewCount,  DateTime createdAt,  List<String> imageUrls)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _AdSummary() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.contactPhone,_that.viewCount,_that.createdAt,_that.imageUrls);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String? description,  double? price,  String status,  String contactPhone,  int viewCount,  DateTime createdAt,  List<String> imageUrls)  $default,) {final _that = this;
switch (_that) {
case _AdSummary():
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.contactPhone,_that.viewCount,_that.createdAt,_that.imageUrls);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String? description,  double? price,  String status,  String contactPhone,  int viewCount,  DateTime createdAt,  List<String> imageUrls)?  $default,) {final _that = this;
switch (_that) {
case _AdSummary() when $default != null:
return $default(_that.id,_that.title,_that.description,_that.price,_that.status,_that.contactPhone,_that.viewCount,_that.createdAt,_that.imageUrls);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _AdSummary extends AdSummary {
  const _AdSummary({required this.id, required this.title, this.description, this.price, this.status = 'approved', this.contactPhone = '', this.viewCount = 0, required this.createdAt, final  List<String> imageUrls = const <String>[]}): _imageUrls = imageUrls,super._();
  factory _AdSummary.fromJson(Map<String, dynamic> json) => _$AdSummaryFromJson(json);

@override final  String id;
@override final  String title;
@override final  String? description;
@override final  double? price;
@override@JsonKey() final  String status;
@override@JsonKey() final  String contactPhone;
@override@JsonKey() final  int viewCount;
@override final  DateTime createdAt;
 final  List<String> _imageUrls;
@override@JsonKey() List<String> get imageUrls {
  if (_imageUrls is EqualUnmodifiableListView) return _imageUrls;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_imageUrls);
}


/// Create a copy of AdSummary
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$AdSummaryCopyWith<_AdSummary> get copyWith => __$AdSummaryCopyWithImpl<_AdSummary>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$AdSummaryToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _AdSummary&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.price, price) || other.price == price)&&(identical(other.status, status) || other.status == status)&&(identical(other.contactPhone, contactPhone) || other.contactPhone == contactPhone)&&(identical(other.viewCount, viewCount) || other.viewCount == viewCount)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt)&&const DeepCollectionEquality().equals(other._imageUrls, _imageUrls));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,description,price,status,contactPhone,viewCount,createdAt,const DeepCollectionEquality().hash(_imageUrls));

@override
String toString() {
  return 'AdSummary(id: $id, title: $title, description: $description, price: $price, status: $status, contactPhone: $contactPhone, viewCount: $viewCount, createdAt: $createdAt, imageUrls: $imageUrls)';
}


}

/// @nodoc
abstract mixin class _$AdSummaryCopyWith<$Res> implements $AdSummaryCopyWith<$Res> {
  factory _$AdSummaryCopyWith(_AdSummary value, $Res Function(_AdSummary) _then) = __$AdSummaryCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String? description, double? price, String status, String contactPhone, int viewCount, DateTime createdAt, List<String> imageUrls
});




}
/// @nodoc
class __$AdSummaryCopyWithImpl<$Res>
    implements _$AdSummaryCopyWith<$Res> {
  __$AdSummaryCopyWithImpl(this._self, this._then);

  final _AdSummary _self;
  final $Res Function(_AdSummary) _then;

/// Create a copy of AdSummary
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? description = freezed,Object? price = freezed,Object? status = null,Object? contactPhone = null,Object? viewCount = null,Object? createdAt = null,Object? imageUrls = null,}) {
  return _then(_AdSummary(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: freezed == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String?,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,contactPhone: null == contactPhone ? _self.contactPhone : contactPhone // ignore: cast_nullable_to_non_nullable
as String,viewCount: null == viewCount ? _self.viewCount : viewCount // ignore: cast_nullable_to_non_nullable
as int,createdAt: null == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime,imageUrls: null == imageUrls ? _self._imageUrls : imageUrls // ignore: cast_nullable_to_non_nullable
as List<String>,
  ));
}


}

// dart format on
