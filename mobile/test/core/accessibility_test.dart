import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/theme/app_colors.dart';
import 'package:kadirli_app/core/theme/app_spacing.dart';
import 'package:kadirli_app/core/theme/app_theme.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';
import 'package:kadirli_app/features/news/data/models/news_article.dart';
import 'package:kadirli_app/features/news/data/models/news_category.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_body.dart';
import 'package:kadirli_app/features/news/presentation/widgets/news_card.dart';
import 'package:kadirli_app/features/transport/data/models/intercity_route.dart';
import 'package:kadirli_app/features/transport/presentation/widgets/intercity_route_card.dart';

/// Erişilebilirlik iddiaları — Faz 11.15.
///
/// 📌 **Neden test:** erişilebilirlik bu projede bugüne kadar yalnız **gözle**
/// denetlendi ("48 dp'ye dikkat ettik", "kontrast iyi görünüyor"). Gözle
/// denetim yeni bir ekranla birlikte çürür. `flutter_test`in yerleşik
/// kılavuzları (`meetsGuideline`) bunu mekanikleştiriyor:
///
/// - [textContrastGuideline] — WCAG AA metin kontrastı,
/// - [androidTapTargetGuideline] — en az 48×48 dokunma hedefi,
/// - [labeledTapTargetGuideline] — her dokunulabilir öğenin ekran okuyucu
///   etiketi var mı (ikon-only düğmelerde kritik).
///
/// Kapsam **temsilci bileşenler**: gerçek ekranlar ağ/router kurulumu
/// gerektiriyor ve kılavuz ihlalleri neredeyse her zaman bileşen düzeyinde
/// doğuyor (buton, chip, kart, form alanı).
void main() {
  Future<void> pumpA11y(
    WidgetTester tester,
    Widget child, {
    Brightness brightness = Brightness.light,
    double textScale = 1,
  }) async {
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp(
          theme: brightness == Brightness.light ? AppTheme.light : AppTheme.dark,
          home: MediaQuery(
            data: MediaQueryData(textScaler: TextScaler.linear(textScale)),
            child: Scaffold(
              body: Padding(
                padding: const EdgeInsets.all(AppSpacing.lg),
                child: child,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 300));
  }

  group('dokunma hedefi en az 48 dp', () {
    testWidgets('AppButton (normal + expand)', (tester) async {
      final handle = tester.ensureSemantics();
      await pumpA11y(
        tester,
        Column(
          spacing: AppSpacing.md,
          children: [
            AppButton(label: 'Kaydet', onPressed: () {}),
            AppButton.ghost(label: 'Vazgeç', expand: true, onPressed: () {}),
            AppButton.danger(label: 'Sil', onPressed: () {}),
          ],
        ),
      );

      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      handle.dispose();
    });

    testWidgets('FilterChoiceChip — dense varyantı dahil', (tester) async {
      final handle = tester.ensureSemantics();
      await pumpA11y(
        tester,
        Wrap(
          spacing: AppSpacing.sm,
          children: [
            FilterChoiceChip(label: 'Tümü', selected: true, onTap: () {}),
            FilterChoiceChip(label: 'Ücretsiz', selected: false, dense: true, onTap: () {}),
          ],
        ),
      );

      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      handle.dispose();
    });

    testWidgets('ContactActions düğmeleri', (tester) async {
      final handle = tester.ensureSemantics();
      await pumpA11y(
        tester,
        const ContactActions(
          phone: '+905321110001',
          latitude: 37.37,
          longitude: 36.09,
          website: 'https://kadirli.bel.tr',
        ),
      );

      await expectLater(tester, meetsGuideline(androidTapTargetGuideline));
      handle.dispose();
    });
  });

  group('dokunulabilir her öğenin ekran okuyucu etiketi var', () {
    testWidgets('AppButton', (tester) async {
      final handle = tester.ensureSemantics();
      await pumpA11y(tester, AppButton(label: 'İlan ver', onPressed: () {}));

      await expectLater(tester, meetsGuideline(labeledTapTargetGuideline));
      handle.dispose();
    });

    testWidgets('ContactActions — ikonlu düğmeler etiketsiz kalmamalı', (tester) async {
      final handle = tester.ensureSemantics();
      await pumpA11y(
        tester,
        const ContactActions(phone: '+905321110001', website: 'https://kadirli.bel.tr'),
      );

      await expectLater(tester, meetsGuideline(labeledTapTargetGuideline));
      handle.dispose();
    });
  });

  group('metin kontrastı (WCAG AA)', () {
    for (final brightness in Brightness.values) {
      final themeName = brightness == Brightness.light ? 'açık' : 'koyu';

      testWidgets('durum görünümleri — $themeName tema', (tester) async {
        final handle = tester.ensureSemantics();
        await pumpA11y(
          tester,
          brightness: brightness,
          const Column(
            children: [
              Expanded(child: EmptyView(title: 'Henüz ilan yok', message: 'İlk ilanı siz verin.')),
              OfflineBanner(),
            ],
          ),
        );

        await expectLater(tester, meetsGuideline(textContrastGuideline));
        handle.dispose();
      });

      testWidgets('bilgi şeritleri — $themeName tema', (tester) async {
        final handle = tester.ensureSemantics();
        await pumpA11y(
          tester,
          brightness: brightness,
          const Column(
            spacing: AppSpacing.sm,
            children: [
              InfoBanner(message: 'Doğrulama kodu gönderildi.'),
              InfoBanner(tone: InfoBannerTone.success, message: 'İlanınız gönderildi.'),
              InfoBanner(tone: InfoBannerTone.warning, message: 'Giriş yapmadınız.'),
              InfoBanner(tone: InfoBannerTone.danger, message: 'Oturum süresi doldu.'),
            ],
          ),
        );

        await expectLater(tester, meetsGuideline(textContrastGuideline));
        handle.dispose();
      });

      testWidgets('birincil ve yıkıcı butonlar — $themeName tema', (tester) async {
        final handle = tester.ensureSemantics();
        await pumpA11y(
          tester,
          brightness: brightness,
          Column(
            spacing: AppSpacing.md,
            children: [
              AppButton(label: 'Kaydet', onPressed: () {}),
              AppButton.ghost(label: 'Vazgeç', onPressed: () {}),
              AppButton.danger(label: 'Hesabı sil', onPressed: () {}),
            ],
          ),
        );

        await expectLater(tester, meetsGuideline(textContrastGuideline));
        handle.dispose();
      });
    }
  });

  group('yazı ölçeği 1.4 iken düzen bozulmuyor', () {
    // ⚠️ 1.4 uygulamanın üst sınırı (`app.dart`'ta `withClampedTextScaling`).
    // Taşma bu projede altı kez tam bu ölçekte çıktı → mekanik denetim.
    testWidgets('ortak bileşenler 360 dp × 1.4 ölçekte taşmıyor', (tester) async {
      tester.view.physicalSize = const Size(360, 800) * tester.view.devicePixelRatio;
      addTearDown(tester.view.reset);

      await pumpA11y(
        tester,
        textScale: 1.4,
        SingleChildScrollView(
          child: Column(
            spacing: AppSpacing.md,
            children: [
              AppButton(
                label: 'Cenaze namazının kılınacağı camiyi seç',
                icon: Icons.mosque_rounded,
                expand: true,
                onPressed: () {},
              ),
              const AppTextField(
                label: 'Cenaze namazının kılınacağı cami',
                required: true,
                hint: 'Cami adı',
              ),
              const InfoBanner(
                tone: InfoBannerTone.warning,
                title: 'Giriş yapmadınız',
                message: 'Anonim gönderilen bildirimi daha sonra takip edemezsiniz.',
              ),
              FilterChoiceChip(label: 'Yaklaşan etkinlikler', selected: true, onTap: () {}),
            ],
          ),
        ),
      );

      // `RenderFlex overflowed` bir istisna olarak raporlanır → burada null olmalı.
      expect(tester.takeException(), isNull);
    });

    testWidgets('ulaşım kartı gün rozetleriyle 360 dp × 1.4 ölçekte taşmıyor', (
      tester,
    ) async {
      // 🔴 Faz 12.6 — kart bu fazda üç yeni öge kazandı (araç rozeti, kalkış
      // noktası satırı, saat hapları içinde gün etiketi). Üçü de `Row`/`Wrap`
      // içine giren METİN: bu projenin yedi kez tekrarlayan taşma sınıfı.
      tester.view.physicalSize =
          const Size(360, 1400) * tester.view.devicePixelRatio;
      addTearDown(tester.view.reset);

      await pumpA11y(
        tester,
        textScale: 1.4,
        SingleChildScrollView(
          child: IntercityRouteCard(
            now: DateTime.utc(2026, 8, 3, 12),
            expanded: true,
            onToggle: () {},
            onShare: () {},
            route: IntercityRoute(
              id: 'ic-1',
              destination: 'Kahramanmaraş Elbistan',
              company: 'Kadirli Öz Seyahat Turizm Taşımacılık',
              price: 220,
              durationMinutes: 105,
              vehicleType: 'minibus',
              departurePointName: 'Kadirli Şehirlerarası Otobüs Terminali',
              departurePointAddress:
                  'Cumhuriyet Mahallesi Otogar Caddesi No:1, Kadirli/Osmaniye',
              departurePointLatitude: 37.3745,
              departurePointLongitude: 36.0972,
              schedules: const [
                IntercityDeparture(
                  id: 's1',
                  departureTime: '06:30',
                  days: ['mon', 'tue', 'wed', 'thu', 'fri'],
                ),
                IntercityDeparture(
                  id: 's2',
                  departureTime: '09:15',
                  days: ['mon', 'wed', 'fri'],
                ),
              ],
            ),
          ),
        ),
      );

      expect(tester.takeException(), isNull);
    });

    testWidgets('haber kartı 360 dp × 1.4 ölçekte taşmıyor', (tester) async {
      // 🔴 Faz 12.14 — kart gazeteden gelen metni gösteriyor: başlığın uzunluğu
      // **bizim denetimimizde değil** (manşetler tamamı büyük harf geliyor).
      // Uzun kategori adı ("Bilim ve Teknoloji") + kaydedilmiş rozeti + 1.4
      // ölçek, bu projenin yedi kez tekrarlayan taşma sınıfının haber tarafı.
      tester.view.physicalSize =
          const Size(360, 1600) * tester.view.devicePixelRatio;
      addTearDown(tester.view.reset);

      await pumpA11y(
        tester,
        textScale: 1.4,
        SingleChildScrollView(
          child: NewsCard(
            now: DateTime.utc(2026, 8, 12, 12),
            isSaved: true,
            onTap: () {},
            article: NewsArticle(
              id: 'n1',
              title:
                  'OSMANİYE’DE KAMYONETTE 89 KİLO 550 GRAM UYUŞTURUCU MADDE '
                  'ELE GEÇİRİLDİ, OLAYLA İLGİLİ BİR KİŞİ TUTUKLANDI',
              excerpt:
                  'Osmaniye’de polis ekiplerinin Gaziantep Emniyet Müdürlüğü '
                  'ekipleriyle düzenlediği ortak çalışmada, durdurulan '
                  'kamyonette narkotik köpeği ile arama yapıldı.',
              publishedAt: DateTime.utc(2026, 8, 12, 9),
              modifiedAt: DateTime.utc(2026, 8, 12, 9),
              readingMinutes: 4,
              categories: const [
                NewsCategory(
                  id: 'c1',
                  name: 'Bilim ve Teknoloji',
                  slug: 'bilim-teknoloji',
                ),
              ],
            ),
          ),
        ),
      );

      expect(tester.takeException(), isNull);
    });

    testWidgets('haber gövdesi 360 dp × 1.4 ölçekte taşmıyor', (tester) async {
      // Gövdenin düzenini `flutter_html` kuruyor, yani bir kısmı **bizim
      // widget'larımız değil**: paket sürümü değiştiğinde ilk kırılacak yer
      // burası ve kırılma sessiz olur (ekran açılır, satır taşar).
      tester.view.physicalSize =
          const Size(360, 1600) * tester.view.devicePixelRatio;
      addTearDown(tester.view.reset);

      await pumpA11y(
        tester,
        textScale: 1.4,
        const SingleChildScrollView(
          child: NewsBody(
            html:
                '<p>Osmaniye’de bir dönem Yer Fıstığı Müzesi olarak hizmet '
                'veren simgesel yapı, özgün mimari yapısı korunarak 150 '
                'kişilik halk kütüphanesine dönüştürülüyor.</p>'
                '<h2>Çalışmalarda son durum</h2>'
                '<ul><li>Kaba inşaat tamamlandı</li>'
                '<li>İnce işlerde yüzde 95 seviyesine ulaşıldı</li></ul>'
                '<blockquote>Kısa sürede gençlerin kullanımına '
                'sunulacak.</blockquote>',
          ),
        ),
      );

      expect(tester.takeException(), isNull);
    });
  });

  group('renk tek başına anlam taşımıyor', () {
    testWidgets('offline şeridi metinle de anlatır', (tester) async {
      await pumpA11y(tester, const OfflineBanner());

      expect(find.text('İnternet bağlantısı yok'), findsOneWidget);
    });

    testWidgets('kaydedilmiş haber kartta METİNLE de söylenir', (tester) async {
      // Yer imi ikonu + vurgu rengi tek başına bilgi taşımaz; ekran okuyucu
      // kullanan ya da renk körü biri için "Kaydedildi" **yazılı** olmalı.
      await pumpA11y(
        tester,
        NewsCard(
          now: DateTime.utc(2026, 8, 12, 12),
          isSaved: true,
          article: NewsArticle(
            id: 'n1',
            title: 'Kadirli’de yaz akşamları sinema keyfiyle renkleniyor',
            excerpt: 'Açık hava sineması etkinlikleri sürüyor.',
            publishedAt: DateTime.utc(2026, 8, 12, 9),
            readingMinutes: 2,
            categories: const [
              NewsCategory(id: 'c1', name: 'Gündem', slug: 'gundem'),
            ],
          ),
        ),
      );

      expect(find.text('Kaydedildi'), findsOneWidget);
    });

    testWidgets('palet semantik renkleri iki temada da tanımlı', (tester) async {
      // Renk körü kullanıcılar için kural: her semantik renk bir METİN/ikonla
      // birlikte kullanılır. Bu test paletin eksiksizliğini kilitliyor —
      // eksik bir rol sessizce `null` dönemez.
      for (final palette in [AppPalette.light, AppPalette.dark]) {
        expect(palette.success, isNotNull);
        expect(palette.info, isNotNull);
        expect(palette.warning, isNotNull);
        expect(palette.danger, isNotNull);
        expect(palette.muted, isNotNull);
      }
    });
  });
}
