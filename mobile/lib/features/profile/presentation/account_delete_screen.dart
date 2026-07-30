import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../data/profile_repository.dart';

/// Hesabı sil — `DELETE /v1/users/me`.
///
/// Mağaza zorunluluğu (App Store 5.1.1(v) / Google Play): hesap silme
/// uygulamanın **içinden** ve bulunabilir bir yerden yapılabilmeli. Bu yüzden
/// diyalog değil, sonuçları tek tek anlatan ayrı bir ekran: kullanıcı neyi
/// kaybettiğini görerek onaylar.
///
/// Backend davranışı (10.8): soft delete + anonimleştirme — telefon yeniden
/// kayda açılır, ilanlar yayından düşer, favoriler silinir; vefat ilanı /
/// şikayet gibi topluluk içeriği "silinmiş kullanıcı" olarak kalır.
class AccountDeleteScreen extends ConsumerStatefulWidget {
  const AccountDeleteScreen({super.key});

  @override
  ConsumerState<AccountDeleteScreen> createState() => _AccountDeleteScreenState();
}

class _AccountDeleteScreenState extends ConsumerState<AccountDeleteScreen> {
  bool _deleting = false;
  String? _error;

  Future<void> _delete() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Hesabınız silinsin mi?'),
        content: const Text(
          'Bu işlem geri alınamaz. Hesabınız ve ilanlarınız yayından '
          'kaldırılacak.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(
              'Evet, sil',
              style: TextStyle(color: Theme.of(context).palette.danger),
            ),
          ),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() {
      _deleting = true;
      _error = null;
    });

    try {
      // Refresh token gövdeye konur → sunucu o token'ın jti'sini iptal eder.
      final refreshToken = await ref.read(tokenStoreProvider).readRefreshToken();
      await ref.read(profileRepositoryProvider).deleteAccount(refreshToken: refreshToken);

      // Sunucu tarafı bitti → yerel oturum kapatılır (çıkış ucu ÇAĞRILMAZ:
      // hesap artık pasif, `logout` 401 dönerdi).
      await ref.read(authControllerProvider.notifier).completeAccountDeletion();
      ref.read(authNoticeProvider.notifier).set(
        'Hesabınız silindi. Aynı numarayla dilediğiniz zaman yeniden '
        'kayıt olabilirsiniz.',
      );
      // Yönlendirmeyi router yapar (anonim + misafir tercihi sıfır → Giriş).
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _deleting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final user = ref.watch(currentUserProvider);

    if (user == null) {
      // Oturum bu ekrandayken kapandı (silme tamamlandı). ⚠️ Yalnız router'ın
      // yönlendirmesine güvenmek YETMİYOR: bu ekran `context.push` ile
      // yığının üstüne binmiş, redirect ise altındaki konumu değiştiriyor →
      // kullanıcı sonsuz spinner'da kalıyordu (canlıda yakalandı). `go`
      // yığını komple değiştirir; nereye gidileceğine yine router karar verir
      // (anonim + misafir tercihi sıfır → Giriş).
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) context.go(AppRoutes.home);
      });
      return const AppScaffold(title: 'Hesabı sil', body: LoadingView.compact());
    }

    return AppScaffold(
      title: 'Hesabı sil',
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          Icon(
            Icons.warning_amber_rounded,
            size: 48,
            color: palette.danger,
          ),
          AppSpacing.gapLg,
          Text(
            'Hesabınızı silmek üzeresiniz',
            style: theme.textTheme.titleMedium,
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapSm,
          Text(
            'Silme işlemi geri alınamaz. Devam etmeden önce lütfen aşağıdakileri '
            'okuyun.',
            style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
            textAlign: TextAlign.center,
          ),
          AppSpacing.gapXl,

          if (_error != null) ...[
            InfoBanner(tone: InfoBannerTone.danger, message: _error!),
            AppSpacing.gapLg,
          ],

          if (!user.isStandardUser) ...[
            const InfoBanner(
              tone: InfoBannerTone.warning,
              message:
                  'Yönetici ve personel hesapları uygulamadan silinemez; '
                  'bu işlem yönetim panelinden yapılır.',
            ),
            AppSpacing.gapLg,
          ],

          AppCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: const [
                _Consequence(
                  icon: Icons.person_off_outlined,
                  text: 'Profiliniz, kullanıcı adınız ve fotoğrafınız silinir.',
                ),
                _Consequence(
                  icon: Icons.sell_outlined,
                  text: 'İlanlarınız yayından kaldırılır.',
                ),
                _Consequence(
                  icon: Icons.favorite_border_rounded,
                  text: 'Favorileriniz ve bildirim tercihleriniz silinir.',
                ),
                _Consequence(
                  icon: Icons.groups_outlined,
                  text:
                      'Paylaştığınız vefat ilanı ve şikayet kayıtları toplulukta '
                      'kalır, ancak adınızla ilişkilendirilmez.',
                ),
                _Consequence(
                  icon: Icons.phone_iphone_rounded,
                  text:
                      'Telefon numaranız serbest kalır — dilediğiniz zaman '
                      'yeniden kayıt olabilirsiniz.',
                  last: true,
                ),
              ],
            ),
          ),
          AppSpacing.gapXl,

          AppButton.danger(
            label: 'Hesabımı sil',
            icon: Icons.delete_forever_rounded,
            expand: true,
            loading: _deleting,
            onPressed: _deleting || !user.isStandardUser ? null : _delete,
          ),
          AppSpacing.gapSm,
          Center(
            child: TextButton(
              onPressed: _deleting ? null : () => Navigator.of(context).maybePop(),
              child: const Text('Vazgeç'),
            ),
          ),
        ],
      ),
    );
  }
}

class _Consequence extends StatelessWidget {
  const _Consequence({required this.icon, required this.text, this.last = false});

  final IconData icon;
  final String text;
  final bool last;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: EdgeInsets.only(bottom: last ? 0 : AppSpacing.md),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: theme.palette.muted),
          AppSpacing.wGapMd,
          Expanded(child: Text(text, style: theme.textTheme.bodyMedium)),
        ],
      ),
    );
  }
}
