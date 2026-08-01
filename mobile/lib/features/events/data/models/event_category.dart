import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'event_category.freezed.dart';
part 'event_category.g.dart';

/// `GET /v1/events/categories` öğesi (lookup: `{id, name, slug}`).
///
/// Rehber kategorilerindeki gibi ikon **slug'dan** türetiliyor: sunucu ikon
/// alanı taşımıyor ve yeni bir ikon paketi eklemek istemiyoruz.
@freezed
abstract class EventCategory with _$EventCategory {
  const factory EventCategory({
    required String id,
    required String name,
    @Default('') String slug,
  }) = _EventCategory;

  const EventCategory._();

  factory EventCategory.fromJson(Map<String, dynamic> json) =>
      _$EventCategoryFromJson(json);

  IconData get materialIcon => switch (slug) {
    'konser' => Icons.music_note_rounded,
    'festival' => Icons.festival_rounded,
    'tiyatro' => Icons.theater_comedy_rounded,
    'sergi' => Icons.palette_rounded,
    'spor' => Icons.sports_soccer_rounded,
    'seminer' || 'konferans' => Icons.record_voice_over_rounded,
    'sinema' => Icons.movie_rounded,
    _ => Icons.local_activity_rounded,
  };
}
