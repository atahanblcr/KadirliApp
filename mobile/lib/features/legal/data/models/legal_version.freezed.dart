// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'legal_version.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$LegalVersion {

 String get id; String get documentType; String get documentTitle; int get versionNumber; String? get summary; String get body; DateTime? get effectiveFrom; DateTime? get publishedAt; bool get isLive; DateTime? get supersededAt;
/// Create a copy of LegalVersion
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LegalVersionCopyWith<LegalVersion> get copyWith => _$LegalVersionCopyWithImpl<LegalVersion>(this as LegalVersion, _$identity);

  /// Serializes this LegalVersion to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LegalVersion&&(identical(other.id, id) || other.id == id)&&(identical(other.documentType, documentType) || other.documentType == documentType)&&(identical(other.documentTitle, documentTitle) || other.documentTitle == documentTitle)&&(identical(other.versionNumber, versionNumber) || other.versionNumber == versionNumber)&&(identical(other.summary, summary) || other.summary == summary)&&(identical(other.body, body) || other.body == body)&&(identical(other.effectiveFrom, effectiveFrom) || other.effectiveFrom == effectiveFrom)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt)&&(identical(other.isLive, isLive) || other.isLive == isLive)&&(identical(other.supersededAt, supersededAt) || other.supersededAt == supersededAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,documentType,documentTitle,versionNumber,summary,body,effectiveFrom,publishedAt,isLive,supersededAt);

@override
String toString() {
  return 'LegalVersion(id: $id, documentType: $documentType, documentTitle: $documentTitle, versionNumber: $versionNumber, summary: $summary, body: $body, effectiveFrom: $effectiveFrom, publishedAt: $publishedAt, isLive: $isLive, supersededAt: $supersededAt)';
}


}

/// @nodoc
abstract mixin class $LegalVersionCopyWith<$Res>  {
  factory $LegalVersionCopyWith(LegalVersion value, $Res Function(LegalVersion) _then) = _$LegalVersionCopyWithImpl;
@useResult
$Res call({
 String id, String documentType, String documentTitle, int versionNumber, String? summary, String body, DateTime? effectiveFrom, DateTime? publishedAt, bool isLive, DateTime? supersededAt
});




}
/// @nodoc
class _$LegalVersionCopyWithImpl<$Res>
    implements $LegalVersionCopyWith<$Res> {
  _$LegalVersionCopyWithImpl(this._self, this._then);

  final LegalVersion _self;
  final $Res Function(LegalVersion) _then;

/// Create a copy of LegalVersion
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? documentType = null,Object? documentTitle = null,Object? versionNumber = null,Object? summary = freezed,Object? body = null,Object? effectiveFrom = freezed,Object? publishedAt = freezed,Object? isLive = null,Object? supersededAt = freezed,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,documentType: null == documentType ? _self.documentType : documentType // ignore: cast_nullable_to_non_nullable
as String,documentTitle: null == documentTitle ? _self.documentTitle : documentTitle // ignore: cast_nullable_to_non_nullable
as String,versionNumber: null == versionNumber ? _self.versionNumber : versionNumber // ignore: cast_nullable_to_non_nullable
as int,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,effectiveFrom: freezed == effectiveFrom ? _self.effectiveFrom : effectiveFrom // ignore: cast_nullable_to_non_nullable
as DateTime?,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,isLive: null == isLive ? _self.isLive : isLive // ignore: cast_nullable_to_non_nullable
as bool,supersededAt: freezed == supersededAt ? _self.supersededAt : supersededAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}

}


/// Adds pattern-matching-related methods to [LegalVersion].
extension LegalVersionPatterns on LegalVersion {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LegalVersion value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LegalVersion() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LegalVersion value)  $default,){
final _that = this;
switch (_that) {
case _LegalVersion():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LegalVersion value)?  $default,){
final _that = this;
switch (_that) {
case _LegalVersion() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String documentType,  String documentTitle,  int versionNumber,  String? summary,  String body,  DateTime? effectiveFrom,  DateTime? publishedAt,  bool isLive,  DateTime? supersededAt)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LegalVersion() when $default != null:
return $default(_that.id,_that.documentType,_that.documentTitle,_that.versionNumber,_that.summary,_that.body,_that.effectiveFrom,_that.publishedAt,_that.isLive,_that.supersededAt);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String documentType,  String documentTitle,  int versionNumber,  String? summary,  String body,  DateTime? effectiveFrom,  DateTime? publishedAt,  bool isLive,  DateTime? supersededAt)  $default,) {final _that = this;
switch (_that) {
case _LegalVersion():
return $default(_that.id,_that.documentType,_that.documentTitle,_that.versionNumber,_that.summary,_that.body,_that.effectiveFrom,_that.publishedAt,_that.isLive,_that.supersededAt);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String documentType,  String documentTitle,  int versionNumber,  String? summary,  String body,  DateTime? effectiveFrom,  DateTime? publishedAt,  bool isLive,  DateTime? supersededAt)?  $default,) {final _that = this;
switch (_that) {
case _LegalVersion() when $default != null:
return $default(_that.id,_that.documentType,_that.documentTitle,_that.versionNumber,_that.summary,_that.body,_that.effectiveFrom,_that.publishedAt,_that.isLive,_that.supersededAt);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LegalVersion extends LegalVersion {
  const _LegalVersion({required this.id, required this.documentType, required this.documentTitle, this.versionNumber = 1, this.summary, this.body = '', this.effectiveFrom, this.publishedAt, this.isLive = false, this.supersededAt}): super._();
  factory _LegalVersion.fromJson(Map<String, dynamic> json) => _$LegalVersionFromJson(json);

@override final  String id;
@override final  String documentType;
@override final  String documentTitle;
@override@JsonKey() final  int versionNumber;
@override final  String? summary;
@override@JsonKey() final  String body;
@override final  DateTime? effectiveFrom;
@override final  DateTime? publishedAt;
@override@JsonKey() final  bool isLive;
@override final  DateTime? supersededAt;

/// Create a copy of LegalVersion
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LegalVersionCopyWith<_LegalVersion> get copyWith => __$LegalVersionCopyWithImpl<_LegalVersion>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LegalVersionToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LegalVersion&&(identical(other.id, id) || other.id == id)&&(identical(other.documentType, documentType) || other.documentType == documentType)&&(identical(other.documentTitle, documentTitle) || other.documentTitle == documentTitle)&&(identical(other.versionNumber, versionNumber) || other.versionNumber == versionNumber)&&(identical(other.summary, summary) || other.summary == summary)&&(identical(other.body, body) || other.body == body)&&(identical(other.effectiveFrom, effectiveFrom) || other.effectiveFrom == effectiveFrom)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt)&&(identical(other.isLive, isLive) || other.isLive == isLive)&&(identical(other.supersededAt, supersededAt) || other.supersededAt == supersededAt));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,documentType,documentTitle,versionNumber,summary,body,effectiveFrom,publishedAt,isLive,supersededAt);

@override
String toString() {
  return 'LegalVersion(id: $id, documentType: $documentType, documentTitle: $documentTitle, versionNumber: $versionNumber, summary: $summary, body: $body, effectiveFrom: $effectiveFrom, publishedAt: $publishedAt, isLive: $isLive, supersededAt: $supersededAt)';
}


}

/// @nodoc
abstract mixin class _$LegalVersionCopyWith<$Res> implements $LegalVersionCopyWith<$Res> {
  factory _$LegalVersionCopyWith(_LegalVersion value, $Res Function(_LegalVersion) _then) = __$LegalVersionCopyWithImpl;
@override @useResult
$Res call({
 String id, String documentType, String documentTitle, int versionNumber, String? summary, String body, DateTime? effectiveFrom, DateTime? publishedAt, bool isLive, DateTime? supersededAt
});




}
/// @nodoc
class __$LegalVersionCopyWithImpl<$Res>
    implements _$LegalVersionCopyWith<$Res> {
  __$LegalVersionCopyWithImpl(this._self, this._then);

  final _LegalVersion _self;
  final $Res Function(_LegalVersion) _then;

/// Create a copy of LegalVersion
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? documentType = null,Object? documentTitle = null,Object? versionNumber = null,Object? summary = freezed,Object? body = null,Object? effectiveFrom = freezed,Object? publishedAt = freezed,Object? isLive = null,Object? supersededAt = freezed,}) {
  return _then(_LegalVersion(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,documentType: null == documentType ? _self.documentType : documentType // ignore: cast_nullable_to_non_nullable
as String,documentTitle: null == documentTitle ? _self.documentTitle : documentTitle // ignore: cast_nullable_to_non_nullable
as String,versionNumber: null == versionNumber ? _self.versionNumber : versionNumber // ignore: cast_nullable_to_non_nullable
as int,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,effectiveFrom: freezed == effectiveFrom ? _self.effectiveFrom : effectiveFrom // ignore: cast_nullable_to_non_nullable
as DateTime?,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,isLive: null == isLive ? _self.isLive : isLive // ignore: cast_nullable_to_non_nullable
as bool,supersededAt: freezed == supersededAt ? _self.supersededAt : supersededAt // ignore: cast_nullable_to_non_nullable
as DateTime?,
  ));
}


}

// dart format on
