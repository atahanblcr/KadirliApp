import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/phone.dart';
import '../../../core/widgets/widgets.dart';
import '../../lookups/data/lookups_repository.dart';
import '../../lookups/data/models/neighborhood.dart';
import '../application/auth_controller.dart';
import '../application/auth_state.dart';
import 'widgets/brand_header.dart';

/// Giriş — 3. adım (yalnız yeni kullanıcı): kaydı tamamla
/// (`POST /v1/auth/register`).
///
/// Sunucu kuralları burada da önden uygulanır (kullanıcı adı 3–30 ve boşluksuz,
/// yaş 13–120) — ama **son söz sunucunun**: dönen hata ilgili alanın altına
/// yazılır.
class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _username = TextEditingController();
  final _age = TextEditingController();

  String? _neighborhoodId;
  String? _usernameError;
  String? _ageError;
  String? _neighborhoodError;
  String? _generalError;
  bool _submitting = false;

  @override
  void dispose() {
    _username.dispose();
    _age.dispose();
    super.dispose();
  }

  bool _validateLocally() {
    final username = _username.text.trim();
    final ageText = _age.text.trim();
    int? age;

    String? usernameError;
    String? ageError;

    if (username.length < 3 || username.length > 30) {
      usernameError = 'Kullanıcı adı 3-30 karakter olmalı.';
    } else if (username.contains(RegExp(r'\s'))) {
      usernameError = 'Kullanıcı adı boşluk içermemeli.';
    }

    if (ageText.isNotEmpty) {
      age = int.tryParse(ageText);
      if (age == null) {
        ageError = 'Yaş sayı olmalı.';
      } else if (age < 13 || age > 120) {
        ageError = 'Yaş 13-120 aralığında olmalı.';
      }
    }

    setState(() {
      _usernameError = usernameError;
      _ageError = ageError;
      _neighborhoodError = _neighborhoodId == null ? 'Mahalle seçin.' : null;
      _generalError = null;
    });

    return usernameError == null && ageError == null && _neighborhoodId != null;
  }

  Future<void> _submit() async {
    if (!_validateLocally()) return;
    FocusScope.of(context).unfocus();
    setState(() => _submitting = true);

    try {
      await ref.read(authControllerProvider.notifier).completeRegistration(
        username: _username.text.trim(),
        neighborhoodId: _neighborhoodId!,
        age: _age.text.trim().isEmpty ? null : int.parse(_age.text.trim()),
      );
      // Başarı → durum "authenticated" olur, yönlendirmeyi router yapar.
    } on ApiException catch (error) {
      if (mounted) setState(() => _applyServerError(error));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  /// Sunucu hatasını doğru alanın altına yerleştirir.
  ///
  /// ⚠️ Kontrat hangi **alanın** hatalı olduğunu bildirmiyor (tek `code` +
  /// serbest metin), bu yüzden yerleştirme mesaj içeriğine bakar; eşleşme
  /// olmazsa mesaj form üstünde genel uyarı olarak gösterilir — hiçbir durumda
  /// kaybolmaz.
  void _applyServerError(ApiException error) {
    _usernameError = null;
    _ageError = null;
    _neighborhoodError = null;
    _generalError = null;

    final message = error.message;
    final lowered = message.toLowerCase();

    if (lowered.contains('kullanıcı adı')) {
      _usernameError = message;
    } else if (lowered.contains('yaş')) {
      _ageError = message;
    } else if (lowered.contains('mahalle')) {
      _neighborhoodError = message;
    } else {
      _generalError = message;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final authState = ref.watch(authControllerProvider);
    final phone = authState is AuthRegistering ? authState.phone : null;
    final neighborhoods = ref.watch(neighborhoodsProvider);

    return AppScaffold(
      title: 'Kaydı tamamla',
      leading: BackButton(
        onPressed: _submitting
            ? null
            : () => ref.read(authControllerProvider.notifier).cancelRegistration(),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.xxl,
        ),
        children: [
          const BrandHeader(compact: true),
          AppSpacing.gapXl,

          Text(
            'Numaranız doğrulandı 🎉',
            style: theme.textTheme.titleMedium,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXs,
          Text(
            phone == null
                ? 'Hesabınızı oluşturmak için son bir adım kaldı.'
                : '${AppPhone.display(phone)} için hesabınızı oluşturalım.',
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXl,

          if (_generalError != null) ...[
            InfoBanner(tone: InfoBannerTone.danger, message: _generalError!),
            AppSpacing.gapLg,
          ],

          AppTextField(
            label: 'Kullanıcı adı',
            required: true,
            hint: 'ornek_kullanici',
            helper: '3-30 karakter, boşluk olmadan.',
            controller: _username,
            textInputAction: TextInputAction.next,
            inputFormatters: [FilteringTextInputFormatter.deny(RegExp(r'\s'))],
            maxLength: 30,
            errorText: _usernameError,
            enabled: !_submitting,
            onChanged: (_) {
              if (_usernameError != null) setState(() => _usernameError = null);
            },
          ),
          AppSpacing.gapLg,

          _NeighborhoodField(
            neighborhoods: neighborhoods,
            value: _neighborhoodId,
            errorText: _neighborhoodError,
            enabled: !_submitting,
            onChanged: (value) => setState(() {
              _neighborhoodId = value;
              _neighborhoodError = null;
            }),
            onRetry: () => ref.invalidate(neighborhoodsProvider),
          ),
          AppSpacing.gapLg,

          AppTextField(
            label: 'Yaş (isteğe bağlı)',
            hint: '30',
            controller: _age,
            keyboardType: TextInputType.number,
            textInputAction: TextInputAction.done,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            maxLength: 3,
            errorText: _ageError,
            enabled: !_submitting,
            onChanged: (_) {
              if (_ageError != null) setState(() => _ageError = null);
            },
            onSubmitted: (_) => _submit(),
          ),
          AppSpacing.gapXl,

          AppButton(
            label: 'Kaydı Tamamla',
            icon: Icons.check_rounded,
            expand: true,
            loading: _submitting,
            onPressed: _submitting ? null : _submit,
          ),
          AppSpacing.gapMd,

          Center(
            child: TextButton(
              onPressed: _submitting
                  ? null
                  : () => ref.read(authControllerProvider.notifier).cancelRegistration(),
              child: const Text('Vazgeç'),
            ),
          ),
          AppSpacing.gapSm,

          Text(
            'Kullanıcı adı ve mahalle sonradan 30 günde bir değiştirilebilir.',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

/// Mahalle seçimi — `GET /v1/neighborhoods` (yükleniyor/hata durumlarıyla).
class _NeighborhoodField extends StatelessWidget {
  const _NeighborhoodField({
    required this.neighborhoods,
    required this.value,
    required this.onChanged,
    required this.onRetry,
    this.errorText,
    this.enabled = true,
  });

  final AsyncValue<List<Neighborhood>> neighborhoods;
  final String? value;
  final ValueChanged<String?> onChanged;
  final VoidCallback onRetry;
  final String? errorText;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(
              'Mahalleniz',
              style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
            ),
            Text(
              ' *',
              style: theme.textTheme.labelMedium?.copyWith(color: palette.danger),
            ),
          ],
        ),
        AppSpacing.gapXs,
        switch (neighborhoods) {
          AsyncData(value: final items) => DropdownButtonFormField<String>(
            initialValue: value,
            isExpanded: true,
            decoration: InputDecoration(errorText: errorText),
            hint: const Text('Mahalle seçin'),
            items: items
                .map(
                  (item) => DropdownMenuItem(
                    value: item.id,
                    child: Text(item.label, overflow: TextOverflow.ellipsis),
                  ),
                )
                .toList(),
            onChanged: enabled ? onChanged : null,
          ),
          AsyncError(:final error) => InfoBanner(
            tone: InfoBannerTone.danger,
            message: error is ApiException
                ? '${error.message} Mahalle listesi olmadan kayıt tamamlanamaz.'
                : 'Mahalle listesi alınamadı.',
            icon: Icons.refresh_rounded,
            onClose: onRetry,
          ),
          _ => const SkeletonBox(height: AppA11y.minTapSize),
        },
      ],
    );
  }
}
