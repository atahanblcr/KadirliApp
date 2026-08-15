import 'package:flutter/material.dart';

import '../../../../core/widgets/widgets.dart';

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
/// Bunlar 12.14b'den beri aynalanıyor; aynalanamayanlar gövdede kaynağın
/// adresiyle kalır ve açılmazlarsa **yer tutucu bile göstermeden gizlenir**:
/// paragrafların arasında duran kırık bir kutu, hiç olmayan bir görselden daha
/// çok dikkat çeker ve bilgi taşımaz.
///
/// ## 12.17 — çizim çekirdeği [RichHtmlBody]'ye taşındı
/// Hukuki metinler de HTML çizmeye başlayınca stil kuralları ortak bir
/// bileşene çıkarıldı. **Sahiplik değişmedi:** haber gövdesi hakkındaki her
/// karar (hangi HTML, hangi kaynak, görselin ne olacağı) hâlâ burada;
/// [RichHtmlBody] yalnız *nasıl çizildiğini* biliyor. İkinci bir kopya
/// yazılsaydı iki dosya ayrı ayrı doğru başlar ve zamanla ayrışırdı.
class NewsBody extends StatelessWidget {
  const NewsBody({super.key, required this.html});

  final String html;

  @override
  Widget build(BuildContext context) => RichHtmlBody(html: html);
}
