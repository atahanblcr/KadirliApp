import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';

/// Tek bir skeleton bloğu (metin satırı, görsel yeri, avatar…).
///
/// "Kullanıcı beklerken skeleton görür, asla boş beyaz ekran değil"
/// (MOBILE_UX_PLAN §4). Shimmer, "hareketi azalt" ayarında durur.
class SkeletonBox extends StatefulWidget {
  const SkeletonBox({
    super.key,
    this.width,
    this.height = 14,
    this.borderRadius = AppRadius.rSm,
    this.shape,
  });

  /// Yuvarlak (avatar/ikon) skeleton.
  const SkeletonBox.circle({super.key, required double size})
    : width = size,
      height = size,
      borderRadius = AppRadius.rPill,
      shape = BoxShape.circle;

  final double? width;
  final double height;
  final BorderRadius borderRadius;
  final BoxShape? shape;

  @override
  State<SkeletonBox> createState() => _SkeletonBoxState();
}

class _SkeletonBoxState extends State<SkeletonBox> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: AppDurations.shimmer,
  );

  @override
  void initState() {
    super.initState();
    _controller.repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).palette;
    final reduceMotion = MediaQuery.disableAnimationsOf(context);
    final isCircle = widget.shape == BoxShape.circle;

    final base = Container(
      width: widget.width,
      height: widget.height,
      decoration: BoxDecoration(
        color: palette.skeletonBase,
        borderRadius: isCircle ? null : widget.borderRadius,
        shape: isCircle ? BoxShape.circle : BoxShape.rectangle,
      ),
    );

    if (reduceMotion) return ExcludeSemantics(child: base);

    return ExcludeSemantics(
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, child) {
          // -1 → 2 aralığında kayan parlama bandı.
          final t = _controller.value * 3 - 1;
          return ShaderMask(
            blendMode: BlendMode.srcATop,
            shaderCallback: (bounds) => LinearGradient(
              begin: Alignment(t - 1, 0),
              end: Alignment(t + 1, 0),
              colors: [
                palette.skeletonBase,
                palette.skeletonHighlight,
                palette.skeletonBase,
              ],
              stops: const [0.35, 0.5, 0.65],
            ).createShader(bounds),
            child: child,
          );
        },
        child: base,
      ),
    );
  }
}

/// Liste ekranlarının varsayılan yükleniyor görünümü: birkaç kart iskeleti.
class SkeletonCardList extends StatelessWidget {
  const SkeletonCardList({
    super.key,
    this.itemCount = 4,
    this.hasImage = true,
    this.padding = const EdgeInsets.all(AppSpacing.lg),
    this.shrinkWrap = false,
  });

  final int itemCount;

  /// Kartta hero görsel yeri var mı (ilan/duyuru kartları).
  final bool hasImage;

  final EdgeInsetsGeometry padding;

  /// ⚠️ **Başka bir kaydırılabilir alanın içinde** kullanılıyorsa (Ana Sayfa
  /// vitrini gibi) `true` verilmeli — yoksa "Vertical viewport was given
  /// unbounded height" hatası alınır.
  final bool shrinkWrap;

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).palette;
    return ListView.separated(
      padding: padding,
      shrinkWrap: shrinkWrap,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: itemCount,
      separatorBuilder: (_, _) => AppSpacing.gapMd,
      itemBuilder: (context, index) => Container(
        padding: const EdgeInsets.all(AppSpacing.lg),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: AppRadius.rMd,
          border: Border.all(color: palette.border),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (hasImage) ...[
              const SkeletonBox(width: 64, height: 64, borderRadius: AppRadius.rSm),
              AppSpacing.wGapMd,
            ],
            const Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SkeletonBox(height: 16, width: double.infinity),
                  AppSpacing.gapSm,
                  SkeletonBox(height: 12, width: 220),
                  AppSpacing.gapSm,
                  SkeletonBox(height: 12, width: 120),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
