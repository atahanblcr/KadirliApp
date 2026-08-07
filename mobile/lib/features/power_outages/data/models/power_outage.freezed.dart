// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'power_outage.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$PowerOutage {

 String get id;/// Mahalle **adı**. Faz 12.3'ten beri sunucuda sözlükten türetiliyor (yazım
/// farkı yok); eski kayıtlarda hâlâ serbest metin olabilir.
 String? get neighborhood;/// Faz 12.3 (yeni): sözlükteki mahalle kimliği. Eski sürümlerde ve şehir
/// geneli kesintilerde `null`.
 String? get neighborhoodId;/// Faz 12.3 (yeni): mahallenin hangi kısmı ("Atatürk Caddesi ve çevresi").
 String? get areaDetail; DateTime get startTime; DateTime get endTime; String? get reason;/// Faz 12.3 (yeni): bu kesinti için üretilmiş duyuru. Dolu olması
/// "bildirim gönderildi" demektir.
 String? get announcementId;
/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$PowerOutageCopyWith<PowerOutage> get copyWith => _$PowerOutageCopyWithImpl<PowerOutage>(this as PowerOutage, _$identity);

  /// Serializes this PowerOutage to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is PowerOutage&&(identical(other.id, id) || other.id == id)&&(identical(other.neighborhood, neighborhood) || other.neighborhood == neighborhood)&&(identical(other.neighborhoodId, neighborhoodId) || other.neighborhoodId == neighborhoodId)&&(identical(other.areaDetail, areaDetail) || other.areaDetail == areaDetail)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.reason, reason) || other.reason == reason)&&(identical(other.announcementId, announcementId) || other.announcementId == announcementId));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,neighborhood,neighborhoodId,areaDetail,startTime,endTime,reason,announcementId);

@override
String toString() {
  return 'PowerOutage(id: $id, neighborhood: $neighborhood, neighborhoodId: $neighborhoodId, areaDetail: $areaDetail, startTime: $startTime, endTime: $endTime, reason: $reason, announcementId: $announcementId)';
}


}

/// @nodoc
abstract mixin class $PowerOutageCopyWith<$Res>  {
  factory $PowerOutageCopyWith(PowerOutage value, $Res Function(PowerOutage) _then) = _$PowerOutageCopyWithImpl;
@useResult
$Res call({
 String id, String? neighborhood, String? neighborhoodId, String? areaDetail, DateTime startTime, DateTime endTime, String? reason, String? announcementId
});




}
/// @nodoc
class _$PowerOutageCopyWithImpl<$Res>
    implements $PowerOutageCopyWith<$Res> {
  _$PowerOutageCopyWithImpl(this._self, this._then);

  final PowerOutage _self;
  final $Res Function(PowerOutage) _then;

/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? neighborhood = freezed,Object? neighborhoodId = freezed,Object? areaDetail = freezed,Object? startTime = null,Object? endTime = null,Object? reason = freezed,Object? announcementId = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,neighborhood: freezed == neighborhood ? _self.neighborhood : neighborhood // ignore: cast_nullable_to_non_nullable
as String?,neighborhoodId: freezed == neighborhoodId ? _self.neighborhoodId : neighborhoodId // ignore: cast_nullable_to_non_nullable
as String?,areaDetail: freezed == areaDetail ? _self.areaDetail : areaDetail // ignore: cast_nullable_to_non_nullable
as String?,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as DateTime,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as DateTime,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,announcementId: freezed == announcementId ? _self.announcementId : announcementId // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [PowerOutage].
extension PowerOutagePatterns on PowerOutage {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _PowerOutage value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _PowerOutage value)  $default,){
final _that = this;
switch (_that) {
case _PowerOutage():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _PowerOutage value)?  $default,){
final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String? neighborhood,  String? neighborhoodId,  String? areaDetail,  DateTime startTime,  DateTime endTime,  String? reason,  String? announcementId)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
return $default(_that.id,_that.neighborhood,_that.neighborhoodId,_that.areaDetail,_that.startTime,_that.endTime,_that.reason,_that.announcementId);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String? neighborhood,  String? neighborhoodId,  String? areaDetail,  DateTime startTime,  DateTime endTime,  String? reason,  String? announcementId)  $default,) {final _that = this;
switch (_that) {
case _PowerOutage():
return $default(_that.id,_that.neighborhood,_that.neighborhoodId,_that.areaDetail,_that.startTime,_that.endTime,_that.reason,_that.announcementId);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String? neighborhood,  String? neighborhoodId,  String? areaDetail,  DateTime startTime,  DateTime endTime,  String? reason,  String? announcementId)?  $default,) {final _that = this;
switch (_that) {
case _PowerOutage() when $default != null:
return $default(_that.id,_that.neighborhood,_that.neighborhoodId,_that.areaDetail,_that.startTime,_that.endTime,_that.reason,_that.announcementId);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _PowerOutage extends PowerOutage {
  const _PowerOutage({required this.id, this.neighborhood, this.neighborhoodId, this.areaDetail, required this.startTime, required this.endTime, this.reason, this.announcementId}): super._();
  factory _PowerOutage.fromJson(Map<String, dynamic> json) => _$PowerOutageFromJson(json);

@override final  String id;
/// Mahalle **adı**. Faz 12.3'ten beri sunucuda sözlükten türetiliyor (yazım
/// farkı yok); eski kayıtlarda hâlâ serbest metin olabilir.
@override final  String? neighborhood;
/// Faz 12.3 (yeni): sözlükteki mahalle kimliği. Eski sürümlerde ve şehir
/// geneli kesintilerde `null`.
@override final  String? neighborhoodId;
/// Faz 12.3 (yeni): mahallenin hangi kısmı ("Atatürk Caddesi ve çevresi").
@override final  String? areaDetail;
@override final  DateTime startTime;
@override final  DateTime endTime;
@override final  String? reason;
/// Faz 12.3 (yeni): bu kesinti için üretilmiş duyuru. Dolu olması
/// "bildirim gönderildi" demektir.
@override final  String? announcementId;

/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$PowerOutageCopyWith<_PowerOutage> get copyWith => __$PowerOutageCopyWithImpl<_PowerOutage>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$PowerOutageToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _PowerOutage&&(identical(other.id, id) || other.id == id)&&(identical(other.neighborhood, neighborhood) || other.neighborhood == neighborhood)&&(identical(other.neighborhoodId, neighborhoodId) || other.neighborhoodId == neighborhoodId)&&(identical(other.areaDetail, areaDetail) || other.areaDetail == areaDetail)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.reason, reason) || other.reason == reason)&&(identical(other.announcementId, announcementId) || other.announcementId == announcementId));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,neighborhood,neighborhoodId,areaDetail,startTime,endTime,reason,announcementId);

@override
String toString() {
  return 'PowerOutage(id: $id, neighborhood: $neighborhood, neighborhoodId: $neighborhoodId, areaDetail: $areaDetail, startTime: $startTime, endTime: $endTime, reason: $reason, announcementId: $announcementId)';
}


}

/// @nodoc
abstract mixin class _$PowerOutageCopyWith<$Res> implements $PowerOutageCopyWith<$Res> {
  factory _$PowerOutageCopyWith(_PowerOutage value, $Res Function(_PowerOutage) _then) = __$PowerOutageCopyWithImpl;
@override @useResult
$Res call({
 String id, String? neighborhood, String? neighborhoodId, String? areaDetail, DateTime startTime, DateTime endTime, String? reason, String? announcementId
});




}
/// @nodoc
class __$PowerOutageCopyWithImpl<$Res>
    implements _$PowerOutageCopyWith<$Res> {
  __$PowerOutageCopyWithImpl(this._self, this._then);

  final _PowerOutage _self;
  final $Res Function(_PowerOutage) _then;

/// Create a copy of PowerOutage
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? neighborhood = freezed,Object? neighborhoodId = freezed,Object? areaDetail = freezed,Object? startTime = null,Object? endTime = null,Object? reason = freezed,Object? announcementId = freezed,}) {
  return _then(_PowerOutage(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,neighborhood: freezed == neighborhood ? _self.neighborhood : neighborhood // ignore: cast_nullable_to_non_nullable
as String?,neighborhoodId: freezed == neighborhoodId ? _self.neighborhoodId : neighborhoodId // ignore: cast_nullable_to_non_nullable
as String?,areaDetail: freezed == areaDetail ? _self.areaDetail : areaDetail // ignore: cast_nullable_to_non_nullable
as String?,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as DateTime,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as DateTime,reason: freezed == reason ? _self.reason : reason // ignore: cast_nullable_to_non_nullable
as String?,announcementId: freezed == announcementId ? _self.announcementId : announcementId // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on
