import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import 'app_network_image.dart';

/// Kullanıcı avatarı — fotoğraf varsa yuvarlak görsel, yoksa baş harf.
///
/// Tek bileşen olmasının sebebi: profil fotoğrafı Ayarlar, Profil sekmesi ve
/// (ileride) yorum/ilan sahibi satırlarında aynı görünmeli. URL göreli gelir →
/// [AppNetworkImage] mutlaklaştırır.
class UserAvatar extends StatelessWidget {
  const UserAvatar({
    super.key,
    required this.initial,
    this.photoUrl,
    this.radius = 24,
    this.onTap,
  });

  final String initial;
  final String? photoUrl;
  final double radius;

  /// Verilirse avatar dokunulabilir olur (profil fotoğrafı değiştirme).
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final size = radius * 2;
    final hasPhoto = photoUrl != null && photoUrl!.trim().isNotEmpty;

    final Widget content = hasPhoto
        ? AppNetworkImage(
            url: photoUrl,
            width: size,
            height: size,
            borderRadius: BorderRadius.circular(radius),
            fallbackIcon: Icons.person_outline_rounded,
          )
        : Container(
            width: size,
            height: size,
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(
                initial,
                style: (radius >= 32
                        ? theme.textTheme.headlineSmall
                        : theme.textTheme.titleMedium)
                    ?.copyWith(color: theme.colorScheme.onPrimaryContainer),
              ),
            ),
          );

    if (onTap == null) return content;

    return Stack(
      alignment: Alignment.center,
      children: [
        ClipOval(
          child: Material(
            color: Colors.transparent,
            child: InkWell(
              onTap: onTap,
              child: SizedBox(width: size, height: size, child: content),
            ),
          ),
        ),
        // Küçük "düzenle" rozeti: fotoğrafın değiştirilebildiği görünür olsun
        // (yalnız dokunma alanı ipucu vermiyor).
        PositionedDirectional(
          bottom: 0,
          end: 0,
          child: IgnorePointer(
            child: Container(
              padding: const EdgeInsets.all(AppSpacing.xs),
              decoration: BoxDecoration(
                color: theme.colorScheme.primary,
                shape: BoxShape.circle,
                border: Border.all(color: theme.colorScheme.surface, width: 2),
              ),
              child: Icon(
                Icons.photo_camera_rounded,
                size: 14,
                color: theme.colorScheme.onPrimary,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

/// Avatar + ad + ikincil satır (telefon/mahalle) — kart başlıklarının ortak
/// kalıbı (Ayarlar "Hesap" kartı, Profil sekmesi).
class UserIdentityRow extends StatelessWidget {
  const UserIdentityRow({
    super.key,
    required this.initial,
    required this.name,
    this.photoUrl,
    this.subtitle,
    this.trailing,
  });

  final String initial;
  final String name;
  final String? photoUrl;
  final String? subtitle;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        UserAvatar(initial: initial, photoUrl: photoUrl),
        AppSpacing.wGapMd,
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(name, style: theme.textTheme.titleSmall),
              if (subtitle != null)
                Text(
                  subtitle!,
                  style: theme.textTheme.bodySmall?.copyWith(color: theme.palette.muted),
                ),
            ],
          ),
        ),
        ?trailing,
      ],
    );
  }
}
