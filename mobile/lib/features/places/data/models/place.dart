import 'dart:convert';

import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';

part 'place.freezed.dart';
part 'place.g.dart';

/// `GET /v1/places` ve `/v1/places/{id}` gövdesi (`PlaceResponseDto`).
///
/// ⚠️ Public uç yalnız **aktif** mekanları döndürür ve **ada göre** sıralar
/// (sunucuda başka sıralama seçeneği yok → istemcide sıralama chip'i çizilmedi;
/// sayfalar arası tutarsız olurdu).
///
/// ⚠️ [amenities] kolonu veritabanında `jsonb` ama DTO'da `string` → yanıtta
/// **JSON içeren bir metin** olarak geliyor (`"{\"WC\":true}"`), nesne olarak
/// değil. [amenityMap] bu iki şekli de çözer.
@freezed
abstract class Place with _$Place {
  const factory Place({
    required String id,
    required String categoryId,
    required String name,
    String? description,
    String? address,
    @Default(0) double latitude,
    @Default(0) double longitude,
    double? entranceFee,
    @Default(false) bool isFree,
    String? openingHours,
    String? bestSeason,
    String? howToGetThere,
    double? distanceFromCenter,

    /// Ham `jsonb` içeriği — çözümlenmiş hâli için [amenityMap].
    @JsonKey(fromJson: _rawAmenities) String? amenities,
    String? coverImageId,
    String? coverImageUrl,
    @Default(true) bool isActive,
    DateTime? createdAt,
  }) = _Place;

  const Place._();

  factory Place.fromJson(Map<String, dynamic> json) => _$PlaceFromJson(json);

  /// Koordinat gerçekten girilmiş mi (0,0 = "girilmemiş", Kadirli 37/36 civarı).
  bool get hasLocation => latitude != 0 && longitude != 0;

  /// "12,5 km" / "38 km" — merkeze uzaklık; girilmemişse null.
  ///
  /// ⚠️ `AppMoney.plain` burada kullanılamaz: para biçimi kuruş varsa **iki**
  /// hane yazıyor ("12,50 km") — mesafede tek hane doğal.
  String? get distanceLabel {
    final value = distanceFromCenter;
    if (value == null || value <= 0) return null;
    final rounded = (value * 10).round() / 10;
    final text = rounded == rounded.roundToDouble()
        ? rounded.round().toString()
        : rounded.toString().replaceAll('.', ',');
    return '$text km';
  }

  /// "Ücretsiz" / "25 ₺" — giriş ücreti bilgisi; hiçbiri yoksa null
  /// (`0 ₺` yazmak yanlış bilgi olurdu — 11.8 `AppMoney` kararı).
  String? get feeLabel {
    if (isFree) return 'Ücretsiz';
    final fee = entranceFee;
    if (fee == null || fee <= 0) return null;
    return AppMoney.amount(fee);
  }

  /// `{"WC": true, "Wi-Fi": false}` — **anahtarda olmayan olanak
  /// "belirtilmemiş"** demektir (panelin veri modeli böyle), "yok" değil.
  Map<String, bool> get amenityMap {
    final raw = amenities?.trim();
    if (raw == null || raw.isEmpty) return const {};
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map) return const {};
      return {
        for (final entry in decoded.entries)
          if (entry.value is bool) entry.key.toString(): entry.value as bool,
      };
    } catch (_) {
      // Bozuk/beklenmeyen içerik ekranı patlatmasın.
      return const {};
    }
  }

  List<String> get availableAmenities => [
    for (final entry in amenityMap.entries)
      if (entry.value) entry.key,
  ]..sort();

  List<String> get missingAmenities => [
    for (final entry in amenityMap.entries)
      if (!entry.value) entry.key,
  ]..sort();

  String shareText({String? categoryName}) {
    final lines = <String>[
      '📍 $name',
      ?categoryName,
      if ((address ?? '').trim().isNotEmpty) address!.trim(),
      if (distanceLabel != null) 'Merkeze uzaklık: $distanceLabel',
      if (openingHours != null && openingHours!.trim().isNotEmpty)
        'Saatler: ${openingHours!.trim()}',
      if (feeLabel != null) 'Giriş: $feeLabel',
      '',
      '— Kadirli uygulaması',
    ];
    return lines.join('\n');
  }
}

/// Sunucu bugün metin gönderiyor; ileride gerçek JSON nesnesine dönerse
/// model kırılmasın diye ikisi de metne indirgenir.
String? _rawAmenities(Object? value) => switch (value) {
  null => null,
  String text => text,
  _ => jsonEncode(value),
};
