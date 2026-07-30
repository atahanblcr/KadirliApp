// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'on_duty_pharmacy.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$OnDutyPharmacy {

 String get scheduleId; DateTime get dutyDate; String get startTime; String get endTime; String get pharmacyId; String get name; String? get address; String? get phone; double? get latitude; double? get longitude; String? get pharmacistName; String? get workingHours;
/// Create a copy of OnDutyPharmacy
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$OnDutyPharmacyCopyWith<OnDutyPharmacy> get copyWith => _$OnDutyPharmacyCopyWithImpl<OnDutyPharmacy>(this as OnDutyPharmacy, _$identity);

  /// Serializes this OnDutyPharmacy to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is OnDutyPharmacy&&(identical(other.scheduleId, scheduleId) || other.scheduleId == scheduleId)&&(identical(other.dutyDate, dutyDate) || other.dutyDate == dutyDate)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.pharmacyId, pharmacyId) || other.pharmacyId == pharmacyId)&&(identical(other.name, name) || other.name == name)&&(identical(other.address, address) || other.address == address)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.pharmacistName, pharmacistName) || other.pharmacistName == pharmacistName)&&(identical(other.workingHours, workingHours) || other.workingHours == workingHours));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,scheduleId,dutyDate,startTime,endTime,pharmacyId,name,address,phone,latitude,longitude,pharmacistName,workingHours);

@override
String toString() {
  return 'OnDutyPharmacy(scheduleId: $scheduleId, dutyDate: $dutyDate, startTime: $startTime, endTime: $endTime, pharmacyId: $pharmacyId, name: $name, address: $address, phone: $phone, latitude: $latitude, longitude: $longitude, pharmacistName: $pharmacistName, workingHours: $workingHours)';
}


}

/// @nodoc
abstract mixin class $OnDutyPharmacyCopyWith<$Res>  {
  factory $OnDutyPharmacyCopyWith(OnDutyPharmacy value, $Res Function(OnDutyPharmacy) _then) = _$OnDutyPharmacyCopyWithImpl;
@useResult
$Res call({
 String scheduleId, DateTime dutyDate, String startTime, String endTime, String pharmacyId, String name, String? address, String? phone, double? latitude, double? longitude, String? pharmacistName, String? workingHours
});




}
/// @nodoc
class _$OnDutyPharmacyCopyWithImpl<$Res>
    implements $OnDutyPharmacyCopyWith<$Res> {
  _$OnDutyPharmacyCopyWithImpl(this._self, this._then);

  final OnDutyPharmacy _self;
  final $Res Function(OnDutyPharmacy) _then;

/// Create a copy of OnDutyPharmacy
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? scheduleId = null,Object? dutyDate = null,Object? startTime = null,Object? endTime = null,Object? pharmacyId = null,Object? name = null,Object? address = freezed,Object? phone = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? pharmacistName = freezed,Object? workingHours = freezed,}) {
  return _then(_self.copyWith(
scheduleId: null == scheduleId ? _self.scheduleId : scheduleId // ignore: cast_nullable_to_non_nullable
as String,dutyDate: null == dutyDate ? _self.dutyDate : dutyDate // ignore: cast_nullable_to_non_nullable
as DateTime,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as String,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as String,pharmacyId: null == pharmacyId ? _self.pharmacyId : pharmacyId // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,pharmacistName: freezed == pharmacistName ? _self.pharmacistName : pharmacistName // ignore: cast_nullable_to_non_nullable
as String?,workingHours: freezed == workingHours ? _self.workingHours : workingHours // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}

}


/// Adds pattern-matching-related methods to [OnDutyPharmacy].
extension OnDutyPharmacyPatterns on OnDutyPharmacy {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _OnDutyPharmacy value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _OnDutyPharmacy() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _OnDutyPharmacy value)  $default,){
final _that = this;
switch (_that) {
case _OnDutyPharmacy():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _OnDutyPharmacy value)?  $default,){
final _that = this;
switch (_that) {
case _OnDutyPharmacy() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String scheduleId,  DateTime dutyDate,  String startTime,  String endTime,  String pharmacyId,  String name,  String? address,  String? phone,  double? latitude,  double? longitude,  String? pharmacistName,  String? workingHours)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _OnDutyPharmacy() when $default != null:
return $default(_that.scheduleId,_that.dutyDate,_that.startTime,_that.endTime,_that.pharmacyId,_that.name,_that.address,_that.phone,_that.latitude,_that.longitude,_that.pharmacistName,_that.workingHours);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String scheduleId,  DateTime dutyDate,  String startTime,  String endTime,  String pharmacyId,  String name,  String? address,  String? phone,  double? latitude,  double? longitude,  String? pharmacistName,  String? workingHours)  $default,) {final _that = this;
switch (_that) {
case _OnDutyPharmacy():
return $default(_that.scheduleId,_that.dutyDate,_that.startTime,_that.endTime,_that.pharmacyId,_that.name,_that.address,_that.phone,_that.latitude,_that.longitude,_that.pharmacistName,_that.workingHours);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String scheduleId,  DateTime dutyDate,  String startTime,  String endTime,  String pharmacyId,  String name,  String? address,  String? phone,  double? latitude,  double? longitude,  String? pharmacistName,  String? workingHours)?  $default,) {final _that = this;
switch (_that) {
case _OnDutyPharmacy() when $default != null:
return $default(_that.scheduleId,_that.dutyDate,_that.startTime,_that.endTime,_that.pharmacyId,_that.name,_that.address,_that.phone,_that.latitude,_that.longitude,_that.pharmacistName,_that.workingHours);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _OnDutyPharmacy extends OnDutyPharmacy {
  const _OnDutyPharmacy({required this.scheduleId, required this.dutyDate, this.startTime = '', this.endTime = '', required this.pharmacyId, required this.name, this.address, this.phone, this.latitude, this.longitude, this.pharmacistName, this.workingHours}): super._();
  factory _OnDutyPharmacy.fromJson(Map<String, dynamic> json) => _$OnDutyPharmacyFromJson(json);

@override final  String scheduleId;
@override final  DateTime dutyDate;
@override@JsonKey() final  String startTime;
@override@JsonKey() final  String endTime;
@override final  String pharmacyId;
@override final  String name;
@override final  String? address;
@override final  String? phone;
@override final  double? latitude;
@override final  double? longitude;
@override final  String? pharmacistName;
@override final  String? workingHours;

/// Create a copy of OnDutyPharmacy
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$OnDutyPharmacyCopyWith<_OnDutyPharmacy> get copyWith => __$OnDutyPharmacyCopyWithImpl<_OnDutyPharmacy>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$OnDutyPharmacyToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _OnDutyPharmacy&&(identical(other.scheduleId, scheduleId) || other.scheduleId == scheduleId)&&(identical(other.dutyDate, dutyDate) || other.dutyDate == dutyDate)&&(identical(other.startTime, startTime) || other.startTime == startTime)&&(identical(other.endTime, endTime) || other.endTime == endTime)&&(identical(other.pharmacyId, pharmacyId) || other.pharmacyId == pharmacyId)&&(identical(other.name, name) || other.name == name)&&(identical(other.address, address) || other.address == address)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.pharmacistName, pharmacistName) || other.pharmacistName == pharmacistName)&&(identical(other.workingHours, workingHours) || other.workingHours == workingHours));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,scheduleId,dutyDate,startTime,endTime,pharmacyId,name,address,phone,latitude,longitude,pharmacistName,workingHours);

@override
String toString() {
  return 'OnDutyPharmacy(scheduleId: $scheduleId, dutyDate: $dutyDate, startTime: $startTime, endTime: $endTime, pharmacyId: $pharmacyId, name: $name, address: $address, phone: $phone, latitude: $latitude, longitude: $longitude, pharmacistName: $pharmacistName, workingHours: $workingHours)';
}


}

/// @nodoc
abstract mixin class _$OnDutyPharmacyCopyWith<$Res> implements $OnDutyPharmacyCopyWith<$Res> {
  factory _$OnDutyPharmacyCopyWith(_OnDutyPharmacy value, $Res Function(_OnDutyPharmacy) _then) = __$OnDutyPharmacyCopyWithImpl;
@override @useResult
$Res call({
 String scheduleId, DateTime dutyDate, String startTime, String endTime, String pharmacyId, String name, String? address, String? phone, double? latitude, double? longitude, String? pharmacistName, String? workingHours
});




}
/// @nodoc
class __$OnDutyPharmacyCopyWithImpl<$Res>
    implements _$OnDutyPharmacyCopyWith<$Res> {
  __$OnDutyPharmacyCopyWithImpl(this._self, this._then);

  final _OnDutyPharmacy _self;
  final $Res Function(_OnDutyPharmacy) _then;

/// Create a copy of OnDutyPharmacy
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? scheduleId = null,Object? dutyDate = null,Object? startTime = null,Object? endTime = null,Object? pharmacyId = null,Object? name = null,Object? address = freezed,Object? phone = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? pharmacistName = freezed,Object? workingHours = freezed,}) {
  return _then(_OnDutyPharmacy(
scheduleId: null == scheduleId ? _self.scheduleId : scheduleId // ignore: cast_nullable_to_non_nullable
as String,dutyDate: null == dutyDate ? _self.dutyDate : dutyDate // ignore: cast_nullable_to_non_nullable
as DateTime,startTime: null == startTime ? _self.startTime : startTime // ignore: cast_nullable_to_non_nullable
as String,endTime: null == endTime ? _self.endTime : endTime // ignore: cast_nullable_to_non_nullable
as String,pharmacyId: null == pharmacyId ? _self.pharmacyId : pharmacyId // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,pharmacistName: freezed == pharmacistName ? _self.pharmacistName : pharmacistName // ignore: cast_nullable_to_non_nullable
as String?,workingHours: freezed == workingHours ? _self.workingHours : workingHours // ignore: cast_nullable_to_non_nullable
as String?,
  ));
}


}

// dart format on
