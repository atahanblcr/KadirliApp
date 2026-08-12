// GENERATED CODE - DO NOT MODIFY BY HAND
// coverage:ignore-file
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'news_article.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

// dart format off
T _$identity<T>(T value) => value;

/// @nodoc
mixin _$NewsArticle {

 String get id; String get title; String get excerpt;/// Temizlenmiş HTML gövde — **yalnız detayda** dolu.
///
/// Temizlik alım anında sunucuda yapıldı (12.12, `NewsHtmlPolicy` beyaz
/// listesi). İstemci **ikinci bir beyaz liste yazmaz**: iki sahipli bir
/// güvenlik kuralı, ayrıştıkları anda hangisinin doğru olduğu
/// bilinemeyen iki gerçeklik üretir.
 String? get contentHtml;/// Aynalanmış kapak görseli — **göreli** (`/uploads/…`, §7 madde 9).
 String? get imageUrl;/// Kaynak görselinin ölçüleri; yöneticinin koyduğu kapakta **null** gelir
/// (boyutu istemci ölçer).
 int? get imageWidth; int? get imageHeight;/// Haberin gazetedeki adresi ("Kaynakta oku" + paylaşım metni).
 String? get sourceUrl; DateTime? get publishedAt; DateTime? get modifiedAt;/// Sunucuda üretilen okuma süresi (200 kelime/dk, en az 1).
///
/// ⚠️ İstemcide **hesaplanmaz**: liste ucu gövdeyi zaten taşımıyor, yani
/// hesaplanabilseydi bile listede yanlış sonuç verirdi.
 int get readingMinutes; bool get isFeatured; List<NewsCategory> get categories;
/// Create a copy of NewsArticle
/// with the given fields replaced by the non-null parameter values.
@JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
$NewsArticleCopyWith<NewsArticle> get copyWith => _$NewsArticleCopyWithImpl<NewsArticle>(this as NewsArticle, _$identity);

  /// Serializes this NewsArticle to a JSON map.
  Map<String, dynamic> toJson();


@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is NewsArticle&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.excerpt, excerpt) || other.excerpt == excerpt)&&(identical(other.contentHtml, contentHtml) || other.contentHtml == contentHtml)&&(identical(other.imageUrl, imageUrl) || other.imageUrl == imageUrl)&&(identical(other.imageWidth, imageWidth) || other.imageWidth == imageWidth)&&(identical(other.imageHeight, imageHeight) || other.imageHeight == imageHeight)&&(identical(other.sourceUrl, sourceUrl) || other.sourceUrl == sourceUrl)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt)&&(identical(other.modifiedAt, modifiedAt) || other.modifiedAt == modifiedAt)&&(identical(other.readingMinutes, readingMinutes) || other.readingMinutes == readingMinutes)&&(identical(other.isFeatured, isFeatured) || other.isFeatured == isFeatured)&&const DeepCollectionEquality().equals(other.categories, categories));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,excerpt,contentHtml,imageUrl,imageWidth,imageHeight,sourceUrl,publishedAt,modifiedAt,readingMinutes,isFeatured,const DeepCollectionEquality().hash(categories));

@override
String toString() {
  return 'NewsArticle(id: $id, title: $title, excerpt: $excerpt, contentHtml: $contentHtml, imageUrl: $imageUrl, imageWidth: $imageWidth, imageHeight: $imageHeight, sourceUrl: $sourceUrl, publishedAt: $publishedAt, modifiedAt: $modifiedAt, readingMinutes: $readingMinutes, isFeatured: $isFeatured, categories: $categories)';
}


}

/// @nodoc
abstract mixin class $NewsArticleCopyWith<$Res>  {
  factory $NewsArticleCopyWith(NewsArticle value, $Res Function(NewsArticle) _then) = _$NewsArticleCopyWithImpl;
@useResult
$Res call({
 String id, String title, String excerpt, String? contentHtml, String? imageUrl, int? imageWidth, int? imageHeight, String? sourceUrl, DateTime? publishedAt, DateTime? modifiedAt, int readingMinutes, bool isFeatured, List<NewsCategory> categories
});




}
/// @nodoc
class _$NewsArticleCopyWithImpl<$Res>
    implements $NewsArticleCopyWith<$Res> {
  _$NewsArticleCopyWithImpl(this._self, this._then);

  final NewsArticle _self;
  final $Res Function(NewsArticle) _then;

/// Create a copy of NewsArticle
/// with the given fields replaced by the non-null parameter values.
@pragma('vm:prefer-inline') @override $Res call({Object? id = null,Object? title = null,Object? excerpt = null,Object? contentHtml = freezed,Object? imageUrl = freezed,Object? imageWidth = freezed,Object? imageHeight = freezed,Object? sourceUrl = freezed,Object? publishedAt = freezed,Object? modifiedAt = freezed,Object? readingMinutes = null,Object? isFeatured = null,Object? categories = null,}) {
  return _then(_self.copyWith(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,excerpt: null == excerpt ? _self.excerpt : excerpt // ignore: cast_nullable_to_non_nullable
as String,contentHtml: freezed == contentHtml ? _self.contentHtml : contentHtml // ignore: cast_nullable_to_non_nullable
as String?,imageUrl: freezed == imageUrl ? _self.imageUrl : imageUrl // ignore: cast_nullable_to_non_nullable
as String?,imageWidth: freezed == imageWidth ? _self.imageWidth : imageWidth // ignore: cast_nullable_to_non_nullable
as int?,imageHeight: freezed == imageHeight ? _self.imageHeight : imageHeight // ignore: cast_nullable_to_non_nullable
as int?,sourceUrl: freezed == sourceUrl ? _self.sourceUrl : sourceUrl // ignore: cast_nullable_to_non_nullable
as String?,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,modifiedAt: freezed == modifiedAt ? _self.modifiedAt : modifiedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,readingMinutes: null == readingMinutes ? _self.readingMinutes : readingMinutes // ignore: cast_nullable_to_non_nullable
as int,isFeatured: null == isFeatured ? _self.isFeatured : isFeatured // ignore: cast_nullable_to_non_nullable
as bool,categories: null == categories ? _self.categories : categories // ignore: cast_nullable_to_non_nullable
as List<NewsCategory>,
  ));
}

}


/// Adds pattern-matching-related methods to [NewsArticle].
extension NewsArticlePatterns on NewsArticle {
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

@optionalTypeArgs TResult maybeMap<TResult extends Object?>(TResult Function( _NewsArticle value)?  $default,{required TResult orElse(),}){
final _that = this;
switch (_that) {
case _NewsArticle() when $default != null:
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

@optionalTypeArgs TResult map<TResult extends Object?>(TResult Function( _NewsArticle value)  $default,){
final _that = this;
switch (_that) {
case _NewsArticle():
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

@optionalTypeArgs TResult? mapOrNull<TResult extends Object?>(TResult? Function( _NewsArticle value)?  $default,){
final _that = this;
switch (_that) {
case _NewsArticle() when $default != null:
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

@optionalTypeArgs TResult maybeWhen<TResult extends Object?>(TResult Function( String id,  String title,  String excerpt,  String? contentHtml,  String? imageUrl,  int? imageWidth,  int? imageHeight,  String? sourceUrl,  DateTime? publishedAt,  DateTime? modifiedAt,  int readingMinutes,  bool isFeatured,  List<NewsCategory> categories)?  $default,{required TResult orElse(),}) {final _that = this;
switch (_that) {
case _NewsArticle() when $default != null:
return $default(_that.id,_that.title,_that.excerpt,_that.contentHtml,_that.imageUrl,_that.imageWidth,_that.imageHeight,_that.sourceUrl,_that.publishedAt,_that.modifiedAt,_that.readingMinutes,_that.isFeatured,_that.categories);case _:
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

@optionalTypeArgs TResult when<TResult extends Object?>(TResult Function( String id,  String title,  String excerpt,  String? contentHtml,  String? imageUrl,  int? imageWidth,  int? imageHeight,  String? sourceUrl,  DateTime? publishedAt,  DateTime? modifiedAt,  int readingMinutes,  bool isFeatured,  List<NewsCategory> categories)  $default,) {final _that = this;
switch (_that) {
case _NewsArticle():
return $default(_that.id,_that.title,_that.excerpt,_that.contentHtml,_that.imageUrl,_that.imageWidth,_that.imageHeight,_that.sourceUrl,_that.publishedAt,_that.modifiedAt,_that.readingMinutes,_that.isFeatured,_that.categories);case _:
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

@optionalTypeArgs TResult? whenOrNull<TResult extends Object?>(TResult? Function( String id,  String title,  String excerpt,  String? contentHtml,  String? imageUrl,  int? imageWidth,  int? imageHeight,  String? sourceUrl,  DateTime? publishedAt,  DateTime? modifiedAt,  int readingMinutes,  bool isFeatured,  List<NewsCategory> categories)?  $default,) {final _that = this;
switch (_that) {
case _NewsArticle() when $default != null:
return $default(_that.id,_that.title,_that.excerpt,_that.contentHtml,_that.imageUrl,_that.imageWidth,_that.imageHeight,_that.sourceUrl,_that.publishedAt,_that.modifiedAt,_that.readingMinutes,_that.isFeatured,_that.categories);case _:
  return null;

}
}

}

/// @nodoc
@JsonSerializable()

class _NewsArticle extends NewsArticle {
  const _NewsArticle({required this.id, this.title = '', this.excerpt = '', this.contentHtml, this.imageUrl, this.imageWidth, this.imageHeight, this.sourceUrl, this.publishedAt, this.modifiedAt, this.readingMinutes = 1, this.isFeatured = false, final  List<NewsCategory> categories = const <NewsCategory>[]}): _categories = categories,super._();
  factory _NewsArticle.fromJson(Map<String, dynamic> json) => _$NewsArticleFromJson(json);

@override final  String id;
@override@JsonKey() final  String title;
@override@JsonKey() final  String excerpt;
/// Temizlenmiş HTML gövde — **yalnız detayda** dolu.
///
/// Temizlik alım anında sunucuda yapıldı (12.12, `NewsHtmlPolicy` beyaz
/// listesi). İstemci **ikinci bir beyaz liste yazmaz**: iki sahipli bir
/// güvenlik kuralı, ayrıştıkları anda hangisinin doğru olduğu
/// bilinemeyen iki gerçeklik üretir.
@override final  String? contentHtml;
/// Aynalanmış kapak görseli — **göreli** (`/uploads/…`, §7 madde 9).
@override final  String? imageUrl;
/// Kaynak görselinin ölçüleri; yöneticinin koyduğu kapakta **null** gelir
/// (boyutu istemci ölçer).
@override final  int? imageWidth;
@override final  int? imageHeight;
/// Haberin gazetedeki adresi ("Kaynakta oku" + paylaşım metni).
@override final  String? sourceUrl;
@override final  DateTime? publishedAt;
@override final  DateTime? modifiedAt;
/// Sunucuda üretilen okuma süresi (200 kelime/dk, en az 1).
///
/// ⚠️ İstemcide **hesaplanmaz**: liste ucu gövdeyi zaten taşımıyor, yani
/// hesaplanabilseydi bile listede yanlış sonuç verirdi.
@override@JsonKey() final  int readingMinutes;
@override@JsonKey() final  bool isFeatured;
 final  List<NewsCategory> _categories;
@override@JsonKey() List<NewsCategory> get categories {
  if (_categories is EqualUnmodifiableListView) return _categories;
  // ignore: implicit_dynamic_type
  return EqualUnmodifiableListView(_categories);
}


/// Create a copy of NewsArticle
/// with the given fields replaced by the non-null parameter values.
@override @JsonKey(includeFromJson: false, includeToJson: false)
@pragma('vm:prefer-inline')
_$NewsArticleCopyWith<_NewsArticle> get copyWith => __$NewsArticleCopyWithImpl<_NewsArticle>(this, _$identity);

@override
Map<String, dynamic> toJson() {
  return _$NewsArticleToJson(this, );
}

@override
bool operator ==(Object other) {
  return identical(this, other) || (other.runtimeType == runtimeType&&other is _NewsArticle&&(identical(other.id, id) || other.id == id)&&(identical(other.title, title) || other.title == title)&&(identical(other.excerpt, excerpt) || other.excerpt == excerpt)&&(identical(other.contentHtml, contentHtml) || other.contentHtml == contentHtml)&&(identical(other.imageUrl, imageUrl) || other.imageUrl == imageUrl)&&(identical(other.imageWidth, imageWidth) || other.imageWidth == imageWidth)&&(identical(other.imageHeight, imageHeight) || other.imageHeight == imageHeight)&&(identical(other.sourceUrl, sourceUrl) || other.sourceUrl == sourceUrl)&&(identical(other.publishedAt, publishedAt) || other.publishedAt == publishedAt)&&(identical(other.modifiedAt, modifiedAt) || other.modifiedAt == modifiedAt)&&(identical(other.readingMinutes, readingMinutes) || other.readingMinutes == readingMinutes)&&(identical(other.isFeatured, isFeatured) || other.isFeatured == isFeatured)&&const DeepCollectionEquality().equals(other._categories, _categories));
}

@JsonKey(includeFromJson: false, includeToJson: false)
@override
int get hashCode => Object.hash(runtimeType,id,title,excerpt,contentHtml,imageUrl,imageWidth,imageHeight,sourceUrl,publishedAt,modifiedAt,readingMinutes,isFeatured,const DeepCollectionEquality().hash(_categories));

@override
String toString() {
  return 'NewsArticle(id: $id, title: $title, excerpt: $excerpt, contentHtml: $contentHtml, imageUrl: $imageUrl, imageWidth: $imageWidth, imageHeight: $imageHeight, sourceUrl: $sourceUrl, publishedAt: $publishedAt, modifiedAt: $modifiedAt, readingMinutes: $readingMinutes, isFeatured: $isFeatured, categories: $categories)';
}


}

/// @nodoc
abstract mixin class _$NewsArticleCopyWith<$Res> implements $NewsArticleCopyWith<$Res> {
  factory _$NewsArticleCopyWith(_NewsArticle value, $Res Function(_NewsArticle) _then) = __$NewsArticleCopyWithImpl;
@override @useResult
$Res call({
 String id, String title, String excerpt, String? contentHtml, String? imageUrl, int? imageWidth, int? imageHeight, String? sourceUrl, DateTime? publishedAt, DateTime? modifiedAt, int readingMinutes, bool isFeatured, List<NewsCategory> categories
});




}
/// @nodoc
class __$NewsArticleCopyWithImpl<$Res>
    implements _$NewsArticleCopyWith<$Res> {
  __$NewsArticleCopyWithImpl(this._self, this._then);

  final _NewsArticle _self;
  final $Res Function(_NewsArticle) _then;

/// Create a copy of NewsArticle
/// with the given fields replaced by the non-null parameter values.
@override @pragma('vm:prefer-inline') $Res call({Object? id = null,Object? title = null,Object? excerpt = null,Object? contentHtml = freezed,Object? imageUrl = freezed,Object? imageWidth = freezed,Object? imageHeight = freezed,Object? sourceUrl = freezed,Object? publishedAt = freezed,Object? modifiedAt = freezed,Object? readingMinutes = null,Object? isFeatured = null,Object? categories = null,}) {
  return _then(_NewsArticle(
id: null == id ? _self.id : id // ignore: cast_nullable_to_non_nullable
as String,title: null == title ? _self.title : title // ignore: cast_nullable_to_non_nullable
as String,excerpt: null == excerpt ? _self.excerpt : excerpt // ignore: cast_nullable_to_non_nullable
as String,contentHtml: freezed == contentHtml ? _self.contentHtml : contentHtml // ignore: cast_nullable_to_non_nullable
as String?,imageUrl: freezed == imageUrl ? _self.imageUrl : imageUrl // ignore: cast_nullable_to_non_nullable
as String?,imageWidth: freezed == imageWidth ? _self.imageWidth : imageWidth // ignore: cast_nullable_to_non_nullable
as int?,imageHeight: freezed == imageHeight ? _self.imageHeight : imageHeight // ignore: cast_nullable_to_non_nullable
as int?,sourceUrl: freezed == sourceUrl ? _self.sourceUrl : sourceUrl // ignore: cast_nullable_to_non_nullable
as String?,publishedAt: freezed == publishedAt ? _self.publishedAt : publishedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,modifiedAt: freezed == modifiedAt ? _self.modifiedAt : modifiedAt // ignore: cast_nullable_to_non_nullable
as DateTime?,readingMinutes: null == readingMinutes ? _self.readingMinutes : readingMinutes // ignore: cast_nullable_to_non_nullable
as int,isFeatured: null == isFeatured ? _self.isFeatured : isFeatured // ignore: cast_nullable_to_non_nullable
as bool,categories: null == categories ? _self._categories : categories // ignore: cast_nullable_to_non_nullable
as List<NewsCategory>,
  ));
}


}

// dart format on
