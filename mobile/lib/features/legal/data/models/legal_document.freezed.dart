// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'legal_document.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$LegalDocument {

 String get id;/// ⚠️ **Kontrat** — `kvkk_aydinlatma` · `acik_riza` · `kullanim_kosullari` ·
/// `gizlilik_politikasi` · `ticari_ileti`. Tanınmayan tür sunucuda
/// **varsayılana düşmez, 404 olur**; istemci de bu değerleri yalnız
/// **taşır**, yorumlamaz.
 String get type; String get title;/// 🔴 Rızanın bağlanacağı kimlik.
 String get versionId; int get versionNumber;/// Onay kutusunun yanındaki tek cümle (boş olabilir → başlık kullanılır).
 String? get summary;/// Metnin kendisi (HTML).
 String get body;/// 🔴 `true` ise bu kutu işaretlenmeden kayıt tamamlanmaz.
 bool get isMandatory;/// Kayıt ekranında sorulsun mu (ayarlar ekranı hepsini gösterir).
 bool get showAtRegistration; int get sortOrder; DateTime? get effectiveFrom;/// Bu sürüm yeniden onay gerektiriyor mu (yeniden onay ekranı bunu okur).
 bool get requiresReconsent;
/// Create a copy of LegalDocument
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$LegalDocumentCopyWith<LegalDocument> get copyWith => _$LegalDocumentCopyWithImpl<LegalDocument>(this as LegalDocument, _$identity);

  /// Serializes this LegalDocument to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is LegalDocument&&(identical(other.id, id) || other.id == id)&&(identical(other.type, type) || other.type == type)&&(identical(other.title, title) || other.title == title)&&(identical(other.versionId, versionId) || other.versionId == versionId)&&(identical(other.versionNumber, versionNumber) || other.versionNumber == versionNumber)&&(identical(other.summary, summary) || other.summary == summary)&&(identical(other.body, body) || other.body == body)&&(identical(other.isMandatory, isMandatory) || other.isMandatory == isMandatory)&&(identical(other.showAtRegistration, showAtRegistration) || other.showAtRegistration == showAtRegistration)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.effectiveFrom, effectiveFrom) || other.effectiveFrom == effectiveFrom)&&(identical(other.requiresReconsent, requiresReconsent) || other.requiresReconsent == requiresReconsent));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,type,title,versionId,versionNumber,summary,body,isMandatory,showAtRegistration,sortOrder,effectiveFrom,requiresReconsent);

@override
String toString() {
  return 'LegalDocument(id: $id, type: $type, title: $title, versionId: $versionId, versionNumber: $versionNumber, summary: $summary, body: $body, isMandatory: $isMandatory, showAtRegistration: $showAtRegistration, sortOrder: $sortOrder, effectiveFrom: $effectiveFrom, requiresReconsent: $requiresReconsent)';
}


}

/// @nodoc
abstract mixin class $LegalDocumentCopyWith<$Res>  {
  factory $LegalDocumentCopyWith(LegalDocument value, $Res Function(LegalDocument) _then) = _$LegalDocumentCopyWithImpl;
@useResult
$Res call({
 String id, String type, String title, String versionId, int versionNumber, String? summary, String body, bool isMandatory, bool showAtRegistration, int sortOrder, DateTime? effectiveFrom, bool requiresReconsent
});




}
/// @nodoc
class _$LegalDocumentCopyWithImpl<$Res>
    implements $LegalDocumentCopyWith<$Res> {
  _$LegalDocumentCopyWithImpl(this._self, this._then);

  final LegalDocument _self;
  final $Res Function(LegalDocument) _then;

/// Create a copy of LegalDocument
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? type = null,Object? title = null,Object? versionId = null,Object? versionNumber = null,Object? summary = freezed,Object? body = null,Object? isMandatory = null,Object? showAtRegistration = null,Object? sortOrder = null,Object? effectiveFrom = freezed,Object? requiresReconsent = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,versionId: null == versionId ? _self.versionId : versionId // ignore: cast_nullable_to_non_nullable
as String,versionNumber: null == versionNumber ? _self.versionNumber : versionNumber // ignore: cast_nullable_to_non_nullable
as int,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,isMandatory: null == isMandatory ? _self.isMandatory : isMandatory // ignore: cast_nullable_to_non_nullable
as bool,showAtRegistration: null == showAtRegistration ? _self.showAtRegistration : showAtRegistration // ignore: cast_nullable_to_non_nullable
as bool,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,effectiveFrom: freezed == effectiveFrom ? _self.effectiveFrom : effectiveFrom // ignore: cast_nullable_to_non_nullable
as DateTime?,requiresReconsent: null == requiresReconsent ? _self.requiresReconsent : requiresReconsent // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}

}


/// Adds pattern-matching-related methods to [LegalDocument].
extension LegalDocumentPatterns on LegalDocument {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _LegalDocument value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _LegalDocument() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _LegalDocument value)  $default,){
final _that = this;
switch (_that) {
case _LegalDocument():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _LegalDocument value)?  $default,){
final _that = this;
switch (_that) {
case _LegalDocument() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String type,  String title,  String versionId,  int versionNumber,  String? summary,  String body,  bool isMandatory,  bool showAtRegistration,  int sortOrder,  DateTime? effectiveFrom,  bool requiresReconsent)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _LegalDocument() when $default != null:
return $default(_that.id,_that.type,_that.title,_that.versionId,_that.versionNumber,_that.summary,_that.body,_that.isMandatory,_that.showAtRegistration,_that.sortOrder,_that.effectiveFrom,_that.requiresReconsent);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String type,  String title,  String versionId,  int versionNumber,  String? summary,  String body,  bool isMandatory,  bool showAtRegistration,  int sortOrder,  DateTime? effectiveFrom,  bool requiresReconsent)  $default,) {final _that = this;
switch (_that) {
case _LegalDocument():
return $default(_that.id,_that.type,_that.title,_that.versionId,_that.versionNumber,_that.summary,_that.body,_that.isMandatory,_that.showAtRegistration,_that.sortOrder,_that.effectiveFrom,_that.requiresReconsent);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String type,  String title,  String versionId,  int versionNumber,  String? summary,  String body,  bool isMandatory,  bool showAtRegistration,  int sortOrder,  DateTime? effectiveFrom,  bool requiresReconsent)?  $default,) {final _that = this;
switch (_that) {
case _LegalDocument() when $default != null:
return $default(_that.id,_that.type,_that.title,_that.versionId,_that.versionNumber,_that.summary,_that.body,_that.isMandatory,_that.showAtRegistration,_that.sortOrder,_that.effectiveFrom,_that.requiresReconsent);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _LegalDocument extends LegalDocument {
  const _LegalDocument({required this.id, required this.type, required this.title, required this.versionId, this.versionNumber = 1, this.summary, this.body = '', this.isMandatory = false, this.showAtRegistration = false, this.sortOrder = 0, this.effectiveFrom, this.requiresReconsent = false}): super._();
  factory _LegalDocument.fromJson(Map<String, dynamic> json) => _$LegalDocumentFromJson(json);

@override final  String id;
/// ⚠️ **Kontrat** — `kvkk_aydinlatma` · `acik_riza` · `kullanim_kosullari` ·
/// `gizlilik_politikasi` · `ticari_ileti`. Tanınmayan tür sunucuda
/// **varsayılana düşmez, 404 olur**; istemci de bu değerleri yalnız
/// **taşır**, yorumlamaz.
@override final  String type;
@override final  String title;
/// 🔴 Rızanın bağlanacağı kimlik.
@override final  String versionId;
@override@JsonKey() final  int versionNumber;
/// Onay kutusunun yanındaki tek cümle (boş olabilir → başlık kullanılır).
@override final  String? summary;
/// Metnin kendisi (HTML).
@override@JsonKey() final  String body;
/// 🔴 `true` ise bu kutu işaretlenmeden kayıt tamamlanmaz.
@override@JsonKey() final  bool isMandatory;
/// Kayıt ekranında sorulsun mu (ayarlar ekranı hepsini gösterir).
@override@JsonKey() final  bool showAtRegistration;
@override@JsonKey() final  int sortOrder;
@override final  DateTime? effectiveFrom;
/// Bu sürüm yeniden onay gerektiriyor mu (yeniden onay ekranı bunu okur).
@override@JsonKey() final  bool requiresReconsent;

/// Create a copy of LegalDocument
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$LegalDocumentCopyWith<_LegalDocument> get copyWith => __$LegalDocumentCopyWithImpl<_LegalDocument>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$LegalDocumentToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _LegalDocument&&(identical(other.id, id) || other.id == id)&&(identical(other.type, type) || other.type == type)&&(identical(other.title, title) || other.title == title)&&(identical(other.versionId, versionId) || other.versionId == versionId)&&(identical(other.versionNumber, versionNumber) || other.versionNumber == versionNumber)&&(identical(other.summary, summary) || other.summary == summary)&&(identical(other.body, body) || other.body == body)&&(identical(other.isMandatory, isMandatory) || other.isMandatory == isMandatory)&&(identical(other.showAtRegistration, showAtRegistration) || other.showAtRegistration == showAtRegistration)&&(identical(other.sortOrder, sortOrder) || other.sortOrder == sortOrder)&&(identical(other.effectiveFrom, effectiveFrom) || other.effectiveFrom == effectiveFrom)&&(identical(other.requiresReconsent, requiresReconsent) || other.requiresReconsent == requiresReconsent));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,type,title,versionId,versionNumber,summary,body,isMandatory,showAtRegistration,sortOrder,effectiveFrom,requiresReconsent);

@override
String toString() {
  return 'LegalDocument(id: $id, type: $type, title: $title, versionId: $versionId, versionNumber: $versionNumber, summary: $summary, body: $body, isMandatory: $isMandatory, showAtRegistration: $showAtRegistration, sortOrder: $sortOrder, effectiveFrom: $effectiveFrom, requiresReconsent: $requiresReconsent)';
}


}

/// @nodoc
abstract mixin class _$LegalDocumentCopyWith<$Res> implements $LegalDocumentCopyWith<$Res> {
  factory _$LegalDocumentCopyWith(_LegalDocument value, $Res Function(_LegalDocument) _then) = __$LegalDocumentCopyWithImpl;
@override @useResult
$Res call({
 String id, String type, String title, String versionId, int versionNumber, String? summary, String body, bool isMandatory, bool showAtRegistration, int sortOrder, DateTime? effectiveFrom, bool requiresReconsent
});




}
/// @nodoc
class __$LegalDocumentCopyWithImpl<$Res>
    implements _$LegalDocumentCopyWith<$Res> {
  __$LegalDocumentCopyWithImpl(this._self, this._then);

  final _LegalDocument _self;
  final $Res Function(_LegalDocument) _then;

/// Create a copy of LegalDocument
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? type = null,Object? title = null,Object? versionId = null,Object? versionNumber = null,Object? summary = freezed,Object? body = null,Object? isMandatory = null,Object? showAtRegistration = null,Object? sortOrder = null,Object? effectiveFrom = freezed,Object? requiresReconsent = null,}) {
  return _then(_LegalDocument(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,type: null == type ? _self.type : type // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,versionId: null == versionId ? _self.versionId : versionId // ignore: cast_nullable_to_non_nullable
as String,versionNumber: null == versionNumber ? _self.versionNumber : versionNumber // ignore: cast_nullable_to_non_nullable
as int,summary: freezed == summary ? _self.summary : summary // ignore: cast_nullable_to_non_nullable
as String?,body: null == body ? _self.body : body // ignore: cast_nullable_to_non_nullable
as String,isMandatory: null == isMandatory ? _self.isMandatory : isMandatory // ignore: cast_nullable_to_non_nullable
as bool,showAtRegistration: null == showAtRegistration ? _self.showAtRegistration : showAtRegistration // ignore: cast_nullable_to_non_nullable
as bool,sortOrder: null == sortOrder ? _self.sortOrder : sortOrder // ignore: cast_nullable_to_non_nullable
as int,effectiveFrom: freezed == effectiveFrom ? _self.effectiveFrom : effectiveFrom // ignore: cast_nullable_to_non_nullable
as DateTime?,requiresReconsent: null == requiresReconsent ? _self.requiresReconsent : requiresReconsent // ignore: cast_nullable_to_non_nullable
as bool,
  ));
}


}

// dart format on
