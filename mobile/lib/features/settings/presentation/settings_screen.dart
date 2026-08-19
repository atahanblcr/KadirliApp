import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/app_info.dart';
import '../../../core/config/env.dart';
import '../../../core/preferences/app_preferences.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import 'widgets/notification_preferences_card.dart';
import 'widgets/theme_mode_selector.dart';

/// Ayarlar / Kontrol ekranı — "uygulamanın kontrol merkezi"
/// (MOBILE_UX_PLAN §5, kullanıcının özel isteği).
///
/// Bölümler: **Hesap** (profil özeti + düzenleme) · **Bildirimler** (6 anahtar,
/// `PATCH /v1/users/me/notifications`) · **Görünüm** (tema) · **Hakkında**
/// (sürüm + şikayet) · **Hesap işlemleri** (çıkış + hesabı sil) · Geliştirici.
class SettingsScreen extends ConsumerWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);
    final preferencesDegraded = ref.watch(preferencesDegradedProvider);

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
          // 🔴 12.23: tercih deposu açılamadı → yazılanlar uygulama kapanınca
          // kaybolacak. Söylenmeseydi kullanıcı bir haberi kaydeder, yer imi
          // dolar, ertesi gün liste boş olurdu — ve hiçbir yerde sebebi yazmazdı.
          if (preferencesDegraded) ...[
            const InfoBanner(
              tone: InfoBannerTone.warning,
              icon: Icons.sync_problem_rounded,
              title: 'Tercihleriniz kaydedilemiyor',
              message:
                  'Cihazın ayar deposu açılamadı. Tema, okuma boyutu ve '
                  'kaydettiğiniz haberler bu oturumda tutulur ama uygulamayı '
                  'kapatınca kaybolur. Uygulamayı yeniden başlatmayı deneyin.',
            ),
            AppSpacing.gapXl,
          ],
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
                  UserIdentityRow(
                    initial: user.initial,
                    name: user.displayName,
                    photoUrl: user.profilePhotoUrl,
                    subtitle: AppPhone.display(user.phone),
                  ),
                  if (user.primaryNeighborhoodName != null) ...[
                    AppSpacing.gapMd,
                    Row(
                      children: [
                        Icon(Icons.place_outlined, size: 16, color: palette.muted),
                        AppSpacing.wGapSm,
                        Text(
                          user.primaryNeighborhoodName!,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: palette.muted,
                          ),
                        ),
                      ],
                    ),
                  ],
                  AppSpacing.gapLg,
                  AppButton.ghost(
                    label: 'Profili düzenle',
                    icon: Icons.edit_outlined,
                    expand: true,
                    onPressed: () => context.push(AppRoutes.profileEdit),
                  ),
                ],
              ),
            ),

          if (user != null) ...[
            AppSpacing.gapXl,
            const SectionHeader(
              title: 'Bildirimler',
              subtitle: 'Hangi konularda bildirim almak istediğinizi seçin.',
            ),
            const NotificationPreferencesCard(),
          ],
          AppSpacing.gapXl,

          const SectionHeader(title: 'Görünüm'),
          const ThemeModeSelector(),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Hakkında'),
          const _AboutCard(),

          if (user != null) ...[
            AppSpacing.gapXl,
            const SectionHeader(title: 'Hesap işlemleri'),
            AppCard(
              padding: EdgeInsets.zero,
              child: Column(
                children: [
                  const _LogoutTile(),
                  Divider(height: 1, thickness: 1, color: palette.border),
                  ListTile(
                    leading: Icon(Icons.delete_outline_rounded, color: palette.danger),
                    title: Text(
                      'Hesabı sil',
                      style: theme.textTheme.bodyLarge?.copyWith(color: palette.danger),
                    ),
                    subtitle: Text(
                      'Hesabınız ve ilanlarınız kaldırılır',
                      style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
                    ),
                    onTap: () => context.push(AppRoutes.accountDelete),
                  ),
                ],
              ),
            ),
          ],

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

/// Uygulama tanıtımı + **sürüm** (11.15 mağaza yayınında otomatik doğru olur)
/// + şikayet kısayolu.
class _AboutCard extends ConsumerWidget {
  const _AboutCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final info = ref.watch(appInfoProvider);

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text('Kadirli', style: theme.textTheme.titleSmall)),
              Text(
                'Sürüm ${info.value?.display ?? AppInfo.unknown.display}',
                style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
              ),
            ],
          ),
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
          AppSpacing.gapSm,
          // Mağaza zorunluluğu (Faz 11.16): gizlilik politikasına uygulamanın
          // İÇİNDEN de erişilebilmeli — yalnız mağaza sayfasında olması yetmez.
          //
          // 🔑 12.17: hedef artık **uygulama içi** ekran. Web bağlantısı
          // silinmedi, ekranın **boş** dalına taşındı: hiçbir metin
          // yayınlanmamışken bu butonu kaldırmak mağaza şartını kırardı.
          AppButton.ghost(
            label: 'Yasal metinler',
            icon: Icons.privacy_tip_outlined,
            expand: true,
            onPressed: () => context.push(AppRoutes.legal),
          ),
        ],
      ),
    );
  }
}

/// Çıkış: onay diyaloğu + `POST /v1/auth/logout` (refresh iptal + FCM temizlik).
class _LogoutTile extends ConsumerStatefulWidget {
  const _LogoutTile();

  @override
  ConsumerState<_LogoutTile> createState() => _LogoutTileState();
}

class _LogoutTileState extends ConsumerState<_LogoutTile> {
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
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListTile(
      leading: _loggingOut
          ? const SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : Icon(Icons.logout_rounded, color: theme.colorScheme.primary),
      title: Text('Çıkış yap', style: theme.textTheme.bodyLarge),
      onTap: _loggingOut ? null : _logout,
    );
  }
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
