// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'campaign.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Campaign {

 String get id; String get businessId; String? get businessName; String get title; String get description; double? get discountPercentage; String? get discountCode; String? get terms; DateTime get startDate; DateTime get endDate; int get codeViewCount; String? get coverImageId; String? get coverImageUrl; String get status; DateTime? get createdAt;
/// Create a copy of Campaign
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$CampaignCopyWith<Campaign> get copyWith => _$CampaignCopyWithImpl<Campaign>(this as Campaign, _$identity);

  /// Serializes this Campaign to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Campaign&&(identical(other.id, id) || other.id == id)&&(identical(other.businessId, businessId) || other.businessId == businessId)&&(identical(other.businessName, businessName) || other.businessName == businessName)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.discountPercentage, discountPercentage) || other.discountPercentage == discountPercentage)&&(identical(other.discountCode, discountCode) || other.discountCode == discountCode)&&(identical(other.terms, terms) || other.terms == terms)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.codeViewCount, codeViewCount) || other.codeViewCount == codeViewCount)&&(identical(other.coverImageId, coverImageId) || other.coverImageId == coverImageId)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.status, status) || other.status == status)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,businessId,businessName,title,description,discountPercentage,discountCode,terms,startDate,endDate,codeViewCount,coverImageId,coverImageUrl,status,createdAt);

@override
String toString() {
  return 'Campaign(id: $id, businessId: $businessId, businessName: $businessName, title: $title, description: $description, discountPercentage: $discountPercentage, discountCode: $discountCode, terms: $terms, startDate: $startDate, endDate: $endDate, codeViewCount: $codeViewCount, coverImageId: $coverImageId, coverImageUrl: $coverImageUrl, status: $status, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class $CampaignCopyWith<$Res>  {
  factory $CampaignCopyWith(Campaign value, $Res Function(Campaign) _then) = _$CampaignCopyWithImpl;
@useResult
$Res call({
 String id, String businessId, String? businessName, String title, String description, double? discountPercentage, String? discountCode, String? terms, DateTime startDate, DateTime endDate, int codeViewCount, String? coverImageId, String? coverImageUrl, String status, DateTime? createdAt
});




}
/// @nodoc
class _$CampaignCopyWithImpl<$Res>
    implements $CampaignCopyWith<$Res> {
  _$CampaignCopyWithImpl(this._self, this._then);

  final Campaign _self;
  final $Res Function(Campaign) _then;

/// Create a copy of Campaign
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? businessId = null,Object? businessName = freezed,Object? title = null,Object? description = null,Object? discountPercentage = freezed,Object? discountCode = freezed,Object? terms = freezed,Object? startDate = null,Object? endDate = null,Object? codeViewCount = null,Object? coverImageId = freezed,Object? coverImageUrl = freezed,Object? status = null,Object? createdAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,businessId: null == businessId ? _self.businessId : businessId // ignore: cast_nullable_to_non_nullable
as String,businessName: freezed == businessName ? _self.businessName : businessName // ignore: cast_nullable_to_non_nullable
as String?,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: null == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String,discountPercentage: freezed == discountPercentage ? _self.discountPercentage : discountPercentage // ignore: cast_nullable_to_non_nullable
as double?,discountCode: freezed == discountCode ? _self.discountCode : discountCode // ignore: cast_nullable_to_non_nullable
as String?,terms: freezed == terms ? _self.terms : terms // ignore: cast_nullable_to_non_nullable
as String?,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime,endDate: null == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime,codeViewCount: null == codeViewCount ? _self.codeViewCount : codeViewCount // ignore: cast_nullable_to_non_nullable
as int,coverImageId: freezed == coverImageId ? _self.coverImageId : coverImageId // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [Campaign].
extension CampaignPatterns on Campaign {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Campaign value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Campaign() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Campaign value)  $default,){
final _that = this;
switch (_that) {
case _Campaign():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Campaign value)?  $default,){
final _that = this;
switch (_that) {
case _Campaign() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String businessId,  String? businessName,  String title,  String description,  double? discountPercentage,  String? discountCode,  String? terms,  DateTime startDate,  DateTime endDate,  int codeViewCount,  String? coverImageId,  String? coverImageUrl,  String status,  DateTime? createdAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Campaign() when $default != null:
return $default(_that.id,_that.businessId,_that.businessName,_that.title,_that.description,_that.discountPercentage,_that.discountCode,_that.terms,_that.startDate,_that.endDate,_that.codeViewCount,_that.coverImageId,_that.coverImageUrl,_that.status,_that.createdAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String businessId,  String? businessName,  String title,  String description,  double? discountPercentage,  String? discountCode,  String? terms,  DateTime startDate,  DateTime endDate,  int codeViewCount,  String? coverImageId,  String? coverImageUrl,  String status,  DateTime? createdAt)  $default,) {final _that = this;
switch (_that) {
case _Campaign():
return $default(_that.id,_that.businessId,_that.businessName,_that.title,_that.description,_that.discountPercentage,_that.discountCode,_that.terms,_that.startDate,_that.endDate,_that.codeViewCount,_that.coverImageId,_that.coverImageUrl,_that.status,_that.createdAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String businessId,  String? businessName,  String title,  String description,  double? discountPercentage,  String? discountCode,  String? terms,  DateTime startDate,  DateTime endDate,  int codeViewCount,  String? coverImageId,  String? coverImageUrl,  String status,  DateTime? createdAt)?  $default,) {final _that = this;
switch (_that) {
case _Campaign() when $default != null:
return $default(_that.id,_that.businessId,_that.businessName,_that.title,_that.description,_that.discountPercentage,_that.discountCode,_that.terms,_that.startDate,_that.endDate,_that.codeViewCount,_that.coverImageId,_that.coverImageUrl,_that.status,_that.createdAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Campaign extends Campaign {
  const _Campaign({required this.id, required this.businessId, this.businessName, required this.title, this.description = '', this.discountPercentage, this.discountCode, this.terms, required this.startDate, required this.endDate, this.codeViewCount = 0, this.coverImageId, this.coverImageUrl, this.status = 'approved', this.createdAt}): super._();
  factory _Campaign.fromJson(Map<String, dynamic> json) => _$CampaignFromJson(json);

@override final  String id;
@override final  String businessId;
@override final  String? businessName;
@override final  String title;
@override@JsonKey() final  String description;
@override final  double? discountPercentage;
@override final  String? discountCode;
@override final  String? terms;
@override final  DateTime startDate;
@override final  DateTime endDate;
@override@JsonKey() final  int codeViewCount;
@override final  String? coverImageId;
@override final  String? coverImageUrl;
@override@JsonKey() final  String status;
@override final  DateTime? createdAt;

/// Create a copy of Campaign
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$CampaignCopyWith<_Campaign> get copyWith => __$CampaignCopyWithImpl<_Campaign>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$CampaignToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Campaign&&(identical(other.id, id) || other.id == id)&&(identical(other.businessId, businessId) || other.businessId == businessId)&&(identical(other.businessName, businessName) || other.businessName == businessName)&&(identical(other.title, title) || other.title == title)&&(identical(other.description, description) || other.description == description)&&(identical(other.discountPercentage, discountPercentage) || other.discountPercentage == discountPercentage)&&(identical(other.discountCode, discountCode) || other.discountCode == discountCode)&&(identical(other.terms, terms) || other.terms == terms)&&(identical(other.startDate, startDate) || other.startDate == startDate)&&(identical(other.endDate, endDate) || other.endDate == endDate)&&(identical(other.codeViewCount, codeViewCount) || other.codeViewCount == codeViewCount)&&(identical(other.coverImageId, coverImageId) || other.coverImageId == coverImageId)&&(identical(other.coverImageUrl, coverImageUrl) || other.coverImageUrl == coverImageUrl)&&(identical(other.status, status) || other.status == status)&&(identical(other.createdAt, createdAt) || other.createdAt == createdAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,businessId,businessName,title,description,discountPercentage,discountCode,terms,startDate,endDate,codeViewCount,coverImageId,coverImageUrl,status,createdAt);

@override
String toString() {
  return 'Campaign(id: $id, businessId: $businessId, businessName: $businessName, title: $title, description: $description, discountPercentage: $discountPercentage, discountCode: $discountCode, terms: $terms, startDate: $startDate, endDate: $endDate, codeViewCount: $codeViewCount, coverImageId: $coverImageId, coverImageUrl: $coverImageUrl, status: $status, createdAt: $createdAt)';
}


}

/// @nodoc
abstract mixin class _$CampaignCopyWith<$Res> implements $CampaignCopyWith<$Res> {
  factory _$CampaignCopyWith(_Campaign value, $Res Function(_Campaign) _then) = __$CampaignCopyWithImpl;
@override @useResult
$Res call({
 String id, String businessId, String? businessName, String title, String description, double? discountPercentage, String? discountCode, String? terms, DateTime startDate, DateTime endDate, int codeViewCount, String? coverImageId, String? coverImageUrl, String status, DateTime? createdAt
});




}
/// @nodoc
class __$CampaignCopyWithImpl<$Res>
    implements _$CampaignCopyWith<$Res> {
  __$CampaignCopyWithImpl(this._self, this._then);

  final _Campaign _self;
  final $Res Function(_Campaign) _then;

/// Create a copy of Campaign
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? businessId = null,Object? businessName = freezed,Object? title = null,Object? description = null,Object? discountPercentage = freezed,Object? discountCode = freezed,Object? terms = freezed,Object? startDate = null,Object? endDate = null,Object? codeViewCount = null,Object? coverImageId = freezed,Object? coverImageUrl = freezed,Object? status = null,Object? createdAt = freezed,}) {
  return _then(_Campaign(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,businessId: null == businessId ? _self.businessId : businessId // ignore: cast_nullable_to_non_nullable
as String,businessName: freezed == businessName ? _self.businessName : businessName // ignore: cast_nullable_to_non_nullable
as String?,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,description: null == description ? _self.description : description // ignore: cast_nullable_to_non_nullable
as String,discountPercentage: freezed == discountPercentage ? _self.discountPercentage : discountPercentage // ignore: cast_nullable_to_non_nullable
as double?,discountCode: freezed == discountCode ? _self.discountCode : discountCode // ignore: cast_nullable_to_non_nullable
as String?,terms: freezed == terms ? _self.terms : terms // ignore: cast_nullable_to_non_nullable
as String?,startDate: null == startDate ? _self.startDate : startDate // ignore: cast_nullable_to_non_nullable
as DateTime,endDate: null == endDate ? _self.endDate : endDate // ignore: cast_nullable_to_non_nullable
as DateTime,codeViewCount: null == codeViewCount ? _self.codeViewCount : codeViewCount // ignore: cast_nullable_to_non_nullable
as int,coverImageId: freezed == coverImageId ? _self.coverImageId : coverImageId // ignore: cast_nullable_to_non_nullable
as String?,coverImageUrl: freezed == coverImageUrl ? _self.coverImageUrl : coverImageUrl // ignore: cast_nullable_to_non_nullable
as String?,status: null == status ? _self.status : status // ignore: cast_nullable_to_non_nullable
as String,createdAt: freezed == createdAt ? _self.createdAt : createdAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
