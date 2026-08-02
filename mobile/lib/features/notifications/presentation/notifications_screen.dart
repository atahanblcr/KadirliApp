import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/paging/paged_list_footer.dart';
import '../../../core/push/push_messaging.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/app_date.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../../auth/presentation/widgets/sign_in_prompt.dart';
import '../application/notifications_feed.dart';
import '../application/push_controller.dart';
import '../data/models/app_notification.dart';
import 'widgets/notification_tile.dart';

/// Bildirimler sekmesi (11.4 iskelet → 11.13 tam).
///
/// **KARARLAR:**
/// 1. Liste **gün gruplu** ("Bugün / Dün / 12 Ağustos"): bildirim zamana bağlı
///    okunur, tarihsiz düz liste kullanıcıyı her satırda hesap yapmaya zorlar.
/// 2. Okundu işaretleme **iyimser** (11.8 favori kalbinin deseni). "Yalnız
///    okunmamışlar" görünümündeyken okunan satır **gözünün önünde kaybolmaz**
///    (bir sonraki tazelemede düşer) — kaybolan satır kullanıcıya "yanlış şeye
///    mi dokundum?" dedirtiyor.
/// 3. Satıra dokunuş → [PushCoordinator.openNotification]: **push dokunuşuyla
///    birebir aynı yol**, davranış ayrışması imkânsız.
/// 4. Bildirim izni verilmemişse üstte şerit: liste çalışıyor olsa da cihaza
///    push düşmüyor demektir ve kullanıcı bunu bilmeli.
class NotificationsScreen extends ConsumerStatefulWidget {
  const NotificationsScreen({super.key});

  @override
  ConsumerState<NotificationsScreen> createState() =>
      _NotificationsScreenState();
}

class _NotificationsScreenState extends ConsumerState<NotificationsScreen> {
  final _scrollController = ScrollController();

  static const _loadMoreThreshold = 400.0;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - _loadMoreThreshold) {
      ref.read(notificationsFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isSignedIn = ref.watch(
      authControllerProvider.select((state) => state.isAuthenticated),
    );

    if (!isSignedIn) {
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

    final state = ref.watch(notificationsFeedProvider);
    final controller = ref.read(notificationsFeedProvider.notifier);
    final hasUnread = state.items.any((item) => !item.isRead);

    return AppScaffold(
      title: 'Bildirimler',
      showBackButton: false,
      onRefresh: controller.refresh,
      actions: [
        // ⚠️ "Tümünü okundu yap" bilinçli olarak AppBar ikonu: filtre şeridinde
        // metin butonu olarak durduğunda 360 dp'ye sığmıyor ve 1.4 yazı
        // ölçeğinde iyice taşıyordu (widget testi yakaladı — bu projenin
        // tekrar eden `Row` taşma tuzağının altıncısı, 11.7-11.12).
        IconButton(
          tooltip: 'Tümünü okundu yap',
          onPressed: hasUnread ? () => _markAll(controller) : null,
          icon: const Icon(Icons.done_all_rounded),
        ),
        IconButton(
          tooltip: 'Bildirim tercihleri',
          onPressed: () => context.push(AppRoutes.settings),
          icon: const Icon(Icons.tune_rounded),
        ),
      ],
      body: Column(
        children: [
          const _PushPermissionNotice(),
          _FilterBar(
            unreadOnly: state.filter.unreadOnly,
            onToggleUnread: controller.toggleUnreadOnly,
          ),
          Expanded(child: _Body(scrollController: _scrollController)),
        ],
      ),
    );
  }

  Future<void> _markAll(NotificationsFeedController controller) async {
    final done = await controller.markAllRead();
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          done
              ? 'Tüm bildirimler okundu olarak işaretlendi.'
              : 'İşaretleme yapılamadı, tekrar deneyin.',
        ),
      ),
    );
  }
}

/// Bildirim izni verilmemişse: liste çalışır ama **cihaza push düşmez**.
/// Bunu söylemek şart — yoksa kullanıcı "bildirim gelmiyor" diye düşünür.
///
/// ⚠️ **Canlı testte yakalanan tuzak:** Android'de izin *hiç sorulmamışken* de
/// `denied` dönüyor (`notDetermined` pratikte yalnız iOS'ta görülüyor). İlk
/// sürümde "İzin ver" düğmesi yalnız `notDetermined`'da çiziliyordu → Android
/// kullanıcısı şeridi görüyor ama **hiçbir şey yapamıyordu**. Düğme artık her
/// iki durumda da var; sistem diyaloğu açılmıyorsa (kalıcı ret) metin telefon
/// ayarlarına yönlendiriyor.
class _PushPermissionNotice extends ConsumerStatefulWidget {
  const _PushPermissionNotice();

  @override
  ConsumerState<_PushPermissionNotice> createState() =>
      _PushPermissionNoticeState();
}

class _PushPermissionNoticeState extends ConsumerState<_PushPermissionNotice> {
  /// Bu ekranda izin istendi ve yine reddedildi mi? (Kalıcı ret göstergesi.)
  bool _refusedAfterAsking = false;

  @override
  Widget build(BuildContext context) {
    final permission = ref.watch(pushPermissionProvider);

    // `unavailable` = bu derlemede Firebase yapılandırılmamış → kullanıcının
    // yapabileceği bir şey yok, şerit gösterilmez (gürültü olurdu).
    if (permission == PushPermission.granted ||
        permission == PushPermission.unavailable) {
      return const SizedBox.shrink();
    }

    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.md,
        AppSpacing.lg,
        0,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          InfoBanner(
            tone: InfoBannerTone.warning,
            message: _refusedAfterAsking
                ? 'Bildirim izni kapalı. Açmak için telefon ayarlarından '
                      'Kadirli → Bildirimler bölümünü kullanın; listeyi bu '
                      'ekrandan takip etmeye devam edebilirsiniz.'
                : 'Bildirim izni kapalı. İzin verirseniz duyurular telefonunuza '
                      'anında düşer.',
          ),
          if (!_refusedAfterAsking) ...[
            AppSpacing.gapSm,
            AppButton.ghost(
              label: 'Bildirimlere izin ver',
              icon: Icons.notifications_active_outlined,
              size: AppButtonSize.small,
              onPressed: _requestPermission,
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _requestPermission() async {
    final result = await ref.read(pushCoordinatorProvider).requestPermission();
    if (!mounted || result == PushPermission.granted) return;
    setState(() => _refusedAfterAsking = true);
  }
}

class _FilterBar extends StatelessWidget {
  const _FilterBar({required this.unreadOnly, required this.onToggleUnread});

  final bool unreadOnly;
  final VoidCallback onToggleUnread;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.lg,
          AppSpacing.md,
          AppSpacing.lg,
          AppSpacing.sm,
        ),
        child: FilterChoiceChip(
          label: 'Okunmamışlar',
          icon: Icons.mark_email_unread_outlined,
          selected: unreadOnly,
          onTap: onToggleUnread,
        ),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(notificationsFeedProvider);
    final controller = ref.read(notificationsFeedProvider.notifier);
    final theme = Theme.of(context);

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 4, hasImage: false);
    }

    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        message: state.error!.message,
        traceId: state.error!.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      return EmptyView(
        icon: Icons.notifications_none_rounded,
        title: state.filter.unreadOnly
            ? 'Okunmamış bildirim yok'
            : 'Henüz bildiriminiz yok',
        message: state.filter.unreadOnly
            ? 'Hepsini okumuşsunuz. Filtreyi kapatarak geçmişe bakabilirsiniz.'
            : 'Belediye duyuruları ve size özel gelişmeler burada görünecek.',
      );
    }

    final rows = buildNotificationRows(state.items);

    return ListView.separated(
      controller: scrollController,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.sm,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      itemCount: rows.length + 1,
      separatorBuilder: (_, _) => AppSpacing.gapSm,
      itemBuilder: (context, index) {
        if (index == rows.length) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'bildirim',
          );
        }

        return switch (rows[index]) {
          NotificationDayHeader(:final label) => Padding(
            padding: const EdgeInsets.only(
              top: AppSpacing.md,
              bottom: AppSpacing.xs,
            ),
            child: Text(
              label,
              style: theme.textTheme.labelMedium?.copyWith(
                color: theme.palette.muted,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          NotificationRow(:final notification) => NotificationTile(
            notification: notification,
            onTap: () =>
                ref.read(pushCoordinatorProvider).openNotification(
                  notificationId: notification.id,
                  relatedType: notification.relatedType,
                  relatedId: notification.relatedId,
                ),
          ),
        };
      },
    );
  }
}

/// Liste satırı: gün başlığı ya da bildirim.
sealed class NotificationListRow {
  const NotificationListRow();
}

class NotificationDayHeader extends NotificationListRow {
  const NotificationDayHeader(this.label);
  final String label;
}

class NotificationRow extends NotificationListRow {
  const NotificationRow(this.notification);
  final AppNotification notification;
}

/// Bildirimleri gün başlıklarıyla böler (saf mantık — testte doğrudan çağrılır).
///
/// Tarihi olmayan kayıt "Daha önce"ye düşer: sunucu `createdAt` göndermezse
/// liste yine de çizilmeli.
List<NotificationListRow> buildNotificationRows(
  List<AppNotification> items, {
  DateTime? now,
}) {
  final rows = <NotificationListRow>[];
  String? currentLabel;

  for (final item in items) {
    final label = notificationDayLabel(item.createdAt, now: now);
    if (label != currentLabel) {
      rows.add(NotificationDayHeader(label));
      currentLabel = label;
    }
    rows.add(NotificationRow(item));
  }
  return rows;
}

String notificationDayLabel(DateTime? value, {DateTime? now}) {
  if (value == null) return 'Daha önce';

  final reference = now == null ? AppDate.nowInTurkey : AppDate.toTurkey(now);
  final day = AppDate.toTurkey(value);
  final difference = DateTime(reference.year, reference.month, reference.day)
      .difference(DateTime(day.year, day.month, day.day))
      .inDays;

  return switch (difference) {
    0 => 'Bugün',
    1 => 'Dün',
    _ => AppDate.date(value),
  };
}
