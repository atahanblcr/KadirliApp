// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'notification_preferences.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$NotificationPreferences {

 bool get announcements; bool get deaths; bool get pharmacy; bool get events; bool get ads; bool get campaigns;
/// Create a copy of NotificationPreferences
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NotificationPreferencesCopyWith<NotificationPreferences> get copyWith => _$NotificationPreferencesCopyWithImpl<NotificationPreferences>(this as NotificationPreferences, _$identity);

  /// Serializes this NotificationPreferences to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NotificationPreferences&&(identical(other.announcements, announcements) || other.announcements == announcements)&&(identical(other.deaths, deaths) || other.deaths == deaths)&&(identical(other.pharmacy, pharmacy) || other.pharmacy == pharmacy)&&(identical(other.events, events) || other.events == events)&&(identical(other.ads, ads) || other.ads == ads)&&(identical(other.campaigns, campaigns) || other.campaigns == campaigns));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,announcements,deaths,pharmacy,events,ads,campaigns);

@override
String toString() {
  return 'NotificationPreferences(announcements: $announcements, deaths: $deaths, pharmacy: $pharmacy, events: $events, ads: $ads, campaigns: $campaigns)';
}


}

/// @nodoc
abstract mixin class $NotificationPreferencesCopyWith<$Res>  {
  factory $NotificationPreferencesCopyWith(NotificationPreferences value, $Res Function(NotificationPreferences) _then) = _$NotificationPreferencesCopyWithImpl;
@useResult
$Res call({
 bool announcements, bool deaths, bool pharmacy, bool events, bool ads, bool campaigns
});




}
/// @nodoc
class _$NotificationPreferencesCopyWithImpl<$Res>
    implements $NotificationPreferencesCopyWith<$Res> {
  _$NotificationPreferencesCopyWithImpl(this._self, this._then);

  final NotificationPreferences _self;
  final $Res Function(NotificationPreferences) _then;

/// Create a copy of NotificationPreferences
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? announcements = null,Object? deaths = null,Object? pharmacy = null,Object? events = null,Object? ads = null,Object? campaigns = null,}) {
  return _then(_self.copyWith(
announcements: null == announcements ? _self.announcements : announcements // ignore: cast_nullable_to_non_nullable
as bool,deaths: null == deaths ? _self.deaths : deaths // ignore: cast_nullable_to_non_nullable
as bool,pharmacy: null == pharmacy ? _self.pharmacy : pharmacy // ignore: cast_nullable_to_non_nullable
as bool,events: null == events ? _self.events : events // ignore: cast_nullable_to_non_nullable
as bool,ads: null == ads ? _self.ads : ads // ignore: cast_nullable_to_non_nullable
as bool,campaigns: null == campaigns ? _self.campaigns : campaigns // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [NotificationPreferences].
extension NotificationPreferencesPatterns on NotificationPreferences {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NotificationPreferences value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NotificationPreferences() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NotificationPreferences value)  $default,){
final _that = this;
switch (_that) {
case _NotificationPreferences():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NotificationPreferences value)?  $default,){
final _that = this;
switch (_that) {
case _NotificationPreferences() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( bool announcements,  bool deaths,  bool pharmacy,  bool events,  bool ads,  bool campaigns)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NotificationPreferences() when $default != null:
return $default(_that.announcements,_that.deaths,_that.pharmacy,_that.events,_that.ads,_that.campaigns);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( bool announcements,  bool deaths,  bool pharmacy,  bool events,  bool ads,  bool campaigns)  $default,) {final _that = this;
switch (_that) {
case _NotificationPreferences():
return $default(_that.announcements,_that.deaths,_that.pharmacy,_that.events,_that.ads,_that.campaigns);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( bool announcements,  bool deaths,  bool pharmacy,  bool events,  bool ads,  bool campaigns)?  $default,) {final _that = this;
switch (_that) {
case _NotificationPreferences() when $default != null:
return $default(_that.announcements,_that.deaths,_that.pharmacy,_that.events,_that.ads,_that.campaigns);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NotificationPreferences extends NotificationPreferences {
  const _NotificationPreferences({this.announcements = true, this.deaths = true, this.pharmacy = true, this.events = true, this.ads = false, this.campaigns = false}): super._();
  factory _NotificationPreferences.fromJson(Map<String, dynamic> json) => _$NotificationPreferencesFromJson(json);

@override@JsonKey() final  bool announcements;
@override@JsonKey() final  bool deaths;
@override@JsonKey() final  bool pharmacy;
@override@JsonKey() final  bool events;
@override@JsonKey() final  bool ads;
@override@JsonKey() final  bool campaigns;

/// Create a copy of NotificationPreferences
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NotificationPreferencesCopyWith<_NotificationPreferences> get copyWith => __$NotificationPreferencesCopyWithImpl<_NotificationPreferences>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NotificationPreferencesToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _NotificationPreferences&&(identical(other.announcements, announcements) || other.announcements == announcements)&&(identical(other.deaths, deaths) || other.deaths == deaths)&&(identical(other.pharmacy, pharmacy) || other.pharmacy == pharmacy)&&(identical(other.events, events) || other.events == events)&&(identical(other.ads, ads) || other.ads == ads)&&(identical(other.campaigns, campaigns) || other.campaigns == campaigns));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,announcements,deaths,pharmacy,events,ads,campaigns);

@override
String toString() {
  return 'NotificationPreferences(announcements: $announcements, deaths: $deaths, pharmacy: $pharmacy, events: $events, ads: $ads, campaigns: $campaigns)';
}


}

/// @nodoc
abstract mixin class _$NotificationPreferencesCopyWith<$Res> implements $NotificationPreferencesCopyWith<$Res> {
  factory _$NotificationPreferencesCopyWith(_NotificationPreferences value, $Res Function(_NotificationPreferences) _then) = __$NotificationPreferencesCopyWithImpl;
@override @useResult
$Res call({
 bool announcements, bool deaths, bool pharmacy, bool events, bool ads, bool campaigns
});




}
/// @nodoc
class __$NotificationPreferencesCopyWithImpl<$Res>
    implements _$NotificationPreferencesCopyWith<$Res> {
  __$NotificationPreferencesCopyWithImpl(this._self, this._then);

  final _NotificationPreferences _self;
  final $Res Function(_NotificationPreferences) _then;

/// Create a copy of NotificationPreferences
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? announcements = null,Object? deaths = null,Object? pharmacy = null,Object? events = null,Object? ads = null,Object? campaigns = null,}) {
  return _then(_NotificationPreferences(
announcements: null == announcements ? _self.announcements : announcements // ignore: cast_nullable_to_non_nullable
as bool,deaths: null == deaths ? _self.deaths : deaths // ignore: cast_nullable_to_non_nullable
as bool,pharmacy: null == pharmacy ? _self.pharmacy : pharmacy // ignore: cast_nullable_to_non_nullable
as bool,events: null == events ? _self.events : events // ignore: cast_nullable_to_non_nullable
as bool,ads: null == ads ? _self.ads : ads // ignore: cast_nullable_to_non_nullable
as bool,campaigns: null == campaigns ? _self.campaigns : campaigns // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
