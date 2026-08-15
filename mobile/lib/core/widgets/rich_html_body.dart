import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_html/flutter_html.dart';

import '../theme/app_colors.dart';
import '../theme/app_spacing.dart';
import '../utils/utils.dart';

/// **Sunucudan gelen HTML gövdenin ortak çizim çekirdeği** (12.17'de
/// `NewsBody`'den çıkarıldı; ilk sahibi 12.14'tü).
///
/// ## Burada ikinci bir beyaz liste YOK — ve bu, bileşenin birinci kuralı
/// Temizlik **sunucudadır**: haberde alım anında (`NewsHtmlPolicy`, 12.12),
/// hukuki metinde panelin kendi yazma kapısında (12.16). İstemcide ikinci bir
/// beyaz liste yazmak, ayrıştıkları anda hangisinin doğru olduğu bilinemeyen
/// iki gerçeklik üretirdi: sunucu "gönderdim" der, ekran boş kalır, log
/// temizdir (§7 madde 61). **İstemcinin işi yalnız stil.**
///
/// ## Neden ortak bir çekirdek (ve neden ikinci bir kopya değil)
/// 12.17'de hukuki metinler de HTML çizmeye başladı. İkinci bir gerçekleme
/// yazılsaydı iki dosya **ayrı ayrı doğru** başlar ve zamanla ayrışırdı:
/// birinde `<a>`'ya `onLinkTap` bağlıyken diğerinde bağlanmaz, birinde
/// `1.4` yazı ölçeğinde paragraf marjı düzeltilmişken diğerinde kalırdı — ve
/// hiçbiri hata vermezdi. Modülün kendi kararları (hangi metni çiziyorum,
/// görseli nasıl ele alıyorum) sarmalayıcıda kalır, **çizim burada**.
///
/// ## Üç bağ, üçü de sessiz hasar üretir
/// - [Html.onLinkTap] bağlanmazsa gövdedeki bağlantı **çizilir, tıklanır,
///   hiçbir şey olmaz** ("işlevsiz buton yok" kuralının gövde içi karşılığı).
/// - `<img>` paketin varsayılanına (`Image.network`) bırakılırsa
///   **önbelleklenmez** ve hata durumunda **kırık kutu** basar.
/// - Yükleme sırasında yer tutulursa görsel gelince metin **zıplar**.
class RichHtmlBody extends StatelessWidget {
  const RichHtmlBody({super.key, required this.html});

  final String html;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final palette = theme.palette;
    final body = theme.textTheme.bodyLarge;

    return Html(
      data: html,
      // ⚠️ `onLinkTap` bağlanmazsa `<a>` **çizilir ama hiçbir şey yapmaz**.
      onLinkTap: (url, _, _) {
        final target = url?.trim();
        if (target == null || target.isEmpty) return;
        AppLinks.web(target);
      },
      extensions: [
        // Varsayılan `<img>` çizimi `Image.network` kullanır: önbelleklemez ve
        // hata durumunda kırık simge basar. İkisi de istenmiyor.
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
        'h1': Style(
          fontSize: FontSize(theme.textTheme.headlineSmall?.fontSize ?? 22),
          fontWeight: FontWeight.w700,
          margin: Margins.only(top: AppSpacing.md, bottom: AppSpacing.sm),
        ),
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
        // Hukuki metinlerde tablo kullanılıyor (veri kategorileri × amaçlar);
        // paketin varsayılanı sınırsız genişleyebildiği için hücreye nefes
        // payı verilir, taşma `RichHtmlBody`'yi saran ekranın işidir.
        'td': Style(padding: HtmlPaddings.all(AppSpacing.xs)),
        'th': Style(
          padding: HtmlPaddings.all(AppSpacing.xs),
          fontWeight: FontWeight.w600,
        ),
      },
    );
  }
}

/// Gövde içi görsel: önbellekli, **açılmazsa hiç yer kaplamaz**.
///
/// Yer tutucu bile göstermemek bilinçli: paragrafların arasında duran kırık bir
/// kutu, hiç olmayan bir görselden **daha çok** dikkat çeker ve sıfır bilgi
/// taşır (haberde ölçüldü — gövde görsellerinin bir kısmı süreli adreslerdi).
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
