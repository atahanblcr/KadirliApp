import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../core/preferences/app_preferences.dart' show sharedPreferencesProvider;

/// İlan verme formunun **taslağı**.
///
/// ⭐ Plan dışı (11.9): ilan verme, uygulamanın en uzun formu — kategori,
/// başlık, açıklama, fiyat, kategoriye özel alanlar. Kullanıcı telefonu
/// kilitlerse / bir aramaya cevap verirse / yanlışlıkla geri tuşuna basarsa
/// yazdıklarının kaybolması en sinir bozucu davranış olurdu. Taslak yerelde
/// saklanır ve form yeniden açıldığında **sorularak** geri yüklenir.
///
/// **Ne zaman yazılıyor** (12.23'te düzeltildi — bu liste ÖNCEDEN yanlıştı,
/// yorum *"her değişiklikte"* diyordu): kategori seçiminde · adım
/// geçişlerinde · geri tuşu diyaloğunda *"Taslağı sakla"* denince · ve
/// **uygulama arka plana alınırken** (`didChangeAppLifecycleState`,
/// `ad_form_screen.dart`). Sonuncusu 12.23'e kadar **yoktu**, yani yukarıda
/// sayılan iki senaryonun (*telefon kilitlendi* / *arama geldi*) hiçbiri
/// gerçekte kapsanmıyordu — kapsanan tek şey geri tuşuydu.
/// ⚠️ Her tuş vuruşunda **yazılmıyor**: her karakterde platform kanalına
/// gitmenin bedeli ölçülmedi, kazancı da (arka plan kancası varken) küçük.
///
/// **Görseller taslağa YAZILMAZ**: `image_picker` yolları geçici önbellekte
/// duruyor (uygulama kapanınca silinebilir) ve olmayan bir dosyayı "seçili"
/// göstermek yalan olur.
@immutable
class AdDraft {
  const AdDraft({
    this.categoryId,
    this.rootCategoryId,
    this.categoryName,
    this.title = '',
    this.description = '',
    this.price = '',
    this.sellerName = '',
    this.contactPhone = '',
    this.propertyValues = const {},
    this.savedAt,
  });

  final String? categoryId;
  final String? rootCategoryId;
  final String? categoryName;
  final String title;
  final String description;
  final String price;
  final String sellerName;
  final String contactPhone;
  final Map<String, String> propertyValues;
  final DateTime? savedAt;

  /// Boş bir taslağı geri yüklemeyi teklif etmek anlamsız — kullanıcı yalnız
  /// ekranı açıp kapatmış olabilir.
  bool get isMeaningful =>
      title.trim().isNotEmpty ||
      description.trim().isNotEmpty ||
      categoryId != null;

  Map<String, dynamic> toJson() => {
    'categoryId': categoryId,
    'rootCategoryId': rootCategoryId,
    'categoryName': categoryName,
    'title': title,
    'description': description,
    'price': price,
    'sellerName': sellerName,
    'contactPhone': contactPhone,
    'propertyValues': propertyValues,
    'savedAt': (savedAt ?? DateTime.now()).toUtc().toIso8601String(),
  };

  static AdDraft? fromJson(Map<String, dynamic> json) {
    final rawProperties = json['propertyValues'];
    return AdDraft(
      categoryId: json['categoryId'] as String?,
      rootCategoryId: json['rootCategoryId'] as String?,
      categoryName: json['categoryName'] as String?,
      title: (json['title'] as String?) ?? '',
      description: (json['description'] as String?) ?? '',
      price: (json['price'] as String?) ?? '',
      sellerName: (json['sellerName'] as String?) ?? '',
      contactPhone: (json['contactPhone'] as String?) ?? '',
      propertyValues: rawProperties is Map
          ? {
              for (final entry in rawProperties.entries)
                entry.key.toString(): entry.value.toString(),
            }
          : const {},
      savedAt: DateTime.tryParse((json['savedAt'] as String?) ?? ''),
    );
  }
}

/// Taslağı yerelde saklar. Tek kullanıcı / tek taslak: ilan verme aynı anda
/// tek yerde açılıyor, çoklu taslak kuyruğu bu ölçekte gereksiz karmaşa.
class AdDraftStore {
  AdDraftStore(this._prefs);

  static const _key = 'ads.draft';

  /// Bundan eski taslak teklif edilmez (kullanıcı çoktan unutmuştur ve
  /// kategori/fiyat bilgisi bayatlamıştır).
  static const maxAge = Duration(days: 7);

  final SharedPreferences _prefs;

  Future<void> save(AdDraft draft) =>
      _prefs.setString(_key, jsonEncode(draft.toJson()));

  Future<void> clear() => _prefs.remove(_key);

  AdDraft? read() {
    final raw = _prefs.getString(_key);
    if (raw == null || raw.isEmpty) return null;
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map) return null;
      final draft = AdDraft.fromJson(Map<String, dynamic>.from(decoded));
      if (draft == null || !draft.isMeaningful) return null;
      final savedAt = draft.savedAt;
      if (savedAt != null && DateTime.now().toUtc().difference(savedAt) > maxAge) {
        return null;
      }
      return draft;
    } catch (_) {
      // Bozuk taslak kullanıcının işini engellememeli — sessizce yok sayılır.
      return null;
    }
  }
}

final adDraftStoreProvider = Provider<AdDraftStore>(
  (ref) => AdDraftStore(ref.watch(sharedPreferencesProvider)),
);
