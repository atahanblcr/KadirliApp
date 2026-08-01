import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../../core/utils/utils.dart';

part 'campaign.freezed.dart';
part 'campaign.g.dart';

/// `GET /v1/campaigns` ve `/v1/campaigns/{id}` gövdesi (`CampaignResponseDto`).
///
/// ⚠️ Public uç **yalnız onaylı ve tarihi geçerli** kampanyaları döndürür
/// (`OnlyActive=true` controller'da sabit) → istemcinin "süresi dolmuş mu"
/// filtrelemesi gerekmez; yine de kalan gün sayısı [daysLeft] ile gösteriliyor.
///
/// ⚠️ [discountCode] gövdede geliyor ama ekranda **doğrudan gösterilmez**:
/// kod `POST /{id}/view-code` ile açılır, çünkü "kodu kaç farklı kullanıcı
/// gördü" sayacı esnafın kampanya ölçümü (10.12). Uç kodu yeniden döndürdüğü
/// için modal her zaman **sunucudan gelen** değeri gösterir.
@freezed
abstract class Campaign with _$Campaign {
  const factory Campaign({
    required String id,
    required String businessId,
    String? businessName,
    required String title,
    @Default('') String description,
    double? discountPercentage,
    String? discountCode,
    String? terms,
    required DateTime startDate,
    required DateTime endDate,
    @Default(0) int codeViewCount,
    String? coverImageId,
    String? coverImageUrl,
    @Default('approved') String status,
    DateTime? createdAt,
  }) = _Campaign;

  const Campaign._();

  factory Campaign.fromJson(Map<String, dynamic> json) =>
      _$CampaignFromJson(json);

  bool get hasCode => (discountCode ?? '').trim().isNotEmpty;

  /// "%25" — ondalık varsa korunur ("%12,5"), yoksa yazılmaz.
  String? get discountLabel {
    final value = discountPercentage;
    if (value == null || value <= 0) return null;
    return '%${AppMoney.plain(value)}';
  }

  /// Bitişe kaç gün kaldı (bugün biten kampanya 0). Gün bazında hesaplanır:
  /// saat farkı yüzünden "0 gün kaldı" yazan bir kampanya bugün hâlâ geçerli.
  int daysLeft({DateTime? now}) {
    final today = AppDate.toTurkey(now ?? DateTime.now());
    final reference = DateTime(today.year, today.month, today.day);
    final end = AppDate.toTurkey(endDate);
    return DateTime(end.year, end.month, end.day).difference(reference).inDays;
  }

  /// "Son gün!" / "3 gün kaldı" — yalnız bitişe bir hafta kalınca çıkar,
  /// yoksa her kartta aciliyet rozeti olur ve rozet anlamını yitirir.
  String? urgencyLabel({DateTime? now}) {
    final days = daysLeft(now: now);
    return switch (days) {
      < 0 => null,
      0 => 'Son gün!',
      1 => 'Son 1 gün',
      < 8 => '$days gün kaldı',
      _ => null,
    };
  }

  String get validityLabel =>
      '${AppDate.date(startDate)} – ${AppDate.date(endDate)}';
}
