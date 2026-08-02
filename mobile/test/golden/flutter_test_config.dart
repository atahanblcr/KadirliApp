import 'dart:async';
import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';

import 'golden_harness.dart';

/// `test/golden/` altındaki testler için ortak kurulum.
///
/// Flutter, bir test dosyasının bulunduğu klasörden köke doğru
/// `flutter_test_config.dart` arar → burası **yalnız golden testlerini**
/// etkiler, süitin geri kalanı dokunulmadan kalır.
Future<void> testExecutable(FutureOr<void> Function() testMain) async {
  await loadAppFonts();
  // ⚠️ `LocalFileComparator` taban dizini **dosya** URI'sinin klasörü olarak
  // hesaplar → burada bir dosya adı verilmeli, yoksa golden'lar bir üst
  // klasöre (`test/`) yazılır.
  goldenFileComparator = _TolerantGoldenComparator(
    Uri.file('${Directory.current.path}/test/golden/golden_harness.dart'),
  );
  await testMain();
}

/// Piksel farkına küçük bir pay bırakan karşılaştırıcı.
///
/// **Neden gerekli:** golden'lar geliştirme makinesinde (macOS) üretiliyor,
/// CI'da ise ayrı bir makinede karşılaştırılıyor. Yazı tipi **dosyası** aynı
/// olduğu için satır kırılmaları ve düzen birebir aynı çıkar, ama **kenar
/// yumuşatma** (anti-aliasing) rasterleştiriciye göre birkaç tonluk fark
/// üretebilir. Sıfır toleransla bu, sahte kırmızı demek olurdu.
///
/// ⚠️ Tolerans **düzen hatasını gizlemeyecek** kadar dar tutuldu (%0.5):
/// aradığımız hata sınıfı (taşma çubuğu, kırpılmış metin, kayan hizalama)
/// binlerce pikseli birden değiştirir. Boyut değişimi ise hiç tolere edilmez —
/// yükseklik/genişlik kaydıysa düzen gerçekten değişmiştir.
///
/// 📌 Piksel karşılaştırması `flutter_test`in kendi
/// [GoldenFileComparator.compareLists]'iyle yapılıyor: yalnız eşik için `image`
/// gibi yeni bir paket eklemek gereksiz bir bağımlılık olurdu.
class _TolerantGoldenComparator extends LocalFileComparator {
  _TolerantGoldenComparator(super.testFile);

  /// Farklı olabilecek piksellerin üst sınırı (oran).
  static const double _maxDiffRatio = 0.005; // %0.5

  @override
  Future<bool> compare(Uint8List imageBytes, Uri golden) async {
    // ⚠️ `getTestUri` sürüm numarası içindir, yol çözmez: golden'ın gerçek yeri
    // her zaman `basedir.resolveUri(...)`. Göreli URI'yi doğrudan `File`'a
    // vermek onu çalışma dizinine göre çözer ve dosya "yok" görünür.
    final goldenFile = File.fromUri(basedir.resolveUri(golden));
    if (!goldenFile.existsSync()) {
      throw TestFailure(
        'Golden bulunamadı: $golden\n'
        'Üretmek için: flutter test --update-goldens test/golden',
      );
    }

    final result = await GoldenFileComparator.compareLists(
      imageBytes,
      await getGoldenBytes(golden),
    );

    if (result.passed || result.diffPercent <= _maxDiffRatio) {
      result.dispose();
      return true;
    }

    final percent = (result.diffPercent * 100).toStringAsFixed(2);
    await generateFailureOutput(result, golden, basedir);
    result.dispose();
    throw TestFailure(
      'Golden uyuşmuyor ($golden): piksellerin %$percent\'i farklı '
      '(sınır %${(_maxDiffRatio * 100).toStringAsFixed(2)}).\n'
      'Farkın görüntüsü `test/golden/failures/` altına yazıldı.\n'
      'Değişiklik kasıtlıysa: flutter test --update-goldens test/golden',
    );
  }
}
