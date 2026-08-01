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
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../../lookups/data/lookups_repository.dart';
import '../../lookups/data/models/named_lookup.dart';
import '../../lookups/data/models/neighborhood.dart';
import '../application/death_submission_service.dart';
import '../application/deaths_providers.dart';

/// Vefat bildirimi (`POST /v1/deaths` `[A]`) — kabuğun dışında tam ekran.
///
/// **Taslak kaydı bilinçli olarak YOK** (11.9 `AdDraftStore` deseninden sapma):
/// vefat bildirimi acil ve tek seferlik bir iştir; "taslağı sakla / sonra devam
/// et" teklifi bu bağlamda hem gereksiz hem de yersiz kaçıyor. Form kısa ve tek
/// ekranda bittiği için yarıda kalma riski de düşük.
///
/// Sunucuda bu uç için **FluentValidation doğrulayıcısı yok** → zorunlu alan
/// denetimi tamamen istemcide; boş isimle gönderilen kayıt moderasyon kuyruğunu
/// kirletirdi.
class DeathReportScreen extends ConsumerStatefulWidget {
  const DeathReportScreen({super.key});

  @override
  ConsumerState<DeathReportScreen> createState() => _DeathReportScreenState();
}

class _DeathReportScreenState extends ConsumerState<DeathReportScreen> {
  final _name = TextEditingController();
  final _condolenceAddress = TextEditingController();
  final _scrollController = ScrollController();

  DateTime? _funeralDate;
  TimeOfDayValue? _funeralTime;
  String? _mosqueId;
  String? _cemeteryId;
  String? _neighborhoodId;
  String? _photoPath;

  String? _nameError;
  String? _dateError;
  String? _timeError;
  String? _generalError;
  bool _sending = false;

  @override
  void dispose() {
    _name.dispose();
    _condolenceAddress.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  // --- Tarih / saat ---

  Future<void> _pickDate() async {
    final today = AppDate.nowInTurkey;
    final initial = _funeralDate ?? DateTime(today.year, today.month, today.day);
    final picked = await showDatePicker(
      context: context,
      initialDate: initial,
      // Geç bildirilen bir cenaze de girilebilmeli; ileriye bir ay yeter.
      firstDate: DateTime(today.year, today.month, today.day - 7),
      lastDate: DateTime(today.year, today.month, today.day + 30),
      helpText: 'Cenaze namazı tarihi',
      cancelText: 'Vazgeç',
      confirmText: 'Seç',
    );
    if (picked == null || !mounted) return;
    setState(() {
      _funeralDate = picked;
      _dateError = null;
    });
  }

  Future<void> _pickTime() async {
    final current = _funeralTime;
    final picked = await showTimePicker(
      context: context,
      initialTime: current == null
          ? const TimeOfDay(hour: 13, minute: 0)
          : TimeOfDay(hour: current.hour, minute: current.minute),
      helpText: 'Cenaze namazı saati',
      cancelText: 'Vazgeç',
      confirmText: 'Seç',
      // Kadirli'de saat 24 saat biçiminde okunuyor; AM/PM kafa karıştırır.
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(alwaysUse24HourFormat: true),
        child: child!,
      ),
    );
    if (picked == null || !mounted) return;
    setState(() {
      _funeralTime = TimeOfDayValue(picked.hour, picked.minute);
      _timeError = null;
    });
  }

  // --- Fotoğraf ---

  Future<void> _pickPhoto() async {
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
            if (_photoPath != null)
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
      setState(() => _photoPath = null);
      return;
    }

    try {
      final picked = await ImagePicker().pickImage(
        source: action == _PhotoAction.camera
            ? ImageSource.camera
            : ImageSource.gallery,
        maxWidth: 1440,
        maxHeight: 1440,
        imageQuality: 85,
      );
      if (picked == null || !mounted) return;
      setState(() {
        _photoPath = picked.path;
        _generalError = null;
      });
    } on PlatformException catch (error) {
      if (!mounted) return;
      setState(() {
        _generalError = error.code == 'camera_access_denied'
            ? 'Kamera izni verilmedi. Ayarlar’dan izin verebilirsiniz.'
            : 'Fotoğraf seçilemedi. Lütfen tekrar deneyin.';
      });
    }
  }

  // --- Gönderme ---

  bool _validate() {
    final name = _name.text.trim();
    final nameError = name.isEmpty
        ? 'Merhumun adı soyadı zorunlu.'
        : (name.length < 3 ? 'Ad soyad en az 3 karakter olmalı.' : null);

    setState(() {
      _nameError = nameError;
      _dateError = _funeralDate == null ? 'Cenaze namazı tarihini seçin.' : null;
      _timeError = _funeralTime == null ? 'Cenaze namazı saatini seçin.' : null;
      _generalError = null;
    });

    // Hata formun ÜSTÜNDEyse görünür kılınmalı: kullanıcı gönder butonunun
    // yanında hiçbir şey olmadığı için "buton çalışmıyor" sanıyordu (11.9'da
    // ilan formunda yaşanan hatanın tersi: orada gereksiz yere kaydırılıyordu).
    if (nameError != null && _scrollController.hasClients) {
      _scrollController.animateTo(
        0,
        duration: AppDurations.medium,
        curve: Curves.easeOut,
      );
    }

    return nameError == null && _funeralDate != null && _funeralTime != null;
  }

  Future<void> _submit() async {
    if (!_validate()) return;
    FocusScope.of(context).unfocus();

    setState(() => _sending = true);
    try {
      await ref
          .read(deathSubmissionServiceProvider)
          .submit(
            DeathNoticeDraft(
              deceasedName: _name.text.trim(),
              funeralDate: _funeralDate!,
              funeralTime: _funeralTime!,
              mosqueId: _mosqueId,
              cemeteryId: _cemeteryId,
              neighborhoodId: _neighborhoodId,
              condolenceAddress: _condolenceAddress.text.trim(),
              photoPath: _photoPath,
            ),
          );

      // Kayıt `pending` olduğu için listede henüz görünmez; yine de listeyi
      // tazeliyoruz ki onaydan sonra geri dönüldüğünde taze olsun.
      ref.read(deathsFeedProvider.notifier).refresh();

      if (!mounted) return;
      await _showSuccessDialog();
      if (!mounted) return;
      // ⚠️ `context.push` ile açılan ekran router redirect'inin ÜSTÜNDE kalır
      // (11.5 dersi) → kapatma bir kare sonraya bırakılır.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && context.canPop()) context.pop();
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _generalError = error.message);
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  Future<void> _showSuccessDialog() => showDialog<void>(
    context: context,
    builder: (context) => AlertDialog(
      icon: const Icon(Icons.check_circle_outline_rounded, size: 36),
      title: const Text('Bildiriminiz alındı'),
      content: const Text(
        'Vefat bildiriminiz görevlilere iletildi. Kontrol edildikten sonra '
        'yayına alınacak ve herkes tarafından görülebilecek.\n\n'
        'Başınız sağ olsun.',
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

    // Oturum bu ekrandayken düşerse (11.5 dersi) köke dönülür.
    if (!ref.watch(authControllerProvider).isAuthenticated) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) context.go(AppRoutes.home);
      });
      return const AppScaffold(
        title: 'Vefat bildir',
        body: LoadingView.compact(),
      );
    }

    return AppScaffold(
      title: 'Vefat bildir',
      body: ListView(
        controller: _scrollController,
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          const InfoBanner(
            tone: InfoBannerTone.info,
            message:
                'Bildiriminiz yayına alınmadan önce görevlilerce kontrol '
                'edilir. Lütfen bilgileri aile ile teyit ederek girin.',
          ),
          AppSpacing.gapXl,

          if (_generalError != null) ...[
            InfoBanner(tone: InfoBannerTone.danger, message: _generalError!),
            AppSpacing.gapLg,
          ],

          AppTextField(
            label: 'Merhumun adı soyadı',
            required: true,
            hint: 'Örn. Emine Kaya',
            controller: _name,
            textCapitalization: TextCapitalization.words,
            textInputAction: TextInputAction.next,
            maxLength: 100,
            errorText: _nameError,
            enabled: !_sending,
            onChanged: (_) {
              if (_nameError != null) setState(() => _nameError = null);
            },
          ),
          AppSpacing.gapLg,

          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: _PickerField(
                  label: 'Cenaze namazı tarihi',
                  isRequired: true,
                  icon: Icons.calendar_today_rounded,
                  value: _funeralDate == null
                      ? null
                      : AppDate.dayWithWeekday(
                          DateTime.utc(
                            _funeralDate!.year,
                            _funeralDate!.month,
                            _funeralDate!.day,
                          ),
                        ),
                  hint: 'Tarih seçin',
                  errorText: _dateError,
                  onTap: _sending ? null : _pickDate,
                ),
              ),
              AppSpacing.wGapMd,
              Expanded(
                child: _PickerField(
                  label: 'Saat',
                  isRequired: true,
                  icon: Icons.schedule_rounded,
                  value: _funeralTime?.label,
                  hint: 'Saat seçin',
                  errorText: _timeError,
                  onTap: _sending ? null : _pickTime,
                ),
              ),
            ],
          ),
          AppSpacing.gapLg,

          LookupDropdown<NamedLookup>(
            label: 'Cenaze namazının kılınacağı cami',
            items: ref.watch(mosquesProvider),
            value: _mosqueId,
            idOf: (item) => item.id,
            labelOf: (item) => item.name,
            hint: 'Cami seçin (isteğe bağlı)',
            enabled: !_sending,
            onChanged: (value) => setState(() => _mosqueId = value),
            onRetry: () => ref.invalidate(mosquesProvider),
            emptyMessage: 'Cami listesi henüz girilmemiş.',
          ),
          AppSpacing.gapLg,

          LookupDropdown<NamedLookup>(
            label: 'Defnedileceği mezarlık',
            items: ref.watch(cemeteriesProvider),
            value: _cemeteryId,
            idOf: (item) => item.id,
            labelOf: (item) => item.name,
            hint: 'Mezarlık seçin (isteğe bağlı)',
            enabled: !_sending,
            onChanged: (value) => setState(() => _cemeteryId = value),
            onRetry: () => ref.invalidate(cemeteriesProvider),
            emptyMessage: 'Mezarlık listesi henüz girilmemiş.',
          ),
          AppSpacing.gapLg,

          LookupDropdown<Neighborhood>(
            label: 'Mahalle',
            items: ref.watch(neighborhoodsProvider),
            value: _neighborhoodId,
            idOf: (item) => item.id,
            labelOf: (item) => item.label,
            hint: 'Mahalle seçin (isteğe bağlı)',
            helper: 'Merhumun ikamet ettiği mahalle.',
            enabled: !_sending,
            onChanged: (value) => setState(() => _neighborhoodId = value),
            onRetry: () => ref.invalidate(neighborhoodsProvider),
          ),
          AppSpacing.gapLg,

          AppTextField(
            label: 'Taziye adresi (isteğe bağlı)',
            hint: 'Örn. Yenimahalle, 1234 Sk. No: 5',
            helper: 'Taziyelerin kabul edileceği yer.',
            controller: _condolenceAddress,
            maxLines: 2,
            maxLength: 250,
            textCapitalization: TextCapitalization.sentences,
            enabled: !_sending,
          ),
          AppSpacing.gapXl,

          _PhotoField(
            path: _photoPath,
            onTap: _sending ? null : _pickPhoto,
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
            'Yanlış ya da doğrulanmamış bilgi içeren bildirimler yayına '
            'alınmaz.',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ],
      ),
    );
  }
}

enum _PhotoAction { gallery, camera, remove }

/// Tarih/saat gibi dokunarak seçilen alanlar — `AppTextField` ile aynı görsel
/// dile sahip ama klavye açmaz.
class _PickerField extends StatelessWidget {
  const _PickerField({
    required this.label,
    required this.icon,
    required this.value,
    required this.hint,
    required this.onTap,
    this.errorText,
    this.isRequired = false,
  });

  final String label;
  final IconData icon;
  final String? value;
  final String hint;
  final VoidCallback? onTap;
  final String? errorText;
  final bool isRequired;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final hasError = errorText != null && errorText!.isNotEmpty;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Flexible(
              child: Text(
                label,
                style: theme.textTheme.labelMedium?.copyWith(
                  color: palette.muted,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            if (isRequired)
              Text(
                ' *',
                style: theme.textTheme.labelMedium?.copyWith(
                  color: palette.danger,
                ),
              ),
          ],
        ),
        AppSpacing.gapXs,
        Semantics(
          button: true,
          label: '$label: ${value ?? hint}',
          child: Material(
            color: theme.colorScheme.surface,
            shape: RoundedRectangleBorder(
              borderRadius: AppRadius.rMd,
              side: BorderSide(color: hasError ? palette.danger : palette.border),
            ),
            child: InkWell(
              onTap: onTap,
              borderRadius: AppRadius.rMd,
              child: Container(
                constraints: const BoxConstraints(minHeight: AppA11y.minTapSize),
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.lg,
                  vertical: AppSpacing.md,
                ),
                child: Row(
                  children: [
                    Icon(icon, size: 18, color: palette.muted),
                    AppSpacing.wGapSm,
                    Expanded(
                      child: Text(
                        value ?? hint,
                        style: theme.textTheme.bodyLarge?.copyWith(
                          color: value == null
                              ? palette.muted
                              : theme.colorScheme.onSurface,
                        ),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
        if (hasError) ...[
          AppSpacing.gapXs,
          Text(
            errorText!,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.danger),
          ),
        ],
      ],
    );
  }
}

class _PhotoField extends StatelessWidget {
  const _PhotoField({required this.path, required this.onTap});

  final String? path;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Row(
      children: [
        Semantics(
          button: true,
          label: path == null ? 'Fotoğraf ekle' : 'Fotoğrafı değiştir',
          child: InkWell(
            onTap: onTap,
            borderRadius: AppRadius.rMd,
            child: Container(
              width: 76,
              height: 92,
              decoration: BoxDecoration(
                color: theme.colorScheme.surface,
                borderRadius: AppRadius.rMd,
                border: Border.all(color: palette.border),
              ),
              clipBehavior: Clip.antiAlias,
              child: path == null
                  ? Icon(Icons.add_a_photo_outlined, color: palette.muted)
                  : Image.file(File(path!), fit: BoxFit.cover),
            ),
          ),
        ),
        AppSpacing.wGapLg,
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Fotoğraf (isteğe bağlı)', style: theme.textTheme.titleSmall),
              AppSpacing.gapXs,
              Text(
                path == null
                    ? 'Merhuma ait bir fotoğraf ekleyebilirsiniz.'
                    : 'Fotoğraf seçildi. Değiştirmek için dokunun.',
                style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
