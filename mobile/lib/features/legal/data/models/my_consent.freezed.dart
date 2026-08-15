// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'my_consent.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$MyConsent {

 String get type; String get title; bool get isMandatory;/// Yayındaki sürüm — "onayınız güncel mi?" karşılaştırmasının sol tarafı.
 String get currentVersionId; int get currentVersionNumber;/// Kullanıcının karar verdiği sürüm; hiç karar vermemişse `null`.
 String? get consentedVersionId; int? get consentedVersionNumber;/// ⚠️ `false`, "hiç sorulmadı" demek **değildir** — onun cevabı
/// [decidedAt]'in `null` olmasıdır ("sormadık" ≠ "sorduk, hayır dedi").
 bool get granted; DateTime? get decidedAt; DateTime? get revokedAt;/// 🔑 **Sunucuda türetilir.** İstemcide hesaplansaydı iki sahip doğardı ve
/// mağazadaki eski sürümler kuralın eski hâlini uygulamaya devam ederdi.
 bool get needsReconsent;
/// Create a copy of MyConsent
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$MyConsentCopyWith<MyConsent> get copyWith => _$MyConsentCopyWithImpl<MyConsent>(this as MyConsent, _$identity);

  /// Serializes this MyConsent to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is MyConsent&&(identical(other.type, type) || other.type == type)&&(identical(other.title, title) || other.title == title)&&(identical(other.isMandatory, isMandatory) || other.isMandatory == isMandatory)&&(identical(other.currentVersionId, currentVersionId) || other.currentVersionId == currentVersionId)&&(identical(other.currentVersionNumber, currentVersionNumber) || other.currentVersionNumber == currentVersionNumber)&&(identical(other.consentedVersionId, consentedVersionId) || other.consentedVersionId == consentedVersionId)&&(identical(other.consentedVersionNumber, consentedVersionNumber) || other.consentedVersionNumber == consentedVersionNumber)&&(identical(other.granted, granted) || other.granted == granted)&&(identical(other.decidedAt, decidedAt) || other.decidedAt == decidedAt)&&(identical(other.revokedAt, revokedAt) || other.revokedAt == revokedAt)&&(identical(other.needsReconsent, needsReconsent) || other.needsReconsent == needsReconsent));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,type,title,isMandatory,currentVersionId,currentVersionNumber,consentedVersionId,consentedVersionNumber,granted,decidedAt,revokedAt,needsReconsent);

@override
String toString() {
  return 'MyConsent(type: $type, title: $title, isMandatory: $isMandatory, currentVersionId: $currentVersionId, currentVersionNumber: $currentVersionNumber, consentedVersionId: $consentedVersionId, consentedVersionNumber: $consentedVersionNumber, granted: $granted, decidedAt: $decidedAt, revokedAt: $revokedAt, needsReconsent: $needsReconsent)';
}


}

/// @nodoc
abstract mixin class $MyConsentCopyWith<$Res>  {
  factory $MyConsentCopyWith(MyConsent value, $Res Function(MyConsent) _then) = _$MyConsentCopyWithImpl;
@useResult
$Res call({
 String type, String title, bool isMandatory, String currentVersionId, int currentVersionNumber, String? consentedVersionId, int? consentedVersionNumber, bool granted, DateTime? decidedAt, DateTime? revokedAt, bool needsReconsent
});




}
/// @nodoc
class _$MyConsentCopyWithImpl<$Res>
    implements $MyConsentCopyWith<$Res> {
  _$MyConsentCopyWithImpl(this._self, this._then);

  final MyConsent _self;
  final $Res Function(MyConsent) _then;

/// Create a copy of MyConsent
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? type = null,Object? title = null,Object? isMandatory = null,Object? currentVersionId = null,Object? currentVersionNumber = null,Object? consentedVersionId = freezed,Object? consentedVersionNumber = freezed,Object? granted = null,Object? decidedAt = freezed,Object? revokedAt = freezed,Object? needsReconsent = null,}) {
  return _then(_self.copyWith(
type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,isMandatory: null == isMandatory ? _self.isMandatory : isMandatory // ignore: cast_nullable_to_non_nullable
as bool,currentVersionId: null == currentVersionId ? _self.currentVersionId : currentVersionId // ignore: cast_nullable_to_non_nullable
as String,currentVersionNumber: null == currentVersionNumber ? _self.currentVersionNumber : currentVersionNumber // ignore: cast_nullable_to_non_nullable
as int,consentedVersionId: freezed == consentedVersionId ? _self.consentedVersionId : consentedVersionId // ignore: cast_nullable_to_non_nullable
as String?,consentedVersionNumber: freezed == consentedVersionNumber ? _self.consentedVersionNumber : consentedVersionNumber // ignore: cast_nullable_to_non_nullable
as int?,granted: null == granted ? _self.granted : granted // ignore: cast_nullable_to_non_nullable
as bool,decidedAt: freezed == decidedAt ? _self.decidedAt : decidedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,revokedAt: freezed == revokedAt ? _self.revokedAt : revokedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,needsReconsent: null == needsReconsent ? _self.needsReconsent : needsReconsent // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [MyConsent].
extension MyConsentPatterns on MyConsent {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _MyConsent value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _MyConsent() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _MyConsent value)  $default,){
final _that = this;
switch (_that) {
case _MyConsent():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _MyConsent value)?  $default,){
final _that = this;
switch (_that) {
case _MyConsent() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String type,  String title,  bool isMandatory,  String currentVersionId,  int currentVersionNumber,  String? consentedVersionId,  int? consentedVersionNumber,  bool granted,  DateTime? decidedAt,  DateTime? revokedAt,  bool needsReconsent)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _MyConsent() when $default != null:
return $default(_that.type,_that.title,_that.isMandatory,_that.currentVersionId,_that.currentVersionNumber,_that.consentedVersionId,_that.consentedVersionNumber,_that.granted,_that.decidedAt,_that.revokedAt,_that.needsReconsent);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String type,  String title,  bool isMandatory,  String currentVersionId,  int currentVersionNumber,  String? consentedVersionId,  int? consentedVersionNumber,  bool granted,  DateTime? decidedAt,  DateTime? revokedAt,  bool needsReconsent)  $default,) {final _that = this;
switch (_that) {
case _MyConsent():
return $default(_that.type,_that.title,_that.isMandatory,_that.currentVersionId,_that.currentVersionNumber,_that.consentedVersionId,_that.consentedVersionNumber,_that.granted,_that.decidedAt,_that.revokedAt,_that.needsReconsent);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String type,  String title,  bool isMandatory,  String currentVersionId,  int currentVersionNumber,  String? consentedVersionId,  int? consentedVersionNumber,  bool granted,  DateTime? decidedAt,  DateTime? revokedAt,  bool needsReconsent)?  $default,) {final _that = this;
switch (_that) {
case _MyConsent() when $default != null:
return $default(_that.type,_that.title,_that.isMandatory,_that.currentVersionId,_that.currentVersionNumber,_that.consentedVersionId,_that.consentedVersionNumber,_that.granted,_that.decidedAt,_that.revokedAt,_that.needsReconsent);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _MyConsent extends MyConsent {
  const _MyConsent({required this.type, required this.title, this.isMandatory = false, required this.currentVersionId, this.currentVersionNumber = 1, this.consentedVersionId, this.consentedVersionNumber, this.granted = false, this.decidedAt, this.revokedAt, this.needsReconsent = false}): super._();
  factory _MyConsent.fromJson(Map<String, dynamic> json) => _$MyConsentFromJson(json);

@override final  String type;
@override final  String title;
@override@JsonKey() final  bool isMandatory;
/// Yayındaki sürüm — "onayınız güncel mi?" karşılaştırmasının sol tarafı.
@override final  String currentVersionId;
@override@JsonKey() final  int currentVersionNumber;
/// Kullanıcının karar verdiği sürüm; hiç karar vermemişse `null`.
@override final  String? consentedVersionId;
@override final  int? consentedVersionNumber;
/// ⚠️ `false`, "hiç sorulmadı" demek **değildir** — onun cevabı
/// [decidedAt]'in `null` olmasıdır ("sormadık" ≠ "sorduk, hayır dedi").
@override@JsonKey() final  bool granted;
@override final  DateTime? decidedAt;
@override final  DateTime? revokedAt;
/// 🔑 **Sunucuda türetilir.** İstemcide hesaplansaydı iki sahip doğardı ve
/// mağazadaki eski sürümler kuralın eski hâlini uygulamaya devam ederdi.
@override@JsonKey() final  bool needsReconsent;

/// Create a copy of MyConsent
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$MyConsentCopyWith<_MyConsent> get copyWith => __$MyConsentCopyWithImpl<_MyConsent>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$MyConsentToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _MyConsent&&(identical(other.type, type) || other.type == type)&&(identical(other.title, title) || other.title == title)&&(identical(other.isMandatory, isMandatory) || other.isMandatory == isMandatory)&&(identical(other.currentVersionId, currentVersionId) || other.currentVersionId == currentVersionId)&&(identical(other.currentVersionNumber, currentVersionNumber) || other.currentVersionNumber == currentVersionNumber)&&(identical(other.consentedVersionId, consentedVersionId) || other.consentedVersionId == consentedVersionId)&&(identical(other.consentedVersionNumber, consentedVersionNumber) || other.consentedVersionNumber == consentedVersionNumber)&&(identical(other.granted, granted) || other.granted == granted)&&(identical(other.decidedAt, decidedAt) || other.decidedAt == decidedAt)&&(identical(other.revokedAt, revokedAt) || other.revokedAt == revokedAt)&&(identical(other.needsReconsent, needsReconsent) || other.needsReconsent == needsReconsent));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,type,title,isMandatory,currentVersionId,currentVersionNumber,consentedVersionId,consentedVersionNumber,granted,decidedAt,revokedAt,needsReconsent);

@override
String toString() {
  return 'MyConsent(type: $type, title: $title, isMandatory: $isMandatory, currentVersionId: $currentVersionId, currentVersionNumber: $currentVersionNumber, consentedVersionId: $consentedVersionId, consentedVersionNumber: $consentedVersionNumber, granted: $granted, decidedAt: $decidedAt, revokedAt: $revokedAt, needsReconsent: $needsReconsent)';
}


}

/// @nodoc
abstract mixin class _$MyConsentCopyWith<$Res> implements $MyConsentCopyWith<$Res> {
  factory _$MyConsentCopyWith(_MyConsent value, $Res Function(_MyConsent) _then) = __$MyConsentCopyWithImpl;
@override @useResult
$Res call({
 String type, String title, bool isMandatory, String currentVersionId, int currentVersionNumber, String? consentedVersionId, int? consentedVersionNumber, bool granted, DateTime? decidedAt, DateTime? revokedAt, bool needsReconsent
});




}
/// @nodoc
class __$MyConsentCopyWithImpl<$Res>
    implements _$MyConsentCopyWith<$Res> {
  __$MyConsentCopyWithImpl(this._self, this._then);

  final _MyConsent _self;
  final $Res Function(_MyConsent) _then;

/// Create a copy of MyConsent
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? type = null,Object? title = null,Object? isMandatory = null,Object? currentVersionId = null,Object? currentVersionNumber = null,Object? consentedVersionId = freezed,Object? consentedVersionNumber = freezed,Object? granted = null,Object? decidedAt = freezed,Object? revokedAt = freezed,Object? needsReconsent = null,}) {
  return _then(_MyConsent(
type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,isMandatory: null == isMandatory ? _self.isMandatory : isMandatory // ignore: cast_nullable_to_non_nullable
as bool,currentVersionId: null == currentVersionId ? _self.currentVersionId : currentVersionId // ignore: cast_nullable_to_non_nullable
as String,currentVersionNumber: null == currentVersionNumber ? _self.currentVersionNumber : currentVersionNumber // ignore: cast_nullable_to_non_nullable
as int,consentedVersionId: freezed == consentedVersionId ? _self.consentedVersionId : consentedVersionId // ignore: cast_nullable_to_non_nullable
as String?,consentedVersionNumber: freezed == consentedVersionNumber ? _self.consentedVersionNumber : consentedVersionNumber // ignore: cast_nullable_to_non_nullable
as int?,granted: null == granted ? _self.granted : granted // ignore: cast_nullable_to_non_nullable
as bool,decidedAt: freezed == decidedAt ? _self.decidedAt : decidedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,revokedAt: freezed == revokedAt ? _self.revokedAt : revokedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,needsReconsent: null == needsReconsent ? _self.needsReconsent : needsReconsent // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
