// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'pharmacy.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$Pharmacy {

 String get id; String get name; String? get address; String? get phone; double? get latitude; double? get longitude;/// "08:30 - 19:00" gibi serbest metin (saat aritmetiği yapma).
 String? get workingHours; String? get pharmacistName; bool get isActive;
/// Create a copy of Pharmacy
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$PharmacyCopyWith<Pharmacy> get copyWith => _$PharmacyCopyWithImpl<Pharmacy>(this as Pharmacy, _$identity);

  /// Serializes this Pharmacy to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is Pharmacy&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.address, address) || other.address == address)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.workingHours, workingHours) || other.workingHours == workingHours)&&(identical(other.pharmacistName, pharmacistName) || other.pharmacistName == pharmacistName)&&(identical(other.isActive, isActive) || other.isActive == isActive));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,address,phone,latitude,longitude,workingHours,pharmacistName,isActive);

@override
String toString() {
  return 'Pharmacy(id: $id, name: $name, address: $address, phone: $phone, latitude: $latitude, longitude: $longitude, workingHours: $workingHours, pharmacistName: $pharmacistName, isActive: $isActive)';
}


}

/// @nodoc
abstract mixin class $PharmacyCopyWith<$Res>  {
  factory $PharmacyCopyWith(Pharmacy value, $Res Function(Pharmacy) _then) = _$PharmacyCopyWithImpl;
@useResult
$Res call({
 String id, String name, String? address, String? phone, double? latitude, double? longitude, String? workingHours, String? pharmacistName, bool isActive
});




}
/// @nodoc
class _$PharmacyCopyWithImpl<$Res>
    implements $PharmacyCopyWith<$Res> {
  _$PharmacyCopyWithImpl(this._self, this._then);

  final Pharmacy _self;
  final $Res Function(Pharmacy) _then;

/// Create a copy of Pharmacy
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? name = null,Object? address = freezed,Object? phone = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? workingHours = freezed,Object? pharmacistName = freezed,Object? isActive = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,workingHours: freezed == workingHours ? _self.workingHours : workingHours // ignore: cast_nullable_to_non_nullable
as String?,pharmacistName: freezed == pharmacistName ? _self.pharmacistName : pharmacistName // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [Pharmacy].
extension PharmacyPatterns on Pharmacy {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _Pharmacy value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _Pharmacy() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _Pharmacy value)  $default,){
final _that = this;
switch (_that) {
case _Pharmacy():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _Pharmacy value)?  $default,){
final _that = this;
switch (_that) {
case _Pharmacy() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String name,  String? address,  String? phone,  double? latitude,  double? longitude,  String? workingHours,  String? pharmacistName,  bool isActive)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _Pharmacy() when $default != null:
return $default(_that.id,_that.name,_that.address,_that.phone,_that.latitude,_that.longitude,_that.workingHours,_that.pharmacistName,_that.isActive);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String name,  String? address,  String? phone,  double? latitude,  double? longitude,  String? workingHours,  String? pharmacistName,  bool isActive)  $default,) {final _that = this;
switch (_that) {
case _Pharmacy():
return $default(_that.id,_that.name,_that.address,_that.phone,_that.latitude,_that.longitude,_that.workingHours,_that.pharmacistName,_that.isActive);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String name,  String? address,  String? phone,  double? latitude,  double? longitude,  String? workingHours,  String? pharmacistName,  bool isActive)?  $default,) {final _that = this;
switch (_that) {
case _Pharmacy() when $default != null:
return $default(_that.id,_that.name,_that.address,_that.phone,_that.latitude,_that.longitude,_that.workingHours,_that.pharmacistName,_that.isActive);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _Pharmacy extends Pharmacy {
  const _Pharmacy({required this.id, required this.name, this.address, this.phone, this.latitude, this.longitude, this.workingHours, this.pharmacistName, this.isActive = true}): super._();
  factory _Pharmacy.fromJson(Map<String, dynamic> json) => _$PharmacyFromJson(json);

@override final  String id;
@override final  String name;
@override final  String? address;
@override final  String? phone;
@override final  double? latitude;
@override final  double? longitude;
/// "08:30 - 19:00" gibi serbest metin (saat aritmetiği yapma).
@override final  String? workingHours;
@override final  String? pharmacistName;
@override@JsonKey() final  bool isActive;

/// Create a copy of Pharmacy
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$PharmacyCopyWith<_Pharmacy> get copyWith => __$PharmacyCopyWithImpl<_Pharmacy>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$PharmacyToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _Pharmacy&&(identical(other.id, id) || other.id == id)&&(identical(other.name, name) || other.name == name)&&(identical(other.address, address) || other.address == address)&&(identical(other.phone, phone) || other.phone == phone)&&(identical(other.latitude, latitude) || other.latitude == latitude)&&(identical(other.longitude, longitude) || other.longitude == longitude)&&(identical(other.workingHours, workingHours) || other.workingHours == workingHours)&&(identical(other.pharmacistName, pharmacistName) || other.pharmacistName == pharmacistName)&&(identical(other.isActive, isActive) || other.isActive == isActive));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,name,address,phone,latitude,longitude,workingHours,pharmacistName,isActive);

@override
String toString() {
  return 'Pharmacy(id: $id, name: $name, address: $address, phone: $phone, latitude: $latitude, longitude: $longitude, workingHours: $workingHours, pharmacistName: $pharmacistName, isActive: $isActive)';
}


}

/// @nodoc
abstract mixin class _$PharmacyCopyWith<$Res> implements $PharmacyCopyWith<$Res> {
  factory _$PharmacyCopyWith(_Pharmacy value, $Res Function(_Pharmacy) _then) = __$PharmacyCopyWithImpl;
@override @useResult
$Res call({
 String id, String name, String? address, String? phone, double? latitude, double? longitude, String? workingHours, String? pharmacistName, bool isActive
});




}
/// @nodoc
class __$PharmacyCopyWithImpl<$Res>
    implements _$PharmacyCopyWith<$Res> {
  __$PharmacyCopyWithImpl(this._self, this._then);

  final _Pharmacy _self;
  final $Res Function(_Pharmacy) _then;

/// Create a copy of Pharmacy
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? name = null,Object? address = freezed,Object? phone = freezed,Object? latitude = freezed,Object? longitude = freezed,Object? workingHours = freezed,Object? pharmacistName = freezed,Object? isActive = null,}) {
  return _then(_Pharmacy(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,name: null == name ? _self.name : name // ignore: cast_nullable_to_non_nullable
as String,address: freezed == address ? _self.address : address // ignore: cast_nullable_to_non_nullable
as String?,phone: freezed == phone ? _self.phone : phone // ignore: cast_nullable_to_non_nullable
as String?,latitude: freezed == latitude ? _self.latitude : latitude // ignore: cast_nullable_to_non_nullable
as double?,longitude: freezed == longitude ? _self.longitude : longitude // ignore: cast_nullable_to_non_nullable
as double?,workingHours: freezed == workingHours ? _self.workingHours : workingHours // ignore: cast_nullable_to_non_nullable
as String?,pharmacistName: freezed == pharmacistName ? _self.pharmacistName : pharmacistName // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
