import 'package:freezed_annotation/freezed_annotation.dart';

part 'announcement.freezed.dart';
part 'announcement.g.dart';

/// Duyuru önceliği (panel: 0 Normal / 1 Yüksek / 2 Acil).
enum AnnouncementPriority { normal, high, urgent }

/// `GET /v1/announcements` öğesi (`AnnouncementDto`).
///
/// 11.4 yalnız Ana Sayfa'daki "Öne çıkan" kartları için kullanır; 11.6 aynı
/// modeli liste + detay ekranlarında kullanacak (alanların tamamı burada).
@freezed
abstract class Announcement with _$Announcement {
  const factory Announcement({
    required String id,
    required String title,
    @Default('') String body,
    String? typeId,
    String? typeName,
    @Default(0) int priority,
    @Default('') String status,

    /// Yayına çıkış anı — liste sıralaması ve "2 saat önce" bunun üzerinden.
    DateTime? sentAt,
    DateTime? scheduledFor,
    DateTime? visibleUntil,
    DateTime? createdAt,

    /// Göreli olabilir → `AppImage.url` ile mutlaklaştırılır.
    String? imageUrl,
    String? source,
    String? sourceUrl,
    @Default(false) bool hasLink,
    String? externalLink,
    @Default(false) bool hasLocation,
    double? latitude,
    double? longitude,
    String? locationName,
  }) = _Announcement;

  const Announcement._();

  factory Announcement.fromJson(Map<String, dynamic> json) =>
      _$AnnouncementFromJson(json);

  AnnouncementPriority get priorityLevel => switch (priority) {
    >= 2 => AnnouncementPriority.urgent,
    1 => AnnouncementPriority.high,
    _ => AnnouncementPriority.normal,
  };

  /// Kart altındaki zaman damgası için en anlamlı tarih.
  DateTime? get publishedAt => sentAt ?? createdAt;
}
