import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/phone.dart';
import '../../../core/widgets/widgets.dart';
import '../application/auth_controller.dart';
import '../application/otp_flow_controller.dart';
import 'widgets/brand_header.dart';

/// Giriş — 1. adım: telefon numarası (`POST /v1/auth/login`).
///
/// Numara **ulusal 10 hane** girilir, `+90` sabit ön ek olarak gösterilir;
/// sunucuya E.164 gider (bkz. [AppPhone]).
class PhoneLoginScreen extends ConsumerStatefulWidget {
  const PhoneLoginScreen({super.key});

  @override
  ConsumerState<PhoneLoginScreen> createState() => _PhoneLoginScreenState();
}

class _PhoneLoginScreenState extends ConsumerState<PhoneLoginScreen> {
  final _controller = TextEditingController();
  String? _localError;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final phone = AppPhone.toE164(_controller.text);
    if (phone == null) {
      setState(() => _localError = 'Numara 5 ile başlayan 10 hane olmalı.');
      return;
    }

    setState(() => _localError = null);
    FocusScope.of(context).unfocus();

    final sent = await ref.read(otpFlowProvider.notifier).requestOtp(phone);
    if (sent && mounted) context.push(AppRoutes.otpVerify);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final flow = ref.watch(otpFlowProvider);
    final notice = ref.watch(authNoticeProvider);

    return AppScaffold(
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.xxl,
          AppSpacing.xl,
          AppSpacing.xxl,
        ),
        children: [
          const BrandHeader(),
          AppSpacing.gapXl,

          if (notice != null) ...[
            InfoBanner(
              message: notice,
              onClose: () => ref.read(authNoticeProvider.notifier).clear(),
            ),
            AppSpacing.gapLg,
          ],

          Text(
            'Telefonunuzla giriş yapın',
            style: theme.textTheme.titleMedium,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXs,
          Text(
            'Numaranıza tek kullanımlık bir doğrulama kodu göndereceğiz.',
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXl,

          AppTextField(
            label: 'Telefon numarası',
            hint: '5xx xxx xx xx',
            prefixText: AppPhone.countryCode,
            prefixIcon: Icons.phone_iphone_rounded,
            controller: _controller,
            keyboardType: TextInputType.phone,
            textInputAction: TextInputAction.done,
            autofillHints: const [AutofillHints.telephoneNumberNational],
            inputFormatters: const [PhoneInputFormatter()],
            errorText: _localError ?? flow.error?.message,
            enabled: !flow.isBusy,
            onChanged: (_) {
              if (_localError != null) setState(() => _localError = null);
              if (flow.error != null) ref.read(otpFlowProvider.notifier).clearError();
            },
            onSubmitted: (_) => _submit(),
          ),
          AppSpacing.gapXl,

          AppButton(
            label: 'Kod Gönder',
            icon: Icons.sms_rounded,
            expand: true,
            loading: flow.status == OtpFlowStatus.requesting,
            onPressed: flow.isBusy ? null : _submit,
          ),
          AppSpacing.gapMd,

          AppButton.ghost(
            label: 'Misafir olarak devam et',
            expand: true,
            onPressed: flow.isBusy
                ? null
                : () async {
                    await ref.read(authControllerProvider.notifier).continueAsGuest();
                    if (context.mounted) context.go(AppRoutes.home);
                  },
          ),
          AppSpacing.gapLg,

          Text(
            'Duyuruları, nöbetçi eczaneyi ve ilanları girmeden de '
            'görebilirsiniz. Giriş yalnız ilan verme, favori ve bildirim gibi '
            'kişisel işlemler için gerekir.',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),

          if (Env.showDevTools) ...[
            AppSpacing.gapXl,
            const InfoBanner(
              tone: InfoBannerTone.warning,
              title: 'Geliştirme modu',
              message:
                  'Sunucu Otp:DevMode=true ile çalışıyorsa kod her zaman '
                  '123456 ve kod ekranında otomatik doldurulur.',
            ),
          ],
        ],
      ),
    );
  }
}
