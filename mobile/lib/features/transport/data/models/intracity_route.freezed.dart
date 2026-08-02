// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'intracity_route.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$IntracityRoute {

 String get id; String get routeNumber; String get routeName; String? get firstDeparture; String? get lastDeparture; int? get frequencyMinutes; bool get isActive; List<IntracityStop> get stops;
/// Create a copy of IntracityRoute
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$IntracityRouteCopyWith<IntracityRoute> get copyWith => _$IntracityRouteCopyWithImpl<IntracityRoute>(this as IntracityRoute, _$identity);

  /// Serializes this IntracityRoute to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is IntracityRoute&&(identical(other.id, id) || other.id == id)&&(identical(other.routeNumber, routeNumber) || other.routeNumber == routeNumber)&&(identical(other.routeName, routeName) || other.routeName == routeName)&&(identical(other.firstDeparture, firstDeparture) || other.firstDeparture == firstDeparture)&&(identical(other.lastDeparture, lastDeparture) || other.lastDeparture == lastDeparture)&&(identical(other.frequencyMinutes, frequencyMinutes) || other.frequencyMinutes == frequencyMinutes)&&(identical(other.isActive, isActive) || other.isActive == isActive)&&const DeepCollectionEquality().equals(other.stops, stops));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,routeNumber,routeName,firstDeparture,lastDeparture,frequencyMinutes,isActive,const DeepCollectionEquality().hash(stops));

@override
String toString() {
  return 'IntracityRoute(id: $id, routeNumber: $routeNumber, routeName: $routeName, firstDeparture: $firstDeparture, lastDeparture: $lastDeparture, frequencyMinutes: $frequencyMinutes, isActive: $isActive, stops: $stops)';
}


}

/// @nodoc
abstract mixin class $IntracityRouteCopyWith<$Res>  {
  factory $IntracityRouteCopyWith(IntracityRoute value, $Res Function(IntracityRoute) _then) = _$IntracityRouteCopyWithImpl;
@useResult
$Res call({
 String id, String routeNumber, String routeName, String? firstDeparture, String? lastDeparture, int? frequencyMinutes, bool isActive, List<IntracityStop> stops
});




}
/// @nodoc
class _$IntracityRouteCopyWithImpl<$Res>
    implements $IntracityRouteCopyWith<$Res> {
  _$IntracityRouteCopyWithImpl(this._self, this._then);

  final IntracityRoute _self;
  final $Res Function(IntracityRoute) _then;

/// Create a copy of IntracityRoute
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? routeNumber = null,Object? routeName = null,Object? firstDeparture = freezed,Object? lastDeparture = freezed,Object? frequencyMinutes = freezed,Object? isActive = null,Object? stops = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,routeNumber: null == routeNumber ? _self.routeNumber : routeNumber // ignore: cast_nullable_to_non_nullable
as String,routeName: null == routeName ? _self.routeName : routeName // ignore: cast_nullable_to_non_nullable
as String,firstDeparture: freezed == firstDeparture ? _self.firstDeparture : firstDeparture // ignore: cast_nullable_to_non_nullable
as String?,lastDeparture: freezed == lastDeparture ? _self.lastDeparture : lastDeparture // ignore: cast_nullable_to_non_nullable
as String?,frequencyMinutes: freezed == frequencyMinutes ? _self.frequencyMinutes : frequencyMinutes // ignore: cast_nullable_to_non_nullable
as int?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,stops: null == stops ? _self.stops : stops // ignore: cast_nullable_to_non_nullable
as List<IntracityStop>,
  ));
}

}


/// Adds pattern-matching-related methods to [IntracityRoute].
extension IntracityRoutePatterns on IntracityRoute {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _IntracityRoute value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _IntracityRoute() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _IntracityRoute value)  $default,){
final _that = this;
switch (_that) {
case _IntracityRoute():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _IntracityRoute value)?  $default,){
final _that = this;
switch (_that) {
case _IntracityRoute() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String routeNumber,  String routeName,  String? firstDeparture,  String? lastDeparture,  int? frequencyMinutes,  bool isActive,  List<IntracityStop> stops)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _IntracityRoute() when $default != null:
return $default(_that.id,_that.routeNumber,_that.routeName,_that.firstDeparture,_that.lastDeparture,_that.frequencyMinutes,_that.isActive,_that.stops);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String routeNumber,  String routeName,  String? firstDeparture,  String? lastDeparture,  int? frequencyMinutes,  bool isActive,  List<IntracityStop> stops)  $default,) {final _that = this;
switch (_that) {
case _IntracityRoute():
return $default(_that.id,_that.routeNumber,_that.routeName,_that.firstDeparture,_that.lastDeparture,_that.frequencyMinutes,_that.isActive,_that.stops);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String routeNumber,  String routeName,  String? firstDeparture,  String? lastDeparture,  int? frequencyMinutes,  bool isActive,  List<IntracityStop> stops)?  $default,) {final _that = this;
switch (_that) {
case _IntracityRoute() when $default != null:
return $default(_that.id,_that.routeNumber,_that.routeName,_that.firstDeparture,_that.lastDeparture,_that.frequencyMinutes,_that.isActive,_that.stops);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _IntracityRoute extends IntracityRoute {
  const _IntracityRoute({required this.id, this.routeNumber = '', this.routeName = '', this.firstDeparture, this.lastDeparture, this.frequencyMinutes, this.isActive = true, final  List<IntracityStop> stops = const <IntracityStop>[]}): _stops = stops,super._();
  factory _IntracityRoute.fromJson(Map<String, dynamic> json) => _$IntracityRouteFromJson(json);

@override final  String id;
@override@JsonKey() final  String routeNumber;
@override@JsonKey() final  String routeName;
@override final  String? firstDeparture;
@override final  String? lastDeparture;
@override final  int? frequencyMinutes;
@override@JsonKey() final  bool isActive;
 final  List<IntracityStop> _stops;
@override@JsonKey() List<IntracityStop> get stops {
  if (_stops is EqualUnmodifiableListView) return _stops;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_stops);
}


/// Create a copy of IntracityRoute
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$IntracityRouteCopyWith<_IntracityRoute> get copyWith => __$IntracityRouteCopyWithImpl<_IntracityRoute>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$IntracityRouteToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _IntracityRoute&&(identical(other.id, id) || other.id == id)&&(identical(other.routeNumber, routeNumber) || other.routeNumber == routeNumber)&&(identical(other.routeName, routeName) || other.routeName == routeName)&&(identical(other.firstDeparture, firstDeparture) || other.firstDeparture == firstDeparture)&&(identical(other.lastDeparture, lastDeparture) || other.lastDeparture == lastDeparture)&&(identical(other.frequencyMinutes, frequencyMinutes) || other.frequencyMinutes == frequencyMinutes)&&(identical(other.isActive, isActive) || other.isActive == isActive)&&const DeepCollectionEquality().equals(other._stops, _stops));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,routeNumber,routeName,firstDeparture,lastDeparture,frequencyMinutes,isActive,const DeepCollectionEquality().hash(_stops));

@override
String toString() {
  return 'IntracityRoute(id: $id, routeNumber: $routeNumber, routeName: $routeName, firstDeparture: $firstDeparture, lastDeparture: $lastDeparture, frequencyMinutes: $frequencyMinutes, isActive: $isActive, stops: $stops)';
}


}

/// @nodoc
abstract mixin class _$IntracityRouteCopyWith<$Res> implements $IntracityRouteCopyWith<$Res> {
  factory _$IntracityRouteCopyWith(_IntracityRoute value, $Res Function(_IntracityRoute) _then) = __$IntracityRouteCopyWithImpl;
@override @useResult
$Res call({
 String id, String routeNumber, String routeName, String? firstDeparture, String? lastDeparture, int? frequencyMinutes, bool isActive, List<IntracityStop> stops
});




}
/// @nodoc
class __$IntracityRouteCopyWithImpl<$Res>
    implements _$IntracityRouteCopyWith<$Res> {
  __$IntracityRouteCopyWithImpl(this._self, this._then);

  final _IntracityRoute _self;
  final $Res Function(_IntracityRoute) _then;

/// Create a copy of IntracityRoute
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? routeNumber = null,Object? routeName = null,Object? firstDeparture = freezed,Object? lastDeparture = freezed,Object? frequencyMinutes = freezed,Object? isActive = null,Object? stops = null,}) {
  return _then(_IntracityRoute(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,routeNumber: null == routeNumber ? _self.routeNumber : routeNumber // ignore: cast_nullable_to_non_nullable
as String,routeName: null == routeName ? _self.routeName : routeName // ignore: cast_nullable_to_non_nullable
as String,firstDeparture: freezed == firstDeparture ? _self.firstDeparture : firstDeparture // ignore: cast_nullable_to_non_nullable
as String?,lastDeparture: freezed == lastDeparture ? _self.lastDeparture : lastDeparture // ignore: cast_nullable_to_non_nullable
as String?,frequencyMinutes: freezed == frequencyMinutes ? _self.frequencyMinutes : frequencyMinutes // ignore: cast_nullable_to_non_nullable
as int?,isActive: null == isActive ? _self.isActive : isActive // ignore: cast_nullable_to_non_nullable
as bool,stops: null == stops ? _self._stops : stops // ignore: cast_nullable_to_non_nullable
as List<IntracityStop>,
  ));
}


}


/// @nodoc
mixin _$IntracityStop {

 String get id; String get stopName; int get stopOrder; int? get timeFromStart;
/// Create a copy of IntracityStop
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$IntracityStopCopyWith<IntracityStop> get copyWith => _$IntracityStopCopyWithImpl<IntracityStop>(this as IntracityStop, _$identity);

  /// Serializes this IntracityStop to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is IntracityStop&&(identical(other.id, id) || other.id == id)&&(identical(other.stopName, stopName) || other.stopName == stopName)&&(identical(other.stopOrder, stopOrder) || other.stopOrder == stopOrder)&&(identical(other.timeFromStart, timeFromStart) || other.timeFromStart == timeFromStart));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,stopName,stopOrder,timeFromStart);

@override
String toString() {
  return 'IntracityStop(id: $id, stopName: $stopName, stopOrder: $stopOrder, timeFromStart: $timeFromStart)';
}


}

/// @nodoc
abstract mixin class $IntracityStopCopyWith<$Res>  {
  factory $IntracityStopCopyWith(IntracityStop value, $Res Function(IntracityStop) _then) = _$IntracityStopCopyWithImpl;
@useResult
$Res call({
 String id, String stopName, int stopOrder, int? timeFromStart
});




}
/// @nodoc
class _$IntracityStopCopyWithImpl<$Res>
    implements $IntracityStopCopyWith<$Res> {
  _$IntracityStopCopyWithImpl(this._self, this._then);

  final IntracityStop _self;
  final $Res Function(IntracityStop) _then;

/// Create a copy of IntracityStop
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? stopName = null,Object? stopOrder = null,Object? timeFromStart = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,stopName: null == stopName ? _self.stopName : stopName // ignore: cast_nullable_to_non_nullable
as String,stopOrder: null == stopOrder ? _self.stopOrder : stopOrder // ignore: cast_nullable_to_non_nullable
as int,timeFromStart: freezed == timeFromStart ? _self.timeFromStart : timeFromStart // ignore: cast_nullable_to_non_nullable
as int?,
  ));
}

}


/// Adds pattern-matching-related methods to [IntracityStop].
extension IntracityStopPatterns on IntracityStop {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _IntracityStop value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _IntracityStop() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _IntracityStop value)  $default,){
final _that = this;
switch (_that) {
case _IntracityStop():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _IntracityStop value)?  $default,){
final _that = this;
switch (_that) {
case _IntracityStop() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String stopName,  int stopOrder,  int? timeFromStart)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _IntracityStop() when $default != null:
return $default(_that.id,_that.stopName,_that.stopOrder,_that.timeFromStart);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String stopName,  int stopOrder,  int? timeFromStart)  $default,) {final _that = this;
switch (_that) {
case _IntracityStop():
return $default(_that.id,_that.stopName,_that.stopOrder,_that.timeFromStart);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String stopName,  int stopOrder,  int? timeFromStart)?  $default,) {final _that = this;
switch (_that) {
case _IntracityStop() when $default != null:
return $default(_that.id,_that.stopName,_that.stopOrder,_that.timeFromStart);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _IntracityStop extends IntracityStop {
  const _IntracityStop({required this.id, this.stopName = '', this.stopOrder = 0, this.timeFromStart}): super._();
  factory _IntracityStop.fromJson(Map<String, dynamic> json) => _$IntracityStopFromJson(json);

@override final  String id;
@override@JsonKey() final  String stopName;
@override@JsonKey() final  int stopOrder;
@override final  int? timeFromStart;

/// Create a copy of IntracityStop
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$IntracityStopCopyWith<_IntracityStop> get copyWith => __$IntracityStopCopyWithImpl<_IntracityStop>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$IntracityStopToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _IntracityStop&&(identical(other.id, id) || other.id == id)&&(identical(other.stopName, stopName) || other.stopName == stopName)&&(identical(other.stopOrder, stopOrder) || other.stopOrder == stopOrder)&&(identical(other.timeFromStart, timeFromStart) || other.timeFromStart == timeFromStart));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,stopName,stopOrder,timeFromStart);

@override
String toString() {
  return 'IntracityStop(id: $id, stopName: $stopName, stopOrder: $stopOrder, timeFromStart: $timeFromStart)';
}


}

/// @nodoc
abstract mixin class _$IntracityStopCopyWith<$Res> implements $IntracityStopCopyWith<$Res> {
  factory _$IntracityStopCopyWith(_IntracityStop value, $Res Function(_IntracityStop) _then) = __$IntracityStopCopyWithImpl;
@override @useResult
$Res call({
 String id, String stopName, int stopOrder, int? timeFromStart
});




}
/// @nodoc
class __$IntracityStopCopyWithImpl<$Res>
    implements _$IntracityStopCopyWith<$Res> {
  __$IntracityStopCopyWithImpl(this._self, this._then);

  final _IntracityStop _self;
  final $Res Function(_IntracityStop) _then;

/// Create a copy of IntracityStop
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? stopName = null,Object? stopOrder = null,Object? timeFromStart = freezed,}) {
  return _then(_IntracityStop(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,stopName: null == stopName ? _self.stopName : stopName // ignore: cast_nullable_to_non_nullable
as String,stopOrder: null == stopOrder ? _self.stopOrder : stopOrder // ignore: cast_nullable_to_non_nullable
as int,timeFromStart: freezed == timeFromStart ? _self.timeFromStart : timeFromStart // ignore: cast_nullable_to_non_nullable
as int?,
  ));
}


}

// dart format on
