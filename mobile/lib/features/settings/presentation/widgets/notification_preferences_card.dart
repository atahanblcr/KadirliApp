import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/widgets/widgets.dart';
import '../../../auth/application/auth_controller.dart';
import '../../../auth/data/models/notification_preferences.dart';
import '../../application/notification_preferences_controller.dart';

/// Altı bildirim anahtarı — `PATCH /v1/users/me/notifications`.
///
/// Kaydet butonu yok: her anahtar dokunulduğu an yazılır (iyimser güncelleme,
/// hata olursa geri alınır). Liste [NotificationTopic] enum'undan üretilir.
class NotificationPreferencesCard extends ConsumerWidget {
  const NotificationPreferencesCard({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);
    final state = ref.watch(notificationPreferencesProvider);

    if (user == null) {
      return AppCard(
        child: Text(
          'Bildirim tercihlerinizi ayarlamak için giriş yapmanız gerekiyor.',
          style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
        ),
      );
    }

    final preferences = user.notificationPreferences;

    return Column(
      children: [
        if (state.error != null) ...[
          InfoBanner(
            tone: InfoBannerTone.danger,
            message: state.error!,
            onClose: () => ref.read(notificationPreferencesProvider.notifier).clearError(),
          ),
          AppSpacing.gapSm,
        ],
        AppCard(
          padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
          child: Column(
            children: [
              for (final topic in NotificationTopic.values)
                _PreferenceRow(
                  topic: topic,
                  value: preferences.valueOf(topic),
                  pending: state.isPending(topic),
                  onChanged: (value) => ref
                      .read(notificationPreferencesProvider.notifier)
                      .toggle(topic, value),
                  showDivider: topic != NotificationTopic.values.last,
                ),
            ],
          ),
        ),
      ],
    );
  }
}

class _PreferenceRow extends StatelessWidget {
  const _PreferenceRow({
    required this.topic,
    required this.value,
    required this.pending,
    required this.onChanged,
    required this.showDivider,
  });

  final NotificationTopic topic;
  final bool value;
  final bool pending;
  final ValueChanged<bool> onChanged;
  final bool showDivider;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return Column(
      children: [
        SwitchListTile.adaptive(
          value: value,
          // Yazma sürerken kilitli: art arda dokunuşta istekler yarışmasın.
          onChanged: pending ? null : onChanged,
          contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
          secondary: pending
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : Icon(topic.icon, size: 22, color: theme.colorScheme.primary),
          title: Text(topic.label, style: theme.textTheme.bodyLarge),
          subtitle: Text(
            topic.description,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ),
        if (showDivider)
          Divider(height: 1, thickness: 1, color: palette.border, indent: AppSpacing.lg),
      ],
    );
  }
}
