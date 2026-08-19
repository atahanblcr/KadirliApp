import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

/// Yayın yapılandırması sözleşmesi — Faz 11.16.
///
/// **Neden bu dosya var:** platform yapılandırması (`AndroidManifest.xml`,
/// `Info.plist`) hiçbir Dart testinin uğramadığı bir kör nokta. Buradaki hatalar
/// `flutter run` ile **görünmez** — çünkü debug build'in kuralları farklı — ve ilk
/// kez mağazadan inen uygulamada ortaya çıkar. `ARCHITECTURE.md` §7'nin
/// "bayrakla kapatılmış kod yolu = hiç test edilmemiş kod yolu" maddesinin
/// yapılandırma dosyalarındaki karşılığı budur.
///
/// İki gerçek bulgu bu testleri doğurdu (11.16):
///  - `NSCameraUsageDescription` **yoktu** → iOS'ta kamera anında çökerdi,
///  - `INTERNET` izni ana manifestte **yoktu** → release'e yalnız
///    `firebase_messaging`in manifestinden dolaylı olarak giriyordu.
///
/// ⚠️ Elle liste tutulmuyor: izin gerektiren kullanımlar `lib/` taranarak
/// bulunuyor, böylece yeni bir kamera/galeri çağrısı yapılandırma eksikse test
/// **kendiliğinden** kırmızıya döner.
void main() {
  final androidManifest = File('android/app/src/main/AndroidManifest.xml');
  final infoPlist = File('ios/Runner/Info.plist');

  /// `lib/` altındaki tüm Dart kaynağı — izin gerektiren API kullanımını aramak için.
  String readLibSources() {
    final buffer = StringBuffer();
    for (final entity in Directory('lib').listSync(recursive: true)) {
      if (entity is File && entity.path.endsWith('.dart')) {
        buffer.writeln(entity.readAsStringSync());
      }
    }
    final source = buffer.toString();
    expect(source, isNotEmpty, reason: 'lib/ okunamadıysa test hiçbir şey denetlemez');
    return source;
  }

  group('Android — ana manifest', () {
    test('INTERNET izni AÇIKÇA bildiriliyor', () {
      // Flutter şablonu bu izni yalnız debug/profile manifestlerine koyar.
      // Release'e bugün firebase_messaging'in manifestinden birleşerek giriyor —
      // yani uygulamanın ağa çıkabilmesi bir eklentinin iç detayına bağlı.
      // Push kaldırılırsa ya da eklenti manifestini daraltırsa release build
      // ağa hiç çıkamaz ve debug'da bu ASLA fark edilmez.
      expect(
        androidManifest.readAsStringSync(),
        contains('android.permission.INTERNET'),
        reason: 'INTERNET izni ana manifestte yok — release build ağa çıkamaz, '
            'debug build ise sorunsuz çalışır (izin debug manifestinde var).',
      );
    });

    test('POST_NOTIFICATIONS izni bildiriliyor (Android 13+ push)', () {
      expect(
        androidManifest.readAsStringSync(),
        contains('android.permission.POST_NOTIFICATIONS'),
        reason: 'İzin olmadan Android 13+ cihazlarda bildirim hiç görünmez.',
      );
    });

    test('uygulama adı "Kadirli" (mağaza kimliğiyle tutarlı)', () {
      expect(androidManifest.readAsStringSync(), contains('android:label="Kadirli"'));
    });
  });

  group('Android — yedekleme/aktarma kapalı (Faz 12.23, §7 madde 86)', () {
    // 🔴 Bu grubun tamamı TEK BİR HASARI kapatıyor ve kapının İKİ YÖNÜ var.
    // Android Auto Backup varsayılan olarak AÇIKTIR (manifest bir şey
    // demezse). Açık kaldığında iki şey buluta/yeni cihaza kopyalanıyordu:
    //  (a) `auth.cachedUser` — düz metin profil (KVKK bloğuyla çelişir),
    //  (b) `flutter_secure_storage`ın EncryptedSharedPreferences dosyası —
    //      şifreleme anahtarı CİHAZA BAĞLI, yedekten dönen cihazda ÇÖZÜLEMEZ.
    // (b) Flutter'da en sık bildirilen "oturum bozuldu / dış servis çalışmıyor"
    // vakalarından biridir ve hatanın kaynağı Flutter değil, Android'dir.

    test('allowBackup AÇIKÇA false (varsayılan AÇIK — sessizlik = yedekleme var)', () {
      expect(
        androidManifest.readAsStringSync(),
        contains('android:allowBackup="false"'),
        reason: 'Öznitelik yoksa Android varsayılanı `true`dur: profil önbelleği '
            'düz metin buluta gider ve şifreli jeton deposu yedekten dönen '
            'cihazda çözülemez.',
      );
    });

    test('dataExtractionRules bağlı — allowBackup TEK BAŞINA D2D\'yi kapatmaz', () {
      // Android belgesi birebir: API 31+ hedefleyen uygulamalarda BAZI
      // üreticilerin cihazlarında `allowBackup="false"` bulut yedeklemesini
      // kapatır ama CİHAZDAN CİHAZA aktarımı kapatmaz. Yalnız `allowBackup`
      // yazılsaydı koruma yarım ölü olur ve *"yedekleme kapalı mı?"*
      // sorusunun cevabı YANLIŞ BİR "evet" olurdu — 12.22'nin ölü trigram
      // indeksiyle aynı hasar sınıfı.
      expect(
        androidManifest.readAsStringSync(),
        contains('android:dataExtractionRules="@xml/data_extraction_rules"'),
        reason: 'API 31+ cihazlarda cihazdan cihaza aktarım açık kalır.',
      );
    });

    test('kural dosyası diskte VE iki bölümü de dışlıyor', () {
      final rules = File('android/app/src/main/res/xml/data_extraction_rules.xml');
      expect(
        rules.existsSync(),
        isTrue,
        reason: 'Manifest var olmayan bir @xml kaynağına işaret ederse '
            'derleme kırılır — ama bunu ancak Android derlemesi söyler.',
      );

      final source = rules.readAsStringSync();
      for (final section in const ['cloud-backup', 'device-transfer']) {
        expect(
          source,
          contains('<$section>'),
          reason: '$section bölümü yoksa o yön varsayılanda kalır (= açık).',
        );
      }
      // Bölüm içinde varsayılan "her şey dahil"dir: dışlanmayan alan sessizce
      // aktarılır. `sharedpref` ikisinin de kapsamında olmak zorunda —
      // yedi tercih anahtarı VE şifreli jeton deposu aynı alanda yaşıyor.
      expect(
        '<exclude domain="sharedpref" path="." />'.allMatches(source).length,
        greaterThanOrEqualTo(2),
        reason: 'sharedpref her iki bölümde de dışlanmalı; tek bölümde '
            'dışlamak korumanın yarısını sessizce açık bırakır.',
      );
    });
  });

  group('Geliştirici araçları yayına sızmıyor', () {
    test('dev rotaları KOŞULLU kayıtlı (Env.showDevTools)', () {
      // 🐛 Faz 11.16 bulgusu: rotalar koşulsuz kayıtlıydı. Menü girişleri
      // gizlendiği için "yalnız debug" sanılıyordu, ama `/gelistirici/ag`
      // yayın yapısında da açılabiliyordu — yedi gerçek uca istek atan ve
      // traceId basan bir tanılama ekranı.
      final source = File('lib/core/router/app_router.dart').readAsStringSync();
      final devBlock = RegExp(
        r'if \(Env\.showDevTools\) \.\.\.\[(.*?)\n      \]',
        dotAll: true,
      ).firstMatch(source);

      expect(
        devBlock,
        isNotNull,
        reason: 'Dev rotaları `if (Env.showDevTools) ...[ ]` bloğunda olmalı.',
      );
      for (final route in const ['designPreview', 'networkProbe']) {
        expect(
          devBlock!.group(1),
          contains(route),
          reason: '$route rotası koşullu blokta değil → yayın yapısında açılabilir.',
        );
      }
    });

    test('dev kolaylıkları kDebugMode olmadan açılamaz', () {
      // `showDevTools = isDev && kDebugMode` → FLAVOR=dev ile yapılmış bir
      // RELEASE build'de bile dev araçları kapalı kalır. Bu, "yanlış flavor ile
      // mağazaya çıkma" hatasının ikinci emniyet kemeri.
      final source = File('lib/core/config/env.dart').readAsStringSync();
      expect(
        source,
        contains('isDev && kDebugMode'),
        reason: 'showDevTools yalnız flavor\'a bakarsa, FLAVOR=dev ile alınan bir '
            'release yapısı tanılama ekranlarını mağazaya taşır.',
      );
    });
  });

  group('iOS — Info.plist izin açıklamaları', () {
    /// Bir `Info.plist` anahtarının değerini okur (yoksa null).
    String? plistValue(String key) {
      final source = infoPlist.readAsStringSync();
      final match = RegExp(
        '<key>$key</key>\\s*<string>(.*?)</string>',
        dotAll: true,
      ).firstMatch(source);
      return match?.group(1)?.trim();
    }

    test('kamera kullanılıyorsa NSCameraUsageDescription ZORUNLU', () {
      final usesCamera = readLibSources().contains('ImageSource.camera');
      if (!usesCamera) return; // Kamera kullanılmıyorsa anahtar da gerekmez.

      final value = plistValue('NSCameraUsageDescription');
      expect(
        value,
        isNotNull,
        reason: 'lib/ içinde ImageSource.camera var ama NSCameraUsageDescription yok. '
            'iOS bu durumda izin diyaloğunu bile göstermez, uygulama ANINDA ÇÖKER.',
      );
      expect(value, isNotEmpty);
    });

    test('galeri kullanılıyorsa NSPhotoLibraryUsageDescription ZORUNLU', () {
      final usesGallery = readLibSources().contains('ImageSource.gallery');
      if (!usesGallery) return;

      final value = plistValue('NSPhotoLibraryUsageDescription');
      expect(
        value,
        isNotNull,
        reason: 'lib/ içinde ImageSource.gallery var ama NSPhotoLibraryUsageDescription yok.',
      );
      expect(value, isNotEmpty);
    });

    test('izin açıklamaları Türkçe ve gerekçeli (kullanıcıya gösteriliyor)', () {
      // Değişmez kural 6: arayüz Türkçe. Bu metinler sistem diyaloğunda birebir
      // görünür — Apple da "neden" sorusuna cevap vermeyen metni reddediyor.
      for (final key in const [
        'NSCameraUsageDescription',
        'NSPhotoLibraryUsageDescription',
      ]) {
        final value = plistValue(key);
        if (value == null) continue; // Varlığı yukarıdaki testlerin işi.

        expect(
          value.length,
          greaterThan(30),
          reason: '$key tek kelimelik değil, gerekçeli bir cümle olmalı.',
        );
        expect(
          RegExp(r'[çğıöşüÇĞİÖŞÜ]').hasMatch(value),
          isTrue,
          reason: '$key Türkçe olmalı — kullanıcıya birebir bu metin gösteriliyor.',
        );
      }
    });
  });
}
