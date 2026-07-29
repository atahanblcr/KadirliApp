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
flutter test             # birim + widget testleri
flutter build apk --debug
```

## Klasör yapısı (feature-first)

```
lib/
  core/
    config/    env.dart — flavor, base URL, dev bayrakları
    network/   dio, zarf açma, auth interceptor, token deposu, PagedResult
    router/    go_router yapılandırması + rota sabitleri
    theme/     renk/tipografi/boşluk token'ları, ThemeData, tema tercihi
    utils/     görsel URL, tarih biçimleme, telefon/WhatsApp/harita açma
    widgets/   AppButton · AppCard · AppScaffold · Skeleton · Boş/Hata/Offline
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

## Geliştirici ekranları (yalnız debug)

Ana Sayfa → **Geliştirici**:

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
