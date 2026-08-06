import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/phone.dart';
import '../../../core/widgets/widgets.dart';
import '../application/otp_flow_controller.dart';

/// Giriş — 2. adım: 6 haneli kod (`POST /v1/auth/verify-otp`).
///
/// Kod tamamlanınca **otomatik doğrulanır** (kullanıcı ayrıca butona basmak
/// zorunda değil); buton yine duruyor çünkü hata sonrası tekrar denemek gerekir.
class OtpVerifyScreen extends ConsumerStatefulWidget {
  const OtpVerifyScreen({super.key});

  static const codeLength = 6;

  @override
  ConsumerState<OtpVerifyScreen> createState() => _OtpVerifyScreenState();
}

class _OtpVerifyScreenState extends ConsumerState<OtpVerifyScreen> {
  final _controller = TextEditingController();
  Timer? _ticker;
  bool _devCodeFilled = false;

  @override
  void initState() {
    super.initState();
    // Geri sayım (tekrar gönder kilidi) saniye saniye tazelenir.
    _ticker = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _ticker?.cancel();
    _controller.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    final code = _controller.text.trim();
    if (code.length != OtpVerifyScreen.codeLength) return;
    FocusScope.of(context).unfocus();
    final ok = await ref.read(otpFlowProvider.notifier).verify(code);
    if (!mounted) return;

    // Hata: kod alanı temizlenir ki kullanıcı baştan yazsın.
    if (!ok) {
      _controller.clear();
      return;
    }

    // Başarı: yönlendirme kararını router verdi (durum değişti) — ama bu ekran
    // `context.push` ile yığının ÜSTÜNE bindiği için redirect'in değiştirdiği
    // konum altta kalıyor ve kullanıcı boşalmış kod ekranında sıkışıyordu
    // (30 Tem 2026 canlı testinde kayıtlı kullanıcı girişinde yakalandı).
    // Ekranı bir kare sonra yığından çekiyoruz; nereye gidileceğine yine
    // router karar veriyor.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted && context.canPop()) context.pop();
    });
  }

  void _changePhone() {
    // ⚠️ **Sıra bilinçli: ÖNCE yığından çık, SONRA durumu değiştir.**
    //
    // `changePhone()` `codeSent`'i sıfırlıyor ve bu router'ı **senkron** olarak
    // uyandırıyor; router da `/giris/kod` için "kod gönderilmemişse telefon adımına
    // dön" redirect'ini koşuyor. Ters sırada yazıldığında o redirect, bu ekran hâlâ
    // `push` ile yığının üstündeyken çalışıyordu — projenin kendi kod-dışı
    // sözleşmesinin (`ARCHITECTURE.md` §7: "`context.push` ile açılan ekran router
    // redirect'inin ÜSTÜNDE kalır") tam olarak uyardığı durum.
    //
    // 📌 Bu değişiklik 12.2'de bulunan `_debugCheckDuplicatedPageKeys` çökmesinin
    // **kanıtlanmış** çözümü DEĞİL — çökme widget testinde yeniden üretilemedi
    // (bkz. Progress.md 12.2b). Kendi başına doğru olduğu için yapıldı; çökme
    // tekrar ederse `error_logs` yine yakalayacak.
    if (context.canPop()) {
      context.pop();
    }
    ref.read(otpFlowProvider.notifier).changePhone();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final flow = ref.watch(otpFlowProvider);

    // Dev modda sunucu kodu yanıtta döndürüyor → alanı bir kez doldur.
    final devOtp = flow.devOtp;
    if (Env.showDevTools && devOtp != null && !_devCodeFilled && _controller.text.isEmpty) {
      _devCodeFilled = true;
      _controller.text = devOtp;
    }

    final remaining = flow.resendRemaining;

    return AppScaffold(
      title: 'Doğrulama kodu',
      leading: BackButton(onPressed: _changePhone),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.xxl,
        ),
        children: [
          Text(
            '${AppPhone.masked(flow.phoneE164 ?? '')} numarasına 6 haneli kodu '
            'gönderdik.',
            style: theme.textTheme.bodyLarge,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXl,

          AppTextField(
            hint: '••••••',
            controller: _controller,
            keyboardType: TextInputType.number,
            textInputAction: TextInputAction.done,
            autofocus: true,
            textAlign: TextAlign.center,
            letterSpacing: 10,
            maxLength: OtpVerifyScreen.codeLength,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            autofillHints: const [AutofillHints.oneTimeCode],
            enabled: !flow.isBusy,
            errorText: flow.error?.message,
            onChanged: (value) {
              if (value.length == OtpVerifyScreen.codeLength) {
                _verify();
              } else if (flow.error != null) {
                ref.read(otpFlowProvider.notifier).clearError();
              }
              setState(() {});
            },
            onSubmitted: (_) => _verify(),
          ),
          AppSpacing.gapXl,

          AppButton(
            label: 'Doğrula',
            icon: Icons.verified_rounded,
            expand: true,
            loading: flow.status == OtpFlowStatus.verifying,
            onPressed:
                flow.isBusy || _controller.text.length != OtpVerifyScreen.codeLength
                ? null
                : _verify,
          ),
          AppSpacing.gapLg,

          Center(
            child: remaining > Duration.zero
                ? Text(
                    'Kodu tekrar göndermek için ${remaining.inSeconds} sn',
                    style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                  )
                : TextButton.icon(
                    onPressed: flow.isBusy
                        ? null
                        : () async {
                            _controller.clear();
                            _devCodeFilled = false;
                            await ref.read(otpFlowProvider.notifier).resend();
                          },
                    icon: const Icon(Icons.refresh_rounded, size: 18),
                    label: const Text('Kodu tekrar gönder'),
                  ),
          ),
          AppSpacing.gapSm,

          Center(
            child: TextButton(
              onPressed: flow.isBusy ? null : _changePhone,
              child: const Text('Numarayı değiştir'),
            ),
          ),

          if (Env.showDevTools && devOtp != null) ...[
            AppSpacing.gapLg,
            InfoBanner(
              tone: InfoBannerTone.warning,
              title: 'Geliştirme modu',
              message: 'Sunucudan gelen kod: $devOtp (alan otomatik dolduruldu).',
            ),
          ],
        ],
      ),
    );
  }
}
