import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/paging/paged_list_footer.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../../auth/application/auth_controller.dart';
import '../application/complaints_providers.dart';
import 'widgets/complaint_card.dart';

/// Şikayet / İstek (11.12) — Ayarlar'daki "Şikayet / İstek bildir" kısayolunun
/// hedefi ve hub ızgarasındaki modül.
///
/// Ekran **takip** üzerine kurulu: gönderme ayrı bir ekranda (odaklanmış görev,
/// 11.9/11.11 deseni), burada kullanıcının gönderdiklerinin durumu var.
///
/// **KARAR — anonim kullanıcı buradan atılmaz:** rota `protectedPrefixes`'e
/// yazılmadı. Misafir listeyi göremez ama **bildirim gönderebilir** (uç anonim
/// gönderime açık); ekran onu Giriş'e sürüklemek yerine iki seçeneği de sunar
/// (11.10/11.11 `ensureSignedIn` kararının aynı ruhu).
class ComplaintsScreen extends ConsumerStatefulWidget {
  const ComplaintsScreen({super.key});

  @override
  ConsumerState<ComplaintsScreen> createState() => _ComplaintsScreenState();
}

class _ComplaintsScreenState extends ConsumerState<ComplaintsScreen> {
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
      ref.read(myComplaintsFeedProvider.notifier).loadMore();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isSignedIn = ref.watch(authControllerProvider).isAuthenticated;

    return AppScaffold(
      title: 'Şikayet / İstek',
      onRefresh: isSignedIn
          ? ref.read(myComplaintsFeedProvider.notifier).refresh
          : null,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.complaintCreate),
        icon: const Icon(Icons.edit_outlined),
        label: const Text('Bildirim gönder'),
      ),
      body: isSignedIn
          ? _MyComplaints(scrollController: _scrollController)
          : const _GuestInvite(),
    );
  }
}

/// Misafir hali: listeyi göremez ama **gönderebilir** — bu ayrım açıkça
/// yazılıyor, yoksa "giriş yap" duvarı bildirimi tümden engeller.
class _GuestInvite extends StatelessWidget {
  const _GuestInvite();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.xl,
        AppSpacing.huge,
        AppSpacing.xl,
        AppSpacing.huge,
      ),
      children: [
        Center(
          child: Container(
            padding: const EdgeInsets.all(AppSpacing.lg),
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer,
              shape: BoxShape.circle,
            ),
            child: Icon(
              Icons.support_agent_rounded,
              size: 32,
              color: theme.colorScheme.onPrimaryContainer,
            ),
          ),
        ),
        AppSpacing.gapLg,
        Text(
          'Bildirimlerinizi takip edin',
          style: theme.textTheme.titleMedium,
          textAlign: TextAlign.center,
        ),
        AppSpacing.gapSm,
        Text(
          'Giriş yaparsanız gönderdiğiniz şikayet ve isteklerin durumunu '
          '(bekliyor, işlemde, çözüldü) buradan izleyebilir, yetkili yanıtını '
          'görebilirsiniz.',
          style: theme.textTheme.bodyMedium?.copyWith(color: palette.muted),
          textAlign: TextAlign.center,
        ),
        AppSpacing.gapXl,
        AppButton(
          label: 'Giriş yap',
          icon: Icons.login_rounded,
          expand: true,
          onPressed: () => context.push(AppRoutes.login),
        ),
        AppSpacing.gapLg,
        Text(
          'Giriş yapmadan da bildirim gönderebilirsiniz; yalnız takip '
          'edemezsiniz.',
          style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }
}

class _MyComplaints extends ConsumerWidget {
  const _MyComplaints({required this.scrollController});

  final ScrollController scrollController;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(myComplaintsFeedProvider);
    final controller = ref.read(myComplaintsFeedProvider.notifier);

    if (state.isLoadingFirstPage) {
      return const LoadingView(itemCount: 3, hasImage: false);
    }

    if (state.error != null && state.items.isEmpty) {
      return ErrorView(
        message: state.error!.message,
        traceId: state.error!.traceId,
        onRetry: controller.retry,
      );
    }

    if (state.isEmpty) {
      return const EmptyView(
        icon: Icons.support_agent_rounded,
        title: 'Henüz bildiriminiz yok',
        message:
            'Şehirle ya da uygulamayla ilgili bir sorununuz, isteğiniz veya '
            'öneriniz varsa aşağıdaki düğmeden iletebilirsiniz.',
      );
    }

    return ListView.separated(
      controller: scrollController,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        // FAB'ın altında kalan kart olmasın.
        AppSpacing.huge + AppSpacing.xxl,
      ),
      itemCount: state.items.length + 1,
      separatorBuilder: (_, _) => AppSpacing.gapMd,
      itemBuilder: (context, index) {
        if (index == state.items.length) {
          return PagedListFooter(
            state: state,
            onLoadMore: controller.loadMore,
            itemNoun: 'bildirim',
          );
        }

        return ComplaintCard(complaint: state.items[index]);
      },
    );
  }
}
