import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/widgets/widgets.dart';
import '../application/saved_news_controller.dart';
import 'widgets/news_card.dart';

/// "Kaydedilenler" (plan dışı ek, 12.14).
///
/// Liste **tamamen yerel**: kayıtlar cihazda saklanan anlık görüntülerden
/// çiziliyor, ekran açılırken **hiçbir istek atılmıyor**. Bu, uçağa binen ya da
/// şebekesi olmayan kullanıcının kaydettiklerini yine de görebilmesi demek —
/// ve modülün geri kalanı çevrimiçiyken tek çevrimdışı yüzeyi bu.
///
/// ⚠️ Rota `/haberler`in **alt rotası değil, kardeşi**: alt rota olsaydı
/// go_router üstteki liste ekranını da kurar, arka planda haber/kategori
/// istekleri atardı (§7 kod-dışı, 11.7/11.9'da iki kez yaşandı) — üstelik
/// `/haberler/:id` deseniyle de çakışırdı.
class SavedNewsScreen extends ConsumerWidget {
  const SavedNewsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final saved = ref.watch(savedNewsProvider);

    return AppScaffold(
      title: 'Kaydedilenler',
      actions: [
        if (saved.isNotEmpty)
          IconButton(
            tooltip: 'Tümünü kaldır',
            icon: const Icon(Icons.delete_sweep_rounded),
            onPressed: () => _confirmClear(context, ref),
          ),
      ],
      body: saved.isEmpty
          ? const ScrollableStateBody(
              child: EmptyView(
                icon: Icons.bookmark_border_rounded,
                title: 'Kaydedilen haber yok',
                message:
                    'Bir haberi açıp sağ üstteki yer imi simgesine dokunarak '
                    'sonra okumak üzere kaydedebilirsiniz.',
              ),
            )
          : ListView.separated(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.lg,
                AppSpacing.lg,
                AppSpacing.xxl,
              ),
              itemCount: saved.length + 1,
              separatorBuilder: (_, _) => AppSpacing.gapSm,
              itemBuilder: (context, index) {
                if (index == saved.length) return const _LocalOnlyNote();

                final article = saved[index];
                return NewsCard(
                  article: article,
                  onTap: () => context.push(AppRoutes.newsDetail(article.id)),
                );
              },
            ),
    );
  }

  Future<void> _confirmClear(BuildContext context, WidgetRef ref) async {
    // Geri alınamaz bir işlem **neyi sildiğini söyler** (panelin `data-confirm`
    // kuralının mobil karşılığı).
    final count = ref.read(savedNewsProvider).length;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Kayıtlar silinsin mi?'),
        content: Text('$count kaydedilen haber listeden kaldırılacak.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Vazgeç'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Kaldır'),
          ),
        ],
      ),
    );

    if (confirmed ?? false) await ref.read(savedNewsProvider.notifier).clear();
  }
}

/// Listenin sonunda tek satırlık dürüstlük notu: bu liste **cihaza** bağlı.
///
/// Yazılmasaydı kullanıcı telefon değiştirdiğinde kayıtlarını kaybeder ve
/// bunun bir hata olduğunu sanardı.
class _LocalOnlyNote extends StatelessWidget {
  const _LocalOnlyNote();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.only(top: AppSpacing.md),
      child: Text(
        'Kaydedilen haberler yalnız bu cihazda saklanır.',
        textAlign: TextAlign.center,
        style: theme.textTheme.bodySmall?.copyWith(color: theme.palette.muted),
      ),
    );
  }
}
