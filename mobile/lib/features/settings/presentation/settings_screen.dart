import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import 'widgets/theme_mode_selector.dart';

/// Ayarlar / Kontrol ekranı — "uygulamanın kontrol merkezi"
/// (MOBILE_UX_PLAN §5, kullanıcının özel isteği).
///
/// **11.4 kapsamı:** görünüm (tema), hesap (oturum özeti + çıkış), hakkında,
/// geliştirici kısayolları. **11.5** buraya profil düzenleme, bildirim
/// tercihleri (6 anahtar) ve hesap silmeyi ekleyecek.
class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);

    return AppScaffold(
      title: 'Ayarlar',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          const SectionHeader(title: 'Hesap'),
          if (user == null)
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.person_outline_rounded, color: palette.muted),
                      AppSpacing.wGapMd,
                      Expanded(
                        child: Text(
                          'Misafir olarak geziyorsunuz',
                          style: theme.textTheme.titleSmall,
                        ),
                      ),
                    ],
                  ),
                  AppSpacing.gapSm,
                  Text(
                    'İlan vermek, favorilere eklemek ve bildirim almak için '
                    'giriş yapın.',
                    style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                  ),
                  AppSpacing.gapLg,
                  AppButton(
                    label: 'Giriş yap',
                    icon: Icons.login_rounded,
                    expand: true,
                    onPressed: () => context.push(AppRoutes.login),
                  ),
                ],
              ),
            )
          else
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      CircleAvatar(
                        backgroundColor: theme.colorScheme.primaryContainer,
                        child: Text(
                          user.displayName.substring(0, 1).toUpperCase(),
                          style: theme.textTheme.titleMedium?.copyWith(
                            color: theme.colorScheme.onPrimaryContainer,
                          ),
                        ),
                      ),
                      AppSpacing.wGapMd,
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(user.displayName, style: theme.textTheme.titleSmall),
                            Text(
                              AppPhone.display(user.phone),
                              style: theme.textTheme.bodySmall?.copyWith(
                                color: palette.muted,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  AppSpacing.gapLg,
                  const _LogoutButton(),
                ],
              ),
            ),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Görünüm'),
          const ThemeModeSelector(),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Hakkında'),
          AppCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Kadirli', style: theme.textTheme.titleSmall),
                AppSpacing.gapXs,
                Text(
                  'Şehrin duyuruları, nöbetçi eczanesi, ilanları ve daha '
                  'fazlası tek uygulamada.',
                  style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                ),
                AppSpacing.gapLg,
                AppButton.ghost(
                  label: 'Şikayet / İstek bildir',
                  icon: Icons.support_agent_rounded,
                  expand: true,
                  onPressed: () => context.push(AppRoutes.complaints),
                ),
              ],
            ),
          ),

          if (Env.showDevTools) ...[
            AppSpacing.gapXl,
            const SectionHeader(title: 'Geliştirici'),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _InfoRow(label: 'Ortam', value: Env.flavor.name),
                  AppSpacing.gapSm,
                  _InfoRow(label: 'API', value: Env.apiBaseUrl),
                  AppSpacing.gapLg,
                  AppButton.ghost(
                    label: 'Tasarım sistemi önizlemesi',
                    icon: Icons.palette_rounded,
                    expand: true,
                    onPressed: () => context.push(AppRoutes.designPreview),
                  ),
                  AppSpacing.gapSm,
                  AppButton.ghost(
                    label: 'Ağ tanılama',
                    icon: Icons.lan_rounded,
                    expand: true,
                    onPressed: () => context.push(AppRoutes.networkProbe),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// Çıkış: onay diyaloğu + `POST /v1/auth/logout` (refresh iptal + FCM temizlik).
class _LogoutButton extends ConsumerStatefulWidget {
  const _LogoutButton();

  @override
  ConsumerState<_LogoutButton> createState() => _LogoutButtonState();
}

class _LogoutButtonState extends ConsumerState<_LogoutButton> {
  bool _loggingOut = false;

  Future<void> _logout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Çıkış yapılsın mı?'),
        content: const Text(
          'Hesabınızdan çıkacaksınız. Uygulamayı misafir olarak kullanmaya '
          'devam edebilirsiniz.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Çıkış yap'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() => _loggingOut = true);
    await ref.read(authControllerProvider.notifier).logout();
    // Çıkış sonrası router anonim kullanıcıyı Giriş ekranına alır; bu widget
    // ağaçtan kalkmış olabilir.
    if (mounted) setState(() => _loggingOut = false);
  }

  @override
  Widget build(BuildContext context) => AppButton.ghost(
    label: 'Çıkış yap',
    icon: Icons.logout_rounded,
    expand: true,
    loading: _loggingOut,
    onPressed: _loggingOut ? null : _logout,
  );
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 72,
          child: Text(
            label,
            style: theme.textTheme.labelMedium?.copyWith(color: theme.palette.muted),
          ),
        ),
        Expanded(child: Text(value, style: theme.textTheme.bodyMedium)),
      ],
    );
  }
}
