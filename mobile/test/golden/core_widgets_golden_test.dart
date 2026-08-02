// Golden'lar üretildikleri platformda karşılaştırılır (yazı tipi
// rasterleştirmesi işletim sistemine göre değişir) → CI'da ayrı bir
// macOS işinde koşarlar. Bkz. `.github/workflows/mobile.yml`.
@Tags(['golden'])
library;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:kadirli_app/core/widgets/widgets.dart';

import 'golden_harness.dart';

/// Ortak bileşenlerin görsel regresyonu (11.15).
///
/// Bu bileşenler **her ekranda** kullanılıyor: birinde çıkan taşma tek bir
/// modülü değil uygulamanın tamamını ilgilendiriyor (11.11'de `LookupDropdown`
/// ve `AppTextField` etiketleri tam da böyle taşmıştı).
///
/// Her senaryo bilerek **uzun Türkçe metinle** kuruldu — kısa etiketle hiçbir
/// düzen hatası ortaya çıkmaz, dolayısıyla golden de bir şey korumaz.
void main() {
  testWidgets('AppButton — varyantlar, uzun etiket, devre dışı, yükleniyor', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'app_button',
      height: 1500,
      scenarios: [
        GoldenScenario(
          'Varyantlar',
          Column(
            spacing: 8,
            children: [
              AppButton(label: 'Kaydet', onPressed: () {}),
              AppButton(label: 'Hemen ara', variant: AppButtonVariant.accent, onPressed: () {}),
              AppButton.ghost(label: 'Vazgeç', onPressed: () {}),
              AppButton.danger(label: 'Hesabı sil', onPressed: () {}),
            ],
          ),
        ),
        GoldenScenario(
          'Uzun etiket + ikon (taşma riski)',
          AppButton(
            label: 'Cenaze namazının kılınacağı camiyi seç',
            icon: Icons.mosque_rounded,
            expand: true,
            onPressed: () {},
          ),
        ),
        const GoldenScenario(
          'Devre dışı + yükleniyor',
          Column(
            spacing: 8,
            children: [
              AppButton(label: 'Gönder'),
              AppButton(label: 'Gönderiliyor', loading: true, expand: true),
            ],
          ),
        ),
      ],
    );
  });

  testWidgets('AppCard + SectionHeader', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'app_card',
      height: 1200,
      scenarios: [
        const GoldenScenario(
          'Başlık + kart',
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              SectionHeader(
                title: 'Nöbet takvimi',
                subtitle: 'Nöbetçi eczanesi olan günler işaretli',
              ),
              AppCard(child: Text('Kart gövdesi — normal içerik.')),
            ],
          ),
        ),
        const GoldenScenario(
          'Şeritli kart (red gerekçesi)',
          AppCard(
            accentStripe: Color(0xFFD64545),
            child: Text(
              'Yayınlanmama gerekçesi: İlan görselleri yeterince net değil, '
              'lütfen ürünü gösteren fotoğraflarla tekrar deneyin.',
            ),
          ),
        ),
      ],
    );
  });

  testWidgets('AppTextField — etiket/ipucu/hata/ön ek', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'app_text_field',
      height: 1500,
      scenarios: [
        const GoldenScenario(
          'Uzun zorunlu etiket (11.11 taşması)',
          AppTextField(
            label: 'Cenaze namazının kılınacağı cami',
            hint: 'Cami adı',
            required: true,
          ),
        ),
        const GoldenScenario(
          'Ön ekli + yardımcı metin',
          AppTextField(
            label: 'Telefon',
            prefixText: '+90',
            hint: '532 111 00 01',
            helper: 'Doğrulama kodu bu numaraya gönderilecek.',
          ),
        ),
        const GoldenScenario(
          'Hatalı alan',
          AppTextField(
            label: 'Kullanıcı adı',
            errorText: 'Bu kullanıcı adı zaten alınmış, başka bir tane deneyin.',
          ),
        ),
      ],
    );
  });

  testWidgets('LookupDropdown — dolu / yükleniyor / hata / boş', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'lookup_dropdown',
      height: 1600,
      scenarios: [
        GoldenScenario(
          'Dolu (uzun etiket)',
          LookupDropdown<String>(
            label: 'Cenaze namazının kılınacağı cami',
            isRequired: true,
            items: const AsyncValue.data(['Ulu Cami', 'Ala Cami']),
            value: 'Ulu Cami',
            idOf: (item) => item,
            labelOf: (item) => item,
            onChanged: (_) {},
            onRetry: () {},
          ),
        ),
        GoldenScenario(
          'Yükleniyor',
          LookupDropdown<String>(
            label: 'Mahalle',
            items: const AsyncValue.loading(),
            value: null,
            idOf: (item) => item,
            labelOf: (item) => item,
            onChanged: (_) {},
            onRetry: () {},
          ),
        ),
        GoldenScenario(
          'Hata',
          LookupDropdown<String>(
            label: 'Mahalle',
            items: AsyncValue.error('boom', StackTrace.empty),
            value: null,
            idOf: (item) => item,
            labelOf: (item) => item,
            onChanged: (_) {},
            onRetry: () {},
          ),
        ),
      ],
    );
  });

  testWidgets('FilterChoiceChip — seçili/seçilsiz, ikonlu/ikonsuz, dense', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'filter_chip',
      height: 900,
      scenarios: [
        GoldenScenario(
          'Şerit (360 dp\'ye sığmalı)',
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              FilterChoiceChip(label: 'Tümü', selected: true, onTap: () {}),
              FilterChoiceChip(
                label: 'Yaklaşan',
                icon: Icons.event_rounded,
                selected: false,
                onTap: () {},
              ),
              FilterChoiceChip(label: 'Ücretsiz', selected: false, dense: true, onTap: () {}),
            ],
          ),
        ),
      ],
    );
  });

  testWidgets('InfoBanner — dört ton', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'info_banner',
      height: 1500,
      scenarios: [
        const GoldenScenario(
          'Tonlar',
          Column(
            spacing: 8,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              InfoBanner(message: 'Doğrulama kodu geliştirme modunda otomatik dolduruldu.'),
              InfoBanner(
                tone: InfoBannerTone.success,
                message: 'İlanınız gönderildi, onaydan sonra yayına alınacak.',
              ),
              InfoBanner(
                tone: InfoBannerTone.warning,
                title: 'Giriş yapmadınız',
                message: 'Anonim gönderilen bildirimi daha sonra takip edemezsiniz.',
              ),
              InfoBanner(
                tone: InfoBannerTone.danger,
                message: 'Oturumunuzun süresi doldu, lütfen tekrar giriş yapın.',
              ),
            ],
          ),
        ),
      ],
    );
  });

  testWidgets('ContactActions — veri varken ve kısmen eksikken', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'contact_actions',
      height: 900,
      scenarios: [
        const GoldenScenario(
          'Hepsi dolu',
          ContactActions(
            phone: '+905321110001',
            latitude: 37.37,
            longitude: 36.09,
            mapLabel: 'Karatepe Aslantaş Açık Hava Müzesi',
            website: 'https://kadirli.bel.tr',
            email: 'bilgi@kadirli.bel.tr',
          ),
        ),
        const GoldenScenario(
          'Yalnız telefon (buton uydurulmaz)',
          ContactActions(phone: '+905321110001'),
        ),
      ],
    );
  });

  testWidgets('Durum görünümleri — boş / hata / offline şeridi', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'state_views',
      height: 2000,
      scenarios: [
        GoldenScenario(
          'Boş durum',
          SizedBox(
            height: 380,
            child: EmptyView(
              icon: Icons.sell_outlined,
              title: 'Henüz ilan yok',
              message: 'İlk ilanı siz verin — onaylandıktan sonra burada görünecek.',
              actionLabel: 'İlan ver',
              onAction: () {},
            ),
          ),
        ),
        GoldenScenario(
          'Hata durumu',
          SizedBox(
            height: 430,
            child: ErrorView(
              message: 'İçerik yüklenemedi. Lütfen tekrar deneyin.',
              traceId: '00-abc123-def456-01',
              onRetry: () {},
            ),
          ),
        ),
        const GoldenScenario('Offline şeridi', OfflineBanner()),
      ],
    );
  });

  testWidgets('Skeleton — yükleniyor iskeleti', (tester) async {
    await expectGoldenSheet(
      tester,
      name: 'skeleton',
      height: 900,
      scenarios: [
        const GoldenScenario(
          'Kart iskeleti',
          SizedBox(
            height: 220,
            child: SkeletonCardList(itemCount: 2, padding: EdgeInsets.zero),
          ),
        ),
      ],
    );
  });
}
