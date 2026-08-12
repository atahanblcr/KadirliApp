import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_html/flutter_html.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/utils/utils.dart';

/// Haber gövdesinin **tek çizim sahibi** (12.14).
///
/// ## Güvenlik: burada ikinci bir beyaz liste YOK
/// Gövde alım anında sunucuda temizlendi (12.12, `NewsHtmlPolicy`:
/// `p br strong em a figure figcaption img ul ol li blockquote h2 h3 h4`).
/// İstemcide ikinci bir beyaz liste yazmak, ayrıştıkları anda hangisinin doğru
/// olduğu bilinemeyen iki gerçeklik üretirdi — projedeki "tek sahip" kuralının
/// birebir uygulaması. İstemcinin işi yalnız **stil**.
///
/// ## Neden `flutter_html` (ve reddedilen alternatif)
/// Gövdeyi sunucuda blok JSON'a çevirmek de mümkündü (korpusta yalnız
/// `p/figure/img/strong/a` var, 12.12 ölçümü) ve daha saf olurdu; ama gazete
/// yarın tablo ya da gömülü içerik kullanmaya başlarsa o içerik **sessizce
/// kaybolurdu**. `flutter_html` şüphede kalınca *göstermek* yönünde — projenin
/// "additive bir alanın yokluğu kaydı gizlememeli" ilkesiyle aynı yön.
///
/// ## Metin arası görseller
/// Bunlar **aynalanmıyor** (yalnız kapak aynalanıyor, 12.12): doğrudan gazetenin
/// sunucusundan çekiliyorlar ve ölçüldüğü üzere %9'u **süreli** `fbcdn` linki,
/// yani zamanla 403 olacaklar. Bu yüzden açılmayan görsel **yer tutucu bile
/// göstermez, tamamen gizlenir**: paragrafların arasında duran kırık bir kutu,
/// hiç olmayan bir görselden daha çok dikkat çeker ve bilgi taşımaz.
class NewsBody extends StatelessWidget {
  const NewsBody({super.key, required this.html});

  final String html;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final body = theme.textTheme.bodyLarge;

    return Html(
      data: html,
      // ⚠️ `onLinkTap` bağlanmazsa `<a>` **çizilir ama hiçbir şey yapmaz** —
      // "işlevsiz buton yok" kuralının gövde içindeki karşılığı.
      onLinkTap: (url, _, _) {
        final target = url?.trim();
        if (target == null || target.isEmpty) return;
        AppLinks.web(target);
      },
      extensions: [
        // Varsayılan `<img>` çizimi `Image.network` kullanır: önbelleklemez ve
        // hata durumunda kırık simge basar. İkisi de bu modülde istenmiyor.
        TagExtension(
          tagsToExtend: {'img'},
          builder: (context) => _BodyImage(src: context.attributes['src']),
        ),
      ],
      style: {
        // `margin: zero` + paragraf altı boşluk: paketin varsayılan `em` bazlı
        // marjı 1.4 yazı ölçeğinde paragrafları birbirinden kopartıyordu.
        'body': Style(
          margin: Margins.zero,
          padding: HtmlPaddings.zero,
          fontSize: FontSize(body?.fontSize ?? 16),
          lineHeight: LineHeight.number(1.55),
          color: theme.colorScheme.onSurface,
          fontFamily: body?.fontFamily,
        ),
        'p': Style(margin: Margins.only(bottom: AppSpacing.md)),
        'h2': Style(
          fontSize: FontSize(theme.textTheme.titleLarge?.fontSize ?? 20),
          fontWeight: FontWeight.w700,
          margin: Margins.only(top: AppSpacing.md, bottom: AppSpacing.sm),
        ),
        'h3': Style(
          fontSize: FontSize(theme.textTheme.titleMedium?.fontSize ?? 18),
          fontWeight: FontWeight.w700,
          margin: Margins.only(top: AppSpacing.md, bottom: AppSpacing.sm),
        ),
        'h4': Style(
          fontSize: FontSize(theme.textTheme.titleSmall?.fontSize ?? 16),
          fontWeight: FontWeight.w600,
          margin: Margins.only(top: AppSpacing.sm, bottom: AppSpacing.xs),
        ),
        'a': Style(
          color: theme.colorScheme.primary,
          textDecoration: TextDecoration.underline,
          textDecorationColor: theme.colorScheme.primary,
        ),
        'blockquote': Style(
          margin: Margins.only(bottom: AppSpacing.md),
          padding: HtmlPaddings.only(left: AppSpacing.md),
          border: Border(
            left: BorderSide(color: theme.colorScheme.primary, width: 3),
          ),
          color: palette.muted,
          fontStyle: FontStyle.italic,
        ),
        'figure': Style(margin: Margins.only(bottom: AppSpacing.md)),
        'figcaption': Style(
          fontSize: FontSize(theme.textTheme.bodySmall?.fontSize ?? 13),
          color: palette.muted,
          textAlign: TextAlign.center,
          margin: Margins.only(top: AppSpacing.xs),
        ),
        'ul': Style(margin: Margins.only(bottom: AppSpacing.md, left: AppSpacing.md)),
        'ol': Style(margin: Margins.only(bottom: AppSpacing.md, left: AppSpacing.md)),
        'li': Style(margin: Margins.only(bottom: AppSpacing.xs)),
      },
    );
  }
}

/// Gövde içi görsel: önbellekli, **açılmazsa hiç yer kaplamaz**.
class _BodyImage extends StatelessWidget {
  const _BodyImage({required this.src});

  final String? src;

  @override
  Widget build(BuildContext context) {
    final url = AppImage.url(src);
    if (url == null) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: ClipRRect(
        borderRadius: AppRadius.rSm,
        child: CachedNetworkImage(
          imageUrl: url,
          fit: BoxFit.contain,
          // Yükleme sırasında da yer tutmaz: yükseklik bilinmediği için
          // ayrılan boş alan görsel gelince metni **zıplatırdı**.
          placeholder: (context, _) => const SizedBox.shrink(),
          errorWidget: (context, _, _) => const SizedBox.shrink(),
        ),
      ),
    );
  }
}
