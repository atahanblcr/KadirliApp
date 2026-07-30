import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../utils/image_url.dart';
import 'skeleton.dart';

/// Uygulamanın tek uzak görsel bileşeni.
///
/// - URL'i [AppImage.url] ile mutlaklaştırır (sunucu göreli `/uploads/...`
///   döndürüyor — API_CONTRACT §7), böylece çağıranlar unutamaz.
/// - Yüklenirken skeleton, hata/boş URL'de nötr ikon gösterir (kırık görsel
///   simgesi yok).
/// - `cached_network_image` sayesinde liste kaydırmalarında tekrar indirmez.
class AppNetworkImage extends StatelessWidget {
  const AppNetworkImage({
    super.key,
    required this.url,
    this.width,
    this.height,
    this.fit = BoxFit.cover,
    this.borderRadius = AppRadius.rSm,
    this.fallbackIcon = Icons.image_outlined,
  });

  /// Ham (göreli olabilir) URL.
  final String? url;

  final double? width;
  final double? height;
  final BoxFit fit;
  final BorderRadius borderRadius;
  final IconData fallbackIcon;

  @override
  Widget build(BuildContext context) {
    final resolved = AppImage.url(url);

    Widget content;
    if (resolved == null) {
      content = _Fallback(icon: fallbackIcon, width: width, height: height);
    } else {
      content = CachedNetworkImage(
        imageUrl: resolved,
        width: width,
        height: height,
        fit: fit,
        placeholder: (context, _) => SkeletonBox(
          width: width,
          height: height ?? 14,
          borderRadius: borderRadius,
        ),
        errorWidget: (context, _, _) =>
            _Fallback(icon: fallbackIcon, width: width, height: height),
      );
    }

    return ClipRRect(borderRadius: borderRadius, child: content);
  }
}

class _Fallback extends StatelessWidget {
  const _Fallback({required this.icon, this.width, this.height});

  final IconData icon;
  final double? width;
  final double? height;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      width: width,
      height: height,
      color: theme.palette.skeletonBase,
      child: Icon(icon, size: 20, color: theme.palette.muted),
    );
  }
}
