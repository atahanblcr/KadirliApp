import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../core/network/network.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../core/utils/utils.dart';
import '../../../core/widgets/widgets.dart';
import '../application/news_providers.dart';
import '../application/saved_news_controller.dart';
import '../data/models/news_article.dart';
import 'widgets/news_body.dart';
import 'widgets/news_card.dart';

/// Haber detayı (12.14) — 12.15'in push deep-link hedefi.
class NewsDetailScreen extends ConsumerWidget {
  const NewsDetailScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(newsDetailProvider(id));
    final saved = ref.watch(savedNewsProvider);

    return AppScaffold(
      title: 'Haber',
      actions: [
        if (state case AsyncData(value: final article)) ...[
          _SaveButton(article: article, isSaved: isNewsSaved(saved, article.id)),
          Builder(
            builder: (context) => IconButton(
              tooltip: 'Paylaş',
              icon: const Icon(Icons.ios_share_rounded),
              onPressed: () => AppShare.text(
                article.shareText(),
                subject: article.title,
                origin: AppShare.originOf(context),
              ),
            ),
          ),
        ],
      ],
      onRefresh: () async => ref.invalidate(newsDetailProvider(id)),
      body: switch (state) {
        AsyncData(value: final article) => _Content(article: article),
        AsyncError(:final error) => _DetailError(
          error: error,
          articleId: id,
          onRetry: () => ref.invalidate(newsDetailProvider(id)),
        ),
        _ => const LoadingView(itemCount: 3),
      },
    );
  }
}

/// Kaydet / kaydı kaldır (plan dışı ek).
///
/// ⚠️ Durum **anında** değişir (yerel depo), sonuç `SnackBar` ile söylenir:
/// sessizce çalışan bir yer imi butonu, çalışmayan bir butondan ayırt edilemez.
class _SaveButton extends ConsumerWidget {
  const _SaveButton({required this.article, required this.isSaved});

  final NewsArticle article;
  final bool isSaved;

  @override
  Widget build(BuildContext context, WidgetRef ref) => IconButton(
    tooltip: isSaved ? 'Kaydı kaldır' : 'Kaydet',
    icon: Icon(isSaved ? Icons.bookmark_rounded : Icons.bookmark_border_rounded),
    onPressed: () async {
      final messenger = ScaffoldMessenger.of(context);
      final nowSaved = await ref.read(savedNewsProvider.notifier).toggle(article);
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(
              nowSaved ? 'Haber kaydedildi.' : 'Haber kayıtlardan çıkarıldı.',
            ),
          ),
        );
    },
  );
}

/// "Bulunamadı" ile "yüklenemedi" **bilinçli olarak ayrılır**.
///
/// Haber kaynakta yayından kalkmışsa (12.12'nin `gone` durumu) ya da panelden
/// gizlenmişse uç **404** döner. Böyle bir kayda "Tekrar dene" göstermek
/// anlamsız: tekrar denemek de bulmayacak. Kullanıcı büyük ihtimalle
/// **kaydedilenler listesinden** ya da eski bir bildirimden geliyor.
class _DetailError extends ConsumerWidget {
  const _DetailError({
    required this.error,
    required this.articleId,
    required this.onRetry,
  });

  final Object error;
  final String articleId;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final api = error is ApiException ? error as ApiException : null;

    if (api != null && api.isNotFound) {
      // 🔑 Kaydedilenlerde bu haberin bir **anlık görüntüsü** duruyor olabilir
      // (12.14 eki): o zaman kullanıcıya en azından kaynağa giden bir kapı
      // açılır — "yok" demekle yetinmek, elimizdeki bilgiyi saklamak olurdu.
      final snapshot = ref
          .watch(savedNewsProvider)
          .where((item) => item.id == articleId)
          .firstOrNull;
      final sourceUrl = snapshot?.sourceUrl?.trim();

      return ScrollableStateBody(
        child: Column(
          children: [
            EmptyView(
              icon: Icons.newspaper_outlined,
              title: 'Haber bulunamadı',
              message: snapshot == null
                  ? 'Bu haber yayından kaldırılmış olabilir.'
                  : '"${snapshot.title}" yayından kaldırılmış olabilir.',
            ),
            if (sourceUrl != null && sourceUrl.isNotEmpty) ...[
              Padding(
                padding: AppSpacing.screenPadding,
                child: AppButton.ghost(
                  label: 'Kaynakta oku',
                  icon: Icons.open_in_new_rounded,
                  expand: true,
                  onPressed: () => AppLinks.web(sourceUrl),
                ),
              ),
              AppSpacing.gapLg,
            ],
          ],
        ),
      );
    }

    return ErrorView(
      message: api?.message ?? 'Haber yüklenemedi.',
      traceId: api?.traceId,
      onRetry: onRetry,
    );
  }
}

class _Content extends ConsumerWidget {
  const _Content({required this.article});

  final NewsArticle article;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final published = article.publishedAt;
    final body = article.contentHtml?.trim() ?? '';
    final sourceUrl = article.sourceUrl?.trim();

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.lg,
        AppSpacing.xxl,
      ),
      children: [
        if (article.imageUrl != null) ...[
          // ⚠️ Kaynağın en büyük boyutu **650×368** (12.12 ölçümü) → 3x
          // telefonda yukarı ölçekleniyor; bu **kabul edilen bir yumuşaklık**.
          // Yöneticinin panelden daha iyi bir kapak koyabilmesi (12.13'ün
          // `CoverImageFileIdOverride`'ı) tam bu yüzden var.
          AspectRatio(
            aspectRatio: 16 / 9,
            child: AppNetworkImage(
              url: article.imageUrl,
              borderRadius: AppRadius.rMd,
              fallbackIcon: Icons.newspaper_outlined,
            ),
          ),
          AppSpacing.gapLg,
        ],

        if (article.categories.isNotEmpty) ...[
          // Detayda **tüm** kategoriler görünür (kartta yalnız ilki): kaynakta
          // bir haber birden çok kategoride olabiliyor ve hangi kategorilerde
          // olduğu, kategori süzgecini kullanan biri için gerçek bir bilgi.
          Wrap(
            spacing: AppSpacing.sm,
            runSpacing: AppSpacing.xs,
            children: [
              for (final category in article.categories)
                _CategoryPill(label: category.label),
            ],
          ),
          AppSpacing.gapMd,
        ],

        SelectableText(article.title, style: theme.textTheme.headlineSmall),

        AppSpacing.gapSm,
        Wrap(
          spacing: AppSpacing.md,
          runSpacing: AppSpacing.xs,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            if (published != null)
              _Meta(
                icon: Icons.schedule_rounded,
                text:
                    '${AppDate.dateTime(published)} · ${AppDate.relative(published)}',
              ),
            _Meta(icon: Icons.menu_book_rounded, text: article.readingLabel),
            if (article.wasUpdated && article.modifiedAt != null)
              _Meta(
                icon: Icons.update_rounded,
                text: 'Güncellendi: ${AppDate.dateTime(article.modifiedAt!)}',
              ),
          ],
        ),

        if (body.isNotEmpty) ...[
          AppSpacing.gapLg,
          NewsBody(html: body),
        ] else if (article.excerpt.trim().isNotEmpty) ...[
          // Gövde gelmediyse (eski bir kayıt ya da kaynağın boş içeriği) özet
          // gösterilir: ekranı boş bırakmak, elimizdeki metni saklamak olurdu.
          AppSpacing.gapLg,
          SelectableText(
            article.excerpt.trim(),
            style: theme.textTheme.bodyLarge?.copyWith(height: 1.5),
          ),
        ],

        if (sourceUrl != null && sourceUrl.isNotEmpty) ...[
          AppSpacing.gapXl,
          AppButton.ghost(
            label: 'Kaynakta oku',
            icon: Icons.open_in_new_rounded,
            expand: true,
            onPressed: () => AppLinks.web(sourceUrl),
          ),
          AppSpacing.gapSm,
          Text(
            'Haber içerikleri Sıla Gazetesi kaynaklıdır.',
            textAlign: TextAlign.center,
            style: theme.textTheme.bodySmall?.copyWith(color: palette.muted),
          ),
        ],

        _RelatedNews(article: article),
      ],
    );
  }
}

/// "Bu kategoriden" şeridi (plan dışı ek).
///
/// Yeni bir uç gerektirmiyor — var olan `?categoryId=` süzgeci kullanılıyor.
/// Hata/boş durumda **hiç çizilmez**: ikincil bir bölümün hatası, okunan
/// haberin ekranını kirletmemeli.
class _RelatedNews extends ConsumerWidget {
  const _RelatedNews({required this.article});

  final NewsArticle article;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categoryId = article.categories.firstOrNull?.id;
    if (categoryId == null) return const SizedBox.shrink();

    final related = ref.watch(
      relatedNewsProvider(
        RelatedNewsRequest(categoryId: categoryId, excludeId: article.id),
      ),
    );
    final items = related.value ?? const [];
    if (items.isEmpty) return const SizedBox.shrink();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        AppSpacing.gapXl,
        SectionHeader(title: 'Bu kategoriden'),
        for (final item in items) ...[
          NewsCard(
            article: item,
            // ⚠️ `push`: hedef bir sekme (kabuk) rotası **değil**, modül
            // ekranının alt rotası — kabuk anahtarı çakışması riski yok
            // (§7 kod-dışı, 12.3'ün çökme kök nedeni).
            onTap: () => context.push(AppRoutes.newsDetail(item.id)),
          ),
          AppSpacing.gapSm,
        ],
      ],
    );
  }
}

class _CategoryPill extends StatelessWidget {
  const _CategoryPill({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = theme.colorScheme.primary;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: 2,
      ),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: AppRadius.rPill,
        border: Border.all(color: color.withValues(alpha: 0.35)),
      ),
      child: Text(
        label,
        style: theme.textTheme.labelSmall?.copyWith(
          color: color,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _Meta extends StatelessWidget {
  const _Meta({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = theme.palette.muted;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: color),
        AppSpacing.wGapXs,
        Flexible(
          child: Text(
            text,
            style: theme.textTheme.bodySmall?.copyWith(color: color),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ),
      ],
    );
  }
}
