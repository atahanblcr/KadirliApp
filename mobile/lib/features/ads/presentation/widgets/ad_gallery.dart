import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';

/// İlan görsel galerisi: yatay kaydırmalı görseller + sayfa noktaları +
/// "3 / 7" sayacı; dokununca tam ekran görüntüleyici açılır.
///
/// Görsel yoksa (Kadirli'de fotoğrafsız ilan sık) kırık görsel yerine nötr
/// bir yer tutucu çizilir — ekran boş beyaz kalmaz.
class AdGallery extends StatefulWidget {
  const AdGallery({super.key, required this.imageUrls, required this.title});

  final List<String> imageUrls;
  final String title;

  static const double height = 260;

  @override
  State<AdGallery> createState() => _AdGalleryState();
}

class _AdGalleryState extends State<AdGallery> {
  late final PageController _controller = PageController();
  int _index = 0;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _openViewer(int initialIndex) {
    // 🐛 Canlıda (iOS) yakalandı: sekmenin kendi Navigator'ına push edilince
    // görüntüleyicinin altında **alt sekme çubuğu görünmeye devam ediyordu**
    // — "tam ekran" olmuyordu. Kök Navigator kabuğun tamamının üstüne çıkar.
    Navigator.of(context, rootNavigator: true).push(
      MaterialPageRoute<void>(
        fullscreenDialog: true,
        builder: (_) => AdGalleryViewer(
          imageUrls: widget.imageUrls,
          title: widget.title,
          initialIndex: initialIndex,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (widget.imageUrls.isEmpty) {
      return Container(
        height: AdGallery.height,
        color: theme.colorScheme.surfaceContainerHighest,
        alignment: Alignment.center,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              Icons.image_not_supported_outlined,
              size: 40,
              color: theme.palette.muted,
            ),
            AppSpacing.gapSm,
            Text(
              'Bu ilanda fotoğraf yok',
              style: theme.textTheme.bodySmall?.copyWith(
                color: theme.palette.muted,
              ),
            ),
          ],
        ),
      );
    }

    return SizedBox(
      height: AdGallery.height,
      child: Stack(
        children: [
          PageView.builder(
            controller: _controller,
            itemCount: widget.imageUrls.length,
            onPageChanged: (index) => setState(() => _index = index),
            itemBuilder: (context, index) => Semantics(
              button: true,
              label:
                  '${widget.title} fotoğrafı ${index + 1}, büyütmek için dokun',
              child: GestureDetector(
                onTap: () => _openViewer(index),
                child: AppNetworkImage(
                  url: widget.imageUrls[index],
                  width: double.infinity,
                  height: AdGallery.height,
                  borderRadius: BorderRadius.zero,
                ),
              ),
            ),
          ),
          if (widget.imageUrls.length > 1) ...[
            PositionedDirectional(
              bottom: AppSpacing.md,
              start: 0,
              end: 0,
              child: _Dots(count: widget.imageUrls.length, index: _index),
            ),
            PositionedDirectional(
              top: AppSpacing.md,
              end: AppSpacing.md,
              child: _Counter(
                label: '${_index + 1} / ${widget.imageUrls.length}',
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _Dots extends StatelessWidget {
  const _Dots({required this.count, required this.index});

  final int count;
  final int index;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        for (var i = 0; i < count; i++)
          AnimatedContainer(
            duration: AppDurations.fast,
            margin: const EdgeInsets.symmetric(horizontal: 3),
            width: i == index ? 18 : 6,
            height: 6,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: i == index ? 0.95 : 0.55),
              borderRadius: AppRadius.rPill,
            ),
          ),
      ],
    );
  }
}

class _Counter extends StatelessWidget {
  const _Counter({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xxs,
      ),
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.55),
        borderRadius: AppRadius.rPill,
      ),
      child: Text(
        label,
        style: Theme.of(
          context,
        ).textTheme.labelSmall?.copyWith(color: Colors.white),
      ),
    );
  }
}

/// Tam ekran görüntüleyici — kaydırarak gezinme + çift dokunuş/parmakla
/// yakınlaştırma (`InteractiveViewer`; ek paket yok).
class AdGalleryViewer extends StatefulWidget {
  const AdGalleryViewer({
    super.key,
    required this.imageUrls,
    required this.title,
    this.initialIndex = 0,
  });

  final List<String> imageUrls;
  final String title;
  final int initialIndex;

  @override
  State<AdGalleryViewer> createState() => _AdGalleryViewerState();
}

class _AdGalleryViewerState extends State<AdGalleryViewer> {
  late final PageController _controller = PageController(
    initialPage: widget.initialIndex,
  );
  late int _index = widget.initialIndex;

  /// 🐛 Canlıda yakalandı: `InteractiveViewer` varsayılan olarak **her** yatay
  /// sürüklemeyi yutuyor → tam ekranda fotoğraflar arasında geçilemiyordu
  /// (satır içi galeride sorun yok, orada InteractiveViewer yok).
  /// Çözüm: kaydırma yalnız **yakınlaştırılmışken** açık; 1x'te sürükleme
  /// `PageView`'a gider.
  final _transformation = TransformationController();
  bool _isZoomed = false;

  @override
  void initState() {
    super.initState();
    _transformation.addListener(_onTransform);
  }

  void _onTransform() {
    final zoomed = _transformation.value.getMaxScaleOnAxis() > 1.01;
    if (zoomed != _isZoomed) setState(() => _isZoomed = zoomed);
  }

  @override
  void dispose() {
    _transformation.removeListener(_onTransform);
    _transformation.dispose();
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        // 🐛 Canlıda yakalandı: `foregroundColor` başlığın rengini
        // değiştirmiyor — uygulama temasının `titleTextStyle`'ı kazanıyor ve
        // siyah zeminde koyu başlık okunmuyordu.
        titleTextStyle: theme.appBarTheme.titleTextStyle?.copyWith(
          color: Colors.white,
        ),
        title: Text('${_index + 1} / ${widget.imageUrls.length}'),
      ),
      body: PageView.builder(
        controller: _controller,
        itemCount: widget.imageUrls.length,
        // Yakınlaştırılmışken sayfa geçişi kilitlenir, sürükleme resmi gezdirir.
        physics: _isZoomed
            ? const NeverScrollableScrollPhysics()
            : const PageScrollPhysics(),
        onPageChanged: (index) {
          _transformation.value = Matrix4.identity(); // yeni fotoğraf 1x açılır
          setState(() => _index = index);
        },
        itemBuilder: (context, index) => InteractiveViewer(
          transformationController: index == _index ? _transformation : null,
          panEnabled: _isZoomed,
          minScale: 1,
          maxScale: 4,
          child: Center(
            child: AppNetworkImage(
              url: widget.imageUrls[index],
              fit: BoxFit.contain,
              borderRadius: BorderRadius.zero,
            ),
          ),
        ),
      ),
    );
  }
}
