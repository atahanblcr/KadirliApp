import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../data/auth_repository.dart';
import '../data/models/otp_challenge.dart';
import 'auth_controller.dart';

enum OtpFlowStatus {
  /// Telefon girişi bekleniyor.
  idle,

  /// `POST /v1/auth/login` uçuşta.
  requesting,

  /// Kod gönderildi, 6 hane bekleniyor.
  awaitingCode,

  /// `POST /v1/auth/verify-otp` uçuşta.
  verifying,
}

/// Telefon → OTP akışının ekran durumu.
class OtpFlowState {
  const OtpFlowState({
    this.phoneE164,
    this.challenge,
    this.status = OtpFlowStatus.idle,
    this.error,
    this.resendAvailableAt,
  });

  /// Kodun gönderildiği numara (E.164) — doğrulamada aynısı gönderilmeli.
  final String? phoneE164;
  final OtpChallenge? challenge;
  final OtpFlowStatus status;

  /// Son işlemin hatası (kullanıcıya gösterilir); yeni denemede temizlenir.
  final ApiException? error;

  /// "Tekrar gönder" bu ana kadar kilitli (sunucu `retryAfter` = 60 sn).
  final DateTime? resendAvailableAt;

  bool get isBusy =>
      status == OtpFlowStatus.requesting || status == OtpFlowStatus.verifying;

  bool get codeSent =>
      status == OtpFlowStatus.awaitingCode || status == OtpFlowStatus.verifying;

  /// Dev modda sunucu kodu yanıtta döndürür → alan otomatik doldurulabilir.
  String? get devOtp => challenge?.hasDevOtp == true ? challenge!.otp : null;

  Duration get resendRemaining {
    final until = resendAvailableAt;
    if (until == null) return Duration.zero;
    final remaining = until.difference(DateTime.now());
    return remaining.isNegative ? Duration.zero : remaining;
  }

  bool get canResend => !isBusy && resendRemaining == Duration.zero;

  OtpFlowState copyWith({
    String? phoneE164,
    OtpChallenge? challenge,
    OtpFlowStatus? status,
    ApiException? error,
    DateTime? resendAvailableAt,
    bool clearError = false,
  }) => OtpFlowState(
    phoneE164: phoneE164 ?? this.phoneE164,
    challenge: challenge ?? this.challenge,
    status: status ?? this.status,
    error: clearError ? null : (error ?? this.error),
    resendAvailableAt: resendAvailableAt ?? this.resendAvailableAt,
  );
}

/// Giriş ekranlarının (telefon + kod) ortak denetleyicisi.
///
/// Sunucuya gitmeyen hiçbir doğrulama burada tekrarlanmaz; yalnız akış durumu
/// tutulur. Oturum açma işini [AuthController] yapar — bu sınıf `verify`
/// başarılı olunca ona devreder.
final otpFlowProvider = NotifierProvider<OtpFlowController, OtpFlowState>(
  OtpFlowController.new,
);

class OtpFlowController extends Notifier<OtpFlowState> {
  @override
  OtpFlowState build() => const OtpFlowState();

  AuthRepository get _repository => ref.read(authRepositoryProvider);

  /// Kod ister. [phoneE164] `AppPhone.toE164` çıktısı olmalı.
  ///
  /// ⚠️ Auth uçları IP başına **5 istek/dk** ile sınırlı (API_CONTRACT §8) →
  /// `RATE_LIMITED` normal bir sonuç, hata mesajı olarak gösterilir.
  Future<bool> requestOtp(String phoneE164) async {
    state = state.copyWith(
      phoneE164: phoneE164,
      status: OtpFlowStatus.requesting,
      clearError: true,
    );

    try {
      final challenge = await _repository.requestOtp(phoneE164);
      state = OtpFlowState(
        phoneE164: phoneE164,
        challenge: challenge,
        status: OtpFlowStatus.awaitingCode,
        resendAvailableAt: DateTime.now().add(challenge.resendCooldown),
      );
      return true;
    } on ApiException catch (error) {
      state = state.copyWith(
        status: state.codeSent ? OtpFlowStatus.awaitingCode : OtpFlowStatus.idle,
        error: error,
      );
      return false;
    }
  }

  /// Aynı numaraya yeniden kod gönderir (geri sayım bitmişse).
  Future<bool> resend() async {
    final phone = state.phoneE164;
    if (phone == null || !state.canResend) return false;
    return requestOtp(phone);
  }

  /// Kodu doğrular. Başarılıysa oturum açılmış ya da kayıt akışına geçilmiştir
  /// (yönlendirmeyi router yapar).
  Future<bool> verify(String otp) async {
    final phone = state.phoneE164;
    if (phone == null) return false;

    state = state.copyWith(status: OtpFlowStatus.verifying, clearError: true);

    try {
      final result = await _repository.verifyOtp(phoneE164: phone, otp: otp);
      await ref
          .read(authControllerProvider.notifier)
          .completeOtpVerification(phoneE164: phone, result: result);
      state = const OtpFlowState(); // akış bitti, durum sıfırlanır
      return true;
    } on ApiException catch (error) {
      state = state.copyWith(status: OtpFlowStatus.awaitingCode, error: error);
      return false;
    }
  }

  /// "Numarayı değiştir" — telefon adımına döner.
  void changePhone() => state = const OtpFlowState();

  void clearError() => state = state.copyWith(clearError: true);
}
