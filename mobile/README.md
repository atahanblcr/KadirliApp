# KadirliApp — Mobil (Flutter)

Kadirli şehir uygulamasının mobil istemcisi. Backend (`../KadirliApp.Api`) public API'sini tüketir.

> **Referanslar:** `../Memory_Bank/API_CONTRACT.md` (zarf/hata/auth kontratı) ·
> `../Memory_Bank/MOBILE_UX_PLAN.md` (tasarım sistemi + UX) ·
> `../Memory_Bank/Progress.md` → **Faz 11** (alt-faz planı) · `../docs/openapi.json`.

## Gereksinimler

- Flutter **3.44+** / Dart **3.12+** (`flutter --version`)
- Android SDK + emülatör **veya** Xcode + iOS simülatörü
- Çalışan backend: KadirliApp kökünde `docker compose up -d` ardından `dotnet run --project KadirliApp.Api` (API `:5005`)

## Çalıştırma

```bash
flutter pub get
flutter run                      # dev flavor (varsayılan)
```

**Base URL nasıl seçilir** (`lib/core/config/env.dart`):

| Hedef | URL | Not |
|---|---|---|
| Android emülatörü | `http://10.0.2.2:5005` | ⚠️ emülatörde `localhost` cihazın kendisidir |
| iOS simülatörü / masaüstü | `http://localhost:5005` | host ağı paylaşılır |
| **Gerçek cihaz** | makinenin LAN IP'si | aşağıdaki override ile ver |
| prod | `https://api.kadirli.app` | `--dart-define=FLAVOR=prod` |

```bash
# Gerçek telefonda test (aynı Wi-Fi):
flutter run --dart-define=API_BASE_URL=http://192.168.1.34:5005

# Prod flavor:
flutter run --dart-define=FLAVOR=prod
```

## Kontroller

```bash
flutter analyze          # uyarı/hata kalmamalı
flutter test             # birim + widget + golden testleri
flutter build apk --debug
```

## Golden (görsel regresyon) testleri — Faz 11.15

Dar sütunda `Row` içindeki `Text` taşması bu projede **altı kez** tekrarladı ve
her seferinde el emeğiyle yakalandı. Golden testler bunu mekanikleştiriyor.

```bash
flutter test test/golden                   # yalnız karşılaştır (CI da bunu yapar)
flutter test --update-goldens test/golden  # referans görüntüleri YENİDEN üret
```

- **Referanslar:** `test/golden/goldens/<bileşen>_{light,dark}.png` (depoda tutulur).
- **Matris:** her dosya bileşeni **360 dp** genişlikte ve **1.0 + 1.4 yazı
  ölçeğinde** üst üste gösterir — taşmaların çıktığı tam koşullar.
- **Kapsam:** ortak bileşenler + modül liste kartları. **Tam ekran golden'ı
  YOK**: her metin değişiminde kırılır ve insan "güncelle geç" alışkanlığı
  edinir, testin değeri sıfırlanır.
- **Yazı tipi:** `test/golden/flutter_test_config.dart` `FontManifest.json`'daki
  tüm aileleri (Nunito + MaterialIcons) açıkça yükler. Yüklenmezse Flutter
  varsayılan test fontuyla çizer ve satır kırılmaları gerçek uygulamadan farklı
  olur.
- **Tolerans:** karşılaştırıcı **%0.5** piksel farkına izin verir (makineler
  arası kenar yumuşatma farkı). Gerçek düzen hataları binlerce pikseli
  değiştirdiği için gizlenmez; **boyut değişimi hiç tolere edilmez**.
- ⚠️ **CI golden ÜRETMEZ, yalnız karşılaştırır.** Üretmesine izin verilirse
  hatalı düzen "yeni doğru" diye kaydedilir ve test kendini onaylar.
- Bir golden kırıldığında fark görüntüsü `test/golden/failures/` altına yazılır
  (`.gitignore`'da). Değişiklik kasıtlıysa `--update-goldens` ile yenileyip
  **PNG farkını gözle inceleyin**.

## Erişilebilirlik denetimi

`test/core/accessibility_test.dart` `flutter_test`in yerleşik kılavuzlarını
koşar: metin kontrastı (WCAG AA), 48 dp dokunma hedefi, ekran okuyucu etiketi,
ve **360 dp × 1.4 ölçekte taşma yok** iddiası. `test/core/reduced_motion_test.dart`
"hareketi azalt" ayarına saygıyı kilitler.

## Canlı doğrulama (emülatör / simülatör sürme)

Her alt-faz sonunda ekranlar **gerçek API'ye bağlı** olarak deneniyor. İki
platformda da bunu komut satırından yapabilmek için:

**Android** — `adb` PATH'te değil, tam yol gerekiyor:

```bash
ADB=~/Library/Android/sdk/platform-tools/adb
$ADB exec-out screencap -p > ekran.png
$ADB shell input tap <x> <y>          # screenshot koordinatı birebir
$ADB shell input text "savrun"
$ADB shell input swipe 540 1800 540 400 250   # kaydırma
```

**iOS** — `xcrun simctl` ekran görüntüsü alır ama **dokunuş gönderemez**;
bunun için `tool/ios_sim.sh` yazıldı (macOS erişilebilirlik katmanını kullanır):

```bash
tool/ios_sim.sh check                  # izin + pencere + cihaz ekranı kutusu
tool/ios_sim.sh shot ekran.png
tool/ios_sim.sh tap 808 460            # screenshot koordinatı
tool/ios_sim.sh swipe 600 2200 600 600 # kaydırma
tool/ios_sim.sh text "savrun"
tool/ios_sim.sh key return|escape|delete
```

> **Kurulum:**
> 1. Sistem Ayarları → Gizlilik ve Güvenlik → **Erişilebilirlik**'te terminal
>    uygulamasına izin verilmeli (`check` doğrular).
> 2. `brew install cliclick` — yalnız `swipe` için (~160 KB tek binary).
>    System Events sürükleme olayı üretemiyor; `pyobjc/Quartz` alternatifi
>    ~30-40 MB ve venv gerektirdiği için tercih edilmedi.
>
> ⚠️ **İki tuzak** (31 Tem 2026'da teşhis edildi):
> 1. Simulator penceresi **odakta değilken AX ağacından kaybolur** → `-1719`.
>    Bu izin sorunu sanılıp yanlış teşhis edilebiliyor; gerçek izin hatası
>    `-1743`, `-25204` ise `kAXErrorCannotComplete` — ikisi de izinle ilgisiz.
>    Script her komuttan önce `activate` çağırıyor.
> 2. Koordinat eşlemesinde **pencere kutusu kullanılamaz** (başlık çubuğu +
>    kenar boşluğu içeriyor). Doğru referans, pencerenin içindeki **AXGroup**
>    = cihaz ekranı.
>
> `swipe` ara adımlarla sürüklüyor: tek sıçrayışlı sürükleme bazı listelerde
> hiç hareket ettirmiyor.

## Klasör yapısı (feature-first)

```
lib/
  core/
    config/     env.dart — flavor, base URL, dev bayrakları
    navigation/ app_modules.dart — MODÜL KAYDI (ızgara + rotalar buradan üretilir)
    network/    dio, zarf açma, auth interceptor, token deposu, PagedResult, retry
    router/     go_router yapılandırması + rota sabitleri + alt sekme kabuğu
    theme/      renk/tipografi/boşluk token'ları, ThemeData, tema tercihi
    utils/      görsel URL, tarih biçimleme, telefon numarası, WhatsApp/harita açma
    widgets/    AppButton · AppCard · AppTextField · InfoBanner · AppScaffold
                · AppNetworkImage · Skeleton · Boş/Hata/Offline
  features/
    <modül>/{data,domain,presentation}/
```

**Kurallar**

1. Widget'ta **sabit renk/boyut yazılmaz** — `Theme.of(context).colorScheme`,
   `.palette` (marka/anlam renkleri), `AppSpacing`, `AppRadius` kullanılır.
2. Her liste ekranı dört durumu tanımlar: yükleniyor (skeleton) / boş / hata / offline.
3. Dokunma hedefi min **48dp**; ikon tek başına kullanılmaz, etiketiyle birlikte.
4. Backend kontratı dondu — eksik uç görülürse `Progress.md`'ye not düşülür, backend'e dokunulmaz.

## Ağ katmanı (Faz 11.2)

Repository'ler `ApiClient` kullanır; zarf açma ve hata çevirisi
interceptor'larda yapılır — **dışarı yalnız `ApiException` çıkar.**

```dart
final api = ref.read(apiClientProvider);

final mahalleler = await api.getList('/v1/neighborhoods', Neighborhood.fromJson);
final sayfa      = await api.getPaged('/v1/ads', Ad.fromJson, page: 1, limit: 20);
final ilan       = await api.getObject('/v1/ads/$id', Ad.fromJson);
```

| Parça | Dosya | Not |
|---|---|---|
| Dio kurulumu | `core/network/dio_client.dart` | iki istemci: ana + yenileme (auth'suz) |
| Zarf açma | `interceptors/envelope_interceptor.dart` | `success:false` → `ApiException` (announcements 200-quirk'i dahil) |
| Token + 401 yenileme | `interceptors/auth_interceptor.dart` | tek uçuşlu refresh, rotasyonu saklar, çevrimdışında oturumu düşürmez |
| Token deposu | `core/network/token_store.dart` | `flutter_secure_storage` (+ testler için bellek-içi) |
| Hata sözlüğü | `core/network/error_messages.dart` | `code` → Türkçe mesaj |
| Modeller | `core/network/models/` | `PagedResult<T>`, `ApiMeta` (freezed) |
| Yardımcılar | `core/utils/` | `AppImage.url` · `AppDate` (sabit +03) · `AppLinks` |

Kod üretimi (freezed/json_serializable) gerektiğinde:

```bash
dart run build_runner build       # tek seferlik
dart run build_runner watch       # geliştirirken
```

## Oturum / kimlik doğrulama (Faz 11.3)

Telefon + OTP. Ekranlar **yönlendirme yapmaz**: durumu değiştirir, `go_router`
`redirect`'i karar verir (`core/router/app_router.dart`).

```
/acilis  Splash   → AuthController.bootstrap()  (token var mı → users/me)
/giris   Telefon  → POST /v1/auth/login         (+90 sabit, 10 hane maskeli)
/giris/kod Kod    → POST /v1/auth/verify-otp    (6. hanede otomatik doğrular)
/kayit   Kayıt    → POST /v1/auth/register      (yalnız yeni kullanıcı)
```

| Parça | Nerede | Not |
|---|---|---|
| Durum makinesi | `features/auth/application/auth_state.dart` | `sealed class`: unknown/anonymous/registering/authenticated |
| Oturum sahibi | `application/auth_controller.dart` | token saklama, profil, çıkış, oturum düşmesi |
| Akış durumu | `application/otp_flow_controller.dart` | kod gönder/tekrar gönder/doğrula + geri sayım |
| Uçlar | `data/auth_repository.dart` | yalnız HTTP; hata olarak `ApiException` |
| Korumalı aksiyon | `presentation/widgets/login_required_sheet.dart` | `ensureSignedIn(context, ref, reason: …)` |

**Kurallar**

- Oturum gerektiren bir **aksiyon**: `if (!await ensureSignedIn(context, ref)) return;`
- Oturum gerektiren bir **ekran**: yolunu `AppRoutes.protectedPrefixes`'e ekle — redirect korur.
- Kullanıcı bilgisi: `ref.watch(currentUserProvider)` (oturum yoksa `null`).
- Uygulama **misafir olarak da kullanılabilir**; giriş bir kapı değil davettir.
- `tempToken` (kayıt akışı) **diske yazılmaz**, yalnız bellekte taşınır.
- Telefon numarası her zaman `AppPhone.toE164(...)` ile gönderilir.
- Dev modda (`Otp:DevMode=true`) sunucu kodu yanıtta döner → kod alanı otomatik dolar.

## Kabuk ve Ana Sayfa (Faz 11.4)

Alt sekme kabuğu `core/router/app_shell.dart` (`StatefulShellRoute.indexedStack`):
**Ana Sayfa · İlanlar · Bildirim(rozet) · Profil** — her dalın kendi Navigator'ı
var, sekme değiştirmek durumu ve kaydırmayı bozmaz.

```
/            Ana Sayfa (Hub)   acil şerit + modül ızgarası + öne çıkan duyurular
/ilanlar     İlanlar sekmesi   (11.8'de gerçek liste)
/bildirimler Bildirimler       (11.13'te liste; rozet BUGÜN gerçek veriden)
/profil      Profil            (11.5'te düzenleme)
/ayarlar     Ayarlar/Kontrol   sağ üst ⚙️ — tema, hesap, hakkında
```

**Modül eklemek / rota vermek: tek yer → `core/navigation/app_modules.dart`.**
`kAppModules` her modülün kimliğini, etiketini, ikonunu, rotasını, **hangi fazda
geleceğini** ve **bağlanacağı uçları** taşır; Ana Sayfa ızgarası, modül rotaları
ve "yakında" ekranları hep buradan üretilir. Bir modül gerçeklenince listede
`ready: true` yapılır ve router'da gerçek ekran döndürülür.
`test/core/navigation/app_modules_test.dart` her modülün açılabilir bir ekranı
olduğunu doğrular → **işlevsiz buton kalamaz.**

**Oturum gerektiren sekmeler** (Bildirim/Profil) router'da korumalı **değil**:
misafir sekmenin içinde `SignInPrompt` daveti görür, kabuktan atılmaz.
`AppRoutes.protectedPrefixes` sekme dışı gerçek korumalı ekranlar içindir.

⚠️ **Riverpod 3 tuzağı:** hata veren provider'lar kendiliğinden, sınırsız
yeniden denenir. Uçlara bağlı her provider `retry: apiRetry` almalı
(`core/network/retry_policy.dart`) — yalnız geçici hatalarda (bağlantı/5xx/429)
en fazla 2 tekrar; kalıcı hatada kullanıcıya "Tekrar dene" düğmesi kalır.

## Geliştirici ekranları (yalnız debug)

Ana Sayfa → **Geliştirici** (11.4'ten sonra: **Ayarlar** → Geliştirici):

- **Tasarım sistemi önizlemesi** (`/gelistirici/tasarim`) — palet, tipografi,
  tüm buton/kart varyantları ve durum ekranları, açık+koyu temada.
- **Ağ tanılama** (`/gelistirici/ag`) — açılışta gerçek API'ye 7 uç sorgular:
  base URL doğru mu, zarf açılıyor mu, sayfalama/hata eşlemesi çalışıyor mu.
  Bir modül "veri gelmiyor" derse ilk bakılacak yer burasıdır.

## Font

Nunito (SIL OFL 1.1) — `assets/fonts/`, lisans metni `assets/fonts/OFL.txt`.
Yalnız 3 ağırlık paketlenir: 400 (gövde), 600 (başlık), 700 (vurgu).

## Paket kimliği

Android `applicationId` ve iOS `PRODUCT_BUNDLE_IDENTIFIER`: **`app.kadirli`**.
⚠️ Mağazaya ilk yüklemeden sonra değiştirilemez.
