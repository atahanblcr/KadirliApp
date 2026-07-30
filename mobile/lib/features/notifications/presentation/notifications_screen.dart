import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../../auth/presentation/widgets/sign_in_prompt.dart';
import '../application/unread_count_provider.dart';

/// Bildirimler sekmesi (11.4 iskelet → 11.13 tam).
///
/// Bugün yaptığı iş: oturum durumuna göre doğru şeyi göstermek ve rozeti
/// besleyen sayıyı doğrulamak. Liste, okundu işaretleme ve push deep-link'i
/// 11.13'te gelecek.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isAuthenticated = ref.watch(
      authControllerProvider.select((state) => state.isAuthenticated),
    );

    if (!isAuthenticated) {
      return const AppScaffold(
        title: 'Bildirimler',
        showBackButton: false,
        body: SignInPrompt(
          icon: Icons.notifications_active_outlined,
          title: 'Bildirimleriniz burada olacak',
          message:
              'Duyurular, nöbetçi eczane ve ilanlarınızla ilgili gelişmeleri '
              'kaçırmamak için giriş yapın.',
        ),
      );
    }

    final unread = ref.watch(unreadNotificationCountProvider);

    return AppScaffold(
      title: 'Bildirimler',
      showBackButton: false,
      onRefresh: () async {
        ref.invalidate(unreadNotificationCountProvider);
        await ref.read(unreadNotificationCountProvider.future);
      },
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Center(
            child: Container(
              padding: const EdgeInsets.all(AppSpacing.xl),
              decoration: BoxDecoration(
                color: theme.colorScheme.primaryContainer,
                shape: BoxShape.circle,
              ),
              child: Icon(
                Icons.notifications_none_rounded,
                size: 40,
                color: theme.colorScheme.onPrimaryContainer,
              ),
            ),
          ),
          AppSpacing.gapXl,
          Text(
            switch (unread.value) {
              null || 0 => 'Yeni bildirim yok',
              final count => '$count okunmamış bildirim',
            },
            style: theme.textTheme.titleMedium,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapSm,
          Text(
            'Bildirim listesi ve okundu işaretleme uygulamanın 11.13 sürümüyle '
            'açılacak. Rozetteki sayı şimdiden gerçek verilerden geliyor.',
            style: theme.textTheme.bodyMedium?.copyWith(color: theme.palette.muted),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
