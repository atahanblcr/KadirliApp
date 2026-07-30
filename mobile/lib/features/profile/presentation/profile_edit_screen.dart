import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../../auth/data/models/current_user.dart';
import '../../files/data/files_repository.dart';
import '../../lookups/data/lookups_repository.dart';
import '../../lookups/data/models/neighborhood.dart';
import '../data/profile_repository.dart';

/// Profil düzenleme — `PATCH /v1/users/me`.
///
/// **Kurallar kullanıcıya ÖNCEDEN söylenir:** kullanıcı adı ve mahalle 30
/// günde bir değişebilir (`USERNAME_CHANGE_LIMIT` / `NEIGHBORHOOD_CHANGE_LIMIT`).
/// Süre dolmadıysa alan kilitlenir ve kalan gün yazılır — kullanıcı formu
/// doldurup sunucudan ret yemez.
///
/// **Yalnız değişen alanlar gönderilir** (PATCH semantiği): dokunulmayan alan
/// isteğe hiç girmez.
class ProfileEditScreen extends ConsumerStatefulWidget {
  const ProfileEditScreen({super.key});

  @override
  ConsumerState<ProfileEditScreen> createState() => _ProfileEditScreenState();
}

class _ProfileEditScreenState extends ConsumerState<ProfileEditScreen> {
  final _username = TextEditingController();
  final _age = TextEditingController();

  String? _neighborhoodId;
  String? _pickedPhotoPath;
  bool _removePhoto = false;

  String? _usernameError;
  String? _ageError;
  String? _neighborhoodError;
  String? _generalError;
  bool _saving = false;
  bool _initialized = false;

  @override
  void dispose() {
    _username.dispose();
    _age.dispose();
    super.dispose();
  }

  /// Formu oturumdaki profille doldurur (bir kez — kullanıcı yazarken
  /// provider tazelenirse girdiler silinmesin).
  void _initializeFrom(CurrentUser user) {
    if (_initialized) return;
    _initialized = true;
    _username.text = user.username ?? '';
    _age.text = user.age?.toString() ?? '';
    _neighborhoodId = user.primaryNeighborhoodId;
  }

  // --- Fotoğraf ---

  Future<void> _showPhotoOptions(CurrentUser user) async {
    final hasPhoto = _pickedPhotoPath != null ||
        (!_removePhoto && (user.profilePhotoUrl?.isNotEmpty ?? false));

    final action = await showModalBottomSheet<_PhotoAction>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Galeriden seç'),
              onTap: () => Navigator.of(context).pop(_PhotoAction.gallery),
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Fotoğraf çek'),
              onTap: () => Navigator.of(context).pop(_PhotoAction.camera),
            ),
            if (hasPhoto)
              ListTile(
                leading: Icon(
                  Icons.delete_outline_rounded,
                  color: Theme.of(context).palette.danger,
                ),
                title: const Text('Fotoğrafı kaldır'),
                onTap: () => Navigator.of(context).pop(_PhotoAction.remove),
              ),
          ],
        ),
      ),
    );

    if (action == null || !mounted) return;

    if (action == _PhotoAction.remove) {
      setState(() {
        _pickedPhotoPath = null;
        _removePhoto = true;
      });
      return;
    }

    try {
      final picked = await ImagePicker().pickImage(
        source: action == _PhotoAction.camera
            ? ImageSource.camera
            : ImageSource.gallery,
        // Sunucu sınırı 10 MB; küçültmek yüklemeyi de hızlandırır.
        maxWidth: 1440,
        maxHeight: 1440,
        imageQuality: 85,
      );
      if (picked == null || !mounted) return;
      setState(() {
        _pickedPhotoPath = picked.path;
        _removePhoto = false;
        _generalError = null;
      });
    } on PlatformException catch (error) {
      // Kamera/galeri izni reddedildi ya da cihazda yok.
      if (!mounted) return;
      setState(() {
        _generalError = error.code == 'camera_access_denied'
            ? 'Kamera izni verilmedi. Ayarlar’dan izin verebilirsiniz.'
            : 'Fotoğraf seçilemedi. Lütfen tekrar deneyin.';
      });
    }
  }

  // --- Kaydetme ---

  bool _validateLocally(CurrentUser user) {
    final username = _username.text.trim();
    final ageText = _age.text.trim();

    String? usernameError;
    String? ageError;

    if (username.isEmpty) {
      usernameError = 'Kullanıcı adı boş olamaz.';
    } else if (username.length < 3 || username.length > 30) {
      usernameError = 'Kullanıcı adı 3-30 karakter olmalı.';
    } else if (username.contains(RegExp(r'\s'))) {
      usernameError = 'Kullanıcı adı boşluk içermemeli.';
    }

    if (ageText.isNotEmpty) {
      final age = int.tryParse(ageText);
      if (age == null) {
        ageError = 'Yaş sayı olmalı.';
      } else if (age < 13 || age > 120) {
        ageError = 'Yaş 13-120 aralığında olmalı.';
      }
    }

    setState(() {
      _usernameError = usernameError;
      _ageError = ageError;
      _neighborhoodError = null;
      _generalError = null;
    });

    return usernameError == null && ageError == null;
  }

  Future<void> _save(CurrentUser user) async {
    if (!_validateLocally(user)) return;
    FocusScope.of(context).unfocus();

    final username = _username.text.trim();
    final ageText = _age.text.trim();
    final age = ageText.isEmpty ? null : int.parse(ageText);

    // PATCH semantiği: yalnız gerçekten değişenler gönderilir.
    final changedUsername = username != (user.username ?? '') ? username : null;
    final changedAge = age != null && age != user.age ? age : null;
    final changedNeighborhood =
        _neighborhoodId != null && _neighborhoodId != user.primaryNeighborhoodId
        ? _neighborhoodId
        : null;

    final hasChanges = changedUsername != null ||
        changedAge != null ||
        changedNeighborhood != null ||
        _pickedPhotoPath != null ||
        _removePhoto;

    if (!hasChanges) {
      _showSnack('Değişiklik yapılmadı.');
      return;
    }

    setState(() => _saving = true);
    try {
      String? photoFileId;
      if (_pickedPhotoPath != null) {
        final uploaded = await ref.read(filesRepositoryProvider).upload(
          filePath: _pickedPhotoPath!,
          moduleType: 'profile',
        );
        photoFileId = uploaded.id;
      }

      final updated = await ref.read(profileRepositoryProvider).updateProfile(
        username: changedUsername,
        age: changedAge,
        primaryNeighborhoodId: changedNeighborhood,
        profilePhotoFileId: photoFileId,
        removeProfilePhoto: _removePhoto,
      );

      // Yanıt zaten güncel profil — ek GET atılmaz.
      ref.read(authControllerProvider.notifier).applyProfile(updated);

      if (!mounted) return;
      _showSnack('Profiliniz güncellendi.');

      // ⚠️ Kapatma bir kare SONRAYA bırakılıyor: `applyProfile` oturum durumunu
      // değiştirdiği için router'ın `refreshListenable`'ı tetikleniyor; aynı
      // karede kapatırsak go_router yenilemeyi işlerken bu ekranı geri açıyor
      // (canlıda ve testte ikisinde de görüldü). Kapatmayı da go_router yapmalı
      // — ekran `context.push` ile açıldı, `Navigator.pop` onu bulamıyor.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && context.canPop()) context.pop();
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _applyServerError(error));
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  /// Sunucu hatasını doğru alanın altına koyar.
  ///
  /// Kontrat alan adı bildirmiyor → önce **kod**a bakılır (30 gün kısıtları
  /// nettir), sonra mesaj içeriğine; eşleşmezse form üstünde genel uyarı.
  void _applyServerError(ApiException error) {
    _usernameError = null;
    _ageError = null;
    _neighborhoodError = null;
    _generalError = null;

    final message = error.message;
    final lowered = message.toLowerCase();

    switch (error.code) {
      case ApiErrorCodes.usernameChangeLimit:
        _usernameError = message;
      case ApiErrorCodes.neighborhoodChangeLimit:
        _neighborhoodError = message;
      case ApiErrorCodes.duplicate || ApiErrorCodes.conflict:
        _usernameError = message;
      default:
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
  }

  void _showSnack(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);

    if (user == null) {
      // Oturum bu ekrandayken düştü. `push` ile açılan ekran redirect'in
      // üstünde kaldığı için köke `go` ile dönülür (bkz. AccountDeleteScreen).
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) context.go(AppRoutes.home);
      });
      return const AppScaffold(title: 'Profili düzenle', body: LoadingView.compact());
    }
    _initializeFrom(user);

    final canChangeUsername = user.canChangeUsername();
    final canChangeNeighborhood = user.canChangeNeighborhood();
    final neighborhoods = ref.watch(neighborhoodsProvider);

    return AppScaffold(
      title: 'Profili düzenle',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Center(
            child: Column(
              children: [
                _EditableAvatar(
                  user: user,
                  pickedPhotoPath: _pickedPhotoPath,
                  removed: _removePhoto,
                  onTap: _saving ? null : () => _showPhotoOptions(user),
                ),
                AppSpacing.gapSm,
                Text(
                  _removePhoto
                      ? 'Fotoğraf kaldırılacak'
                      : _pickedPhotoPath != null
                      ? 'Yeni fotoğraf seçildi'
                      : 'Fotoğrafı değiştirmek için dokunun',
                  style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                ),
                if (_pickedPhotoPath != null || _removePhoto)
                  TextButton(
                    onPressed: _saving
                        ? null
                        : () => setState(() {
                            _pickedPhotoPath = null;
                            _removePhoto = false;
                          }),
                    child: const Text('Geri al'),
                  ),
              ],
            ),
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
            helper: canChangeUsername
                ? '3-30 karakter, boşluk olmadan. Değiştirdikten sonra '
                      '${CurrentUser.changeCooldownDays} gün kilitlenir.'
                : 'Kullanıcı adınızı ${user.usernameChangeDaysLeft()} gün sonra '
                      'tekrar değiştirebilirsiniz.',
            controller: _username,
            textInputAction: TextInputAction.next,
            inputFormatters: [FilteringTextInputFormatter.deny(RegExp(r'\s'))],
            maxLength: 30,
            errorText: _usernameError,
            enabled: !_saving && canChangeUsername,
            prefixIcon: canChangeUsername ? null : Icons.lock_outline_rounded,
            onChanged: (_) {
              if (_usernameError != null) setState(() => _usernameError = null);
            },
          ),
          AppSpacing.gapLg,

          _NeighborhoodField(
            neighborhoods: neighborhoods,
            value: _neighborhoodId,
            errorText: _neighborhoodError,
            enabled: !_saving && canChangeNeighborhood,
            helper: canChangeNeighborhood
                ? 'Mahalleniz duyuru ve bildirim hedeflemesinde kullanılır.'
                : 'Mahallenizi ${user.neighborhoodChangeDaysLeft()} gün sonra '
                      'tekrar değiştirebilirsiniz.',
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
            helper: 'Yaş bilgisi profilinizde başkalarına gösterilmez.',
            controller: _age,
            keyboardType: TextInputType.number,
            textInputAction: TextInputAction.done,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            maxLength: 3,
            errorText: _ageError,
            enabled: !_saving,
            onChanged: (_) {
              if (_ageError != null) setState(() => _ageError = null);
            },
            onSubmitted: (_) => _save(user),
          ),
          AppSpacing.gapXl,

          AppButton(
            label: 'Kaydet',
            icon: Icons.check_rounded,
            expand: true,
            loading: _saving,
            onPressed: _saving ? null : () => _save(user),
          ),
          AppSpacing.gapMd,

          Text(
            'Telefon numaranız değiştirilemez. Numaranızı değiştirmek '
            'isterseniz Şikayet/İstek üzerinden bize yazabilirsiniz.',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}

enum _PhotoAction { gallery, camera, remove }

/// Avatar + seçilen yerel fotoğrafın önizlemesi.
class _EditableAvatar extends StatelessWidget {
  const _EditableAvatar({
    required this.user,
    required this.pickedPhotoPath,
    required this.removed,
    required this.onTap,
  });

  final CurrentUser user;
  final String? pickedPhotoPath;
  final bool removed;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    const radius = 44.0;

    if (pickedPhotoPath != null) {
      return GestureDetector(
        onTap: onTap,
        child: ClipOval(
          child: Image.file(
            File(pickedPhotoPath!),
            width: radius * 2,
            height: radius * 2,
            fit: BoxFit.cover,
          ),
        ),
      );
    }

    return UserAvatar(
      initial: user.initial,
      photoUrl: removed ? null : user.profilePhotoUrl,
      radius: radius,
      onTap: onTap,
    );
  }
}

/// Mahalle seçimi (kayıt ekranındaki alanın düzenleme sürümü: kilitlenebilir
/// ve yardımcı metin taşır).
class _NeighborhoodField extends StatelessWidget {
  const _NeighborhoodField({
    required this.neighborhoods,
    required this.value,
    required this.onChanged,
    required this.onRetry,
    this.errorText,
    this.helper,
    this.enabled = true,
  });

  final AsyncValue<List<Neighborhood>> neighborhoods;
  final String? value;
  final ValueChanged<String?> onChanged;
  final VoidCallback onRetry;
  final String? errorText;
  final String? helper;
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
            if (!enabled) ...[
              AppSpacing.wGapXs,
              Icon(Icons.lock_outline_rounded, size: 14, color: palette.muted),
            ],
          ],
        ),
        AppSpacing.gapXs,
        switch (neighborhoods) {
          AsyncData(value: final items) => DropdownButtonFormField<String>(
            initialValue: items.any((item) => item.id == value) ? value : null,
            isExpanded: true,
            // helperMaxLines: varsayılan 1 satır kısıtı metni "..." ile kesiyordu.
            decoration: InputDecoration(
              errorText: errorText,
              helperText: helper,
              helperMaxLines: 3,
              errorMaxLines: 3,
            ),
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
                ? '${error.message} Mahalle listesi yüklenemedi.'
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
