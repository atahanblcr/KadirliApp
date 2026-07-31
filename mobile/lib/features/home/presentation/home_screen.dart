import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/config/env.dart';
import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../../announcements/presentation/widgets/announcement_tile.dart';
import '../../auth/application/auth_controller.dart';
import '../application/home_providers.dart';
import 'widgets/emergency_strip.dart';
import 'widgets/module_grid.dart';

/// Ana Sayfa (Hub) — uygulamanın giriş noktası (MOBILE_UX_PLAN §5).
///
/// Üç katman: **acil şerit** (şu an lazım olan) → **modül ızgarası** (nereye
/// gidebilirim) → **öne çıkanlar** (yeni ne var). Üçü ayrı istek; biri
/// başarısız olursa diğerleri görünmeye devam eder.
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final user = ref.watch(currentUserProvider);

    // ⚠️ Vitrin (`_LatestAnnouncements`) listenin altında kalıyor ve `ListView`
    // onu tembel kuruyor → provider ekran açılışında hiç tetiklenmiyordu.
    // Hub "üç isteklik tek ekran" olarak davranmalı (pull-to-refresh de üçünü
    // tazeliyor), o yüzden duyurular burada da izleniyor.
    ref.watch(latestAnnouncementsProvider);

    return AppScaffold(
      titleWidget: _Greeting(name: user?.displayName),
      showBackButton: false,
      actions: [
        IconButton(
          onPressed: () => context.push(AppRoutes.settings),
          icon: const Icon(Icons.settings_rounded),
          tooltip: 'Ayarlar',
        ),
        AppSpacing.wGapSm,
      ],
      onRefresh: () => refreshHome(ref),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.md,
          AppSpacing.lg,
          AppSpacing.xxl,
        ),
        children: [
          const EmergencyStrip(),
          AppSpacing.gapXl,

          const SectionHeader(title: 'Modüller'),
          const ModuleGrid(),
          AppSpacing.gapXl,

          SectionHeader(
            title: 'Öne çıkan',
            action: 'Tümü',
            onActionTap: () => context.push(AppRoutes.announcements),
          ),
          const _LatestAnnouncements(),

          if (Env.showDevTools) ...[
            AppSpacing.gapXl,
            const SectionHeader(title: 'Geliştirici'),
            AppCard(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${Env.flavor.name} · ${Env.apiBaseUrl}',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.palette.muted,
                    ),
                  ),
                  AppSpacing.gapMd,
                  Row(
                    children: [
                      Expanded(
                        child: AppButton.ghost(
                          label: 'Tasarım',
                          icon: Icons.palette_rounded,
                          size: AppButtonSize.small,
                          expand: true,
                          onPressed: () => context.push(AppRoutes.designPreview),
                        ),
                      ),
                      AppSpacing.wGapSm,
                      Expanded(
                        child: AppButton.ghost(
                          label: 'Ağ tanılama',
                          icon: Icons.lan_rounded,
                          size: AppButtonSize.small,
                          expand: true,
                          onPressed: () => context.push(AppRoutes.networkProbe),
                        ),
                      ),
                    ],
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

/// Saate göre selamlama — "sıcak & topluluk" tonunun küçük ama sürekli görünen
/// parçası (MOBILE_UX_PLAN §0). Saat **Kadirli saatiyle** hesaplanır.
class _Greeting extends StatelessWidget {
  const _Greeting({this.name});

  final String? name;

  @override
  Widget build(BuildContext context) {
    final hour = AppDate.nowInTurkey.hour;
    final greeting = switch (hour) {
      >= 5 && < 11 => 'Günaydın',
      >= 11 && < 18 => 'İyi günler',
      >= 18 && < 23 => 'İyi akşamlar',
      _ => 'İyi geceler',
    };

    final text = name == null ? '$greeting 👋' : '$greeting, $name 👋';
    return Text(text, maxLines: 1, overflow: TextOverflow.ellipsis);
  }
}

/// Son duyurular vitrini — yükleniyor/boş/hata durumları burada.
class _LatestAnnouncements extends ConsumerWidget {
  const _LatestAnnouncements();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(latestAnnouncementsProvider);

    return switch (state) {
      AsyncError(:final error) => AppCard(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              _messageOf(error),
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            AppSpacing.gapMd,
            AppButton.ghost(
              label: 'Tekrar dene',
              icon: Icons.refresh_rounded,
              size: AppButtonSize.small,
              expand: true,
              onPressed: () => ref.invalidate(latestAnnouncementsProvider),
            ),
          ],
        ),
      ),
      AsyncData(value: final items) when items.isEmpty => const AppCard(
        child: Text('Şu an gösterilecek duyuru yok.', textAlign: TextAlign.center),
      ),
      AsyncData(value: final items) => Column(
        children: [
          for (final announcement in items) ...[
            AnnouncementTile(
              announcement: announcement,
              // 11.6'dan beri doğrudan detaya: vitrindeki karta dokunan
              // kullanıcı listeye değil okumak istediği duyuruya gider.
              onTap: () => context.push(
                AppRoutes.announcementDetail(announcement.id),
              ),
            ),
            if (announcement != items.last) AppSpacing.gapSm,
          ],
        ],
      ),
      // Ana Sayfa'nın ListView'ı içinde → shrinkWrap şart.
      _ => const SkeletonCardList(
        itemCount: 2,
        hasImage: false,
        shrinkWrap: true,
        padding: EdgeInsets.zero,
      ),
    };
  }

  static String _messageOf(Object error) => error is ApiException
      ? error.message
      : 'Duyurular yüklenemedi.';
}
