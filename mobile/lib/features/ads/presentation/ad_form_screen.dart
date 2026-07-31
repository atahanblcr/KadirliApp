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
import '../application/ad_submission_service.dart';
import '../application/ads_providers.dart';
import '../application/my_ads_controller.dart';
import '../data/ad_draft_store.dart';
import '../data/models/ad_category.dart';
import '../data/models/ad_detail.dart';
import '../data/models/category_property.dart';
import 'widgets/ad_image_picker.dart';
import 'widgets/ad_property_field.dart';

/// İlan verme / düzenleme formu (11.9).
///
/// **Üç adım** (`/ilan-ver`): Kategori → Bilgiler → Fotoğraflar & gönder.
/// Uzun tek sayfalık bir form yerine adımlar seçildi çünkü **kategoriye özel
/// alanlar ancak kategori bilindikten sonra** çekilebiliyor
/// (`GET /v1/ads/categories/{id}/properties`) — tek sayfada form ortadan
/// büyüyüp kullanıcının altına yazdıklarını kaydırırdı.
///
/// **Düzenleme** (`/ilan-ver/<id>`): kategori değiştirilemez (sunucu kuralı),
/// bu yüzden 1. adım atlanır ve kullanıcı en başta uyarılır: her düzenleme
/// ilanı **yeniden onaya** düşürür.
class AdFormScreen extends ConsumerStatefulWidget {
  const AdFormScreen({super.key, this.adId});

  /// null → yeni ilan; dolu → düzenleme.
  final String? adId;

  bool get isEdit => adId != null;

  @override
  ConsumerState<AdFormScreen> createState() => _AdFormScreenState();
}

class _AdFormScreenState extends ConsumerState<AdFormScreen> {
  static const maxImages = 10; // AdSubmissionRules.MaxImages
  static const maxTitle = 200;
  static const maxDescription = 5000;

  final _title = TextEditingController();
  final _description = TextEditingController();
  final _price = TextEditingController();
  final _sellerName = TextEditingController();
  final _phone = TextEditingController();
  final _propertyControllers = <String, TextEditingController>{};
  final _scrollController = ScrollController();

  int _step = 0;

  String? _categoryId;
  String? _categoryName;
  String? _rootCategoryId;

  /// Kategori adımında içine inilen kök (null → kök listesi gösteriliyor).
  AdCategory? _openRoot;

  final _propertyValues = <String, String>{};
  final _propertyErrors = <String, String>{};

  List<AdFormImage> _images = [];
  List<AdFormImage> _originalImages = [];

  String? _titleError;
  String? _descriptionError;
  String? _priceError;
  String? _phoneError;
  String? _generalError;

  bool _submitting = false;
  int _uploaded = 0;
  int _uploadTotal = 0;

  /// Kullanıcı bir şey yazdı mı — çıkış onayı ve taslak kaydı buna bakar.
  bool _dirty = false;

  bool _prefilled = false;
  bool _draftChecked = false;

  @override
  void initState() {
    super.initState();
    if (widget.isEdit) _step = 1;
    for (final controller in [_title, _description, _price, _sellerName]) {
      controller.addListener(_markDirty);
    }
    _phone.addListener(_markDirty);
  }

  @override
  void dispose() {
    for (final controller in [_title, _description, _price, _sellerName, _phone]) {
      controller.dispose();
    }
    for (final controller in _propertyControllers.values) {
      controller.dispose();
    }
    _scrollController.dispose();
    super.dispose();
  }

  void _markDirty() {
    if (!_dirty) _dirty = true;
  }

  // --- Ön doldurma ---

  /// Yeni ilanda satıcı adı + telefon oturumdaki profilden gelir; kullanıcı
  /// istediği gibi değiştirebilir (ilan telefonu profil telefonundan farklı
  /// olabilir — dükkân numarası gibi).
  void _prefillFromProfile() {
    if (_prefilled) return;
    _prefilled = true;
    final user = ref.read(currentUserProvider);
    if (user == null) return;
    _sellerName.text = user.displayName;
    _phone.text = AppPhone.formatNational(user.phone);
    _dirty = false;
  }

  void _prefillFromAd(AdDetail ad) {
    if (_prefilled) return;
    _prefilled = true;
    _categoryId = ad.categoryId;
    _categoryName = ad.categoryName;
    _title.text = ad.title;
    _description.text = ad.description;
    _price.text = ad.price == null ? '' : AppMoney.plain(ad.price!);
    _sellerName.text = ad.sellerName ?? '';
    _phone.text = AppPhone.formatNational(ad.contactPhone);
    for (final property in ad.properties) {
      final value = property.value.trim();
      if (value.isNotEmpty) _propertyValues[property.propertyId] = value;
    }
    _originalImages = [
      for (final image in ad.images) AdFormImage.fromDetail(image),
    ];
    _images = [..._originalImages];
    _dirty = false;
  }

  /// Yarım kalmış taslak varsa **sorarak** geri yükler (sessizce doldurmak
  /// kullanıcıyı şaşırtır: "bunu ben mi yazdım?").
  Future<void> _offerDraft() async {
    if (_draftChecked || widget.isEdit) return;
    _draftChecked = true;

    final draft = ref.read(adDraftStoreProvider).read();
    if (draft == null || !mounted) return;

    final restore = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Yarım kalan ilanınız var'),
        content: Text(
          draft.title.trim().isEmpty
              ? 'Daha önce başladığınız ilan taslağını geri yükleyelim mi?'
              : '“${draft.title.trim()}” başlıklı taslağı geri yükleyelim mi?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Yeni ilan'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Geri yükle'),
          ),
        ],
      ),
    );

    if (!mounted) return;
    if (restore != true) {
      await ref.read(adDraftStoreProvider).clear();
      return;
    }

    setState(() {
      _categoryId = draft.categoryId;
      _categoryName = draft.categoryName;
      _rootCategoryId = draft.rootCategoryId;
      _title.text = draft.title;
      _description.text = draft.description;
      _price.text = draft.price;
      if (draft.sellerName.isNotEmpty) _sellerName.text = draft.sellerName;
      if (draft.contactPhone.isNotEmpty) {
        _phone.text = AppPhone.formatNational(draft.contactPhone);
      }
      _propertyValues
        ..clear()
        ..addAll(draft.propertyValues);
      if (_categoryId != null) _step = 1;
    });
  }

  Future<void> _saveDraft() async {
    if (widget.isEdit || !_dirty) return;
    await ref.read(adDraftStoreProvider).save(
      AdDraft(
        categoryId: _categoryId,
        rootCategoryId: _rootCategoryId,
        categoryName: _categoryName,
        title: _title.text,
        description: _description.text,
        price: _price.text,
        sellerName: _sellerName.text,
        contactPhone: _phone.text,
        propertyValues: Map.of(_propertyValues),
      ),
    );
  }

  // --- Kategori adımı ---

  void _selectCategory(AdCategory category, {String? rootId}) {
    setState(() {
      _categoryId = category.id;
      _categoryName = category.name;
      _rootCategoryId = rootId ?? category.id;
      _dirty = true;
      _step = 1;
    });
    _saveDraft();
  }

  // --- Görseller ---

  Future<void> _pickImages() async {
    final remaining = maxImages - _images.length;
    if (remaining <= 0) return;

    try {
      final picked = await ImagePicker().pickMultiImage(
        // Sunucu sınırı 10 MB; küçültmek yüklemeyi de hızlandırır (11.5 deseni).
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 85,
        limit: remaining,
      );
      if (picked.isEmpty || !mounted) return;
      setState(() {
        _images = [
          ..._images,
          ...picked.take(remaining).map((file) => AdFormImage.picked(file.path)),
        ];
        _dirty = true;
        _generalError = null;
      });
    } on PlatformException catch (error) {
      if (!mounted) return;
      setState(
        () => _generalError = error.code == 'photo_access_denied'
            ? 'Galeri izni verilmedi. Ayarlar’dan izin verebilirsiniz.'
            : 'Fotoğraf seçilemedi. Lütfen tekrar deneyin.',
      );
    }
  }

  void _removeImage(int index) {
    setState(() {
      _images = [..._images]..removeAt(index);
      _dirty = true;
    });
  }

  void _makeCover(int index) {
    setState(() {
      final next = [..._images];
      next.insert(0, next.removeAt(index));
      _images = next;
      _dirty = true;
    });
  }

  // --- Doğrulama (sunucu kurallarının aynası) ---

  bool _validateDetails(List<CategoryProperty> properties) {
    final title = _title.text.trim();
    final description = _description.text.trim();
    final priceText = _price.text.trim();

    String? titleError;
    String? descriptionError;
    String? priceError;
    String? phoneError;

    if (title.length < 3) {
      titleError = 'Başlık en az 3 karakter olmalı.';
    } else if (title.length > maxTitle) {
      titleError = 'Başlık en fazla $maxTitle karakter olabilir.';
    }

    if (description.isEmpty) {
      descriptionError = 'Açıklama zorunludur.';
    } else if (description.length > maxDescription) {
      descriptionError = 'Açıklama $maxDescription karakteri aşamaz.';
    }

    if (priceText.isNotEmpty && AppMoney.parse(priceText) == null) {
      priceError = 'Geçerli bir fiyat girin (örn. 25.000).';
    }

    if (!AppPhone.isValid(_phone.text)) {
      phoneError = 'Geçerli bir cep telefonu girin (5xx ile başlayan 10 hane).';
    }

    final propertyErrors = <String, String>{};
    for (final property in properties) {
      if (!property.isUsable || !property.isRequired) continue;
      final value = _propertyValues[property.id]?.trim();
      if (value == null || value.isEmpty) {
        propertyErrors[property.id] = 'Bu alan zorunludur.';
      }
    }

    setState(() {
      _titleError = titleError;
      _descriptionError = descriptionError;
      _priceError = priceError;
      _phoneError = phoneError;
      _propertyErrors
        ..clear()
        ..addAll(propertyErrors);
      _generalError = null;
    });

    return titleError == null &&
        descriptionError == null &&
        priceError == null &&
        phoneError == null &&
        propertyErrors.isEmpty;
  }

  // --- Gönderme ---

  Future<void> _submit(List<CategoryProperty> properties) async {
    if (!_validateDetails(properties)) {
      setState(() => _step = 1);
      return;
    }

    final values = AdFormValues(
      categoryId: _categoryId!,
      title: _title.text.trim(),
      description: _description.text.trim(),
      contactPhone: AppPhone.toE164(_phone.text)!,
      price: AppMoney.parse(_price.text),
      sellerName: _sellerName.text.trim().isEmpty ? null : _sellerName.text.trim(),
      propertyValues: {
        for (final entry in _propertyValues.entries)
          if (entry.value.trim().isNotEmpty) entry.key: entry.value.trim(),
      },
    );

    setState(() {
      _submitting = true;
      _generalError = null;
      _uploaded = 0;
      _uploadTotal = _images.where((image) => !image.isExisting).length;
    });

    final service = ref.read(adSubmissionServiceProvider);
    final messenger = ScaffoldMessenger.of(context);

    try {
      if (widget.isEdit) {
        await service.update(
          adId: widget.adId!,
          values: values,
          images: _images,
          originalImages: _originalImages,
          propertyValues: values.propertyValues,
          onProgress: _onUploadProgress,
        );
      } else {
        await service.create(
          values: values,
          images: _images,
          onProgress: _onUploadProgress,
        );
        await ref.read(adDraftStoreProvider).clear();
      }

      // Liste taze olsun: yeni/güncellenen ilan "Onay bekliyor" olarak görünür.
      ref.read(myAdsProvider.notifier).refresh();

      if (!mounted) return;
      _dirty = false;
      messenger.showSnackBar(
        SnackBar(
          content: Text(
            widget.isEdit
                ? 'İlanınız güncellendi ve yeniden onaya gönderildi.'
                : 'İlanınız onaya gönderildi. Onaylandığında bildirim alacaksınız.',
          ),
          duration: const Duration(seconds: 4),
        ),
      );

      // ⚠️ 11.5 dersi: `context.push` ile açılan ekran router redirect'inin
      // ÜSTÜNDE kalır → kapatmayı bir kare sonraya bırak.
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (!mounted) return;
        if (widget.isEdit && context.canPop()) {
          context.pop();
        } else {
          context.go(AppRoutes.myAds);
        }
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _applyServerError(error));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  void _onUploadProgress(int uploaded, int total) {
    if (!mounted) return;
    setState(() {
      _uploaded = uploaded;
      _uploadTotal = total;
    });
  }

  /// Sunucu hatasını **doğru alanın altına** koyar. Kontrat alan adı
  /// bildirmiyor (tek `ValidationException` metni) → mesaj içeriğine bakılır;
  /// eşleşmezse form üstünde genel uyarı (11.5 deseni).
  void _applyServerError(ApiException error) {
    _titleError = null;
    _descriptionError = null;
    _priceError = null;
    _phoneError = null;
    _generalError = null;

    final message = error.message;
    final lowered = message.toLowerCase();

    if (lowered.contains('başlık')) {
      _titleError = message;
      _step = 1;
    } else if (lowered.contains('açıklama')) {
      _descriptionError = message;
      _step = 1;
    } else if (lowered.contains('fiyat')) {
      _priceError = message;
      _step = 1;
    } else if (lowered.contains('telefon')) {
      _phoneError = message;
      _step = 1;
    } else if (lowered.contains('zorunlu özellik')) {
      _generalError = message;
      _step = 1;
    } else {
      _generalError = message;
    }
  }

  // --- Çıkış koruması ---

  Future<bool> _confirmLeave() async {
    if (!_dirty || _submitting) return true;

    final action = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('İlanı yarıda mı bırakıyorsunuz?'),
        content: Text(
          widget.isEdit
              ? 'Yaptığınız değişiklikler kaydedilmedi.'
              : 'Yazdıklarınızı taslak olarak saklayabiliriz; bir sonraki '
                    'açılışta kaldığınız yerden devam edersiniz.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop('stay'),
            child: const Text('Devam et'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop('discard'),
            child: const Text('Vazgeç'),
          ),
          if (!widget.isEdit)
            FilledButton(
              onPressed: () => Navigator.of(context).pop('draft'),
              child: const Text('Taslağı sakla'),
            ),
        ],
      ),
    );

    if (action == 'draft') {
      await _saveDraft();
      return true;
    }
    if (action == 'discard') {
      if (!widget.isEdit) await ref.read(adDraftStoreProvider).clear();
      return true;
    }
    return false;
  }

  // --- Çizim ---

  @override
  Widget build(BuildContext context) {
    if (widget.isEdit) return _buildEdit(context);

    _prefillFromProfile();
    WidgetsBinding.instance.addPostFrameCallback((_) => _offerDraft());

    final properties = _categoryId == null
        ? const <CategoryProperty>[]
        : (ref.watch(adCategoryPropertiesProvider(_categoryId!)).value ??
              const <CategoryProperty>[]);

    return _shell(context, properties: properties);
  }

  Widget _buildEdit(BuildContext context) {
    final detail = ref.watch(adDetailProvider(widget.adId!));

    return switch (detail) {
      AsyncData(value: final ad) => () {
        _prefillFromAd(ad);
        final properties =
            ref.watch(adCategoryPropertiesProvider(ad.categoryId)).value ??
            const <CategoryProperty>[];
        return _shell(context, properties: properties);
      }(),
      AsyncError(:final error) => AppScaffold(
        title: 'İlanı düzenle',
        body: ErrorView(
          message: error is ApiException ? error.message : 'İlan alınamadı.',
          traceId: error is ApiException ? error.traceId : null,
          onRetry: () => ref.invalidate(adDetailProvider(widget.adId!)),
        ),
      ),
      _ => const AppScaffold(title: 'İlanı düzenle', body: LoadingView(itemCount: 3)),
    };
  }

  Widget _shell(
    BuildContext context, {
    required List<CategoryProperty> properties,
  }) {
    final steps = widget.isEdit ? const [1, 2] : const [0, 1, 2];

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) async {
        if (didPop) return;
        if (!await _confirmLeave()) return;
        if (!context.mounted) return;
        if (context.canPop()) {
          context.pop();
        } else {
          context.go(AppRoutes.ads);
        }
      },
      child: AppScaffold(
        title: widget.isEdit ? 'İlanı düzenle' : 'İlan ver',
        body: Column(
          children: [
            _StepIndicator(
              steps: steps,
              current: _step,
              labels: const {0: 'Kategori', 1: 'Bilgiler', 2: 'Fotoğraflar'},
            ),
            Expanded(
              child: switch (_step) {
                0 => _CategoryStep(
                  openRoot: _openRoot,
                  onOpenRoot: (root) => setState(() => _openRoot = root),
                  onSelect: _selectCategory,
                ),
                1 => _detailsStep(context),
                _ => _photosStep(context),
              },
            ),
          ],
        ),
        bottomNavigationBar: _step == 0 ? null : _bottomBar(context, properties),
      ),
    );
  }

  Widget _bottomBar(BuildContext context, List<CategoryProperty> properties) {
    final theme = Theme.of(context);
    final isLastStep = _step == 2;

    return Container(
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        border: Border(top: BorderSide(color: theme.palette.border)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Row(
            children: [
              if (!(widget.isEdit && _step == 1))
                Expanded(
                  child: AppButton.ghost(
                    label: 'Geri',
                    icon: Icons.arrow_back_rounded,
                    expand: true,
                    onPressed: _submitting
                        ? null
                        : () => setState(() => _step -= 1),
                  ),
                ),
              if (!(widget.isEdit && _step == 1)) AppSpacing.wGapSm,
              Expanded(
                flex: 2,
                child: AppButton(
                  label: isLastStep
                      ? (widget.isEdit ? 'Güncelle ve onaya gönder' : 'Yayına gönder')
                      : 'Devam',
                  icon: isLastStep ? Icons.send_rounded : Icons.arrow_forward_rounded,
                  expand: true,
                  loading: _submitting,
                  onPressed: _submitting
                      ? null
                      : () {
                          if (isLastStep) {
                            _submit(properties);
                            return;
                          }
                          if (_validateDetails(properties)) {
                            setState(() => _step = 2);
                            _saveDraft();
                          } else if (_hasBaseFieldError) {
                            // ⚠️ Yalnız **üstteki** alanlarda hata varsa yukarı
                            // kaydır. Hata kategoriye özel alanlardaysa
                            // kullanıcı zaten oraya bakıyor; yukarı kaydırmak
                            // sebebi ekrandan çıkarıyordu (canlıda görüldü).
                            _scrollToTop();
                          }
                        },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// Formun üst kısmındaki (kategoriye özel olmayan) alanlarda hata var mı?
  bool get _hasBaseFieldError =>
      _titleError != null ||
      _descriptionError != null ||
      _priceError != null ||
      _phoneError != null ||
      _generalError != null;

  void _scrollToTop() {
    if (!_scrollController.hasClients) return;
    _scrollController.animateTo(
      0,
      duration: AppDurations.medium,
      curve: Curves.easeOut,
    );
  }

  // --- 2. adım: bilgiler ---

  Widget _detailsStep(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final propertiesAsync = _categoryId == null
        ? const AsyncData<List<CategoryProperty>>(<CategoryProperty>[])
        : ref.watch(adCategoryPropertiesProvider(_categoryId!));

    return ListView(
      controller: _scrollController,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (widget.isEdit) ...[
          const InfoBanner(
            tone: InfoBannerTone.warning,
            message:
                'Düzenlediğiniz ilan yeniden yönetici onayına düşer ve onay '
                'verilene kadar yayından kalkar.',
          ),
          AppSpacing.gapLg,
        ],

        if (_generalError != null) ...[
          InfoBanner(tone: InfoBannerTone.danger, message: _generalError!),
          AppSpacing.gapLg,
        ],

        _CategorySummary(
          name: _categoryName ?? '—',
          locked: widget.isEdit,
          onChange: widget.isEdit ? null : () => setState(() => _step = 0),
        ),
        AppSpacing.gapLg,

        AppTextField(
          label: 'Başlık',
          required: true,
          hint: 'Örn. Az kullanılmış bisiklet',
          controller: _title,
          maxLength: maxTitle,
          textCapitalization: TextCapitalization.sentences,
          textInputAction: TextInputAction.next,
          errorText: _titleError,
          enabled: !_submitting,
          onChanged: (_) {
            if (_titleError != null) setState(() => _titleError = null);
          },
        ),
        AppSpacing.gapLg,

        AppTextField(
          label: 'Açıklama',
          required: true,
          hint: 'Ürünün durumu, kullanım süresi, teslim şekli…',
          controller: _description,
          maxLines: 6,
          maxLength: maxDescription,
          textCapitalization: TextCapitalization.sentences,
          errorText: _descriptionError,
          enabled: !_submitting,
          onChanged: (_) {
            if (_descriptionError != null) {
              setState(() => _descriptionError = null);
            }
          },
        ),
        AppSpacing.gapLg,

        AppTextField(
          label: 'Fiyat',
          hint: '25.000',
          helper:
              'Boş bırakırsanız ilanda “Fiyat belirtilmemiş” yazar '
              '(“0 ₺” yazılmaz).',
          controller: _price,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))],
          suffix: Padding(
            padding: const EdgeInsets.only(right: AppSpacing.md),
            child: Text('₺', style: theme.textTheme.titleMedium),
          ),
          errorText: _priceError,
          enabled: !_submitting,
          onChanged: (_) {
            if (_priceError != null) setState(() => _priceError = null);
          },
        ),
        AppSpacing.gapLg,

        AppTextField(
          label: 'İlan sahibi',
          hint: 'Adınız ya da işletme adınız',
          controller: _sellerName,
          maxLength: 100,
          textCapitalization: TextCapitalization.words,
          enabled: !_submitting,
        ),
        AppSpacing.gapLg,

        AppTextField(
          label: 'İletişim telefonu',
          required: true,
          hint: '532 111 00 01',
          helper: 'Alıcılar bu numaradan arayacak ya da WhatsApp yazacak.',
          controller: _phone,
          prefixIcon: Icons.phone_outlined,
          prefixText: AppPhone.countryCode,
          keyboardType: TextInputType.phone,
          inputFormatters: const [PhoneInputFormatter()],
          errorText: _phoneError,
          enabled: !_submitting,
          onChanged: (_) {
            if (_phoneError != null) setState(() => _phoneError = null);
          },
        ),

        // --- Kategoriye özel alanlar ---
        ...switch (propertiesAsync) {
          AsyncData(value: final items) when items.isNotEmpty => [
            AppSpacing.gapXl,
            Text(
              '${_categoryName ?? 'Kategori'} özellikleri',
              style: theme.textTheme.titleSmall,
            ),
            AppSpacing.gapXs,
            Text(
              'Bu bilgiler ilan detayında tablo olarak görünür ve alıcıların '
              'aradığını bulmasını kolaylaştırır.',
              style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
            ),
            AppSpacing.gapLg,
            for (final property in items)
              if (property.isUsable) ...[
                AdPropertyField(
                  property: property,
                  value: _propertyValues[property.id],
                  controller: _controllerFor(property),
                  errorText: _propertyErrors[property.id],
                  enabled: !_submitting,
                  onChanged: (value) => setState(() {
                    if (value == null) {
                      _propertyValues.remove(property.id);
                    } else {
                      _propertyValues[property.id] = value;
                    }
                    _propertyErrors.remove(property.id);
                    _dirty = true;
                  }),
                ),
                AppSpacing.gapLg,
              ],
          ],
          AsyncLoading() => const [
            AppSpacing.gapXl,
            SkeletonBox(height: 64),
            AppSpacing.gapSm,
            SkeletonBox(height: 64),
          ],
          // Özellikler alınamazsa form çalışmaya devam eder (zorunlu alan
          // varsa sunucu 400 verir ve mesajı ekranda görünür) — 11.6/11.7'deki
          // "çalışmayan bölüm hiç çizilmez" kararının aynısı.
          _ => const <Widget>[],
        },
      ],
    );
  }

  TextEditingController? _controllerFor(CategoryProperty property) {
    if (property.kind != AdPropertyKind.text &&
        property.kind != AdPropertyKind.number) {
      return null;
    }
    return _propertyControllers.putIfAbsent(
      property.id,
      () => TextEditingController(text: _propertyValues[property.id] ?? ''),
    );
  }

  // --- 3. adım: fotoğraflar + özet ---

  Widget _photosStep(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (_generalError != null) ...[
          InfoBanner(tone: InfoBannerTone.danger, message: _generalError!),
          AppSpacing.gapLg,
        ],

        AdImagePickerGrid(
          images: _images,
          maxImages: maxImages,
          enabled: !_submitting,
          onAdd: _pickImages,
          onRemove: _removeImage,
          onMakeCover: _makeCover,
        ),
        AppSpacing.gapXl,

        Text('Özet', style: theme.textTheme.titleSmall),
        AppSpacing.gapSm,
        AppCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _SummaryRow(label: 'Kategori', value: _categoryName ?? '—'),
              _SummaryRow(label: 'Başlık', value: _title.text.trim()),
              _SummaryRow(
                label: 'Fiyat',
                value: AppMoney.price(AppMoney.parse(_price.text)),
              ),
              _SummaryRow(
                label: 'Telefon',
                value: AppPhone.display(_phone.text),
                isLast: true,
              ),
            ],
          ),
        ),
        AppSpacing.gapLg,

        if (_submitting && _uploadTotal > 0) ...[
          InfoBanner(
            tone: InfoBannerTone.info,
            message: 'Fotoğraflar yükleniyor: $_uploaded / $_uploadTotal',
          ),
          AppSpacing.gapLg,
        ],

        InfoBanner(
          tone: InfoBannerTone.info,
          message: widget.isEdit
              ? 'Güncellenen ilan yeniden onaya düşer; onaylandığında yayına '
                    'geri döner.'
              : 'İlanınız yönetici onayından sonra yayınlanır. Yayın süresi '
                    '30 gündür, bitmeden uzatabilirsiniz.',
        ),
        AppSpacing.gapMd,
        Text(
          'İlan verirken yürürlükteki kurallara uyduğunuzu kabul etmiş '
          'olursunuz. Yanıltıcı içerik ve yasak ürünler reddedilir.',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
        ),
      ],
    );
  }
}

/// Adım göstergesi — kullanıcı kaç adım kaldığını görmeli (uzun form
/// korkutmasın).
class _StepIndicator extends StatelessWidget {
  const _StepIndicator({
    required this.steps,
    required this.current,
    required this.labels,
  });

  final List<int> steps;
  final int current;
  final Map<int, String> labels;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.md,
        AppSpacing.lg,
        AppSpacing.sm,
      ),
      child: Row(
        children: [
          for (final step in steps) ...[
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    height: 4,
                    decoration: BoxDecoration(
                      color: step <= current
                          ? theme.colorScheme.primary
                          : palette.border,
                      borderRadius: AppRadius.rPill,
                    ),
                  ),
                  AppSpacing.gapXs,
                  Text(
                    labels[step] ?? '',
                    style: theme.textTheme.labelSmall?.copyWith(
                      color: step == current
                          ? theme.colorScheme.primary
                          : palette.muted,
                      fontWeight: step == current
                          ? FontWeight.w700
                          : FontWeight.w600,
                    ),
                  ),
                ],
              ),
            ),
            if (step != steps.last) AppSpacing.wGapSm,
          ],
        ],
      ),
    );
  }
}

/// 1. adım: kategori seçimi (kök → alt kategori).
class _CategoryStep extends ConsumerWidget {
  const _CategoryStep({
    required this.openRoot,
    required this.onOpenRoot,
    required this.onSelect,
  });

  final AdCategory? openRoot;
  final ValueChanged<AdCategory?> onOpenRoot;
  final void Function(AdCategory category, {String? rootId}) onSelect;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final root = openRoot;

    final categories = root == null
        ? ref.watch(adRootCategoriesProvider)
        : ref.watch(adSubCategoriesProvider(root.id));

    return switch (categories) {
      AsyncData(value: final items) => ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.md,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Text(
            root == null
                ? 'İlanınız hangi kategoride?'
                : '${root.name} içinde bir alt kategori seçin',
            style: theme.textTheme.titleMedium,
          ),
          AppSpacing.gapXs,
          Text(
            'Doğru kategori, ilanınızın aranırken bulunmasını sağlar.',
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
          AppSpacing.gapLg,

          if (root != null) ...[
            _CategoryTile(
              icon: Icons.arrow_back_rounded,
              label: 'Tüm kategoriler',
              onTap: () => onOpenRoot(null),
            ),
            AppSpacing.gapSm,
            // Kök kategoriye doğrudan ilan vermek serbest (sunucu alt kategori
            // zorunlu tutmuyor) — ama alt kategori seçmek önerilir.
            _CategoryTile(
              icon: root.materialIcon,
              label: '${root.name} (genel)',
              subtitle: 'Alt kategori seçmeden bu kategoriye ver',
              onTap: () => onSelect(root, rootId: root.id),
            ),
            AppSpacing.gapMd,
          ],

          for (final category in items) ...[
            _CategoryTile(
              icon: category.materialIcon,
              label: category.name,
              trailing: category.hasSubCategories
                  ? Icons.chevron_right_rounded
                  : null,
              onTap: category.hasSubCategories
                  ? () => onOpenRoot(category)
                  : () => onSelect(category, rootId: root?.id ?? category.id),
            ),
            AppSpacing.gapSm,
          ],

          if (items.isEmpty)
            EmptyView(
              icon: Icons.category_outlined,
              title: 'Kategori bulunamadı',
              message: root == null
                  ? 'Kategoriler henüz tanımlanmamış.'
                  : 'Bu kategoride alt kategori yok.',
              actionLabel: root == null ? null : 'Geri dön',
              onAction: root == null ? null : () => onOpenRoot(null),
            ),
        ],
      ),
      AsyncError(:final error) => ErrorView(
        message: error is ApiException
            ? error.message
            : 'Kategoriler alınamadı.',
        onRetry: () => root == null
            ? ref.invalidate(adRootCategoriesProvider)
            : ref.invalidate(adSubCategoriesProvider(root.id)),
      ),
      _ => const LoadingView(itemCount: 6),
    };
  }
}

class _CategoryTile extends StatelessWidget {
  const _CategoryTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.subtitle,
    this.trailing,
  });

  final IconData icon;
  final String label;
  final String? subtitle;
  final IconData? trailing;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Material(
      color: theme.colorScheme.surface,
      shape: RoundedRectangleBorder(
        borderRadius: AppRadius.rMd,
        side: BorderSide(color: palette.border),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: AppRadius.rMd,
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.md,
          ),
          child: Row(
            children: [
              Icon(icon, size: 22, color: theme.colorScheme.primary),
              AppSpacing.wGapMd,
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(label, style: theme.textTheme.bodyLarge),
                    if (subtitle != null) ...[
                      AppSpacing.gapXs,
                      Text(
                        subtitle!,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: palette.muted,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
              Icon(
                trailing ?? Icons.chevron_right_rounded,
                size: 20,
                color: palette.muted,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _CategorySummary extends StatelessWidget {
  const _CategorySummary({
    required this.name,
    required this.locked,
    required this.onChange,
  });

  final String name;
  final bool locked;
  final VoidCallback? onChange;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return AppCard(
      child: Row(
        children: [
          Icon(
            locked ? Icons.lock_outline_rounded : Icons.category_outlined,
            size: 20,
            color: theme.colorScheme.primary,
          ),
          AppSpacing.wGapMd,
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Kategori',
                  style: theme.textTheme.labelSmall?.copyWith(color: palette.muted),
                ),
                AppSpacing.gapXs,
                Text(name, style: theme.textTheme.bodyLarge),
                if (locked) ...[
                  AppSpacing.gapXs,
                  Text(
                    'İlanın kategorisi sonradan değiştirilemez.',
                    style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                  ),
                ],
              ],
            ),
          ),
          if (onChange != null)
            TextButton(onPressed: onChange, child: const Text('Değiştir')),
        ],
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({
    required this.label,
    required this.value,
    this.isLast = false,
  });

  final String label;
  final String value;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      decoration: isLast
          ? null
          : BoxDecoration(
              border: Border(bottom: BorderSide(color: theme.palette.border)),
            ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          ),
          AppSpacing.wGapMd,
          Expanded(
            flex: 2,
            child: Text(
              value.isEmpty ? '—' : value,
              textAlign: TextAlign.end,
              style: theme.textTheme.bodyMedium?.copyWith(
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
