import 'dart:io';

import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../application/ad_submission_service.dart';

/// İlan görselleri ızgarası: seçme, sıralama (kapak seçimi) ve kaldırma.
///
/// **Kapak = ilk görsel.** Sunucu `CreateAdCommandHandler`'da listenin ilk
/// dosyasını `IsCover` işaretliyor; kullanıcıya "kapak" ayrı bir bayrak gibi
/// değil, **sıradaki ilk fotoğraf** olarak anlatılıyor (aynı model, tek kural).
class AdImagePickerGrid extends StatelessWidget {
  const AdImagePickerGrid({
    super.key,
    required this.images,
    required this.maxImages,
    required this.onAdd,
    required this.onRemove,
    required this.onMakeCover,
    this.enabled = true,
  });

  final List<AdFormImage> images;
  final int maxImages;
  final VoidCallback onAdd;
  final void Function(int index) onRemove;
  final void Function(int index) onMakeCover;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final canAdd = enabled && images.length < maxImages;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text('Fotoğraflar', style: theme.textTheme.titleSmall),
            ),
            Text(
              '${images.length}/$maxImages',
              style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
            ),
          ],
        ),
        AppSpacing.gapXs,
        Text(
          images.isEmpty
              ? 'Fotoğraflı ilanlar çok daha fazla görüntüleniyor. En az bir '
                    'fotoğraf eklemenizi öneririz.'
              : 'İlk fotoğraf kapak olarak kullanılır; değiştirmek için '
                    'fotoğrafa dokunun.',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
        ),
        AppSpacing.gapMd,
        GridView.count(
          crossAxisCount: 3,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisSpacing: AppSpacing.sm,
          mainAxisSpacing: AppSpacing.sm,
          children: [
            for (var index = 0; index < images.length; index++)
              _ImageTile(
                key: ValueKey(
                  images[index].adImageId ?? images[index].localPath,
                ),
                image: images[index],
                isCover: index == 0,
                enabled: enabled,
                onRemove: () => onRemove(index),
                onMakeCover: index == 0 ? null : () => onMakeCover(index),
              ),
            if (canAdd) _AddTile(onTap: onAdd),
          ],
        ),
      ],
    );
  }
}

class _ImageTile extends StatelessWidget {
  const _ImageTile({
    super.key,
    required this.image,
    required this.isCover,
    required this.enabled,
    required this.onRemove,
    required this.onMakeCover,
  });

  final AdFormImage image;
  final bool isCover;
  final bool enabled;
  final VoidCallback onRemove;
  final VoidCallback? onMakeCover;

  Future<void> _showActions(BuildContext context) async {
    final action = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (onMakeCover != null)
              ListTile(
                leading: const Icon(Icons.star_outline_rounded),
                title: const Text('Kapak fotoğrafı yap'),
                subtitle: const Text('İlan listesinde bu fotoğraf görünür.'),
                onTap: () => Navigator.of(context).pop('cover'),
              ),
            ListTile(
              leading: Icon(
                Icons.delete_outline_rounded,
                color: Theme.of(context).palette.danger,
              ),
              title: const Text('Fotoğrafı kaldır'),
              onTap: () => Navigator.of(context).pop('remove'),
            ),
          ],
        ),
      ),
    );

    if (action == 'cover') onMakeCover?.call();
    if (action == 'remove') onRemove();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    final Widget picture = image.isExisting
        ? AppNetworkImage(url: image.remoteUrl, fit: BoxFit.cover)
        : Image.file(File(image.localPath!), fit: BoxFit.cover);

    return Semantics(
      button: true,
      label: isCover ? 'Kapak fotoğrafı' : 'İlan fotoğrafı',
      child: InkWell(
        onTap: enabled ? () => _showActions(context) : null,
        borderRadius: AppRadius.rMd,
        child: ClipRRect(
          borderRadius: AppRadius.rMd,
          child: Stack(
            fit: StackFit.expand,
            children: [
              picture,
              if (isCover)
                Positioned(
                  left: 0,
                  right: 0,
                  bottom: 0,
                  child: Container(
                    color: theme.colorScheme.primary.withValues(alpha: 0.92),
                    padding: const EdgeInsets.symmetric(vertical: AppSpacing.xxs),
                    child: Text(
                      'Kapak',
                      textAlign: TextAlign.center,
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: theme.colorScheme.onPrimary,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                ),
              if (enabled)
                PositionedDirectional(
                  top: 2,
                  end: 2,
                  child: Container(
                    decoration: BoxDecoration(
                      color: Colors.black.withValues(alpha: 0.45),
                      shape: BoxShape.circle,
                    ),
                    padding: const EdgeInsets.all(AppSpacing.xxs),
                    child: const Icon(
                      Icons.more_horiz_rounded,
                      size: 16,
                      color: Colors.white,
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _AddTile extends StatelessWidget {
  const _AddTile({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Semantics(
      button: true,
      label: 'Fotoğraf ekle',
      child: Material(
        color: theme.colorScheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.rMd,
          side: BorderSide(color: palette.border),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.rMd,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.add_a_photo_outlined, color: theme.colorScheme.primary),
              AppSpacing.gapXs,
              Text(
                'Ekle',
                style: theme.textTheme.labelMedium?.copyWith(color: palette.muted),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
