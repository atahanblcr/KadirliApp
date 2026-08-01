import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../files/data/files_repository.dart';
import '../data/deaths_repository.dart';

/// Vefat bildirim formunun sunucuya gidecek alanları.
@immutable
class DeathNoticeDraft {
  const DeathNoticeDraft({
    required this.deceasedName,
    required this.funeralDate,
    required this.funeralTime,
    this.cemeteryId,
    this.mosqueId,
    this.neighborhoodId,
    this.condolenceAddress,
    this.photoPath,
  });

  final String deceasedName;

  /// Cenaze namazının günü (yalnız yıl/ay/gün kullanılır).
  final DateTime funeralDate;

  /// Cenaze namazının saati.
  final TimeOfDayValue funeralTime;

  final String? cemeteryId;
  final String? mosqueId;
  final String? neighborhoodId;
  final String? condolenceAddress;

  /// Cihazdaki fotoğraf yolu (opsiyonel).
  final String? photoPath;
}

/// `TimeOfDay`in Flutter'a bağımlı olmayan karşılığı — servis saf Dart kalsın
/// diye (test edilebilirlik).
@immutable
class TimeOfDayValue {
  const TimeOfDayValue(this.hour, this.minute);

  final int hour;
  final int minute;

  /// Sunucunun beklediği `TimeSpan` biçimi: `"13:30:00"`.
  String get apiValue =>
      '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}:00';

  /// Ekranda gösterilen biçim: `"13:30"`.
  String get label =>
      '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}';

  @override
  bool operator ==(Object other) =>
      other is TimeOfDayValue && other.hour == hour && other.minute == minute;

  @override
  int get hashCode => Object.hash(hour, minute);
}

/// Vefat bildiriminin iki adımlı işini tek yerde toplar: (varsa) fotoğraf
/// `POST /v1/files/upload` ile yüklenir, dönen dosya kimliğiyle ilan yazılır.
///
/// **Neden ayrı servis** (11.9 `AdSubmissionService` deseni): "önce dosya sonra
/// kayıt" sırası ve hata durumunda ne olacağı ekransız test edilebiliyor.
class DeathSubmissionService {
  DeathSubmissionService(this._deaths, this._files);

  final DeathsRepository _deaths;
  final FilesRepository _files;

  /// Bildirimi gönderir; dönen değer yeni kaydın kimliği (`pending`).
  Future<String> submit(DeathNoticeDraft draft) async {
    String? photoFileId;
    final path = draft.photoPath;
    if (path != null && path.isNotEmpty) {
      final uploaded = await _files.upload(filePath: path, moduleType: 'death');
      photoFileId = uploaded.id;
    }

    return _deaths.create(
      deceasedName: draft.deceasedName,
      funeralDate: draft.funeralDate,
      funeralTime: draft.funeralTime.apiValue,
      photoFileId: photoFileId,
      cemeteryId: draft.cemeteryId,
      mosqueId: draft.mosqueId,
      neighborhoodId: draft.neighborhoodId,
      condolenceAddress: draft.condolenceAddress,
    );
  }
}

final deathSubmissionServiceProvider = Provider<DeathSubmissionService>(
  (ref) => DeathSubmissionService(
    ref.watch(deathsRepositoryProvider),
    ref.watch(filesRepositoryProvider),
  ),
);
