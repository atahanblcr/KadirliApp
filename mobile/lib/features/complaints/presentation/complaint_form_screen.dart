import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/navigation/app_modules.dart';
import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../application/complaints_providers.dart';
import '../data/complaints_repository.dart';
import '../data/models/complaint.dart';

/// Şikayet / istek gönderme formu (`POST /v1/complaints`).
///
/// **KARAR — anonim gönderim açık:** uç `[AllowAnonymous]` ve bu bilinçli bir
/// tasarım (10.7); belediyeye "çöp alınmadı" demek için hesap açmak zorunda
/// kalmak bildirimi engeller. Ama anonim kayıtta `user_id` NULL kalır ve
/// **"Bildirimlerim"de hiç görünmez** → kullanıcıya bu **önceden** söylenir,
/// gönderdikten sonra değil.
///
/// Sunucuda bu uç için doğrulayıcı **yok** (11.11 vefat formuyla aynı durum)
/// → zorunlu alan denetimi tamamen istemcide.
class ComplaintFormScreen extends ConsumerStatefulWidget {
  const ComplaintFormScreen({
    super.key,
    this.initialType,
    this.relatedModule,
    this.relatedId,
    this.relatedTitle,
  });

  /// Başka bir ekrandan gelen ön seçim (ör. ilan detayındaki "Şikayet et").
  final String? initialType;
  final String? relatedModule;
  final String? relatedId;

  /// İlgili içeriğin insan tarafından okunabilir adı — ham kimlik gösterilmez.
  final String? relatedTitle;

  @override
  ConsumerState<ComplaintFormScreen> createState() =>
      _ComplaintFormScreenState();
}

class _ComplaintFormScreenState extends ConsumerState<ComplaintFormScreen> {
  final _subject = TextEditingController();
  final _message = TextEditingController();
  final _scrollController = ScrollController();

  late ComplaintType _type;

  /// Türü "içerik şikayeti" ise ilgili modül (yönetici hangi bölüme bakacağını
  /// bilsin diye; panelde "Modül: ads" satırı zaten gösteriliyor).
  String? _module;

  String? _subjectError;
  String? _messageError;
  String? _generalError;
  bool _sending = false;

  static const _minMessageLength = 10;

  bool get _isLinkedToContent => widget.relatedId != null;

  @override
  void initState() {
    super.initState();
    _type =
        ComplaintType.tryParse(widget.initialType) ??
        (_isLinkedToContent ? ComplaintType.content : ComplaintType.complaint);
    _module = widget.relatedModule;
  }

  @override
  void dispose() {
    _subject.dispose();
    _message.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  bool _validate() {
    final subject = _subject.text.trim();
    final message = _message.text.trim();

    final subjectError = subject.isEmpty
        ? 'Konu zorunlu.'
        : (subject.length < 3 ? 'Konu en az 3 karakter olmalı.' : null);
    final messageError = message.isEmpty
        ? 'Mesaj zorunlu.'
        : (message.length < _minMessageLength
              ? 'Mesajı biraz daha açıklayın (en az $_minMessageLength karakter).'
              : null);

    setState(() {
      _subjectError = subjectError;
      _messageError = messageError;
      _generalError = null;
    });

    // Hata formun ÜSTÜNDEyse yukarı kaydır (11.11 dersi); mesaj alanı zaten
    // gönder butonunun hemen üstünde, onun için kaydırma yapılmaz.
    if (subjectError != null && _scrollController.hasClients) {
      _scrollController.animateTo(
        0,
        duration: AppDurations.medium,
        curve: Curves.easeOut,
      );
    }

    return subjectError == null && messageError == null;
  }

  Future<void> _submit() async {
    if (!_validate()) return;
    FocusScope.of(context).unfocus();
    final wasSignedIn = ref.read(authControllerProvider).isAuthenticated;

    setState(() => _sending = true);
    try {
      await ref
          .read(complaintsRepositoryProvider)
          .create(
            subject: _subject.text,
            message: _message.text,
            type: _type.apiValue,
            relatedModule: _type == ComplaintType.content ? _module : null,
            relatedId: _type == ComplaintType.content ? widget.relatedId : null,
          );

      // Girişliyse yeni kayıt "Bildirimlerim"de hemen görünsün.
      if (wasSignedIn) {
        unawaited(ref.read(myComplaintsFeedProvider.notifier).refresh());
      }

      if (!mounted) return;
      // ⚠️ Yükleme göstergesi diyalog AÇILMADAN önce kapanmalı: `AppButton`
      // `loading` iken sonsuz animasyon çiziyor, diyalog boyunca dönmeye
      // devam ederdi (11.10'da kampanya kod modalında yaşanan hatanın aynısı;
      // testte "pumpAndSettle timed out" olarak yakalandı).
      setState(() => _sending = false);
      await _showSuccessDialog(tracked: wasSignedIn);
      if (!mounted) return;
      // ⚠️ `context.push` ile açılan ekran router redirect'inin ÜSTÜNDE kalır
      // (11.5 dersi) → kapatma bir kare sonraya bırakılır.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && context.canPop()) context.pop();
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _generalError = error.message);
    } finally {
      if (mounted && _sending) setState(() => _sending = false);
    }
  }

  Future<void> _showSuccessDialog({required bool tracked}) => showDialog<void>(
    context: context,
    builder: (context) => AlertDialog(
      icon: const Icon(Icons.check_circle_outline_rounded, size: 36),
      title: const Text('Bildiriminiz alındı'),
      content: Text(
        tracked
            ? 'Bildiriminiz ilgili birime iletildi. Durumunu "Bildirimlerim" '
                  'listesinden takip edebilirsiniz.'
            : 'Bildiriminiz ilgili birime iletildi.\n\nGiriş yapmadan '
                  'gönderdiğiniz için bu bildirim "Bildirimlerim" listesinde '
                  'görünmeyecek.',
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Tamam'),
        ),
      ],
    ),
  );

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final isSignedIn = ref.watch(authControllerProvider).isAuthenticated;

    return AppScaffold(
      title: 'Bildirim gönder',
      body: ListView(
        controller: _scrollController,
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          if (!isSignedIn) ...[
            const InfoBanner(
              tone: InfoBannerTone.warning,
              title: 'Giriş yapmadan da gönderebilirsiniz',
              message:
                  'Ancak anonim bildirimler "Bildirimlerim" listesinde '
                  'görünmez ve durumunu takip edemezsiniz.',
            ),
            AppSpacing.gapSm,
            AppButton.ghost(
              label: 'Giriş yap',
              icon: Icons.login_rounded,
              size: AppButtonSize.small,
              onPressed: _sending
                  ? null
                  : () => context.push(AppRoutes.login),
            ),
            AppSpacing.gapXl,
          ],

          if (_generalError != null) ...[
            InfoBanner(tone: InfoBannerTone.danger, message: _generalError!),
            AppSpacing.gapLg,
          ],

          Text(
            'Konu türü',
            style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
          ),
          AppSpacing.gapSm,
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.sm,
            children: [
              for (final type in ComplaintType.selectable)
                FilterChoiceChip(
                  label: type.label,
                  icon: type.icon,
                  dense: true,
                  selected: _type == type,
                  onTap: () {
                    if (_sending) return;
                    setState(() {
                      _type = type;
                      if (type != ComplaintType.content) _module = null;
                    });
                  },
                ),
            ],
          ),
          AppSpacing.gapLg,

          if (_type == ComplaintType.content) ...[
            if (_isLinkedToContent)
              InfoBanner(
                tone: InfoBannerTone.info,
                title: 'İlgili içerik',
                message: widget.relatedTitle?.trim().isNotEmpty == true
                    ? widget.relatedTitle!.trim()
                    : 'Şikayetiniz açtığınız içerikle ilişkilendirilecek.',
              )
            else
              LookupDropdown<AppModule>(
                label: 'Hangi bölümle ilgili?',
                // Modül listesi uygulamanın kendi kaydından geliyor; uç yok.
                items: const AsyncValue.data(kAppModules),
                value: _module,
                idOf: (module) => module.id,
                labelOf: (module) => module.label,
                hint: 'Bölüm seçin (isteğe bağlı)',
                enabled: !_sending,
                onChanged: (value) => setState(() => _module = value),
                onRetry: () {},
              ),
            AppSpacing.gapLg,
          ],

          AppTextField(
            label: 'Konu',
            required: true,
            hint: 'Örn. Çöp toplanmıyor',
            controller: _subject,
            textCapitalization: TextCapitalization.sentences,
            textInputAction: TextInputAction.next,
            maxLength: 150,
            errorText: _subjectError,
            enabled: !_sending,
            onChanged: (_) {
              if (_subjectError != null) setState(() => _subjectError = null);
            },
          ),
          AppSpacing.gapLg,

          AppTextField(
            label: 'Mesajınız',
            required: true,
            hint: _type.messageHint,
            helper: 'Mahalle, sokak gibi bilgiler çözümü hızlandırır.',
            controller: _message,
            maxLines: 6,
            maxLength: 2000,
            textCapitalization: TextCapitalization.sentences,
            errorText: _messageError,
            enabled: !_sending,
            onChanged: (_) {
              if (_messageError != null) setState(() => _messageError = null);
            },
          ),
          AppSpacing.gapXl,

          AppButton(
            label: 'Bildirimi gönder',
            icon: Icons.send_rounded,
            expand: true,
            loading: _sending,
            onPressed: _sending ? null : _submit,
          ),
          AppSpacing.gapMd,
          Text(
            'Bildiriminiz ilgili birime iletilir. Acil durumlarda 112’yi arayın.',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ],
      ),
    );
  }
}
