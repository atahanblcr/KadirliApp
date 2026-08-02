// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'intercity_route.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$IntercityRoute {

 String get id; String get destination; double? get price; int? get durationMinutes; String? get company; bool get isActive; List<IntercityDeparture> get schedules;
/// Create a copy of IntercityRoute
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$IntercityRouteCopyWith<IntercityRoute> get copyWith => _$IntercityRouteCopyWithImpl<IntercityRoute>(this as IntercityRoute, _$identity);

  /// Serializes this IntercityRoute to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is IntercityRoute&&(identical(other.id, id) || other.id == id)&&(identical(other.destination, destination) || other.destination == destination)&&(identical(other.price, price) || other.price == price)&&(identical(other.durationMinutes, durationMinutes) || other.durationMinutes == durationMinutes)&&(identical(other.company, company) || other.company == company)&&(identical(other.isActive, isActive) || other.isActive == isActive)&&const DeepCollectionEquality().equals(other.schedules, schedules));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,destination,price,durationMinutes,company,isActive,const DeepCollectionEquality().hash(schedules));

@override
String toString() {
  return 'IntercityRoute(id: $id, destination: $destination, price: $price, durationMinutes: $durationMinutes, company: $company, isActive: $isActive, schedules: $schedules)';
}


}

/// @nodoc
abstract mixin class $IntercityRouteCopyWith<$Res>  {
  factory $IntercityRouteCopyWith(IntercityRoute value, $Res Function(IntercityRoute) _then) = _$IntercityRouteCopyWithImpl;
@useResult
$Res call({
 String id, String destination, double? price, int? durationMinutes, String? company, bool isActive, List<IntercityDeparture> schedules
});




}
/// @nodoc
class _$IntercityRouteCopyWithImpl<$Res>
    implements $IntercityRouteCopyWith<$Res> {
  _$IntercityRouteCopyWithImpl(this._self, this._then);

  final IntercityRoute _self;
  final $Res Function(IntercityRoute) _then;

/// Create a copy of IntercityRoute
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? destination = null,Object? price = freezed,Object? durationMinutes = freezed,Object? company = freezed,Object? isActive = null,Object? schedules = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,destination: null == destination ? _self.destination : destination // ignore: cast_nullable_to_non_nullable
as String,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,durationMinutes: freezed == durationMinutes ? _self.durationMinutes : durationMinutes // ignore: cast_nullable_to_non_nullable
as int?,company: freezed == company ? _self.company : company // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,schedules: null == schedules ? _self.schedules : schedules // ignore: cast_nullable_to_non_nullable
as List<IntercityDeparture>,
  ));
}

}


/// Adds pattern-matching-related methods to [IntercityRoute].
extension IntercityRoutePatterns on IntercityRoute {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _IntercityRoute value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _IntercityRoute() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _IntercityRoute value)  $default,){
final _that = this;
switch (_that) {
case _IntercityRoute():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _IntercityRoute value)?  $default,){
final _that = this;
switch (_that) {
case _IntercityRoute() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String destination,  double? price,  int? durationMinutes,  String? company,  bool isActive,  List<IntercityDeparture> schedules)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _IntercityRoute() when $default != null:
return $default(_that.id,_that.destination,_that.price,_that.durationMinutes,_that.company,_that.isActive,_that.schedules);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String destination,  double? price,  int? durationMinutes,  String? company,  bool isActive,  List<IntercityDeparture> schedules)  $default,) {final _that = this;
switch (_that) {
case _IntercityRoute():
return $default(_that.id,_that.destination,_that.price,_that.durationMinutes,_that.company,_that.isActive,_that.schedules);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String destination,  double? price,  int? durationMinutes,  String? company,  bool isActive,  List<IntercityDeparture> schedules)?  $default,) {final _that = this;
switch (_that) {
case _IntercityRoute() when $default != null:
return $default(_that.id,_that.destination,_that.price,_that.durationMinutes,_that.company,_that.isActive,_that.schedules);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _IntercityRoute extends IntercityRoute {
  const _IntercityRoute({required this.id, required this.destination, this.price, this.durationMinutes, this.company, this.isActive = true, final  List<IntercityDeparture> schedules = const <IntercityDeparture>[]}): _schedules = schedules,super._();
  factory _IntercityRoute.fromJson(Map<String, dynamic> json) => _$IntercityRouteFromJson(json);

@override final  String id;
@override final  String destination;
@override final  double? price;
@override final  int? durationMinutes;
@override final  String? company;
@override@JsonKey() final  bool isActive;
 final  List<IntercityDeparture> _schedules;
@override@JsonKey() List<IntercityDeparture> get schedules {
  if (_schedules is EqualUnmodifiableListView) return _schedules;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_schedules);
}


/// Create a copy of IntercityRoute
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$IntercityRouteCopyWith<_IntercityRoute> get copyWith => __$IntercityRouteCopyWithImpl<_IntercityRoute>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$IntercityRouteToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _IntercityRoute&&(identical(other.id, id) || other.id == id)&&(identical(other.destination, destination) || other.destination == destination)&&(identical(other.price, price) || other.price == price)&&(identical(other.durationMinutes, durationMinutes) || other.durationMinutes == durationMinutes)&&(identical(other.company, company) || other.company == company)&&(identical(other.isActive, isActive) || other.isActive == isActive)&&const DeepCollectionEquality().equals(other._schedules, _schedules));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,destination,price,durationMinutes,company,isActive,const DeepCollectionEquality().hash(_schedules));

@override
String toString() {
  return 'IntercityRoute(id: $id, destination: $destination, price: $price, durationMinutes: $durationMinutes, company: $company, isActive: $isActive, schedules: $schedules)';
}


}

/// @nodoc
abstract mixin class _$IntercityRouteCopyWith<$Res> implements $IntercityRouteCopyWith<$Res> {
  factory _$IntercityRouteCopyWith(_IntercityRoute value, $Res Function(_IntercityRoute) _then) = __$IntercityRouteCopyWithImpl;
@override @useResult
$Res call({
 String id, String destination, double? price, int? durationMinutes, String? company, bool isActive, List<IntercityDeparture> schedules
});




}
/// @nodoc
class __$IntercityRouteCopyWithImpl<$Res>
    implements _$IntercityRouteCopyWith<$Res> {
  __$IntercityRouteCopyWithImpl(this._self, this._then);

  final _IntercityRoute _self;
  final $Res Function(_IntercityRoute) _then;

/// Create a copy of IntercityRoute
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? destination = null,Object? price = freezed,Object? durationMinutes = freezed,Object? company = freezed,Object? isActive = null,Object? schedules = null,}) {
  return _then(_IntercityRoute(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,destination: null == destination ? _self.destination : destination // ignore: cast_nullable_to_non_nullable
as String,price: freezed == price ? _self.price : price // ignore: cast_nullable_to_non_nullable
as double?,durationMinutes: freezed == durationMinutes ? _self.durationMinutes : durationMinutes // ignore: cast_nullable_to_non_nullable
as int?,company: freezed == company ? _self.company : company // ignore: cast_nullable_to_non_nullable
as String?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,schedules: null == schedules ? _self._schedules : schedules // ignore: cast_nullable_to_non_nullable
as List<IntercityDeparture>,
  ));
}


}


/// @nodoc
mixin _$IntercityDeparture {

 String get id; String get departureTime;
/// Create a copy of IntercityDeparture
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$IntercityDepartureCopyWith<IntercityDeparture> get copyWith => _$IntercityDepartureCopyWithImpl<IntercityDeparture>(this as IntercityDeparture, _$identity);

  /// Serializes this IntercityDeparture to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is IntercityDeparture&&(identical(other.id, id) || other.id == id)&&(identical(other.departureTime, departureTime) || other.departureTime == departureTime));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,departureTime);

@override
String toString() {
  return 'IntercityDeparture(id: $id, departureTime: $departureTime)';
}


}

/// @nodoc
abstract mixin class $IntercityDepartureCopyWith<$Res>  {
  factory $IntercityDepartureCopyWith(IntercityDeparture value, $Res Function(IntercityDeparture) _then) = _$IntercityDepartureCopyWithImpl;
@useResult
$Res call({
 String id, String departureTime
});




}
/// @nodoc
class _$IntercityDepartureCopyWithImpl<$Res>
    implements $IntercityDepartureCopyWith<$Res> {
  _$IntercityDepartureCopyWithImpl(this._self, this._then);

  final IntercityDeparture _self;
  final $Res Function(IntercityDeparture) _then;

/// Create a copy of IntercityDeparture
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? departureTime = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,departureTime: null == departureTime ? _self.departureTime : departureTime // ignore: cast_nullable_to_non_nullable
as String,
  ));
}

}


/// Adds pattern-matching-related methods to [IntercityDeparture].
extension IntercityDeparturePatterns on IntercityDeparture {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _IntercityDeparture value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _IntercityDeparture() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _IntercityDeparture value)  $default,){
final _that = this;
switch (_that) {
case _IntercityDeparture():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _IntercityDeparture value)?  $default,){
final _that = this;
switch (_that) {
case _IntercityDeparture() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String departureTime)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _IntercityDeparture() when $default != null:
return $default(_that.id,_that.departureTime);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String departureTime)  $default,) {final _that = this;
switch (_that) {
case _IntercityDeparture():
return $default(_that.id,_that.departureTime);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String departureTime)?  $default,) {final _that = this;
switch (_that) {
case _IntercityDeparture() when $default != null:
return $default(_that.id,_that.departureTime);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _IntercityDeparture implements IntercityDeparture {
  const _IntercityDeparture({required this.id, this.departureTime = ''});
  factory _IntercityDeparture.fromJson(Map<String, dynamic> json) => _$IntercityDepartureFromJson(json);

@override final  String id;
@override@JsonKey() final  String departureTime;

/// Create a copy of IntercityDeparture
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$IntercityDepartureCopyWith<_IntercityDeparture> get copyWith => __$IntercityDepartureCopyWithImpl<_IntercityDeparture>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$IntercityDepartureToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _IntercityDeparture&&(identical(other.id, id) || other.id == id)&&(identical(other.departureTime, departureTime) || other.departureTime == departureTime));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,departureTime);

@override
String toString() {
  return 'IntercityDeparture(id: $id, departureTime: $departureTime)';
}


}

/// @nodoc
abstract mixin class _$IntercityDepartureCopyWith<$Res> implements $IntercityDepartureCopyWith<$Res> {
  factory _$IntercityDepartureCopyWith(_IntercityDeparture value, $Res Function(_IntercityDeparture) _then) = __$IntercityDepartureCopyWithImpl;
@override @useResult
$Res call({
 String id, String departureTime
});




}
/// @nodoc
class __$IntercityDepartureCopyWithImpl<$Res>
    implements _$IntercityDepartureCopyWith<$Res> {
  __$IntercityDepartureCopyWithImpl(this._self, this._then);

  final _IntercityDeparture _self;
  final $Res Function(_IntercityDeparture) _then;

/// Create a copy of IntercityDeparture
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? departureTime = null,}) {
  return _then(_IntercityDeparture(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,departureTime: null == departureTime ? _self.departureTime : departureTime // ignore: cast_nullable_to_non_nullable
as String,
  ));
}


}

// dart format on
