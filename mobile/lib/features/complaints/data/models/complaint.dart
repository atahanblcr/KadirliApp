import 'package:flutter/material.dart';
import 'package:freezed_annotation/freezed_annotation.dart';

part 'complaint.freezed.dart';
part 'complaint.g.dart';

/// Bildirimin durumu (`complaints.status`).
///
/// Değerler admin panelinin kullandığı dört sabitle birebir
/// (`ComplaintsAdminController.UpdateStatus`): panelde işleme alınan bir kayıt
/// mobilde de "İşlemde" görünmeli.
enum ComplaintStatus {
  pending('pending', 'Bekliyor'),
  inProgress('in_progress', 'İşlemde'),
  resolved('resolved', 'Çözüldü'),
  rejected('rejected', 'Reddedildi'),
  unknown('', 'Bilinmiyor');

  const ComplaintStatus(this.apiValue, this.label);

  final String apiValue;
  final String label;

  static ComplaintStatus parse(String? raw) {
    final value = raw?.trim().toLowerCase();
    for (final status in values) {
      if (status != unknown && status.apiValue == value) return status;
    }
    return unknown;
  }

  /// Durum **her zaman metinle** verilir; renk yalnız destekler (11.9 kararı).
  String get description => switch (this) {
    pending => 'Bildiriminiz sıraya alındı, henüz incelenmedi.',
    inProgress => 'Bildiriminiz ilgili birim tarafından inceleniyor.',
    resolved => 'Bildiriminiz sonuçlandırıldı.',
    rejected => 'Bildiriminiz işleme alınmadı.',
    unknown => '',
  };

  IconData get icon => switch (this) {
    pending => Icons.schedule_rounded,
    inProgress => Icons.autorenew_rounded,
    resolved => Icons.check_circle_outline_rounded,
    rejected => Icons.cancel_outlined,
    unknown => Icons.help_outline_rounded,
  };

  bool get isClosed => this == resolved || this == rejected;
}

/// Bildirim türü (`complaints.type`).
///
/// ⚠️ Sunucu tarafında **serbest metin** — doğrulayıcı ve sözlük ucu yok.
/// Değerler mevcut veriyle uyumlu seçildi (`MockDataSeeder` `content` ve `app`
/// kullanıyor); tanınmayan değer [other]'a düşer ve ham metin ekranda yazılır,
/// yani panelden ya da eski sürümden gelen bir tür kaybolmaz.
enum ComplaintType {
  complaint('complaint', 'Şikayet', Icons.report_problem_outlined),
  request('request', 'İstek / Talep', Icons.front_hand_outlined),
  suggestion('suggestion', 'Öneri', Icons.lightbulb_outline_rounded),
  content('content', 'İçerik şikayeti', Icons.flag_outlined),
  app('app', 'Uygulama hatası', Icons.bug_report_outlined),
  other('other', 'Diğer', Icons.chat_bubble_outline_rounded);

  const ComplaintType(this.apiValue, this.label, this.icon);

  final String apiValue;
  final String label;
  final IconData icon;

  /// Formda seçilebilecek türler (sıra = kullanılma sıklığı tahmini).
  static const selectable = [complaint, request, suggestion, content, app, other];

  static ComplaintType? tryParse(String? raw) {
    final value = raw?.trim().toLowerCase();
    if (value == null || value.isEmpty) return null;
    for (final type in values) {
      if (type.apiValue == value) return type;
    }
    return null;
  }

  /// Türe göre mesaj alanının ipucu metni — boş bir kutuya "ne yazayım"
  /// diye bakan kullanıcıya somut örnek verir.
  String get messageHint => switch (this) {
    complaint => 'Örn. Yenimahalle 1234. sokakta çöpler üç gündür alınmıyor.',
    request => 'Örn. Cumhuriyet Meydanı’na daha fazla bank konulmasını istiyorum.',
    suggestion => 'Örn. Uygulamaya pazar yeri günlerinin eklenmesi faydalı olur.',
    content => 'Şikayet ettiğiniz içeriği ve sebebini kısaca yazın.',
    app => 'Hatayı ne yaparken aldığınızı ve ekranda ne gördüğünüzü yazın.',
    other => 'Konuyu kısaca ve açık şekilde anlatın.',
  };
}

/// `GET /v1/complaints/my` satırı (`ComplaintResponseDto`).
///
/// ⚠️ Anonim gönderilen bildirimlerde `userId` NULL kalır → bu listede
/// **hiç görünmezler**; form ekranı bunu kullanıcıya önceden söyler.
///
/// ⚠️ `userName` yalnız admin sorgusunda dolduruluyor; "benim" listemde
/// gereksiz olduğu için modele de alınmadı.
@freezed
abstract class Complaint with _$Complaint {
  const factory Complaint({
    required String id,
    String? type,
    String? relatedModule,
    String? relatedId,
    @Default('') String subject,
    @Default('') String message,
    @Default('pending') String status,
    String? adminNotes,
    DateTime? resolvedAt,
    required DateTime createdAt,
  }) = _Complaint;

  const Complaint._();

  factory Complaint.fromJson(Map<String, dynamic> json) =>
      _$ComplaintFromJson(json);

  ComplaintStatus get statusValue => ComplaintStatus.parse(status);

  ComplaintType? get typeValue => ComplaintType.tryParse(type);

  /// Bilinen tür varsa Türkçe etiketi, yoksa sunucudaki ham değeri
  /// (panelden girilmiş bir tür sessizce kaybolmasın).
  String? get typeLabel {
    final known = typeValue;
    if (known != null) return known.label;
    final raw = type?.trim();
    return (raw == null || raw.isEmpty) ? null : raw;
  }

  /// Yönetici notu = kullanıcının beklediği **cevap**; kartta öne çıkar.
  String? get answer {
    final value = adminNotes?.trim();
    return (value == null || value.isEmpty) ? null : value;
  }

  bool get hasAnswer => answer != null;
}
