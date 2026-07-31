import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'announcement_type.freezed.dart';
part 'announcement_type.g.dart';

/// `GET /v1/announcements/types` öğesi (`AnnouncementTypeDto`).
///
/// Sunucu **panel için** tasarlanmış iki sunum alanı taşıyor: `icon` bir
/// FontAwesome sınıfı (`fa-bolt`), `color` bir hex string (`#F59E0B`). Mobil
/// bunları körü körüne kullanamaz:
/// - FontAwesome paketi eklemek yerine sık kullanılan adlar Material karşılığına
///   eşlenir ([materialIcon]), bilinmeyen ad nötr ikona düşer.
/// - Hex renk **yalnız chip'in dolgu tonunda** kullanılır; metin/kenarlık rengi
///   temadan gelir → yönetici çok açık bir renk seçse de okunabilirlik bozulmaz
///   ve koyu temada patlamaz (MOBILE_UX_PLAN §0 "palet dışı renk yok" kuralının
///   bilinçli, sınırlı istisnası: tür ayırt ediciliği kullanıcıya değer katıyor).
@freezed
abstract class AnnouncementType with _$AnnouncementType {
  const factory AnnouncementType({
    required String id,
    required String name,
    @Default('') String slug,
    String? icon,
    String? color,
    @Default(0) int displayOrder,
  }) = _AnnouncementType;

  const AnnouncementType._();

  factory AnnouncementType.fromJson(Map<String, dynamic> json) =>
      _$AnnouncementTypeFromJson(json);

  /// `#F59E0B` / `F59E0B` / `#FF00AA55` → [Color]; bozuksa null.
  Color? get accentColor {
    final raw = color?.trim().replaceFirst('#', '');
    if (raw == null || (raw.length != 6 && raw.length != 8)) return null;
    final value = int.tryParse(raw, radix: 16);
    if (value == null) return null;
    return Color(raw.length == 6 ? 0xFF000000 | value : value);
  }

  /// FontAwesome sınıfı → Material ikonu. Eşleşme yoksa nötr etiket ikonu.
  IconData get materialIcon => switch (icon?.trim().toLowerCase()) {
    'fa-bullhorn' => Icons.campaign_rounded,
    'fa-bolt' => Icons.bolt_rounded,
    'fa-tint' || 'fa-droplet' => Icons.water_drop_rounded,
    'fa-landmark' || 'fa-building' => Icons.account_balance_rounded,
    'fa-calendar' || 'fa-calendar-days' => Icons.event_rounded,
    'fa-triangle-exclamation' || 'fa-exclamation' => Icons.warning_amber_rounded,
    'fa-road' => Icons.add_road_rounded,
    'fa-truck' => Icons.local_shipping_rounded,
    'fa-heart' => Icons.favorite_rounded,
    'fa-graduation-cap' => Icons.school_rounded,
    _ => Icons.label_rounded,
  };
}
