import 'package:freezed_annotation/freezed_annotation.dart';

part 'uploaded_file.freezed.dart';
part 'uploaded_file.g.dart';

/// `POST /v1/files/upload` yanıtı (`FileResponseDto`).
///
/// [id] ilgili create/update uçlarına verilir (profil fotoğrafı, ilan
/// görselleri); [cdnUrl] göreli olabilir → gösterirken `AppImage.url`.
@freezed
abstract class UploadedFile with _$UploadedFile {
  const factory UploadedFile({
    required String id,
    required String cdnUrl,
    @Default('') String originalName,
  }) = _UploadedFile;

  factory UploadedFile.fromJson(Map<String, dynamic> json) => _$UploadedFileFromJson(json);
}
