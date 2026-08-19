# KadirliApp - Proje İlerleme Durumu (Progress.md)

Bu dosya, projenin başından itibaren atılan adımları detaylı olarak barındırır. Herhangi bir bağlam kaybında (context reset) buraya bakılarak nelerin tamamlandığı anlaşılabilir.

---

## 🚦 AÇIK MADDELER PANOSU — *tek bakışta ne kaldı*

> **Son doğrulama: 20 Ağustos 2026** (12.23 kapandı; `SharedPreferencesAsync` geçişi **tetikleyici koşuluyla** B'ye eklendi)
> *(önceki: 19 Ağustos 2026 — 12.22)* (kutulara değil **koda/şemaya** bakılarak; aynı gün
> ikinci kez — **denetim oturumu**: 210 sayfalık canlı panel taraması + 11 mutasyonluk
> bozma turu, üç bulgu **12.20** olarak açıldı).
> Bu pano **yalnız AÇIK maddeleri** listeler; bir madde kapandığında satırı **silinir**
> (işaretlenmez). Gerekçe: bu dosya 5000+ satır ve durum bilgisi beş ayrı yere dağılmıştı —
> 13 Ağu denetiminde **22 kutunun 21'i** bayat, iki başlık da yanlış çıktı. Büyüyen bir liste
> yine çürür; **küçülen bir liste** çürümez.
>
> ⚠️ Bu panoyu hiçbir test denetlemiyor (bilinçli karar — bkz. *"Progress.md doc-test'e
> bağlanmayacak"*). Tek güvence, kapatan kişinin satırı **silmesi**.

### A. Ürün — açık maddeler

| Madde | Nerede | Durum / blokaj |
|---|---|---|
| **12.8 — Sosyal giriş: mobil** | `### 12.8` | 🔴 **Apple Developer aboneliği onaylanmadı** (13 Ağu itibarıyla bekleniyor). 🟢 **Google ayağı bugün yazılabilir** — backend hazır ve kapalı-sağlayıcı dalı **test edilmiş** |
| 🔴 **SMS SAĞLAYICISI — yayının TEK GERÇEK BLOKAJI** | `# ✅ 12.21 TESLİM` | 12.21'de **ölçüldü**: API `Production`'da **hiçbir `Sms:Provider` değeriyle açılmıyor** — `Dev` readiness kapısına takılıyor (haklı: SMS gitmezse kimse giremez), başka bir ad ise DI'da *"Bilinmeyen SMS sağlayıcısı"* veriyor, çünkü **gerçeklenmiş başka sağlayıcı yok**. 🔑 Blokaj **doğru**; 12.21'de kapatılan şey *sessizliğiydi* (kapının mesajı artık ne yapılacağını söylüyor, `SmsProviderAgreementTests` uyumu kilitliyor). Gereken: bir `ISmsService` gerçeklemesi (NetGSM vb.) + sağlayıcı anlaşması → `Infrastructure/Notifications/` + `SmsProviders` + `DependencyInjection.SmsImplementations`. 📌 **Panel bu blokajın dışında** — `Production`'da bugün de açılıyor (canlı doğrulandı) |
| 📌 **Hukuki metinlerin GERÇEK içeriği** | 12.16/12.17 notları | 🔴 **Kod işi değil, İNSAN işi** ve yayından önce zorunlu. Zincirin tamamı çalışıyor (12.17 canlı doğrulandı) ama bugün yayında olan metinler **test metnidir** — yerel veritabanında, benim yazdığım örnekler. Gerçek KVKK aydınlatma + açık rıza metnini **hukukçu** yazmalı; kod onu bekliyor, tahmin etmiyor (12.16 kararı: metin **seed edilmez**) |

### B. Karar bekleyenler (kod değil, tercih)

| Madde | Nerede | Ne gerekiyor |
|---|---|---|
| **12.18 adayı — kategori bazlı bildirim aboneliği** | *"açık kalan / ertelenen maddeler"* | *(12.16 adayıydı, KVKK öne alındığı için kaydı.)* Ön koşul: 12.15'in **elle gönderimi canlıda doğrulanmalı**. ⚠️ İkinci bir dispatcher yazılmaz |
| 🆕 **Herkese açık gizlilik metni ADRESİ** | 12.20a notu · `# ✅ 12.21 TESLİM` | 12.20a `/Home/Privacy`'yi sildi (İngilizce, iskeleden kalma bir yer tutucuydu) ve panelde artık **hiç** gizlilik metni adresi yok. 🔴 **Play Console ve App Store Connect yayın için herkese açık bir URL İSTİYOR** ve bugün metin yalnız mobil uygulamanın içinde okunabiliyor. Altyapı **hazır**: `legal_documents` + yayında sürüm + anonim `GET /v1/legal/documents/{type}` (12.16). Eksik olan tek şey **karar**: metin nerede sunulacak (panelde anonim bir sayfa mı, ayrı bir statik site mi)? ⚠️ Panelde anonim sayfa açmak 12.20a'nın **az önce kapattığı** yüzeyi yeniden açmak demek — bilinçli verilmeli. 📌 Ön koşulu zaten açık: metnin **gerçek içeriği** (bir alt satır) |
| 🆕 **`/v1/power-outages` tarih penceresi** | `Memory_Bank/Performance_Baseline.md` §3 | 🔬 **12.22'de ÖLÇÜLDÜ:** uç sayfalamıyor (§7 madde 1) ve **gövde doğrusal büyüyor** — 10.000 satırda **3,7 MB**, 20.000'de **7,5 MB**. 🔑 Sunucu tarafı sorun DEĞİL (20k'da 31 ms), sorun **vatandaşın mobil bağlantısına inen hacim** → cache bunu çözmez, **tarih penceresi** çözer. 🔴 Ama bu bir **KONTRAT** kararı: mobil listede **geçmiş kesintileri de gösteriyor** (*"Sona erdi"*, `power_outage_tile.dart`), yani pencere mağazadaki eski sürümlerde **görünen** bir davranış değişikliğidir. Ölçüm hazır, karar ürün tarafında |
| **Haber gövde override'ı** | Haberler bloğu | İkinci sürümde **eklemeli** alan olarak (tam override değil) |
| **Haber arşiv derinliği (bugün 50, yerel DB'de 180)** | Haberler bloğu · `Memory_Bank/Performance_Baseline.md` §3 | 🔬 **12.22'de ÖLÇÜLDÜ ve tahmin DÜZELDİ:** 102 haberlik gerçek koşu = **3 liste isteği + 178 görsel + 32,5 MB**. Tamamı (27.284) → ~273 istek ✅ ama **~8,9 GB görsel**, ~1,6 GB değil. Fark bir tahmin hatası değil: **12.14b metin arası görselleri de aynalamaya başladı** (~5,5×). **Kod değişikliği gerekmiyor, karar gerekiyor** |
| 🆕 **`SharedPreferencesAsync` geçişi — YAPILMADI, TETİKLEYİCİSİ YAZILI** | `# ✅ 12.23 TESLİM` → *"S6"* | 🔬 12.23'te **incelendi ve bilinçle ertelendi**. Bugün kullanılan `SharedPreferences.getInstance()` paketin **legacy** API'sidir; yenisi `SharedPreferencesAsync` / `SharedPreferencesWithCache` (Android'de DataStore). **Neden bugün gerekmiyor:** legacy'nin bilinen tek gerçek zaafı, bellek içi anlık görüntüsünün **başka bir isolate'in yazdığını görmemesi**; bu projede arka plan isolate'i (`firebaseMessagingBackgroundHandler`) prefs'e **hiç dokunmuyor** (yalnız `debugPrint`) ve `reload()` kod tabanında **hiç çağrılmıyor** (ölçüldü). 🔴 **TETİKLEYİCİ KOŞUL — bu yazıldığı gün geçiş ZORUNLU olur:** arka plan isolate'inde prefs'e **yazma** ihtiyacı doğduğu an (ör. *"arka planda gelen bildirimi okundu işaretle"*, *"son senkron zamanını yaz"*). O gün geçilmezse hasar sessizdir: ikinci isolate yazar, ana isolate **hiç görmez**, hatta üstüne yazar; hata yok, log yok. **Nasıl yapılmalı:** (1) `shared_preferences`ın **resmî göç aracı** var — `lib/util/legacy_to_async_migration_util.dart` (2.5.5'te mevcut, doğrulandı) — elle kopyalama yazma; (2) Android'de legacy XML ile DataStore **ayrı depolardır**, göç **tek yönlüdür** ve yarım kalırsa kullanıcının kaydedilenleri/misafir tercihi **kaybolur** → backend'deki idempotent geri doldurma migration'larıyla **aynı ciddiyet**; (3) `SharedPreferencesWithCache` seçilmeli, düz `SharedPreferencesAsync` değil: `themeModeProvider` · `newsTextScaleProvider` · `SavedNewsController.build()` **senkron** okumaya dayanıyor ve *"ilk kare doğru"* tasarımı (§7 madde 85) buna bağlı; (4) tek sahip zaten var (`core/preferences/app_preferences.dart`), değişecek yer **orası ve altı çağrı yeri**. ⚠️ iOS'ta legacy'nin `flutter.` öneki + `cfprefsd` önbelleği bu projede **zaten bir kez ısırdı** (`Progress.md` 11.4 notu: plist'e enjeksiyon tutmadı) |
| **Progress.md arşivleme** | *"Progress.md'nin şekli"* | Faz 12 kapanınca 11+12 → `Progress_Archive.md` |

### C. Deploy / yayın fazı (mobil geliştirmeyi bloklamaz, **yayından önce zorunlu**)

| Madde | Nerede | Not |
|---|---|---|
| 🍎 **Apple ekosistemi** | 11.16 notları | Abonelik · sertifikalar · TestFlight · App Store Connect · **APNs `.p8`** · mağaza görselleri. **12.8'in tek blokajı** |
| 🤖 **Play** | 11.16 notları | Yayın anahtarı (`keytool`) + Play Console → internal test |
| **Dağıtım HEDEFİ (sunucu/alan adı)** | `# ✅ 12.21 TESLİM` | 🟢 **Hat kuruldu** (Dockerfile'lar · `.dockerignore` · `compose.prod` · `release.yml` → `ghcr.io`, etiket = commit SHA). ⚠️ Eksik olan **kod değil karar**: imajlar nereye gidecek? 12.21 bunu bilinçli olarak kapsam dışı bıraktı ve iş akışının adı da bunu söylüyor (*Release*, *Deploy* değil) |
| **`uploads/` kalıcı volume** | `10.14/(3)` | Bugün risk **yok** (API compose'da değil). API konteynerleştiği gün doğar |
| **Seq production kimlik doğrulaması** | `docker-compose.yml` | Yerelde bilinçli olarak kapalı; production'da `SEQ_FIRSTRUN_ADMINPASSWORD` + API key |

### D. Bilinçli olarak ertelenmiş (ölçüldü — "yapılmadı" değil, **"gerekmediği ölçüldü"**)

| Madde | Nerede | Neden ertelendi |
|---|---|---|
| **Anemik domain + Domain Events** | dış analiz notları | İki kez ölçüldü, **somut kazanç bulunamadı** (ilan listesi zaten cache'li değil; proje açık tek-sahip arayüzleri kullanıyor) → **Faz 13 adayı** |
| **`IQueryable` sızıntısı** | dış analiz notları | Canlı zarar arandı, **bulunamadı** (12 `SoftRemove` çağrısının hepsi izlenen nesnede) |
| **Madde 67'nin duman testi** | `Memory_Bank/Contract_Audit.md` | Bu ortamda **vakum** ve artık **adı bunu söylüyor** (`SmokeCheck_…_VacuousOnAFreshDatabase`); gerçek kilit yanındaki testte |

📌 **Bunların dışında açık madde yoktur.** Görünmez sözleşme denetimi (Faz 0 · B1–B7 · T1/T2 ·
Faz A) **bitti**; bugün **80 maddenin** 79'u 🟢/🟢🟢 — tablo `Memory_Bank/Contract_Audit.md`
(68–70 denetimden sonra Faz 12.7'de, 71–74 Faz 12.16'da, 75–77 Faz 12.17'de,
**78–80 Faz 12.19'da** eklendi).
⚠️ **Tek 🟠 madde 80'dir ve bir eksiklik değil, bilinçli ve belgelenmiş bir SINIRDIR:**
`CommentReferenceTests` yorumdaki *sarkan işaretçiyi* yakalar, **yanlış iddiayı yakalayamaz**.
Kilit kendi belgesinde bunu yazıyor (madde 67'nin `VacuousOnAFreshDatabase` deseni).

---

## Faz 0 - İskelet ve Kurulum
- [x] **Solution ve Projelerin Oluşturulması**: Clean Architecture yapısına uygun olarak `KadirliApp.Domain`, `KadirliApp.Application`, `KadirliApp.Infrastructure`, `KadirliApp.Api` ve `KadirliApp.Web` (MVC Panel) projeleri `dotnet new` komutlarıyla yaratıldı.
- [x] **Bağımlılıklar (NuGet)**: EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL, MediatR, FluentValidation, BCrypt.Net-Next, JwtBearer vb. paketler ilgili projelere eklendi.
- [x] **Docker Altyapısı**: `docker-compose.yml` oluşturularak PostgreSQL (15) ve Redis servisleri konfigüre edildi. Localde başarıyla ayağa kaldırıldı.
- [x] **Temel Nesneler**: `BaseEntity`, `ISoftDeletable` arayüzleri eklendi. Global exception filtreleri ve `ApiResponse<T>` modelleri oluşturuldu.

## Faz 1 - Persistence Çekirdeği (Veritabanı Tasarımı)
- [x] **Identity Modülü Entity'leri**: `User`, `Neighborhood`, `UserNeighborhood`, `AdminPermission` nesneleri `KadirliApp.Domain` içinde detaylı kolonlar ve navigasyon property'leri ile tanımlandı.
- [x] **Yetki ve Diğer Modül Entity'leri**: `Permission`, `RolePermission`, `AuditLog`, `Announcement`, `PowerOutage` vb. eklendi.
- [x] **Kapsamlı Ads Modülü Entity'leri**: İlan, Vefat, Eczane (Ads, AdCategory, AdImage vb.) EF Core nesnelerine çevrildi.
- [x] **AppDbContext ve EF Core Konfigürasyonları**: Toplam ~45 tablo `AppDbContext` içerisinde DbSet olarak kaydedildi. `IEntityTypeConfiguration<T>` kullanılarak Fluent API konfigürasyonları (max length, default value, unique index, soft delete query filters vb.) `KadirliApp.Infrastructure/Persistence/Configurations` içerisine yazıldı.
- [x] **Migration & Veritabanı Güncellemesi**:
  - Postgres `users_role_enum` için EF Core Type Mapping sorunu çözüldü (Property tipi enum kalsa da veritabanında `varchar(20)` olarak ayarlanıp Npgsql çakışmaları önlendi).
  - `InitialSchema` adlı migration başarıyla oluşturuldu ve `dotnet ef database update` ile Postgres'e basıldı.
  - İlanlarda arama yapılabilmesi için özel SQL komutları (`CREATE EXTENSION pg_trgm`) ve GIN indeksleri (`ix_ads_title_trgm`) için ikinci bir migration (`AddTrgmExtension`) yazılıp veritabanına uygulandı.

## Faz 2 - Kimlik Doğrulama & Yetkilendirme (Identity)
- [x] **Arayüzler (Interfaces)**: `KadirliApp.Application/Common/Interfaces` altına `IJwtProvider`, `ICurrentUserService` ve `IPasswordHasher` arayüzleri eklendi.
- [x] **Implementasyonlar (Infrastructure)**: `JwtProvider` token üretimi için eklendi. `PasswordHasher` BCrypt algoritması ile yazıldı. `CurrentUserService` HttpContext üzerinden claim'leri okuyacak şekilde `KadirliApp.Api/Services` içerisinde geliştirildi.
- [x] **CQRS Auth Yapısı**: `Application/Features/Auth` klasörü altında `LoginCommand`, `LoginCommandHandler`, `VerifyOtpCommand` MediatR yapıları oluşturuldu. DTO'lar eklendi.
- [x] **API & Controller**: `KadirliApp.Api/Controllers/AuthController.cs` yazıldı, Swagger'a açıldı. 
- [x] **Security Configuration**: `Program.cs` içerisinde JwtBearer doğrulamaları ve `AdminPanel`, `SuperAdmin` bazlı Policy tabanlı Authorization kuralları eklendi.

## Faz 3 - Generic Altyapı & Temel CQRS Modülleri
- [x] **Veri Erişim Altyapısı**: `IRepository<T>`, `IUnitOfWork` (EF Core tabanlı yazma işlemleri için) ve `IDapperContext` (Dapper tabanlı performanslı okuma işlemleri için) oluşturuldu ve `DependencyInjection.cs` dosyasına Scoped olarak eklendi.
- [x] **Base Controller**: Ortak metotları barındıran `ApiControllerBase` sınıfı oluşturularak tüm controller'ların kalıtım alacağı yapı hazırlandı.
- [x] **Ads (İlan) Modülü CQRS**: İlanlar modülü için `CreateAdDto`, `GetAdsQuery`, `GetAdsQueryHandler` gibi CQRS unsurları ve `AdsController` tamamlandı.
- [x] **Places & Guide (Mekan/Rehber) Modülü CQRS**: Mekan ve şehir rehberi modülü için uç noktalar, command/query handler sınıfları eklendi. Proje son olarak sıfır hatayla derlendi.

## Faz 4 - Kalan Core Modüllerin (Kullanıcı, Duyuru, Vefat, Eczane, Ulaşım vb.) CQRS Endpoints
- [x] Users (Kullanıcılar) modülü CQRS ve Controller oluşturulması. (Profil görüntüleme ve güncelleme API'leri).
- [x] Announcements (Duyurular) ve PowerOutages (Elektrik Kesintileri) modüllerinin oluşturulması.
- [x] Deaths (Vefat İlanları) ve Pharmacies (Nöbetçi Eczaneler) modüllerinin oluşturulması.
- [x] Taxis (Taksiler), Intercity/Intracity Routes (Ulaşım) modüllerinin oluşturulması.

## Faz 5 - Admin Panel (KadirliApp.Web - MVC) ve Background Jobs (Hangfire)
- [x] KadirliApp.Web MVC Projesinde genel Layout ve Auth mantığının oturtulması (Tailwind CSS, DashboardController).
- [x] Hangfire entegrasyonu (ExpireAdsJob, ArchiveDeathsJob).
- [x] S3 / Local Storage dosya yükleme (File Upload) modülünün tamamlanması (`IFileStorageService` ve `FilesController`).

---

# ✅ FAZ 6 - "İŞLEVSİZ BUTON KALMASIN" BÜYÜK ONARIM TURU (3-4 Temmuz 2026) — TAMAMLANDI

> **DURUM (4 Temmuz 2026):** Faz 6'nın TÜM maddeleri (A-K) tamamlandı ve canlı smoke test ile doğrulandı. Build 0 hata. Web paneli (admin/Admin123!) ve API (OTP akışı) uçtan uca çalışıyor. Detaylı test çıktısı: `Live_Test_Report.md`.

## 🔍 Bu Fazı Tetikleyen Denetim Bulguları (3 Temmuz 2026, canlı doğrulandı)

Önceki Memory Bank kayıtları ("kusursuz kuruldu", "production'a hazır") **gerçeği yansıtmıyordu.** Tam stack ayağa kaldırılıp (Docker Postgres+Redis, Web :5002, `admin/admin` girişi, tüm Index sayfaları curl ile gezildi) yapılan denetimde:

1. **Application katmanının büyük kısmı İÇİ BOŞ STUB'dı:** ~26 handler `// TODO` + `Task.FromResult(boş sonuç)` döndürüyordu. Ads listesi (GetAds), Deaths (5 handler), Pharmacies (5), Guide (4), Places (3), Taxis (3), Transport (GetIntracity/GetIntercity/CreateIntercity), Auth API (Login/VerifyOtp hardcoded "123456") — yani admin panelde Create/Edit/Delete butonları "başarılı" toast'ı basıyor ama **DB'ye hiçbir şey yazmıyordu**, listeler her zaman boş dönüyordu. Kullanıcının "butonların işlevi yok" tespiti DOĞRUYDU.
2. **Veritabanı tamamen boştu, seed mekanizması yoktu:** users, neighborhoods, announcement_types, ad_categories... tüm tablolar 0 satır. Zorunlu dropdown'lar boş geldiği için formlar submit bile edilemiyordu.
3. **Web paneli girişi sahteydi:** `AccountController.Login` "admin/admin" hardcoded string karşılaştırması + her girişte RASTGELE yeni GUID'i NameIdentifier claim'i yapıyordu → Approve aksiyonları `approved_by` kolonuna anlamsız GUID yazıyordu.
4. **Users create düz metin şifre kaydediyordu** (`Password = dto.Password`, hash yok).
5. **Ads create `UserId` set etmiyordu** (Guid.Empty yazılıyordu).
6. **Events, Campaigns, Complaints modülleri hiç yoktu** (sadece boş DB tabloları var; Application/Api/Web katmanları yazılmamış).
7. **Places ve Taxi'nin admin panel controller/view'ları yoktu** (yalnızca API tarafı vardı, o da stub'dı).
8. **Dashboard tamamen sahteydi:** hardcoded "1,245 kullanıcı / 324 ilan / 18 onay / 8,432 ziyaretçi" + "Paneli Test Verileriyle Doldur" butonu sadece "yakında" toast'ı basıyordu.
9. AdsAdmin Create formunda kategori alanı "Kategori ID (Geçici)" adında ham GUID text input'uydu; DeathsAdmin Create'te mezarlık/cami/mahalle dropdown'ları yoktu.

## ✅ FAZ 6 ÇALIŞMA PLANI (A–K) — hepsi tamamlandı (4 Temmuz 2026)

> Adım adım uygulama talimatları (hangi dosyaya ne yazılacağı) kaldırıldı — iş bitti, kod
> `ARCHITECTURE.md`'deki modül tablosundan okunuyor. Kalıcı olan **kararlar** aşağıda.

- **A. Stub handler'lar gerçek EF Core'a bağlandı** (Ads/Deaths/Pharmacies/Guide/Places/Taxis/Transport/Auth).
  ⚠️ Buradan çıkan ve **hâlâ geçerli** olan kural: `EF.Functions.ILike` Application katmanında
  derlenmez (Npgsql'e özel, katman kuralı) → projenin her yerinde arama deseni
  `x.Alan.ToLower().Contains(term.ToLower())`. Tek bir yerde farklı semantik kullanmak,
  modül listesiyle global aramanın aynı terimde farklı sonuç vermesi demektir (bkz. 11.16b).
- **B. Seed altyapısı** — `Infrastructure/Persistence/DbSeeder.cs`, `SeedAsync` idempotent
  (her blok kendi tablosu boşsa çalışır): super_admin + 10 mahalle + duyuru/ilan/etkinlik/işletme/mekan/rehber
  kategorileri + mezarlık + cami. Türkçe karakterli `Slugify` **`SlugHelper`'a delege eder**
  (görünmez sözleşme #21 — ikinci bir slug gerçeklemesi yazılmaz).
  ⚠️ Varsayılan panel parolası `Admin123!`'tür ve **11.18'de zorunlu parola değişimi kapısı geldi**:
  parolayı sahibi değil sistem belirlediyse panel hiçbir sayfayı açmaz.
- **C. Gerçek panel girişi** — `admin/admin` hardcoded bloğu silindi; `IPasswordHasher` doğrulaması +
  rol kapısı + `NameIdentifier = gerçek user.Id`. Öncesinde her girişte rastgele GUID claim'i
  yazıldığı için `approved_by` kolonu anlamsız değerlerle doluyordu.
- **D–H. Eksik dikey kesitler yazıldı** — Events / Campaigns / Complaints modülleri (Application+Api+Web),
  Places + Taxi panel controller/view'ları, Ads ve Deaths formlarındaki ham GUID input'ları dropdown'a çevrildi.
- **I. Dashboard'daki uydurma metrikler kaldırıldı** — hardcoded "1,245 kullanıcı / 8,432 ziyaretçi"
  gerçek `Count` sorgularına bağlandı; **ölçülmeyen metrik gösterilmez** kuralıyla "Günlük Ziyaretçi"
  kartı "Toplam Duyuru" oldu (telemetri yok, yalan metrik bırakılmadı). `MockDataSeeder` yazıldı.
- **J. Canlı smoke test** — detay `Live_Test_Report.md`. ⚠️ Buradan çıkan kalıcı ders:
  **eski process yeni kodu yansıtmaz** — test etmeden önce porttaki önceki `dotnet run`
  öldürülmeli (bugün API `:5005`, panel `:5203`; ikisi **aynı anda** `dotnet run` edilmez,
  ref-assembly dosya kilidi yüzünden önce `build`, sonra `--no-build`).
- **K. Memory Bank gerçek duruma göre yeniden yazıldı** — "kusursuz / production-ready"
  abartıları kaldırıldı. Bu fazın asıl dersi zaten buydu: **doküman kendini denetlemiyorsa
  çürür.** Bugün `ArchitectureDocTests` + `CodeReviewChecklistDocTests` bunu mekanik olarak engelliyor.

## ✅ FAZ 7 - Admin Panel İyileştirmeleri (5 Temmuz 2026) — TAMAMLANDI

> Eklenen alanların tek tek listesi kaldırıldı (bugünkü hâli `ARCHITECTURE.md` modül tablosunda
> ve panel view'larında zaten görünüyor). Kalıcı olan **kararlar**:

- **Duyuru:** planlanan yayın zamanı boş bırakılırsa **anında yayınlanır** — bu davranış
  formda açıkça yazılı olmalı (sessiz varsayılan, kullanıcıyı şaşırtan sınıf).
- **Vefat:** cami ve mahalle **opsiyonel** (her cenaze camiden kalkmıyor, zorunlu alan
  yöneticiyi uydurmaya zorluyordu). **Yaş alanı kaldırıldı** — kişisel veri, gösterim değeri yok.
- **Mekan:** açılış/kapanış saati serbest metinden saat seçiciye çevrildi (serbest metin
  saat = istemcide ayrıştırılamayan veri; aynı ders 11.17'de ulaşım saatlerinde tekrarlandı).
- **Etkinlik / Kampanya / Mekan / Duyuru:** görsel ve konum alanları bu fazda geldi.

**Doğrulama:** build 0 hata; sayfalar canlıda 200/302 (`Live_Test_Report.md`).

## ✅ FAZ 8 - Admin API (JSON Endpoints) + Response Zarfı Kontratı (7 Temmuz 2026) — TAMAMLANDI

> **DURUM (7 Temmuz 2026):** Gemini analizi ile tespit edilen "Masterclass Faz 4 (Admin API) hiç yazılmamış" eksiği kapatıldı. Ayrıca analiz sırasında Gemini'nin gözünden kaçan kritik bir kontrat ihlali bulundu ve düzeltildi. Solution 0 hata derleniyor; tüm endpoint'ler canlı smoke test ile doğrulandı (detay: `Live_Test_Report.md`).

### A. Response Zarfı + Exception Middleware (kritik kontrat düzeltmesi) — [x]
Masterclass'ın "Flutter buna göre parse ediyor, **birebir koru**" dediği `{success, data, meta}` zarfını uygulayan HİÇBİR mekanizma API'de yoktu — controller'lar veriyi çıplak `Ok(data)` ile dönüyordu:
- YENİ `Api/Filters/ApiResponseWrapperFilter.cs` (IAsyncResultFilter, global): tüm başarılı yanıtları `{success:true, data, meta:{timestamp,path}}` zarfına sarar. Handler zaten `ApiResponse<T>` dönüyorsa (Announcements, PowerOutages, Users gibi eski handler'lar) çift sarmayı atlar.
- YENİ `Api/Middleware/ExceptionMiddleware.cs`: `NotFoundException→404`, `ForbiddenException→403`, `ConflictException→409`, `UnauthorizedException→401`, `ValidationException→400`, `AppException→400 (kendi Code'u ile)`, diğerleri→500 INTERNAL_ERROR. Yanıt formatı: `{success:false, error:{code,message}, meta}`.
- YENİ exception sınıfları: `Application/Common/Exceptions/` altına `ForbiddenException`, `ConflictException`, `UnauthorizedException` eklendi (mevcut AppException/NotFoundException desenine uygun).
- `Api/Program.cs`: filter `AddControllers(o => o.Filters.Add<...>)` ile, middleware `app.UseMiddleware<ExceptionMiddleware>()` ile pipeline başına bağlandı.

### B. Admin API — `Api/Controllers/Admin/` altında 13 yeni controller — [x]
Hepsi `v1/admin/*` route'unda, `AdminApiControllerBase`'den kalıtım alır (`[Authorize(Policy="AdminPanel")]` + JWT'deki snake_case `user_id` claim'ini okuyan `CurrentAdminId` helper'ı). Mevcut Application CQRS command/query'lerine bağlanırlar (MVC panelin kullandıklarının aynısı):
- `DashboardAdminController` — GET `/v1/admin/dashboard` (KPI kartları) + GET `/dashboard/activities`. Bunun için Application'a YENİ `Features/Dashboard/Queries/GetDashboardStatsQuery` (+Handler) ve `GetRecentActivitiesQuery` yazıldı (Web DashboardController'daki mantığın Application katmanına taşınmış hali; Web tarafı henüz buna geçirilmedi, hâlâ kendi AppDbContext sorgularını kullanıyor).
- `AdsAdminController` — list/getById/create/update/approve/reject/delete.
- `AnnouncementsAdminController` — list (OnlyPublished=false), types GET/POST, getById/create/update/delete.
- `DeathsAdminController` — list/getById/create (AutoApprove:true, panel davranışıyla aynı)/update/approve/delete.
- `EventsAdminController` — list/**calendar**/getById/create (AutoApprove)/update/approve/reject/delete.
- `CampaignsAdminController` — list/getById/create (AutoApprove)/update/approve/reject (reason body'li)/delete.
- `ComplaintsAdminController` — list (status filtreli) + POST `{id}/status` (ResolveComplaintCommand).
- `UsersAdminController` — list/create/update/ban/unban. Bunun için Application'a YENİ `Features/Users/Queries/GetUsers/GetUsersQuery` (arama+ban filtresi+sayfalama) ve `Features/Users/Commands/SetUserBan/SetUserBanCommand` yazıldı (Web'deki inline ban mantığının command'e taşınmış hali — BanReason/BannedAt/BannedBy da set ediyor; Web controller hâlâ kendi inline kodunu kullanıyor).
- `GuideAdminController` — categories GET/getById/POST/PUT + items GET/getById/POST/PUT/DELETE.
- `PharmaciesAdminController`, `PlacesAdminController`, `PowerOutagesAdminController`, `TaxisAdminController` (verify dahil), `TransportAdminController` (intercity GET/POST + intracity GET/POST/PUT) — tam CRUD.

### C. Güvenlik Düzeltmesi — [x]
Public `Api/Controllers/PowerOutagesController`'da POST/PUT/DELETE tamamen kimlik doğrulamasız açıktaydı (herkes kesinti kaydı oluşturup silebilirdi). Bu üç action'a `[Authorize(Policy="AdminPanel")]` eklendi; GET'ler public kaldı (mobil okuyor).

### D. Canlı Smoke Test — [x] (7 Temmuz, detay Live_Test_Report.md)
- 15 admin liste endpoint'i → hepsi 200 + doğru zarf.
- Mutasyonlar: vefat approve (dashboard pending sayısı 1→0 düştü), kullanıcı ban/unban → çalışıyor.
- Yetkisiz istek → 401; olmayan kayıt → 404 + `{success:false, error:{code:"NOT_FOUND",...}}` zarfı.
- Public endpoint'ler zarf değişikliğinden kırılmadı (ads: filter sardı; announcements: handler zarfı korundu, çift sarma yok).

### E. Bu Turda Tespit Edilip HENÜZ Düzeltilmeyen Eksikler — [~] (1-3 ve 5 tamam, 8 Tem)
1. ~~**`AuthController` route'u `api/auth`**~~ — **[x] TAMAMLANDI (8 Tem):** `AuthController` route'u `v1/auth` yapıldı; eski `/api/Auth/*` 404 dönüyor (canlı doğrulandı, bkz. Live_Test_Report.md). Memory_Bank'teki `/api/Auth` referansları güncellendi (test_panel.sh ve .http dosyasında auth referansı yoktu).
2. ~~**Yanlış OTP → 500 INTERNAL_ERROR dönüyor**~~ — **[x] TAMAMLANDI (8 Tem):** VerifyOtp handler'daki 3 `UnauthorizedAccessException` (middleware tanımıyordu → 500) tipli hatalarla değiştirildi: geçersiz OTP → `AppException(..., "INVALID_OTP")` = 400; banlı/pasif hesap → `ForbiddenException` = 403. Canlı doğrulandı: yanlış OTP → 400 `INVALID_OTP`, doğru OTP → 200 + JWT.
3. ~~**`Otp:DevMode` (sabit 123456) aktif değil**~~ — **[x] TAMAMLANDI (8 Tem):** `RedisOtpService` config'deki `Otp:DevMode`'u hiç okumuyordu (appsettings'te `true` olmasına rağmen rastgele OTP üretiyordu). Masterclass 12.2'ye uygun şekilde DevMode'da sabit `123456` üretiyor; canlı doğrulandı (login → otp:"123456", verify 123456 → JWT). OTP hâlâ login yanıtında dönüyor — bu SMS entegrasyonuyla birlikte kaldırılacak (prod öncesi iş, Active_Context'te kayıtlı).
4. ~~**Staff CRUD, admin profile, change-password, modül bazlı `[RequirePermission]`**~~ — **[x] TAMAMLANDI (8 Tem):** Masterclass Faz 4 kapsamında Staff CRUD işlemleri, şifre sıfırlama, rol ve izin yönetimi tamamlandı. `IPermissionService` ve `[RequirePermission]` eklendi, `StaffAdminController` yazıldı ve canlı testte doğrulandı.
5. ~~**Guide kategorisi için Delete command yok**~~ — **[x] TAMAMLANDI (8 Tem):** `DeleteGuideCategoryCommand` (+Handler) yazıldı, `DELETE /v1/admin/guide/categories/{id}` endpoint'i eklendi. Handler, Web'deki inline `CategoryDelete` mantığını Application'a taşır ve genişletir: kategoride item varsa VEYA alt kategorisi varsa `ConflictException` → 409 (FK'lar `ON DELETE RESTRICT` — ön kontrol olmasa DB hatası 500 verirdi; Web inline kodu alt kategori kontrolü YAPMIYOR). Web controller SetUserBanCommand emsalindeki gibi hâlâ kendi inline kodunu kullanıyor. Canlı doğrulandı: boş kategori → 200, dolu → 409 CONFLICT, alt kategorili → 409, olmayan id → data:false, token'sız → 401.
6. ~~**Test projesi ve CI/CD hâlâ yok** (Masterclass Faz 7).~~ — **[x] TAMAMLANDI (8 Tem):** `KadirliApp.Tests` xUnit projesi oluşturuldu. `Testcontainers` ile gerçek veritabanı (PostgreSQL 15) ve Redis ortamında entegrasyon testleri (Auth akışı) ile unit testler (Moq ile) eklendi. `.github/workflows/dotnet.yml` ile GitHub Actions CI/CD akışı kuruldu. Tüm testler başarıyla geçiyor.

### ✅ F. KONTROL EDİLDİ (7 Temmuz 2026) — Masterclass SONRASI Panele Eklenen Yeni Özelliklerin Admin API Kapsaması
> **Arka plan:** Proje `DOTNET_MASTERCLASS.md` ile oluşturulmaya başlandıktan SONRA kullanıcı eksiklikler tespit edip admin panele YENİ özellikler ekletti (Faz 7 maddeleri). Bu özellikler masterclass'ta YOKTUR. Admin API aynı Application command/DTO'larını bind ettiği için kapsama bekleniyordu ama doğrulanmamıştı. **Doğrulama yapıldı** — her özellik için `v1/admin/*` endpoint'ine gerçek istek atıldı ve DB'deki satır kontrol edildi (test kayıtları sonra temizlendi). Detaylı çıktı: `Live_Test_Report.md`.

**Doğrulanan Faz 7 özellikleri (hepsi Admin API'den ÇALIŞIYOR):**
- Duyuru: `targetType=neighborhood` + `targetNeighborhoodIds` (jsonb'ye dizi yazıldı), `locationName/latitude/longitude` DB'de ✅; `POST /v1/admin/announcements/types` (modal karşılığı) ✅. `imageFileId` alanı DTO'da mevcut (dosya id'siyle bağlanabilir).
- Etkinlik: `latitude/longitude` ✅, `GET /v1/admin/events/calendar?year&month` ✅, `coverImageId` gerçek dosya id'siyle bağlandı ve DB'ye yazıldı ✅.
- Kampanya: `coverImageId` command'de mevcut; create → `status=approved` + `approved_at` ✅.
- Vefat: `mosqueId`/`neighborhoodId` NULL kabul ✅, `condolenceAddress/Latitude/Longitude` DB'de ✅, Yaş alanı DTO'da yok (kaldırılmış) ✅.
- Mekan: `openingHours` ✅, `amenities` (jsonb — panelin gönderdiği `{"WC":true,"Wi-Fi":true,...}` JSON-string formatıyla) ✅, `coverImageId` alanı mevcut.
- Dosya yükleme akışı uçtan uca: `POST /v1/files/upload` → dönen id ile kapak görselli etkinlik → DB'de `cover_image_id` + `GET /uploads/...` 200 ✅.
- Şehir rehberi: UX düzeltmeleri UI katmanında; kategori/item command'leri Admin API'de zaten bağlı.

**Kontrol sırasında BULUNAN ve DÜZELTİLEN 5 bug:**
1. `EventsAdminController` (API) `CreatedBy` set etmiyordu → events tablosuna `Guid.Empty` yazılırdı. Düzeltildi: `command.CreatedBy = CurrentAdminId`.
2. `CampaignsAdminController` (API) `ApprovedBy` set etmiyordu → AutoApprove'lu kampanyada `approved_by` NULL kalırdı. Düzeltildi.
3. `AdsAdminController` (API) `UserId` set etmiyordu (Web panel claim'den geçiyor). Düzeltildi.
4. **`ApiResponse<T>` serileştirme bug'ı (mevcut kod, Faz 8 öncesinden):** `Data` üzerindeki `[JsonIgnore(WhenWritingNull)]` değer tiplerinde (`ApiResponse<Guid>`) `InvalidOperationException` fırlatıyordu → **public `POST /v1/announcements` dahil ApiResponse<Guid>/bool dönen TÜM endpoint'ler baştan beri 500 veriyordu** (panel etkilenmiyordu çünkü JSON serileştirmiyor). `WhenWritingDefault` yapıldı.
5. **`UploadFileCommandHandler` bug'ı (mevcut kod):** `new Uri(fileUrl).LocalPath` göreli URL'de (`/uploads/...`) fırlatıyordu → API'den `POST /v1/files/upload` baştan beri 500 veriyordu (Web çalışıyordu çünkü `FileStorage:BaseUrl` dolu). `Path.GetFileName(fileUrl)` yapıldı.

**Bilinen davranış notu (bug değil ama dikkat):** yanıt serileştirmesinde hata olsa bile kayıt DB'ye yazılıyor (SaveChanges yanıttan önce) — 4. bug'ın canlı testinde 500 dönen ilk duyuru isteği DB'ye mükerrer satır bırakmıştı (temizlendi).

---

## 🚀 FAZ 9 - Geliştirilebilir ve Sürdürülebilir Yapı (Gelecek Planı & Analiz)
> Sistem, güncel halinde (8 Temmuz 2026) tüm Masterclass gereksinimlerini ve sonradan eklenen ekstra özellikleri (Faz 7) karşılıyor, testleri ve CI/CD süreçleri tamamlanmış durumda. Ancak daha modüler, ölçeklenebilir ve kurumsal düzeyde bir "Production" ortamı için aşağıdaki geliştirmeler analiz edilip planlanmıştır.

### 1. Panel & UI Eksikleri (Yakın Vadeli İşler)
- [x] **Dosya Yükleme Akışının UI Entegrasyonu — TAMAMLANDI (13 Tem 2026):** Durum tespiti: Bu madde yazıldığında kısmen bayattı — Duyuru/Etkinlik/Kampanya/Mekan (Create+Edit) ve Vefat (Create) formlarına dosya seçici zaten 5 Tem'de eklenmişti. Bu oturumda kalan GERÇEK eksikler kapatıldı:
  - **Vefat Edit formu:** Fotoğraf seçici hiç yoktu (controller `IFormFile? Photo` destekliyordu ama view'da input yoktu — fotoğraf sadece Create'te eklenebiliyordu, sonradan değiştirilemiyordu). Create'teki picker + mevcut fotoğraf önizlemesi + "Fotoğrafı kaldır" checkbox'ı (JS hidden `PhotoFileId`'yi temizler; Application katmanına dokunmadan çalışır çünkü UpdateDeathNoticeCommandHandler `PhotoFileId`'yi koşulsuz yazar) eklendi. Canlı doğrulandı: yükle → `photo_file_id` DB'de + `/uploads/...` 200; kaldır → NULL.
  - **İlanlar (Ads) görsel desteği (EK GELİŞTİRME):** `AdImage` entity'si/tablosu baştan beri vardı ama HİÇBİR katman kullanmıyordu — panelde ilan görseli eklenemiyordu. Eklendi: `CreateAdCommand.ImageFileIds` (ilk görsel `IsCover`), `UpdateAdCommand.NewImageFileIds` + `RemoveImageIds` (silinen kapaksa en düşük sıradaki görsel otomatik kapak yapılır), `GetAdImagesQuery` (+`AdImageDto`), Create/Edit view'larına çoklu görsel picker (`multiple` input, önizleme, kapak rozeti, kaldır checkbox'ları). Admin API'nin `AdsAdminController`'ı aynı command'leri bind ettiği için JSON'la `imageFileIds` gönderilerek API'den de çalışır. Canlı doğrulandı: 2 görselli ilan → `ad_images`'ta 2 satır (ilki `is_cover=t`); kapak silinince kalan görsel kapak oldu.
  - **BULUNAN ve DÜZELTİLEN BUG — `GetAdsQueryHandler.ImageUrls`:** Mevcut kod `Images.Select(i => i.FileId.ToString())` dönüyordu — mobil istemciye URL yerine GUID string'leri gidiyordu (görsel render edilemezdi). `files` tablosuna correlated subquery ile gerçek `cdn_url` dönecek şekilde düzeltildi (kapak ilk sırada). Canlı doğrulandı: `GET /v1/ads` → `imageUrls: ["/uploads/..."]`.
  - **Mobil eksik — Vefat fotoğraf URL'i:** `DeathNoticeResponseDto`'da yalnızca `PhotoFileId` vardı; mobil fotoğrafı gösteremezdi. `PhotoUrl` eklendi (DeathNotice→File navigation'ı olmadığından correlated subquery ile; hem liste hem tekil query'de). Canlı doğrulandı: `GET /v1/deaths` → `photoUrl` dolu geliyor.
  - Not: Görsel URL join'lerinde navigation eklemek yerine correlated subquery tercih edildi — `ad_images.file_id` ve `death_notices.photo_file_id` için DB'de FK kısıtı yok; navigation eklemek migration gerektirirdi.
- [x] **Mobil İstemci API Gözden Geçirmesi — TAMAMLANDI (13 Tem 2026):** Durum tespiti: `search` + `categoryId` (Events) ve `search` + `businessId` (Campaigns) zaten vardı. Eklenenler:
  - **Events `GET /v1/events`:** `startDate` / `endDate` (etkinlik tarihi aralığı, gün bazlı — mobilin "yaklaşan etkinlikler" için; timestamptz kuralına uygun `SpecifyKind(Utc)`), `isFree` (true/false) filtreleri; `search` artık başlığa ek `venueName`'i de tarıyor. Canlı doğrulandı: `?isFree=true`, `?startDate=2026-07-20`, `?startDate=..&endDate=..`, `?search=stadyum` hepsi doğru alt kümeyi döndü.
  - **Campaigns `GET /v1/campaigns`:** `categoryId` (işletme kategorisi — `Business.CategoryId` üzerinden) filtresi; `search` artık kampanya başlığına ek işletme adını da tarıyor. Canlı doğrulandı: Kafe kategorisi → 1, Elektronik → 0; `?search=savrun` işletme adından buldu.
  - Bu filtreler Admin API'de de aynen kullanılabilir (aynı Query DTO'ları bind ediliyor).
  - **Doğrulama (13 Tem):** `dotnet build` 0 hata; xUnit 4/4 geçti; tüm canlı testler panel login (`admin/Admin123!`) + curl ile yapıldı, test kayıtları (CLAUDE-TEST ilanı, test görselleri, vefat test fotoğrafı) DB'den ve `uploads/`'tan temizlendi.

### 2. Güvenlik & İletişim (Production Hazırlığı)
- [x] **SMS ve Mail Entegrasyonu — BAĞLAMAYA HAZIR ALTYAPI TAMAMLANDI (16 Tem 2026):** Kullanıcı henüz SMS/mail sağlayıcısıyla anlaşmadığı için gerçek sağlayıcı BAĞLANMADI; soyutlama + akış değişiklikleri yapıldı, sağlayıcı satın alınınca tek sınıf + config değişikliğiyle devreye girer:
  - **Soyutlama:** `Application/Common/Interfaces/ISmsService.cs` + `IEmailService.cs`; `Infrastructure/Notifications/DevLogSmsService.cs` + `DevLogEmailService.cs` (göndermek yerine Warning loglar). Aktif implementasyon `Sms:Provider` / `Email:Provider` config anahtarıyla seçilir (varsayılan `Dev`; bilinmeyen değer → açılışta açıklayıcı hata). appsettings'e `Sms`/`Email` bölümleri eklendi (Netgsm/Smtp credential placeholder'ları hazır).
  - **🔌 SAĞLAYICI BAĞLAMA TALİMATI:** (1) `Infrastructure/Notifications/` altına `ISmsService` implementasyonu yaz (ör. `NetgsmSmsService`, HttpClient ile), (2) `Infrastructure/DependencyInjection.cs`'teki switch'e `case "netgsm"` ekle, (3) appsettings'te `Sms:Provider=Netgsm` + credential'ları doldur, (4) `Otp:DevMode=false` yap, (5) health check'lere SMS kontrolü ekle (Faz 9.3 notu — dış servis olduğundan `Degraded` seviyesi, readiness'ı bloklamasın). Mesaj içeriği (OTP) gerçek sağlayıcıda ASLA loglanmamalı (Dev adaptörü bilinçli logluyor).
  - **OTP yanıttan kaldırıldı:** `IOtpService.GenerateAndStoreAsync` → `RequestAsync` (`OtpRequestResult{ExpiresInSeconds, RetryAfterSeconds, DevOtp}` döner). `POST /v1/auth/login` yanıtı artık `{message, expiresIn, retryAfter}`; `otp` alanı YALNIZ `Otp:DevMode=true` iken eklenir (sağlayıcısız geliştirme sürsün diye — Faz 10 mobil geliştirme DevMode ile yapılacak). DevMode=false iken kod `ISmsService.SendAsync` ile gönderilir.
  - **Masterclass §12.2 sertleştirmesi tamamlandı (eksikti):** `Otp:MaxAttempts` config'i baştan beri VARDI ama hiç kullanılmıyordu — artık OTP başına 3 hatalı denemeden sonra telefon 5 dk bloklanır (`otp_block:{phone}`), bloklu telefona login/verify 429; karşılaştırma timing-safe (`CryptographicOperations.FixedTimeEquals`). **Düzeltilen bug:** saatlik OTP istek limiti (10/saat) aşımı `InvalidOperationException` fırlatıyordu → istemciye 500 INTERNAL_ERROR gidiyordu; yeni `RateLimitedException` → 429 `RATE_LIMITED` (ExceptionMiddleware'e mapping eklendi).
- [x] **Rate Limiting (Hız Sınırlandırması) — TAMAMLANDI (16 Tem 2026):** .NET 8 yerleşik `AddRateLimiter`/`UseRateLimiter`, limitler appsettings `RateLimiting` bölümünden (IP bazlı fixed-window, `QueueLimit=0`):
  - **Api:** `auth` policy (5 istek/dk/IP) `AuthController`'da (`login` + `verify-otp`); ayrıca global taban limit (300 istek/dk/IP, tüm uçlar — statik `/uploads` middleware sırası gereği HARİÇ, mobil görsel yüklemeleri limite takılmaz). Ret yanıtı Flutter zarfıyla: 429 + `{success:false, error:{code:"RATE_LIMITED"}, meta:{traceId,...}}` + `Retry-After` header.
  - **Web:** `panel-login` policy (5 istek/dk/IP) yalnız `AccountController` POST `Login`'de — panel brute-force koruması (GET login sayfası etkilenmez). Ret: 429 + düz metin Türkçe mesaj.
  - ⚠️ Reverse proxy arkasında deploy edilirse `RemoteIpAddress`'in gerçek istemci IP'si olması için ForwardedHeaders middleware'i eklenmeli (Program.cs'te not düşüldü). Ayrıca Redis tabanlı telefon-başına limitler (10 istek/saat + deneme bloğu) IP limitinin tamamlayıcısı olarak `RedisOtpService`'te.
  - **Testler:** `CustomWebApplicationFactory`'ye override edilebilir `ExtraConfiguration` eklendi (temel factory auth limitini gevşetir — testler art arda istek atar); yeni `RateLimitingTests` (`ProductionModeFactory`: DevMode=false + limit 3): yanıtta `otp` alanı YOK + 4. istek 429 `RATE_LIMITED`; `LoginCommandHandlerTests` (trim + boş telefon) eklendi; mevcut auth entegrasyon testine DevMode `otp==123456` asserti eklendi. **xUnit 7/7 yeşil.**
  - **Canlı doğrulama (16 Tem, curl):** DevMode=true → yanıtta `otp:"123456"`; 6 hızlı login → ilk 5 200, sonrası 429+`Retry-After:60`+zarf; `Otp__DevMode=false` ile → yanıtta `otp` YOK, dev SMS log satırında gerçek kod (`623626`) ve o kodla verify → JWT alındı; 3 yanlış OTP → 400 INVALID_OTP, 4.sü → 429 blok, bloklu telefonla login → 429; panel: 5 hatalı giriş sonrası 6.sı → 429 "Çok fazla deneme...", 61 sn sonra doğru şifre → 302. Test kullanıcıları (2 adet) + Redis `otp*` anahtarları temizlendi, test process'leri kapatıldı.

### 3. Gözlemlenebilirlik (Observability) & İzleme
- [x] **Yapılandırılmış Loglama (Structured Logging) — TAMAMLANDI (15 Tem 2026):** **Serilog** hem Api hem Web'e entegre edildi (`Serilog.AspNetCore` 10.0.0 + `Serilog.Sinks.Seq` 9.1.0). Konfigürasyon tamamen appsettings `"Serilog"` bölümünden okunur (`ReadFrom.Configuration`) — kod değişmeden sink/level değiştirilebilir:
  - **3 sink:** Console (dev okunabilirliği), **File** (`logs/api-.ndjson` / `logs/web-.ndjson` — CompactJsonFormatter ile satır başına JSON, günlük rolling, 14 gün saklama), **Seq** (`http://localhost:5341` — merkezi log sunucusu, aşağıdaki ekstra maddeye bakınız).
  - `UseSerilogRequestLogging()` her iki uygulamada: her istek tek satır yapılandırılmış olay (method, path, status, süre). Web'de statik dosyalardan SONRA konumlandırıldı (css/js gürültüsü loglanmaz); Api'de ExceptionMiddleware'in hemen ardında (handle edilen hatalar nihai status koduyla loglanır).
  - **Gürültü kısıldı:** `Microsoft.EntityFrameworkCore.Database.Command` ve `Hangfire` → Warning override (eskiden her SQL komutu Info seviyesinde konsola akıyordu).
  - **TraceId üzerinden sorun takibi:** `ExceptionMiddleware` artık hata zarfının `meta`'sına `traceId` (W3C `Activity.Current.Id`) ekliyor ve 500'leri `TraceId` property'siyle logluyor. Canlı doğrulandı: hatalı OTP isteğinin yanıtındaki `meta.traceId` (`00-0ff2b843...`) ile Seq'e düşen olayın `TraceId`'si (`0ff2b843...`) birebir eşleşti — istemcinin bildirdiği traceId ile merkezi logda ilgili istek bulunabiliyor.
- [x] **Health Checks (Sağlık Kontrolleri) — TAMAMLANDI (15 Tem 2026):** `AspNetCore.HealthChecks.NpgSql/Redis/Hangfire` (8.0.x) paketleri Infrastructure'a eklendi. Kayıt `AddInfrastructure` içinde (PostgreSQL + Redis + Hangfire kontrolleri, hepsi `"ready"` etiketli; Redis kontrolü uygulamanın MEVCUT singleton `IConnectionMultiplexer`'ını kullanır — ikinci bağlantı açılmaz). Endpoint mapping tek yerde: `Infrastructure/Health/HealthEndpoints.MapInfrastructureHealthEndpoints()` — Api ve Web'in ikisi de tek satırla çağırır. 3 uç nokta:
  - `GET /health/live` — liveness: yalnızca process ayakta mı, bağımlılık kontrolü YAPMAZ (K8s livenessProbe; bağımlılık çöktü diye pod restart edilmesin).
  - `GET /health/ready` — readiness: 3 kritik bağımlılık (K8s readinessProbe / Docker HEALTHCHECK).
  - `GET /health` — detaylı JSON rapor: her kontrolün adı, durumu, süresi (ms), hata mesajı.
  - Canlı doğrulandı (Api + Web ikisinde de): normal durumda üçü de 200/Healthy; **Redis konteyneri durdurulunca** `/health/ready` → 503 `{redis: Unhealthy, postgres/hangfire: Healthy}` dönerken `/health/live` 200 kalmaya devam etti; Redis geri açılınca `/health/ready` tekrar 200 oldu.
  - Not: SMS servisi kontrolü, GERÇEK SMS sağlayıcısı bağlandığında eklenecek (Faz 9.2 altyapısı 16 Tem'de tamamlandı ama aktif sağlayıcı Dev/log adaptörü — health check'i anlamsız olur).
- [x] **EKSTRA GELİŞTİRME — Seq merkezi log sunucusu + yanıt zarfına traceId (15 Tem 2026):**
  - **docker-compose'a `seq` servisi eklendi** (`datalust/seq`, UI: `http://localhost:8081`, ingestion: `:5341`, kalıcı `seqdata` volume'ü, yerel geliştirme için `SEQ_FIRSTRUN_NOAUTHENTICATION=true` — production'da `SEQ_FIRSTRUN_ADMINPASSWORD` + API key kullanılmalı). Api ve Web logları `Application` property'siyle (`KadirliApp.Api`/`KadirliApp.Web`) ayrışır; Seq UI'da TraceId ile arama yapılabilir. Seq kapalıyken sink sessizce buffer'lar, uygulamayı etkilemez (doğrulandı: Seq restart sonrası bekleyen olaylar teslim edildi).
  - **`ApiResponseWrapperFilter` `meta`'sına `traceId` eklendi** (geriye uyumlu ek alan — Flutter zarfı bozulmaz): başarılı yanıtlarda da destek talebi → log eşleştirmesi yapılabilir. Not: handler'ın kendisi `ApiResponse<T>` dönüyorsa filter sarmadığından `meta` handler'ın verdiği gibi kalır (mevcut davranış korunmuştur).
  - **Doğrulama (15 Tem):** `dotnet build` 0 hata; xUnit 4/4 geçti (Testcontainers ile — Serilog ve health check kayıtları test host'unda da sorunsuz); canlı testler curl ile yapıldı, test process'leri kapatıldı. Eski `"Logging"` bölümü appsettings'te duruyor ama `UseSerilog` provider'ları değiştirdiğinden artık etkisiz (kafa karışıklığını önlemek için not düşüldü).

### 4. Performans & Mimari Ölçeklenebilirlik
- [x] **Distributed Caching (Dağıtık Ön Bellekleme) — TAMAMLANDI (15 Tem 2026):** Plandaki gibi Redis üzerinde, invalidation MediatR Behavior'larıyla `Application` katmanında:
  - **Altyapı:** `Application/Common/Caching/CacheContracts.cs` (`ICacheableQuery`: CacheKey+CacheGroup+CacheDuration; `ICacheInvalidator`: CacheGroupsToInvalidate; `CacheGroups` sabitleri), `Application/Common/Interfaces/ICacheService.cs`, `Infrastructure/Caching/RedisCacheService.cs` (mevcut singleton `IConnectionMultiplexer`'ı kullanır; grup üyeliği Redis SET'inde — `cache-group:{grup}` → anahtar listesi; invalidation üyeleri + set'i siler).
  - **Behavior'lar:** `CachingBehavior` (hit'te handler hiç çalışmaz; Redis erişilemezse **fail-open** — istek cache'siz devam eder, warning loglanır) ve `CacheInvalidationBehavior` (command başarıyla bitince grupları temizler; invalidation hatası isteği düşürmez, TTL bayat veriyi en geç süre sonunda temizler). `AddApplication` içinde `AddOpenBehavior` ile kayıtlı — **hem API hem Web paneli aynı pipeline'ı kullandığından cache VE invalidation iki uygulamada da ortak** (canlıda kanıtlandı: panelden kategori silme, API'nin doldurduğu cache'i temizledi).
  - **Cache'lenen query'ler:** `GetGuideCategoriesQuery` + `GetGuideItemsQuery` (grup `guide`, 15 dk), `GetPharmaciesQuery` (grup `pharmacies`, 15 dk), `GetDashboardStatsQuery` + `GetRecentActivitiesQuery` (grup `dashboard`, **60 sn** — istatistikler pek çok modülün yazmasıyla değiştiğinden invalidation yerine kısa TTL). Anahtarlar tüm sorgu parametrelerini içerir (sayfa/filtre kombinasyonları ayrı anahtar).
  - **Invalidate eden command'ler:** Guide kategori+item Create/Update/Delete (6 adet) → `guide`; Pharmacy Create/Update/Delete (3 adet) → `pharmacies`.
  - **Canlı doğrulama (15 Tem):** rehber kategorileri 69ms→10ms, dashboard 37ms→3ms (cache hit); Redis'te `cache:*` + `cache-group:*` anahtarları gözlendi; eczane create → `cache:pharmacies:*` silindi → taze listede yeni kayıt; delete de aynı şekilde; `cache:dashboard:stats` TTL=60 doğrulandı. Test kayıtları temizlendi.
  - **Bilinen sınır:** Web panelinin hâlâ inline DbContext/UnitOfWork kullandığı eski yazma yolları (ör. bazı controller'lardaki SetUserBan benzeri inline kod) invalidation tetiklemez — bu yollarda 15 dk'lık TTL üst sınırdır. Panel yazma yolları command'lere taşındıkça bu sınır kendiliğinden kalkar (CategoryDelete bu oturumda taşındı, aşağıya bakınız).
- [x] **Web Panel ve API İletişimini Ayrıştırma (BFF) — DEĞERLENDİRİLDİ, BİLİNÇLİ OLARAK ERTELENDİ (15 Tem 2026):** Karar: **şimdilik mevcut "shared Application katmanı" mimarisi korunacak.** Gerekçeler: (1) Panel iç ağda tek instance çalışıyor ve trafiği API'nin küçük bir kesri — bağımsız ölçekleme ihtiyacı yok; (2) BFF'e geçiş ~15 controller'ın HttpClient/Refit'e port edilmesi + cookie→JWT servis kimliği + hata/retry politikaları demek, kazancı bugün sıfır; (3) bu oturumda eklenen distributed cache zaten iki uygulamayı Redis üzerinden senkronize ediyor (BFF'in çözeceği tutarlılık probleminin cache ayağı çözüldü). **Geçişi tetikleyecek koşullar:** panel ve API'nin ayrı sunuculara/ağlara ayrılması, panelin DB'ye doğrudan erişiminin güvenlik gereği kesilmesi, veya 3. bir istemcinin (mobil admin vb.) Admin API'yi tüketmeye başlaması. **Geçiş yolu hazır:** Admin API zaten Application ile birebir aynı command/query'leri bind ediyor (Faz 4+F kontrolleriyle kanıtlı) — Web controller'ları `ISender` yerine Refit arayüzüne geçirmek yeterli; ara adım olarak yeni panel özellikleri inline kod yerine HER ZAMAN command/query üzerinden yazılmalı (bu kural cache invalidation için de gerekli).
- [x] **Background Jobs Resiliency (Dirençlilik) — TAMAMLANDI (15 Tem 2026):** 3 Hangfire job'ı (`ExpireAdsJob`, `ArchiveDeathsJob`, `PublishScheduledAnnouncementsJob`) yeniden yazıldı:
  - **İdempotency:** foreach+SaveChanges yerine tek set-tabanlı `ExecuteUpdateAsync` (EF8) — tek atomik UPDATE; tekrar çalışırsa koşula uyan satır kalmadığından yan etki üretmez (ve N+1 yerine tek SQL, daha hızlı).
  - **Retry:** `[AutomaticRetry(Attempts=3, DelaysInSeconds={60,300,900}, OnAttemptsExceeded=Fail)]` — artan gecikmeli 3 deneme; hepsi tükenirse iş Hangfire'ın **Failed** kümesinde kalır (dead-letter karşılığı; `/hangfire` dashboard'unda görünür, sistemi tıkamaz).
  - **Overlap koruması:** `[DisableConcurrentExecution]` (dakikalık publish job'ında 55 sn, diğerlerinde 300 sn) — önceki koşu uzarsa üst üste binme olmaz.
  - **Gözlemlenebilirlik bağı:** etkilenen satır sayısı yapılandırılmış loglanıyor (Serilog → Seq).
  - **Canlı doğrulama (15 Tem):** zamanlanmış CLAUDE-TEST duyurusu oluşturuldu, `scheduled_for` geçmişe çekildi → 40 sn içinde dakikalık job satırı `active` yaptı + `sent_at` doldu + Seq/dosya logunda `Count:1` olayı düştü; hangfire.job tablosunda 11/11 Succeeded, Failed yok. Test kaydı silindi.
- [x] **EKSTRA GELİŞTİRMELER (15 Tem 2026):**
  - **Web DashboardController Application query'lerine geçirildi:** eskiden her panel açılışında 11 inline COUNT/SELECT sorgusu atıyordu (Application'daki `GetDashboardStatsQuery`/`GetRecentActivitiesQuery` ile mükerrer mantık). Artık `ISender` üzerinden aynı handler'ları çağırıyor → kod tekilleşti + 60 sn Redis cache'inden faydalanıyor (`AppDbContext` yalnızca `Seed` action'ında kaldı). Panel dashboard'u canlı doğrulandı (login → Index 200, istatistik + son aktiviteler render).
  - **Web GuideAdminController.CategoryDelete inline kodu Application command'ine geçirildi:** inline sürüm alt kategori kontrolü YAPMIYORDU (Faz 9 öncesi bilinen drift) ve cache invalidation tetiklemiyordu. Artık `DeleteGuideCategoryCommand` kullanıyor (item VEYA alt kategori varsa `ConflictException` → TempData hatası). Canlı doğrulandı: panelden silme → 302, DB'de satır yok, `cache:guide:*` temizlendi.
  - **Hangfire deprecated uyarısı giderildi:** `UsePostgreSqlStorage(conn)` → `UsePostgreSqlStorage(o => o.UseNpgsqlConnection(conn))` (solution'daki tek proje-kodu uyarısıydı; build artık yalnızca Tests'teki Testcontainers uyarılarını veriyor).
  - **Doğrulama (15 Tem):** `dotnet build` 0 hata; xUnit 4/4 (Testcontainers); tüm canlı testler curl + redis-cli + psql ile yapıldı; CLAUDE-TEST kayıtları (eczane, rehber kategorisi, zamanlanmış duyuru) temizlendi; test process'leri kapatıldı.

---

## 📱 FAZ 10 — MOBİL ÖNCESİ HAZIRLIK (Public API'yi Flutter'a Hazırlama)
> **Genel kontrol tarihi: 15 Temmuz 2026.** Mobil uygulama (Flutter) public `v1/*` API'yi tüketecek. Yapılan taramada API'nin admin/panel tarafı olgun, ama **mobil kullanıcı tarafı büyük ölçüde eksik** çıktı. Tespitler `DOTNET_MASTERCLASS.md` §13.2'deki "Tam endpoint haritası" (NestJS orijinalinden çıkarılan mobil kontrat) ile mevcut controller'lar karşılaştırılarak yapıldı.
>
> **Kural:** Her madde tek oturumda bitecek şekilde boyutlandırıldı. Her maddede yeni endpoint'ler MUTLAKA Application command/query üzerinden yazılmalı (Faz 9.4 kuralı — cache invalidation + BFF hazırlığı) ve Flutter zarfı `{success, data, meta}` korunmalı. Her madde canlı curl testi + xUnit yeşil + Progress.md güncellemesiyle kapanır.
>
> ⚠️ **REFERANS KURALI:** `DOTNET_MASTERCLASS.md` projenin BAŞLANGIÇ dokümanıdır — üzerine Faz 7/8/9'da çok geliştirme yapıldı ve kısmen güncelliğini yitirmiş olabilir. Masterclass'ı mobil kontratın (endpoint haritası, NestJS davranışları) İLK referansı olarak kullan ama ASLA tek kaynak sayma: her maddeye başlarken (1) mevcut kodu (ilgili controller/command/entity/DbContext konfigürasyonu) mutlaka oku, (2) `Memory_Bank/Progress.md` + `Active_Context.md` + `Live_Test_Report.md`'deki sonradan alınan kararları kontrol et, (3) çelişki varsa mevcut kod + Memory_Bank kazanır, masterclass'tan sapma varsa nedenini Progress'e not düş. Küçük dosya bile olsa okunması gerekiyorsa OKU — basite kaçma.
>
> **Not:** Faz 9.2 altyapısı 16 Tem 2026'da TAMAMLANDI (OTP yanıttan kaldırıldı — DevMode hariç, ISmsService/IEmailService soyutlaması + Rate Limiting). Mobil geliştirme `Otp:DevMode=true` ile sürebilir. Mobil YAYIN öncesi kalan tek iş: gerçek SMS sağlayıcısı satın alınıp bağlanması (talimat Faz 9.2'de) + `Otp:DevMode=false`.

### 10.1 — ✅ TAMAMLANDI (16 Tem 2026) — 🔴 KRİTİK GÜVENLİK: Public yazma uçlarının yetki taraması ve kapatılması
> **Tespit:** Public controller'ların HİÇBİRİNDE `[Authorize]` yok. Şu an token'sız herkes: duyuru oluşturabilir/silebilir (`POST/PUT/DELETE /v1/announcements` — 7 Tem'de canlı teste de yaradı ama üretimde felaket), eczane/rehber kategorisi/etkinlik/mekan/taksici/ulaşım hattı/vefat ilanı yazabilir, **100 MB'a kadar anonim dosya yükleyebilir** (`POST /v1/files/upload`), ve **herhangi bir kullanıcının profilini ID ile okuyup GÜNCELLEYEBİLİR** (`GET/PUT /v1/users/{id}/profile` — IDOR). Tek istisna: PowerOutages (8 Tem'de kapatılmıştı) ve Complaints (bilinçli anonim).
- Yapılacaklar:
  - Uç uç karar matrisi çıkar ve uygula: **admin-işi olanlar** (announcements/pharmacies/guide/events/places/taxis/transport POST-PUT-DELETE) → `[Authorize(Policy="AdminPanel")]` (admin API'de zaten karşılıkları var; public kopyaları silmek de meşru bir seçenek — karar verilip not düşülmeli). **Kullanıcı-işi olanlar** (deaths POST — vatandaş vefat ilanı gönderir, pending'e düşer) → `[Authorize]` + `CreatedBy` claim'den.
  - `FilesController.Upload` → `[Authorize]` + boyut limitini 10-20 MB'a indir + content-type doğrulaması (yalnız görsel: jpeg/png/webp — magic byte kontrolü, uzantı değil).
  - `UsersController` → `[Authorize]` + `{id}` yerine claim'den kendi profili (madde 10.3'te `/users/me`'ye taşınacak; bu maddede en azından `id == CurrentUserId` kontrolü şart).
  - JWT `AddAuthentication`'ı Web'deki cookie şemasıyla karıştırmadan public uçlarda user rolüne izin ver (mevcut `AdminPanel` policy'si sadece admin rolleri kabul ediyor — normal `user` rolü için ayrı policy gerekmez, `[Authorize]` yeter).
- **Doğrulama:** token'sız POST'lar 401; user token'ıyla admin-işi uçlar 403; kendi olmayan profile PUT 403; testlere en az 2 yetki senaryosu eklenmeli.
- [x] **YAPILDI (16 Tem 2026):**
  - **KARAR: Admin-işi public yazma kopyaları SİLİNDİ** (AdminPanel'e çevirmek yerine) — v1/admin/* altında birebir karşılıkları zaten var, Web paneli HTTP değil `ISender` kullanıyor, taramada hiçbir tüketici bulunmadı; yüzeyi küçültmek 10.10 envanterini de sadeleştirir. Silinenler: announcements POST/POST types/PUT/DELETE, pharmacies POST/PUT/DELETE, guide POST categories, events POST, places POST, taxis POST drivers, transport POST intercity/intracity, deaths PUT/DELETE. **Not (masterclass'tan sapma):** public events POST "kullanıcı etkinlik önerir + moderasyon" için duruyordu; 10.1 karar matrisi etkinliği admin-işi saydığından kaldırıldı — mobilde kullanıcı etkinlik önerisi istenirse `[Authorize]` + `CreatedBy` claim + `AutoApprove=false` ile ayrı maddede geri eklenmeli (controller'a not düşüldü).
  - **Deaths POST kullanıcı-işi:** `[Authorize]` + `AddedBy` artık claim'den (`CurrentUserId`), `AutoApprove=false` sabit → ilan `pending`'e düşer. Canlı doğrulandı: user token'la POST → DB'de `status=pending`, `added_by`=token'daki user_id.
  - **FilesController:** `[Authorize]` + limit 100 MB → **10 MB** + **magic-byte doğrulaması** (yalnız JPEG/PNG/WebP; Content-Type başlığına ve uzantıya güvenilmiyor, ilk 12 bayt okunuyor) + `UploadedBy` claim'den. Ekstra sertleştirme: kaydedilen dosya adı `Path.GetFileName` ile ayıklanıyor (path traversal) ve uzantı tespit edilen türe göre yazılıyor (`.jpg/.png/.webp`) — `evil.png` içinde HTML gönderme (polyglot/XSS) 400 `UNSUPPORTED_FILE_TYPE`. Kaydedilen `MimeType` da artık tespit edilen tür. Web paneli etkilenmiyor (UploadHelper `UploadFileCommand`'i doğrudan çağırıyor; panel formlarının hepsi `accept="image/*"`).
  - **UsersController (IDOR):** controller `[Authorize]`; GET/PUT `{id}/profile`'da `id != user_id claim` → 403 `FORBIDDEN` (geçici çözüm; kalıcısı 10.3 `/users/me`). PowerOutages'ın AdminPanel korumalı POST/PUT/DELETE kopyaları da tutarlılık için silindi (v1/admin/power-outages'ta duruyorlar).
  - **EKSTRA:** `ApiControllerBase`'e `CurrentUserId` property'si eklendi (JWT `user_id` claim'i, anonim istekte null — AdminApiControllerBase.CurrentAdminId'nin public karşılığı; Deaths/Files/Users bunu kullanıyor).
  - **Testler:** `Integration/Security/PublicEndpointAuthorizationTests.cs` — 5 test: token'sız yazmalar 401; silinen 9 POST ucu 404/405; user token'la `v1/admin/*` 403; başkasının profiline PUT 403 `FORBIDDEN` + kendi profili 200; sahte .png (HTML içerik) 400 `UNSUPPORTED_FILE_TYPE` + gerçek PNG imzası 200. **xUnit 12/12 yeşil** (7 eski + 5 yeni).
  - **Canlı doğrulama (16 Tem, curl):** token'sız deaths POST / upload / profile PUT → 401 (upload'a JSON gövde gönderilirse 415 — multipart beklentisi route seçiminde reddediyor, güvenlik açısından sorun değil); silinen 8 public POST → 405; user token'la admin ucu → 403; IDOR PUT → 403 zarfı; kendi profili → 200; sahte png → 400, gerçek png → 200 + `/uploads/` URL. CLAUDE-TEST kayıtları (vefat ilanı, dosya, test kullanıcısı) temizlendi, test API process'i kapatıldı.

### 10.2 — ✅ TAMAMLANDI (16 Tem 2026) — Auth akışının tamamlanması: refresh, logout, kayıt (register) akışı
> **Tespit:** `AuthController`'da yalnız `login` + `verify-otp` var. Config'de `Jwt:RefreshSecret`/`RefreshExpiresDays=90` tanımlı ama **`POST /v1/auth/refresh` endpoint'i hiç yok** — access 30 günde dolunca mobil kullanıcı düpedüz atılır. `logout` yok. Masterclass §12.3'teki kayıt akışı (verify-otp → kullanıcı yoksa `temp_token` → `POST register` ile username+mahalle alınır) uygulanmamış: mevcut VerifyOtpHandler kullanıcıyı username'siz/mahallesiz otomatik oluşturuyor.
- Yapılacaklar:
  - `POST /v1/auth/refresh`: body'deki refresh token'ı `RefreshSecret` ile doğrula → yeni access+refresh çifti dön. Refresh token rotasyonu: eski refresh'i Redis'te iptal listesine yaz (`jti` claim ekle).
  - `POST /v1/auth/logout` (`[Authorize]`): refresh token'ı iptal et + `User.FcmToken`'ı temizle (yanlış cihaza push gitmesin).
  - Kayıt akışı KARARI + implementasyonu: ya masterclass'taki temp_token+register'a geç (mobil onboarding'de username/mahalle/yaş ekranı olacaksa gerekli), ya da mevcut otomatik oluşturmayı koru ve `verify-otp` yanıtına `isNewUser` bayrağı ekleyip profil tamamlamayı 10.3'teki `PATCH /users/me`'ye bırak. Karar Progress'e yazılmalı.
  - Access token süresi 30 gün → refresh artık var; mobil için 1 gün gibi kısa süre değerlendir (`Jwt:AccessExpiresDays` config).
- **Doğrulama:** refresh ile yeni token alınıp korumalı uca girilmeli; logout sonrası aynı refresh 401; süresi geçmiş access + geçerli refresh senaryosu canlı test.
- [x] **YAPILDI (16 Tem 2026):**
  - **KARAR (kayıt akışı): masterclass §12.3'teki temp_token + register'a GEÇİLDİ** (otomatik kullanıcı oluşturma kaldırıldı). Gerekçe: Flutter kontratının endpoint haritası (§13.2) `POST register` içeriyor; mobil onboarding'de username/mahalle ekranı olacak; 10.3'teki username değişim kuralları username'siz otomatik hesaplarla çelişirdi. `verify-otp` yanıtı artık iki biçimli: kayıtlı kullanıcı → `{isNewUser:false, accessToken, refreshToken, expiresIn}`; yeni kullanıcı → `{isNewUser:true, tempToken}` (30 dk, `Jwt:TempTokenMinutes`). Eski `{token}` alanı KALDIRILDI (mobil henüz yazılmadığından kırılma yok; 10.10 kontratına bu şema girecek).
  - **Yeni uçlar (hepsi Application command'i üzerinden, `auth` rate-limit policy'sinde):** `POST /v1/auth/register` (`RegisterCommand`: tempToken doğrula → username 3-30 karakter/boşluksuz + case-insensitive unique (409 CONFLICT), mahalle var+aktif kontrolü (400 VALIDATION_ERROR), yaş 13-120 opsiyonel, soft-delete'li telefon 403; kullanıcı oluşturulur → access+refresh döner). `POST /v1/auth/refresh` (`RefreshTokenCommand`: RefreshSecret doğrulama + jti iptal listesi kontrolü + kullanıcı DB'den TAZE okunur — ban/pasif 403, rol değişimi yeni access'e yansır + **rotasyon**: eski jti kalan ömrü kadar Redis iptal listesine). `POST /v1/auth/logout` (`[Authorize]`, `LogoutCommand`: body'deki refresh SAHİPLİK kontrolüyle (payload.UserId == claim) iptal edilir + `User.FcmToken` temizlenir — cihaz başka hesaba geçerse yanlış push gitmesin).
  - **Token mimarisi (güvenlik):** temp ve refresh token'lar **RefreshSecret** ile imzalanır ve `token_type` claim'i taşır (`registration`/`refresh`) — JwtBearer yalnız AccessSecret kabul ettiğinden bu token'lar `[Authorize]` uçlarından ASLA geçemez (canlı doğrulandı: temp token korumalı uçta 401, refresh olarak da 401); refresh token `jti` (Guid) taşır. İptal listesi: `ITokenBlacklistService` (Application) + `Infrastructure/Identity/RedisTokenBlacklistService` (`revoked_jti:{jti}`, TTL=token'ın kalan ömrü — liste şişmez). `IJwtProvider` genişletildi: `GenerateTokens` (çift), `GenerateTempToken`, `ValidateTempToken`, `ValidateRefreshToken`.
  - **Access süresi 30 gün → 1 GÜN** (`Jwt:AccessExpiresDays:1` — refresh artık var; double okunur, canlı testte 0.0002 gün ≈ 17 sn ile süresi-dolmuş-access senaryosu koşuldu). `expiresIn` yanıtı saniye cinsi (86400).
  - **EKSTRA 1 (kritik test-altyapı keşfi):** Entegrasyon testleri bugüne dek **Testcontainers'a DEĞİL dev veritabanına yazıyormuş** — WebApplicationFactory (minimal hosting) config override'larını host BUILD edilirken eklediğinden `AddInfrastructure`'daki eager `GetConnectionString` okuması dev değerini yakalıyordu (istek anında okunan `Otp:DevMode` override'ı çalıştığı için fark edilmemişti; dev DB'de 15-16 Tem tarihli test kullanıcıları kanıt). Düzeltme: EF/Hangfire/health-check bağlantıları artık kayıt anında değil KULLANIM anında DI'daki `IConfiguration`'dan okunuyor (`PostgresConn(sp)` helper; Redis multiplexer zaten lazy'ydi). Kanıt: test koşusu öncesi/sonrası dev DB satır sayısı değişmiyor.
  - **EKSTRA 2:** Program.cs'teki JwtBearer `AccessSecret ?? "default_secret_key_..."` güvensiz fallback'i kaldırıldı — secret yoksa startup'ta `InvalidOperationException`.
  - **Testler:** `AuthIntegrationTests` yeni akışa göre yeniden yazıldı — 7 test: yeni kullanıcı akışı (tempToken→register→korumalı uç→ikinci verify isNewUser=false), username çakışması 409 (case-insensitive), temp token korumalı uçta VE refresh olarak 401, refresh rotasyonu (eski 401/yeni çalışır), logout iptali + auth şartı, bozuk refresh 401, yanlış OTP 400. `PublicEndpointAuthorizationTests.GetUserTokenAsync` register akışına geçirildi. **xUnit 17/17 yeşil.**
  - **Canlı doğrulama (16 Tem, curl :5005):** login→verify (isNewUser:true+tempToken) → register (86400 sn access + 90 gün refresh, DB'de username/yaş/mahalle doğru) → kendi profili 200; temp token misuse 2×401; refresh rotasyonu → Redis'te `revoked_jti:*` TTL≈90 gün, eski refresh 401 `iptal edilmiş`; token'sız logout 401; logout → refresh 401 + `fcm_token` NULL (öncesinde psql ile doldurulmuştu); `Jwt__AccessExpiresDays=0.0002` ile süresi dolan access 401 → geçerli refresh'le yeni çift → 200. Bu sırada auth rate limit'i de canlıda 429 verdi (Faz 9.2 çalışıyor). Test kullanıcıları silindi, API kapatıldı.
  - ~~**⚠️ KALAN TEMİZLİK (izin engeli — kullanıcı onayı gerekli)**~~ — **[x] TAMAMLANDI (17 Tem 2026, kullanıcı onayıyla):** dev DB'den `files` 2 satır + `users` 4 satır (+905001234567, +905011110001/2/3) silindi; `uploads/` altında 3 fiziksel `*_gercek.png` silindi (Progress'te 2 yazıyordu, üçüncüsü DB'de hiç referansı olmayan aynı test turundan yetim dosyaydı).

### 10.3 — ✅ TAMAMLANDI (17 Tem 2026) — Users/me uçları + FCM token kaydı
> **Tespit:** Masterclass kontratı `GET /users/me`, `PATCH /users/me`, `PATCH /users/me/notifications` istiyor; mevcutta yalnız IDOR'lu `{id}/profile` var. `User` entity'sinde `FcmToken` ve `NotificationPreferences` alanları VAR ama hiçbir uç bunları yazmıyor.
- Yapılacaklar:
  - `GET /v1/users/me` (`[Authorize]`, claim'deki `user_id`): profil + mahalle adı + bildirim tercihleri.
  - `PATCH /v1/users/me`: username (masterclass kuralı: `username_last_changed_at` — örn. 30 günde bir), yaş, `primaryNeighborhoodId` (`neighborhood_last_changed_at` kuralı), `profilePhotoFileId` (upload akışıyla).
  - `PATCH /v1/users/me/notifications`: `NotificationPreferences` jsonb güncelle.
  - `POST /v1/notifications/fcm-token` (`[Authorize]`): `User.FcmToken` yaz (10.7'nin ön koşulu; küçük olduğundan bu maddeye alındı).
  - Eski `GET/PUT /v1/users/{id}/profile` uçlarını kaldır ya da admin-only yap (10.1'deki geçici kontrolün kalıcı çözümü).
- **Doğrulama:** iki farklı kullanıcı token'ıyla /me izolasyonu; username değişim kuralı ihlali → 400/409; fcm-token DB'de.
- [x] **YAPILDI (17 Tem 2026):**
  - **Yeni Application dosyaları (hepsi sade DTO döner → filter zarfı sarar, meta.traceId dolu — 10.10 tercih edilen desen):** `Features/Users/DTOs/MyProfileDto.cs` (+`NotificationPreferencesDto`; profil + mahalle adı + tercihler + `usernameLastChangedAt`/`neighborhoodLastChangedAt`), `Queries/GetMyProfile/GetMyProfileQuery.cs`, `Commands/UpdateMyProfile/UpdateMyProfileCommand.cs`, `Commands/UpdateNotificationPreferences/UpdateNotificationPreferencesCommand.cs`, `Features/Notifications/Commands/RegisterFcmToken/RegisterFcmTokenCommand.cs`.
  - **Uçlar:** `GET /v1/users/me`, `PATCH /v1/users/me` (username/age/primaryNeighborhoodId/profilePhotoFileId/removeProfilePhoto — PATCH semantiği: null alan değişmez), `PATCH /v1/users/me/notifications` (yalnız gönderilen anahtarlar değişir), `POST /v1/notifications/fcm-token` (yeni `NotificationsController` — 10.7'de listeleme/read uçları buraya eklenecek).
  - **Kurallar:** username formatı RegisterCommand ile birebir (3-30, boşluksuz, case-insensitive unique → 409 CONFLICT); değişim sıklığı **30 günde bir** (handler'da const; ihlal → 400 `USERNAME_CHANGE_LIMIT` / `NEIGHBORHOOD_CHANGE_LIMIT`; **ilk değişiklik serbest** — kayıt anı sayaç başlatmaz, aynı değeri göndermek no-op). Mahalle: var+aktif kontrolü. Yaş 13-120.
  - **profilePhotoFileId güvenliği:** yalnız kullanıcının KENDİ yüklediği (`files.uploaded_by = user_id`) dosya kabul edilir — başkasının dosyasını sahiplenme → 400 VALIDATION_ERROR. `ProfilePhotoUrl` dosyanın `cdn_url`'inden set edilir; `removeProfilePhoto:true` → NULL.
  - **KARAR: eski `GET/PUT /v1/users/{id}/profile` uçları SİLİNDİ** (admin-only yapmak yerine; 10.1'in silme deseniyle tutarlı — id her zaman claim'den, IDOR yüzeyi tamamen kalktı; admin karşılığı `v1/admin/users`). Ölü kalan `GetUserProfileQuery`/`UpdateUserProfileCommand`/`UserProfileDto`/`UpdateUserProfileDto` da silindi.
  - **EKSTRA 1 (planda yoktu):** `RegisterFcmTokenCommand` aynı token'ı taşıyan BAŞKA kullanıcılardan token'ı temizler — cihaz logout'suz hesap değiştirirse eski hesaba yanlış push gitmez (10.2'deki LogoutCommand temizliğinin tamamlayıcısı; xUnit'te iki kullanıcıyla doğrulandı).
  - **EKSTRA 2 (planda yoktu):** `KadirliApp.Tests/xunit.runner.json` → `parallelizeTestCollections:false` (+csproj CopyToOutput). Artık 5 entegrasyon test sınıfı var; paralel koşuda her sınıfın kendi Testcontainers çifti çakışıp flaky "connection refused / terminating connection" hataları üretiyordu (bu oturumda canlı gözlendi) — sıralı koşuyla deterministik, süre hâlâ ~7 sn.
  - **Testler:** yeni `Integration/Users/UsersMeTests.cs` — 5 test: /me izolasyonu + mahalle adı + varsayılan tercihler; username ilk değişim OK → ikinci 400 USERNAME_CHANGE_LIMIT → no-op OK → çakışma 409; mahalle ilk değişim OK → ikinci 400; notifications kısmi güncelleme; fcm-token DB'de + ikinci kullanıcı aynı token'ı kaydedince ilkinden temizlenir + boş token 400 + token'sız 401. Auth/Security testlerindeki `{id}/profile` referansları `/v1/users/me`'ye geçirildi (silinen uç için 404 asserti eklendi). **xUnit 22/22 yeşil.**
  - **Canlı doğrulama (17 Tem, curl :5005):** register → GET /me (mahalle adı "Şehit Kansu" + tercihler dolu); username 1. değişim 200 + timestamp, 2. → 400 USERNAME_CHANGE_LIMIT; notifications `{deaths:false, ads:true}` → yalnız o ikisi değişti; PNG upload → `profilePhotoFileId` ile photoUrl set, olmayan/başkasının id → 400, remove → NULL; mahalle 1. değişim "Savrun" + timestamp, 2. → 400 NEIGHBORHOOD_CHANGE_LIMIT; fcm-token → DB'de `fcm_token` dolu; eski `{id}/profile` → 404. Test kullanıcısı + dosya (DB+fiziksel) + Redis otp anahtarları temizlendi, API kapatıldı.

### 10.4 — ✅ TAMAMLANDI (17 Tem 2026) — Nöbetçi eczane + eksik public lookup uçları
> **Tespit:** Mobilin EN ÇOK kullanılacak özelliği "bugün nöbetçi eczane hangisi" — `PharmacySchedule` entity'si ve seed verisi VAR ama public API'de nöbet ucu YOK (yalnız düz liste). Ayrıca mobilin form/filtre ekranları için lookup uçları eksik: mahalleler (kayıt + duyuru hedefleme gösterimi), mezarlık/cami (vefat), etkinlik kategorileri (`EventCategory` entity var, uç yok).
- Yapılacaklar:
  - `GET /v1/pharmacies/on-duty` (bugünün nöbetçileri; `?date=` opsiyonel) ve `GET /v1/pharmacies/schedule?year&month` (aylık takvim) — `PharmacySchedule` üzerinden yeni query'ler.
  - `GET /v1/neighborhoods`, `GET /v1/deaths/cemeteries`, `GET /v1/deaths/mosques`, `GET /v1/events/categories` — basit lookup query'leri.
  - HEPSİNİ Faz 9.4 cache altyapısına bağla: lookup'lar `ICacheableQuery` (yeni `lookups` grubu veya mevcut gruplar; nöbetçi eczane `pharmacies` grubuna — admin nöbet değiştirince invalidation için nöbet CRUD command'lerine `ICacheInvalidator` ekle; admin nöbet CRUD'u yoksa önce onu Application'a çıkar).
- **Doğrulama:** seed verisiyle on-duty doğru eczaneyi dönmeli (DB'deki schedule ile karşılaştır); ikinci istek cache hit (redis-cli); nöbet değişikliği sonrası taze veri.
- [x] **YAPILDI (17 Tem 2026):**
  - **Tespit doğrulandı:** `PharmacySchedule` bugüne dek YALNIZ seeder'da kullanılıyordu — Application/Api/Web hiçbir katmanda nöbet CRUD'u yoktu. Önce o çıkarıldı: `Features/Pharmacies/Commands/PharmacyScheduleCommands.cs` (`CreatePharmacyScheduleCommand` + `DeletePharmacyScheduleCommand`, ikisi de `ICacheInvalidator` → `pharmacies` grubu). Create: eczane var kontrolü, `duty_date` amaçlanan yerel günün UTC gece yarısına normalize (seeder konvansiyonu), **aynı eczane + aynı gün → 409 CONFLICT** (aynı gün FARKLI eczaneler serbest — birden çok nöbetçi olabilir); StartTime/EndTime verilmezse entity varsayılanı (19:00/09:00).
  - **Public uçlar:** `GET /v1/pharmacies/on-duty?date=` (`GetOnDutyPharmaciesQuery` — schedule+eczane bilgisi tek DTO'da; saatler "HH:mm" string, TimeSpan formatı SQL'e çevrilemediğinden projeksiyon bellek tarafında) ve `GET /v1/pharmacies/schedule?year&month` (`GetPharmacyScheduleQuery`; geçersiz yıl/ay → 400). İkisi de `ICacheableQuery`, grup `pharmacies`, 15 dk.
  - **Admin uçlar (`v1/admin/pharmacies`, RequirePermission pharmacies):** `GET /schedule?year&month` (public'le aynı query), `POST /schedule`, `DELETE /schedule/{id}`.
  - **Lookup uçları:** `GET /v1/neighborhoods` (yeni `NeighborhoodsController`; aktifler, DisplayOrder+Name sıralı), `GET /v1/deaths/cemeteries`, `GET /v1/deaths/mosques`, `GET /v1/events/categories`. Hepsi yeni `Features/Lookups/LookupQueries.cs`'te (4 query + 3 DTO tek dosyada), `ICacheableQuery` yeni **`CacheGroups.Lookups`** grubunda 15 dk TTL — bu tabloların CRUD'u YOK (yalnız DbSeeder), invalidation'sız TTL yeterli; ileride admin CRUD'u gelirse command'lere `ICacheInvalidator(Lookups)` eklenmeli (dosyada not var).
  - **EKSTRA 1 (planda yoktu) — `TurkeyClock`:** on-duty'nin varsayılan "bugün"ü UTC değil **Türkiye günü** (`Europe/Istanbul`, bulunamazsa sabit UTC+3 — 2016'dan beri DST yok). UTC kullanılsaydı her gece 00:00-03:00 arası mobile YANLIŞ günün nöbetçisi dönerdi. Cache anahtarı efektif tarihi içerdiğinden gece yarısında kendiliğinden değişir.
  - **EKSTRA 2 (planda yoktu):** mükerrer nöbet koruması (aynı eczane+gün 409) — plan belirtmiyordu, admin çift tıklamasına karşı eklendi.
  - **Testler:** yeni `Integration/Pharmacies/PharmacyScheduleAndLookupTests.cs` — 4 lookup Theory (anonim 200 + seed verisi dolu) + 1 uzun akış Fact: admin token (seed super_admin OTP ile), eczane oluştur, anonim 401 / user token 403, boş on-duty CACHE'LENİR → create sonrası taze (invalidation kanıtı), 409, on-duty içerik+saat asserti, aylık takvim, ay=13 → 400, delete → tekrar boş (ikinci invalidation kanıtı). **xUnit 27/27 yeşil.**
  - **Canlı doğrulama (17 Tem, curl :5005):** 4 lookup seed sayılarıyla döndü (10 mahalle/3 mezarlık/4 cami/6 kategori), Redis'te `cache:lookups:*` 4 anahtar, cache hit 9ms→1.3ms; on-duty seed günü (2026-07-03) Merkez Eczanesi'ni DB ile eşleşerek döndü; admin bugüne Savrun nöbeti ekledi → `cache:pharmacies:*` anahtarlarının SİLİNDİĞİ redis-cli ile gözlendi → on-duty taze "Savrun Eczanesi"; mükerrer 409; aylık takvim 2 kayıt; ay=13 400; delete → boş. Test nöbet kaydı API'den silindi (dev DB'de kalıntı yok), API kapatıldı.

### 10.5 — ✅ TAMAMLANDI (17 Tem 2026) — İlan (Ads) mobil uçları, Bölüm 1: kategoriler, detay, kullanıcı ilan verme
> **Tespit:** Mobilde kullanıcılar ilan verir (masterclass §13.1'de kontrat tam) ama public API'de SADECE `GET /v1/ads` listesi var. `AdCategory`, `CategoryProperty`, `PropertyOption`, `AdPropertyValue` entity'leri hiçbir public uçta kullanılmıyor; ilan DETAY ucu bile yok.
- Yapılacaklar:
  - `GET /v1/ads/categories` (`?parentId=` ile hiyerarşi) ve `GET /v1/ads/categories/{id}/properties` (kategoriye özel alanlar + seçenekleri) — cache'lenebilir (`ads-lookup` grubu).
  - `GET /v1/ads/{id}`: detay + görseller (cdn_url) + property değerleri + iletişim bilgisi; `view_count` artışı (ayrı fire-and-forget update, cache'li listeyi bozmaz).
  - `POST /v1/ads` (`[Authorize]`): kullanıcı ilanı → `UserId` claim'den, `status=pending` (admin onayına düşer — panel approve akışı zaten var), `imageFileIds` + `propertyValues` destekli. Mevcut `CreateAdCommand` admin senaryosuna göre — kullanıcı senaryosu için status/validasyon farkları komutta parametreleşmeli.
- **Doğrulama:** kategori ağacı + properties seed/panel verisiyle dolu dönmeli; user token'la ilan ver → DB'de `pending` + panelde görünüyor + approve edilince public listede.
- [x] **YAPILDI (17 Tem 2026):**
  - **Lookup uçları:** `GET /v1/ads/categories?parentId=` (parametresiz kök, parentId ile alt kategoriler; `subCategoryCount` alanıyla mobil ağacı lazy açabilir) + `GET /v1/ads/categories/{id}/properties` (property + options; olmayan/pasif kategori → 404). İkisi de yeni `Features/Ads/Queries/AdCategoryQueries.cs`'te (LookupQueries deseni: query+handler+DTO tek dosyada), `ICacheableQuery` yeni **`CacheGroups.AdsLookup`** ("ads-lookup") grubunda 15 dk TTL — bu tabloların CRUD'u yok (yalnız DbSeeder), invalidation'sız TTL yeterli (dosyada not var).
  - **Detay ucu:** `GET /v1/ads/{id}` → yeni `GetAdByIdQuery(Id, RequesterId?)` + `AdDetailDto` (kategori adı, görseller `AdImageDto` cdn_url'li, `properties` [propertyId/propertyName/propertyType/value], iletişim, expiresAt). **Görünürlük kuralı:** approved olmayan ilanı YALNIZ sahibi görür (RequesterId=claim), diğer herkese 404 — ilanın varlığı sızdırılmaz. **view_count:** tracked entity değil ayrı atomik `ExecuteUpdateAsync` (+1) — yarışta kayıp artış yok, cache invalidation tetiklemez; bunun için Application'a `Microsoft.EntityFrameworkCore.Relational` 8.0.0 paketi eklendi (ExecuteUpdate o pakette).
  - **Kullanıcı ilan verme:** `POST /v1/ads` `[Authorize]` — controller `CreateAdDto`'dan command kurar: `UserId` claim'den, `IsUserSubmission=true`, `status=pending` sabit (admin onayına düşer; panel approve akışı mevcut). Yanıt 201 + id (CreatedAtAction → detay ucu).
  - **CreateAdCommand parametreleşmesi:** `PropertyValues` (Dictionary<Guid,string>) + `IsUserSubmission` bayrağı eklendi. Handler'a validasyon yazıldı: başlık 3-200 / açıklama ≤5000 / fiyat ≥0 / kategori var+aktif / görsel ≤10 (distinct) — bunlar TÜM akışlarda; yalnız `IsUserSubmission=true` iken ek olarak: cep telefonu format regex'i (panelin sabit hat girebilmesi için user-only), **görsel sahipliği** (`files.uploaded_by == UserId`, 10.3 profilePhotoFileId emsali) ve **zorunlu property denetimi** (panelin property UI'ı yok — admin akışını kilitlememek için user-only). Gönderilen property değerleri her akışta doğrulanır: kategoriye ait olmayan id / Select-MultiSelect'te tanımsız seçenek / Number-Boolean parse hatası / Text >500 → 400 `VALIDATION_ERROR` (FluentValidation.ValidationException, middleware'de zaten mapli).
  - **🐛 BULUNAN ve DÜZELTİLEN GÜVENLİK BUG'ı:** public `GET /v1/ads` bugüne dek status filtrelemiyordu — **pending/rejected ilanlar iletişim telefonlarıyla birlikte herkese dönüyordu**, süresi biten ilanlar da listede kalıyordu. `GetAdsQuery`'ye `OnlyPublished` (varsayılan false — panel/admin API davranışı DEĞİŞMEDİ) eklendi; public uç true geçer → `status=approved AND expires_at > now`.
  - **Seed genişletmesi (DbSeeder.SeedAdCategoryTreeAsync):** ana bloklardan sonra koşan iki YENİ idempotent blok — (1) alt kategoriler (`ParentId != null` yoksa): Araçlar→Otomobil/Motosiklet/Ticari Araç, Emlak→Satılık Konut/Kiralık Konut/Arsa (slug ile ebeveyn bulunur — dev DB'de kategoriler dolu olsa da çalışır); (2) kategori özellikleri (tablo boşsa): Otomobil→Yakıt Tipi*/Vites* (Select+options), Model Yılı* (Number), Kilometre, Renk; Satılık Konut→Oda Sayısı* (Select), Metrekare* (Number), Isınma (Select), Bina Yaşı (* = IsRequired). Dev DB'de doğrulandı: 6 alt kategori + 9 property + 18 option oluştu.
  - **⚠️ NOT (10.10'a taşınan tespit):** `Features/Ads/Validators/*` FluentValidation sınıfları hiçbir pipeline'a kayıtlı DEĞİL (ölü kod — `AddValidatorsFromAssembly`/ValidationBehavior yok). 10.5 validasyonu bilinçli olarak handler'a yazıldı; 10.10 kontrat temizliğinde genel karar verilmeli (behavior ekle + validator'ları canlandır YA DA validator dosyalarını sil).
  - **Testler:** yeni `Integration/Ads/AdsMobileTests.cs` — 4 test: kategori hiyerarşisi (kök 8 + subCategoryCount=3 + parentId filtresi); properties (seed tanımları + options + olmayan kategori 404); tam akış (anonim POST 401 → user ilan + 3 property → DB'de pending → public listede YOK + anonim detay 404 + sahip detayı 200 → admin approve → listede VAR + viewCount ardışık istekte +1); validasyon (zorunlu property eksik / geçersiz select / sahte görsel id / kısa başlık → 4×400 VALIDATION_ERROR zarfı). **xUnit 31/31 yeşil.**
  - **Canlı doğrulama (17 Tem, curl :5005):** kök 8 kategori (araclar/emlak subCount=3), alt kategoriler parentId'li; properties options'larıyla döndü, cache 36ms→3.7ms + Redis'te `cache:ads:*` 3 anahtar `cache-group:ads-lookup` set'inde; olmayan kategori 404 zarfı; register→PNG upload→ilan (201, DB: pending+3 property+is_cover) → public liste 0/anonim 404/sahip 200 (görsel URL'li) → approve 200 → liste 1, viewCount 1→2 (DB 3 — sahip görüntülemesi dahil); 5 validasyon senaryosu (401 + 4×VALIDATION_ERROR, sabit hat reddi dahil). Test kayıtları (ilan+görsel DB/fiziksel+kullanıcı+Redis otp) temizlendi, API kapatıldı.

### 10.6 — ✅ TAMAMLANDI (17 Tem 2026) — İlan (Ads) mobil uçları, Bölüm 2: benim ilanlarım, favoriler, uzatma, iletişim sayaçları
> **Tespit:** `AdFavorite` ve `AdExtension` entity'leri tamamen kullanım dışı; me-scoped uçlar yok; telefon/whatsapp tıklama sayaçları (`contact_phone_clicks` vb. — entity'de hangi alanlar varsa) işlenmiyor.
- Yapılacaklar (hepsi `[Authorize]`):
  - `GET /v1/users/me/ads` (`?status=` filtreli, paged) ve kendi ilanını `PUT /v1/ads/{id}` / `DELETE /v1/ads/{id}` (sahiplik kontrolü: `ad.UserId == CurrentUserId`, değilse 403 — admin uçlarıyla karışmasın).
  - `POST /v1/ads/{id}/favorite` + `DELETE /v1/ads/{id}/favorite` + `GET /v1/users/me/favorites` (`AdFavorite`; aynı ilana ikinci favori → idempotent/409 kararı).
  - `POST /v1/ads/{id}/extend` (`AdExtension` satırı + `expires_at` uzatma; ücretli senaryo yoksa gün sayısı sabit/config).
  - `POST /v1/ads/{id}/track-phone` ve `/track-whatsapp` (anonim olabilir — sayaç artışı).
- **Doğrulama:** iki kullanıcıyla sahiplik izolasyonu (403); favori ekle/çıkar/listele; extend sonrası `expires_at` DB'de uzadı; ExpireAdsJob uzatılmış ilanı vaktinden önce kapatmıyor.
- [x] **YAPILDI (17 Tem 2026):**
  - **Ortak validasyon refactor'ı:** CreateAdCommandHandler'ın 10.5'teki private validasyonları `Features/Ads/AdSubmissionRules.cs`'e (internal static) çıkarıldı — ValidateContent + ValidateImageOwnershipAsync + ValidatePropertyValuesAsync; CreateAd ve yeni UpdateMyAd aynı kuralları paylaşır (davranış birebir korundu, eski testler yeşil).
  - **Kendi ilanını güncelleme — `PUT /v1/ads/{id}`** (`UpdateMyAdCommand`): sahiplik kontrolü (`ad.UserId != claim` → 403 FORBIDDEN), user-submission validasyonları (cep telefonu regex, görsel sahipliği, zorunlu property denetimi), görsel add/remove + kapak garantisi (admin UpdateAd deseni). **KARAR 1 (masterclass'tan sapma):** masterclass §13.1 `PATCH` der, plan `PUT` yazdı — plan izlendi (Memory_Bank kazanır; 10.10 kontratına PUT girecek). **KARAR 2:** her kullanıcı düzenlemesi ilanı **yeniden moderasyona düşürür** (status=pending, ApprovedBy/At + RejectedReason/At temizlenir — onaylı içerik onaysız değişemez; rejected ilanın düzeltilip yeniden gönderilme yolu da bu). **KARAR 3:** kategori DEĞİŞTİRİLEMEZ (property tanımları kategoriye bağlı; body'de categoryId alanı yok). **KARAR 4:** update `ExpiresAt`'e dokunmaz — süre işi extend'in.
  - **Kendi ilanını silme — `DELETE /v1/ads/{id}`** (`DeleteMyAdCommand`): sahiplik 403, olmayan/silinmiş 404, soft delete. Admin'in DeleteAdCommand'i (bool dönen) panel için aynen duruyor.
  - **Favoriler:** `POST/DELETE /v1/ads/{id}/favorite` (`FavoriteAdCommands.cs`) + `GET /v1/users/me/favorites` (`GetMyFavoritesQuery`, paged). **KARAR (plan "idempotent/409 kararı" istiyordu): İDEMPOTENT** — mobil çift tıklama/offline retry'da 409 istemciye gereksiz hata yönetimi doğurur; dönen bool "değişiklik oldu mu" (2. ekleme → 200 + data:false). Yarışta unique index'e takılan `DbUpdateException` da idempotent yutulur. Görünürlük: yalnız approved ilan favorilenebilir (aksi 404 — varlık sızdırılmaz); silinen ilanın favorisi listeden VE totalCount'tan düşer (`f.Ad.DeletedAt == null` join'i), yayından düşen `isAvailable:false` ile döner (mobil soluk gösterir).
  - **Benim ilanlarım — `GET /v1/users/me/ads?status=&page=&limit=`** (`GetMyAdsQuery` + `MyAdDto`): sahibi TÜM statüleri görür; status filtresi whitelist'li (pending/approved/rejected/expired, aksi 400). **EKSTRA (planda yoktu):** `MyAdDto` sahibe özel alanlar taşır — `rejectedReason`, `favoriteCount`, `phoneClickCount`/`whatsappClickCount`, `extensionCount`/`maxExtensions` (ilan performans ekranı için; public AdResponseDto'da yoklar). Me-scoped iki uç UsersController'da (route `v1/users/me/*` doğal düşer).
  - **Uzatma — `POST /v1/ads/{id}/extend`** (`ExtendMyAdCommand`, opsiyonel body `{adsWatched}` — `EmptyBodyBehavior.Allow`): süre **sabit 30 gün** (handler'da const — 10.3 emsali; ücretli/reklamlı senaryo gelirse parametrelenir), hak `ad.MaxExtensions` (varsayılan 3), dolunca 409 CONFLICT. **KARAR:** yalnız approved/expired uzatılabilir (pending/rejected → 400); **expired ilan uzatmayla yeniden approved olur** (içerik zaten moderasyondan geçmişti, yalnız süresi dolmuştu); yeni bitiş `max(now, ExpiresAt) + 30g` — erken uzatan gün kaybetmez. `ad_extensions` satırı (adsWatched/daysExtended/extendedAt) + `extension_count` artar; yanıt `{status, expiresAt, extensionCount, maxExtensions, remainingExtensions}`.
  - **İletişim sayaçları — `POST /v1/ads/{id}/track-phone` + `/track-whatsapp`** (`TrackAdContactCommand`): **anonim** (masterclass §13.1 kontratı); GetAdById'nin view_count deseniyle tek atomik `ExecuteUpdateAsync` (+1) — yarışta kayıp yok, cache invalidation tetiklemez; yalnız approved ilanlarda (aksi 404).
  - **Testler:** yeni `Integration/Ads/AdsMobilePart2Tests.cs` — 5 test: sahiplik (anon 401 / başkası 403 / güncelleme→pending+onay izleri temiz / geçersiz içerik 400 / silme→soft delete→404); favoriler (idempotent data true/false, DB tek satır, me/favorites, pending 404, favoriteCount); me/ads (tüm statüler + filtre + izolasyon + bozuk filtre 400); extend (403/400, **ExpireAdsJob testin içinde koşuldu**: job expired yaptı → extend approved'a döndürdü → job tekrar koştu ve DOKUNMADI; bitişe-ekleme ~60 gün, 4. uzatma 409); sayaçlar (anonim 2+1, pending/olmayan 404, me/ads'te görünür). **xUnit 36/36 yeşil.**
  - **Canlı doğrulama (17 Tem, curl :5005):** iki gerçek kullanıcı + seed admin ile tam akış — anon PUT 401 / başkası PUT-DELETE 403 zarfıyla / sahip PUT 200 → DB `pending`+`approved_by` NULL; favori true→false idempotent + DB 1 satır + me/favorites isAvailable:true + pending favori 404; me/ads favoriteCount:1 + status filtresi + bozuk filtre VALIDATION_ERROR + öteki kullanıcıda total:0; anonim sayaçlar DB'de 2/1, pending+olmayan 404; extend: expired ilan → approved + kalan 30 gün + `ad_extensions(ads_watched=2, days=30)`, 2.-3. uzatma bitişe ekledi (16 Eyl), 4.sü 409 CONFLICT zarfı; sahip silme → `deleted_at` dolu, detay 404, silinen ilan favori listesinden düştü (total:0). Auth rate limit canlıda yine 429 verdi (beklenen — token alımları 60 sn arayla yapıldı). Test kayıtları (3 ilan+1 favori+3 uzatma+2 kullanıcı+Redis otp) temizlendi, API kapatıldı.

> ⚠️ **NUMARALANDIRMA NOTU (17 Tem 2026, denetim sonrası):** 10.6 sonrası yapılan proje geneli denetimde (aşağıdaki 10.7-10.9) mobile geçmeden kapatılması gereken eksik/yanlışlar bulundu ve araya eklendi; eski 10.7-10.10 maddeleri **10.10-10.13'e ötelendi**. 10.6 ve öncesi kayıtlardaki "10.7"/"10.10" referansları ESKİ numaralandırmadır (ör. "karar 10.10'da" → artık 10.13; "10.7'nin ön koşulu" → artık 10.10).

### 10.7 — ✅ TAMAMLANDI (17 Tem 2026) — Public API görünürlük/güvenlik taraması Bölüm 2 + istek sertleştirme (17 Tem 2026 denetim bulguları)
> **Tespit (denetim, 17 Tem 2026):** 10.5'te Ads'te bulunan "status filtresiz public liste" bug'ının AYNISI diğer modüllerde de var. En kritiği Deaths: 10.1'de POST kullanıcı-işi yapıldı (pending'e düşer) ama public liste/detay status filtrelemiyor — **moderasyondan geçmemiş kullanıcı içeriği yayında**. Detay uçlarının çoğu da id bilinirse her statüdeki kaydı dönüyor.
- Yapılacaklar (bug düzeltmeleri — hepsi Ads'teki `OnlyPublished`/görünürlük-kuralı deseniyle):
  - **Deaths:** `GET /v1/deaths` status filtrelemiyor (`GetDeathNoticesQueryHandler` — client `?status=pending` bile isteyebiliyor); public uç yalnız `approved` dönmeli (archived dahil edilmemeli ya da `?includeArchived=` kararı — ArchiveDeathsJob `archived` yapıyor). `GET /v1/deaths/{id}` (`GetDeathNoticeByIdQueryHandler`) statüden bağımsız dönüyor → approved-değilse yalnız ekleyen (`AddedBy`) görsün (Ads detay emsali), diğerine 404. Admin API davranışı DEĞİŞMEMELİ (query'ye OnlyPublished parametresi).
  - **Events detayı:** `GET /v1/events/{id}` (`GetEventByIdQueryHandler`) pending/rejected etkinliği id ile dönüyor (liste doğru — controller `Status="approved"` zorluyor). Approved-değilse 404.
  - **Campaigns detayı:** `GET /v1/campaigns/{id}` (`GetCampaignByIdQuery`) aynı sorun (liste doğru). Approved + tarih-geçerli değilse 404 (liste `OnlyActive` kuralıyla tutarlı).
  - **Announcements detayı:** `GET /v1/announcements/{id}` (`GetAnnouncementByIdQuery`) pending/scheduled duyuruyu id ile dönüyor (liste doğru — `OnlyPublished=true`). Public'te yalnız `active` + görünürlük süresi geçmemiş.
  - **Taxis:** `GET /v1/taxis/drivers` — `IsVerified`/`IsActive` filtreleri İSTEMCİYE bırakılmış; varsayılan tüm sürücüler (doğrulanmamış/pasif, telefonlarıyla) dönüyor. Public uç `IsVerified=true && IsActive=true` zorlamalı; `drivers/{id}` de aynı.
  - **Places:** `GET /v1/places` + `{id}` — `IsActive` filtresi hiç yok (`QueryPlaceDto`'da alan bile yok); pasif mekanlar dönüyor. Public'e IsActive=true zorunlu.
  - **Pharmacies:** `GET /v1/pharmacies` — `IsActive` istemciye bırakılmış (varsayılan pasifler de döner); public uç true zorlamalı (`{id}` de).
  - **Transport:** `GET /v1/transport/intercity-routes` + `intracity-routes` — `IsActive` filtresi yok, pasif hatlar dönüyor. Public'e IsActive=true.
  - **Sayfalama clamp'i (DoS yüzeyi):** HİÇBİR liste query'sinde Page/Limit sınırı yok — `?limit=1000000` tüm tabloyu çeker (tüm public + admin listeler). Ortak çözüm: tek yerde clamp (ör. `PagedResult` yardımcı metodu veya her handler'da `Math.Clamp(limit, 1, 50)`; admin için üst sınır daha yüksek olabilir). Announcements listesinde sayfalama HİÇ yok — bkz. 10.8.
  - **Hedefli rate limit (spam):** `POST /v1/deaths`, `POST /v1/complaints` (anonim!), `POST /v1/ads`, track-phone/whatsapp yalnız global 300/dk/IP'ye tabi — pending kuyruğu doldurma / sayaç şişirme mümkün. Yazma uçlarına daha sıkı bir policy (ör. `public-write` 10-20/dk/IP) değerlendirilip karar not düşülmeli.
- **Doğrulama:** her modül için pending/pasif kayıt oluştur → public liste + detayda YOK (Ads 10.5 testleri emsal); `?status=pending`/`?isVerified=false` gibi istemci parametreleri public uçta etkisiz; `?limit=99999` istenince clamp'lenmiş sayıda satır; testlere görünürlük senaryoları eklenmeli.
- [x] **YAPILDI (17 Tem 2026):**
  - **Desen:** her sorguya opsiyonel görünürlük bayrağı eklendi (varsayılan `false` → admin/panel davranışı BİREBİR korunur; yalnız public controller'lar `true` geçer). Adlandırma modül semantiğine göre: Deaths/Events/Campaigns/Announcements `OnlyPublished`, Places/Pharmacies/Transport `OnlyActive`, Taxis `OnlyPublic` (verified+active).
  - **Deaths:** `GetDeathNoticesQuery(dto, OnlyPublished)` — public liste yalnız `approved`, istemcinin `?status=` parametresi public'te YOK SAYILIR. **KARAR:** archived public listeye DAHİL DEĞİL (`?includeArchived=` eklenmedi — arşiv görünümü istenirse ayrı karar). `GetDeathNoticeByIdQuery(id, OnlyPublished, RequesterId)` — approved olmayanı yalnız ekleyen görür (Ads detay emsali; RequesterId=CurrentUserId claim'den), diğerine 404; archived detay da sahibi-hariç 404 (kural: approved-değilse gizli).
  - **Events/Campaigns/Announcements detayları:** `GetEventByIdQuery(id, OnlyPublished)` → approved değilse 404; `GetCampaignByIdQuery(id, OnlyPublished)` → approved + `StartDate<=now<=EndDate` değilse 404 (liste `OnlyActive` kuralıyla tutarlı); `GetAnnouncementByIdQuery{Id, OnlyPublished}` → yalnız `active` + `VisibleUntil` null/gelecek. NOT: Announcements NOT_FOUND'u mevcut kontrat gereği `ApiResponse.FailureResponse` ile döner (HTTP 200 + success:false) — zarf tutarlılığı 10.13'ün konusu, davranış değiştirilmedi.
  - **Taxis:** public liste/detay `IsVerified && IsActive` ZORUNLU; istemcinin `?isVerified=`/`?isActive=` parametreleri public'te yok sayılır (admin API'de çalışmaya devam eder). **Pharmacies:** public liste/detay `IsActive` zorunlu, `?isActive=` public'te etkisiz; ⚠️ `GetPharmaciesQuery` cache'li olduğundan `OnlyActive` bayrağı **CacheKey'e eklendi** (`:pub{OnlyActive}`) — public/admin sonuçları aynı anahtarı paylaşamaz. **Places + Transport (intercity/intracity):** `IsActive` filtresi ilk kez eklendi, public'e zorunlu.
  - **Sayfalama clamp'i:** yeni `Common/Models/Pagination.cs` — `Pagination.Clamp(page, limit, maxLimit)`; `MaxLimit=50` (public), `AdminMaxLimit=200`. TÜM 16 sayfalı handler'a uygulandı (Ads, MyAds, MyFavorites, Deaths, Events, Campaigns, Complaints, Guide×2, Pharmacies, Places, Staff, Taxis, Transport×2, Users). **KARAR:** görünürlük bayrağı olan handler'larda public=50/admin=200; bayrağı olmayan paylaşımlı handler'larda (Events/Campaigns/Guide listeleri public'ten de çağrılıyor) tek sınır 200 — DoS yüzeyi yine kapalı, panel kırılmaz (paneldeki en büyük sabit limit 50). `PagedResult.CurrentPage/PageSize` clamp'lenmiş değerleri raporlar.
  - **Hedefli rate limit:** Program.cs'e `public-write` policy'si (config `RateLimiting:PublicWrite`, varsayılan **15/dk/IP**, fixed-window, IP partition — auth policy deseni). `[EnableRateLimiting("public-write")]` uygulanan uçlar: `POST /v1/deaths`, `POST /v1/complaints` (anonim), `POST /v1/ads`, `POST /v1/ads/{id}/track-phone` + `/track-whatsapp` (anonim sayaçlar), **EKSTRA: `POST /v1/files/upload`** (maliyetli uç — plandaki listede yoktu, disk doldurma yüzeyi). KARAR: favorite/extend/PUT-DELETE ads gibi hafif+authorize uçlar global 300/dk'da bırakıldı. appsettings.json'a PublicWrite bölümü; test factory'sinde 1000'e gevşetildi (RateLimitingTests'in düşük-limitli türev factory'si hariç).
  - **Testler:** yeni `Integration/Security/PublicVisibilityTests.cs` — 6 test: deaths listesi (approved döner, pending+archived dönmez, `?status=pending` etkisiz, `limit=99999→pageSize 50`); pending death detayı (anonim 404, sahibi 200); event+campaign detayı (pending 404, süresi geçmiş kampanya 404, aktifler 200); announcement (pending success:false NOT_FOUND, aktif success:true); taxi (doğrulanmamış/pasif listede+detayda yok, istemci parametresi etkisiz); pasif place/pharmacy/route (listede yok, detay 404). RateLimitingTests'e `PublicWriteEndpoint_ExcessRequests_ShouldReturn429` eklendi (türev factory'ye PublicWrite:3). **xUnit 43/43 yeşil.**
  - **Canlı doğrulama (17 Tem, curl :5005 + dev DB):** kullanıcı pending vefat ilanı gönderdi → public listede yok + `?status=pending` boş + `limit=99999→pageSize:50` + anonim detay 404 / sahip detay 200 / admin listesi pending'i görmeye devam etti; taksi/mekan/hat/etkinlik kayıtları psql ile geçici pasif/pending yapıldı → public liste+detayda kayboldular (404), sonra GERİ ALINDI (DB değişikliği kalmadı); `?isVerified=false`/`?isActive=false` public'te etkisiz; 16 ardışık anonim complaint POST'unda 16.sı **429 RATE_LIMITED** zarfıyla; auth 5/dk limiti de canlıda tetiklendi (beklenen). Test kalıntıları silindi (15 şikayet + 1 vefat ilanı); yalnız `faz107user` adlı test kullanıcısı DB'de kaldı (FK zinciri nedeniyle bilinçli bırakıldı — zararsız). API kapatıldı.

### 10.8 — ✅ TAMAMLANDI (17 Tem 2026) — Eksik public uçlar ve kontrat boşlukları (17 Tem 2026 denetim bulguları)
> **Tespit:** Masterclass kontratında olup hiç yazılmamış uçlar + mobil ekranları fiilen işlevsiz bırakan veri boşlukları.
- Yapılacaklar:
  - **`GET /v1/complaints/my` YOK:** Faz 6-G kaydı "`[Authorize]` GET my" yazıyor ama `ComplaintsController`'da HİÇBİR GET yok (kayıt/kod driftı) — mobil "şikayetlerim" ekranı için `[Authorize]` GET my (paged) yazılmalı.
  - **`DELETE /v1/files/{id}` YOK:** masterclass §13.2 kontratında var. Kullanıcı yanlış yüklediği görseli silemiyor; hiçbir kayda bağlanmamış yetim dosyalar sonsuza dek birikiyor. `[Authorize]` + yalnız `uploaded_by == user_id` + hiçbir kayıtta referanslı değilse (ya da soft delete). Yetim dosya temizliği için Hangfire job'ı da DEĞERLENDİRİLMELİ (karar not düşülür).
  - **Hesap silme (`DELETE /v1/users/me`) YOK:** masterclass'ta da yok ama **Apple App Store 5.1.1(v) ve Google Play politikası hesap oluşturan uygulamada hesap silmeyi ZORUNLU kılar** (KVKK açısından da gerekli) — mobil yayına bu uç olmadan çıkılamaz. Karar gerekli: hard delete mi soft+anonimleştirme mi (ilanları/favorileri/bildirimleri ne olur); refresh token'lar iptal edilmeli.
  - **Şehirlerarası saatler + şehir içi duraklar hiç yok:** `IntercitySchedule` ve `IntracityStop` entity'leri HİÇBİR katmanda kullanılmıyor (seed dahi yok) — `GET /v1/transport/intercity-routes` kalkış saati DÖNMÜYOR (mobilde "Adana otobüsü kaçta?" cevapsız), intracity yanıtında durak listesi yok. DTO'lara schedules/stops eklenmeli + admin CRUD (panel formu 10.9'da) + seed.
  - **Ads liste sıralama/arama eksikleri:** `GET /v1/ads` yalnız CreatedAt DESC — pazaryeri için `?sort=` (price_asc/price_desc/newest) şart; `search` yalnız başlıkta (description dahil edilmeli mi — karar; pg_trgm GIN indeksi başlık için zaten var).
  - **Announcements public listesi:** sayfalama/filtre HİÇ yok — `GetAnnouncementsQuery` tüm tabloyu dönüyor (duyuru sayısı büyüdükçe mobil açılış ekranını şişirir). Page/Limit + `?typeId=` eklenmeli. Ayrıca mahalle hedefleme KARARI: `targetNeighborhoodIds` DTO'da dönüyor ama sorgu filtrelemiyor — istemci mi filtreler (kontrata yazılır) yoksa `[Authorize]` isteklerde kullanıcının mahallesine göre sunucu mu filtreler? Karar 10.10 (bildirimler) ile tutarlı olmalı.
- **Doğrulama:** her yeni uç canlı curl + en az 1'er xUnit senaryosu; hesap silme akışında token iptali + verinin akıbeti DB'den doğrulanır; transport yanıtında saatler/duraklar seed verisiyle dolu döner.
- [x] **YAPILDI (17 Tem 2026):**
  - **`GET /v1/complaints/my`** (`GetMyComplaintsQuery`, `[Authorize]`, paged/clamp'li): yalnız `user_id` claim'ine ait kayıtlar; anonim gönderimler (user_id NULL) hiçbir listede görünmez; yanıt AdminNotes/ResolvedAt dahil (kullanıcı sonucu görebilsin).
  - **`DELETE /v1/files/{id}`** (`DeleteMyFileCommand`, `[Authorize]`): sahiplik (uploaded_by ≠ claim → 403); dosya HERHANGİ bir kayıtta referanslıysa 409 CONFLICT — `IsReferencedAsync` 13 referans alanını tarar (AdImage, DeathNotice.PhotoFileId, Announcement.Image/PdfFileId, Event.CoverImageId+EventImage, Campaign.CoverImageId+CampaignImage, Place.CoverImageId+PlaceImage, Business.LogoFileId, GuideItem.LogoFileId, TaxiDriver.License/RegistrationFileId, User.ProfilePhotoUrl==cdn_url — profil fotoğrafı id değil URL saklar, 10.3). **KARAR:** soft delete (files.deleted_at) + fiziksel dosya best-effort diskten silinir (hata isteği düşürmez). **KARAR:** yetim dosya Hangfire temizlik job'ı ŞİMDİLİK ERTELENDİ — kullanıcı artık kendi dosyasını silebiliyor; job, dosya hacmi sorun olursa eklenir (10.13 envanterinde hatırlanacak). **🐛 PLANDIŞI BUG FIX:** `LocalFileStorageService.DeleteFileAsync` göreli URL'de (`/uploads/...`) `Uri.LocalPath` ile fırlatıyordu — upload tarafının 7 Tem düzeltmesinin aynısı uygulandı (`Path.GetFileName`).
  - **`DELETE /v1/users/me`** (`DeleteMyAccountCommand`, `[Authorize]`, opsiyonel body `{refreshToken}`): **KARAR — SOFT DELETE + ANONİMLEŞTİRME** (hard değil; FK bütünlüğü + KVKK): Phone→`"del"+id'nin ilk 12 hex'i` (15 kolon sınırı, unique korunur, **telefon yeniden kayda açılır**), Username/Email/Age/FcmToken/ProfilePhotoUrl/PrimaryNeighborhoodId→NULL, IsActive=false, DeletedAt=now. İçerik: ilanlar toplu soft delete (`ExecuteUpdateAsync`), favori satırları silinir (`ExecuteDeleteAsync`); vefat ilanları + şikayetler kalır (topluluk/işletme kaydı). Token: body'deki refresh jti iptal listesine (logout deseni); kalan refresh'ler global soft-delete filtresi sayesinde "Kullanıcı bulunamadı" 401 alır; access token en fazla 1 gün yaşar (kabul edilen sınır, kontrata not). **KARAR:** yalnız `Role=User` silebilir — admin/staff 403 (panelden yönetilir).
  - **Transport saatler + duraklar** — `IntercitySchedule`/`IntracityStop` İLK KEZ kullanımda: `IntercityRouteResponseDto.Schedules` (`{id, departureTime:"HH:mm"}` — TimeSpan formatı SQL'e çevrilemediğinden 10.4 deseniyle bellek tarafında formatlanır, yalnız aktif seferler sıralı) + `IntracityRouteResponseDto.Stops` (`{id, stopName, stopOrder, timeFromStart}` sıralı). Admin CRUD (`ScheduleAndStopCommands.cs`): `POST/DELETE v1/admin/transport/intercity/{routeId}/schedules` + `intracity/{routeId}/stops` — saat "HH:mm" değilse 400, hat yoksa 404, aynı hat+saat / aynı hat+sıra 409; silme hard delete (lookup verisi). MockDataSeeder'a idempotent saat/durak seed'i (hatlar dolu dev DB'de de çalışır — mevcut hatları DB'den okur). Panel formu 10.9(a-benzeri) kapsamında.
  - **Ads sıralama + arama:** `QueryAdDto.Sort` — whitelist `newest`(varsayılan)/`oldest`/`price_asc`/`price_desc`, bilinmeyen değer 400 VALIDATION_ERROR (MyAds status whitelist emsali); price sıralamalarında `ThenByDescending(CreatedAt)` eşitlik kırıcı. **KARAR:** arama artık başlık + AÇIKLAMADA (pazaryeri beklentisi; pg_trgm GIN indeksi yalnız başlıkta — tablo büyürse description indeksi eklenmeli, 10.13 notu).
  - **Announcements sayfalama + tür filtresi:** `GetAnnouncementsQuery` `ApiResponse<List<>>` → **`PagedResult<AnnouncementDto>`'ya geçirildi** (tercih edilen zarf deseni — 10.3 kararı; handler sade döner, filter sarar) + `Page/Limit` (clamp: public 50 / admin 200) + `TypeId`. Public `GET /v1/announcements?typeId=&page=&limit=`; admin API aynı parametrelerle; **Web paneli** `Limit=200` ile `result.Items.ToList()`'e uyarlandı (panel sayfalama UI'ı yok — duyuru 200'ü aşarsa 10.9'a iş çıkar, not). ⚠️ Kontrat değişikliği: `/v1/announcements` data'sı artık liste değil `{items, totalCount, ...}` (10.13 API_CONTRACT'a girecek). **KARAR (mahalle hedefleme):** `targetNeighborhoodIds` DTO'da dönmeye devam eder, sunucu taraflı filtreleme KARARI 10.10'A ERTELENDİ (bildirim hedeflemesiyle aynı mantık tek yerde kurulmalı; o zamana dek kural "istemci filtreler").
  - **Testler:** yeni `Integration/MissingEndpoints/MissingEndpointsTests.cs` — 6 test: complaints/my (401 + kullanıcı izolasyonu), files delete (403/200/404 + deleted_at + ilana bağlı dosya 409), hesap silme (anonimleştirme alanları + ilan soft delete + refresh 401 + admin 403 + ikinci silme 404), transport CRUD (200/409/400/404 + public DTO "HH:mm"/durak + silme sonrası boş), ads sort (asc/desc + bozuk 400 + description araması), announcements (limit=1 sayfalama + typeId + clamp 50). DİKKAT: entity konfigürasyonlarında `HasQueryFilter(DeletedAt == null)` global filtreleri VAR (File/Ad/User/Death/Campaign/Event/Announcement/TaxiDriver) — testte soft-silinmiş satırı okumak için `IgnoreQueryFilters()` şart. **xUnit 49/49 yeşil.**
  - **Canlı doğrulama (17 Tem, curl :5005 + dev DB):** complaints/my anonim 401 / sahibi 1 kayıt; upload→başkası DELETE 403→sahibi 200 (deleted_at dolu + fiziksel dosya silindi)→tekrar 404; transport: saat ekleme 200, aynı saat 409, bozuk saat 400, aynı durak sırası 409, public yanıtta `Adana → ['07:00']` + duraklar, DELETE'ler 200 sonrası public boş; ads price_asc/desc doğru sıralı, bozuk sort 400, search çalışır; announcements limit=1 → totalCount:3/totalPages:3, typeId filtresi + olmayan tür 0; hesap silme: yeni kullanıcı+ilan → DELETE me 200 → DB `phone=del..., username=NULL, active=false` + ilan soft-deleted + iptal edilen refresh 401 + silinmiş hesap 404 + admin 403. Test kalıntıları temizlendi (şikayet, ilan, anonim kullanıcı, dosya, saat/durak kayıtları — saat/durak tabloları boş bırakıldı ki panel "Test Verileriyle Doldur" butonu tam seed edebilsin).

> ✅ **10.1-10.8 DENETİMİ (18 Tem 2026):** Bölümün tamamı kodla madde madde karşılaştırıldı — kayıtlar kodla uyumlu, xUnit 51/51 yeşil, derleme 0 uyarı. Bulunan tek tutarsızlık düzeltildi: **süresi geçmiş ama ExpireAdsJob'un (saatlik) henüz expired'a çevirmediği ilan** public listeden düşüyor ve favorilerde `isAvailable:false` oluyordu ama detay/track-phone/track-whatsapp/favorite-ekleme yalnız `Status=="approved"` baktığından ≤1 saatlik pencerede sızıyordu. `GetAdByIdQueryHandler` + `TrackAdContactCommand` + `AddAdFavoriteCommand` filtrelerine `ExpiresAt > now` eklendi (sahip detayı görmeye devam eder); yeni test `DateExpiredButStillApprovedAd_IsHiddenFromDetailTrackAndFavorite` (AdsMobilePart2Tests). **xUnit 52/52 yeşil.** Kayıtlı iki bilinçli açık aynen duruyor (10.13'te karar): Announcements NOT_FOUND'un 200+success:false zarfı, Ads FluentValidation ölü validator'ları.

### 10.9 — ⭐ ÖZELLİK EKLEMELERİ: Admin panel tamamlama + yönetim boşlukları (17 Tem 2026 denetim bulguları) — ✅ TAMAMEN TAMAM (19 Tem 2026: Bölüm 1 a+f+h, Bölüm 2 b, Bölüm 3 c+i-çekirdek+d-API+g-API, Bölüm 4 d-panel, Bölüm 5 e+g-panel+i-kapsam — TÜM alt maddeler a-i kapandı)
> **Tespit:** Mobil uçlar geliştikçe panelin yönetemediği alanlar birikti — bazı modüller "API'de var, panelde yönetilemez" durumda; bazı veriler ise YALNIZ seed'den geliyor, admin hiçbir araçla ekleyemiyor/değiştiremiyor. Bu başlık, denetimde gerekli görülen özellik eklemelerinin toplandığı ana başlıktır (kullanıcı talebiyle tek başlıkta toplandı). Muhtemelen 2-3 oturuma bölünmesi gerekir — bölünürse alt maddeler a/b/c olarak işaretlenip ayrı ayrı kapatılmalı.
- Yapılacaklar:
  - **(a) Nöbetçi eczane takvimi paneli — EN ACİL:** 10.4'te nöbet CRUD'u Admin API'ye yazıldı ama **Web panelde nöbet yönetimi UI'ı YOK** — mobilin 1 numaralı özelliği olan nöbet verisi panelden GİRİLEMİYOR (yalnız curl ile girilebilir!). PharmaciesAdmin'e aylık takvim görünümü + gün seçip eczane atama/silme formu (mevcut `CreatePharmacyScheduleCommand`/`DeletePharmacyScheduleCommand`/`GetPharmacyScheduleQuery` zaten hazır — yalnız controller action + view işi).
  - **(b) İşletme (Business) yönetimi — kampanya modülünün ön koşulu:** `Business`/`BusinessCategory` CRUD'u HİÇBİR katmanda yok (Application command/query, admin API, panel UI — hiçbiri); kampanya oluştururken `ViewBag.Businesses` MockDataSeeder'ın 2 sahte işletmesine mahkûm — **kampanya modülü gerçek işletmelerle fiilen kullanılamaz**. Application CRUD + `v1/admin/businesses` + panel UI (sidebar'a "İşletmeler") + kampanya formuna entegrasyon.
  - **(c) İlan kategori & özellik yönetimi:** `AdCategory` (alt kategoriler dahil), `CategoryProperty`, `PropertyOption` CRUD'u hiçbir katmanda yok — kategori ağacı ve form alanları YALNIZ DbSeeder'dan geliyor; admin yeni kategori/özellik ekleyemez, mevcut ada dokunamaz. Application CRUD + admin API + panel UI. DİKKAT: `ads-lookup` cache grubuna `ICacheInvalidator` eklenmeli (10.5 notu); kategori silme, ilanı/alt kategorisi/property'si olanlarda engellenmiş olmalı (DeleteGuideCategory 409 emsali).
  - **(d) Lookup tablolarının yönetimi:** Mahalle (`Neighborhood`), mezarlık (`Cemetery`), cami (`Mosque`), etkinlik/mekan kategorileri (`EventCategory`/`PlaceCategory`) — hiçbirinin CRUD'u yok, yalnız seed. En azından Create/Update(+IsActive) admin API + basit panel UI'ları (AnnouncementTypes modal deseni yeterli). `Lookups`/`ads-lookup` cache gruplarına invalidator (10.4 notundaki şart).
  - **(e) Personel (Staff) yönetimi paneli:** Faz 8'de `v1/admin/staff` API yazıldı (CRUD+izinler+şifre sıfırlama) ama **panel UI'ı yok** — personel yalnız curl'le yönetilebiliyor. UsersAdmin desenine uygun StaffAdmin controller+view'lar (izin matrisi düzenleme dahil).
  - **(f) Panel şifre değiştirme:** `AccountController`'da yalnız Login/Logout var — admin kendi şifresini panelden DEĞİŞTİREMİYOR (seed'deki Admin123! ile yaşıyor). "Şifremi değiştir" sayfası (mevcut change-password Application command'i varsa ona, yoksa yeni command'e bağlanır).
  - **(g) İlan moderasyonunda property görünürlüğü:** kullanıcı ilanları artık kategoriye özel alanlarla geliyor (10.5) ama `AdsAdmin` Create/Edit/Index view'ları property değerlerini HİÇ göstermiyor (`GetAdByIdForEditQuery`'de de yok) — admin "Yakıt Tipi/Model Yılı"nı göremeden onay veriyor. En azından Edit/detayda salt-okunur property listesi (düzenleme UI'ı opsiyonel — karar).
  - **(h) Panel inline yazma driftleri (Faz 9.4 kural ihlalleri):** `UsersAdminController.Ban/Unban` inline `IsBanned` yazıyor — `SetUserBanCommand` DURURKEN; BanReason/BannedAt/BannedBy hiç SET EDİLMİYOR (satır ~117-145). `TransportAdminController` IntracityRoute silmeyi inline + Remove (hard delete) yapıyor (satır ~39-43) — command'e taşınmalı. İkisi de mevcut command'lere bağlanarak kapatılır.
  - **(i) AuditLog hâlâ ölü:** entity + tablo var, HİÇBİR admin aksiyonu iz bırakmıyor (masterclass audit_log öngörür). Approve/reject/ban/delete command'lerine audit yazımı (MediatR behavior ile merkezî çözüm değerlendirilebilir) — kapsam kararı bu maddede verilip not düşülmeli.
- **Doğrulama:** her UI panelden canlı kullanılır (nöbet gir → mobil on-duty ucu taze döner; işletme ekle → kampanya formunda görünür; kategori ekle → mobil kategori ucu cache invalidation sonrası döner); inline drift düzeltmeleri DB kolonlarıyla (ban_reason/banned_at dolu) doğrulanır.
- [x] **YAPILDI — BÖLÜM 1 (18 Tem 2026): (a) + (f) + (h) kapatıldı.** Kalan alt maddeler (b işletme, c ilan kategori/özellik, d lookup CRUD'ları, e Staff paneli, g moderasyonda property görünürlüğü, i AuditLog) sonraki 10.9 oturumlarına.
  - **(a) Nöbet takvimi paneli:** `PharmaciesAdminController`'a 3 action — `Schedule(year?, month?, date?)` (GET; `GetPharmacyScheduleQuery` + atama dropdown'u için yalnız AKTİF eczaneler + `GetOnDutyPharmaciesQuery`), `ScheduleCreate` (POST; `CreatePharmacyScheduleCommand`, `Source:"panel"`, saatler boşsa entity varsayılanı 19:00–09:00; `AppException/ConflictException` → TempData hata banner'ı), `ScheduleDelete` (POST; yıl/ay korunarak geri döner). Yeni `Views/PharmaciesAdmin/Schedule.cshtml` — **EventsAdmin/Calendar deseni birebir** (Pazartesi başlangıçlı server-rendered grid, ay navigasyonu, lejant): gün hücrelerinde atanmış eczane chip'leri (üzerinde × ile tek tık silme, confirm'li), "+" günü üstteki forma JS ile doldurur (JS kapalıysa aynı link server-side ön-dolumla çalışır — `?date=` parametresi). **PLANDIŞI EKLER:** (1) sayfa üstünde "Bugün nöbetçi" banner'ı — bugün atama yoksa kırmızı "mobilde nöbetçi görünmeyecek!" uyarısı (operasyonel boşluk anında görülür); (2) bugün+sonrası atanmamış günler takvimde amber "Nöbetçi yok" ile işaretli (görsel boşluk taraması). Index'e "Nöbet Takvimi" butonu.
  - **(f) Panel şifre değiştirme:** yeni `ChangeMyPasswordCommand(UserId, CurrentPassword, NewPassword)` (`Features/Users/Commands/ChangeMyPassword/`) — mevcut şifre `IPasswordHasher.VerifyPassword` ile doğrulanır (hatalıysa `INVALID_PASSWORD`), min 6 karakter + eskisiyle aynı olamaz, `Password==null || Role==User` hesaplara `VALIDATION_ERROR` (OTP kullanıcısının şifresi yok). `AccountController`'a `ChangePassword` GET/POST (`[Authorize(Roles="admin,super_admin,moderator")]` + `[ValidateAntiForgeryToken]`; tekrar alanı uyuşmazlığı controller'da) + `Views/Account/ChangePassword.cshtml`; sidebar alt bölümüne "Şifremi Değiştir" linki. **PLANDIŞI:** header'da artık giriş yapan kullanıcının adı görünüyor (`User.Identity.Name`).
  - **(h) Inline drift düzeltmeleri:** `UsersAdminController.Ban/Unban` inline `IsBanned` yazımı → `SetUserBanCommand` (BanReason/BannedAt/BannedBy artık dolu; admin id claim'den). **PLANDIŞI UI:** ban butonu artık JS'siz `<details>` popover'ıyla **ban sebebi soruyor** (opsiyonel, maxlength 500); listede "Yasaklı" rozetinde sebep+tarih tooltip'i ve altında kırmızı sebep satırı. `TransportAdminController.Delete` inline hard `Remove` → yeni `DeleteIntracityRouteCommand` (Application; IntracityRoute soft-delete DEĞİL — bilinçli hard delete, duraklar FK cascade; transport sorguları cache'siz olduğundan invalidator gerekmedi). Yeni POST'lara `[ValidateAntiForgeryToken]` eklendi.
  - **Doğrulama (18 Tem canlı, Web :5203 + Api :5005 + psql):** xUnit **49/49**; nöbet: panelden bugüne atama → `GET /v1/pharmacies/on-duty` TAZE döndü (cache invalidation kanıtlandı), aynı eczane+gün ikinci atama → "zaten nöbet kaydı var" banner'ı (409 yutulmadan mesajlandı), × ile silme → on-duty anında boş; ban: `faz107user` sebeple banlandı → DB `ban_reason` dolu + `banned_by`=gerçek admin id + listede sebep görünür → unban üç kolonu NULL'ladı; şifre: yanlış mevcut şifre "Mevcut şifreniz hatalı", uyuşmayan tekrar reddi, gerçek değişim sonrası eski şifre 401-benzeri reddedildi yeni şifre girdi (sonra seed şifresi `Admin123!`'e GERİ DÖNÜLDÜ); transport: T99 test hattı command üzerinden silindi (DB 0 satır). Test kalıntıları temizlendi (nöbet kaydı, T99; yalnız 3 Tem mock nöbeti duruyor — MockDataSeeder verisi, dokunulmadı).
- [x] **YAPILDI — BÖLÜM 2 (18 Tem 2026): (b) İşletme/Business yönetimi kapatıldı** — kampanya modülü artık gerçek işletmelerle kullanılabilir.
  - **Application (`Features/Businesses/`):** `BusinessDtos.cs` (Response/Query/Create/Update + BusinessCategoryDto; Response'ta CategoryName, LogoUrl [`x.LogoFile.CdnUrl` navigation — Campaigns deseni], **CampaignCount** [projeksiyondaki `x.Campaigns.Count` Campaign'in global soft-delete filtresine tabidir → yalnız aktif kampanyalar]); `BusinessQueries.cs` — `GetBusinessesQuery` (arama/kategori/isVerified filtreli, `Pagination.Clamp` AdminMaxLimit, **cache'siz** — admin verisi), `GetBusinessByIdQuery`, `GetBusinessCategoriesQuery`; `BusinessCommands.cs` — Create/Update (`BusinessRules.ValidateAsync` paylaşımlı: ad ≥2 karakter + kategori var; InstagramHandle `TrimStart('@')`), `DeleteBusinessCommand` — **KARAR: Business soft-delete DEĞİL + campaigns FK'sı DB'de CASCADE → silme kampanya geçmişini fiziksel yok eder; bu yüzden `IgnoreQueryFilters` ile DB seviyesinde HERHANGİ bir kampanyası olan işletme 409** (soft-silinmiş kampanya da engeller — bilinçli), `SetBusinessVerificationCommand(Id, Verified, AdminId)` (VerifyTaxiDriver emsali; geri alma izleri NULL'lar), `CreateBusinessCategoryCommand` (aynı ad 409, Slugify CreateAnnouncementType emsali).
  - **Admin API:** yeni `BusinessesAdminController` — `v1/admin/businesses` GET/POST + `{id}` GET/PUT/DELETE + `{id}/verify|unverify` POST + `categories` GET/POST; `[RequirePermission("businesses", ...)]` (permission sistemi dinamik string — admin/super_admin otomatik geçer, moderatöre AdminPermission satırı gerekir).
  - **Panel:** yeni `BusinessesAdminController`(Web) + `Views/BusinessesAdmin/Index|Create|Edit.cshtml` — Index: logo küçük resmi, kategori filtresi, kampanya sayısı rozeti, Doğrula/Doğrulamayı Kaldır/Düzenle/Sil + sayfalama; Create/Edit: logo upload (`UploadHelper`, "business" modülü, multipart) + **hızlı kategori ekleme `<details>` formu** (ana formun DIŞINDA — iç içe form HTML'de geçersiz; `returnUrl` ile geri döner). Sidebar'a "İşletmeler" (Kampanyalar'ın altı). **Kampanya entegrasyonu:** `CampaignsAdminController.LoadBusinessesAsync` inline `_uow` sorgusundan `GetBusinessesQuery`'ye geçirildi (Faz 9.4 kuralı; DTO da `.Id`/`.BusinessName` taşıdığından view değişmedi) + kampanya formuna "Aradığınız işletme yok mu? Yeni işletme ekleyin" linki.
  - **Doğrulama (18 Tem canlı + test):** xUnit **51/51** (yeni `BusinessesAdminTests` 2 test: 401 + tam CRUD akışı — kategori 409, geçersiz kategori 400, `@` soyulması, verify alanları, kampanyalı silme 409 → kampanya kalkınca 200 hard delete); canlı panel: "Fırın ve Pastane" kategorisi hızlı-ekleme ile eklendi → dropdown'a düştü → "Kadirli Unlu Mamuller" oluşturuldu (instagram `@kadirliunlu`→`kadirliunlu` DB'de doğrulandı) → Verify → `is_verified/verified_by/verified_at` dolu → **kampanya formunda göründü** → kampanyalı mock işletme silinemedi ("kampanya kaydı var — silinemez" banner'ı, satır duruyor) → kampanyasız test işletmesi silindi. Test kalıntıları temizlendi (test işletmesi + kategorisi; mock işletmelere dokunulmadı). ⚠️ curl notu: `-F "alan=@değer"` curl'de dosya upload'a dönüşür — form testlerinde `--form-string` kullan.
- [x] **YAPILDI — BÖLÜM 3 (18 Tem 2026, 2. oturum): (c) İlan kategori & özellik yönetimi kapatıldı** — kategori ağacı ve form alanları artık yalnız seed'e mahkûm değil. Oturum kullanıcı talebiyle (c) sonrası KESİLDİ; (i)'nin çekirdeği ile (d) ve (g)'nin API katmanları bu oturumda YAN ÜRÜN olarak bitti, panel UI'ları sonraki oturuma kaldı (aşağıda "KALAN İŞLER").
  - **(c) Application (`Features/Ads/Commands/AdCategoryCommands.cs`):** Create/Update/Delete `AdCategory` + Create/Update/Delete `CategoryProperty` + Create/Delete `PropertyOption` — hepsi ortak `AdsLookupInvalidatorBase` tabanıyla **`ICacheInvalidator(ads-lookup)`** (10.5 notundaki ŞART kapandı) ve `IAuditableCommand`. **KARARLAR:** (1) Update'te ParentId değiştirilemez — ağaçta taşıma yok (döngü riski + ilanların ağacı kayar); (2) rename slug'ı yeniden üretir, slug unique → çakışma 409 (`AdCategoryRules.ValidateNameAsync` Create/Update paylaşımlı); (3) PropertyType sonradan DEĞİŞTİRİLEMEZ (mevcut ilan değerleri anlamsızlaşır); (4) option güncelleme yok — sil+ekle yeterli (AdPropertyValue seçeneğe FK ile değil DEĞER KOPYASIYLA bağlı, option silmek mevcut ilanları bozmaz); (5) kategori silme 409 kuralları: alt kategorisi VAR ∥ ilanı VAR (IgnoreQueryFilters — soft-silinmiş ilan da FK ile işaret eder) ∥ özellik tanımı VAR (properties FK'sı CASCADE ama form tanımları sessizce yok olmasın); (6) özellik silme: değer girilmiş ilan varsa 409 (ad_property_values FK RESTRICT); (7) Select/MultiSelect en az 1 seçenekle oluşturulur, aksi 400. Slugify `Common/Utils/SlugHelper`'a ORTAKLAŞTI (BusinessRules artık delegate).
  - **(c) Query'ler (`AdCategoryAdminQueries.cs`):** `GetAdCategoriesAdminQuery` (pasifler DAHİL düz liste + ParentName + alt kategori/özellik/ilan sayaçları; ilan sayacı IgnoreQueryFilters — silme kuralıyla aynı ölçüt, admin "neden silemiyorum"u görür) ve `GetCategoryPropertiesAdminQuery` (options + kaç ilanda kullanıldığı) — CACHE'SİZ (admin verisi, GetBusinessesQuery emsali). + `GetAdPropertyValuesQuery` (g'nin hazırlığı — aşağıda).
  - **(c) Admin API:** yeni `AdCategoriesAdminController` `v1/admin/ads/categories` — GET/POST + `{id}` PUT/DELETE + `{id}/properties` GET/POST + `properties/{propertyId}` PUT/DELETE + `properties/{propertyId}/options` POST + `options/{optionId}` DELETE; `[RequirePermission("ads", ...)]`. "categories" LİTERAL segmenti `v1/admin/ads/{id}`'den önceliklidir (ASP.NET route precedence) — çakışma yok.
  - **(c) Panel:** yeni Web `AdCategoriesAdminController` + `Views/AdCategoriesAdmin/` — Index (kök+alt girintili ağaç tablosu, `_CategoryRow` partial; özellik/ilan sayaçları linkli), Create (üst kategori dropdown'u — yalnız kökler seçilebilir), Edit (slug önizleme + "N ilan var, pasife almak yeni ilanı engeller" uyarısı), Properties (`<details>` yeni-özellik formu — Select seçilince virgüllü seçenek alanı açılır; satır içi `<details>` düzenleme popover'ı — UsersAdmin ban deseni; seçenek chip'leri × silme + inline "+ Ekle" mini formu). AdsAdmin Index başlığına "Kategoriler" butonu (GuideAdmin Categories deseni — sidebar'a girilmedi). **PLANDIŞI drift düzeltmesi:** `AdsAdmin.LoadCategoriesAsync` inline `_uow` sorgusundan `GetAdCategoriesAdminQuery`'ye geçirildi (Faz 9.4 kuralı; view `category.Id/.Name` okuduğundan kırılmadı).
  - **(i) YAN ÜRÜN — AuditLog ÇEKİRDEĞİ CANLANDI (kalan: kapsam genişletme):** `Common/Auditing/AuditContracts.cs` (`IAuditableCommand`: AuditModule/AuditAction/AffectedId/AffectedType/AuditDetails; `IAuditContext`: aktör UserId+IP+UserAgent) + `Common/Behaviors/AuditBehavior` MediatR pipeline'a eklendi (Caching→CacheInvalidation→Audit sırası). **KARARLAR:** merkezî behavior (handler'lara kod girmez); audit yazım hatası isteği DÜŞÜRMEZ (iş zaten yapıldı — CacheInvalidation emsali, loglanır); bool-false yanıt (kayıt yok) iz bırakmaz; Guid dönen create'lerde AffectedId YANITTAN alınır; komutun tamamı ASLA serialize edilmez (şifre riski) — yalnız komutun bilinçli verdiği küçük `AuditDetails` nesnesi jsonb `details`'a yazılır (reset-password bilinçli detaysız); HTTP dışı bağlam (Hangfire) UserId null → iz yok. Host implementasyonları: Api `Services/HttpAuditContext` (`user_id` claim) + Web `Common/HttpAuditContext` (`NameIdentifier`) — ikisi de DI'da. **Marker uygulanan komutlar:** ApproveAd/RejectAd/DeleteAd, SetUserBan (ban/unban + reason detayı), Staff×5 (Create/Update/Delete/SetPermissions/ResetPassword), DeleteBusiness, SetBusinessVerification + TÜM yeni AdCategory/Lookup komutları. **KAPSAM KARARI:** onay/ret/ban/silme/personel/doğrulama audit'lenir; salt içerik Update'leri bilinçli DIŞARIDA (gürültü); deaths/events/campaigns/complaints onay komutlarına marker eklemek KALAN İŞ (mekanik — aynı desen).
  - **(d) YAN ÜRÜN — Application+API tamam, PANEL UI YOK:** `Features/Lookups/LookupCommands.cs` — Neighborhood/Cemetery/Mosque/EventCategory/PlaceCategory Create+Update (ortak `LookupsInvalidatorBase` → **`ICacheInvalidator(lookups)`**, 10.4 notundaki ŞART kapandı; `LookupRules` paylaşımlı ad/slug benzersizliği — `EF.Property<string>` ile genel). **KARAR: lookup DELETE YOK** — hepsi FK ile referanslanan sözlük verisi (vefat→mezarlık/cami, kullanıcı→mahalle, etkinlik/mekan→kategori); mahalle `IsActive` ile pasifleşir. + `LookupAdminQueries.cs` (`GetNeighborhoodsAdminQuery` pasifler dahil + birincil mahalle SAKİN sayısı; `GetPlaceCategoriesAdminQuery` + mekan sayısı — cache'siz) + yeni `LookupsAdminController` `v1/admin/lookups/{neighborhoods|cemeteries|mosques|event-categories|place-categories}` GET/POST/PUT. ⚠️ `[RequirePermission("lookups", ...)]` — YENİ modül adı: moderatöre lookup yetkisi verilecekse AdminPermission satırında Module="lookups" kullanılmalı.
  - **(g) YAN ÜRÜN — API yarısı hazır, PANEL GÖSTERİMİ YOK:** `GetAdPropertyValuesQuery(AdId)` (PropertyName+PropertyType+Value, DisplayOrder sıralı) + Admin API `GET v1/admin/ads/{id}/properties`. Kalan: AdsAdmin Edit view'ında salt-okunur bölüm olarak gösterim.
  - **Doğrulama (18 Tem canlı + test):** xUnit **54/54** (yeni `AdCategoryAdminTests` 2 test: 401 + tam akış — slug çakışması 409, alt kategorili silme 409, Select seçeneksiz 400, mükerrer özellik adı 409, public uçtan CACHE INVALIDATION assert'ü, audit satırı DB'den assert). Canlı (Api :5005 + Web :5203): public `/v1/ads/categories` ısıtıldı → admin API'yle kategori eklendi → public liste 8→9 TAZE döndü (TTL beklenmedi; redis'te `cache-group:ads-lookup` gözlendi) → Select özellik + 2 seçenek → public properties ucunda göründü → özellikli kategori silme 409 → panel login → kategori listesi/özellik sayfası 200 (Türkçe içerik HTML-encoded — grep'te `&#x131;` aranır) → panel POST'larıyla (antiforgery) özellik+kategori silindi → public 8'e döndü + DB 0 satır. `audit_logs`: API aktörüyle create-category/create-property (gerçek admin id + ip `::1` + jsonb details), panel aktörüyle delete-property/delete-category (user_agent dolu) — **iki HttpAuditContext de canlıda kanıtlandı**. Test kalıntıları temizlendi (kategori+özellik zaten akışta silindi; canlı smoke'un 4 audit satırı da silindi — audit_logs 0'dan başlar).
  - ~~**📌 KALAN İŞLER (sonraki 10.9 oturumu/oturumları):** (d-panel) LookupsAdmin panel UI; (e) Staff panel UI; (g-panel) AdsAdmin Edit'te salt-okunur özellikler; (i-kapsam) onay-ret komutlarına marker~~ — TAMAMI 19 Tem'de kapandı (d-panel: Bölüm 4, e+g-panel+i-kapsam: Bölüm 5 aşağıda). Karar netleşti: panelde audit log görüntüleme sayfası ŞİMDİLİK YOK — psql/Seq'ten okunur (ihtiyaç doğarsa ayrı iş).
- [x] **YAPILDI — BÖLÜM 4 (19 Tem 2026): (d-panel) LookupsAdmin panel UI kapatıldı** — lookup sözlükleri (mahalle/mezarlık/cami/etkinlik+mekan kategorisi) artık panelden yönetiliyor; (d) alt maddesi TAMAMEN kapandı.
  - **Panel:** yeni Web `LookupsAdminController` (`[Authorize(Roles="admin,super_admin")]`) — tek `Index(open?)` GET + 10 POST action (Neighborhood/Cemetery/Mosque/EventCategory/PlaceCategory × Create/Update; hepsi `[ValidateAntiForgeryToken]`, `AppException` → TempData banner). Silme aksiyonu bilinçli YOK (Application kararına uygun — FK'lı sözlük verisi). Yeni `Models/LookupsIndexViewModel` (5 liste + OpenSection) — Index sorguları: `GetNeighborhoodsAdminQuery`/`GetPlaceCategoriesAdminQuery` (admin, pasifler+sayaçlar) + mezarlık/cami/etkinlik kategorisi için public query'ler (filtresizler; command'ler lookups grubunu invalidate ettiğinden panel bayat cache görmez — LookupAdminQueries'teki karara uygun).
  - **View (`Views/LookupsAdmin/Index.cshtml`):** planlandığı gibi tek sayfa + 5 `<details>` akordiyon bölümü (başlıkta sayı rozeti; mahallede ayrıca "N pasif") — her bölümde üstte satır-içi ekleme formu + tablo; satırlarda `<details>` düzenleme popover'ı (UsersAdmin ban / AdCategoriesAdmin deseni, JS'siz). **POST-redirect sonrası ilgili bölüm açık kalır:** her POST `RedirectToAction(Index, new { open = "<bölüm>" })` → view `open` attribute'unu yalnız o bölüme basar. Mahalle satırında sakin sayısı + Aktif/Pasif rozeti (pasif satır soluk); popover'da sakini olan mahalleye pasife alma etki uyarısı. Mahalle tip select'i (—/Merkez/Belde/Köy; boş → NULL, `LookupRules.Clean`). Ad değişiminde "slug yeniden üretilir" notu. Sidebar'a "Tanımlar" linki (fa-tags, Şikayetler'in altı).
  - **Doğrulama (19 Tem canlı + test):** xUnit **54/54** (yeni test yok — Application/API 18 Tem'de test edilmişti, bu oturum salt panel işi canlı smoke ile). Canlı (Api :5005 + Web :5203): public `/v1/neighborhoods` ısıtıldı (10) → panelden mahalle eklendi → public 11 TAZE (invalidation TTL beklemeden) → popover formuyla pasife alındı → public 10'a düştü + sayfada "Pasif" rozeti; mükerrer mahalle adı → "Bu adla bir mahalle kaydı zaten var." banner'ı; mezarlık + etkinlik kategorisi eklendi → public uçlarda taze göründü (slug: "Smoke Etkinlik Türü" → `smoke-etkinlik-turu`); mekan kategorisi create+update (ad/ikon/sıra değişimi + slug yenilendi DB'den doğrulandı). `audit_logs`: 6 satır — tüm create/update'ler panel aktörüyle (user_id + user_agent dolu, jsonb details). Test kalıntıları temizlendi (4 smoke satırı psql'le silindi + audit_logs sıfırlandı + lookups cache anahtarları flush edildi — public uç 10'a döndü). ⚠️ Antiforgery öğrenimi tekrar doğrulandı: token, POST'tan hemen önceki sayfa GET'i `-b` VE `-c` ile yapılarak alınmalı (yalnız `-b` ile alınan token 400 verdi).
- [x] **YAPILDI — BÖLÜM 5 (19 Tem 2026, 2. oturum): (e) Staff paneli + (g-panel) moderasyonda özellik gösterimi + (i-kapsam) audit genişletme kapatıldı → 10.9 BAŞLIĞI TAMAMEN BİTTİ.**
  - **(e) Staff panel UI:** yeni Web `StaffAdminController` (`[Authorize(Roles="admin,super_admin")]`) + `Views/StaffAdmin/Index|Create|Edit.cshtml` + paylaşımlı **`_PermissionMatrix.cshtml`** partial — Faz 8'deki Application/API katmanı (GetStaff/GetStaffById/Create/Update/SetPermissions/ResetPassword/Delete) olduğu gibi kullanıldı, yeni command YOK. **İzin matrisi:** 16 modül × read/create/update/delete/approve checkbox tablosu; modül listesi `StaffAdminController.Modules` sabitinde ve **API'deki `[RequirePermission]` adlarıyla birebir** ("lookups" dahil — plan şartı); indexed model binding (`permissions[i].Module` hidden + `CanX` checkbox), **hiçbir kutusu işaretli olmayan modül satırı kaydedilmez** (`CleanPermissions` filtresi — canlıda boş "events" satırının elendiği kanıtlandı). Index: rol rozetleri, moderatörlerde izinli modül chip'leri (tooltip'te eylemler) + **izinsiz moderatöre amber "İzin yok!" uyarısı**; super_admin satırı "Korumalı" (Edit/Delete gösterilmez — command'ler de zaten reddeder). Edit: bilgiler + izin matrisi AYRI formlar (SetStaffPermissions "komple değiştir" semantiği korunur) + şifre sıfırlama bölümü ("loglanmaz" notu — command'in AuditDetails'i bilinçli null). Sidebar'a "Personel" (fa-user-shield, Tanımlar'ın altı). Create formunda rol seçenekleri açıklamalı (moderatör=matrise tabi / admin=hepsi); telefon Edit'te değiştirilemez (komutta alan yok — bilinçli).
  - **(g-panel):** `AdsAdmin.Edit` GET (+ POST'un invalid-model dönüşü) artık `ViewBag.PropertyValues = GetAdPropertyValuesQuery(id)` dolduruyor; `Edit.cshtml`'e form kartının ALTINA ayrı "Kategori Özellikleri (salt-okunur)" kartı — dl/dt/dd listesi, Boolean değerler Evet/Hayır'a çevrilir, değer yoksa bilgi mesajı. Düzenleme UI'ı bilinçli YOK (10.9(g) kararı: kullanıcı düzenlemesi mobilden gelir ve yeniden moderasyona düşürür — panel yalnız görür).
  - **(i-kapsam):** `IAuditableCommand` marker'ı 9 komuta eklendi (mekanik — davranış/handler değişikliği yok): ApproveDeathNotice + DeleteDeathNotice (deaths/approve+delete), ApproveEvent + RejectEvent + DeleteEvent (events), ApproveCampaign + RejectCampaign (reason varsa details'a) + DeleteCampaign (campaigns), ResolveComplaint (complaints/resolve, details `{status}` — **AdminNotes bilinçli details DIŞINDA**, serbest metin gürültü olmasın). Kapsam kararı korundu: salt içerik Update'leri hâlâ audit dışı; panelde audit görüntüleme sayfası YOK (psql/Seq).
  - **Doğrulama (19 Tem canlı + test):** xUnit **54/54**. Canlı (panel, antiforgery akışıyla): moderatör `smokemod` 2 modül izniyle oluşturuldu → `admin_permissions` 2 satır doğru bayraklarla (boş satır elendi) → Edit'te 4 izin + Aktif checkbox'ı işaretli render → izinler tek "users/read" satırıyla KOMPLE değiştirildi → şifre sıfırlama (BCrypt hash değişimi DB'den kanıtlı) → silme (soft delete `deleted_at` dolu). g: Fiat Egea ilanına psql'le 2 özellik değeri eklendi → Edit sayfasında "Yakıt Tipi: Dizel / Model Yılı: 2019" göründü. i: panel Reject/Approve (event) + Reject (campaign) + UpdateStatus (complaint) → `audit_logs`'ta events/reject+approve, campaigns/reject, complaints/resolve `{"status":"resolved"}` satırları affected_id'li düştü; staff akışının create/set-permissions/reset-password(detaysız)/delete izleri de doğrulandı. ⚠️ Smoke sırasında öğrenim: mock "Kadirli Yaz Konseri" etkinliği 3 Tem'den beri SOFT-SİLİNMİŞ (listede görünmez ama tabloda durur) — canlı test hedefi seçerken `deleted_at IS NULL` şartıyla sorgula. Tüm kalıntılar temizlendi (smokemod+izinleri, 2 özellik değeri, kampanya/şikayet statüleri eski haline, audit_logs sıfır).

- [x] **YAPILDI — 10.9 DENETİMİ (19 Tem 2026, 3. oturum): a-i'nin tamamı kod düzeyinde yeniden denetlendi; 5 eksik/yanlış bulundu ve kapatıldı.** Alt maddelerin kendisinde işlevsel hata çıkmadı; bulgular çevresindeki boşluklardı:
  - **🔴 (1) Panel antiforgery boşluğu:** 10.9'un YENİ POST'ları korumalıydı ama ESKİ aksiyonların çoğu değildi (Ads/Events/Campaigns Approve-Reject-Delete, Users Create/Edit, Deaths, PowerOutages, Guide, Places, Taxi, Complaints UpdateStatus, Account Login — toplam 14 controller'da ~30 aksiyon). ÇÖZÜM: Web `Program.cs`'te **global `AutoValidateAntiforgeryTokenAttribute`** — tüm unsafe metotlar token ister; form tag helper'ları token'ı zaten bastığından görünüm değişmedi. İstisnalar elden geçirildi: `Login.cshtml` düz HTML form olduğundan `@Html.AntiForgeryToken()` eklendi; AnnouncementsAdmin'in JS `fetch(CreateType)` çağrısı zaten `RequestVerificationToken` header'ı gönderiyormuş (dokunulmadı). Kanıt: token'sız Login ve token'sız EventsAdmin/Reject artık 400; token'lı akışlar 302.
  - **🔴 (2) AccessDenied 404'ü:** cookie config'i `AccessDeniedPath="/account/denied"` diyordu ama action YOKTU — yetkisiz sayfa açan kullanıcı (özellikle (e) ile artık kolayca oluşturulabilen moderatörler panele girip Dashboard'a yönlenince) 404 görüyordu. ÇÖZÜM: `AccountController.Denied` + `Views/Account/Denied.cshtml` (açıklama + Şifremi Değiştir/Çıkış linkleri). NOT: moderatörün panel modül sayfalarına erişememesi TASARIM GEREĞİ (izin matrisi Admin API'yi yönetir, panel admin+super_admin'indir) — artık düzgün mesajla karşılanıyor.
  - **🟠 (3) ResolveComplaint status whitelist'i handler'da yoktu:** panel kendini koruyordu ama Admin API `body.Status`'u ham geçiriyordu — API'den `status:"banana"` DB'ye ve (i sonrası) audit details'a yazılabilirdi. ÇÖZÜM: whitelist (`in_progress|resolved|rejected`) handler'a taşındı (tek doğruluk kaynağı), API doc comment'teki yanlış "in_review" düzeltildi. Kanıt: API'den "banana" → 400 VALIDATION_ERROR, DB değişmedi.
  - **🟠 (4) Audit kapsam tutarsızlığı — 13 komut daha marker aldı:** kapsam kuralı "silme/doğrulama audit'lenir" derken şu ADMIN komutları işaretsizdi: DeleteGuideItem, DeleteGuideCategory, **DeleteIntracityRoute (10.9(h)'nin KENDİ komutu — gözden kaçmıştı)**, DeleteIntercitySchedule, DeleteIntracityStop, DeleteTaxiDriver, **VerifyTaxiDriver** (doğrulama!), DeletePlace, DeleteAnnouncement, DeletePowerOutage, DeletePharmacy, **CreatePharmacySchedule (assign-duty — nöbeti kim atadı artık izleniyor, details: pharmacyId+dutyDate) + DeletePharmacySchedule (delete-duty)**, **ChangeMyPassword (staff/change-password — detaysız, şifre loglanmaz)**. Kullanıcı self-servis komutları (DeleteMyAd/DeleteMyAccount/DeleteMyFile) BİLİNÇLİ kapsam dışı — kural admin/moderasyon aksiyonları içindir. YAN İYİLEŞTİRME: `AuditBehavior` artık eski desen `ApiResponse<bool>` yanıtlarını da tanıyor — `Success=false` iz bırakmaz (announcements/power-outages delete).
  - **🟡 (5) StaffAdmin Create UX:** hata durumunda (örn. mükerrer telefon) form komple sıfırlanıyordu — girilen değerler (izin matrisi işaretleri dahil, ŞİFRE HARİÇ) artık korunuyor (`Create.cshtml` modeli `CreateStaffCommand`). Kanıt: mükerrer telefonla POST → banner + username/telefon/matris işaretleri formda duruyor.
  - **Rapor edilen ama BİLİNÇLİ dokunulmayanlar:** Logout GET (CSRF'le tetiklense de etkisi yalnız oturum kapatma — düşük risk, kayda geçirildi); CreateStaffCommand'de telefon format validasyonu zayıf (Faz 8'den — mobil auth'tan farklı olarak panel girdisi, ihtiyaç olursa ayrı iş); DeathsAdmin form lookup'ları inline `_uow` okuması (Faz 9.4 kuralı YAZMA yolları içindir, okuma driftleri 10.13 kapsamına not).
  - **Doğrulama (19 Tem canlı + test):** build 0 hata, xUnit **54/54**. Canlı: token'sız POST'lar 400 / token'lılar çalışıyor; `/Account/Denied` 200; event reject→approve audit'li; taxi verify → `is_verified+verified_by` dolu + `taxis/verify` izi; nöbet ataması → `pharmacies/assign-duty` izi (details'ta pharmacyId+dutyDate); API'den geçersiz şikayet statüsü 400. Kalıntılar temizlendi (test nöbeti silindi, audit_logs 0; smoke'ta unverify edilen mock taksici panel üzerinden yeniden doğrulandı — verified_by artık gerçek admin id).

### ✅ 10.10 (eski 10.7) — Bildirim modülü: uçlar + duyuru yayınında bildirim üretimi — TAMAMLANDI (19 Temmuz 2026)
> **Tespit:** `Notification` entity'si (user_id, title, body, type, related_id/related_type, is_read, fcm_sent...) migration'da VAR ama Application'da ve API'de SIFIR kullanım. Mobilin bildirim ekranı ve rozet sayısı için gerekli.
- [x] **Uçlar (hepsi `[Authorize]`, `NotificationsController`):**
  - `GET /v1/notifications?page=&limit=&unreadOnly=` → `GetMyNotificationsQuery` → **`NotificationListDto : PagedResult<NotificationDto>` + `unreadCount`** (filtre bağımsız TOPLAM okunmamış — rozet için; **KARAR: zarf `meta`'sı `ApiResponseWrapperFilter`'da sabit kurulduğundan unreadCount `data` İÇİNDE taşınır**, 10.13 kontratına böyle girecek). Cache'siz — kullanıcıya özel, sık değişen veri. `NotificationDto`: id, title, body, type, relatedId, relatedType, isRead, readAt, createdAt.
  - `PATCH /v1/notifications/{id}/read` → `MarkNotificationReadCommand` — sahiplik `UserId` filtresiyle; **başkasının bildirimi 404** (varlık sızmasın), ikinci çağrı idempotent 200.
  - `POST /v1/notifications/read-all` → `MarkAllNotificationsReadCommand` — tek atomik `ExecuteUpdateAsync` (UpdatedAt dahil), yanıtta `{markedCount}`.
- [x] **Bildirim üretimi — yeni `IAnnouncementNotificationGenerator` (Application/Common/Interfaces) + `AnnouncementNotificationGenerator` (Features/Notifications/Services, Application DI'da scoped).** Üç çağıran: `CreateAnnouncementCommand` (anında yayın, `isScheduled=false` ise), `UpdateAnnouncementCommand` (**plandışı ek:** scheduled/yayınlanmamış duyuru güncellemede anında yayına geçerse — o da bir "yayın anı"), `PublishScheduledAnnouncementsJob`.
  - **KARAR: `send_push_notification=false` ise satır DA yazılmaz** (plandaki 1. seçenek) — böylece 10.11 FCM job'ı "fcm_sent=false her satır push'lanabilir" varsayımıyla sade kalır.
  - Hedefleme: `targetType=neighborhood` → `PrimaryNeighborhoodId` VEYA `UserNeighborhood` eşleşmesi (TargetNeighborhoods JSON'ı deserialize edilir); diğer tüm targetType değerleri "all" gibi (panel yalnız all/neighborhood üretiyor). Kullanıcı filtresi: `IsActive && !IsBanned` (+soft-delete global filtresi).
  - **PLANDIŞI (karar): `NotificationPreferences.Announcements=false` kullanıcıya satır yazılmaz** — tercih üretim anında uygulanır; aksi hâlde 10.3'teki tercih ekranı ölü kalırdı (EF8 `OwnsOne().ToJson()` üzerinde `u.NotificationPreferences.Announcements` sorgusu jsonb'ye çevrildi, canlıda kanıtlı).
  - **PLANDIŞI (karar): Body 500 karaktere kırpılır** ("…" ekiyle) — bildirim listesi özet, tam metin duyuru detayında.
  - İdempotency işareti: duyuru başına `related_type='announcement' AND related_id` VARSA üretim komple atlanır (plandaki "related_id kontrolü" seçeneği). Toplu insert: hedef id listesi + AddRange benzeri döngü + TEK SaveChanges (plandaki 1. seçenek; Kadirli ölçeğinde yeterli, raw SQL'e gerek görülmedi).
- [x] **`PublishScheduledAnnouncementsJob` yeniden düzenlendi:** set-tabanlı tek `ExecuteUpdateAsync`'ten "duyuruları yükle → her biri için ÖNCE bildirim üret SONRA status=active+SentAt → tek SaveChanges" akışına geçti (üretim için duyuru başına Id/Title/Body gerekiyor). **Sıralama bilinçli:** arada çökülürse duyuru scheduled kalır, sonraki koşu devralır; üretim idempotent olduğundan mükerrer satır oluşmaz. Faz 9.4 resiliency (AutomaticRetry + DisableConcurrentExecution) korunuyor; log artık bildirim sayısını da basar. Job ile generator aynı scoped `AppDbContext`'i paylaşır (UnitOfWork aynı context'i sarar — doğrulandı).
- [x] **Doğrulama:** xUnit **58/58** (yeni `NotificationsTests` 4 test: anında yayın + tercih + read/read-all/sahiplik; mahalle hedefleme; push=false → 0 satır; job iki kez koşunca mükerrer üretmez). Canlı curl: all duyurusu → kullanıcıda satır + unreadCount=1; anonim 401; read 200/200(idempotent)/başkası 404; H2-hedefli duyuru kullanıcıya DÜŞMEDİ, H1-hedefli DÜŞTÜ; push=false → DB'de 0 satır; tercih kapalıyken yeni duyuru satır üretmedi; zamanlanmış duyuru Hangfire dakikalık job'ıyla active oldu + 8 kullanıcıya 8 satır (count=distinct → mükerrer yok); read-all sonrası unreadCount=0. Test kalıntıları (6 duyuru + bildirimleri + canlitest kullanıcısı) temizlendi.

### ✅ 10.10-A (ara başlık — plan dışı vizyon turu, 19 Temmuz 2026) — Panel etkileşim istatistikleri — TAMAMLANDI
> **Bağlam:** Kullanıcı talebiyle 10.11'den önce yapıldı. Masterclass'ın vizyon eksiği tespiti: mobil etkileşim sayaçları (10.12'de eklenen view/click/view-code/call + 10.6 ilan sayaçları) DB'de birikecekti ama WEB PANELİNDE HİÇBİRİ GÖRÜNMÜYORDU. Kapsam, ayrı bir tartışma ajanıyla karşılıklı beyin fırtınası yapılarak belirlendi (KES/KALSIN/EKLE turu + karşı-itiraz turu); mutabakat kayıtları aşağıda.
- **Tartışmada alınan kararlar (ajanla mutabakat):**
  - 🔑 **Panel Index'leri public query paylaşıyor** (GetAnnouncements/GetTaxiDrivers/GetAds/GetCampaigns) → public DTO'lara sayaç alanı EKLENMEDİ (kontrat donmak üzere + toplam sayaçlar mobil kullanıcıya sızmamalı). Desen: **panel-only `Get{Modül}AdminStatsQuery`** — controller iki sonucu ViewBag'de birleştirir; DTO çatallama 10.13'ün konusu.
  - **KES:** ayrı "İstatistikler" sayfası (veri şu an sıfır — boş vitrin; kolon+dashboard satırı doğru altitude), duyuru başına bildirim üretilen/okunan metriği (FCM yokken hiçbir şey ölçmez — 10.11 SONRASI aday), client-side JS tablo sıralaması (panelin "JS'siz" konvansiyonunu kırar), sıralanabilir kolonlar, admin API uçları (tüketicisi yok; query Application'da — ileride uç açmak dakikalık, Progress notu yeterli).
  - **KVKK ÇİZGİSİ:** kullanıcı bazlı "kim gördü/kim favoriledi" listeleri PANELE GİRMEZ (amaç sınırlılığı; küçük ilçede "vefat duyurusunu kimler açtı" dedikodu altyapısıdır). Yalnız SAYI gösterilir. taxi_calls yolcu kimliği de gösterilmiyor; ihtiyaç doğarsa audit-loglu super_admin-only görünüm ayrıca tartışılır.
  - **Tek kaynak kuralı (taksi):** panel `total_calls` denormalize sayacını DEĞİL `taxi_calls` GROUP BY (COUNT + MAX(called_at)) okur — çift kaynaklı tutarsızlık görünümü olmasın; son çağrı zamanı "ölü sürücü" tespiti sağlar.
  - ⚠️ `announcement_views`'ta timestamp YOK (composite PK yalın) → duyuru görüntülemede 7-günlük trend İMKANSIZ, toplam gösterilir. Buna karşılık "tekil erişim" (COUNT — kullanıcı başına tek satır) view_count'tan daha değerli: erişim oranının temeli.
- [x] **Yeni Application query'leri (üçü de cache'siz — admin-only + "şu anki sayı" beklentisi; modül-içi yerleşim, `Get{Modül}AdminStatsQuery` kalıbı — `grep AdminStats` hepsini bulur):**
  - `Features/Taxis/Queries/GetTaxiAdminStatsQuery` → `Dictionary<Guid, TaxiAdminStatsDto(CallCount, LastCallAt)>`
  - `Features/Announcements/Queries/GetAnnouncementAdminStats/` → `Dictionary<Guid, AnnouncementAdminStatsDto(ViewCount, ClickCount, UniqueViewers)>` (announcement_views 10.12'deki `IUnitOfWork.SetQuery<T>` ile okunur)
  - `Features/Ads/Queries/GetAdAdminStatsQuery(AdId)` → `AdAdminStatsDto(ViewCount, PhoneClickCount, WhatsappClickCount, FavoriteCount)` — AdDetailDto telefon/WhatsApp sayaçlarını TAŞIMIYOR ve GetMyAdsQuery public kontrat olduğundan yeni mini query şart çıktı (ajan tespiti).
- [x] **Panel değişiklikleri:** TaxiAdmin Index "Çağrı"+"Son Çağrı" kolonları (hiç çağrı yoksa 0/—); AnnouncementsAdmin Index "Görüntülenme (n tekil)"+"Tıklama" kolonları (tıklama yalnız HasLink duyuruda, değilse "—"); CampaignsAdmin Index "Kod Gör." kolonu (CodeViewCount DTO'da zaten vardı — bedava); AdsAdmin Index "Görüntülenme" kolonu (ViewCount DTO'da vardı — bedava); AdsAdmin **Edit**'e salt-okunur "Etkileşim" kartı (10.9g kart deseni; görüntülenme/telefon/WhatsApp/favori — "ilanım tutmuyor" şikayetinde adminin bakacağı yer).
- [x] **Dashboard "Etkileşim" satırı** (`GetDashboardStatsQuery`'ye 4 alan — 60 sn cache bedava; DTO admin API'den de dönüyor, additive olduğundan kırıcı değil): **Son 7 gün yeni kayıt** (mobil yayın sonrası her sabah bakılacak sayı), **son 7 gün taksi çağrısı**, **son 7 gün yeni ilan** (ajan önerisiyle "toplam ilan görüntüleme"nin yerine — hep büyüyen gösteriş sayısı yerine haftalık nabız), **toplam duyuru görüntüleme** (timestamp olmadığından toplam kalmak zorunda).
- **Doğrulama:** xUnit **62/62** (yeni `PanelStatsTests`: üç stats query aggregate'leri + dashboard yeni alanları). Canlı panel smoke (psql ile geçici sayaç verisi basılarak): Dashboard Etkileşim satırı render; AnnouncementsAdmin 42/(1 tekil)/7 göründü; TaxiAdmin çağrı=1 + Son Çağrı; CampaignsAdmin "Kod Gör."; AdsAdmin Index 99; AdsAdmin Edit kartı 99/13/6+favori. Geçici veriler + dashboard cache anahtarı temizlendi. ⚠️ Öğrenim düzeltmesi: view'lardaki STATİK Türkçe metin ham UTF-8 basılır (entity-encoding yalnız model-bound çıktıda) — grep'te düz Türkçe kullan.

### ✅ 10.11 (eski 10.8) — FCM push gönderimi (FirebaseAdmin + Hangfire) — TAMAMLANDI (25 Temmuz 2026, no-op default'la; gerçek service-account İSTENMEDİ — bilinçli, aşağıda)
> **Yapıldı (SMS/e-posta adaptör deseninin aynısı):** `FirebaseAdmin` 3.6.0 (Infrastructure) + `IPushService` (Application; `SendAsync(IReadOnlyList<PushMessage>)`→`IReadOnlyList<PushResult>` + `bool IsConfigured`) + iki adaptör: `NoOpPushService` (varsayılan, `Fcm:Provider=None`) ve `FcmPushService` (`Fcm:Provider=Firebase`; service-account yolu `Fcm:ServiceAccountKeyPath`'ten, YOKSA yine no-op + uyarı → Firebase'siz ortam çökmez). DI switch (SMS deseni; bilinmeyen sağlayıcı `InvalidOperationException`). `SendPushNotificationsJob` (Cron.Minutely, 9.4 resiliency: AutomaticRetry{60,300,900}→Fail + DisableConcurrentExecution): `IsConfigured=false` ise DB'ye hiç dokunmadan döner; aksi hâlde `fcm_sent=false && User.FcmToken!=null` bildirimleri ≤500'lük batch'le `SendEachAsync`'e verir. **İşaretleme semantiği:** `fcm_sent=true` terminal (tekrar denenmez); başarıda `fcm_sent_at` dolu + `fcm_error=null`, başarısızlıkta `fcm_sent_at=null` + `fcm_error=sebep` (ikisi bir arada olmaz); mesaj-bazı hata terminal, yalnız BATCH exception Hangfire retry'ına gider. **UNREGISTERED** → `User.FcmToken=null` temizlenir. Token'sız kullanıcının bildirimi sorguya hiç girmez (mobil öncesi token olmadığından job doğal olarak boş koşar; sağlayıcı sonradan bağlanınca token'lı bekleyenler gönderilir). Config `Fcm:{Provider:None, ServiceAccountKeyPath:""}` Api+Web appsettings'te. Health check `fcm` EKLENMEDİ (opsiyoneldi; no-op'ta anlamsız — gerçek bağlamada 10.13/deploy'da eklenebilir). **⭐ PLANDIŞI (raporlandı):** push `data` yükünde `notificationId`(+type/relatedId/relatedType) → mobil push'tan ilgili kayda deep-link + okundu işaretleme yapabilsin. xUnit **65/65** (yeni `PushNotificationsJobTests` 2 test: no-op hiç göndermez/DB'ye dokunmaz + gerçek sağlayıcıda sent/error/invalid-token-temizliği; test için `FakePushService`). Build 0 hata (yeni uyarı yok).
> **⚠️ KALAN TEK BAĞLAMA İŞİ (kullanıcı mobili bitirince yapacak):** Firebase Console → Project Settings → Service Accounts'tan JSON al → makinede bir yola koy → `Fcm:Provider=Firebase` + `Fcm:ServiceAccountKeyPath=<yol>`. Başka KOD değişmez. FCM ücretsizdir (Flutter da aynı projeyi kullanır). Gerçek cihaza push doğrulaması mobil faza kalır.
> **Orijinal tespit (referans):** Push gönderim altyapısı hiç yoktu. Masterclass §14: `FirebaseAdmin` NuGet + Hangfire job.
- Yapılacaklar:
  - `FirebaseAdmin` paketi; `IPushService` (Application interface) + `FcmPushService` (Infrastructure; service-account yolu config'ten, yoksa **no-op + warning log** — Firebase'siz ortamda sistem çalışmaya devam etmeli, dev'de böyle test edilir).
  - Hangfire job: `fcm_sent=false` bildirimleri batch'le gönder (500'lük FCM multicast), sonucu `fcm_sent/fcm_sent_at/fcm_error`'a yaz; geçersiz token hatasında (`UNREGISTERED`) kullanıcının `FcmToken`'ını temizle. Faz 9.4 resiliency desenleri: `AutomaticRetry` + `DisableConcurrentExecution` + yapılandırılmış log.
  - Health check'e `fcm` eklenmesi OPSİYONEL (dış servis; readiness'ı bloklamamalı — `Degraded` seviyesi kullan).
- **Doğrulama:** service-account'suz ortamda no-op logu; sahte token'lı kullanıcıyla job koşunca `fcm_error` dolu ve token temizlendi; gerçek cihaz testi mobil faza kalır (not düş).

### ✅ 10.12 (eski 10.9) — Etkileşim sayaçları ve küçük eksik uçlar — TAMAMLANDI (19 Temmuz 2026; 10.11'den ÖNCE yapıldı — FCM'den bağımsız, 10.11 dış bağımlılık [Firebase JSON] beklediği için öne alındı, kullanıcı onayıyla)
> **Tespit:** `AnnouncementView`, `CampaignCodeView`, `TaxiCall` entity'leri kullanım dışı; `announcements.view_count/click_count` hiç artmıyor; masterclass'taki `POST /campaigns/{id}/view-code` ve `POST /taxis/drivers/{id}/call` yok.
- [x] **`POST /v1/announcements/{id}/view` + `/{id}/click`** (`TrackAnnouncementCommand(Id, Kind, UserId?)` — anonim serbest, `[EnableRateLimiting("public-write")]` 10.7 deseni): `view_count`/`click_count` atomik `ExecuteUpdateAsync` (TrackAdContact deseni); görünürlük public detayla aynı — yalnız active + süresi dolmamış duyuru, diğerine 404 (sayaç şişirme + varlık sızıntısı yok). **View + giriş yapmış kullanıcı:** `announcement_views`'a (announcement_id, user_id) satırı — composite PK, kullanıcı başına TEK satır ("kim gördü" kümesi), yarışta DbUpdateException yutulur (favorite deseni); `view_count` ise HER çağrıda artar (toplam açılış sayısı). Anonim view'da satır yok (UserId non-null).
- [x] **`POST /v1/campaigns/{id}/view-code`** (`[Authorize]`, `ViewCampaignCodeCommand` → `CampaignCodeDto{code, viewedAt}`): yalnız approved + tarih-geçerli kampanya (public kural), diğerine 404. Kullanıcı başına TEK `campaign_code_views` kaydı — ikinci istek MEVCUT kaydı döner (aynı viewedAt), `code_view_count` artmaz → **sayaç semantiği: "kodu kaç FARKLI kullanıcı gördü"**. **KARAR: `DiscountCode` NULL/boş kampanyada 400 VALIDATION_ERROR** ("Bu kampanyanın indirim kodu yok."), iz de düşülmez — istatistik şişmez. Not: campaign_code_views'ta unique index yok — eşzamanlı ilk-istek yarışı nadiren çift satır bırakabilir; okuma ViewedAt sırasıyla ilkini döndüğünden davranış bozulmaz (kabul edilen risk).
- [x] **`POST /v1/taxis/drivers/{id}/call`** (`[Authorize]` — plandaki öneri izlendi: telefon döndüğü + `TaxiCall.PassengerId` zorunlu olduğu için anonim yok; `CallTaxiDriverCommand` → `TaxiCallResultDto{phone}`): yalnız verified+active sürücü (public kural) 404 aksi; her çağrı YENİ `taxi_calls` satırı (tekrarlanabilir eylem) + `total_calls` atomik artış; yanıtta aranacak telefon.
- [x] **Cache etkileşimi (plan şartı):** announcements/campaigns/taxis sorgularının HİÇBİRİ cache'li değil (grep ICacheableQuery ile doğrulandı — cache'li gruplar: guide/pharmacies/dashboard/lookups/ads-lookup) → sayaç artışları ayrı atomik UPDATE, hiçbir cache grubunu invalidate etmiyor; ileride bu listeler cache'lenirse sayaçlar yine dokunmamalı (TTL tazeler).
- [x] **PLANDIŞI (altyapı, zorunlu):** `IUnitOfWork`'e `SetQuery<T>()` + `AddToSetAsync<T>()` eklendi (`where T : class`) — `AnnouncementView` BaseEntity DEĞİL (composite key, Id/CreatedAt yok) ve `IRepository<T> where T : BaseEntity` kısıtına takılıyordu; `IDapperContext` hiç kullanılmayan ölü arayüz + Dapper paketi Application'da yok olduğundan EF `Set<T>()` üzerinden ince genel erişim tercih edildi. UserNeighborhood gibi diğer join tabloları da ileride buradan yazılabilir.
- **Doğrulama:** xUnit **61/61** (yeni `CountersTests` 3 test: view/click sayaçları + auth iz tekilliği + yayında-olmayan 404; view-code kod/tek-kayıt/kodsuz-400/anonim-401; taksi çağrı iz+sayaç/unverified-404/anonim-401). Canlı curl: anonim 2 view + 1 click + auth 2 view → view_count=4, click_count=1, announcement_views=1 satır; olmayan duyuru 404; view-code ilk/ikinci istek AYNI viewedAt + code_view_count=1, kodsuz 400, anonim 401; taksi 2 çağrı → taxi_calls=2 + total_calls=2 + yanıtta telefon, doğrulanmamış sürücü 404, anonim 401. Kalıntılar temizlendi (kampanya orijinal TATLI20/end_date'ine geri döndü, sayaçlar sıfırlandı, test kullanıcısı silindi). ⚠️ Not: dev DB'deki mock kampanyaların tarihi geçmiş (canlı testte end_date geçici uzatıldı, geri alındı).

### ✅ 10.13 (eski 10.10) — API kontrat temizliği + Flutter el kitabı (API_CONTRACT.md) + CORS — TAMAMLANDI (25 Temmuz 2026)
> **Yapıldı:** (1) **Meta tutarlılığı** — `ApiResponseWrapperFilter` artık kendi `ApiResponse<T>`'sini dönen ~13 handler'ın (Announcements/PowerOutages/Users) BOŞ meta'sını da dolduruyor (reflection'la `Meta` prop'u; handler/consumer'a dokunulmadı) → TÜM public uçlarda `meta.timestamp/path/traceId` (canlı curl: neighborhoods sade-DTO + announcements/types & power-outages self-wrap → hepsi meta taşıyor). (2) **CORS** — `AddCors`+`UseCors("Default")` eklendi, `Cors:Origins`'ten okur (boşsa etkisiz; native mobil CORS istemez, yalnız Flutter WEB için); canlı preflight `Access-Control-Allow-Origin` döndü. (3) **Swagger JWT** — `AddSecurityDefinition("Bearer")` (⚠️ Microsoft.OpenApi 2.7.5'te tipler `Microsoft.OpenApi` namespace'inde, eski `.Models` + `OpenApiReference`/`ReferenceType` API'si kalktı → sadeleştirildi, yalnız definition). (4) **Path kebab-case** — `ApiControllerBase`'in `[Route("v1/[controller]")]`'ını miras alan 5 controller PascalCase üretiyordu (`/v1/Announcements`, `/v1/PowerOutages`, Files/Notifications/Users) → yeni `SlugifyParameterTransformer` + `RouteTokenTransformerConvention` ile tümü kebab (explicit-route'lu adminlere dokunmaz); openapi'de artık SIFIR PascalCase public path. ⚠️ `power-outages` tireli olduğundan eski `/v1/PowerOutages` 404 olur (diğer 4 tek-kelime case-insensitive çalışır); `PublicEndpointAuthorizationTests` kanonik path'e güncellendi. (5) **openapi.json** — `docs/openapi.json` üretildi (OpenAPI 3.0.4, 135 uç, Bearer şeması; çalışan dev API'den `/swagger/v1/swagger.json`). (6) **`Memory_Bank/API_CONTRACT.md`** yazıldı (zarf, hata kodları sözlüğü [ExceptionMiddleware eşlemesi + AppException kodları], auth akışı OTP→verify→register/refresh→logout, sayfalama `PagedResult`, tarih UTC-ISO, görsel URL kuralı, rate limit, CORS, 60 public uç envanteri + görünürlük kuralları). (7) **Ölü kod temizliği:** AutoMapper paketi kaldırıldı (sıfır kullanım); `FluentValidation.DependencyInjectionExtensions`→core `FluentValidation` (yalnız `ValidationException` kullanılıyor, validator DI yok); 6 ölü FluentValidation validator dosyası silindi (Ads×3/Transport×2/Taxis×1 — hiçbir pipeline'a kayıtlı değildi, [10.5/10.10 notu KAPANDI]); ölü `IDapperContext`+`DapperContext`+DI kaydı silindi (Dapper paketi Hangfire.PostgreSql için kalır); ölü `Files` appsettings bloğu (kimse okumuyordu) kaldırıldı, gerçek `FileStorage:BaseUrl` belgelendi. **Karar (görsel URL):** göreli `/uploads/...` korundu — kontrat kuralı "istemci API origin'i ekler; prod'da `FileStorage:BaseUrl` mutlak yapılır". **Karar (announcements NOT_FOUND 200+success:false):** kontrata quirk olarak YAZILDI, davranış değiştirilmedi (geriye uyum). Build 0 hata; xUnit **65/65**. **⭐ PLANDIŞI (raporlandı):** path kebab-case tutarlılığı (planda yoktu; Flutter codegen için gerçek tutarsızlıktı).
> **Orijinal tespit (referans):** (1) Bazı eski handler'lar `ApiResponse<T>`'yi kendisi dönüyor — bunların `meta`'sı NULL (traceId/timestamp yok), filter'la sarılanlarda dolu: Flutter tarafında iki farklı meta davranışı olur. (2) Görsel URL'leri göreli (`/uploads/...`) — mobilin base URL ekleme kuralı YAZILI DEĞİL. (3) `Cors:Origins` appsettings'te tanımlı ama **`AddCors`/`UseCors` Program.cs'te HİÇ YOK** (ölü config; Flutter web hedeflenirse şart). (4) Swagger yalnız Development'ta; Flutter için makine-okur kontrat (openapi.json) çıkarılmamış. (5) Hata kodları sözlüğü (INVALID_OTP, VALIDATION_ERROR, CONFLICT...) tek yerde belgelenmemiş.
- Yapılacaklar:
  - Zarf denetimi: `ApiResponse<T>` dönen public handler'ları tara (grep `ApiResponse<`) — ya filter zarfına geçir (handler sade DTO dönsün; tercih edilen) ya da `SuccessResponse`'a meta doldurt; sonuçta TÜM public uçlarda `meta.timestamp/path/traceId` tutarlı olmalı.
  - Görsel URL kararı: ya `Files:PublicBaseUrl`'i mutlak yap (prod domain'i config'ten) ya da "istemci base URL ekler" kuralını kontrata yaz. Karar + uygulama.
  - `AddCors` + `UseCors` (config'teki origins; yalnız gerekiyorsa — mobil native CORS istemez, kararı yaz).
  - Swagger'dan `openapi.json` üret (build'de `swagger tofile` veya çalışan API'den indir) → repo'ya `docs/openapi.json`; Flutter tarafında kod üretimi (`openapi_generator` / `dio`) için temel.
  - **`Memory_Bank/API_CONTRACT.md` yaz:** zarf şeması, hata kodları sözlüğü (ExceptionMiddleware + AppException türevlerinden derle), auth akışı (OTP→verify→refresh), sayfalama alanları (`items/totalCount/pageSize/currentPage/totalPages`), tarih formatı (UTC ISO-8601, timestamptz kuralı), görsel URL kuralı, public endpoint envanteri (10.1-10.12 sonrası güncel hali). Bu dosya mobil oturumlarının ana referansı olacak.
- **Doğrulama:** 3-4 farklı uçtan meta tutarlılığı curl ile; openapi.json geçerli (swagger-cli validate veya editor.swagger.io); API_CONTRACT.md'deki her endpoint'e örnek istek/yanıt.

### 10.14 — 🔎 SUNUM DOKÜMANI DENETİMİ BULGULARI (20 Temmuz 2026) — YAPILACAK
> **Bağlam:** `SUNUM.MD` hazırlanırken kod tabanı 4 bağımsız ajanla baştan tarandı (mimari / API+güvenlik / ürün+panel / yol haritası). Geliştirme YAPILMADI, yalnız tespit. Aşağıdaki 3 madde **kod düzeyinde doğrulanmış** bulgulardır; tamamı `SUNUM.MD` Bölüm VI'da da listelidir. Diğer bulgular (CORS, Swagger JWT, FluentValidation/AutoMapper/Dapper ölü kod) zaten 10.13 kapsamındadır, buraya tekrarlanmadı.

- [x] **(1) 🔴 `RejectAdCommandHandler` red sebebini KAYDETMİYORDU — ✅ TAMAMLANDI (25 Temmuz 2026).**
  - **Neydi:** Handler reddederken `RejectedReason`/`RejectedAt` yerine `ApprovedBy`/`ApprovedAt`'i dolduruyordu → `MyAdDto.rejectedReason` daima NULL, **kullanıcı ilanının neden reddedildiğini göremiyordu.** Kampanya ve etkinlik reddi doğru alanları kullanıyordu; tutarsızlık yalnız Ads'teydi.
  - **Kalıcı kural:** bir kayıt aynı anda onaylı+reddedilmiş olamaz — `Reject` onay izini, `Approve` da red gerekçesini **null'lar**. Bu kural sonradan kampanyada da eksik çıktı (11.15b) ve `CODE_REVIEW_CHECKLIST` §2'ye madde olarak girdi. "Kim reddetti" izi `IAuditableCommand`'dan okunur, onay alanları ezilerek değil.
  - **Doğrulama:** panelden sebep girerek reddet → `rejected_reason`/`rejected_at` dolu, `approved_by`/`approved_at` NULL → sahibi `GET /v1/users/me/ads?status=rejected` çağırınca gerekçeyi görüyor. Regresyon testi: `Reject_StoresReason_ClearsApprovalTrail_And_VisibleInMyAds`.

- [x] **(2) 🟠 `/hangfire` dashboard'unda yetkilendirme filtresi YOK — production fazında zorunlu.** ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  - **Tespit:** `UseHangfireDashboard` çağrısında `IDashboardAuthorizationFilter` verilmemiş. Hangfire'ın varsayılan davranışı yalnız **yerel (localhost) isteklere** izin vermek olduğundan bugün dev ortamında sorun görünmüyor.
  - **Etki:** Uygulama bir reverse proxy (nginx/Traefik/IIS) arkasına alındığı anda tüm istekler proxy'den geliyormuş gibi görünür, localhost koruması KALKAR → **iş kuyruğu paneli herkese açılır** (job tetikleme, kuyruk temizleme, argüman görüntüleme).
  - **Yapılacak:** `IDashboardAuthorizationFilter` implementasyonu — panel cookie auth'una bağlanıp `admin`/`super_admin` rolü şartı (Web host'ta) veya ayrı basic-auth (Api host'ta). Aynı deploy turunda `ForwardedHeaders` middleware'i de yapılandırılmalı (aksi hâlde rate limit partition'ları da tek IP'ye düşer — bkz. SUNUM.MD Bölüm VI/G3).
  - **Doğrulama:** Giriş yapmamış oturumla `/hangfire` → 302/403; admin oturumuyla → 200.

- [ ] **(3) 🔴 `uploads/` için kalıcı Docker volume YOK — veri kaybı riski.** ⏭️ *(13 Ağu 2026: **deploy fazına ait** — API bugün compose'da değil, `uploads/` repo yanında düz klasör; volume bağlanacak servis yok. Risk API konteynerleştiği gün doğar.)*
  - **Tespit:** `docker-compose.yml` yalnız `pgdata` ve `seqdata` volume'larını tanımlıyor. Yüklenen görseller (`LocalFileStorageService` → `uploads/`) konteynerin yazılabilir katmanında duruyor.
  - **Etki:** Konteyner yeniden oluşturulursa (imaj güncellemesi, `docker compose down`) **yüklenmiş TÜM görseller kalıcı olarak kaybolur** — ilan fotoğrafları, duyuru/etkinlik/kampanya görselleri, profil fotoğrafları. `files` tablosundaki kayıtlar kalır ama dosyalar gider → kırık görsel.
  - **Yapılacak:** `uploads` için named volume (veya bind mount) tanımla; API servisini compose'a aldığında `volumes: - uploaddata:/app/uploads`. Orta vadede object storage'a (S3/MinIO) geçiş değerlendirilmeli — `IFileStorageService` soyutlaması zaten mevcut, adaptör eklemek yeterli.
  - **Doğrulama:** Görsel yükle → `docker compose down && up -d` → görsel hâlâ erişilebilir.

> **Önerilen sıra (25 Tem güncellendi):** ~~10.1 → 10.9~~ (TAMAM) → ~~10.10 (bildirim)~~ → ~~10.12 (sayaçlar)~~ → ~~10.14/(1) Ads reject düzeltmesi~~ (✅ TAMAM 25 Tem) → ~~10.11 (FCM)~~ (✅ TAMAM 25 Tem, no-op default) → **10.13 (kontrat temizliği — en sonda çünkü envanter tüm uçlar bittikten sonra anlamlı)**. Kalan production/deploy: 10.14/(2) hangfire dashboard auth, 10.14/(3) uploads volume; ayrıca gerçek SMS sağlayıcısı + Firebase service-account bağlama (mobil yayın anı). **10.14/(2) ve (3) production/deploy fazına ait** — mobil geliştirmeyi bloklamaz ama YAYINDAN önce zorunlu. Faz 9.2 altyapısı tamam (16 Tem); mobil YAYINDAN önce ayrıca gerekli: gerçek SMS sağlayıcısının bağlanması (~~hesap silme ucu~~ 10.8'de tamamlandı).

---

# 📱 FAZ 11 — MOBİL UYGULAMA (Flutter) — YENİ BÜYÜK FAZ (25 Temmuz 2026'da planlandı)

> **Durum:** Backend + Admin panel bitti (Faz 0-10). Public API Flutter'a hazır. Bu faz, mobil uygulamayı **sıfırdan yayına** kadar götürür. Her alt-faz **bir oturumda tam bitirilebilecek** boyuta bölündü — yarıda kalma yok: her alt-faz sonunda uygulama DERLENİR, ÇALIŞIR (gerçek API'ye bağlı smoke) ve Memory Bank güncellenip commit atılır.

## 📌 Referanslar (her mobil oturumda önce bunlar okunur)
- **`Memory_Bank/API_CONTRACT.md`** — zarf `{success,data,meta}`, hata kodları sözlüğü, auth akışı (OTP→verify→register/refresh→logout), sayfalama (`PagedResult{items,totalCount,pageSize,currentPage,totalPages}`), tarih (UTC ISO-8601), görsel URL kuralı (göreli `/uploads/…` → istemci origin ekler), 60 public uç envanteri, görünürlük kuralları.
- **`Memory_Bank/MOBILE_UX_PLAN.md`** — tasarım sistemi (renk/tip/ikon/hareket), navigasyon (alt sekme + Ana Sayfa hub + sağ üst Ayarlar), modül ekran deseni, ortak durumlar, backend'e göre davranış.
- **`docs/openapi.json`** — makine-okur şema (kod üretimi/doğrulama için).
- **`docs/mobile-mockup.html`** — 6 ekranın görsel mockup'ı + "buton → uç" haritası (yayın: claude.ai artifact).
- **`Memory_Bank/Active_Context.md`** — son oturum durumu.

## 🎨 Tasarım token'ları (kod için — MOBILE_UX_PLAN'dan)
- **Açık:** primary `#2C7A57`, primary-deep `#215B41`, primary-tint `#E8F3EC`, accent `#E08A3C`, bg `#FAF9F6`, surface `#FFFFFF`, border `#E7E4DD`, ink `#1E2A24`, muted `#5C6B63`. Semantik: success `#2E8B57`, info `#2F6FB0`, warning `#E0A32E`, danger `#D64545`.
- **Koyu:** bg `#121815`, surface `#1B2420`, border `#2A352F`, primary `#46B083`, ink `#ECF1EE`, muted `#9DB0A6`.
- **Tip:** yuvarlak-sıcak sans (hedef **Nunito**, pubspec'e font ekle); ölçek Display 28 / H1 22 / H2 18 / Body 16 / sm 14 / caption 13 / label 12; satır ~1.4. **Hareket:** az & anlamlı — geçiş 220ms, skeleton loader, pull-to-refresh, buton scale .98.

## 📍 Mobil proje konumu ve komutları (11.1'de kuruldu)
- Kod: **`mobile/`** (repo kökünde, .NET çözümünün yanında). Kurulum/çalıştırma: `mobile/README.md`.
- `cd mobile && flutter pub get && flutter run` · gerçek cihaz: `--dart-define=API_BASE_URL=http://<LAN-IP>:5005` · prod: `--dart-define=FLAVOR=prod`.
- Her oturum sonunda: `flutter analyze` (0 sorun) + `flutter test` (hepsi geçer).
- ⚠️ Bu makinede **Xcode kurulumu eksik → iOS derlemesi/simülatörü yok**; doğrulama Android emülatörü (`Pixel_9`, API 37) üzerinden yapılıyor. iOS ilk kez yayın fazından (11.16) önce denenmeli (CocoaPods da kurulu değil).

## ⚙️ Teknoloji kararları (11.1'de kurulur, sonra sabit)
- **Flutter** (stable, Dart 3) · **State:** Riverpod (`flutter_riverpod` + `riverpod_annotation`) — bu ölçek için sade, test edilebilir · **Routing:** `go_router` (push deep-link dostu) · **HTTP:** `dio` (interceptor'lar) · **Model/JSON:** `freezed` + `json_serializable` · **Güvenli depo:** `flutter_secure_storage` (token'lar) · **Tercih/tema:** `shared_preferences` · **Görsel:** `cached_network_image` + `image_picker` (yükleme) · **Tarih/Türkçe:** `intl` · **Dış link:** `url_launcher` (telefon/WhatsApp) · **Push:** `firebase_core` + `firebase_messaging` (11.13'te, Firebase config kullanıcıdan) · **Sayfalama:** manuel veya `infinite_scroll_pagination`.
- **Mimari:** feature-first klasör (`lib/features/<modül>/{data,domain,presentation}` + `lib/core/{network,theme,router,widgets,utils}`). Her modül: repository (dio çağrısı) → provider (state) → ekran(lar).
- **KARAR:** Her `[A]` uç Bearer ister; 401'de otomatik refresh (interceptor), refresh de olmazsa login'e yönlendir. Görsel URL'e origin ekleme tek helper'da. Hata `error.code` → kullanıcı mesajı tek sözlükte.

## 🧭 Çalışma kuralları (her mobil oturum)
1. Oturum başında referansları oku (özellikle API_CONTRACT + MOBILE_UX_PLAN + Active_Context).
2. **Yalnız o oturumun alt-fazını** bitir (kapsam kayması yok). Alt-faz "bitti" = ekran(lar) çalışır + gerçek API'ye bağlı manuel smoke geçer + boş/yükleniyor/hata durumları var.
3. Backend'e DOKUNULMAZ (kontrat dondu). Eksik/uç gerekirse Progress'e not düş, o oturumda backend'e geçme.
4. Oturum sonunda: `flutter analyze` temiz, uygulama derlenir/çalışır, Progress + Active_Context güncellenir, commit atılır.
5. ⚠️ **11.13 (FCM) oturumunda Firebase yapılandırması (google-services.json / GoogleService-Info.plist) kullanıcıdan istenir.**

---

### 11.1 — Proje iskeleti + tasarım sistemi (tema) — [x] ✅ TAMAMLANDI (29 Temmuz 2026)
- [x] `flutter create --org app.kadirli --platforms=android,ios` → **`mobile/`** dizini (Flutter 3.44.2 / Dart 3.12.2). Web/masaüstü platform kodu üretilmedi.
- [x] `pubspec.yaml`: riverpod 3 · go_router 17 · dio 5 · freezed+json_serializable · flutter_secure_storage · shared_preferences · cached_network_image · image_picker · intl · url_launcher · flutter_localizations (+ build_runner/freezed dev). **Nunito** 3 ağırlık (400/600/700) `assets/fonts/` + OFL lisansı.
- [x] Klasör yapısı: `lib/core/{config,network,router,theme,utils,widgets}` + `lib/features/{home,settings,dev}/presentation`. `main.dart` = ProviderScope + `app.dart` (MaterialApp.router).
- [x] **Tema:** `core/theme/` → `app_colors.dart` (ham token'lar + **`AppPalette` ThemeExtension**: accent/success/info/warning/danger/border/muted/skeleton — Material `ColorScheme`'in karşılamadığı roller), `app_typography.dart` (ölçek + `TextTheme` eşlemesi), `app_spacing.dart` (`AppSpacing`/`AppRadius`/`AppDurations`/`AppA11y.minTapSize=48`), `app_theme.dart` (açık+koyu `ThemeData`, elle yazılmış `ColorScheme`'ler — `ColorScheme.fromSeed` tonu kaydırıyordu), `theme_controller.dart` (Riverpod `Notifier` + `shared_preferences`, Açık/Koyu/Sistem).
- [x] **Config:** `core/config/env.dart` — `AppFlavor{dev,prod}`, `--dart-define=FLAVOR=`, Android emülatörde `10.0.2.2:5005` / iOS-sim `localhost:5005` / prod `https://api.kadirli.app`; **`--dart-define=API_BASE_URL=` override'ı** (gerçek cihaz LAN testi için).
- [x] **Ortak bileşenler** (`core/widgets`): `AppButton` (primary/accent/ghost/danger × normal/small, loading, expand, 48dp, basılı 0.98 ölçek), `AppCard` (+`accentStripe`, `SectionHeader`), `AppScaffold` (AppBar + offline şerit + pull-to-refresh), `SkeletonBox`/`SkeletonCardList` (shimmer), `LoadingView`/`EmptyView`/`ErrorView`(traceId)/`OfflineBanner` — hepsi `widgets.dart` barrel'ından.
- **Bitti kriteri: ✅ karşılandı** — Android emülatöründe (API 37) uygulama açılıyor, Türkçe karakterler Nunito'da doğru, açık↔koyu tema anında değişiyor ve tercih yeniden açılışta korunuyor. `flutter analyze` **0 sorun**, `flutter test` **11/11 geçti**.
- **Plan dışı eklenenler (bilinçli):**
  - **`/gelistirici/tasarim` — yaşayan stil kılavuzu** (yalnız debug): palet, tipografi ölçeği, tüm buton/kart varyantları, dört durum ekranı tek sayfada. Token'ların iki temada da doğru göründüğü gözle doğrulanabiliyor.
  - **Test altyapısı** (plan yalnız `analyze` istiyordu): `test/app_smoke_test.dart` (açılış + font/renk + tema kalıcılığı), `test/core/config/env_test.dart`, `test/core/widgets/app_button_test.dart`.
  - **Paket kimliği `app.kadirli`** olarak sadeleştirildi (Flutter varsayılanı Android'de `app.kadirli.kadirli_app`, iOS'ta `app.kadirli.kadirliApp` üretiyordu → iki platform ayrışıyordu). Uygulama adı her iki platformda **"Kadirli"**. ⚠️ Mağaza yüklemesinden sonra değiştirilemez.
  - Türkçe locale sabitlendi (`tr_TR` + `flutter_localizations` + `initializeDateFormatting`), dikey yönlendirme kilidi, yazı ölçeği 0.9–1.4 arası sınırlandı (erişilebilirlik ile düzen bozulması dengesi), `SegmentedButton` teması yeşil tint'e çekildi (Material varsayılanı turuncu `secondaryContainer` kullanıyordu).
  - `mobile/README.md`: kurulum, base URL tablosu, dart-define örnekleri, klasör kuralları.
- **Karşılaşılan/çözülen 3 hata:** (1) `Material`'a hem `borderRadius` hem `shape` verilemez → yalnız `shape`; (2) `AppCard.accentStripe` `Row(stretch)` ile sonsuz yükseklik istiyordu → `Stack`+`PositionedDirectional`; (3) **`Container`'a `alignment` verilince gevşek kısıtta tüm genişliği kaplıyor** → butonlar ekranı kaplıyordu; `alignment` kaldırıldı, hizalama `Row`'a bırakıldı (regresyon testi yazıldı). Ayrıca devre dışı buton koyu temada okunmuyordu → nötr yüzey + muted metin.

### 11.2 — Ağ katmanı + kontrat modelleri — [x] ✅ TAMAMLANDI (29 Temmuz 2026)
- [x] **Dio kurulumu** (`core/network/dio_client.dart`): baseUrl (`Env.apiBaseUrl`), timeout'lar (bağlantı 15sn / alma-gönderme 20sn), JSON header'ları. **İKİ istemci üretir:** ana istemci `[AuthInterceptor, EnvelopeInterceptor, (dev log)]` + yardımcı istemci `[EnvelopeInterceptor]` (token yenileme ve 401 sonrası yeniden gönderim orada koşar → sonsuz döngü yok). Sıralama kritik: hata yolunda AuthInterceptor ham 401'i ÖNCE görür.
- [x] **Zarf açma** (`interceptors/envelope_interceptor.dart`): `success:true` → `response.data` = zarfın `data`'sı, `meta` `response.extra`'da taşınır (`ApiMetaAccess.apiMeta` uzantısı). `success:false` → HTTP 200 olsa bile `ApiException`. ⚠️ **Announcements quirk'i burada normalleşir** — üst katmanlar gerçek 404'ten ayırmak zorunda değil. Zarfsız yanıtlar (`/health`) dokunulmadan geçer.
- [x] **Hata sözlüğü** (`api_error_codes.dart` + `error_messages.dart` + `api_exception.dart`): 14 sunucu kodu + 4 istemci kodu (`NETWORK_ERROR`/`TIMEOUT`/`CANCELLED`/`UNEXPECTED_RESPONSE`). `ApiException` tek hata tipi: `code/message/traceId/statusCode/retryAfter` + `isNotFound`/`isConflict`/`isConnectionProblem`… yardımcıları; 429'da `Retry-After` header'ı okunur. **Mesaj önceliği:** sunucu mesajı (Türkçe ve daha spesifik) > sözlük > genel mesaj.
- [x] **Auth interceptor** (`interceptors/auth_interceptor.dart`): Bearer ekler; 401 → `/v1/auth/refresh` → **dönen YENİ refresh saklanır (rotasyon)** → istek bir kez tekrarlanır (`auth.retried` işareti, sonsuz döngü yok); refresh reddedilirse token'lar silinir + `onSessionExpired` sinyali. **Eşzamanlı 401'ler tek refresh'e düşer** (tek uçuşlu kilit — rotasyon yüzünden şart). Auth uçlarının (`login/verify-otp/register/refresh`) 401'i yenileme denemez.
- [x] **Token deposu** (`token_store.dart`): `TokenStore` arayüzü + `SecureTokenStore` (`flutter_secure_storage`, bellek önbellekli) + `InMemoryTokenStore` (test). `tempToken` saklanmaz (11.3'te bellekte taşınır).
- [x] **Ortak modeller** (`core/network/models/`): freezed `PagedResult<T>` (`genericArgumentFactories` + `hasNextPage`) ve `ApiMeta`. **build_runner ilk kez çalıştırıldı** (`dart run build_runner build`).
- [x] **Yardımcılar** (`core/utils/`): `AppImage.url/urls` (göreli → origin; mutlak URL'e dokunmaz), `AppDate` (tarih/saat/göreli zaman/`isoDay`/ulaşım `HH:mm`), `AppLinks` (`call`/`whatsapp`/`map`/`mapSearch`/`web`/`email`).
- [x] **Riverpod bağlantıları** (`network_providers.dart`): `tokenStoreProvider` · `dioProvider` · `apiClientProvider` · `sessionExpiredProvider` (11.3'te router dinleyip Giriş'e yönlendirecek — ağ katmanı yönlendirme yapmaz).
- **Bitti kriteri: ✅ karşılandı** — Android emülatöründe gerçek API'ye karşı 7/7 uç beklendiği gibi: `/v1/neighborhoods` 10 kayıt, `/v1/announcements?page=1&limit=3` PagedResult, `/v1/pharmacies/on-duty` boş liste, announcements quirk → NOT_FOUND+traceId, gerçek 404 → NOT_FOUND, `/v1/users/me` → UNAUTHORIZED, `/health` zarfsız geçti. `flutter analyze` **0 sorun**, `flutter test` **47/47** (36 yeni).
- **KARARLAR:**
  - **`AppClient` yerine ince `ApiClient`:** `get/post/put/patch/delete` ham `data` döndürür; `getObject/getList/getPaged` tipli ayrıştırır. Şekil beklenenden farklıysa sessiz `null` yerine **`UNEXPECTED_RESPONSE` hatası** (kontrat-model ayrışması görünür olsun).
  - **Tarihte sabit UTC+3** (`timezone` paketi yerine): Türkiye 2016'dan beri kalıcı +03, yaz saati yok. Sonuç cihazın saat diliminden bağımsız ("Kadirli saati") ve testler makineden bağımsız. Ofset değişirse tek sabit güncellenir.
  - **Çevrimdışıyken oturum düşürülmez:** refresh isteği bağlantı hatası alırsa token'lar SİLİNMEZ (yalnız sunucu reddettiyse) — kullanıcı metroda hesabından atılmasın.
- **⭐ PLANDIŞI (bilinçli):**
  - **`/gelistirici/ag` — Ağ tanılama ekranı** (debug-only, `/gelistirici/tasarim` deseninin eşi): açılışta 7 gerçek uç sorgular, her biri için süre/sonuç/`traceId` gösterir. Hem 11.2'nin canlı doğrulama aracı hem de sonraki fazlarda "veri gelmiyor" şikayetinde ilk bakılacak yer. Ana Sayfa → Geliştirici'den açılır.
  - **Kompakt `NetworkLogInterceptor`** (Dio'nun `LogInterceptor`'ı yerine): tek satır — yön, metot, yol, durum, süre. Liste uçlarında tüm gövdeyi basmak konsolu boğuyordu.
  - **Teknik sunucu mesajı filtresi:** canlı testte `GET /v1/ads/{yok}` **İngilizce/teknik** mesaj döndü — `Entity "Ad" (guid) was not found.` (backend `NotFoundException`'ın genel biçimi, `KadirliApp.Application/Common/Exceptions/NotFoundException.cs`). Kullanıcıya gösterilemez. Kontrat dondurulduğu için (kural 3) backend'e DOKUNULMADI; istemci bu kalıbı eleyip sözlükteki Türkçe mesajı gösteriyor. Handler'ların yazdığı özel Türkçe mesajlar ("Duyuru bulunamadı.") etkilenmiyor. **📌 Backend'e not (11'den sonra):** `NotFoundException` mesajı Türkçeleştirilirse istemcideki filtre gereksizleşir.
  - **Testlerde sahte HTTP adaptörü** (`test/core/network/fake_http_adapter.dart`): `http_mock_adapter` bağımlılığı eklemeden Dio'nun `httpClientAdapter`'ı değiştirildi → **tüm interceptor zinciri gerçekteki gibi koşuyor** (401→refresh→tekrar dahil). `DioClient.create`'e testler için `adapter` parametresi eklendi.
- **Karşılaşılan/çözülen 2 tuzak:** (1) `DioExceptionType` switch'i `transformTimeout`'u da kapsamalı (dio 5.9 exhaustive uyarısı); (2) named parametre private alana (`this._tokenStore`) atanamıyor → `prefer_initializing_formals` uyarısını gidermek için alanlar public yapıldı.

### 11.3 — Kimlik doğrulama akışı (uçtan uca) — [x] ✅ TAMAMLANDI (30 Temmuz 2026)
- [x] **Bootstrap/Splash** (`features/auth/presentation/splash_screen.dart` + `AuthController.bootstrap`): token var mı → `GET /v1/users/me` ile geçerlilik → Ana Sayfa; yoksa Giriş. Splash `context.go` ÇAĞIRMAZ — durumu değiştirir, yönlendirmeyi router yapar.
- [x] **Giriş ekranı:** ulusal 10 hane girişi (`+90` sabit ön ek + `532 111 00 01` maskesi) → `POST /v1/auth/login` → kod ekranı (6 hane, `retryAfter` sayaçlı "tekrar gönder"; dev modda kod otomatik dolu).
- [x] **Doğrulama:** `POST /v1/auth/verify-otp` → `isNewUser:false` → token'lar `SecureTokenStore`'a, Ana Sayfa; `isNewUser:true` → Kayıt ekranı (`tempToken` **yalnız bellekte**). Kod 6 haneye ulaşınca otomatik doğrulanır.
- [x] **Kayıt:** kullanıcı adı + mahalle (`GET /v1/neighborhoods` dropdown) + opsiyonel yaş → `POST /v1/auth/register` → token sakla → Ana Sayfa. Sunucu kuralları önden uygulanır (3-30 karakter/boşluksuz, yaş 13-120); sunucu hatası ilgili **alanın altına** yerleştirilir (kontrat alan adı bildirmediği için mesaj içeriğine bakılır; eşleşmezse form üstü genel uyarı).
- [x] **Auth state** (`authControllerProvider`): `AuthState` = **sealed class** (unknown/anonymous/registering/authenticated) — freezed kullanılmadı (JSON'a çevrilmeyen durum makinesi için Dart 3 `sealed` yeterli, kod üretimi bedava kazanç değil). `logout()` = `POST /v1/auth/logout` + token/önbellek temizliği. `go_router` redirect'i tek karar noktası; `sessionExpiredProvider` (11.2 sinyali) buraya bağlandı → yenileme reddedilince anonime düşer + "Oturumunuzun süresi doldu" bilgisi Giriş ekranında görünür.
- [x] FCM token kaydı **stub'ı** (`features/notifications/data/fcm_token_service.dart`): giriş sonrası `registerAfterLogin()` çağrılıyor; `deviceFcmTokenProvider` bugün **null** döndüğü için uca istek gitmiyor. 11.13'te YALNIZ bu provider `FirebaseMessaging.getToken()` ile override edilecek — çağıran kod değişmeyecek.
- **Bitti kriteri: ✅ karşılandı** (Android emülatörü + çalışan API, canlı): yeni telefon → OTP → kayıt (**kullanıcı kendi eliyle**: `+905555555555` / `atahanblcr` / yaş 25, DB'de doğrulandı) · kayıtlı kullanıcı (`+905321110002 aysedmr`) → kayıt ekranı atlanarak **direkt giriş** · uygulama force-stop + yeniden açılış → **oturum korundu** · çıkış → onay diyaloğu → Giriş ekranı + DB'de `fcm_token` NULL'landı · hatalı kod → alan altında "Geçersiz veya süresi dolmuş OTP." + ekran değişmiyor · misafir devam → Ana Sayfa + tercih kalıcı · "Profilim" (korumalı aksiyon) → nazik giriş daveti. `flutter analyze` **0 sorun**, `flutter test` **88/88** (41 yeni), `dotnet test` **65/65**.
- **KARARLAR:**
  - **Misafir gezinme birinci sınıf:** uygulamanın çoğu içeriği anonim okunabildiği için Giriş ekranı zorunlu kapı değil, **davet**. Router kuralı: "misafir olarak devam" demeyen anonim kullanıcı Ana Sayfa'da tutulmaz (ilk açılış + **çıkış sonrası** aynı kuralla Giriş'e döner; çıkışta misafir tercihi de sıfırlanır) — plandaki "çıkış → login'e döner" kriteri böyle karşılandı.
  - **Telefon normalizasyonu tek yerde** (`core/utils/phone.dart`): sunucu OTP'yi **ham telefon string'iyle** anahtarlıyor → istemci her zaman `+90` önekli E.164 gönderir (biçim tutarsızlığı aynı kullanıcıya iki OTP kaydı açardı). Girdi `0532…`/`+90 532…`/`905…` hepsini 10 haneye indirir; 10 hane doluyken yeni hane yok sayılır (maske yazarken kaymasın).
  - **Profil önbelleği** (`shared_preferences`, `auth.cachedUser`): çevrimdışı açılışta oturum düşmüş görünmüyor ve selamlama ilk karede dolu. Sunucu 401/403 verirse önbellek + token silinir; **bağlantı hatasında dokunulmaz** (11.2 kararının devamı).
  - `CurrentUser` modeli **kısmi** (`MyProfileDto`'nun oturum için gereken alanları). Bilinmeyen JSON alanları yok sayıldığı için güvenli; bildirim tercihleri/değişiklik tarihleri 11.5'te eklenecek.
- **⭐ PLANDIŞI (bilinçli):**
  - **`AppTextField`** ortak metin girişi bileşeni (tasarım sisteminde yoktu; 11.5'ten itibaren tüm formlar bunu kullanacak): üstte 12sp etiket, zorunlu `*`, yardımcı/hata metni, sabit ön ek. ⚠️ Material'ın `prefix`/`prefixText`'i **yalnız odaklıyken** görünüyor → ülke kodu görünmüyordu; ön ek `prefixIcon` yuvasına taşındı (canlı testte yakalandı ve düzeltildi).
  - **`InfoBanner`** (info/success/warning/danger) — kısmi mesajlar için (`ErrorView` tüm ekranı kaplıyor): oturum bildirimi, dev modu notu, form üstü hata.
  - **`ensureSignedIn` + `showLoginRequiredSheet`** (MOBILE_UX_PLAN §7 "Yetki gerekli"): korumalı aksiyonların tek kapısı; 11.4+ favori/ilan ver/kod gör/taksi ara hepsi bununla başlayacak.
  - **`features/lookups/`** (mahalle listesi repository + `neighborhoodsProvider`, `keepAlive`) — 11.5 (profil mahallesi), 11.10/11.11 (kategori/mezarlık/cami) aynı deseni büyütecek.
  - **Test altyapısı `test/helpers/pump_app.dart`**: gerçek uygulamayı (router + redirect + tüm interceptor zinciri) sahte HTTP adaptörüyle ayağa kaldırır. ⚠️ Splash'teki `CircularProgressIndicator` sonsuz animasyon olduğu için `pumpAndSettle` kilitleniyor → `settleApp()` yardımcısı. 41 yeni test: telefon normalizasyonu, repository'nin **gerçek yanıt gövdeleriyle** eşleşmesi, durum makinesi (çevrimdışı/401/çıkış/oturum düşmesi), uçtan uca 10 widget senaryosu.
  - **Ana Sayfa "Hesap" kartı** (geçici): oturum özeti + çıkış / misafirken giriş daveti — 11.3'ün akışını elle denenebilir kılmak için; 11.4-11.5'te yerini sekme kabuğu + Profil ekranı alacak.
  - 🐛 **BACKEND'E DOKUNULDU (bilinçli tek istisna, yalnız loglama):** canlı testte hatalı OTP istemciye doğru şekilde `400 INVALID_OTP` dönüyordu ama Serilog/Seq'e **`500 ERR`** olarak düşüyordu — `Program.cs`'te `ExceptionMiddleware` `UseSerilogRequestLogging`'den ÖNCE (dışta) kayıtlıydı, Serilog istisnayı "yakalanmamış" görüp 500 yazıyordu. İki satır yer değiştirdi (Serilog en dışta) → artık `400 INF`. Yanıt gövdesi/kontrat DEĞİŞMEDİ; gerçek 500'lerin istisna ayrıntısı hâlâ `ExceptionMiddleware.LogError`'dan geliyor. Doğrulandı: curl ile 400 + log satırı, `dotnet test` 65/65.
  - **`API_CONTRACT.md` §4 düzeltildi:** doküman `expiresInSeconds`/`retryAfterSeconds`/`devOtp` diyordu; `AuthController.Login` gerçekte `{message, expiresIn, retryAfter, otp}` döndürüyor (canlı doğrulandı). Model gerçek yanıta göre yazıldı, doküman güncellendi.
- **Karşılaşılan/çözülen tuzaklar:** (1) `Override` tipi `flutter_riverpod` 3'ten export EDİLMİYOR → test yardımcısında override listesi parametresi kaldırıldı; (2) Dio'nun `RequestOptions.data` adaptör seviyesinde **Map** (JSON string değil) → gövde iddiaları doğrudan Map ile yazıldı; (3) `AsyncData(:final value)` deseninde alan adı çakışması → `AsyncData(value: final items)`; (4) tema testi "Hesap" kartı eklendiği için kaydırma gerektirdi + kaydırınca selamlama liste dışına çıkıyor (çapa widget değişti).

### 11.4 — Uygulama kabuğu + Ana Sayfa (Hub) — [x] ✅ TAMAMLANDI (30 Temmuz 2026, 2. oturum)
- [x] **Alt sekme kabuğu** (`core/router/app_shell.dart` + `StatefulShellRoute.indexedStack`): 4 sekme — Ana Sayfa / İlanlar / Bildirim(rozet) / Profil. Her dalın kendi `Navigator`'ı → kaydırma konumu ve ekran durumu korunur; aynı sekmeye tekrar dokunmak o dalın köküne döner (`goBranch(initialLocation: true)`). Modül ekranları bilinçli olarak **kabuğun dışında** (hub'dan modüle girmek "içeri girmek"tir, geri tuşu hub'a döner — mockup deseni).
- [x] **Ana Sayfa (Hub)** (`features/home/presentation/home_screen.dart`): **saate göre selamlama** (Günaydın/İyi günler/İyi akşamlar/İyi geceler + kullanıcı adı, Kadirli saatiyle) + sağ üst ⚙️ → Ayarlar. **Acil şerit** (`widgets/emergency_strip.dart`): "ŞU AN KADİRLİ" başlığı altında iki satır — bugün nöbetçi eczane (`GET /v1/pharmacies/on-duty`; birden fazlaysa "+N") ve kesinti durumu (`GET /v1/power-outages`); satırlar **birbirinden bağımsız** yükleniyor/boş/dolu/hata durumuna girer, dokununca ilgili modüle gider, hatada satıra dokunmak tekrar dener. **Modül ızgarası** (`widgets/module_grid.dart`): 4 sütun × **12 modül**. **Öne çıkan:** son 3 duyuru (`GET /v1/announcements?limit=3`) + "Tümü".
- [x] Boş/yükleniyor(skeleton)/hata(tekrar dene) durumları; pull-to-refresh **dört kaynağı birden** tazeler (üç hub isteği + bildirim rozeti) ve hepsi bitene kadar bekler.
- **Bitti kriteri: ✅ karşılandı** (Android emülatörü Pixel_9 + çalışan API, canlı): şeritte gerçek veri ("Bugün nöbetçi: Merkez Eczanesi" ve süren kesinti **kırmızı** "Savrun · kesinti sürüyor · 18:21'ye kadar"), nöbet/kesinti yokken sakin metin ("henüz girilmedi" / "Planlı elektrik kesintisi yok" — boş ≠ hata), 12 modül kartı dokununca kendi ekranını açıyor, sekmeler arası geçiş yeniden istek atmıyor, giriş sonrası selamlama kişiselleşiyor (`aysedmr`), rozet DB'ye eklenen 2 okunmamış bildirimi turuncu gösteriyor, misafir Bildirim/Profil sekmesinde davet görüyor, ⚙️→Ayarlar→Koyu tema anında değişiyor. `flutter analyze` **0 sorun**, `flutter test` **120/120** (32 yeni).
- **KARARLAR:**
  - **Sekmeler `protectedPrefixes`'e YAZILMADI** (plan öyle diyordu — bilinçli sapma): Bildirim/Profil sekmesine dokunan misafiri router'la giriş ekranına atmak sekme kabuğunu kapatıyor ve 11.3'ün "misafir gezinme birinci sınıf" kararıyla çelişiyordu. Yerine sekmenin **içinde** `SignInPrompt` daveti gösteriliyor (MOBILE_UX_PLAN §7): alt sekme çubuğu yerinde kalır, kullanıcı vazgeçebilir. `protectedPrefixes` mekanizması duruyor — 11.9 "ilan ver" gibi sekme dışı gerçek korumalı ekranlar için.
  - **Modül kaydı tek liste** (`core/navigation/app_modules.dart` → `kAppModules`): id/etiket/ikon/rota/**hangi fazda geleceği**/özet/**bağlanacağı uçlar**. Izgara, rotalar ve "yakında" ekranları hep bu listeden üretiliyor → yeni modül eklemek tek satır, "işlevsiz buton yok" şartı ise **test edilebilir** hale geldi.
  - **Hub üç ayrı provider** (`onDutyPharmaciesProvider` / `relevantOutagesProvider` / `latestAnnouncementsProvider`), tek "hub isteği" değil: eczane ucu patlarsa duyurular yine görünür. `autoDispose` yok → sekme değiştirip dönmek yeniden istek atmıyor.
  - **Kesinti filtresi istemcide:** `GET /v1/power-outages` **tüm** kayıtları döner (tarih filtresi/sayfalama yok, backend dondurulmuş) → süren/gelecek ayrımını `PowerOutage.isActive/isUpcoming/isRelevant` yapıyor, en yakın önce sıralanıyor. Süren kesinti `danger`, planlı olan vurgulu, hiçbiri yoksa sakin metin.
  - **Bildirim rozeti anonimde istek atmaz** (`unreadNotificationCountProvider` oturum yoksa 0 döner — uç `[A]`, boşuna 401 üretmenin anlamı yok); `unreadCount` sayfalı gövdenin **içinden** okunuyor (Faz 10.10 kontrat özelliği), `limit=1` ile en küçük yanıt istenir.
- **⭐ PLANDIŞI (bilinçli):**
  - **`ModulePlaceholderScreen`** — yazılmamış 12 modülün gerçek ekranı: modül adı/ikonu, bir cümlelik özet, "bu bölüm 11.x sürümüyle açılacak" bilgisi, **debug modda bağlanacağı uçların listesi**. Ölü/gri buton yok; aynı zamanda yaşayan bir yapılacaklar panosu.
  - **`SettingsScreen` iskeleti** (`/ayarlar`): plan ⚙️'nin çalışmasını istiyordu ama Ayarlar ekranı 11.5'in işi → Ana Sayfa'daki geçici "Hesap" kartı + tema seçici + geliştirici kısayolları buraya taşındı (Hesap/çıkış · Görünüm/tema · Hakkında + Şikayet kısayolu · Geliştirici). 11.5 bunun üstüne profil düzenleme, 6 bildirim anahtarı ve hesap silmeyi ekleyecek.
  - **`ProfileScreen` iskeleti** (Profil sekmesi): avatar + ad/telefon/mahalle + "Ayarlar ve kontrol / İlanlarım / Favorilerim" satırları (son ikisi "Yakında" etiketli, İlanlar sekmesine gider).
  - **`apiRetry` politikası** (`core/network/retry_policy.dart`): ⚠️ **Riverpod 3 hata veren provider'ları kendiliğinden, sınırsız yeniden deniyor** — 404/401/400 gibi tekrarlanınca da aynı sonucu verecek hatalarda bu boşuna istek (pil + sunucu) ve testlerde sönmeyen zamanlayıcı demek. Yeni politika yalnız **geçici** hatalarda (bağlantı/timeout/5xx/429) en fazla 2 tekrar yapıyor, 429'da `Retry-After`'a uyuyor; kalıcı hatada kullanıcıya "Tekrar dene" düğmesi kalıyor.
  - **`AppNetworkImage`** (`core/widgets/`): uzak görsellerin tek bileşeni — URL'i `AppImage.url` ile mutlaklaştırır (çağıran unutamaz), yüklenirken skeleton, hata/boş URL'de nötr ikon, `cached_network_image` ile kaydırmada tekrar indirmez. 11.6+ tüm görselli kartlar bunu kullanacak.
  - **`AnnouncementTile`** (11.6 listesinde de kullanılacak): öncelik şeridi + **metinli** "Acil"/"Önemli" etiketi (renk körü kullanıcı için renk tek başına yetmez), tür adı, göreli zaman, opsiyonel görsel.
  - **`SignInPrompt`** ortak bileşeni + **32 yeni test** (toplam 120): hub durumları (nöbet var/yok, kesinti süren/geçmiş/yok, duyuru listesi, tek uç patlarken diğerlerinin çalışması, pull-to-refresh sayacı, sekme geçişinde yeniden istek atmama), 3 modelin gerçek sunucu gövdeleriyle ayrıştırılması + sınır durumları, rozet (anonimde istek yok / sayı / 99+), retry politikası ve **"her modül kartı açılabilir bir ekrana gider" denetim testi**.
  - **`test/helpers/pump_app.dart` → `homeStubs()`**: hub'ın 4 ucu için varsayılan sahte yanıtlar; başka konuya odaklanan testler (auth/tema) bunu serperek 404 gürültüsünden kurtuluyor.
- **Karşılaşılan/çözülen 4 tuzak:**
  1. 🐛 **`SkeletonCardList` başka bir kaydırılabilir alanın içinde patlıyordu** ("Vertical viewport was given unbounded height") — Ana Sayfa'nın `ListView`'ı içindeki duyuru vitrini **yükleniyor** durumuna girdiği anda ekran çöküyordu. Bileşene `shrinkWrap`+`padding` parametreleri eklendi (testte yakalandı, canlıda görülmeden düzeldi).
  2. **`ListView` tembel kurulum:** vitrin ekranın altında kaldığı için provider açılışta hiç tetiklenmiyordu (istek atılmıyordu) → hub "üç isteklik tek ekran" davranmalı, `HomeScreen.build` duyuruları da izliyor.
  3. **Riverpod 3 API:** `AsyncValue.valueOrNull` yok → `.value`; `FutureProvider(..., retry:)` ile otomatik tekrar kapatılabiliyor.
  4. **Canlı kontrolde yakalanan görsel hata:** Kampanya'nın `local_offer` ikonu İlanlar'ın `sell` ikonuyla neredeyse aynı görünüyordu → bilet ikonu (`confirmation_number`, mockup'taki 🎟️).
- **🍎 iOS İLK KEZ DERLENDİ VE ÇALIŞTI** (11.1'den beri açık olan risk kapandı):
  - **Kurulum:** kullanıcı Xcode + iOS simülatörünü kurdu ve `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer` + lisans/first-launch adımlarını yaptı. **CocoaPods `brew install cocoapods` ile kuruldu (sudo GEREKMEDİ — Homebrew mevcut; `sudo gem install cocoapods` önerisi gereksizdi).** `pod --version` 1.17.0.
  - **Sonuç:** `pod install` 5,3 sn'de geçti (ek yapılandırma gerekmedi), `Xcode build done` **169,7 sn**, uygulama **iPhone 17 simülatöründe (iOS 26) açıldı**. Splash → (token yok) → Giriş yönlendirmesi çalıştı; yani router + `AuthController.bootstrap` + `flutter_secure_storage` iOS'ta da sorunsuz. **Nunito** ve palet doğru; `xcrun simctl ui … appearance dark` ile **koyu tema** anında ve doğru renklerle geldi. Derleme uyarısı/hatası yok, kod tarafında **iOS'a özel tek satır değişiklik gerekmedi**.
  - **⚠️ iOS'ta HENÜZ GÖRÜLMEYEN:** Ana Sayfa hub'ının kendisi ve sekme kabuğu. Sebep: simülatörde programatik dokunma yok — `osascript`/System Events erişilebilirlik izni istiyor (kullanıcı onayı gerektirir), `NSUserDefaults` plist'ine "misafir devam" tercihini enjekte etme denemesi de `cfprefsd` önbelleği yüzünden tutmadı. **Tek dokunuşla görülebilir:** simülatörde "Misafir olarak devam et"e basmak yeterli. iOS'a özgü bakılacak yerler: `SafeArea` alt boşluğu + `NavigationBar` yüksekliği (sekme çubuğu home-indicator ile çakışıyor mu), `CupertinoPageTransition` geçişleri.
  - 📌 **Sonraki oturumlar için:** iOS artık çalıştırılabilir → cila fazından (11.15) önce her fazın sonunda **iki platformda** bakılabilir. `flutter run -d <iphone-udid>`; ilk derleme uzun, sonrakiler hızlı.

### 11.5 — Ayarlar/Kontrol + Profil (users/me) — [x] ✅ TAMAMLANDI (31 Temmuz 2026)  ★ kullanıcının özel isteği
- [x] **Profil sekmesi + Ayarlar ekranı** (sağ üst ⚙️'den de gelir). Ayarlar bölümleri: **Hesap** (avatar+ad+telefon+mahalle özeti + "Profili düzenle") · **Bildirimler** (6 anahtar) · **Görünüm** (tema) · **Hakkında** (sürüm + Şikayet/İstek) · **Hesap işlemleri** (Çıkış + Hesabı sil) · Geliştirici. Profil sekmesi: avatar/ad/telefon/mahalle + "… tarihinden beri aramızda" + Ayarlar/Bildirim tercihleri/İlanlarım/Favorilerim satırları + pull-to-refresh (`users/me` tazeler).
- [x] **Profil düzenleme** (`features/profile/presentation/profile_edit_screen.dart`, rota `/profil/duzenle`): `PATCH /v1/users/me` — kullanıcı adı / mahalle / yaş / profil fotoğrafı. **Yalnız değişen alan gönderilir** (PATCH semantiği); hiçbir şey değişmediyse istek atılmaz. Fotoğraf: `image_picker` (galeri/kamera/kaldır sayfası) → `POST /v1/files/upload` (`moduleType=profile`) → dönen id `profilePhotoFileId` olarak PATCH'e. ⚠️ **30 gün kuralı önden uygulanır:** `usernameLastChangedAt`/`neighborhoodLastChangedAt`'e göre alan **kilitlenir** ve "X gün sonra tekrar değiştirebilirsiniz" yazar → kullanıcı formu doldurup sunucudan ret yemez; yine de sunucu `USERNAME_CHANGE_LIMIT`/`NEIGHBORHOOD_CHANGE_LIMIT` derse mesaj **ilgili alanın altına** düşer (kod → alan eşlemesi, mesaj metnine bakmadan).
- [x] **Bildirim tercihleri:** 6 anahtar (`NotificationTopic` enum'undan üretilir) → `PATCH /v1/users/me/notifications`. **Kaydet butonu yok**: her anahtar dokunulduğu an **iyimser** güncellenir, istek arkada gider, sunucu reddederse eski değere döner + sebep şeritte gösterilir. Yanıttaki TÜM tercihler yazılır → başka cihazdaki değişiklik senkronlanır.
- [x] **Tema:** Açık/Koyu/Sistem segmenti (11.1'den, `shared_preferences`).
- [x] **Hesap:** Çıkış · **Hesabı Sil** (`DELETE /v1/users/me`) · Hakkında (**sürüm `package_info_plus` ile**, yayın fazında (11.16) elle güncelleme gerekmez) + Şikayet/İstek kısayolu.
- **Bitti kriteri: ✅ karşılandı** (Android emülatörü Pixel_9 + çalışan API, canlı): profil düzenleme mahalleyi değiştirdi (DB'de `neighborhood_last_changed_at` doldu) ve alan **anında kilitlendi** ("30 gün sonra…") · yaş için yerel doğrulama sunucuya istek attırmadı · bildirim anahtarı DB'ye yazıldı (`{"Ads": true …}`) · **uçak modunda** anahtar geri alındı + "İnternet bağlantısı kurulamadı" şeridi · misafirde bildirim/hesap bölümleri hiç görünmüyor · profil fotoğrafı galeriden yüklendi (`/uploads/…` DB'de, avatar ekranda) ve "Fotoğrafı kaldır" ile silindi · **hesap silme uçtan uca**: uygulamadan yeni test hesabı açıldı (`+905443322110`), silindi (DB'de `del…` anonimleştirme + `is_active=false`), Giriş ekranına dönüldü ve **aynı numarayla yeniden kayıt olunabildi**. `flutter analyze` **0 sorun**, `flutter test` **160/160** (40 yeni), `dotnet test` **65/65**, **iOS simülatör derlemesi geçti** (yeni `package_info_plus` pod'u sorunsuz).
- **KARARLAR:**
  - **Kısıtı sunucuya sormadan göster:** 30 günlük kilit istemcide `CurrentUser.canChangeUsername/DaysLeft` ile hesaplanıyor (sabit `changeCooldownDays = 30`, sunucudaki `UpdateMyProfileCommandHandler` sabitiyle aynı). Kalan süre **yukarı yuvarlanır** (3 saat kaldıysa "1 gün") — kullanıcıya erken umut vermemek için.
  - **Tercihlerin tek kaynağı oturum kullanıcısı:** `NotificationPreferencesController` tercihleri kendi tutmuyor, `CurrentUser.notificationPreferences`'ı güncelliyor (`AuthController.applyProfile`) → Ayarlar, Profil ve önbellek tek yerden besleniyor.
  - **PATCH yanıtı = güncel profil** → başarılı kaydetmeden sonra ek `GET /v1/users/me` atılmıyor.
  - **Fotoğraf kaydederken yükleniyor** (seçer seçmez değil): kullanıcı vazgeçerse sunucuda yetim dosya kalmıyor.
  - **Hesap silme diyalog değil ayrı ekran** (`/ayarlar/hesabi-sil`): mağaza şartı "bulunabilir ve anlaşılır" olmasını istiyor → ne silineceği/ne kalacağı madde madde yazılı, üstüne bir de onay diyaloğu. Yönetici/personel hesabında buton **baştan kapalı** (kullanıcı `SELF_DELETE_FORBIDDEN` hatasına hiç çarpmıyor).
  - **Silmeden sonra `logout` ucu çağrılmaz** (hesap zaten pasif, 401 dönerdi); refresh token gövdeye konur, sunucu jti'yi iptal eder.
- **⭐ PLANDIŞI (bilinçli):**
  - **`UserAvatar` + `UserIdentityRow`** (`core/widgets/`): fotoğraf varsa yuvarlak görsel, yoksa baş harf (**Türkçe kuralı: `i` → `İ`**); `onTap` verilince "fotoğraf değiştir" rozeti çıkar. Ayarlar/Profil/düzenleme aynı bileşeni kullanıyor, 11.9+ ilan sahibi satırları da kullanacak.
  - **`features/files/` — ortak yükleme repository'si** (`POST /v1/files/upload`, multipart, alan adı `file`): 11.5 profil fotoğrafı için yazıldı ama 11.9 (ilan görselleri) ve 11.11 (vefat fotoğrafı) doğrudan bunu kullanacak.
  - **`package_info_plus` + `core/config/app_info.dart`**: Ayarlar → Hakkında'da sürüm/build `pubspec`'ten okunuyor (platform kanalı yoksa "—", ekran düşmüyor).
  - **`SectionHeader`'a `subtitle`** (bölümün ne işe yaradığını yazmak için).
  - **`AppRoutes.protectedPrefixes` ilk kez dolu**: `/profil/duzenle` + `/ayarlar/hesabi-sil` (⚠️ `startsWith` eşleşmesi → `/profil` sekmesi takılmıyor).
  - **40 yeni test** (toplam 160): tam `MyProfileDto` ayrıştırması + bayat/kısmi önbellek gövdesi, 30 gün kısıtının sınır durumları, PATCH gövdesinin **yalnız değişenleri** taşıması, bildirim anahtarının iyimser güncelleme + geri alma davranışı, multipart yükleme, ekran testleri (kilitli alan, hatanın alan altına düşmesi, misafir görünümü, hesap silme onayı/yasağı).
- **🐛 CANLI TESTTE YAKALANAN GERÇEK HATA (ve kardeşi):** `context.push` ile açılan bir ekran **router'ın redirect'inin ÜSTÜNDE kalıyor** — redirect alttaki konumu değiştiriyor ama kullanıcı hâlâ eski ekranı görüyor. Üç yerde ortaya çıktı: (1) profil kaydedince ekran kapanmıyordu, (2) **hesap silindikten sonra sonsuz spinner** (kullanıcı uygulamada kilitleniyordu), (3) ⚠️ **11.3'ten beri var olan hata: kayıtlı kullanıcı giriş yapınca boşalmış kod ekranında sıkışıyordu** (yeni kullanıcıda kayıt ekranına geçtiği için fark edilmemişti). Çözüm deseni: durum değiştikten **bir kare sonra** (`addPostFrameCallback`) ekranı yığından çek — `context.pop()`; oturum kapandığı durumda `context.go(AppRoutes.home)` ile yığın komple değiştirilip karar yine router'a bırakılıyor. Üçü için de regresyon testi yazıldı (eski testler "ekran üstte kaldı mı" diye sormadığı için hatayı göremiyordu — `find.text` alttaki sayfayı da buluyor).
- **Karşılaşılan diğer tuzaklar:** `DropdownButtonFormField`'in `helperText`'i varsayılan **1 satır** → yardımcı metin "…" ile kesiliyordu (`helperMaxLines`); mahalle adına " Mahallesi" eklemek yanlış ("Yenimahalle Mahallesi", ileride köyler) → ad olduğu gibi + 📍 ikonu; `MapEntry` yapısal eşitlik taşımıyor (multipart alan testi); Ayarlar büyüyünce testlerdeki "Çıkış yap" ekran dışında kaldı (`scrollUntilVisible`).

### 11.6 — Duyurular + Elektrik Kesintileri — [x] ✅ TAMAMLANDI (31 Temmuz 2026, 4. oturum)
- [x] **Duyurular listesi** (`features/announcements/`, rota `/duyurular`): `GET /v1/announcements?page=&limit=&typeId=`, **sonsuz kaydırma** (liste sonuna 400px kala sonraki sayfa), tür filtresi chip'leri (`GET /v1/announcements/types`), pull-to-refresh, yükleniyor/boş/hata durumları + liste sonunda "Toplam N duyuru".
- [x] **Duyuru detayı** (`/duyurular/:id`): `GET /v1/announcements/{id}` (⚠️ 200+success:false NOT_FOUND zarf interceptor'ında normalleşiyor → ekran `isNotFound`'a bakıp "kaldırılmış olabilir" der, "Tekrar dene" **göstermez**); açılıp **yüklendiğinde** `POST …/{id}/view` (bir kez); dış bağlantıya dokununca `POST …/{id}/click` → sonra tarayıcı; öncelik şeridi, tür, tarih, gövde, konum kartı (harita), kaynak, paylaş.
- [x] **Elektrik kesintileri** (`features/power_outages/`, rota `/kesintiler` + `/kesintiler/:id`): Güncel/Geçmiş sekmesi (sayı rozetli), **Şu an sürüyor / Planlanan** başlıkları, dakikada tazelenen geri sayım, "Sadece \<mahallem\>" filtresi, detayda başlangıç/bitiş/süre/sebep + paylaş.
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + çalışan API, canlı; geçici veriyle): 27 duyuru **iki sayfada** sonsuz kaydırmayla yüklendi · tür filtresi `?typeId=` gönderdi ve liste süzüldü · detay açılınca DB'de `view_count` 0→1, "Bağlantıyı aç" ile `click_count` 0→1 · kesinti listesinde Güncel (3)/Geçmiş (2), süren kesinti kırmızı "Bitmesine 1 sa 18 dk", planlı "4 sa 58 dk sonra başlıyor" · mahalle filtresi açılınca yalnız **şehir geneli** kaldı + "4 kesinti mahalle filtresi yüzünden gizli." · paylaşım sayfası Kadirli saatiyle doğru metni verdi. `flutter analyze` **0 sorun**, `flutter test` **211/211** (51 yeni), **iOS simülatör derlemesi geçti** (yeni `share_plus` pod'u sorunsuz).
- **KARARLAR:**
  - **Sayfalı liste `AsyncValue` değil kendi durumu** (`AnnouncementFeedState`): sonsuz kaydırmada üç ayrı yükleme hâli var (ilk sayfa / sonraki sayfa / tazeleme) ve **ikinci sayfa hatası okunan içeriği silmemeli** — `AsyncValue` tek bir loading/error taşıdığı için bunu ifade edemiyor. Sayfa hatasında liste kalır, altta "Devamını yükle" çıkar.
  - **Tür filtresi ayrı provider değil aynı denetleyicide**: tür değişimi listeyi *sıfırlayan* bir işlem; geç dönen eski yanıt `_requestId` ile eleniyor (yarış durumu). Aynı türe tekrar dokunmak filtreyi kaldırır.
  - **Sayfalar arası mükerrer kayıt id'ye göre eleniyor** — araya yeni duyuru girince sunucu aynı kaydı iki sayfada döndürebilir.
  - **Hub kendi kesinti isteğini atmıyor**: `relevantOutagesProvider` artık 11.6'nın `allPowerOutagesProvider`'ından türüyor (uç zaten tek istekte tüm kayıtları döndürüyor) → hub'dan modüle girmek **ikinci ağ isteği doğurmuyor**; `refreshHome` kaynağı tazeliyor.
  - **Kesinti gruplaması/filtresi tamamen istemcide** (uç sayfalamıyor + tarih filtrelemiyor — 10.x'te bilinçli donduruldu): süren/planlı/geçmiş ayrımı, sıralama, geri sayım ve mahalle filtresi `PowerOutageGroups.from`'da.
  - **Mahalle filtresi şehir genelini ELEMEZ**: mahallesi boş kayıt herkesi ilgilendirir; gizlenen kayıt sayısı ekranda yazılır ("N kesinti mahalle filtresi yüzünden gizli") → kullanıcı listeyi boş görüp "veri yok" sanmaz. Eşleşme **ad üzerinden** (uç mahalle id'si döndürmüyor), büyük/küçük harf ve boşluğa duyarsız.
  - **Tür renginin sınırlı kullanımı:** sunucunun hex rengi yalnız **seçili olmayan** chip'in ikon tonunda; seçili chip tamamen temanın birincil rengi → yönetici çok açık bir renk seçse de okunabilirlik ve koyu tema bozulmaz. FontAwesome sınıfı (`fa-bolt`) Material ikonuna eşleniyor, bilinmeyen ad nötr ikona düşüyor (yeni paket eklenmedi).
  - **Görüntülenme yalnız başarılı yüklemeden sonra sayılır** (silinmiş duyurunun bağlantısı istatistiği kirletmesin); tıklama sayacı **bağlantıyı bekletmiyor** (ateşle-unut).
  - **Tür listesi patlarsa filtre şeridi hiç çizilmez** — çalışmayan bir filtre "işlevsiz buton" olurdu; duyurular yine okunur.
- **⭐ PLANDIŞI (bilinçli):**
  - **`share_plus` + `core/utils/app_share.dart`** — Kadirli'de bilgi WhatsApp gruplarından yayılıyor; duyuru ve kesinti tek dokunuşla paylaşılıyor (emoji'li, Kadirli saatiyle biçimlenmiş metin). iPad popover çapası için `AppShare.originOf`. **11.8+ (ilan/etkinlik/vefat paylaşımı) doğrudan bunu kullanacak.**
  - **`AppDate.range` + `AppDate.duration`** — aralık aynı güne düşerse tek tarih ("12 Ağustos 2026, 09:00 – 15:00"), yayılırsa iki tam tarih; süre "2 sa 30 dk"/"1 gün 4 saat". ⚠️ Gün ayrımı **Kadirli saatine** göre (UTC'ye göre farklı günde olan iki an aynı güne düşebiliyor — teste bağlandı). Süre **yukarı yuvarlanır** ("0 dakika kaldı" yazan sayaç bozuk görünür).
  - **Detay rotaları alt rota** (`/duyurular/:id`, `/kesintiler/:id`) + `AppRoutes.announcementDetail/powerOutageDetail` üreteçleri → **11.13 push deep-link hedefi hazır** (bildirimin `data.relatedId`'si doğrudan buraya gider).
  - **Hub vitrini artık doğrudan detaya gidiyor** (listeye değil — okumak istediği duyuruya).
  - **`PowerOutageTile`** (durum rozeti + geri sayım + "Mahalleniz" rozeti; renk **her zaman metinle** birlikte) · **`AnnouncementTypeFilter`** · liste ekranlarında boş/hata durumları da **kaydırılabilir** (yoksa pull-to-refresh ölür).
  - **51 yeni test (toplam 211)**: akış denetleyicisi (sayfalama/filtre/yarış/mükerrer/2. sayfa hatası), tür modeli (hex + fa→Material + bozuk girdi), kesinti durum-süre-gruplama-mahalle matrisi, ekran testleri (sayaçların gerçekten POST'lanması, NOT_FOUND ayrımı, misafirde filtre anahtarının çizilmemesi), `AppDate.range/duration`.
- **Karşılaşılan tuzaklar:** `app_modules_test`/`home_screen_test` "yakında" ekranını doğruluyordu → modül `ready` olunca kırıldı (test artık `ready` bayrağına göre iki ayrı iddia kuruyor; **bu kırılma iyi bir işaret**, kayıt ile ekran eşleşmesi gerçekten denetleniyor). Kart rozeti "Şu an sürüyor" bölüm başlığıyla birebir aynıydı → rozet "Sürüyor"a kısaltıldı. `flutter pub add` sonrası `json_annotation` sürüm uyarısı zararsız (kod üretimi geçiyor).

### 11.7 — Nöbetçi Eczane + Şehir Rehberi — [x] ✅ TAMAMLANDI (31 Temmuz 2026, 5. oturum)
- [x] **Nöbetçi eczane** (`features/pharmacies/`, rota `/eczaneler` + `/eczaneler/:id`): iki sekme — **Nöbetçi** (bugünün nöbetçisi öne çıkan kart: ad/nöbet saatleri/adres + "Eczaneyi ara"/"Yol tarifi" + **aylık nöbet takvimi ızgarası**, ay ileri-geri, nöbetli gün işaretli, bugün çerçeveli, güne dokununca o günün nöbetçisi) ve **Eczaneler** (aramalı sayfalı tam liste). Detay: eczacı/adres/telefon/çalışma saatleri + iletişim aksiyonları + **"bu ayki nöbet günleri"** (ek uç yok, aylık liste süzülüyor) + paylaş.
- [x] **Şehir rehberi** (`features/guide/`, rota `/rehber` + `/rehber/:id`): **tek ekran** — arama kutusu + kategori chip'leri (ikisi **birlikte** uygulanır) + sayfalı liste (sonsuz kaydırma) + liste kartında **tek dokunuşla arama düğmesi**; detay: telefon/adres/çalışma saatleri/e-posta/web satırları + `ContactActions` + paylaş.
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + çalışan API, canlı; geçici nöbet verisiyle): bugünün nöbetçisi doğru göründü ("Şifa Eczanesi · 19:00 - 09:00") ve hub şeridine de düştü · takvimde 3/25/28/31 işaretli, **31 (bugün) çerçeveli**, 25'e dokununca "25 Temmuz Cumartesi → Merkez Eczanesi + *Nöbet ertesi gün 09:00'a kadar sürer*" · Eczaneler sekmesinden detay açıldı, "Bu ayki nöbet günleri" 3 Tem + 25 Tem çipleri geldi · rehberde 6 kategori chip'i + kayıtlar, "hastane" araması **1 kayıt** döndürdü, detayda Ara/Yol tarifi çıktı. `flutter analyze` **0 sorun**, `flutter test` **244/244** (33 yeni), `dotnet test` **83/83**, **iOS simülatör derlemesi geçti**.
- **KARARLAR:**
  - **Rehber tek ekran** (plandan bilinçli sapma): plan "kategoriler → liste → detay" diyordu; kategori sayısı 6 ve kullanıcı çoğunlukla "hastane" yazıp arıyor → ayrı kategori ekranı fazladan bir dokunuş olurdu. Kategoriler yine gezilebilir, sadece aynı ekranda chip olarak. **Kategori + arama birlikte** çalışır.
  - **Rehber listesinde doğrudan arama düğmesi:** rehberin asıl işi "numarayı bul, ara" — kullanıcıyı detaya girmeye zorlamak iki fazla dokunuş. Telefonu olmayan kayıtta düğme hiç çizilmez.
  - **Eczane ekranı iki sekme, varsayılan Nöbetçi:** kullanıcı buraya genelde "şu an açık eczane hangisi" diye geliyor.
  - **Takvim ızgara, liste değil:** "bu ayın hangi günü hangi eczane" sorusu tarayarak değil bakarak cevaplanır. Nöbeti olmayan gün **dokunulabilir değil** (işlevsiz buton yok). Hafta Pazartesi'den başlar.
  - **`dutyDate` saat dilimi kaydırılmaz:** sunucu `duty_date`'i zaten "Türkiye günü, 00:00 UTC" konvansiyonuyla yazıyor (`TurkeyClock`) → `dayKey` ham UTC alanlarından üretiliyor; üstüne +3 eklemek gereksiz.
  - **Nöbet saatleri metin** (`"19:00"`), tarih değil → aritmetik yok; gece yarısını aşan nöbet (`19:00-09:00`) tespit edilip **"ertesi gün"** notu yazılıyor (kullanıcı 01:00'de eczane arıyorsa kritik bilgi).
  - **Detaydaki "bu ayki nöbet günleri" ek uç ISTEMEZ** — takvim sekmesinin zaten çektiği aylık liste süzülüyor (aynı provider, aynı önbellek). Takvim alınamazsa bölüm sessizce gizlenir, detay ekranı hataya düşmez.
  - **Kategori listesi patlarsa filtre şeridi hiç çizilmez** (11.6 kararının aynısı).
- **⭐ PLANDIŞI (bilinçli):**
  - 🔑 **`core/paging/paged_feed.dart` — ortak sayfalama çekirdeği:** 11.6'da yazılan `AnnouncementFeedState` deseni `PagedFeedState<T, F>` + `PagedFeedController<T, F>` olarak genelleştirildi (sayfalama, yarış durumu `_requestId`, mükerrer eleme, filtre değişimi, 2. sayfa hatasının listeyi silmemesi — hepsi tek yerde). Duyurular buna taşındı (**22 testin hiçbiri değişmedi**), eczane listesi ve rehber listesi bedavaya geldi. **11.8 ilanlar listesi de bunu kullanacak.** Alt sınıf yalnız üç şey söylüyor: başlangıç filtresi, sayfa nasıl çekilir, öğe kimliği.
  - **`core/widgets/contact_actions.dart` — `ContactActions` + `InfoRow`:** Ara / Yol tarifi / Web / E-posta aksiyonları tek bileşende; veri yoksa buton **hiç çizilmez**, dış uygulama açılamazsa şerit mesajı çıkar (sessiz başarısızlık yok). Koordinat yoksa harita **adres metniyle** aranır. **11.11 taksi + mekanlar doğrudan bunu kullanacak.**
  - **`DutyCalendar` + `MonthSwitcher`** (11.10 etkinlik takvimi için hazır zemin) · **`PharmacyTile`** · **`GuideItemTile`** · kategori slug'ı → Material ikon eşlemesi (yeni paket yok; sunucunun `icon`/`color` alanları seed'de boş) · **Acil Numaralar kategorisi chip'te vurgulu** (seçili değilken bile `danger` tonu).
  - **33 yeni test (toplam 244)**: nöbet modeli (dayKey sınırları, gece yarısını aşan nöbet, bozuk saat, ay sarması, `dutyDaysOf`), eczane ekranı (bugünkü nöbetçi, boş durum, takvimde gün seçimi, **nöbetsiz güne dokunmanın hiçbir şey yapmaması**, ay değişince yeni istek, arama → `?search=`, detay, NOT_FOUND), rehber (kategori+arama birlikte, kategoriler patlayınca şeridin çizilmemesi, telefonsuz kayıtta arama düğmesinin olmaması, filtre temizleme, detay, NOT_FOUND), model testleri.
- **🐛 Testin yakaladığı gerçek hata:** `PharmacyTile`'ın çalışma saati satırı `Expanded` olmadığı için uzun metinle (**"Nöbet ertesi gün 09:00'a kadar sürer"**) `RenderFlex overflowed by 222 pixels` veriyordu — canlıda kısa metinle fark edilmezdi.
- **Karşılaşılan tuzaklar:** varsayılan widget-test yüzeyi **800x600** → takvim ızgarası ve altındaki bölümler "off-screen" sayılıp `tap` reddediliyordu; çözüm `tester.view.physicalSize` ile gerçek telefon yüzeyi vermek (kaydırma jimnastiğinden kurtarıyor). Yatay chip şeridi tembel → testte dokunulacak kategori `displayOrder` ile başa alınmalı. İç içe rota (`/eczaneler/:id`) **alt ekranı da kurar** → liste ekranının istekleri de arkada başlar, `pumpAndSettle` sonrası bir kare daha gerekiyor (yoksa "pending timer").

### 11.8 — İlanlar Bölüm 1: liste + detay — [x] (31 Temmuz 2026, 6. oturum)
- [x] **Liste:** `GET /v1/ads?sort=&search=&categoryId=&minPrice=&maxPrice=&page=` — sıralama chip'leri (newest/oldest/price_asc/price_desc), **gecikmeli** arama, **iki katmanlı** kategori şeridi (`GET /v1/ads/categories[?parentId=]`), sonsuz kaydırma, kart (görsel/başlık/fiyat/tarih/görüntülenme/favori kalbi).
- [x] **Detay:** `GET /v1/ads/{id}` (galeri + tam ekran görüntüleyici, açıklama, fiyat, kategori özellikleri, view_count, satıcı, yayın süresi, paylaş). Aksiyonlar: **Favori** (`POST/DELETE /v1/ads/{id}/favorite` `[A]`, idempotent, anonimse giriş daveti), **Ara** (`POST …/track-phone` → telefonu aç), **WhatsApp** (`POST …/track-whatsapp` → hazır mesajlı wa linki). Görseller `AppNetworkImage` (`cached_network_image` + `AppImage.url`).
- **Bitti kriteri:** ✅ Liste sıralama/arama/kategori/fiyat + sonsuz kaydırma çalışıyor; detayda favori/ara/whatsapp uçları canlıda tetiklendi (DB'de sayaçlar arttı); anonim kullanıcı favoriye basınca giriş daveti çıkıyor.
- **KARARLAR:**
  - **Kartta mahalle yok, tarih + görüntülenme var** (plandan bilinçli sapma): `AdResponseDto` mahalle taşımıyor ve `Ad` varlığında mahalle alanı **hiç yok** (backend Faz 10'da donduruldu). Boş bir alan çizmek yerine kullanıcının gerçekten baktığı iki bilgi konuldu.
  - 🔑 **Kategori şeridi TEK şerit, iki katmanlı (drill-in):** kök seçilince şerit o kökün içine iner (`[Tümü] [Araçlar] [Otomobil] [Motosiklet]`). Sebep: ⚠️ **sunucu kategori filtresi TAM EŞLEŞME** — `?categoryId=<Araçlar>` yalnız doğrudan Araçlar'a yazılmış ilanları döndürür, "Otomobil"dekileri **döndürmez**. Alt kategoriler görünmezse kullanıcı aradığını bulamaz. Hangi chip vurguluysa filtre odur; belirsizlik yok. Alt kategorisi olmayan kökte (`subCategoryCount=0`) ikinci istek **hiç atılmaz**.
  - **Favori durumu ayrı bir kümede:** `AdDetailDto`'da `isFavorited` **yok** ve favori uçları yalnız "değişiklik oldu mu" döndürüyor → `GET /v1/users/me/favorites` bir kez okunup bellekte küme tutuluyor (`FavoriteAdsController`); liste kartı ve detay **aynı kümeye** bakıyor, kalp **iyimser** doluyor, hata olursa geri alınıyor. Anonimde uca **hiç istek gitmez**.
  - **Detay sekmenin İÇİNDE açılır** (`/ilanlar/:id` = İlanlar dalının alt rotası): alt sekme çubuğu yerinde kalır, geri liste konumunu korur. Diğer modüllerin detayları (11.6-11.7) kabuğun dışında; İlanlar sekme olduğu için farklı.
  - **`view_count` yanıtı artıştan ÖNCEKİ değerdir** (backend `GetAdByIdQueryHandler`) → ekranda "sen açmadan önce kaç kişi baktı" anlamına gelir, +1 eklenmedi. Detay provider'ı `autoDispose` ama gereksiz invalidate edilmiyor (her çağrı sayacı şişirir).
  - **Sayaç uçları (`track-phone`/`track-whatsapp`) beklenmez ve hatası yutulur** — arama, sayaç yüzünden gecikmemeli.
  - **Ters fiyat aralığı sessizce düzeltilir** (min > max girilirse takas edilir): "sonuç yok" göstermek yerine niyet uygulanır.
  - **Kategoriler alınamazsa şerit hiç çizilmez** (11.6/11.7 kararının aynısı), sıralama ve liste çalışmaya devam eder.
- **⭐ PLANDIŞI (bilinçli):**
  - **Fiyat aralığı filtresi** (`minPrice`/`maxPrice`): uç baştan destekliyordu, panelde de var (10.9), mobilde eksikti; pazaryerinde en sık kurulan filtre. Alt sayfa + şeritte özet chip ("100.000 ₺ – 500.000 ₺", ikinci dokunuş kaldırır).
  - **`core/utils/app_money.dart`** — `AppMoney.amount/price/rangeLabel/parse`. Kuruş **yalnız varsa** yazılır, **fiyatsız ilan "0 ₺" DEĞİL "Fiyat belirtilmemiş"** (bedava sanılmasın). `parse` Türkçe klavyeyi anlar: yalnız nokta varsa ve her gruptan sonra tam 3 hane geliyorsa **binlik** kabul edilir (`50.000` → 50000), aksi hâlde ondalık (`1250.50`). **11.9/11.10 kullanacak.**
  - **`core/utils/debouncer.dart`** — arama sunucuda **başlık + açıklamada** koşuyor (pg_trgm indeksi yalnız başlıkta); 350 ms gecikme. Canlıda **7 harf → 1 istek** doğrulandı.
  - **Galeri + tam ekran görüntüleyici** (`AdGallery`/`AdGalleryViewer`): sayfa noktaları + "2 / 3" sayacı, `InteractiveViewer` ile yakınlaştırma (**yeni paket yok**), fotoğrafsız ilanda nötr yer tutucu.
  - **Paylaş** (`AppShare`, 11.6'dan) · **WhatsApp'a hazır mesaj** ("… ilanınız hakkında bilgi almak istiyorum") · **satıcı kartı** · **yayın süresi + güvenlik uyarısı** · arama kutusunda temizleme (✕).
  - **Hub ızgarası düzeltmesi:** modül rotası aynı zamanda bir **sekme** ise (`İlanlar`) `context.push` kabuğun üstüne ikinci kabuk yığıyordu → `AppRoutes.tabs` kontrolüyle `context.go` (sekme değiştirir).
  - **61 yeni test (toplam 304)**: para/filtre/model birimleri, liste ekranı (sıralama→whitelist değeri, gecikmeli arama, kategori drill-in, alt kategori, fiyat aralığı, 2. sayfa hatası, boş durum, favori akışları), detay (özellik biçimleme, sayaç uçları, telefonsuz ilan, NOT_FOUND, tam ekran galeri), favori denetleyicisi (iyimser/geri alma/çok sayfa/anonim).
- **🐛 Canlı testin yakaladığı üç gerçek hata** (hiçbiri birim testte görünmüyordu):
  1. **"Filtreleri temizle" arama kutusunu temizlemiyordu** — filtre sıfırlanıyor, kutuda eski metin kalıyordu (kullanıcı dolu listeyi görüp "neden?" diyor). `ref.listen` ile kutu senkronlandı. ⚠️ **Aynı hata 11.7 Rehber ekranında da vardı**, o da düzeltildi; ikisi de regresyon testine bağlandı.
  2. **Tam ekran görüntüleyici tam ekran değildi** — dal Navigator'ına push edildiği için **alt sekme çubuğu altta görünmeye devam ediyordu** → `Navigator.of(context, rootNavigator: true)`.
  3. **Görüntüleyicide fotoğraflar arasında geçilemiyordu** — `InteractiveViewer` **her** yatay sürüklemeyi yutuyor; kaydırma artık yalnız yakınlaştırılmışken açık (`panEnabled: _isZoomed` + `NeverScrollableScrollPhysics`). Ayrıca AppBar başlığı siyah zeminde koyu çıkıyordu (`foregroundColor` `titleTextStyle`'ı ezmiyor) → açık renk `titleTextStyle`.
- **🧪 Test kapsamı gözden geçirmesi (aynı oturum, 11.8 sonrası):** mevcut senaryolar tarandı, **beş boşluk** kapatıldı ve **+59 test** eklendi (304 → **364**):
  1. **`core/paging/paged_feed_test.dart`** — çekirdeğin en ince kısmı olan **yarış durumu** (`_requestId`) hiç test edilmiyordu (dört modül buna dayanıyor): geç dönen eski filtrenin/tazelemenin/`loadMore` sayfasının yanıtı yeni listeyi ezmemeli; tazeleme hatası kayıtları silmemeli, filtre değişimi hatası silmeli.
  2. **`core/utils/app_links_test.dart`** — dış uygulama URL'leri hiç doğrulanmıyordu; **hatalı normalleştirilmiş numara yanlış kişinin WhatsApp sohbetini açar** ve kullanıcı bunu ancak karşı taraf cevap verince anlar (`0532…`/`+90532…`/`532…` üçü de elle giriliyor). `url_launcher` platform kanalı sahtelenip açılan URL yakalanıyor; `geo:` → Google Haritalar düşüşü de kapsandı.
  3. **`core/widgets/contact_actions_test.dart`** — "veri yoksa buton çizilmez" kuralının **kendisi** test edilmiyordu (yalnız pozitif hâli dolaylı olarak); 11.11 taksi + mekanlar bu bileşene dayanacak.
  4. **`features/ads/ad_card_test.dart`** — 11.7'deki `PharmacyTile` taşmasının aynı sınıfı için uzun başlık / 7 haneli fiyat / **1.4 yazı ölçeği** / dar ekran kombinasyonları.
  5. **`features/ads/ads_feed_controller_test.dart`** + yazı ölçeği sınırı (`app_smoke_test`) + favori kümesinin çıkışta sıfırlanması — canlıda doğrulanmış ama testle kilitlenmemiş kurallar (ters fiyat aralığının takası, seçili köke/alt kategoriye tekrar dokunma, "Filtreleri temizle" sıralamayı korur, `MediaQuery.withClampedTextScaling` 0.9–1.4 gerçekten uygulanıyor mu).
- **⚠️ Gözden geçirmede öğrenilen test kuralı:** provider (widget'sız) testlerinde **sabit `Future.delayed` kullanılmaz** — tek dosya koşarken yeten süre, tüm süit paralel koştuğunda yetmiyor ve test **flaky** oluyor (yeni denetleyici testi ilk tam koşuda böyle kırıldı). `test/helpers/pump_app.dart`'a **`waitUntil(condition)`** eklendi; pozitif iddialar koşul bekliyor, "istek gitmemeli" gibi **negatif** iddialar sınırlı bekleme kullanıyor. Mevcut favori testleri de bu desene taşındı. Tam süit **3 kez üst üste** koşturulup doğrulandı.
- **🐛 Gözden geçirmenin yakaladığı gerçek hata:** `AdCard`'ın sağ sütunu **sabit** `SizedBox(height: 104)` idi → yazı ölçeği 1.4'te `RenderFlex` **15 px dikey taşma**; ayrıca tarih + görüntülenme tek `Row`'da olduğu için dar ekran + 1.4'te **8.6 px yatay taşma**. 104 artık yalnız **alt sınır** (`ConstrainedBox(minHeight:)`), meta satırı **`Wrap`** (görüntülenme gerekirse alt satıra iner). Canlıda uzun başlıklı + `1.234.567,89 ₺` fiyatlı geçici ilanla doğrulandı.
- **Karşılaşılan tuzaklar:** yatay `ListView` **tembel** → ekran dışı chip hiç kurulmuyor (hem erişilebilirlik ağacından düşüyor hem testte "yok" görünüyor) → şeritler `SingleChildScrollView + Row`. `pumpAndSettle` **`Timer`'ı ilerletmez** (çerçeve planlanmıyor) → debounce testlerinde süre elle pump edilmeli. Görselli ekranlarda `AppNetworkImage` shimmer'ı **sonsuz animasyon** → `pumpAndSettle` kilitleniyor, sabit sayıda kare pump gerekiyor. `find.bySemanticsLabel` semantik ağaç açık değilken 0 döner → ikon bazlı finder.

### 11.9 — İlanlar Bölüm 2: ilan verme + benim ilanlarım + favorilerim — [x] (1 Ağustos 2026)
- [x] **İlan ver** (`POST /v1/ads` `[A]`, `/ilan-ver`): **üç adımlı sihirbaz** (Kategori → Bilgiler → Fotoğraflar & gönder). Kategori kök→alt drill-in (+ "X (genel)" ile köke verme), **kategoriye özel dinamik alanlar** (`GET /v1/ads/categories/{id}/properties` → Text/Number/Select/MultiSelect/Boolean widget üretimi; zorunlular önden denetlenir), başlık/açıklama/fiyat/satıcı/iletişim telefonu (E.164), **çoklu görsel** (`pickMultiImage` → `POST /v1/files/upload` ×N → id'ler) + **kapak seçimi**, özet kartı. Validasyon hataları alan altında.
- [x] **İlanlarım** (`/profil/ilanlarim`): `GET /v1/users/me/ads?status=` — statü şeridi (Tümü/Onay bekliyor/Yayında/Reddedildi/Süresi doldu), **rejected → gerekçe kırmızı kartta**, **performans sayaçları** (görüntülenme/arama/WhatsApp/favori), süre uyarısı, Düzenle (`PUT` — yeniden moderasyon uyarısıyla) / Sil (onaylı `DELETE`) / Uzat (`POST …/extend`, kalan hak butonda, 409 mesajı).
- [x] **Favorilerim** (`/profil/favorilerim`): `GET /v1/users/me/favorites` — `isAvailable=false` **soluk + "Şu an yayında değil" rozeti** ve detayı kapalı; favoriden çıkarma **geri alınabilir**.
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + iPhone 17 + çalışan API + panel): 3 fotoğraflı, 5 özellikli ilan verildi → DB'de `pending`, panelde "Bekliyor"; kapak seçilen fotoğraf oldu; düzenleme fiyatı değiştirip kapağı taşıdı ve ilanı yeniden `pending` yaptı; panelden reddedilince gerekçe kartı mobilde göründü; uzatma DB'de +30 gün/`extension_count=1`; silme soft-delete; favori ekle/çıkar/geri al DB'de doğrulandı. `flutter analyze` **0**, `flutter test` **432/432**, `dotnet test` **83/83**.
- **KARARLAR:**
  - 🔑 **Form üç adımlı sihirbaz** (tek uzun sayfa değil): kategoriye özel alanlar **ancak kategori bilindikten sonra** çekilebiliyor; tek sayfada form ortadan büyüyüp kullanıcının yazdıklarını kaydırırdı. Düzenlemede kategori kilitli olduğu için 1. adım hiç çizilmez (2 adım).
  - ⚠️ **Düzenleme rotası `/ilan-ver`in ALT ROTASI DEĞİL, kardeşi** (`/ilan-duzenle/:id`): go_router iç içe rotada **üst ekranı da kurar** (11.7'de eczane detayında görülen tuzak) → düzenlemeye girerken arkada "yeni ilan" formu açılıyor, kategori isteği atıyor ve **taslak diyaloğunu düzenleme ekranının üstüne fırlatıyordu**. Test "pending timer" ile yakaladı.
  - **İlan verme kabuğun DIŞINDA tam ekran** (alt sekme çubuğu yok): uzun ve odaklanmış bir görev, kullanıcı formun ortasında başka sekmeye davet edilmemeli. Buna karşılık **me-scoped listeler Profil sekmesinin İÇİNDE** (`/profil/ilanlarim`, `/profil/favorilerim`) — geri tuşu profile döner.
  - **"İlan ver" düğmesi anonimde router'la Giriş'e ATMAZ**: `ensureSignedIn` daveti çıkar (11.3/11.4 "misafir gezinme birinci sınıf" kararı), vazgeçen listede kalır. Rota yine `protectedPrefixes`'te — derin bağlantı/geri gezinme için.
  - 🔑 **Kapak = ilk fotoğraf** (ayrı bayrak değil): sunucu `CreateAdCommandHandler`'da listenin ilkini `IsCover` işaretliyor; kullanıcıya da "ilk fotoğraf kapaktır" diye anlatılıyor — tek kural, iki yerde aynı.
  - ⚠️ **Düzenlemede kapak/sıra değişimi "yeniden bağlama" ile**: `UpdateMyAdCommand` yalnız "ekle/sil" biliyor, yeni görselleri **sona ve `isCover=false`** yazıyor, kapağı ancak "hiç kapak kalmadıysa" en düşük sıradakine veriyor. Sıra değiştiyse mevcut görseller **dosya kimlikleriyle** silinip yeni sırada ekleniyor → kapak tam olarak kullanıcının seçtiği görsel oluyor. **Sıra değişmediyse bu yola hiç girilmez** (gereksiz satır silme yok).
  - **Zorunlu alan denetimi istemcide de var** (`AdSubmissionRules`'ın aynası: başlık 3-200, açıklama ≤5000, fiyat ≥0, telefon `5xx` 10 hane, zorunlu property'ler) — kullanıcı formu doldurup 400 yemesin. Sunucu yine de reddederse mesaj **ilgili alanın altına** düşer (11.5 deseni).
  - **Boolean alan başlangıçta seçili değil** ("Hayır" ile "cevaplanmadı" aynı şey değil; opsiyonel alan dokunulmadıysa sunucuya hiç gitmez); **seçeneksiz `select` alanı hiç çizilmez** (kullanıcı hiçbir şey seçemez, zorunluysa formu kilitler).
  - **Uzatma butonu yalnız `approved`/`expired` ve hak varken açık** (sunucu diğerlerinde 400 veriyor) — "işlevsiz buton yok" kuralı; kalan hak butonun üstünde yazıyor (`Uzat (2)`).
  - **Silme/uzatma listeyi yerinde günceller** (tam tazeleme beklenmez): silinen satır anında düşer, uzatılan satırın yeni bitiş tarihi/kalan hakkı sunucunun yanıtından yazılır.
  - **Yayında olmayan favorinin detayı açılmaz** (public uç 404 verirdi) — satır soluk + **metinli** rozet; ilan silinmediği için listeden de düşürülmez.
  - ⚠️ **Düzenleme ekranı `GET /v1/ads/{id}` çağırdığı için ilanın `view_count`'u 1 artar** (uç sahibi için de sayıyor). Kabul edildi: görsel kimlikleri (`images[].id`) ve property değerleri yalnız detay yanıtında var, `MyAdDto` taşımıyor.
- **⭐ PLANDIŞI (bilinçli):**
  - **Taslak kaydı** (`data/ad_draft_store.dart`): ilan verme uygulamanın en uzun formu; telefon kilitlenirse / araya arama girerse yazılanların kaybolması en sinir bozucu davranış olurdu. Çıkarken **"Taslağı sakla / Vazgeç / Devam et"** sorulur, form yeniden açılınca **sorularak** geri yüklenir (sessizce doldurmak "bunu ben mi yazdım?" dedirtir). **Görseller taslağa yazılmaz** (`image_picker` yolları geçici önbellekte, olmayan dosyayı "seçili" göstermek yalan olur), **7 günden eski taslak teklif edilmez**.
  - **İlan performans kartı**: `MyAdDto` zaten görüntülenme/telefon/WhatsApp/favori sayaçlarını döndürüyordu, plan bunları kullanmıyordu — ilan sahibinin en çok merak ettiği bilgi kartın içinde.
  - **Süre uyarısı + CTA**: yayına 7 günden az kalan ilanda "… 3 gün kaldı", süresi dolanda tarihiyle uyarı — kullanıcı süreyi ancak ilan düşünce fark etmesin.
  - **Profil satırında ilan sayısı rozeti** (`myAdsCountProvider`) — **ek istek atmaz**, listeyi zaten okuyan denetleyiciden türer; anonimde uca hiç istek gitmez.
  - **`AdSubmissionService`** (`application/`): görsel yükleme + "hangi görsel yeni/silinecek, sıra değişti mi" kararları ekransız saf mantık → ekransız test edilebiliyor. **Yükleme ilerlemesi** ("Fotoğraflar yükleniyor: 2 / 3").
  - **`FavoriteAdsController.setFavorite(id, value:)`** — `toggle` yerine **yönü açıkça söyleyen** API (aşağıdaki hata bunun için eklendi).
  - **`AppMoney.plain`** (sembolsüz fiyat girişi; `parse` ile gidip gelebiliyor) · **`AdPropertyField`** (5 tip → widget) · **`AdImagePickerGrid`** · **`MyAdCard`** · `AdStatus` enum (Türkçe etiket + bir cümlelik açıklama + renk/ikon; **durum her zaman metinle**, renk tek başına yetmez) · **68 yeni test (toplam 432)**.
- **🐛 Testin/canlının yakaladığı üç gerçek hata:**
  1. **Favori listesinde "çıkar" bazen POST atıyordu** — ekran `toggle` çağırıyordu, `toggle` ise favori **kimlik kümesine** bakıyor; küme `ref.read` ile o an kurulduğu için henüz boştu → "ekle" yönüne gidiyordu. Kullanıcı favorisini silemez, üstelik zaten favorideki ilana ikinci kez POST atılırdı. Çözüm: `setFavorite(value: false)` — **durumu bilen ekran yönü kendisi söyler**.
  2. **`_FavoriteTile` meta satırı taşıyordu** (`RenderFlex overflowed`): kartın metin sütunu görsel + kalp butonu payı düşülünce ~168 px kalıyor, `Wrap` çocuğuna sınırlı genişlik veriyor → `Flexible` + `ellipsis`. **Aynı sınıf hata 11.7 `PharmacyTile` ve 11.8 `AdCard`'da da çıkmıştı** — dar sütun + `Row` içinde çıplak `Text` bu projede tekrar eden bir tuzak.
  3. **Zorunlu özellik boşken form en üste kaydırıyordu** — hata mesajları aşağıdaki kategori alanlarında, kullanıcı yukarı fırlayınca sebebi göremiyordu. Artık yukarı kaydırma yalnız **üstteki** alanlarda hata varsa yapılıyor (canlıda yakalandı).
- **Karşılaşılan tuzaklar:** iç içe `GoRoute` üst ekranı da kurar (yukarıdaki karar); form uzun + `ListView` **tembel** → widget testinde ekran dışı alan hiç kurulmuyor, yüzey `tester.view.physicalSize` ile yükseltildi (11.7 tuzağının devamı); `MultipartFile.fromFile` **gerçek dosya** ister → servis testinde `Directory.systemTemp` geçici dosyaları; ipucu metni her sayısal alana uymuyordu ("Kilometre: Örn. 2018") → sayısal property'lerde ipucu yazılmıyor; `find.text` **`EditableText`'i de eşler** → ipucu ile değer aynı metinse iki eşleşme çıkar, controller doğrudan okunmalı.

### 11.10 — Etkinlikler + Kampanyalar — [x] (1 Ağustos 2026)
- [x] **Etkinlikler** (`/etkinlikler` + `/etkinlikler/:id`): iki sekme — **Liste** (arama + **Yaklaşan/Geçmiş/Ücretsiz** şeridi + kategori chip'leri + sonsuz kaydırma + "Toplam N etkinlik") ve **Takvim** (aylık ızgara, etkinlikli günler işaretli, bugün çerçeveli, güne dokununca o günün saat sıralı listesi → detay). Kart: **takvim yaprağı (gün/ay)** + tür + başlık + saat + mekan + "Bugün/Yarın/N gün sonra" + "Ücretsiz"/fiyat. Detay: tarih-saat kartı + geri sayım, mekan/adres + **Yol tarifi**, bilet, açıklama, düzenleyen, paylaş; geçmiş etkinlikte bilgi şeridi.
- [x] **Kampanyalar** (`/kampanyalar` + `/kampanyalar/:id`): aramalı liste (işletme adı + kampanya başlığı), kartta indirim oranı / "İndirim kodu" / **"Son gün!"** rozetleri + geçerlilik aralığı; detayda indirim, geçerlilik, açıklama, koşullar ve **"İndirim kodunu göster"** → `POST /{id}/view-code` → **kod modalı** (büyük seçilebilir kod + **kopyala** + geçerlilik + ilk görüntüleme anı + koşullar). Kodsuz kampanyada buton yok, yerine ne yapılacağını söyleyen şerit.
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + iPhone 17 + çalışan API, geçici veriyle): yaklaşan liste **en yakın tarih başta** (1/3/10/26 Ağu), "Ücretsiz" filtresi 150 ₺'lik etkinliği eledi (4→3), "Geçmiş" sekmesi soluk tarihli iki geçmiş etkinliği azalan sırada verdi, **pending etkinlik hiçbir yerde görünmedi**; takvimde 1/3/10/26 işaretli + bugün çerçeveli, 10'a dokununca "10 Ağustos Pazartesi · 20:30 Yaz Akşamı Türkü Gecesi" → detay; kampanya listesinde "Son 1 gün" rozeti, kod modalı açıldı ve **DB'de `code_view_count` 0→1 + `campaign_code_views` satırı (aysedmr)**, **ikinci kez açmak sayacı artırmadı**, kod panoya kopyalandı, kodsuz kampanyada buton yerine şerit çıktı. `flutter analyze` **0**, `flutter test` **473/473**, `dotnet test` **87/87** (4 yeni).
- **KARARLAR:**
  - 🔑 **Backend'e tek küçük ekleme: `GET /v1/events?sort=date_asc`** (varsayılan `date_desc` korundu, bilinmeyen değer varsayılana düşer). Sebep: uç `EventDate` **azalan** sıralıyordu; "yaklaşan etkinlikler" listesinde ilk sayfa **en uzak tarihli** etkinlikleri getiriyor, sayfalama kullanıcı için anlamsız hâle geliyordu. İstemci tarafında çözülemezdi (sıralama sayfalar arası). Additive: admin paneli ve mevcut istemciler etkilenmedi, 4 entegrasyon testiyle kilitlendi.
  - **Liste varsayılanı "Yaklaşan"**, geçmiş ayrı chip: etkinlik uygulamasında sorulan soru "yakında ne var". Ayrım **gün bazında** (`startDate=bugün` / `endDate=dün`) → **bugün akşamki etkinlik sabah 09:00'da hâlâ "yaklaşan"**.
  - **Takvim ayrı sekme, liste değil**: "ayın hangi gününde ne var" bakarak cevaplanır. Etkinliksiz gün **dokunulabilir değil** (işlevsiz buton yok); ay değişince seçim temizlenir; gezilen aylar `family` anahtarıyla önbellekte kalır.
  - ⚠️ **`eventDate` saat dilimi KAYDIRILMAZ** (11.7 `dutyDate` dersinin aynısı): sunucu "TR günü 00:00 UTC" yazıyor, saat ayrı `eventTime` alanında. Gün anahtarı ham UTC'den, saat metinden okunur.
  - **Kampanyada kategori/durum filtresi YOK**: uç `?categoryId=` ile **işletme** kategorisine bakıyor ama public bir işletme-kategori lookup ucu yok (olmayan listeden chip üretmek işlevsiz buton olurdu); "aktif/geçmiş" ayrımı da gereksiz — public uç zaten yalnız yürürlükteki kampanyaları döndürüyor (süresi dolan detay **404**).
  - ⚠️ **`discountCode` liste gövdesinde geliyor ama gösterilmiyor**: kod `view-code` ile açılır, çünkü sayaç esnafın kampanya ölçümü (10.12) ve "kaç FARKLI kullanıcı gördü" demek. Aynı kullanıcı ikinci kez isterse sunucu aynı kaydı döner → modal "kodu ilk görüntüleme" anını yazar.
  - **Kod paylaşılan metne konmaz** (kişiye özel açılıyor, yayınlamak ölçümü de bozar); paylaşım metni kampanya + işletme + oran + geçerlilik.
  - **"Kodu göster" anonimde router'la Giriş'e ATMAZ** → `ensureSignedIn` daveti (11.9 "İlan ver" kararının aynısı); anonimde uca hiç istek gitmez.
  - **Aciliyet/geri sayım rozetleri yalnız bir hafta içinde** çıkar (her kartta çıkan rozet anlamını yitirir), süre **Kadirli gününe göre** hesaplanır.
- **⭐ PLANDIŞI (bilinçli):**
  - 🔑 **`core/widgets/month_calendar.dart` — ortak aylık takvim** (`MonthCalendar` + `MonthSwitcher` + `CalendarMonth`/`shiftCalendarMonth`): 11.7'nin nöbet takvimi eczaneye özeldi; etkinlik takvimi aynı ızgarayı isteyince çizim işi ortak bileşene çıkarıldı, `DutyCalendar` **ince bir sarmalayıcı** oldu (gün→kayıt sayısı eşlemesi + kendi ekran okuyucu metni). Eczane testlerinin hiçbiri değişmedi.
  - 🔑 **`core/widgets/filter_chip.dart` — ortak filtre chip'i** (`FilterChoiceChip`): aynı chip 11.6'dan beri her ekranda kopyalanıyordu; ilan (`_FilterChip`) ve rehber (`_CategoryChip`) kopyaları silinip bu bileşene taşındı (rehberin "acil numaralar" vurgusu `accent` parametresiyle korundu). İkon **opsiyonel** yapıldı (aşağıdaki hata).
  - **"Ücretsiz" filtresi** (`?isFree=`): uç destekliyordu, plan saymıyordu; ücretsiz etkinlik en çok sorulan ayrımlardan biri.
  - **Kod modalında panoya kopyala** (`Clipboard`, yeni paket yok) — kod kasada okunacak; ayrıca büyük punto + seçilebilir metin.
  - **Paylaş** (etkinlik + kampanya, `AppShare`) · **`ContactActions` ile yol tarifi** (koordinat yoksa **mekan adı + adres** metniyle harita araması) · **takvim yaprağı kartı** · geçmiş etkinlikte soluk tarih rozeti · **51 yeni test (toplam 473)**.
  - **`waitUntil` zaman aşımı 5 sn → 15 sn**: süit 473 teste çıkınca paralel isolate'ler 5 sn'yi aşıp testleri flaky yapıyordu (koşul sağlanınca hemen döndüğü için geçen testler yavaşlamıyor).
- **🐛 Yakalanan üç gerçek hata:**
  1. 🔴 **`PagedFeedController` atılmış provider'ın `ref`'ine dokunuyordu** — `build()` içindeki `Future.microtask(loadFirstPage)` provider kapatıldıktan sonra çalışırsa *"Cannot use the Ref … after it has been disposed"*; `await` sonrası `state` yazımı da aynı riski taşıyor. Tüm sayfalı listeleri (duyuru/eczane/rehber/ilan/etkinlik/kampanya) ilgilendiriyordu; **süit büyüyünce flaky test olarak ortaya çıktı**, gerçek uygulamada da hızlı ekran kapatmada tetiklenebilirdi → mikro-görev ve her `await` sonrası `ref.mounted` kontrolü.
  2. **Kod modalı açıkken buton arkada dönmeye devam ediyordu** (`await showModalBottomSheet` bitene kadar `loading: true`) — testte "pumpAndSettle timed out" olarak yakalandı; yükleme göstergesi artık modal açılmadan **önce** kapanıyor.
  3. **Kampanya kartındaki rozet 1.4 yazı ölçeğinde 49 px taşıyordu** (`Wrap` çocuğuna sınırlı genişlik verir) → `Flexible` + `ellipsis`. **Bu, aynı sınıf hatanın dördüncü tekrarı** (11.7 `PharmacyTile`, 11.8 `AdCard`, 11.9 `_FavoriteTile`) — yeni kartlar için taşma testi baştan yazıldı.
- **Karşılaşılan tuzaklar:** **üç filtre chip'i 360 dp'lik telefona sığmıyordu** ("Ücretsiz" ekran dışında kalıyordu; şerit kaydırılabilir olsa da görünmeyen filtre keşfedilmiyor) → o şeritte `dense` + **ikonsuz** chip; canlıda **"% %25 indirim"** okunuyordu (yüzde ikonu + "%25" etiketi) → ikon `local_offer`; widget testinde `DateFormat('d MMM','tr_TR')` için `initializeDateFormatting('tr_TR')` şart; `AppMoney.plain(12.5)` → "12,50" (ondalık iki hane).

### 11.11 — Vefat + Taksi + Mekanlar — [x] ✅ TAMAMLANDI (2 Ağustos 2026)
- [x] **Vefat** (`features/deaths/`): liste `/vefat` (arama + sonsuz kaydırma + **"Bugün cenaze namazı: X, 13:30" hatırlatması**) + detay `/vefat/:id` (cenaze namazı kartı + **bugünkü cenazede geri sayım**, cami/defin/taziye satırları, yol tarifi, paylaş, başsağlığı dileği) + **Vefat bildir** `/vefat-bildir` `[A]` (ad + tarih/saat seçici + cami/mezarlık/mahalle dropdown + taziye adresi + opsiyonel foto → `pending`).
- [x] **Taksiciler** (`features/taxis/`): liste `/taksi` (ad **ve plaka** araması, **listede doğrudan "Ara"**) + detay `/taksi/:id`; **Ara** = `ensureSignedIn` → `POST /drivers/{id}/call` → dönen telefon `AppLinks.call` ile açılır.
- [x] **Mekanlar** (`features/places/`): liste `/mekanlar` (arama + **kategori chip şeridi**) + detay `/mekanlar/:id` (kapak, yol tarifi, adres/uzaklık/saat/giriş/mevsim, "Nasıl gidilir?", **olanaklar var/yok**).
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + iPhone 17 + çalışan API, canlı): mobilden gönderilen vefat bildirimi DB'de `pending` + tarih/saat/cami doğru · "Ara" → `taxi_calls` satırı + `total_calls` 0→1 + çevirici doğru numarayla açıldı · mekan kategorisi filtresi 3→2 · olanaklar (jsonb) doğru çözümlendi. `flutter analyze` **0**, `flutter test` **527/527**, `dotnet test` **90/90**, iOS derlemesi geçti.
- **KARARLAR:**
  1. 🔑 **Backend'e tek additive ekleme: `GET /v1/places/categories`** (`GetPlaceCategoriesQuery`, lookup cache grubunda 15 dk). `PlaceResponseDto` yalnız `categoryId` taşıyor ve public bir kategori ucu yoktu → mobil ne kartta kategori adını yazabiliyor ne de filtre chip'i çizebiliyordu; "olmayan listeden chip üretme" kuralı (11.6) bunu istemcide çözülemez yapıyordu. 3 entegrasyon testi (sıralama + `?categoryId=` uyumu + **literal segment `{id}` ile çakışmıyor**).
  2. **Vefat ekranı bilinçli olarak uygulamanın en sade ekranı**: filtre şeridi, renkli rozet, görüntülenme sayacı yok. Tek vurgu **bugünkü cenaze** (ince şerit + yeşil saat) çünkü kullanıcının aradığı bilgi bu.
  3. **Taslak kaydı YOK** (11.9 `AdDraftStore` deseninden bilinçli sapma): vefat bildirimi acil ve tek seferlik; "taslağı sakla, sonra devam et" teklifi bu bağlamda yersiz.
  4. ⚠️ **`funeralDate` saat dilimi KAYDIRILMAZ** (11.7 `dutyDate` / 11.10 `eventDate` dersinin üçüncüsü); saat ayrı `funeralTime` (`TimeSpan` → `"13:30:00"`).
  5. **Geri sayım yalnız bugünkü cenazede** ("3 gün 4 saat kaldı" bir vefat ilanında yersiz; bilgi tarihte zaten yazılı).
  6. **Vefat detayı taziye konumu yoksa cami/mezarlığın lookup koordinatına düşer** — gövde yalnız *ad* taşıyor, koordinatlar lookup uçlarında; ikisi de yoksa buton **hiç çizilmez**.
  7. **Taksi "Ara" dayanıklılığı:** uç 5xx/ağ hatası verirse çağrı kaydı tutulamaz ama kullanıcı taksiye ihtiyaç duyuyor → listeden gelen telefonla arama yine denenir ve **sebebi kullanıcıya yazılır** (sessiz başarısızlık yok).
  8. ⚠️ **Taksi arama parametresi `searchTerm`** (diğer modüllerde `search`) — `QueryTaxiDriverDto` böyle tanımlı, yanlış ad sessizce yok sayılırdı.
  9. ⚠️ **`places.amenities` `jsonb` kolonu ama DTO'da `string`** → yanıtta **JSON içeren metin** geliyor (`"{\"WC\":true}"`), nesne değil. Model iki şekli de çözer; **anahtarda olmayan olanak "belirtilmemiş"** demek, "yok" değil.
  10. **Mekan sıralaması sunucuda ada göre sabit** → sıralama chip'i çizilmedi (sayfalar arası tutarsız olurdu). `0,0` koordinatı "girilmemiş" sayılır.
  11. **Vefat bildir/Ara anonimde router'la Giriş'e ATMAZ** → `ensureSignedIn` daveti, anonimde uca istek yok (11.10 kararı).
- **⭐ PLANDIŞI (bilinçli):**
  - 🔑 **`core/widgets/lookup_dropdown.dart` — `LookupDropdown<T>`**: sözlük ucundan beslenen açılır liste (yükleniyor/hata/**boş liste** durumlarıyla). 11.5'teki mahalle alanı vefat formunda **üç kez** tekrarlanınca ortak bileşene çıkarıldı; 11.12 de kullanabilir.
  - **`features/deaths/application/death_submission_service.dart`** (foto yükle → kayıt yaz; ekransız test edilebilir, `AdSubmissionService` deseni) + `TimeOfDayValue` (Flutter'a bağımsız saat değeri).
  - **`RecentTaxiCallsStore` — "Son aradıklarınız"** (cihazda, en fazla 3): taksi çağırmak tekrarlanan bir eylem ve sunucuda "benim çağrılarım" ucu yok. ⚠️ **Telefon SAKLANMAZ** — arama yine `POST /call` ile yapılır (numara taze + sayaç işler); ad/plaka anlık görüntüsü saklanır ki bölüm yüklenmiş sayfaya bağımlı olmasın.
  - **`todaysFuneralsProvider`** (ek istek yok, yüklenmiş listeden türer — 11.6 kararı) · **zorunlu alan boşsa formu başa kaydırma** · vefat/taksi/mekan **paylaşımı** · plaka rozeti · mekan kategorisi slug→Material ikon eşlemesi (yeni paket yok).
  - **54 yeni test (toplam 527)** + **3 yeni dotnet entegrasyon testi (toplam 90)**.
- 🐛 **Testin yakaladığı gerçek hata:** `LookupDropdown` ve **`AppTextField`in etiket `Row`'u** uzun etiketle (`"Cenaze namazının kılınacağı cami"`) `RenderFlex` taşırıyordu → `Flexible` + ellipsis. **Aynı sınıf hatanın beşinci tekrarı** (11.7/11.8/11.9/11.10) — ama ilk kez **ortak bir bileşende**, yani tüm formları ilgilendiriyordu.
- 🐛 **11.10 testlerinde gece patlayan gizli hata (bu oturumda ortaya çıktı ve düzeltildi):** `event_card_test` / `events_screen_test` fixture'ları `eventDate`'i **`DateTime.now().toUtc()`** ile üretiyordu; oysa alan "Türkiye günü, 00:00 UTC" konvansiyonunda ve model onu kaydırmıyor → saat **00:00-03:00 arasında** UTC günü bir geri kalıyor ve **5 test yalnız geceleri** başarısız oluyordu. Fixture'lar `AppDate.nowInTurkey`e çevrildi. Ayrıca "detay açılır" testi ekran dışında kalan satırı arıyordu (gün adı uzayınca kart bir satır büyüyor) → iddia kaydırmaya bağlandı.
- **Tuzaklar:** `scrollUntilVisible` kabuk dalları ayakta olduğu için "Too many elements" veriyor → `dragUntilVisible(..., find.byType(ListView).last, ...)`; **5xx geçici hata sayılıyor** (`apiRetry`) → "liste gelmezse" testlerinde **kalıcı** hata (404) kullanılmalı, yoksa "pending timer"; kategori şeridi 360 dp'ye sığmıyor → testte şeridi elle kaydır; `AppMoney.plain(12.5)` "12,50" verir (mesafede tek hane doğal → ayrı biçimleyici).

### 11.12 — Ulaşım + Şikayet/İstek — [x] ✅ TAMAMLANDI (2 Ağustos 2026)
- [x] **Ulaşım** (`features/transport/`, `/ulasim`): iki sekme — **Şehirlerarası** (arama + kart: hedef/firma/süre/ücret + ⭐**"Sıradaki 14:00 · 1 sa 16 dk sonra"**; kart açılınca **tüm kalkış saatleri** hap olarak, geçenler üstü çizili+soluk, sıradaki dolu vurgulu + "Saatleri paylaş") ve **Şehir içi** (hat numarası rozeti + ad + servis saatleri + ⭐**"Şu an çalışıyor · 20 dk arayla · yaklaşık 12:50"**; kart açılınca **durak zaman çizelgesi** `+7 dk`/`+21 dk` + "Hattı paylaş").
- [x] **Şikayet/İstek** (`features/complaints/`): liste `/sikayet` (**Bildirimlerim** — durum rozeti [Bekliyor/İşlemde/Çözüldü/Reddedildi] + tür + **"Yetkili yanıtı" kartı** + sonuçlanma tarihi; misafirde Giriş daveti ama FAB açık) + form `/sikayet-bildir` (**6 tür chip'i** + konu + mesaj + türe göre değişen ipucu; anonimde uyarı şeridi).
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + iPhone 17 + çalışan API + admin panel, canlı): şehirlerarası "Sıradaki 14:00 · 1 sa 16 dk sonra" (saat 12:44) ve açılan kartta 07:00/10:30 üstü çizili, 14:00 vurgulu · şehir içi "Şu an çalışıyor · 20 dk arayla · yaklaşık 12:50" + 4 duraklı çizelge · mobilden gönderilen şikayet **DB'de `type='complaint'`, `user_id` bağlı, `pending`** · **panelden "Çözüldü" + not** → mobilde **"Yetkili yanıtı"** kartı + "Sonuçlandırıldı: 2 Ağustos 2026, 13:00". `flutter analyze` **0**, `flutter test` **578/578**, `dotnet test` **90/90**, iOS derlemesi geçti.
- **KARARLAR:**
  1. 🔑 **Ulaşımda detay ekranı/rotası YOK** (plandaki "rota detayları" kart içinde karşılandı). Sunucuda `transport/.../{id}` ucu yok; saatler (`schedules`) ve duraklar (`stops`) zaten **liste gövdesinde** geliyor. Listeyi tarayıp id bulan sahte bir detay rotası hem **derin bağlantıda** hem **ikinci sayfada** kırılırdı → kart **yerinde açılıyor**, aynı anda tek kart açık (iki hattın saatini yan yana okumak değil, bir hattın saatini bulmak istiyoruz).
  2. ⚠️ **Uçlar sayfalı** (`PagedResult`) — planın "sayfasız düz liste olabilir" uyarısının aksine → `FutureProvider` değil ortak **`PagedFeedController`** kullanıldı; hat sayısı büyürse ekran çalışmaya devam eder.
  3. ⚠️ **Arama parametresi `searchTerm`** (11.11 taksi tuzağının aynısı, `QueryTransportDto`); yanlış ad sessizce yok sayılırdı → teste bağlandı.
  4. ⚠️ **Saatler tarihsiz "duvar saati"** — şehirlerarası `"07:00"`, şehir içi `TimeSpan` → **`"06:30:00"`**. İkisini de tek çözümleyici okuyor; hesap **Kadirli gün içi dakikası** üzerinden (saat dilimi kaydırması yok — 11.7 `dutyDate` / 11.10 `eventDate` / 11.11 `funeralDate` dersinin dördüncüsü, bu kez "tarihi hiç olmayan" alan).
  5. **Şehir içi sıradaki kalkış "yaklaşık"** ve ekranda da öyle yazıyor: sunucu yalnız ilk/son saat + sıklık veriyor, kesin bilgi gibi sunmak kullanıcıyı durakta bekletirdi. Sefer listesi yoksa saat **uydurulmaz**, yalnız "Şu an çalışıyor" denir.
  6. **Şikayet gönderimi anonimde de açık** (uç `[AllowAnonymous]`, 10.7 kararı): "çöp alınmadı" demek için hesap açmak zorunda kalmak bildirimi engeller. Ama anonim kayıtta `user_id` NULL kalır ve **"Bildirimlerim"de görünmez** → uyarı **formun başında, gönderimden önce** verilir; başarı diyaloğu da anonimde takip vaat etmez.
  7. **Şikayet rotası `protectedPrefixes`'e yazılmadı** — misafir listeyi göremez (davet görür) ama **gönderebilir**; sert yönlendirme modülün asıl işini engellerdi (11.10/11.11 `ensureSignedIn` kararının aynı ruhu).
  8. ⚠️ **Şikayet türü sunucuda serbest metin** (doğrulayıcı ve sözlük ucu yok). Mobil 6 tür tanımlıyor (`complaint/request/suggestion/content/app/other`; **`content` ve `app` mevcut veriyle uyumlu seçildi**), **tanınmayan değer ham hâliyle** yazılıyor → panelden ya da eski sürümden gelen tür kaybolmuyor.
  9. **"Bildirimlerim"de durum filtresi yok** — uç desteklemiyor, istemcide süzmek sayfalama ile tutarsız sonuç verirdi (11.11 mekan sıralaması kararı).
  10. **Form `/sikayet`in kardeşi** (`/sikayet-bildir`), alt rotası değil — go_router iç içe rotada üst ekranı da kurar (11.7/11.9/11.11 tuzağı).
- **⭐ PLANDIŞI (bilinçli):**
  - 🔑 **`features/transport/application/departure_times.dart` — "sıradaki kalkış" hesabı** (Flutter'a bağımsız saf mantık, 17 test): modülün asıl sorusu "otobüs kaçta?" — saat listesini gözle tarayıp "şu an 13:40, demek ki 14:00" hesabını kullanıcıya yaptırmak gereksiz. Bugünkü seferler bittiyse **yarının ilk seferine** düşer ("Bugünkü seferler bitti · Yarın 07:00"), 30 dk kala kart **vurgulanır**. Şehir içi için ayrıca **servis durumu** (ilk seferden önce / çalışıyor / bitti / bilinmiyor), gece yarısını aşan servis penceresi dahil.
  - **30 sn'de tazelenen geri sayım** (11.6 kesinti ekranının deseni) · **hat paylaşımı** (`AppShare`; "Kadirli → Adana: 07:00 · 10:30 · 14:00 · 17:30" WhatsApp gruplarına doğrudan gider) · **durak zaman çizelgesi** (nokta+çizgi, ilk/son durak vurgulu).
  - 🔑 **İlan detayında "Bu ilanı şikayet et"** — eski metin kullanıcıyı *"Ayarlar → Şikayet/İstek"*e yolluyordu, oraya gidince hangi ilanı şikayet ettiğini de kendisi yazması gerekiyordu. Form artık **tür + modül + ilan kimliği + ilan adıyla** açılıyor (`AppRoutes.complaintForContent`), ham kimlik değil **ilan adı** gösteriliyor.
  - 🔑 **Panelde şikayet türü rozeti** (`ComplaintsAdmin/Index.cshtml`, tek additive view değişikliği): `Type` panelde **hiç gösterilmiyordu** → mobilde seçilen tür yöneticiye ulaşmıyordu. Bilinmeyen değer ham basılır.
  - **Türe göre değişen mesaj ipucu** (boş kutuya "ne yazayım" diye bakan kullanıcıya somut örnek) · **`LookupDropdown` uçsuz da kullanılabiliyor** (`AsyncValue.data(kAppModules)` ile bölüm seçimi) · **48 yeni test (toplam 578)**.
- 🐛 **İKİ GERÇEK HATA:**
  1. 🔴 **`AppScaffold(onRefresh:)` — pull-to-refresh kısa listelerde SESSİZCE ÖLÜYDU** (canlıda yakalandı, **11.6'dan beri her liste ekranını ilgilendiriyor**): içerik ekrana sığdığında Android'in varsayılan `ClampingScrollPhysics`i taşmaya izin vermiyor, aşağı çekme jesti `RefreshIndicator`a hiç ulaşmıyordu. ⚠️ Tetikleyici dar: `ListView` **kendi `controller`ını aldığında** `primary` `false` olur ve Flutter'ın otomatik `AlwaysScrollableScrollPhysics` takviyesi devreye girmez — bu projedeki **her sonsuz kaydırmalı liste** `loadMore` için controller veriyor. Düzeltme `AppScaffold`ta (`onRefresh` orada): `ScrollConfiguration` ile alt ağaca uygulanıyor. ⚠️ **İlk denemem çok genişti** — `AlwaysScrollableScrollPhysics()` tek başına verilince platform fiziği tümden düşüyor (fling simülasyonu ve sınır davranışı bozuluyor); iki test kırıldı (`ads_screen` tek hamlelik 4000 px sürükleme, `home_screen` `scrollUntilVisible`) → **`applyTo` ile mevcut fiziğin üstüne** eklendi. Regresyon testi hatayı gerçekten yakalıyor (düzeltme kaldırılınca kırmızı).
  2. **Başarı diyaloğu açıkken "Bildirimi gönder" arkada dönmeye devam ediyordu** (`AppButton.loading` sonsuz animasyon) → gösterge diyalog **açılmadan önce** kapanıyor. **11.10'daki kampanya kod modalı hatasının birebir tekrarı**; testte "pumpAndSettle timed out" olarak yakalandı.
- **Tuzaklar:** `FilterChoiceChip.onTap` **nullable değil** → devre dışı bırakmak için gövdede erken dönüş; misafir formunda başa uyarı şeridi eklenince gönder butonu ekran dışında kalıyor ve `ListView` tembel olduğu için hiç kurulmuyor → testte elle sürükleme; `adb shell input text` **boşlukta kesiyor** → `%s`; POST gövdesi testte `String` değil `Map` (`jsonDecode` gerekmez).

### 11.13 — Bildirimler tamamlama + FCM Push — [x] ✅ TAMAMLANDI (2 Ağustos 2026, 4. oturum)
- [x] **Bildirim merkezi:** `GET /v1/notifications?page=` (okunmamış rozet `data.unreadCount`), okundu (`PATCH …/{id}/read`), tümü okundu (`POST …/read-all`), boş/yükleniyor durumları. ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
- [x] **FCM:** `firebase_core`+`firebase_messaging` kur; izin iste; token al → giriş sonrası `POST /v1/notifications/fcm-token` (11.3 stub'ı gerçekle — **yalnız `deviceFcmTokenProvider` override edilecek**, çağıran kod değişmeyecek). Ön plan/arka plan/terminated handler'ları. ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
- [x] **Deep-link:** push `data.notificationId/relatedType/relatedId` → `go_router` ile ilgili ekrana git + `PATCH …/read`. **Hedef rotalar 11.6-11.11'de zaten hazır** (`/duyurular/:id`, `/kesintiler/:id`, `/ilanlar/:id`, `/etkinlikler/:id`, `/kampanyalar/:id`, `/vefat/:id`, `/taksi/:id`, `/mekanlar/:id`) ve `kAppModules` **12/12 `ready`** → eşleme tek listeden türetilebilir. ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
- **⚠️ FİREBASE DURUMU (2 Ağustos 2026'da denetlendi, aynı gün service-account bağlandı):**
  - ✅ **Var:** `mobile/android/app/google-services.json` (proje `kadirliapp`, paket `app.kadirli`) + `mobile/ios/Runner/GoogleService-Info.plist` (bundle `app.kadirli`). **Commit EDİLMEZ** — artık `.gitignore`'da.
  - ✅ **BAĞLANDI — service-account JSON** (2 Ağu): kullanıcı indirdi → **`secrets/firebase-service-account.json`** (klasör `.gitignore`'da, `chmod 600`; yer tutucu + nasıl edinileceği **`secrets/README.md`**'de, o dosya commit edilir). `appsettings.Development.json` → `Fcm:Provider="Firebase"`, `ServiceAccountKeyPath="../secrets/firebase-service-account.json"` (yol **`KadirliApp.Api/` dizinine göre** çözülür, `FileStorage:UploadDirectory` ile aynı kural). **Doğrulandı:** API açılışında `FCM push sağlayıcısı hazır (...)` logu düşüyor. `appsettings.json` varsayılanı bilinçli olarak `"None"` bırakıldı (yapılandırılmamış ortam sessizce no-op).
  - 🐛 **BAĞLARKEN ÇIKAN GERÇEK HATA (düzeltildi):** `FcmPushService` **10.11'den beri bozuktu ama hiç çalıştırılmamıştı** (`Fcm:Provider` hep `"None"` olduğu için sınıf hiç kurulmuyordu). FirebaseAdmin **.NET** SDK'sında `FirebaseApp.GetInstance(name)` uygulama yoksa `ArgumentException` **fırlatmaz, `null` döndürür** (Java SDK'sı fırlatır; kod ona göre yazılmış) → `catch` hiç çalışmıyor, `Create` hiç çağrılmıyor, `GetMessaging(null)` *"App argument must not be null"* ile patlıyordu. Gerçek anahtar bağlanır bağlanmaz **Hangfire `send-push-notifications` işi dakikada bir hata verdi.** Düzeltme: `GetInstance(...) ?? Create(...)` + tüm kurulumun `try/catch`i (bozuk anahtar da çökertmemeli — sınıfın sözleşmesi bu). **4 yeni unit test** (`Unit/Infrastructure/FcmPushServiceTests.cs`, sahte ama biçimsel olarak geçerli service-account üretir, ağa çıkmaz) — düzeltme geri alınınca **2'si kırmızı oluyor**, yani hatayı gerçekten yakalıyorlar. `dotnet test` **90 → 94**.
  - 📌 **Ders (11.14 `ARCHITECTURE.md`'ye görünmez sözleşme olarak yazılacak):** yapılandırma bayrağıyla kapatılmış kod yolu **hiç test edilmiyor demektir**; bayrak açıldığı gün ilk kez çalışır. 10.11'de yazılan bu sınıfın 90 testlik süitte hiçbir karşılığı yoktu.
  - ❌ **Eksik — APNs Auth Key (.p8)** (iOS'a push **düşmesi** için). **Apple Developer Program üyeliği gerektirir (yıllık ücretli)** ve push gerçek iPhone ister — simülatörde güvenilir test edilemez. Yoksa iOS push'u 11.16'ya (yayın, Apple hesabı zaten orada zorunlu) ertelenir.
  - 📌 **Sonuç:** Backend push **gönderime hazır**; 11.13'te Android'de uçtan uca doğrulanabilir. iOS push APNs gelene kadar (11.16) açık kalır.
- **Bitti kriteri:** Bildirim listesi + okundu/tümü-okundu çalışır; service-account bağlı olduğu için Android'de gerçek cihazda push alınır ve dokununca deep-link çalışır. APNs yoksa iOS push'u 11.16'ya devredilir ve bu **açıkça not edilir** (sessizce atlanmaz).
- **Bitti kriteri: ✅ karşılandı** (Android Pixel_9 + çalışan API + Hangfire + gerçek FCM, canlı). Teslim edilenler:
  - [x] **Bildirim merkezi** (`features/notifications/`): sayfalı liste + sonsuz kaydırma, **gün gruplaması** ("Bugün / Dün / 12 Ağustos"), okunmamış vurgusu (dolu ikon zemini + kalın başlık + nokta), tür rozeti (`relatedType` → Türkçe etiket + ikon), **"Okunmamışlar" filtresi** (uçtaki `?unreadOnly=`), **"Tümünü okundu yap"**, pull-to-refresh, boş/yükleniyor/hata durumları, misafirde `SignInPrompt`.
  - [x] **FCM**: `firebase_core` + `firebase_messaging`, Android `google-services` gradle eklentisi + `POST_NOTIFICATIONS` izni; izin isteme, token alma/kaydetme, **token yenileme**, ön plan / arka plan / kapalı-uygulama handler'ları.
  - [x] **Deep-link**: push `data.relatedType` + `data.relatedId` → `notificationRouteFor` → mevcut detay rotaları; okundu işaretleme + gezinme **tek yoldan** (`PushCoordinator.openNotification`) → liste dokunuşu ile push dokunuşu **aynı kodu** çalıştırıyor.
- **KARARLAR:**
  1. 🔑 **`PushMessaging` arayüzü + `NoopPushMessaging`** — Firebase yapılandırma dosyaları depoda tutulmuyor (`.gitignore`); `Firebase.initializeApp()` başarısız olursa uygulama **çökmüyor, push'suz açılıyor**. Bu, backend'deki `Fcm:Provider=None` no-op kararının birebir istemci aynası. Gerçek sağlayıcı `main.dart`'ta override ediliyor (`sharedPreferencesProvider` deseni) → **widget testleri Firebase kanalı olmadan koşuyor.**
  2. 🔑 **11.3'ün sözü tutuldu:** `deviceFcmTokenProvider` 11.3'te "yalnız bu provider değişecek, çağıran kod değişmeyecek" diye yazılmıştı — gerçekten **yalnız o provider** gerçeklendi, `registerAfterLogin` çağrı yeri hiç değişmedi. Provider artık **önce izin, sonra token** istiyor (izinsiz `getToken()` Android 13+/iOS'ta işe yaramıyor).
  3. **Liste `PagedFeedController` üstünde**, filtre `unreadOnly` **sunucuda** uygulanıyor (istemcide süzmek sayfalamayı tutarsız yapardı — 11.11/11.12 kararlarının aynısı).
  4. ⚠️ **Okunan satır "yalnız okunmamışlar" görünümünde gözünün önünde KAYBOLMUYOR** (bir sonraki tazelemede düşer): kaybolan satır kullanıcıya "yanlış şeye mi dokundum?" dedirtiyor.
  5. **Okundu işaretleme iyimser** (11.8 favori kalbinin deseni); 404 hatasında geri alınmaz (bildirim gerçekten yok), diğer hatalarda satır eski hâline döner.
  6. ⚠️ **Sınıf adı `AppNotification`** — Flutter'ın kendi `Notification` sınıfı `material.dart` ile geliyor, aynı adı kullanmak her ekranda `hide` gerektirirdi.
  7. **Deep-link'te uydurma rotaya gidilmez**: tanınmayan `relatedType` ya da **GUID biçiminde olmayan** kimlik → gezinme iptal, ama okundu işaretlemesi yine yapılır. (Yol enjeksiyonu denemesi de böylece eleniyor; testte var.)
  8. **Rozet sunucu-otoriter kalıyor**: mutasyondan sonra `unreadNotificationCountProvider` invalidate ediliyor (`limit=1` ile en küçük yanıt). İyimser sayaç tutmak, iki ekranın farklı sayı göstermesi riskini getirirdi.
- **⭐ PLANDIŞI (bilinçli):**
  - 🔑 **Ön planda gelen push için uygulama içi şerit** — uygulama açıkken sistem bildirimi **gösterilmez** (FCM davranışı), yani kullanıcı olan bitenden habersiz kalırdı. `SnackBar` + **"Görüntüle"** eylemi aynı deep-link'i çalıştırıyor. Ayrıca ön plan mesajı geldiğinde **rozet ve liste kendiliğinden tazeleniyor**.
  - 🔑 **Bildirim izni şeridi** — izin kapalıyken liste çalışır ama cihaza push düşmez; kullanıcı bunu bilmezse "bildirim gelmiyor" der. Şerit + "Bildirimlere izin ver" düğmesi.
  - **Gün gruplaması** (`buildNotificationRows` saf mantık, testte doğrudan çağrılıyor) · **tür rozeti/ikonu** (`NotificationKind`, tanınmayan tür genel zile düşer, kaybolmaz) · **AppBar'da "Bildirim tercihleri" kısayolu** (Ayarlar'daki 6 anahtara) · **35 yeni test (toplam 613)**.
- 🐛 **CANLI TESTİN YAKALADIĞI GERÇEK HATA:** **Android'de izin hiç sorulmamışken de `getNotificationSettings()` `denied` döndürüyor** (`notDetermined` pratikte yalnız iOS'ta görülüyor). İlk sürümde "İzin ver" düğmesi yalnız `notDetermined` durumunda çiziliyordu → Android kullanıcısı uyarı şeridini görüyor ama **hiçbir şey yapamıyordu (çıkmaz sokak)**. Düğme artık her iki durumda da var; istendiği hâlde yine reddedilirse metin **telefon ayarlarına** yönlendiriyor. Emülatörde doğrulandı: düğme → sistem diyaloğu → "Allow" → şerit kayboldu, `POST_NOTIFICATIONS granted=true`.
- 🐛 **TESTİN YAKALADIĞI TAŞMA (bu tuzağın ALTINCI tekrarı, 11.7-11.12):** filtre şeridindeki `Row` ("Okunmamışlar" chip'i + "Tümünü okundu yap" metin butonu) **360 dp'ye 113 px taşıyordu**; 1.4 yazı ölçeğinde daha da kötüydü. "Tümünü okundu yap" **AppBar ikonuna** taşındı (tooltip'li) → satır artık taşamaz.
- 🐛 **iOS DERLEMESİ KIRILDI ve düzeltildi:** `firebase_core` **en az iOS 15** istiyor, proje `IPHONEOS_DEPLOYMENT_TARGET = 13.0` idi → simülatör derlemesi *"increase your app's minimum platform version from 13.0 to at least 15.0"* ile durdu. Hedef **15.0**'a çekildi (`Runner.xcodeproj/project.pbxproj` 3 yerde + `Podfile`'da `platform :ios, '15.0'`). ⚠️ **Yayın etkisi (11.16'da hatırlanacak):** uygulama artık iOS 13-14 cihazlarda kurulamaz; bu, Firebase'i kullanmanın bedeli ve bilinçli kabul edildi.
- **Tuzaklar:** `flutter analyze` **`build/ios/SourcePackages/`** altındaki eklenti kopyalarını da tarıyordu (firebase_messaging'in kendi mock'ları farklı `mockito` sürümüne göre yazılmış) → **85 sahte hata**; `analysis_options.yaml`'a `exclude: build/**` eklendi. · `find.bySemanticsLabel` semantik ağaç kapalıyken 0 döner (11.8) → okunmamış noktası `ValueKey` ile test ediliyor. · Provider testlerinde sabit `Future.delayed` flaky → `waitUntil`. · Arka plan handler'ı **üst düzey fonksiyon + `@pragma('vm:entry-point')`** olmalı (ayrı isolate).
- **Doğrulama (canlı, uçtan uca):** Panelden duyuru yayınlandı → **8 bildirim satırı** üretildi (`related_type='announcement'`) → Hangfire `send-push-notifications` işi **FCM'e gerçekten iletti** (`fcm_sent=true`, `fcm_sent_at` dolu, `fcm_error` boş) → **cihazda ön plan şeridi belirdi**, liste kendiliğinden tazelendi ("Bugün" başlığı + "1 dakika önce" + okunmamış noktası), **alt sekme rozeti 1 oldu** → "Görüntüle" → **duyuru detayı açıldı** → DB'de `is_read=true` + `read_at` dolu. Token kaydı `onTokenRefresh` boru hattıyla **kendiliğinden** yapıldı (giriş beklemeden). `flutter analyze` **0**, `flutter test` **613/613**, `dotnet test` **161/161**.
- ❌ **iOS push 11.16'ya DEVREDİLDİ (açıkça not ediliyor, sessizce atlanmadı):** APNs Auth Key (`.p8`) **Apple Developer Program üyeliği (yıllık ücretli) gerektiriyor** ve push **gerçek iPhone** ister — simülatörde güvenilir test edilemez. iOS'ta uygulama derleniyor, bildirim **listesi/okundu/deep-link çalışıyor**; eksik olan yalnız cihaza push **düşmesi**. Apple hesabı 11.16'da zaten zorunlu.

### 11.14 — Devir Teslim: mimari haritası + backend emniyet ağı + CI — [x] ✅ TAMAMLANDI (2 Ağustos 2026, 3. oturum)

> **Neden bu faz var (2 Ağustos 2026'da eklendi):** Proje sahibinin ailesinden gelen ve **haklı** olan soru: *"Yarın biz bir modül eklemek, değiştirmek ya da kaldırmak istersek yapının bozulmayacağını nereden bileceğiz? Neyin nerede ve ne amaçla olduğunu nasıl bulacağız?"* Yapının kendisi bu soruyu büyük ölçüde zaten cevaplıyor (20 backend modülünün **tamamı** `Features/X/{Commands,Queries,Dtos}`, mobil modüllerin tamamı `features/x/{application,data,presentation}`, katman ihlali **derlenmiyor**, modül kaydı tek listede ve "ölü buton yok" testle denetleniyor, mobilde 578 test). **Eksik olan yapı değil, o yapıya dışarıdan girişi sağlayan iki şey:** (a) kronolojik karar günlüğü değil **harita** niteliğinde bir doküman, (b) backend tarafında **iş kurallarını kilitleyen** testler. Bu faz o iki açığı kapatır.
>
> ⚠️ **Konumu bilinçli:** 11.13'ten sonra, cila (11.15) ve yayın (11.16) fazlarından önce. Daha erken yapılırsa modüller hâlâ değiştiği için doküman anında çürür; daha geç yapılırsa yayın telaşına karışır.

- [x] **`ARCHITECTURE.md` (kök dizinde) — projenin haritası.** `DOTNET_MASTERCLASS.md` "sıfırdan nasıl inşa edilir" rehberi, `Progress.md` kronolojik karar günlüğü; **ikisi de "şu an neyin nerede olduğunu" soran birine cevap vermiyor.** Bu doküman verecek: ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  - **Modül envanteri tablosu:** her modül için → backend dosyaları (entity / configuration / commands / queries / dtos / controller / panel controller+view) + public uçlar + mobil dosyaları (repository / providers / ekranlar / testler). "İlan fiyatı doğrulaması nerede?" sorusu tabloya bakarak cevaplanmalı.
  - **Katman diyagramı + kural:** `Domain ← Application ← Infrastructure ← Api/Web`; **yanlış yön derlenmez** (proje referanslarıyla zorlanıyor, disiplin meselesi değil) — bu güvence açıkça yazılmalı.
  - 🔑 **"Modül EKLE" reçetesi** — dokunulacak dosyaların tam listesi, sırasıyla (entity → configuration → migration → command/query/dto → controller → izin adı → panel → `API_CONTRACT.md` → mobil repository/model/provider/ekran → `app_modules.dart` + `app_routes.dart` + router → testler).
  - 🔑 **"Modül DEĞİŞTİR" reçetesi** — özellikle **DTO alanı eklemek/çıkarmak** (additive güvenli, alan silmek kontrat kırar), uç davranışı değiştirmek, sıralama/sayfalama değiştirmek.
  - 🔑 **"Modül KALDIR" reçetesi** — feature klasörü + kayıt satırı + rota + izin + panel + testler; **soft-delete'li veriye ne olacağı** dahil.
  - 🔑 **"GÖRÜNMEZ SÖZLEŞMELER" listesi** — kodun kendisine bakarak anlaşılmayan, bozulunca **sessizce** hasar veren bağımlılıklar. Bilinenler (Progress geçmişinden toplanacak, en az şunlar): `GetPowerOutagesQuery` **bilerek sayfalamıyor** çünkü mobil 11.4/11.6 süren/planlı ayrımını tam listeye bakarak yapıyor · `announcements` NOT_FOUND **200 + `success:false`** quirk'i (istemci buna göre yazıldı) · `GET /v1/ads/{id}` **her çağrıda `view_count` artırır ve artıştan ÖNCEKİ değeri döner** · taksi/ulaşım arama parametresi **`searchTerm`**, diğerlerinde `search` · `places.amenities` **jsonb ama DTO `string`** → JSON içeren metin gelir · `dutyDate`/`eventDate`/`funeralDate` **"TR günü 00:00 UTC"** → saat dilimi kaydırılmaz · ulaşım saatleri **tarihsiz duvar saati**, iki farklı biçimde (`"07:00"` / `"06:30:00"`) · `UpdateMyAdCommand` görsel **sırası/kapağı** kavramını bilmiyor.
  - **Test haritası:** hangi katmanda ne test ediliyor, yeni kod için hangi test yazılır, `flutter test` / `dotnet test` nasıl koşulur, bilinen test tuzakları (yüzey boyutu, kalıcı vs geçici hata, `pumpAndSettle` + `Timer`, shimmer sonsuz animasyonu).
- [x] **Backend iş kuralı testleri — en büyük gerçek açık.** Bugün **41.000 satır C#'a karşılık 90 test** var ve çoğu uç seviyesinde entegrasyon testi; handler'lardaki iş kuralları neredeyse çıplak. (Karşılaştırma: mobilde 43.000 satıra **578 test**.) Yani **bugün biri bir handler'ı değiştirse testler bunu büyük ihtimalle yakalamaz.** Öncelik sırasıyla kilitlenecek kurallar: ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  - **İlan yaşam döngüsü:** moderasyon geçişleri (`pending→approved/rejected`, red gerekçesi zorunlu), **uzatma hakkı** (yalnız `approved`/`expired`, `maxExtensions` dolunca 409), süre dolması, soft-delete'in listelerden düşürmesi.
  - **`AdSubmissionRules` / dinamik kategori alanları:** zorunlu alan, `select` **seçenek metniyle** doğrulama, sayısal alanda `InvariantCulture`, boolean'ın metin taşınması.
  - **Görünürlük kuralları:** public uçların **yalnız onaylı + silinmemiş + süresi geçmemiş** kayıt döndürmesi (modül modül; `PublicVisibilityTests` var ama dar).
  - **Yetki:** rol bazlı panel/admin uçları, `[Authorize]` kaçağı olmaması (mevcut `PublicEndpointAuthorizationTests` genişletilir).
  - **Sayaçlar ve tekillik:** `view_count`/`phone_click`/`whatsapp_click` artışı, kampanya `code_view` **aynı kullanıcıda ikinci kez artmaz**, taksi `total_calls`.
  - **Hedef: `dotnet test` 90 → ~160-180**, ve **her yeni testin gerçekten bir kuralı kilitlediği** (kural bozulunca kırmızı olduğu) doğrulanır — 11.12'de pull-to-refresh regresyon testinde yapıldığı gibi.
- [x] **CI (GitHub Actions):** her push/PR'da `dotnet build` + `dotnet test` + `flutter analyze` + `flutter test`. Testler "biri hatırlarsa" değil **otomatik** koşsun; kırık commit ana dala giremesin. ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
- [x] **`CLAUDE.md` (kök):** yeni bir oturumun/geliştiricinin ilk 30 saniyede okuyacağı özet — proje ne, nasıl çalıştırılır, hangi dokümanı ne zaman okumalı (`ARCHITECTURE.md` = harita, `DOTNET_MASTERCLASS.md` = referans, `Progress.md` = geçmiş, `API_CONTRACT.md` = istemci kontratı), değişmez kurallar (kontrat additive, katman yönü, "işlevsiz buton yok", Türkçe arayüz). ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
- [x] **Doküman çürüme önlemi:** `ARCHITECTURE.md` modül tablosunun gerçekle uyumunu denetleyen **küçük bir test** (mobil `kAppModules` ↔ rota ↔ ekran zaten test ediliyor; aynı fikir backend modül listesi için de kurulur) — böylece doküman yalan söylemeye başlayınca süit kırmızı olur. ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
- **Bitti kriteri:** Projeye ilk kez bakan biri **`ARCHITECTURE.md`'yi okuyarak** (bana ya da geçmiş oturumlara sormadan) yeni bir modül ekleyebilir, mevcut bir modülde alan değiştirebilir ve bir modülü kaldırabilir; `dotnet test` iş kurallarını kilitliyor (kasten bozulan bir kural testi kırıyor); CI yeşil ve her push'ta koşuyor.
- **Bitti kriteri: ✅ karşılandı.** Teslim edilenler:
  - [x] **`ARCHITECTURE.md` (kök, ~380 satır)** — 10 bölüm: sistem şeması + **katman kuralı** ("yanlış yön derlenmez", proje referans grafiğiyle birlikte) · klasör haritası (backend + mobil) · **20 satırlık modül envanteri** (her modül için `Features/` klasörü + public uçlar + panel controller + **izin adı** + mobil `features/` klasörü + mobil rotalar) + Hangfire iş tablosu · **§4 "Modül EKLE" 18 adımlı reçete** (entity → configuration → migration → feature → controller → izin → panel → kontrat → mobil model/provider/ekran/rota/`kAppModules` → testler → doküman) · **§5 "Modül DEĞİŞTİR"** (additive vs kırıcı alan değişikliği, üç adımlı alan silme planı, sıralama/sayfalama, ortak bileşen uyarısı) · **§6 "Modül KALDIR"** (mobil önce → uç → panel → application → izin → **veri: soft-delete'li satırlar için "tabloyu düşürme" tavsiyesi + `pg_dump` yedeği** → doküman → testler) · **§7 GÖRÜNMEZ SÖZLEŞMELER (15 madde tablo + 5 kod-dışı madde)** · **§8 test haritası** (hangi katmanda ne, nasıl koşulur, yeni kod için hangi test, **bilinen test tuzakları** backend+mobil) · §9 çalıştırma + yapılandırma anahtarları · §10 değişmez kurallar.
  - [x] **Backend testleri 94 → 161** (+67; hedef 160-180 tutturuldu). Dört yeni dosya: (1) **`Unit/Application/Ads/AdSubmissionRulesTests.cs` (40 test)** — içerik kuralları (başlık/açıklama/fiyat/telefon; kullanıcı gönderiminde cep formatı, panelde sabit hat serbest) + **dinamik kategori alanları** (zorunlu alan yalnız kullanıcıda, boşluk-değer eksik sayılır, yabancı kategori property'si, Number/Boolean/Select/MultiSelect/Text kuralları, kırpma) + görsel sahipliği. ⚠️ Sınıf `internal` → `KadirliApp.Application.csproj`'a **`InternalsVisibleTo`** eklendi. (2) **`Integration/Contracts/InvisibleContractsTests.cs` (12 test)** — §7 tablosunun her satırı. (3) **`Integration/Security/EndpointAuthorizationSweepTests.cs` (5 test)** — **yapısal**: `EndpointDataSource`'tan gerçek uç tablosu okunuyor, "her `/v1/admin/*` yetki ister", "anonim yazma uçları TAM OLARAK şu 9 tanedir", "`/v1/users/me*` hepsi korumalı", "Admin klasöründeki her controller `AdminApiControllerBase`'den türer" → **yeni uç eklendiğinde test kendiliğinden kapsıyor**, kimsenin listeyi güncellemesi gerekmiyor. (4) **`Integration/Security/ModuleVisibilitySweepTests.cs` (5 test)** — mevcut `PublicVisibilityTests` çoğunlukla DETAY ucunu deniyordu; bunlar **liste** seviyesinde (rehber/mekan pasif, etkinlik pending+rejected+soft-delete, vefat pending, kampanya süresi dolmuş+pending).
  - [x] **`Integration/Architecture/ArchitectureDocTests.cs` (5 test) — doküman çürüme önlemi.** `ARCHITECTURE.md` ↔ gerçek: her `Features/X/` klasörü tabloda geçiyor mu, tabloda artık olmayan modül var mı, her mobil `features/x/` klasörü geçiyor mu, admin controller sayısı doğru mu, vaat edilen bölümler duruyor mu. **Gerçekten çürümeyi yakaladığı doğrulandı**: tabloda `Transport/` → `Ulasim/` yapılınca iki test birden kırmızıya döndü.
  - [x] **CI**: `.github/workflows/mobile.yml` **yeni** (Flutter 3.44.2 pinlenmiş; `flutter analyze` + `flutter test` + ardından debug APK derlemesi). Mevcut `dotnet.yml` zaten `dotnet build` + `dotnet test` koşuyordu → **denetlendi**, `services:` blokları ve "Apply Migrations" adımının Testcontainers yüzünden **gereksiz** olduğu not düşüldü (çalışan hattı doğrulamadan değiştirmemek için bırakıldı).
  - [x] **`CLAUDE.md` (kök)** — 30 saniyelik giriş: proje ne, üç komutla nasıl çalıştırılır, nasıl denetlenir, **hangi dokümanı ne zaman okumalı** tablosu, 8 değişmez kural, "yeni modül mü ekleyeceksin?" yönlendirmesi.
- 🐛 **TESTİN YAKALADIĞI GERÇEK BACKEND HATASI (bu oturumun en önemli bulgusu):** `AdSubmissionRules` sayısal kategori alanlarını `decimal.TryParse(value, **NumberStyles.Number**, InvariantCulture)` ile doğruluyordu. `NumberStyles.Number` **`AllowThousands` içerir** ve .NET grup boyutlarını denetlemez → Türkçe ondalık gösterimi olan **`"2020,5"` doğrulamadan GEÇİYOR** ve sayı olarak okunduğunda **`20205`** çıkıyordu (10 kat sapma, hiçbir uyarı yok). Aynı şekilde `"1,000"` de geçiyordu. Kural sıkılaştırıldı: **`AllowLeadingSign | AllowDecimalPoint`** — binlik ayracı hiç kabul edilmiyor, ondalık ayracı yalnız nokta, mesaj da açık ("ondalık ayracı nokta"). ⚠️ **Risk denetlendi:** `ad_property_values` tablosunda hiç sayısal değer yoktu (0 satır) ve panelin property arayüzü yok → mevcut veri/akış etkilenmiyor; mobil zaten girişte virgülü noktaya çeviriyor (11.9'da yazılmış). **Canlı doğrulandı:** `"150,5"` ve `"150,000"` → **400** + Türkçe mesaj, `"150.5"` → **201**; Android'de kullanıcı olarak **"150,5" yazıldı, veritabanına "150.5" yazıldı**.
- 🐛 **MOBİLDE İKİ SAATE BAĞIMLI TEST (11.10 dersinin akşam sürümü, düzeltildi):** `transport_screen_test` "sıradaki kalkışla listelenir" ve `deaths_screen_test` "geri sayım yalnız bugünkü cenazede üretilir" testleri **duvar saatine** bağlıydı — ilki sabit `07:00/10:30/14:00/17:30` fixture'ı yüzünden **akşamdan sonra** (bugünkü seferler bitti → "Yarın 07:00"), ikincisi `timeUntilFuneral()`'a `now` verilmediği için **23:59'dan sonra** kırmızıya dönüyordu. Oturum 21:45'te koştuğu için ikisi de yakalandı. Düzeltme: ulaşım fixture'ı saatleri **şimdiye göre** üretiyor (gece yarısına taşmayı da kolluyor), vefat testi zamanı **sabitliyor** (`now:` parametresi zaten vardı, kullanılmıyordu). 📌 Ders: 11.10'da "fixture tarihi `DateTime.now()` olmasın" öğrenilmişti; bu kez sorun tarihte değil **saatte**ydi — aynı sınıfın üçüncü tekrarı.
- **⭐ PLANDIŞI (bilinçli):**
  - 🔑 **Görünmez sözleşmelerin TESTLE kilitlenmesi.** Plan yalnız "`ARCHITECTURE.md`'ye liste yaz" diyordu; liste yazmak onu **korumaz**. `InvisibleContractsTests.cs` §7 tablosunun her satırına bir test koyuyor → doküman ile kod birlikte yürümek zorunda. Test dosyasının başında "bu testler neyi test ediyor ki?" diye silinmemesi için gerekçe yazılı.
  - 🔑 **Yetki denetiminin YAPISAL yapılması.** Mevcut `PublicEndpointAuthorizationTests` elle yazılmış bir uç listesini deniyor; liste güncellenmezse boşluk görünmez kalıyor. Yeni süpürme `EndpointDataSource`'tan okuyor → 60+ ucun tamamı, otomatik. "Anonim yazma uçları tam olarak şu 9'u" iddiası **ilk denemede tuttu** (yani bugün kaçak yok) ve bundan sonra kaçak eklenirse kırılacak.
  - **`InternalsVisibleTo`** (`KadirliApp.Application.csproj`) — iş kuralı sınıfları `internal` kalabilsin ama birim testle kilitlenebilsin diye.
  - Doküman çürüme testinin **iki yönlü** olması (eksik modül + fazla modül) ve "envanter tablosu okunabildi mi" kontrolü (test sessizce hiçbir şey denetlemesin diye).
- **Tuzaklar:** test veritabanı yalnız `DbSeeder`'ın **lookup** verisiyle geliyor (`MockDataSeeder` yalnız `Program.cs`'te koşuyor) → yeni entegrasyon testleri **kendi verisini kurmalı** (benzersiz marker + `IAsyncLifetime` + `ExecuteDeleteAsync` temizliği); `typeof(...).Assembly.GetTypes()` **derleyici üretimi durum makinelerini** ve iç içe DTO record'larını da döndürüyor → `Name.EndsWith("Controller")` ile süz; dosya yükleme yanıtında alan adı **`cdnUrl`** (`url` değil); `POST /v1/complaints` **200** döndürüyor (201 değil); `UpdateMyAdCommand` alanları **`newImageFileIds`/`removeImageIds`** (tek bir `imageFileIds` yok); `Business` varlığında ad alanı **`BusinessName`** ve `IsActive` yok (`IsVerified` var); markdown tablosunda regex ararken doküman gövdesindeki yol örnekleri (`Jobs/`, `Entities/`) eşleşiyor → yalnız **numaralı tablo satırlarına** bak.
- **Doğrulama:** `dotnet test` **161/161** (94 → +67), `flutter analyze` **0**, `flutter test` **578/578**, canlı API + Android Pixel_9 + iPhone 17 simülatörü. Geçici veri (1 ilan + 4 property değeri) ve `uploads/` artefaktları temizlendi → `ads` oturum öncesi hâlinde (5 satır).

### 11.15 — Cilalama, durumlar ve erişilebilirlik — [x] ✅ TAMAMLANDI (3 Ağustos 2026)

- [x] **Tutarlı durumlar.** Boş/yükleniyor(skeleton)/hata(tekrar dene) zaten her ekranda vardı; eksik olan ikisi kapatıldı:
  - 🐛🔴 **`EmptyView`/`ErrorView` kaydırılamıyordu → pull-to-refresh boş ve hata durumunda SESSİZCE ÖLÜYDU.** `RefreshIndicator` jesti ancak kaydırılabilir bir alt ağaç varsa yakalar; bu iki görünüm `Center` idi. 11.6'da duyuru ve kesinti ekranlarında **tek tek** çözülmüştü (`_ScrollableState`, `_EmptyScrollable`), kalan **12 liste ekranında açıktı** — üstelik tam da kullanıcının en çok ihtiyaç duyduğu iki anda ("liste boş, yenileyeyim" / "hata aldım, tekrar deneyeyim"). Düzeltme **çağrı yerinde değil bileşenin İÇİNDE** (`ScrollableStateBody`) → yeni yazılan ekran unutamaz; yerel kopyalar silindi. ⚠️ Yüksekliği **sınırsız** ebeveynde (ör. `ListView` çocuğu) sarmalayıcı kurulmaz, içerik olduğu gibi geçer.
  - 🔑 **`OfflineBanner` 11.1'den beri yazılıydı ama uygulamada HİÇ GÖRÜNMEMİŞTİ** — `AppScaffold(offline:)` sabit `false`'du ve hiçbir ekran değer geçmiyordu (yalnız `/gelistirici/tasarim` stil kılavuzunda duruyordu). Artık `AppScaffold` sinyali kendi okuyor → **tek yerden 27 ekran**.
- [x] **Sonsuz kaydırma sonu.** Altbilgi 11 ekranda **birebir kopyalanmıştı** ve kopyalar ayrışmıştı → `core/paging/paged_list_footer.dart` (`PagedListFooter`). Kazançlar: (a) 10 ekran sayfa hatasının **sebebini** göstermiyordu (kullanıcı boş bir "Devamını yükle" düğmesi görüyordu), (b) 🐛 **eczane ekranında altbilgi HİÇ YOKTU** — 2. sayfa patlarsa liste sessizce eksik kalıyor, kullanıcı "hepsi bu kadarmış" sanıyordu.
- [x] **Hareket cilası + `reduced motion`.** Buton ölçeği ve shimmer zaten saygılıydı ama **test edilmiyordu** → `reduced_motion_test.dart`. Planın "sayaç pop" kalemi hiç yapılmamıştı (`AppDurations.pop` tanımlı ama kullanılmamış) → okunmamış rozeti **artışta** kısa bir pop oynuyor (`TweenSequence`, tek atomik geçiş; düşüşte oynamaz, hareket azaltılmışken hiç oynamaz).
- [x] **Erişilebilirlik iddiaları testle** (`test/core/accessibility_test.dart`): `textContrastGuideline` (iki tema), `androidTapTargetGuideline`, `labeledTapTargetGuideline` + **"360 dp × 1.4 ölçekte taşma yok"**.
- [x] **Türkçe metin / hata sözlüğü** (`test/core/turkish_ui_test.dart`): sözlük eksiksizliği **kaynağı tarayarak** denetleniyor (elle liste tutulmuyor → çürümez), İngilizce sızıntı taraması, teknik `NotFoundException` kalıbının elenmesi + handlerların özel Türkçe mesajının korunması.
- [x] **Ölü buton denetimi**: 11.4'ün modül ızgarası testi + 11.8/11.9'un giriş noktası testleri zaten kapsıyor; `AdCard`'ın tooltip'siz `IconButton`'ı incelendi — **bilinçli** (`Semantics` etiketi zaten var, tooltip ekran okuyucuda etiketi ikinci kez okutur).
- [x] 🆕 🔑 **GOLDEN TEST ALTYAPISI** (`test/golden/`) — bu fazın en yüksek getirili kalemi.
  - `golden_harness.dart`: `FontManifest.json`'dan **tüm yazı tiplerini** (Nunito + MaterialIcons) açıkça yükler; `expectGoldenSheet` her bileşeni **360 dp** genişlikte, **1.0 + 1.4 yazı ölçeğinde**, **açık + koyu** temada tek sayfaya dizer → bileşen başına **2 dosya**.
  - `flutter_test_config.dart`: yalnız `test/golden/` altını etkiler; **%0.5 toleranslı** karşılaştırıcı (makineler arası kenar yumuşatma farkı için; **boyut değişimi hiç tolere edilmez**). Piksel karşılaştırması `flutter_test`in kendi `GoldenFileComparator.compareLists`'iyle — yalnız eşik için `image` paketi eklenmedi.
  - Kapsam: **8 ortak bileşen + 7 modül liste kartı**, toplam **32 referans PNG**. **Tam ekran golden YOK** (her metin değişiminde kırılır, insan "güncelle geç" alışkanlığı edinir, testin değeri sıfırlanır).
  - **CI**: golden'lar `@Tags(['golden'])` ile ayrıldı; ubuntu işi `--exclude-tags golden`, **yeni `macos-latest` işi** `--tags golden` koşuyor (yazı tipi rasterleştirmesi platforma bağlı). ⚠️ `--update-goldens` CI'da **bilinçli olarak yok** — CI üretirse hatalı düzen "yeni doğru" diye kaydedilir ve test kendini onaylar. Kırılan golden'ın fark görüntüsü artifact olarak yükleniyor.
- 🐛 **GOLDEN + ERİŞİLEBİLİRLİK TESTİNİN İLK GÜNDE YAKALADIĞI GERÇEK HATA — taşma sınıfının YEDİNCİ tekrarı, ilk kez MEKANİK olarak:** `FilterChoiceChip` 1.4 yazı ölçeğinde uzun etiketle 360 dp'ye **90 px taşıyordu** (`Row` içinde çıplak `Text`) **ve** dokunma hedefi **36 dp** idi (48 dp altı). İkisi de düzeltildi: `Flexible`+ellipsis; hap görsel olarak ince kalıyor ama çevresi **gerçekten** dokunulabilir (Material'ın `MaterialTapTargetSize.padded` davranışının aynısı — kozmetik semantik boyutu değil, `InkWell` `excludeFromSemantics` + dıştaki 48 dp'lik `GestureDetector`).
- **Doğrulama:** `flutter analyze` **0**, `flutter test` **669/669** (613 → **+56**), `dotnet test` **161/161** (backend'e dokunulmadı). **Kuralı bilerek boz ölçütü uygulandı:** `ScrollableStateBody` kaldırılınca 2 test kırmızı; `AdCard` görsel boyutu 104→96 yapılınca golden **%19.67 fark** ile kırmızı; sözlüğe karşılıksız kod eklenince Türkçe testi kırmızı.
- **Canlı (Android Pixel_9 + çalışan API):** duyuru listesinde ortak altbilgi ("Toplam 3 duyuru") · **API durdurulup pull-to-refresh → "İnternet bağlantısı yok" şeridi çıktı ve okunan içerik silinmedi**, API geri gelip yenilenince şerit kayboldu · **boş durumda (tür filtresi 0 sonuç) pull-to-refresh göstergesi çalıştı** — bu hareket düzeltmeden önce ölüydü.
- ⚠️ **iOS:** yeni kodla **derlendi ve açıldı** (iPhone 17 simülatörü, açık tema doğru, çökme yok). Bu oturumda simülatörü **sürerek** (dokunarak) doğrulama yapılamadı: `mobile/tool/ios_sim.sh` koordinat eşlemesi iPhone 17'de kayıyor (giriş ölçeği ~0.2755 kullanılıyor, doğrusu ekran-kutusu/ekran-görüntüsü oranı ~0.2303). İki denemeden sonra bırakıldı — **11.16'da düzeltilmeli**. Değişen bileşenler zaten Android'de ve iki temada golden ile doğrulandı.
- **Plan dışı (bilinçli):** `ScrollableStateBody` · `PagedListFooter` · `ConnectivityStatus` + `ConnectivityInterceptor` · rozet pop · `dart_test.yaml` etiketleri · CI macOS golden işi · `mobile/README.md`'ye golden + erişilebilirlik bölümleri · `ARCHITECTURE.md` §8 test haritası güncellemesi.

### 11.15b — Emniyet ağı 2. tur: panel + önbellek + moderasyon — [x] (3 Ağustos 2026, 2. oturum)

> **Neden bu faz var (2 Ağustos 2026'da eklendi):** 11.14 backend testlerini 94 → 161'e çıkardı ama açığı **daralttı, kapatmadı**. Bugünkü oran: backend **~40.000 satır C# / 161 test (1 test ≈ 250 satır)**, mobil **~43.000 satır Dart / 613 test (1 test ≈ 70 satır)**. Backend hâlâ ince taraf.
>
> ⚠️ **Hedef bir SAYI DEĞİL.** "Şu kadar test case olsun" ölçüsü kolayca şişirilir: `[Theory]+InlineData` her satırı ayrı test sayılır, döngüyle yazılan on doğrulama tek test görünür — **aynı koruma, üç katı sayı**. Bu fazın ölçütü 11.14'teki ölçütün aynısı: **kuralı bilerek boz, test kırmızıya dönüyor mu?** Sayı (tahminen 161 → ~350-400) bunun *sonucu* olarak gelir, *hedefi* olarak değil.

- [x] 🔑 **Admin paneli (`KadirliApp.Web`) — en büyük çıplak alan.** 20 controller + Razor görünümü var, **neredeyse hiç otomatik testi yok** (bugüne kadar elle `curl` ile denendi). En az: her panel controller'ı için (a) oturumsuz erişim → giriş sayfasına yönlendirme, (b) yetkisiz rol → 403, (c) liste sayfası 200 + beklenen kolon, (d) create/update mutasyonu DB'ye yazıyor + audit log düşüyor.
  - ⚠️ Bilinen tuzaklar (10.x oturumlarından): antiforgery cookie POST'tan önce sayfanın GET'inden `-b` VE `-c` ile alınmalı; panel HTML'inde **yalnız model-bound Türkçe metin** entity-encoded, statik view metni ham UTF-8.
- [x] 🔑 **Redis önbellek geçersizleştirme.** Klasik **sessiz** hata alanı: mutasyondan sonra yanlış anahtar temizlenirse panelde güncellenen veri mobilde eski kalır ve **kimse hata almaz**. Her `ICacheable` sorgu için: (a) anahtar üretimi filtre değişince değişiyor mu, (b) ilgili mutasyon doğru cache grubunu invalidate ediyor mu, (c) invalidate sonrası uç taze veri dönüyor mu.
- [x] **Diğer modüllerin moderasyon durum makineleri.** İlanlar 11.14'te iyi kaplandı; **vefat / etkinlik / kampanya / işletme** onay-red geçişleri, red gerekçesi zorunluluğu, onay izinin temizlenmesi ve soft-delete etkileşimi aynı titizlikte kilitlenmeli (`AdsMobilePart2Tests` deseni kopyalanabilir).
- [x] **Handler seviyesi birim testleri.** Bugün yalnız `AdSubmissionRules` birim testli (11.14, `InternalsVisibleTo` ile). Diğer modüllerin iş kuralları yalnız uç üzerinden dolaylı test ediliyor → handler değişse yakalanmayabilir.
- [x] **Arka plan işleri:** `ArchiveDeathsJob` ve `PublishScheduledAnnouncementsJob` için "iki kez koşarsa mükerrer üretmez" + sınır tarihleri testleri (`ExpireAdsJob` ve `SendPushNotificationsJob` zaten kaplı).
- **Bitti kriteri:** Panelde onaysız/pasif içerik sızarsa, bir önbellek anahtarı yanlış temizlenirse ya da bir moderasyon geçişi bozulursa **`dotnet test` kırmızı olur**. Her yeni test için kural bilerek bozulup kırmızıya döndüğü **doğrulanır**.
- **Konumu bilinçli:** yayından (11.16) hemen önce — daha erken yapılırsa panel/önbellek hâlâ değişiyor, daha geç yapılırsa yayın telaşına karışır.

**SONUÇ — teslim edilenler ve bulunanlar:**

- **`dotnet test` 161 → 327 (+166).** Sayı hedef değildi, ölçüt uygulandı: **kuralı bilerek boz, kırmızıya dönüyor mu?** Dashboard moderatöre kapatılıp `UsersAdmin`'den `[PanelPermission]` silinince **4 test kırmızı**; lookup cache grubu `"lookup"` yazılınca **6 test kırmızı**.
- 🔑 **Panelin ilk test altyapısı** (`Integration/Panel/`, **98 test**): `WebPanelApplicationFactory` (⚠️ `extern alias WebPanel` şart) + `PanelClient` (antiforgery, cookie oturumu, HTML çözme). Kapsam: oturumsuz→giriş yönlendirmesi + ReturnUrl korunumu, yapısal `[Authorize]` süpürmesi, CSRF, **21 liste + 16 form sayfasının render'ı**, menü bağlantılarının çözülmesi, yazma yolu + audit izi + soft/hard delete ayrımı.
- 🔑 **Önbellek**: 10 yapısal (`CacheContractTests`) + 5 davranışsal (`CacheInvalidationTests`, gerçek Redis). Davranışsal testlerin kritik adımı iddiadan **önce** gelir: önbelleğin gerçekten **bayat veri döndürdüğü** gösterilir — o adım olmadan "invalidate çalıştı" iddiası, önbellek kapalıyken bile yeşil kalırdı.
- **Moderasyon** (13) + **arka plan işleri** (8) + **saf mantık/handler birim testleri** (33).
- 🐛🔴 **MODERATÖR HESABI ÇIKMAZ SOKAKTI (düzeltildi).** Panel 10.9(e)'den beri "Moderatör (izin matrisine tabi)" rolü ve 16 modüllük matris sunuyordu, ama **her panel controller'ı `admin,super_admin` ile sınırlıydı** → moderatör giriş yapıyor, Dashboard dâhil **hiçbir sayfayı açamıyordu**. Matris yalnız `/v1/admin/*` uçlarını etkiliyordu; onları da hiçbir istemci çağırmıyor. Düzeltme: `PanelPermissionAttribute` (izin eylemi **aksiyon adından** türetiliyor — 18 controller × ~8 aksiyona elle etiket yazılsa biri unutulurdu; yanlış türetme daima **dar** tarafa düşer), 16 controller etiketlendi, Dashboard iniş sayfası olarak açıldı, **StaffAdmin + `Dashboard/Seed` bilinçli olarak matris dışında** bırakıldı.
- ⭐ **Panel menüsü tek listeye çekildi** (`PanelMenu.Items`, 17 kopya `<a>` bloğu gitti) ve **izne göre süzülüyor** — mobildeki `kAppModules` + "işlevsiz buton yok" kuralının panel karşılığı. 🐛 Canlı testte yakalandı: Dashboard açılınca "Paneli Test Verileriyle Doldur" butonu moderatöre görünüyor ama admin'e kilitliydi → gizlendi, testle kilitlendi.
- 🐛 **`SlugHelper` Türkçe `'İ'`yi çeviremiyordu.** `ToLowerInvariant()` U+0130'ı küçültmez → slug'a ham girerdi ("İstasyon Mahallesi" → `İstasyon-mahallesi`). Slug ASCII kimlik olmaktan çıkıyor **ve** `"İstasyon"` ≠ `"istasyon"` olduğu için benzersizlik denetimi ikisini de kabul ediyordu (mükerrer mahalle). Kök sebep: 10.9'daki ortaklaştırmada büyük harf eşlemesi düşmüş, kural fiilen iki yere ayrılmıştı (`DbSeeder.Slugify` doğru, `SlugHelper` yanlış) → **`DbSeeder` artık `SlugHelper`'a delege ediyor**, tek sahip.
- 🐛 **Onaylanan kampanyada eski red gerekçesi kalıyordu** — ilanlarda 10.14(1)'de çözülmüş kural kampanyaya taşınmamıştı.
- 🐛 **Mobilde zamana bağlı golden** (plan dışı, ama `flutter test` yeşil şartı): `AnnouncementTile`/`ComplaintCard` göreli tarihi gerçek saatten hesaplıyordu → golden **her gün** kırılıyordu. `EventCard` deseniyle `now` enjekte edildi, referanslar bir kez yenilendi (PNG farkı gözle incelendi). 📌 11.10/11.14 derslerinin üçüncü biçimi; bu kez sorun fixture'da değil **widget'ın kendi saate bakmasında**ydı.
- **Canlı (Chrome + panel + API + Android):** panelden matrisle moderatör oluşturuldu → giriş yaptı, Dashboard açıldı, **menüde yalnız izinli iki modül**, `/UsersAdmin` "erişim yetkiniz yok"a düştü · panelden "İstasyon Mahallesi" eklendi → slug **`istasyon-mahallesi`**, Redis anahtarı silindi, mobilin gördüğü liste **anında** tazelendi.
- **Doğrulama:** `dotnet test` **327/327** · `flutter analyze` **0** · `flutter test` **669/669**. Geçici veri ve `uploads/` artefaktları temizlendi.

### 11.15c — Panel canlı denetimi: bulgular + "gerçek yönetim paneli" eksikleri — [x] A grubu BİTTİ (3 Ağustos 2026, 4. oturum)

> **Neden bu faz var (3 Ağustos 2026, 3. oturum'da eklendi):** 11.15b paneli **moderatör** gözüyle
> düzeltti ve testle kilitledi; ama panel bugüne dek **`super_admin` hesabıyla, canlı Chrome'da,
> sayfa sayfa** hiç gezilmemişti. Bu oturumda gezildi (18 sayfa + form/alt ekranlar, gerçek yazma
> denemeleriyle: ilan onayı, duyuru oluştur→sil, kategori özellikleri). Aşağıdaki maddelerin
> **tamamı bu gezide gözle görüldü ve kanıtlandı** — hiçbiri "olsa iyi olur" listesi değil.
>
> ⚠️ **Bu faz kod yazmadan kapanmaz ama sırası esnektir:** A grubu (hatalar) yayından **önce**,
> B grubu (eksikler) yayından sonra ilk bakım turuna bırakılabilir. C grubu (güvenlik)
> **11.16'nın içine** girmeli — canlıya çıkarken açık kalamaz.
>
> 📌 **Genel teşhis:** panel *modül modül* çok iyi (18 ekranın 15'i gerçekten iyi tasarlanmış:
> nöbet takvimi mobili uyarıyor, duyuru formunda harita+push+hedefleme var, kategori özellik
> editörü dinamik mobil formu birebir besliyor). Zayıf olan taraf **ekranların arası**: ortak
> durum-etiketi yok, ortak sayfa-dışı navigasyon yok, ortak "kim ne yaptı" izi yok.

**A. Canlı gezide bulunan gerçek hatalar (yayından önce)**

- [x] 🔴 **Dar ekranda panelde HİÇ gezinme yok — hamburger butonu ölü.** `Views/Shared/_Layout.cshtml:19`
  butonunda `id`/`onclick`/`data-*` yok ve panelde onu bağlayan JS yok; kenar çubuğu ise
  `hidden lg:flex`. **<1024 px'de menü açılmıyor**, modüller arası tek geçiş yolu adres çubuğuna
  URL yazmak. Canlı doğrulandı (500 px pencere, tıklandı, hiçbir şey olmadı). Ekran okuyucu
  etiketi de çevrilmemiş: `"Open sidebar"`. 📌 Bu, mobildeki **"işlevsiz buton yok"** kuralının
  panel ihlali — 11.15b'de menü tek listeye çekilirken bu buton gözden kaçtı.
- [x] 🔴 **Fiyatlar `¤750,000.00` görünüyor.** `Program.cs:13-14` paneli **`InvariantCulture`**'a
  sabitliyor (form ondalıkları için bilinçli), `Views/AdsAdmin/Index.cshtml:129` ise
  `Price.ToString("C2")` çağırıyor → para birimi simgesi jenerik **`¤`**, ayırıcılar ABD düzeni.
  Tek yer: diğer modüller zaten `$"₺{x:N2}"` yazıyor (`EventsAdmin`, `PlacesAdmin`). Düzeltme
  `"C"` formatını bırakıp aynı desene geçmek; **ortak bir `TL()` yardımcısı** en doğrusu.
- [x] 🔴 **Yedi listede ham İngilizce durum/rol rozeti** — CLAUDE.md kural 6 ihlali. Görülenler:
  `expired` (İlanlar), `archived` (Vefat), ayrıca `Moderator`/`SuperAdmin`/`User`
  (`UsersAdmin/Index.cshtml:84`). Sebep: her görünüm `approved`/`pending` için elle Türkçe
  yazıyor, **geri kalan her değeri gri rozetle ham basıyor**
  (`AnnouncementsAdmin:133`, `DeathsAdmin:87`, `EventsAdmin:84`, `ComplaintsAdmin:101`,
  `CampaignsAdmin:84`, `AdsAdmin:153`). 🔑 **Kök sebep panelde ortak bir "durum → Türkçe etiket +
  renk + ikon" yardımcısının olmaması** — mobilde bunun karşılığı (`AdStatus`) 11.10'da yazılmıştı.
  Bir `StatusBadge` tag helper'ı/partial'ı yedi görünümü birden kapatır ve yeni modül unutamaz.
- [x] 🔴 **Duyuru silinince bildirimleri kalıyor → mobilde ölü bildirim.** Canlı kanıt: panelden
  push'lu duyuru oluşturuldu → **9 `notifications` satırı** üretildi → duyuru panelden silindi →
  **9 satır aynen durdu**; `GET /v1/announcements/{id}` artık `NOT_FOUND` ("Duyuru bulunamadı").
  `GetMyNotificationsQuery` yalnız `UserId` + `IsRead` süzüyor, **hedefin hâlâ yayında olup
  olmadığına bakmıyor**. Kullanıcı bildirimi görür, dokunur, boş sayfaya düşer.
  Düzeltme seçenekleri: (a) silme komutunda ilgili bildirimleri de sil, (b) sorguda "hedefi
  yaşayan" süzgeci. **Aynı sınıf risk vefat/etkinlik/kampanya silmelerinde de var — hepsi kontrol edilmeli.**
- [x] 🟡 **Süresi geçmiş ilan uyarısız onaylanıyor ve panel ile vatandaş farklı şey görüyor.**
  Canlı: `expired` bir ilana "Onayla" → "İlan başarıyla onaylandı." Ama `expires_at` geçmişte
  (2026-08-02), yani **mobilde hiç görünmüyor** (`GET /v1/ads` boş döndü) ve `ExpireAdsJob` bir
  saat içinde durumu sessizce geri `expired` yapacak. Panelde **süre uzatma aksiyonu yok**
  (`POST /v1/ads/{id}/extend` ucu duruyor, panelde karşılığı yok). Onay ekranı ya süreyi
  uzatmalı ya da "bu ilanın süresi dolmuş, onaylasanız da görünmez" demeli.
- [x] 🟡 **Dashboard "Aktif İlanlar" süreyi yok sayıyor.** `GetDashboardStatsQueryHandler:30`
  yalnız `Status == "approved"` sayıyor; `expires_at` süzgeci yok. Canlı: panel **1** derken
  public uç **0** döndürdü. Aynı satırda "Toplam Duyuru" da yayınlanmamış/zamanlanmışları sayıyor.
- [x] 🟡 **`AdsAdmin` tablosu işlem sütununu ekran dışına itiyor.** 1470 px'lik pencerede tablo
  1437 px, içerik alanı 1102 px → "Onayla / Reddet / Düzenle / Sil" **yatay kaydırma olmadan
  görünmüyor**. (`overflow-x-auto` var, yani erişilemez değil ama görünmüyor.) Ayrıca bu tek liste
  **metin butonlu**, diğer 15 liste **ikon butonlu** → panel içi tasarım tutarsızlığı.
- [x] 🟡 **`StaffAdmin` izin rozetleri ham modül anahtarı basıyor** (`deaths`, `announcements`).
  Türkçe karşılıkları **`PanelMenu.Items`'ta zaten var** (11.15b'de yazıldı) — tek satırlık eşleme.
- [x] 🟡 **404 gövdesiz.** `Program.cs`'te `UseStatusCodePages*` yok; `/BuBirSayfaDegil` → **404,
  0 bayt**, bembeyaz sayfa. Markalı "Sayfa bulunamadı" + panele dönüş bağlantısı gerek.
  (`UseExceptionHandler("/Home/Error")` yalnız 500 için ve yalnız non-Development'ta.)

**B. "Gerçek bir yönetim panelinde olur, burada yok" (yayın sonrası ilk bakım turu)**

- [x] 🔴 **Denetim izi ekranı yok.** `AuditBehavior` her yazma komutunu `audit_logs`'a yazıyor ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  (canlı DB'de 6 satır var) ama **onu okuyan tek bir ekran, tek bir uç yok** — `KadirliApp.Web`
  ve `KadirliApp.Api/Controllers` içinde `AuditLog` geçen tek satır bile yok. "Bu ilanı kim, ne
  zaman sildi?" sorusu bugün ancak `psql` ile cevaplanıyor. Moderatör rolü artık gerçekten
  çalıştığına göre (11.15b) bu **kaçınılmaz** oldu. Not: `audit_logs.details` **jsonb** →
  LINQ `.Contains()` `like_escape` hatası verir, belleğe alıp süzmek gerekir (11.15b tuzağı).
- [x] 🔴 **Şehirlerarası ulaşım panelden hiç yönetilemiyor.** `TransportAdminController` (panel) ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  baştan sona **yalnız `Intracity`** komut/sorgularını çağırıyor. Oysa `Application/Features/Transport`
  içinde `CreateIntercityRouteCommand`, `CreateIntercityScheduleCommand`, `DeleteIntercityScheduleCommand`,
  `CreateIntracityStopCommand`, `DeleteIntracityStopCommand` **hazır** ve yalnız `/v1/admin/transport/*`
  uçlarından erişilebiliyor — **onları çağıran hiçbir istemci yok** (11.15b'de tespit edilen
  "karşılığı olmayan yetki" deseninin ikinci örneği). Sonuç: mobildeki **"Şehirlerarası" sekmesi,
  kalkış saatleri ve şehir içi durak zaman çizelgesi** panelden ne oluşturulabiliyor ne düzenlenebiliyor.
  🔑 Bu, listedeki **tek gerçek işlevsel boşluk** — diğer 11 modülün tamamı panelden yönetilebiliyor.
- [x] 🔴 **Silinen kayıt geri getirilemiyor.** Soft delete (`deleted_at`) her modülde var, ama ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  panelde **ne çöp kutusu ne geri alma** var. Yanlışlıkla silinen duyuru/ilan/vefat kaydı
  yönetici için kaybolmuş durumda. (`GuideItem` `ISoftDeletable` değil — silme fiziksel,
  onun için geri alma **mümkün değil**, bu ayrıca not edilmeli.)
- [x] 🟡 **Tek onay kuyruğu yok, üstelik verisi zaten hesaplanıyor.** Dashboard "Bekleyen Onaylar" ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  bir `<div>`, tıklanamıyor. `GetDashboardStatsQueryHandler` **`PendingBreakdown`**'ı
  (ilan/vefat/etkinlik/kampanya/şikayet ayrı ayrı) hesaplıyor ama `PendingBreakdown` kelimesi
  `KadirliApp.Web` ve `KadirliApp.Api` içinde **hiç geçmiyor** → hesaplanıp çöpe atılıyor.
  En küçük düzeltme: kırılımı Dashboard'da göstermek ve her satırı ilgili modülün
  `?status=pending` filtresine bağlamak.
- [x] 🟡 **Toplu işlem yok.** Hiçbir listede satır seçimi (checkbox) yok → 40 bekleyen ilan ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  tek tek onaylanıyor.
- [x] 🟡 **Dışa aktarma yok.** Hiçbir ekranda CSV/Excel çıktısı yok (kaynakta `csv`/`excel`/ ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  `Dışa Aktar` geçen tek satır bile yok). Belediyeye rapor verilecek en basit senaryo bile
  elle kopyalamaya kalıyor.
- [x] 🟡 **Bağımsız bildirim/push ekranı yok ve gönderim sonucu görünmüyor.** Push yalnız ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  duyuruya iliştirilebiliyor (`Bildirim (Push) Gönder` kutusu). "Kaç cihaza gitti, kaç hata
  aldı, `fcm_sent` oldu mu" panelde hiç görünmüyor — canlıda 9 bildirim üretildi, panel bunu
  hiçbir yerde söylemedi. `Notifications` modülünün `ARCHITECTURE.md` tablosunda panel sütunu
  zaten *(yok)*; artık bilinçli bir eksik olarak mı kalacağı karara bağlanmalı.
- [x] 🟡 **Sütun sıralaması yalnız İlanlar'da** (ve o da bir açılır liste). Diğer 15 listede ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  başlığa tıklayarak sıralama yok — tarih/görüntülenme/ad sıralaması yapılamıyor.
- [x] 🟡 **Elektrik Kesintileri ekranında hiç arama/filtre yok** — mahalle, tarih aralığı, ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  süren/planlanan ayrımı yok; oysa mobil bu ayrımı gösteriyor (11.6, istemcide hesaplıyor).
  Neredeyse tüm diğer listelerde en az bir arama kutusu var.
- [x] 🟡 **Global arama yok** (üst çubukta "her yerde ara"). Bir telefon numarasını bulmak için ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  yönetici hangi modülde olduğunu önceden bilmek zorunda.

**C. Güvenlik / yayın — bunlar 11.16'nın içine alınmalı**

- [x] 🔴 **Giriş sayfası varsayılan yönetici parolasını KOŞULSUZ yazıyor.**
  `Views/Account/Login.cshtml:51` → `Varsayılan Yönetici: admin / Admin123!`; `IsDevelopment()`
  koşulu **yok**, yani üretimde de basılır. Aynı değerler `DbSeeder.cs:16-18`'de sabit.
  Yayında: satırı ortama bağla **ve** ilk açılışta parola değiştirmeye zorla.
  ✅ **11.15c'de yapıldı (sızıntı tarafı):** satır `@inject IWebHostEnvironment Env` +
  `Env.IsDevelopment()` koşuluna alındı. ⏭️ **11.16'ya kalan:** ilk girişte parola
  değişimini ZORLAMA (kod sızıntısı kapandı, zayıf varsayılan parola duruyor).
- [x] 🔴 **Oturum iptal edilemiyor.** Cookie `ExpireTimeSpan = 8 saat` (`Program.cs:41`) ve ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  `OnValidatePrincipal` **yok** → personel silinse, banlansa veya rolü düşürülse bile
  **elindeki oturum 8 saat boyunca çalışmaya devam eder**; şifre değişimi de açık oturumları
  düşürmez. (Bu oturumda gözlemlendi: önceki oturumdan kalan moderatör cookie'si panele hâlâ
  giriyordu.) Düzeltme: `OnValidatePrincipal`'da kullanıcıyı DB'den tazele (aktif mi, silinmiş mi,
  rolü ne) — panelin izin filtresi zaten her istekte DB'ye gidiyor, maliyet marjinal.
- [x] 🟡 **Panel parola politikası zayıf:** "en az 6 karakter" (`Account/ChangePassword`), 2FA yok, ✅ *(13 Ağu 2026 açık-madde denetiminde doğrulandı: **yapılmış**, kutu işaretlenmemişti)*
  başarısız denemede hesap kilidi yok (giriş **hız sınırı** var — o taraf iyi).
- [x] 🟡 **Silme onayı tarayıcının `confirm()`'i** (21 yerde) ve **neyin silindiğini yazmıyor**
  ("Duyuruyu silmek istediğinize emin misiniz?"). Kayıt adını yazan bir modal, yanlış satırı
  silmeyi zorlaştırır.
  ✅ **11.15c'de yapıldı:** 21 inline `onsubmit="return confirm(...)"` tek bir
  **`data-confirm` özniteliğine** çevrildi (`_Layout.cshtml`'de tek dinleyici). 10 silme
  onayı artık kaydın **adını** yazıyor ("“Pazar Yeri Taşınıyor” duyurusunu silmek…").
  🔑 Yan fayda: kayıt adını inline JS dizesine gömmek **kırılgandı** — Razor öznitelikleri
  HTML-encode ettiği için tırnak içeren bir başlık ("Ali'nin arabası") JS dizesini bozardı;
  öznitelikte taşınıp `getAttribute` ile okununca bu sorun yok. Modal'a geçilmedi
  (tarayıcı `confirm()`'i korundu) — asıl eksik "neyin silindiği" idi, o kapandı.

- **Bitti kriteri:** (a) A grubunun tamamı düzeltilmiş ve **her biri için kuralı bozunca kırmızıya
  dönen bir test** yazılmış (11.14/11.15b ölçütü — özellikle "ham İngilizce durum sızmıyor" ve
  "menü <1024 px'de açılıyor" yapısal olarak denetlenebilir); (b) B grubundan hangilerinin
  yapılacağı **karara bağlanmış**, yapılmayanlar `ARCHITECTURE.md` §7'ye *bilinçli eksik* olarak
  yazılmış (sessizce unutulmamış); (c) C grubu 11.16 kontrol listesine taşınmış.
- **Konumu bilinçli:** 11.15b'nin hemen ardından — panelin **testi** yazıldı ama panelin
  **kendisi** bir yöneticinin gözüyle daha hiç denetlenmemişti. Sıralama olarak A → 11.16 → B.

#### ✅ 11.15c A grubu TAMAMLANDI (3 Ağustos 2026, 4. oturum)

**Kök sebep tek cümleyle:** A grubunun dokuz maddesinden altısı **ortak bir yardımcının
olmamasından** doğuyordu (durum etiketi, para biçimi, modül adı, silme onayı, dar ekran
menüsü, 404 gövdesi). Bu yüzden düzeltmeler **çağrı yerinde değil ortak bileşende** yapıldı —
11.15'in "yeni ekran unutamaz" ölçütü.

- 🔑 **`KadirliApp.Web/Common/PanelDisplay.cs`** — panelin ortak görsel dili: `Status()` /
  `Role()` (Türkçe etiket + renk + ikon) ve `TL()` (₺750.000,00). `Views/Shared/_StatusBadge.cshtml`
  partial'ı 7 görünümdeki if/else zincirini birden kaldırdı; her zincirin **son `else` dalı**
  ham İngilizce basıyordu. Bilinmeyen durum artık ham geçmez, **kırmızı "Bilinmeyen durum (x)"**
  olarak işaretlenir — sorun gizlenmiyor ama kullanıcıya İngilizce de sızmıyor.
- 🔑 **Dar ekran menüsü JS'siz `<details>` ile** (panelde zaten kullanılan desen). Bağlantılar
  `_MenuLinks.cshtml` + `_AccountLinks.cshtml` partial'larından geliyor — kenar çubuğuyla
  **aynı** listeyi paylaşıyorlar, ikinci kopya yok (11.15b'nin 17-kopya düzeltmesini geri
  getirmemek için). JS olmadığı için "menü gerçekten açılıyor mu" **sunucu render testiyle**
  denetlenebiliyor. Dar ekranda **çıkış yapmanın** da yolu yoktu, o da eklendi.
- 🔑 **`StaffAdminController.Modules` artık `PanelMenu.Items`'tan türüyor** — elle yazılmış
  ikinci bir anahtar+etiket kopyasıydı (görünmez sözleşme #20'nin tam da uyardığı ayrışma).
- 🔑 **21 inline `confirm()` → tek `data-confirm` dinleyicisi**; 10 silme onayı kaydın adını yazıyor.
- **Backend iş kuralı düzeltmeleri** (hepsi "panel ile vatandaş farklı gerçeklik görüyordu"
  sınıfından, hiçbiri hata vermiyordu):
  - `ApproveAdCommandHandler` süresi geçmiş ilana **taze 30 günlük pencere** verir. Koşul
    duruma değil **tarihe** bakıyor — onay kuyruğunda 30 günden fazla bekleyen `pending` ilan
    da aynı tuzağa düşüyordu. Süresi devam eden ilanın süresi uzatılmaz (onay bir uzatma aracı değil).
  - `GetDashboardStatsQueryHandler` "aktif" sayaçlarını **public sorguların görünürlük
    tanımıyla** hizaladı (`ExpiresAt > now`, `Status == "active" && VisibleUntil`).
    Etiketler de düzeltildi: "Aktif İlanlar" → **"Yayındaki İlanlar"**, "Toplam Duyuru" →
    **"Yayındaki Duyurular"**.
  - **Ölü bildirim iki katmanda** kapatıldı: `DeleteAnnouncementCommand` ilgili bildirimleri
    **fiziksel** siler (türetilmiş veri, kaynağı yoksa anlamı yok) **ve**
    `GetMyNotificationsQuery` "hedefi yaşayan" süzgeci uygular — ikincisi silme DIŞINDAKİ
    görünmezleşme yollarını (draft'a çekme, `VisibleUntil`'in geçmesi) kapatır. `unreadCount`
    **aynı** `baseQuery`'den türüyor; ayrılsaydı rozet "3" derken liste 1 satır gösterirdi.
- ⭐ PLANDIŞI (B grubundan öne alındı): **Dashboard onay kuyruğu**. `PendingBreakdown`
  10.10'dan beri hesaplanıp `Web`/`Api`'de hiç okunmuyordu. Artık modül kırılımı çiziliyor,
  her satır ilgili listenin `?status=pending` filtresine gidiyor, **sıfır olan satır
  çizilmiyor** ve satırlar **menüyle aynı izin süzgecinden** geçiyor (moderatöre "Yetkiniz yok"a
  götürecek bağlantı çizilmez — 11.15b dersi). Bunun için `QueryAdDto`'ya **additive `Status`**
  alanı eklendi; ⚠️ handler'da bilinçli `else if` — public yol (`OnlyPublished=true`) bu alanı
  **hiç okumaz**, yoksa `?status=pending` onaylanmamış ilanları iletişim telefonlarıyla açardı (10.5 emsali).
- 🐛 **MOBİLDE ZAMANA BAĞLI GOLDEN'IN DÖRDÜNCÜ TEKRARI (bu oturumda kendiliğinden kırmızıydı):**
  `NotificationTile` göreli tarihi **gerçek saatten** hesaplıyordu. 11.15b'de `AnnouncementTile`
  ve `ComplaintCard` düzeltilmiş, **bu kart atlanmıştı**. Referans PNG'nin ne kodladığı teşhisi
  doğruladı: golden üretildiği an fixture tarihi **gelecekteydi**, bu yüzden referans
  "3 Ağustos 2026, 14:40" (tam tarih fallback'i) gösteriyordu — yani referans **hatalı
  davranışın çıktısıydı**. `now` enjekte edildi, referanslar yenilendi, PNG farkı gözle
  incelendi (yalnız tarih metni değişti: → "20 dakika önce"; düzen aynı).
- **Testler: 327 → 368 (+41).** `PanelDisplayTests` (13 — sözlük eksiksizliği, iki rol string
  biçimi, `¤` dönüşünü yakalayan para testi, izin matrisi ↔ menü ayrışması),
  `PanelUsabilityTests` (9 — dar ekran menüsü, Türkçe rozet/rol, ₺ biçimi, gövdeli 404,
  onay kuyruğu bağlantısı, status filtresi), `PanelBusinessRuleTests` (9 — onay/pencere,
  Dashboard sayaçları, ölü bildirim iki katman + `unreadCount` tutarlılığı).
- **"Kuralı bilerek boz" ölçütü uygulandı:** Dashboard süzgeci + onay penceresi + `TL()` +
  `expired` etiketi geri alındığında **7 test kırmızıya döndü** (5 farklı sınıftan), geri
  yüklenince 173/173 yeşil.
- **Canlı doğrulama (Chrome + panel + API + Postgres):** 600 px pencerede hamburger **açıldı**,
  modüle gidildi (önceden hiçbir şey olmuyordu) · fiyat **₺750.000,00** · rozet **"Süresi Doldu"** ·
  `AdsAdmin` tablo taşması **335 px → 0 px** (işlem sütunu artık görünür) · `/BuBirSayfaDegil`
  **404 + 6360 bayt** (önce 0 bayt) · **süresi dolmuş ilan onaylandı → `GET /v1/ads` 0'dan 1'e
  çıktı** ve Dashboard da "Yayındaki İlanlar 1" dedi (panel ile vatandaş artık aynı sayıyı
  görüyor) · **push'lu duyuru → 9 bildirim → duyuru silindi → 0 bildirim** (geçen oturumda
  9'u ayakta kalmıştı). Geçici veri temizlendi (test duyurusu silindi, ilan `expired`'a geri alındı).
- **Doğrulama:** `dotnet test` **382/382** (327 → +55) · `flutter analyze` **0** · `flutter test` **669/669**.

#### 📌 B grubu KARARI (bitti kriteri (b))

| Madde | Karar |
|---|---|
| Tek onay kuyruğu (`PendingBreakdown` kullanılmıyor) | ✅ **11.15c'de YAPILDI** (yukarıda) |
| Denetim izi ekranı yok | ⏭️ **11.17'ye** — yayını bloklamıyor ama moderatör rolü gerçekten çalıştığı için kaçınılmaz. ⚠️ `audit_logs.details` **jsonb** → LINQ `.Contains()` `like_escape` verir, belleğe alıp süz |
| Şehirlerarası ulaşım panelden yönetilemiyor | ⏭️ **11.17'ye** — listedeki **tek gerçek işlevsel boşluk**. Komutlar `Application`'da hazır, yalnız panel ekranı yok. Yayını bloklamaz: mobil şehirlerarası sekmesi seed verisiyle çalışıyor, ama ilk saat değişikliğinde `psql` gerekir |
| Silinen kayıt geri getirilemiyor (çöp kutusu) | ⏭️ **11.17'ye**. `GuideItem` `ISoftDeletable` **değil** → onun için geri alma **mümkün değil**, bu bilinçli fark |
| Toplu işlem / dışa aktarma / global arama / sütun sıralaması | ⏭️ **11.17'ye**. Onay kuyruğu geldiği için toplu onayın aciliyeti düştü |
| Bağımsız bildirim/push ekranı, gönderim sonucu görünmüyor | ⏭️ **11.17'ye** — `Notifications` modülünün panel sütunu `ARCHITECTURE.md`'de zaten *(yok)*; **bilinçli eksik olarak kalıyor**, push duyuruya iliştirilmeye devam ediyor |
| Elektrik Kesintileri'nde arama/filtre yok | ⏭️ **11.17'ye** (küçük, tek ekran) |

> 🔴 **B grubu için Faz 11.17 açıldı** (aşağıda). Karar gerekçesi: A grubu yayını bloklayan
> *hataları* kapattı; B grubu **eksikler**dir ve hiçbiri vatandaşın gördüğü uygulamayı
> etkilemiyor. Yayın (11.16) önce gelir; 11.17 yayın sonrası ilk bakım turudur.

### 11.16 — Yayına hazırlık (release) — [x] Apple'sız kısım BİTTİ (4 Ağustos 2026, 3. oturum)

> **Kapsam kararı:** Apple Developer aboneliği **henüz alınmadı** → iOS imzalama /
> TestFlight / App Store kaydı bu oturumda **yapılamaz**. Kullanıcı isteğiyle
> Apple gerektirmeyen her madde tamamlandı; iOS'un *koda dokunan* kısmı
> (`Info.plist` izin açıklamaları) da **şimdiden** kapatıldı çünkü abonelikle
> ilgisi yok ve bir çökme sebebiydi.

- [x] **App ikonu + açılış ekranı.** `tool/generate_branding.py` — ikon bir "sanat
  dosyası" değil **türetilen** çıktı: renkler `MOBILE_UX_PLAN` token'larından, harf
  uygulamanın kendi yazı tipinden (Nunito). Marka rengi değişirse betik yeniden koşar.
  `flutter_launcher_icons` + `flutter_native_splash` yalnız boyutları türetiyor.
  🐛 **Canlıda yakalanan iki hata:** (1) açılış logosu ikonla aynı **beyaz** marktı,
  açılış zemini ise açık tema rengi (#FAF9F6) → **logo neredeyse görünmüyordu**;
  tema başına ayrı renkli görsel üretildi. (2) Android 12+ splash logoyu **dairesel
  maskeye** oturtuyor (1152 px tuvalde güvenli alan ortadaki 768 px çaplı daire) →
  filiz kesilip ekranda "K + havada duran turuncu sap" kalıyordu; `android_12` için
  daireye sığan ayrı görsel üretildi.
  ⚠️ Tasarımın kendisi de üç turda oturdu: yaprak önce K'nın koluyla birleşip
  "roket" gibi okundu, sonra harfle yarışıp 48 px'te silueti bozdu. Kural yazıldı:
  **yaprak açıkça ikincil** — küçük boyutta zarifçe kaybolması normal, silueti
  bozması değil.
- [x] **Sürümleme + flavor.** `version: 1.0.0+1`; `Env.showDevTools = isDev && kDebugMode`
  zaten iki emniyet kemeri taşıyordu (yanlış flavor'la alınan release'te bile dev
  araçları kapalı).
  🐛 **Ama gerçek bir sızıntı vardı:** `/gelistirici/tasarim` ve `/gelistirici/ag`
  rotaları **koşulsuz** kayıtlıydı. Menü girişleri gizlendiği için "yalnız debug"
  sanılıyordu; oysa rota tablosunda durdukları sürece **yayın yapısında da
  açılabiliyorlardı**. `/gelistirici/ag` yedi gerçek uca istek atıp `traceId`
  basan bir tanılama ekranı — vatandaşın elindeki uygulamada bulunmamalı.
  Kayıt artık `if (Env.showDevTools)` bloğunda.
- [x] **İzinler + gizlilik.** 🔴 **iOS'ta gerçek çökme:** `image_picker` iki yerde
  `ImageSource.camera` kullanıyor (profil fotoğrafı 11.5, vefat bildirimi 11.11) ama
  `Info.plist`'te **`NSCameraUsageDescription` yoktu** → iOS'ta kamera açılır açılmaz
  uygulama **anında çöker** (izin diyaloğu bile görünmez) ve App Store incelemesi de
  reddeder. `NSPhotoLibraryUsageDescription` ile birlikte eklendi, metinler Türkçe
  ve "neden" sorusunu cevaplıyor.
  Gizlilik politikası bağlantısı Ayarlar → Hakkında'ya eklendi (`Env.privacyPolicyUrl`,
  tek yer). ⚠️ Testi bilinçli olarak **misafir** oturumla yazıldı: bağlantı yanlışlıkla
  oturum gerektiren bir bloğa konursa mağazanın istediği kullanıcı onu hiç göremez.
  **Hesap silme** zaten karşılanmıştı (11.5).
  📌 **Ağ izni:** `INTERNET` ana manifestte yoktu — ama release'e `firebase_messaging`in
  kendi manifestinden **birleşerek giriyor** (release APK'sı dökülerek doğrulandı),
  yani bugün bir engel *değildi*. Uygulamanın can damarı bir eklentinin iç detayına
  bağlı kalmasın diye açıkça bildirildi.
- [x] **Android imzalama.** `key.properties` tabanlı `signingConfig` + R8 küçültme
  (`isMinifyEnabled`/`isShrinkResources` + `proguard-rules.pro`; FCM sınıfları
  korunuyor, yoksa push **sessizce** çalışmazdı). `key.properties`/`*.jks` `.gitignore`'da,
  örnek dosya + `secrets/README.md`'de anahtar üretme adımları.
  🔑 **Kapı nereye konuldu, neden:** ilk denemede anahtar yoksa yalnız `logger.warn`
  yazılmıştı — **`flutter build` Gradle uyarılarını yutuyor, uyarı hiç görünmedi.**
  Yani "sessizce debug anahtarıyla imzalanmış yayın yapısı" riski duruyordu. Artık
  `bundleRelease` (mağazaya yüklenen **tek** artefakt) anahtarsız **derlenmiyor**;
  `assembleRelease` (APK) çalışmaya devam ediyor — yerel yayın denemesi ve
  CI'ın `--debug` derlemesi kırılmasın diye. Yüklenemeyecek bir APK zarar veremez.
  ⏭️ **Play internal test yüklemesi:** anahtar üretimi + Play Console hesabı
  kullanıcıda; altyapı hazır, yükleme adımı bekliyor.
- [x] **Backend prod bağımlılıkları — kontrol listesi *kapıya* çevrildi.**
  Listedeki her ayarın kodda gerçekten okunduğu doğrulandı, sonra
  `ProductionReadinessGuard` yazıldı: `ASPNETCORE_ENVIRONMENT=Production` iken
  güvensiz ayar varsa uygulama **açılmıyor** ve hepsini tek seferde yazıyor.
  🔴 **En tehlikelisi `Otp:DevMode`:** açık kalırsa `POST /v1/auth/login` OTP'yi
  **yanıtın içinde** döndürür → herkes istediği numarayla giriş yapar ve bu
  hiçbir yerde hata olarak görünmez. Diğerleri: `Sms:Provider=Dev` (uç "OTP
  gönderildi" der, kimse kod almaz) · **commit edilmiş JWT sırları** (depo herkese
  açık → üçüncü kişi geçerli jeton üretebilir) · Hangfire panosu kimliksiz
  (reverse-proxy arkasında "yerel istek" kontrolü çöker).
  🐛 **Listenin kendisinde bir hata bulundu:** madde "`FileStorage:BaseUrl` prod
  domain" diyordu — **yanlış.** Görünmez sözleşme #9 görsel URL'lerinin **göreli**
  dönmesini şart koşuyor, origin'i istemci ekliyor; doldurulursa mobil
  `http://…http://…` üretir ve **hiçbir görsel açılmaz.** Kapı artık BaseUrl
  *dolu* ise engelliyor, madde de düzeltildi.
  ⚠️ `Fcm:Provider=None` **engelleyici değil** (push'suz yayın meşru tercih) —
  yalnız uyarı loglanıyor.
  ⏭️ Kalan, koda dokunmayan operasyon maddeleri: gerçek SMS sağlayıcı sözleşmesi ·
  uploads kalıcı volume (10.14/3) · CORS (yalnız web hedeflenirse).
- [x] 🔴 **Panel güvenlik kapanışı** — ✅ **tamamı 11.18'de yapıldı** (oturum iptali ·
  ilk girişte parola değişimi · parola politikası · hesap kilidi).
- ⏭️ **Apple bekleyen maddeler** (abonelik alınınca): iOS imzalama sertifikaları ·
  TestFlight · App Store Connect kaydı · **APNs `.p8`** (iOS push hâlâ bunu bekliyor) ·
  mağaza görselleri/açıklama metni.
- **Bitti kriteri:** ✅ Apple'sız kısım için karşılandı — imzalama altyapısı kurulu ve
  anahtarsız `.aab` üretimi **engelleniyor**; yayın engeli listesi artık doküman
  değil **açılış kapısı**; iki gerçek yayın hatası (iOS kamera çökmesi, dev
  rotalarının sızması) kapatıldı. ⏭️ "İnternal test kanalında çalışan uygulama"
  maddesi Play Console hesabı + anahtar üretimine bağlı.

#### 11.16 kapanış notları

- **Testler: backend 534 → 545 (+11), mobil 669 → 678 (+9).** Yeni:
  `ProductionReadinessGuardTests` (11), `test/release/release_config_test.dart` (8),
  Ayarlar'a gizlilik bağlantısı testi (1).
- 🔑 **Yeni test sınıfı: platform yapılandırması.** `AndroidManifest.xml` ve
  `Info.plist` bugüne kadar hiçbir testin uğramadığı bir kör noktaydı; oradaki
  hatalar `flutter run` ile **görünmüyor** ve ilk kez mağazadan inen uygulamada
  ortaya çıkıyor. `release_config_test.dart` bunları kaynağı tarayarak kilitliyor:
  izin gerektiren kullanım (`ImageSource.camera`) `lib/` taranarak bulunuyor, elle
  liste tutulmuyor → yeni bir kamera çağrısı yapılandırma eksikse test kendiliğinden
  kırmızıya döner.
- **"Kuralı bilerek boz" ölçütü uygulandı:** `INTERNET` izni silindi ·
  `NSCameraUsageDescription` silindi · `appsettings.json`'daki JWT sırrı değiştirildi
  (kapının yanmış-sır listesi dosyayla ayrışsın diye) → **3 test kırmızıya döndü**,
  geri alınınca 545/545 + 678/678 yeşil.
- **Canlı (Chrome + panel + API + Postgres + Android emülatörü + iOS simülatörü):**
  ikon Android çekmecesinde **yeşil K** olarak göründü · açılış ekranı açık ve koyu
  temada **ayrı ayrı** doğrulandı (marka rengi zeminle uyumlu, filiz kesilmiyor) ·
  release APK dökülüp **`INTERNET` izni** ve **debug imzası** teyit edildi ·
  anahtarsız `bundleRelease` **durdu** ve Türkçe yönergeyi bastı · uygulama
  emülatörde açıldı, oturum korundu ve gerçek duyuru API'den geldi.
- ⚠️ **Devralınan:** emülatörde eski paket kimliğinden kalma bir **`kadirliapp`
  kurulumu** duruyor (Flutter varsayılan ikonuyla) — 11.1'deki paket adı
  sadeleştirmesinin kalıntısı, repoyla ilgisi yok, elle kaldırılabilir.

### 11.17 — Panel "gerçek yönetim paneli" eksikleri (11.15c B grubu) — [x] 4/6 (4 Ağustos 2026)

> **Neden ayrı faz:** 11.15c'nin bitti kriteri (b) "B grubundan hangilerinin yapılacağı karara
> bağlanmış olmalı" diyordu. Karar: **hiçbiri yayını bloklamıyor** (hepsi *eksik*, *hata* değil;
> vatandaşın gördüğü uygulamayı etkilemiyorlar) → yayından sonraki ilk bakım turuna alındı.
> Tek onay kuyruğu maddesi ucuz ve etkili olduğu için 11.15c'de öne alınıp **yapıldı**.
>
> 📌 **11.16'dan ÖNCE yapıldı** (kullanıcı isteği): "panel ve uygulama arası API bağlantılarını
> sorunsuz hale getirelim". Kalan iki 🟡 madde (toplu işlem/dışa aktarma, bağımsız push ekranı)
> **11.18**'e alındı — gerekçe aşağıda.

- [x] 🔴 **Şehirlerarası ulaşım paneli.** Listedeki **tek gerçek işlevsel boşluk** kapandı.
  `TransportAdminController` artık iki sekmeli (`_TransportTabs` partial'ı — menüde ikinci satır
  açılmadı, mobildeki iki sekmeli ulaşım ekranının aynısı): şehir içi hatlar + **duraklar**
  (`Stops`), şehirlerarası hatlar + **kalkış saatleri** (`IntercityEdit`).
  10.8'de yazılmış ama hiç çağrılmamış komutlar (`CreateIntercitySchedule`,
  `CreateIntracityStop`, `DeleteIntercitySchedule`, `DeleteIntracityStop`) ilk kez istemci buldu;
  eksik olan ikisi (`UpdateIntercityRouteCommand`, `DeleteIntercityRouteCommand`) yazıldı.
  Ayrıca `GetIntercityRouteByIdQuery` / `GetIntracityRouteByIdQuery` (detay ekranları için).
  ⭐ **Saatsiz hat / duraksız hat listede sarı uyarıyla işaretleniyor** — panel "kaydettim"
  derken mobilde sefer görünmemesi tam olarak bu fazın kapattığı sessiz hata sınıfı.
  ⚠️ `IntercityRouteResponseDto.ScheduleDto`'ya **additive `IsActive`** eklendi: liste sorgusu
  (mobil) yalnız aktifleri döndürür, panelin tek-kayıt sorgusu pasifleri de döndürüp işaretler —
  aksi hâlde panel, mobilde **görünmeyen** bir saati yayındaymış gibi gösterirdi.
- [x] 🔴 **Denetim izi ekranı** (`AuditLogsAdmin`). `AuditBehavior` 10.9(i)'den beri yazıyordu,
  okuyan yoktu. Süzgeçler: modül · işlem · personel · tarih aralığı · **etkilenen kayıt kimliği**
  ("bu ilana ne oldu?" — çöp kutusundan da buraya bağlantı var).
  ⚠️ **`details` üzerinde serbest metin araması BİLİNÇLİ olarak yok**: kolon `jsonb`, LINQ
  `.Contains()` `like_escape(jsonb, unknown)` verir; belleğe alıp süzmek panelin en hızlı büyüyen
  tablosunu belleğe çekerdi (checklist §8). Yapılandırılmış süzgeçler aynı soruları zaten cevaplıyor.
  ⚠️ **Silinen personelin izi isimsizleşmemeli** → kullanıcı sorgusunda `IgnoreQueryFilters()`.
  ⚠️ `Enum.ToString()` ve `IPAddress.ToString()` SQL'e çevrilemez → sayfa boyu kadar satır ham
  çekilip bellekte biçimleniyor.
  🔑 **35 denetim eylemi Türkçeleştirildi** (`PanelDisplay.AuditAction`) ve sözlük **kaynak
  taranarak** kilitlendi: `AuditAction => "…"` literal'lerini bulan bir `TheoryData` her eylemin
  Türkçe karşılığı olduğunu denetliyor — yeni bir `IAuditableCommand` eklenip sözlüğe satır
  atılmazsa test kırmızıya döner. (`restore` eklenince gerçekten kırmızı oldu, sözlüğe eklendi.)
- [x] 🔴 **Çöp kutusu / geri alma** (`TrashAdmin`). Kapsam `TrashModules.Supported`'da **tek liste**
  (sorgu ve komut ondan türüyor — iki ayrı `switch` yazılsaydı "listede görünen ama geri
  getirilemeyen kayıt" doğardı): ilan, duyuru, vefat, etkinlik, kampanya, taksi.
  🔑 **Geri getirme yayına alma DEĞİL**: `RestoreRecordCommand` yalnız `deleted_at`'i temizler,
  `status`'e dokunmaz. Dokunsaydı çöp kutusu moderasyonun **arka kapısı** olurdu (reddedilmiş ilan
  → sil → geri getir → yayında). Bu, kod okunarak fark edilmeyecek bir karar olduğu için testle kilitli.
  ⚠️ `IgnoreQueryFilters()` unutulursa çöp kutusu **her zaman boş** görünür ve kimse hata almaz.
  ⚠️ Bilinçli kapsam dışı: **`GuideItem`** (`ISoftDeletable` değil, silmesi fiziksel — geri alma
  *mümkün değil*, eksik değil), **`User`** (hesap silme mağaza/gizlilik gereği; yönetici geri
  açamamalı), **`File`** (kayıt değil ek). Ekranın altında bu üçü kullanıcıya da yazılı.
- [x] 🟡 **Elektrik Kesintileri arama/filtre.** Mahalle (parçalı, harf duyarsız) · durum
  (sürüyor/planlandı/bitti) · tarih aralığı · filtrelenmiş/toplam sayaç · Türkçe durum rozeti.
  🔑 **Asıl iş süzgeç değil, zaman tanımı**: `PowerOutagePhaseRules` mobildeki
  `PowerOutage.isActive/isUpcoming/isPast` ile **birebir** (başlangıç anı dâhil, bitiş anı hariç)
  ve sınır anları testle kilitli — uç sayfalamıyor ve tarih süzmüyor (görünmez sözleşme #1),
  ayrım tümüyle istemcide olduğu için iki tanım ayrışırsa panel "sürüyor" derken vatandaş
  "planlı" görür ve **kimse hata almaz**.
  🔑 Tarih aralığı **kesişim** üzerinden: 1–3 Ağustos'u seçen yönetici, 31 Temmuz'da başlayıp
  2 Ağustos'ta biten kesintiyi de görmeli. Yalnız `StartTime`'a bakan bir süzgeç **uzun**
  kesintileri sessizce elerdi — ve tam da onlar en önemlileridir.
- ⏭️ 🟡 **Toplu işlem / CSV dışa aktarma / global arama / sütun sıralaması** → **11.18**.
  Gerekçe: dördü de *ayrı* birer ürün kararı (hangi sütunlar? hangi format? arama neyi kapsıyor?)
  ve hiçbiri bir hatayı kapatmıyor. Onay kuyruğu 11.15c'de geldiği için toplu onayın aciliyeti
  zaten düşmüştü.
- ⏭️ 🟡 **Bağımsız bildirim/push ekranı + gönderim sonucu** → **11.18**.
  📌 Hâlâ **bilinçli eksik**: `ARCHITECTURE.md` modül tablosunda `Notifications` panel sütunu
  *(yok)*; push duyuruya iliştirilmeye devam ediyor. "Kaç cihaza gitti" sorusu FCM yanıtının
  saklanmasını gerektiriyor — bu bir şema değişikliği, tek ekranlık iş değil.

#### 11.17 kapanış notları

- 🔑 **Yeni desen: yalnız admin'e açık panel ekranı.** `AuditLogsAdmin` ve `TrashAdmin`
  `[Authorize(Roles = "admin,super_admin")]` + **`[PanelPermission]` YOK** + `PanelMenu.Items`
  satırının `Module`'ü **`null`** + `AdminOnlyControllers`'a controller adı.
  Modül anahtarı verilseydi izin matrisinde moderatöre dağıtılabilen ama rol kapısı yüzünden asla
  çalışmayacak bir yetki belirirdi — **11.15b'nin en büyük bulgusunun ("karşılığı olmayan yetki")
  tekrarı olurdu.** `ARCHITECTURE.md` §3'e yazıldı, iki testle kilitli.
- **Görünmez sözleşmelere #27 (kesinti zaman tanımı) ve #28 (geri getirme yayına almaz) eklendi.**
- **Testler: 382 → 464 (+82).** `PanelTransportTests` (12), `PanelAuditLogTests` (39 — 35'i
  kaynak taramalı eylem sözlüğü), `PanelTrashTests` (12), `PanelPowerOutageFilterTests` (13),
  ayrıca smoke/auth listelerine 4 yeni sayfa.
- **"Kuralı bilerek boz" ölçütü uygulandı:** geri getirme `status`'ü `approved` yapsın ·
  kesinti bitiş anı dâhil olsun · `IgnoreQueryFilters()` kalksın · `restore` etiketi silinsin ·
  `AddSchedule` komutu göndermesin → **12 test kırmızıya döndü (4 sınıfın hepsinde)**, geri
  alınınca 464/464 yeşil.
- **Doğrulama:** `dotnet test` **464/464** · `flutter analyze` **0** · `flutter test` **669/669**.
- **Bitti kriteri:** ✅ yapılan her madde panel render + davranış testiyle kapandı;
  yapılmayan ikisi yukarıda **11.18**'e gerekçesiyle bağlandı ve `Notifications` paneli
  `ARCHITECTURE.md`'de *bilinçli eksik* olarak yazılı kaldı.

### 11.18 — Panel güvenlik kapanışı + toplu işlem + sıralama — [x] 4/4 madde (4 Ağustos 2026, 2. oturum)

> **Neden bu faz var:** 11.15c'nin C grubundan (güvenlik) iki madde ve B grubundan
> (yönetim paneli eksikleri) iki madde açık kalmıştı. Kullanıcı, kalanları tespit edip
> sıraya koymamı istedi; sıralama **"sessiz risk → günlük işi tıkayan → rahatlık"**
> ölçütüyle kuruldu ve ilk dört madde birlikte onaylandı.
>
> 📌 Doküman karmaşasının kendisi de bir bulguydu: 11.15c B grubu maddeleri Progress.md'de
> hâlâ `[ ]` işaretliydi ama beşi 11.17'de yapılmıştı. Bu faz kapanırken **her madde tek
> yerde** işaretlendi.

- [x] 🔴 **Oturum iptali (`OnValidatePrincipal`).** Çerez 8 saatlikti ve doğrulayıcı yoktu:
  personel **silinse, banlansa, pasife alınsa, rolü düşürülse bile elindeki oturum 8 saat
  çalışmaya devam ediyordu**; parola değişimi de açık oturumları düşürmüyordu. 11.15c'de
  canlıda gözlenmişti (önceki oturumdan kalan moderatör çerezi hâlâ giriyordu) — yani
  "yetkiyi geri aldım" diyen yönetici aslında hiçbir şey geri almamış oluyordu.
  `PanelPrincipalValidator` artık her istekte kullanıcıyı DB'den tazeliyor.
  🔑 **Rol değişimi oturumu düşürmez, TAZELER** (`ReplacePrincipal`): rolü düşürülen kişi
  çalışmaya devam eder ama artık *yeni* rolüyle. Atsaydık rol **yükseltmesi** de kullanıcıyı
  sebepsizce dışarı atardı.
  ⚠️ **`IgnoreQueryFilters()` bilerek YOK**: silinmiş kullanıcı zaten bulunamaz ve oturum
  düşer; eklenseydi silinmiş personelin oturumu ayakta kalırdı — tam ters sonuç.
  🐛 **Testin yakaladığı gerçek hata:** parola damgası ile çerezin düzenlenme anı ham
  karşılaştırılınca **parolasını değiştiren kişi kendi oturumundan atılıyordu**. Sebep:
  `IssuedUtc` bilete RFC1123 (`"r"`) biçiminde yazılıyor ve o biçim **saniye altını
  taşımıyor** → damga 12:00:00.750, okunan an 12:00:00.000. Karşılaştırma iki tarafı da
  saniyeye yuvarlayacak şekilde düzeltildi.
- [x] 🔴 **İlk girişte parola değişimi + parola politikası + hesap kilidi.**
  `User.MustChangePassword` eklendi (migration `PanelPasswordSecurity`): parolayı **sahibi
  değil başkası** belirlediyse (seed · `CreateStaffCommand` · `ResetStaffPasswordCommand`)
  işaretlenir ve kişi kendi parolasını seçene kadar **panelin hiçbir sayfası açılmaz**.
  🔑 **Seed'de ölçüt "super_admin" DEĞİL, "hâlâ varsayılan parolayı kullanıyor"**: zaten
  kurulmuş sistemlerde de bayrak geriye dönük atılıyor (hash doğrulanarak), ama parolasını
  çoktan değiştirmiş yönetici her açılışta parola ekranına düşürülmüyor.
  🔑 **Politikanın tek sahibi `PanelPasswordPolicy`**: kural 11.18 öncesi **üç ayrı
  handler'da** elle `Length < 6` olarak kopyalanmıştı — sıkılaştıran biri birini atlarsa
  o kapıdan zayıf parola girmeye devam ederdi (`SlugHelper`/`PanelDisplay` ile aynı ders).
  Yeni kural: **en az 10 karakter + en az bir harf + en az bir rakam**, kullanıcı adı/telefonla
  aynı olamaz. Form yardım metni de aynı sabitten geliyor (elle yazılan "en az 6 karakter"
  cümlesi kuraldan bağımsız yaşıyordu).
  🔑 **Hesap kilidi** (`PanelLockoutPolicy`, 5 deneme → 15 dk): 9.2'deki hız sınırı **IP**'yi
  kısıtlıyordu, **hesabı** değil. ⚠️ Kilitliyken **doğru parola da reddedilir** — sonra
  gelseydi kilit yalnız yanlış tahminleri yavaşlatır, doğru tahmini hiç engellemezdi.
  ⚠️ Kapının **yetkilendirme filtresi** olması şart (ilk hâli `IActionFilter`'dı): aksiyon
  filtreleri izin filtresinden sonra koştuğu için izni olmayan moderatör parola ekranına
  değil "yetkiniz yok"a düşüyordu. `Order = int.MinValue` ile öne alındı.
- [x] 🟡 **Toplu işlem** (ilan · etkinlik · kampanya · vefat · duyuru). Satır seçimi +
  toplu onay/red/silme; hiçbir liste satır seçimi sunmuyordu, onay kuyruğundaki 40 ilan
  tek tek onaylanıyordu.
  🔴 **En kritik karar — aksiyon adı:** `ApproveSelected`, **`BulkApprove` DEĞİL**. Panelin
  izin eylemi aksiyon adının **önekinden** türetilir (görünmez sözleşme #19); `BulkApprove`
  hiçbir moderasyon önekiyle eşleşmez ve sessizce **`update`**'e düşerdi → yalnız *düzenleme*
  yetkisi olan moderatör **toplu onay** yapabilir hâle gelirdi. 11.15b'nin "karşılığı olmayan
  yetki" bulgusunun üçüncü biçimi; testle kilitlendi (**#29**).
  🔑 **Toplu işlem yeni iş mantığı yazmaz**: her kayıt için modülün **tek-kayıt komutu**
  çağrılır. Toplu SQL yazılsaydı denetim izi (komut başına düşer), önbellek geçersizleştirmesi
  ve **görünmez sözleşme #25'in onay penceresi** hiç çalışmazdı — panel "42 ilan onaylandı"
  der, mobil hiçbirini göstermezdi.
  ⚠️ **İç içe form tuzağı:** satırlarda zaten tek-kayıt formları var; tabloyu forma sarmak
  HTML'de geçersiz iç içe form üretir ve satır butonları sessizce ölür. Kutular HTML5
  `form="…"` özniteliğiyle dışarıdaki **boş hedef forma** bağlandı.
  ⚠️ Bir kaydın başarısızlığı partiyi durdurmaz (41 kaydı 1'i yüzünden geri çevirmek
  yöneticiyi "hangisiydi?" diye aramaya bırakır); başarısızlar sayılıp mesajda söylenir.
  📌 JS **tek dinleyicide** (`_Layout`), 11.15c'nin `data-confirm` dersinin aynısı — yeni
  bir liste toplu işlem kazanırken JS yazmaz, üç partial'ı yerleştirir.
- [x] 🟡 **Sütun sıralaması** (ilan · etkinlik · kampanya · vefat · duyuru). Sıralama yalnız
  İlanlar'da vardı, o da bir açılır listeydi. Başlığa tıklayınca artan/azalan; ok + `aria-sort`.
  🔑 Tanımlar tek dosyada (`PanelSorts`), motor ortak (`SortMap<T>`).
  🐛 **Testin yakaladığı gerçek hata:** `title_asc`'in ikincil sırası `CreatedAt`'ti ve
  **başlığı VE tarihi aynı** iki kayıtta o da eşitti → sıra kararsız. Kararsız sırada
  Postgres sayfalı listede **aynı kaydı iki sayfada gösterip bir başkasını hiç
  göstermeyebilir** (sessiz veri kaybı). Her anahtar artık `ThenBy(Id)` ile bitiyor (**#30**).
  ⚠️ **Varsayılan sıra hiçbir modülde değişmedi** — değişseydi mobil liste sessizce ters
  dönerdi (checklist §1). Her modülün varsayılanı ayrı testle kilitli.
  ⚠️ **Bilinmeyen anahtarda davranış modülün mevcut sözleşmesidir, tercih değil:** İlanlar
  10.8'den beri 400 döndürüyor, Etkinlikler ise DTO'sunda açıkça "bilinmeyen değer
  varsayılana düşer" diyor. `SortMap` bu ayrımı `rejectUnknown` bayrağıyla koruyor;
  tekleştirmek iki modülden birinin yayındaki istemcilerini kırardı.
- 🐛 **Test altyapısı bulgusu:** `AppDbContext.SaveChanges`, `State == Added` olan her
  varlığın `CreatedAt`'ini `UtcNow` ile **ezer** — kurucuda verilen tarih hiç yaşamaz.
  Sıralama testi bu yüzden rastgele sonuç veriyordu; tarihler ikinci bir geçişte yazılıyor.
- **Testler: 464 → 534 (+70).** `PanelPasswordSecurityTests` (27), `PanelBulkActionTests` (27),
  `PanelSortingTests` (16).
- **"Kuralı bilerek boz" ölçütü uygulandı:** oturum tazeleme devre dışı · politika 6 karaktere
  düşsün · `ApproveSelected` → `BulkApprove` · duyuru varsayılan sırası ters çevrilsin ·
  `title_asc`'in `ThenBy(Id)` ayracı silinsin → **15 test kırmızıya döndü (3 sınıfın hepsinde)**,
  geri alınınca 534/534 yeşil.
- **Doğrulama:** `dotnet test` **534/534** · `flutter analyze` **0** · `flutter test` **669/669**.
- **Canlı (Chrome + panel + API + Postgres + Android emülatörü):** `admin/Admin123!` ile giriş →
  **302 `/Account/ChangePassword`**; `/AdsAdmin/Index` ve `/Dashboard/Index` de aynı yere düştü,
  parola ekranının kendisi 200 açıldı (sonsuz döngü yok) · zayıf parola (`abc123`) **reddedildi**,
  güçlü parola kabul edildi ve **kendi oturumu düşmedi** · İlanlar listesinde çubuk
  "Toplu işlem için satır seçin" dedi, iki satır seçilince **"2 kayıt seçildi"** ve butonlar
  aktifleşti · **"2 ilan onaylandı."** → rozet "Süresi Doldu" → "Onaylandı" ·
  🔑 toplu onay **taze pencere verdi** (`expires_at` 2026-08-02 → **2026-09-03**, dokunulmayanlar
  eski tarihte kaldı) → `GET /v1/ads` **0'dan 2'ye** çıktı → **Android emülatöründe
  "Toplam 2 ilan"** göründü (panel → API → telefon halkası toplu yolda da kapandı) ·
  denetim izinde **komut başına ayrı satır** (2 `approve`). Geçici veri temizlendi
  (iki ilan `expired`'a ve eski `expires_at`'ine geri alındı).
- ⚠️ **Yerel geliştirme notu:** canlı doğrulama sırasında panel admin parolası varsayılandan
  **değiştirildi** (özelliğin kendisi bunu zorunlu kılıyor). Varsayılan parolaya dönülürse
  `DbSeeder` bayrağı tekrar atar ve ilk girişte yine değişim ister — bu tasarımın kendisi.
  🔴 **DERS (bu fazın kendi hatası):** yeni parola ilk yazımda bu dosyaya **düz metin olarak
  yazıldı ve herkese açık depoya push edildi.** Progress/Active_Context oturum hafızası
  olduğu için "yöneticiye söylemek" refleksi doğru, ama **bu dosyalar repoda yaşıyor** —
  gerçek parola asla buraya girmez, yalnız "değiştirildi" bilgisi girer. Sızan parola
  geçmişte kaldığı için **yanmış sayılır ve yeniden kullanılmaz.**
- ⏭️ **11.18'in kalan iki maddesi devam ediyor:** CSV dışa aktarma · global arama ·
  bağımsız push ekranı (şema değişikliği gerektiriyor: "kaç cihaza gitti" için FCM
  yanıtının saklanması).

### 11.16b — CSV dışa aktarma + global arama (11.18'den kalanlar) — [x] 2/3 (4 Ağustos 2026, 3. oturum)

> **Neden ayrı başlık:** 11.18 dört maddesini bitirip üç madde bırakmıştı. Kullanıcı
> yayın hazırlığından sonra bu ikisini istedi; üçüncüsü (bağımsız push ekranı) **şema
> değişikliği** gerektirdiği için hâlâ açık.

- [x] 🟡 **CSV dışa aktarma** (ilan · duyuru · etkinlik · kampanya · vefat).
  Ortak çekirdek `PanelCsv`; her liste kendi sütunlarını söyler, biçimlendirme tek yerde.
  🔑 **Dört ayrıntının hepsi "sessiz hasar" sınıfı** — dosya indirilir, açılır ve *yanlış*
  görünür: **UTF-8 BOM** (yoksa Excel Türkçe Windows'ta "İstanbul" → "Ä°stanbul" yapar) ·
  **noktalı virgül ayraç** (Türkçe yerelde ondalık ayracı virgül olduğu için Excel `;`
  bekler; virgülle yazılan dosya **tek sütuna** düşer) · **formül enjeksiyonu kaçışı**
  (`=`/`+`/`-`/`@` ile başlayan hücreyi Excel **çalıştırır**; ilan başlıklarını *vatandaş*
  yazdığı için bu gerçek bir saldırı yüzeyi — `=HYPERLINK(...)` başlıklı bir ilan
  yöneticinin Excel'inde canlı bağlantıya dönüşür) · **tavan aşımında ret**
  (kırpma değil: yarım bir dosyayı tam sanmak, dosyayı hiç alamamaktan kötü).
  🐛 **Yazarken düşülen ve testle yakalanan tuzak:** dışa aktarma ilk hâlinde tek istekle
  `Limit = 5000` gönderiyordu. Ama `Pagination.Clamp` (10.7, DoS koruması) panel
  sorgularını **`AdminMaxLimit = 200`'e kırpıyor ve bunu sessizce yapıyor**: istek 200
  satır döner, `TotalCount` yine 4.000 der, dosya inilir ve yönetici 200 satırı "tüm
  liste" sanır. Çözüm clamp'i gevşetmek **değil** — `PanelCsv.CollectAsync` sayfa boyunu
  clamp'in izin verdiği azami değere sabitleyip sayfaları dolaşıyor.
  🔑 **Buton controller adını model olarak ALMIYOR**, `RouteData`'dan okuyor: model
  geçseydi kopyala-yapıştırla yanlış ad kalabilir ve buton sessizce **başka bir modülün**
  listesini indirirdi. Mevcut sorgu dizesi aynen taşınıyor (yalnız `page` düşüyor) →
  dosya **ekranda görünen filtrenin** aynısı.
  ⚠️ Aksiyon adı `ExportCsv`: hiçbir yazma önekiyle eşleşmediği için GET olarak
  **`read`** iznine düşer (görünmez sözleşme #19) — dışa aktarma toplu bir okumadır.
- [x] 🟡 **Global arama.** Üst çubukta her sayfada duran kutu → `GlobalSearch/Index`.
  Dokuz modülde arar (ilan · duyuru · etkinlik · kampanya · vefat · mekan · rehber ·
  işletme · kullanıcı), sonuçlar modül modül gruplanır, modül başına 5 sonuç + toplam
  sayı + "tümünü gör".
  🔑 **YENİ KALICI DESEN — sonucu süzen controller.** Global arama tek modüle ait
  olmadığı için `[PanelPermission]` **takamaz**. Yapısal testi gevşetmek yerine üçüncü
  bir desen tanımlandı: izin **ekranın kapısında değil sorgunun içinde** uygulanır
  (aranacak modüller `IPanelMenuProvider`'dan gelir — menüyü çizen sağlayıcının aynısı,
  ikinci bir izin mantığı yazılmadı) ve controller adı
  `PanelMenu.PermissionFilteredControllers`'a **bildirilir**.
  ⚠️ **Listeye ad yazmak testi susturmaya yetmiyor:** `GlobalSearchTests` süzmenin
  gerçekten çalıştığını kanıtlıyor (aynı terim hem duyuruda hem ilanda geçiyor,
  moderatöre yalnız duyuru izni veriliyor → ilan sonucu **görünmemeli**), ikinci bir
  yapısal test de listenin "muafiyet çöplüğü"ne dönmesini engelliyor.
  ⚠️ **Silinen kayıt aramada görünmez** (`IgnoreQueryFilters` yok): yeri Çöp Kutusu
  (11.17). Görünseydi hem "silmiştim ama çıkıyor" karmaşası doğardı hem de sonuçtan
  düzenleme ekranına giden bağlantı boş sayfaya götürürdü. Boş sonuç ekranı bu yüzden
  Çöp Kutusu'nu hatırlatıyor.
  ⚠️ **`EF.Functions.ILike` KULLANILAMADI:** Npgsql'e özel ve `Application` katmanı
  sağlayıcıyı tanımaz (katman kuralı). Projenin her yerindeki `ToLower().Contains()`
  deseni kullanıldı — tek yerde farklı arama semantiği, modül listesiyle global aramanın
  aynı terimde farklı sonuç vermesi demek olurdu.
  ⚠️ Menüye satır **konmadı**: giriş noktası üst çubuktaki kutu (dar ekranda da görünür).
- ⏭️ 🟡 **Bağımsız push ekranı** → hâlâ açık. "Kaç cihaza gitti" FCM yanıtının
  saklanmasını gerektiriyor: **şema değişikliği**, tek ekranlık iş değil.

#### 11.16b kapanış notları

- **Testler: 545 → 567 (+22).** `PanelCsvExportTests` (13), `GlobalSearchTests` (8),
  `PanelModeratorPermissionTests`'e 1 yapısal test.
- **`ARCHITECTURE.md` modül tablosuna satır 23 (`Search/`) eklendi** — `ArchitectureDocTests`
  bunu **gerçekten kırmızıya döndürerek** hatırlattı (doküman bilerek çürüyemiyor).
  §3'e üçüncü panel deseni ("sonucu süzen ekran") yazıldı.
- **"Kuralı bilerek boz" ölçütü uygulandı:** sayfa dolaşımı tek sayfaya indirildi ·
  BOM kaldırıldı · global aramanın izin süzgeci atlandı → **4 test kırmızıya döndü**
  (ikisi CSV, ikisi yetki), geri alınınca 567/567 yeşil.
- **Checklist §11'e beş satır** eklendi.

> **AKTİF SIRADAKİ: 11.16 — Yayına hazırlık.** (11.17'nin 4 maddesi 4 Ağustos 2026'da bitti —
> **kullanıcı isteğiyle 11.16'dan ÖNCE**: "panel ve uygulama arası API bağlantılarını sorunsuz
> hale getirelim". Kalan iki 🟡 madde **11.18**'e alındı.)
>
> 11.17'den devralınanlar: 🔑 **yalnız-admin panel ekranı deseni** (`[Authorize(Roles=
> "admin,super_admin")]` + `[PanelPermission]` **YOK** + menü `Module` **null** +
> `AdminOnlyControllers`; modül anahtarı verilirse izin matrisinde *karşılığı olmayan yetki*
> belirir) · **panelde bir zaman/durum ayrımı istemcide de yapılıyorsa iki tanım testle
> eşitlenmeli** (`PowerOutagePhaseRules` ↔ mobil `PowerOutage.isActive/…`, görünmez sözleşme #27) ·
> **`IgnoreQueryFilters()` gerektiren yeni sorguda "önce boş görünür" tuzağı** (unutulursa ekran
> her zaman boş, hata yok) · **geri getirme `status`'e dokunmaz** (#28) · **yeni
> `IAuditableCommand` → `PanelDisplay.AuditAction` sözlüğüne satır** (kaynak taraması kırar) ·
> ⚠️ çöp kutusu boşken sayfada hiç form (dolayısıyla hiç antiforgery token) olmaz — panel
> testinde token'ı önceden al · ⚠️ API ve paneli **aynı anda** `dotnet run` etme (Infrastructure
> ref-assembly dosya kilidi).
>
> ~~AKTİF SIRADAKİ: 11.16~~ (11.17 araya girdi) — (11.15c A grubu 3 Ağustos 4. oturumda bitti;
> B grubu **11.17**'ye, C grubunun kalanı **11.16**'ya bağlandı.)
> 11.16'ya devralınanlar: **yeni panel görünümünde durum/rol asla ham basılmaz** →
> `PanelDisplay.Status()`/`.Role()` + `_StatusBadge` partial'ı; **para `PanelDisplay.TL()`'den
> geçer** (panel `InvariantCulture`'a sabit, `ToString("C2")` `¤` basar); **silme onayı
> `data-confirm` özniteliğiyle** yazılır ve kaydın adını içerir; **panel menüsü iki yerde
> çizilir ama tek listeden gelir** (`_MenuLinks` partial'ı — kenar çubuğu + dar ekran menüsü);
> **panelin "aktif" sayaçları public sorguların görünürlük tanımıyla aynı olmak zorunda**
> (görünmez sözleşme #23); ⚠️ **`mobile/tool/ios_sim.sh` koordinat eşlemesi iPhone 17'de
> hâlâ bozuk** · ⚠️ iOS push APNs `.p8` bekliyor.
>
> ~~AKTİF SIRADAKİ: 11.15c~~ (A grubu tamamlandı) — 11.15c, 3 Ağustos 2026 (3. oturum)
> **canlı Chrome + `super_admin`** gezisiyle açıldı: 18 panel sayfası + form/alt ekranlar
> gezildi, gerçek yazma denemeleri yapıldı. En kritik üçü: **dar ekranda hamburger ölü →
> panelde hiç menü yok** (✅ düzeltildi) · **silinen duyurunun bildirimleri kalıyor → mobilde
> boş sayfaya götüren bildirim** (✅ iki katmanda düzeltildi) · **şehirlerarası ulaşım
> panelden hiç yönetilemiyor** (⏭️ 11.17 — tek gerçek işlevsel boşluk).
>
> ~~AKTİF SIRADAKİ: 11.16~~ (11.15c araya girdi) — **11.16 — Yayına hazırlık.** 11.15b'den devralınanlar: **panel testi yazarken `[Collection("panel")]`** (kendi fixture'ını açan sınıf süiti dakikalarca uzatır) · **yeni panel controller'ı = `[Authorize(...,moderator)]` + `[PanelPermission("<modül>")]` + `PanelMenu.Items` satırı** (rol listesine moderator yazıp özniteliği unutmak moderatöre **sınırsız** yetki verir) · **yeni cache'lenen sorgu = `CacheGroups` sabiti + en az bir invalidator** · **tarih gösteren yeni karta `now` enjekte edilebilmeli** (yoksa golden her gün kırılır) · ⚠️ **`mobile/tool/ios_sim.sh` koordinat eşlemesi iPhone 17'de hâlâ bozuk** · ⚠️ iOS push APNs `.p8` bekliyor.
>
> ~~AKTİF SIRADAKİ: 11.15b~~ (tamamlandı) — **11.15b — Emniyet ağı 2. tur (panel + önbellek + moderasyon).** 11.15'ten devralınanlar: **`PagedListFooter`** (yeni sayfalı liste bunu kullanmalı, kendi altbilgisini yazmamalı) · **`ScrollableStateBody`** (boş/hata görünümleri kendiliğinden kaydırılabilir — çağrı yerinde sarmalamayın, iç içe kaydırma olur) · **`connectivityStatusProvider`** (şerit `AppScaffold`ta otomatik; `offline:` yalnız test/stil kılavuzu için) · **`test/golden/` altyapısı** (yeni ortak bileşen/liste kartı eklerken oraya bir senaryo düşün, **uzun Türkçe metinle**) · **`accessibility_test.dart`** (48 dp + kontrast + 1.4 ölçekte taşma) · ⚠️ golden'lar `@Tags(['golden'])`, CI'da **macOS işinde** koşuyor ve **asla üretmiyor** · ⚠️ **`mobile/tool/ios_sim.sh` koordinat eşlemesi iPhone 17'de bozuk** (11.16'da düzeltilmeli).
>
> ~~AKTİF SIRADAKİ: 11.15~~ (tamamlandı) — **11.15 — Cilalama, durumlar ve erişilebilirlik.** _(11.13 ve 11.14 tamamlandı — 11.14 kullanıcı isteğiyle 11.13'ten önce yapıldı.)_ 11.13'ten devralınanlar: **`PushMessaging` arayüzü** (sahte sağlayıcıyla test edilebilir; yeni bir push senaryosu eklenirken Firebase'e değil buna bakılır) · **`PushCoordinator.openNotification`** (liste + push tek yol) · **`notificationRouteFor`** (yeni `relatedType` sunucuda üretilirse buraya eklenmeli, yoksa sessizce yok sayılır) · **uygulama içi ön plan şeridi** (`scaffoldMessengerKey` app.dart'ta) · ⚠️ **iOS push APNs `.p8` bekliyor → 11.16** · ⚠️ `analysis_options.yaml`'da `exclude: build/**` (iOS SPM kopyaları) · ⚠️ CI Android derlemesi `google-services.json` yer tutucusu yazıyor. **ESKİ NOT:**  _(11.14 Devir Teslim **plan sırası dışında, kullanıcı isteğiyle 11.13'ten ÖNCE tamamlandı** — 2 Ağustos 2026, 3. oturum.)_ 11.14'ten devralınanlar: **`ARCHITECTURE.md` §4 reçetesi** (bildirim modülü zaten var, ama deep-link eşlemesi eklenirken §7 görünmez sözleşme listesine satır düşülmeli) · **`InvisibleContractsTests`** (push `data` alanlarının sözleşmesi oraya yazılabilir) · **`EndpointAuthorizationSweepTests`** yeni FCM ucunu kendiliğinden kapsar; `POST /v1/notifications/fcm-token` **anonim yazma listesinde DEĞİL** — orada kalmalı · ⚠️ **`ArchitectureDocTests` yüzünden** yeni bir `Features/` klasörü ya da mobil `features/` klasörü açarsanız `ARCHITECTURE.md` tablosuna satır eklemeden `dotnet test` yeşile dönmez · 📌 **11.13'ün kendi dersi zaten yazılı:** bayrakla kapatılmış kod yolu hiç test edilmemiş demektir (`FcmPushService`). 11.12'den devralınanlar: **`PagedFeedController`** · **detay alt-rota / kardeş-rota deseni** (deep-link hedefleri hazır: `/duyurular/:id`, `/kesintiler/:id`, `/ilanlar/:id`, `/etkinlikler/:id`, `/kampanyalar/:id`, `/vefat/:id`, `/taksi/:id`, `/mekanlar/:id`) · **`AppRoutes.complaintForContent` deseni** (sorgu parametreli rota üretimi — push `data.relatedType/relatedId` eşlemesi aynı yaklaşımı kullanabilir) · **`kAppModules` artık TAMAMEN `ready`** (12/12) → deep-link eşlemesi tek listeden türetilebilir · ⚠️ Firebase yapılandırması **kullanıcıda hazır** (`mobile/android/app/google-services.json` + `mobile/ios/Runner/GoogleService-Info.plist` var, bilinçli olarak commit EDİLMEDİ) · ⚠️ backend `Fcm:Provider=Firebase` + `ServiceAccountKeyPath` bekliyor (10.11 hazır) · ⚠️ `deviceFcmTokenProvider` 11.3'ten beri **null stub**, burada gerçeklenecek · ⚠️ `AppScaffold` pull-to-refresh düzeltmesi (11.12) bildirim listesini de ilgilendirir · testlerde `homeStubs()` + `settleApp()` + `tester.view.physicalSize` + kalıcı hata (404).
>
> ~~AKTİF SIRADAKİ: 11.12~~ (tamamlandı) — **11.12 — Ulaşım + Şikayet/İstek.** 11.11'den devralınanlar: **`LookupDropdown<T>`** (şikayet tipi/konu seçimi doğrudan kullanabilir) · **`PagedFeedController`** · **`ContactActions`** · **`FilterChoiceChip`** · **`ensureSignedIn`** ("benim şikayetlerim" `[A]`) · detay **alt-rota** deseni · **form deseni** (`_PickerField` + zorunlu alan denetimi istemcide + hata varsa başa kaydırma + başarı diyaloğu + `addPostFrameCallback` ile kapatma) · ⚠️ ulaşım uçları **sayfasız düz liste** döndürüyor olabilir → `PagedFeedController` yerine `FutureProvider` gerekebilir, önce uca bak · ⚠️ `AppDate.clockLabel` ulaşım `"HH:mm"` saatleri için hazır (11.2'de yazıldı, henüz hiç kullanılmadı) · ⚠️ dar sütunda `Row` içindeki `Text` **`Flexible`+`ellipsis`** (beş fazda üst üste çıkan taşma; 11.11'de ortak bileşenlerde de çıktı) · testlerde `homeStubs()` + `settleApp()` + `tester.view.physicalSize` + uzun formda `dragUntilVisible` + kalıcı hata (404) kullan.
>
> ~~AKTİF SIRADAKİ: 11.11~~ (tamamlandı) — **11.11 — Vefat + Taksi + Mekanlar.** 11.10'dan devralınanlar: **`ContactActions`** (taksi durağı + mekan iletişimi doğrudan kullanacak; koordinat yoksa adres metniyle harita araması) · **`FilterChoiceChip`** (ortak filtre chip'i — mekan/taksi filtreleri buna bağlanır) · **`MonthCalendar`** · **`PagedFeedController`** (artık `ref.mounted` korumalı) · **`AdDraftStore` deseni** (vefat bildirimi uzun form) · **`AdSubmissionService`** (vefat fotoğrafı yüklemesi) · **`ensureSignedIn`** (vefat bildir + taksi çağır `[A]`) · detay **alt-rota** deseni (`/vefat/:id` → 11.13 deep-link) · ⚠️ vefat ekranı **saygılı ve sade** olmalı (rozet/renk kalabalığı yok) · ⚠️ `POST /v1/taxis/drivers/{id}/call` **telefonu döndürür**, çeviriciyi istemci açar (11.8 `AppLinks.call` deseni) · ⚠️ dar sütunda `Row` içindeki `Text` **`Flexible`+`ellipsis`** (dört fazda üst üste çıkan taşma) · testlerde `homeStubs()` + `settleApp()` + uzun ekranlarda `tester.view.physicalSize` + kart taşma testi.
>
> ~~AKTİF SIRADAKİ: 11.10~~ (tamamlandı) — **11.10 — Etkinlikler + Kampanyalar.** 11.9'dan devralınanlar: **`PagedFeedController`** (etkinlik/kampanya listeleri doğrudan kullanacak) · **`AdPropertyField` deseni** (sunucudan gelen tanımdan form üretme — başka modülde dinamik alan çıkarsa aynı yaklaşım) · **`AdDraftStore`** (uzun formlarda taslak deseni; **11.11 vefat bildirimi** aynı ihtiyacı taşıyor) · **`AdSubmissionService`** (görsel yükleme + ilerleme; 11.11 vefat fotoğrafı) · **`AdStatus` deseni** (statü → Türkçe etiket/renk/ikon + **metinli** rozet) · **`MonthSwitcher`/`DutyCalendar`** (11.7'den — etkinlik takvimi bunun üstüne kurulacak) · **`ContactActions`** · ⚠️ **iç içe `GoRoute` üst ekranı da kurar** — form/detay rotalarını kardeş yapmayı düşün · ⚠️ dar sütunlarda `Row` içindeki `Text` **`Flexible`+`ellipsis`** olmalı (üç fazda üst üste çıkan taşma hatası) · testlerde `homeStubs()` + `settleApp()` + `profileBody()` + uzun ekranlarda `tester.view.physicalSize`.
>
> ~~AKTİF SIRADAKİ: 11.9~~ (tamamlandı) — **11.9 — İlanlar Bölüm 2.** 11.8'den devralınanlar: **`AdsRepository`** (liste/kategori/detay/favori/sayaç hazır; `POST/PUT/DELETE /v1/ads` + `extend` eklenecek) · **`AdCategory` + `adRootCategoriesProvider`/`adSubCategoriesProvider`** (ilan verme formunun kategori seçimi hazır; `GET /v1/ads/categories/{id}/properties` yeni) · **`FavoriteAd` modeli TAM** (11.8 yalnız id kümesini kullanıyordu — "Favorilerim" ekranı doğrudan bunu listeleyecek, `isAvailable=false` soluk gösterilecek) · **`AppMoney`** (fiyat girişi `AppMoney.parse` ile okunur) · **`features/files/` yükleme repository'si** (11.5'ten; çoklu görsel `upload(filePath:, moduleType:'ad')` ×N → id'ler) · **`AdGallery`** (kapak seçimi önizlemesi) · **`ensureSignedIn`** ("İlan ver" korumalı) · ⚠️ ilan verme ekranı **sekme dışı korumalı rota** olacak → `AppRoutes.protectedPrefixes`'e yazılmalı · ⚠️ **`context.push` ile açılan ekran router redirect'inin üstünde kalır** (11.5 dersi: durum değişiminden sonra `addPostFrameCallback` içinde kapat) · testlerde `homeStubs()` + `settleApp()` + `profileBody()`; ⚠️ yatay şeritlerde `ListView` yerine `SingleChildScrollView`, debounce testinde süre elle pump.

> ~~AKTİF SIRADAKİ: 11.6~~ (tamamlandı) — **11.6 — Duyurular + Elektrik Kesintileri.** 11.5'ten devralınanlar: **`features/files/` yükleme repository'si hazır** (11.9 ilan görselleri aynı çağrıyı kullanacak: `upload(filePath:, moduleType:)` → `UploadedFile.id`) · **`UserAvatar`/`UserIdentityRow`** · **`AppNetworkImage`** (duyuru görselleri) · **`AnnouncementTile`** (11.4'te yazıldı, liste ekranında yeniden kullanılacak) · `apiRetry` her yeni provider'a verilmeli · ⚠️ **`context.push` ile açılan ekran router redirect'inin üstünde kalır** — durum değişiminden sonra ekranı `addPostFrameCallback` içinde `context.pop()`/`context.go` ile kapat (11.5'te üç ayrı hata bu yüzden çıktı) · testlerde `homeStubs()` + `settleApp()` + `profileBody()` fixture'ı.
>
> ~~AKTİF SIRADAKİ: 11.5~~ (tamamlandı) — 11.4'ten devralınanlar: **iskeletler yerinde** — `features/settings/presentation/settings_screen.dart` (Hesap · Görünüm · Hakkında · Geliştirici bölümleri var; içine profil düzenleme, **6 bildirim anahtarı** `PATCH /v1/users/me/notifications` ve **hesabı sil** `DELETE /v1/users/me` girecek) ve `features/profile/presentation/profile_screen.dart` (avatar+ad+telefon+mahalle; "İlanlarım"/"Favorilerim" satırları bugün "Yakında" — 11.9'da bağlanacak). Profil fotoğrafı için **`AppNetworkImage`** hazır (`image_picker` → `POST /v1/files/upload` → dönen id → PATCH). ⚠️ `CurrentUser` modeli **kısmi** — `MyProfileDto`'nun bildirim tercihleri + `usernameChangedAt`/`neighborhoodChangedAt` alanları 11.5'te eklenecek (30 günlük kısıt mesajları `USERNAME_CHANGE_LIMIT`/`NEIGHBORHOOD_CHANGE_LIMIT`). Ağ hatalarında `apiRetry` politikası (`core/network/retry_policy.dart`) yeni provider'lara da verilmeli. Testlerde `homeStubs()` + `settleApp()`; korumalı aksiyonlar `ensureSignedIn` ile, sekme içi davetler `SignInPrompt` ile. Modül eklemek/rota vermek gerekirse **tek yer**: `core/navigation/app_modules.dart`.

> **Sıra:** 11.1 → 11.2 → … → 11.14 → **11.15 → 11.15b** → 11.16 (sıralı; her biri öncekine dayanır). Büyük modül olan İlanlar bilerek 2'ye bölündü (11.8-11.9). FCM (11.13) Firebase config gerektirir — o oturuma girmeden kullanıcıdan istenir. İlk 5 alt-faz temel/iskelet, **11.6-11.13 modül dikey kesitleri**, **11.14 devir teslim** (mimari haritası + backend emniyet ağı + CI — 2 Ağustos 2026'da araya eklendi, bkz. o başlıktaki gerekçe), **11.15 cila (+golden test) → 11.15b emniyet ağı 2. tur → 11.16 yayın**.

---

# 🔭 FAZ 12 — GÖZLEM, ALAN MODELİ VE GİRİŞ KOLAYLIĞI (5 Ağustos 2026'da planlandı)

> **Bu faz neden var?** Faz 11 bittiğinde panel "her modülü yönetebiliyor" durumuna geldi
> (11.17 son işlevsel boşluğu kapattı). Ama üç ayrı eksen açık kaldı ve üçü de kullanıcının
> canlı kullanımda göreceği sınıftan:
>
> 1. **Panel ne yapıldığını gösteriyor, ne olduğunu göstermiyor.** `AuditLog` yöneticinin
>    *başarılı* yazma eylemlerini tutuyor; vatandaşın aldığı hata, mobilde oluşan çökme ve
>    *başarısız* giriş denemeleri **hiçbir yerde kayıtlı değil**. Hatalar yalnız Seq'e akıyor
>    (`localhost:5341`) — yani panelden bakan yöneticinin erişemediği bir yere.
> 2. **Üç modülün alan modeli yarım kalmış** ve yarıda kalan parçalar kodda **boş çengel**
>    olarak duruyor: `PowerOutage.AnnouncementId` (hiç doldurulmuyor), `Event.City` (panelde
>    formu bile yok), `Event.IsLocal` (mobil parse ediyor, hiçbir widget kullanmıyor),
>    `IntercityRoute` (araç tipi / kalkış noktası / sefer günü yok).
> 3. **Girişte sürtünme var** — tek yol telefon + OTP.
>
> **Bu fazın ortak özelliği: hepsi ADDITIVE.** Hiçbir alt-fazda DTO alanı silinmiyor veya
> yeniden adlandırılmıyor (`ARCHITECTURE.md` §5 kırıcı-değişiklik kuralı hiç devreye girmiyor),
> hiçbir tablo düşürülmüyor (§6). Mağazadaki eski sürümler her adımdan sonra çalışmaya devam eder.

## 📌 Faza girmeden bilinmesi gerekenler

- **Sıra bilinçli:** gözlem katmanı (12.1–12.2) **önce** kuruluyor. Gerekçe: 12.3–12.8 arasında
  altı migration + üç modülün şema değişikliği var; bunların ürettiği hatayı görebileceğimiz
  bir ekranın **önceden** var olması gerekiyor. Kendi değişikliğimizi kendi kurduğumuz
  aynayla izleyeceğiz.
- **`ARCHITECTURE.md` ve `CODE_REVIEW_CHECKLIST.md` bu planla birlikte GÜNCELLENMEDİ** —
  bilerek. İkisi de testle gerçeğe çivili (`ArchitectureDocTests`, `CodeReviewChecklistDocTests`);
  henüz var olmayan bir modülü tabloya, henüz var olmayan bir test sınıfını checklist'e yazmak
  `dotnet test`'i **bugün** kırardı. Her alt-faz kendi satırını **kendi oturumunda** ekler
  (18 adımlı reçetenin son adımı).
- **Her alt-faz bir oturumda bitecek boyuta bölündü.** Bitti kriteri her yerde aynı:
  `dotnet test` + `flutter analyze` + `flutter test` yeşil · **kuralı bilerek boz → testin
  kırmızıya döndüğü görüldü** · Memory Bank güncel · commit atıldı.
- **Yeni görünmez sözleşmeler doğacak.** Her alt-fazın altında hangi maddelerin `ARCHITECTURE.md`
  §7 tablosuna ekleneceği yazılı. Numaralar **30'dan devam eder**, alt-faz tamamlanınca kesinleşir.

## ⚙️ Faz başında alınan kararlar (kullanıcı onayladı, 5 Ağustos 2026)

| Karar | Seçim | Gerekçe |
|---|---|---|
| **Sosyal girişte telefon** | **Zorunlu kalır** | `User.Phone` required+unique ve **42 dosyanın** kimlik çıpası (JWT claim'i, OTP, ban, hesap silme, ilan iletişimi). Telefonsuz hesap ayrıca *doğrulanmamış* kullanıcıya ilan verme/taksi çağırma açardı → moderasyon riski. Sosyal giriş **kayıt formunu ön dolduran** bir kısayol olur; ikinci girişten sonra zaten tek buton. |
| **Kesinti bildirimi** | **Otomatik duyuru üretilir** | `PowerOutage.AnnouncementId` alanı tam bunun için konmuş ve boş bırakılmış. Var olan `AnnouncementNotificationGenerator` + `SendPushNotificationsJob` + deep-link **aynen** çalışır → **mobilde sıfır değişiklik**, mağazadaki eski sürümler de bildirimi alır. Ayrı `relatedType` seçilseydi görünmez sözleşme #18 gereği eski sürümler bildirime dokunduğunda **sessizce hiçbir yere gitmezdi**. |
| **Sıra** | Gözlem → alan modeli → sosyal giriş | Yukarıda. |

## 🗺️ Alt-faz haritası

| # | Alt-faz | Katman | Şema | Tahmini test artışı |
|---|---|---|---|---|
| 12.1 | Hata günlüğü modülü ✅ | backend + panel + mobil (raporlayıcı) | ✔ | **+38 backend, +7 mobil** |
| 12.2 | Şüpheli giriş günlüğü + e-posta raporlama (+ `ForwardedHeaders` + `StaffAdmin` izin tutarsızlığı) | backend + panel | ✔ | ~35 backend |
| 12.2b | Bildirim teslim panosu + bağımsız push ekranı ✅ | backend + panel | ✔ | **+26 backend** |
| 12.3 | Kesinti mahalle referansı + mahalle bazlı bildirim ✅ | backend + panel + mobil | ✔ | **+40 backend, +11 mobil** |
| 12.4 | Etkinlik konumu (il/ilçe) ✅ | backend + panel + mobil | ✔ | **+55 backend, +7 mobil** |
| 12.5 | Ulaşım alan modeli (araç tipi · kalkış noktası · sefer günleri) ✅ | backend + panel | ✔ | **+59 backend** |
| 12.6 | Ulaşım mobil (ikili kalkış · gün rozetleri · "sıradaki sefer") ✅ | mobil | — | **+48 mobil** |
| 12.7 | Sosyal giriş — backend | backend + panel | ✔ | ~30 backend |
| 12.8 | Sosyal giriş — mobil | mobil | — | ~20 mobil |
| 12.9 | Panelin dış bağımlılıklarını yerelleştirme (CDN → self-host + nonce'lu CSP) ✅ | panel + yayın kapısı | — | **+20 backend** |
| 12.10 | Moderasyon geçişinin tek sahibi (Düzenle formunun açtığı ikinci yol) ✅ | backend + panel | — | **+46 backend** |
| 12.11 | Tek sahipliğin derleyiciye devri (`init` + varlıkta geçişler) ✅ **plan dışı — dış analizden doğdu** | backend | — | **+4 backend** |
| 12.12 | **Haberler — alım çekirdeği** (WP istemcisi · senkron/mutabakat işleri · sanitizasyon · görsel aynalama) **plan dışı — kullanıcı isteği** | backend | ✔ | **BİTTİ (11 Ağu) — +78 backend testi** |
| 12.13 | **Haberler — panel** (liste/ayrıntı/override · kategori görünürlüğü · senkron panosu) | panel | ✔ | ~40 backend |
| 12.14 | **Haberler — mobil** (liste · detay · kategori süzgeci · `flutter_html`) ✅ | mobil | — | **BİTTİ (12 Ağu) — +59 mobil test** |
| 12.15 | **Haberler — bildirim** (elle gönderim · `relatedType="news"` · deep-link) | backend + panel + mobil | ✔ | ~20 backend, ~8 mobil |

---

### 12.1 — Hata günlüğü modülü — [x] ✅ TAMAMLANDI (5 Ağustos 2026)

**Hedef:** Vatandaşın ve yöneticinin aldığı hataların panelden görülebilmesi. Bugün 5xx
yalnız Seq'e akıyor, mobilde oluşan hata **hiçbir yere** akmıyor.

#### Backend

- **Varlık `ErrorLog : BaseEntity`** (`error_logs`, snake_case):
  `Source` (`api`|`web`|`mobile`) · `Level` (`error`|`fatal`) · `Code` · `Message` ·
  `StackTrace text?` · `Path` · `Method` · `StatusCode int?` · **`TraceId`** ·
  `UserId Guid?` · `IpAddress inet?` · `UserAgent` · `AppVersion` · `Platform` · `OsVersion` ·
  **`Fingerprint`** · `OccurrenceCount int` · `FirstSeenAt` · `LastSeenAt` ·
  `IsResolved` · `ResolvedBy Guid?` · `ResolvedAt` · `ResolvedNote`.
  İndeks: `(Fingerprint)` unique, `(LastSeenAt desc)`, `(Source, Level)`, `(IsResolved)`.
- **Tekilleştirme (`Fingerprint`) zorunlu.** Fingerprint = `SHA256(Source + Code + normalize(Message) + ilk yığın karesi)`.
  Aynı fingerprint tekrar gelirse **yeni satır yazılmaz**, `OccurrenceCount++` ve `LastSeenAt`
  güncellenir. 🔴 Bu opsiyonel bir iyileştirme değil: tek bir 500 döngüsü tekilleştirme
  olmadan tabloyu **dakikada on binlerce satırla** doldurur ve `error_logs` projenin en büyük
  tablosu hâline gelir. `normalize()` GUID/sayı/tarihleri maskeler (yoksa her istek ayrı
  parmak izi üretir ve tekilleştirme hiç çalışmaz).
- **Yazma yolu isteği DÜŞÜREMEZ.** `IErrorLogSink` → `Channel<ErrorLogEntry>` (sınırlı kapasite,
  dolunca **en eskiyi düşürür**) + `ErrorLogWriterService : BackgroundService` toplu yazar.
  🔴 Neden kanal: `ExceptionMiddleware`'in `catch` bloğunda **senkron DB yazmak** en tehlikeli
  tasarım — veritabanı çöktüğünde hata yazma denemesi de patlar, `catch` içinde istisna oluşur
  ve istemci **zarfsız** ham 500 alır (görünmez sözleşme #10 kırılır). Kanal ayrıca yanıt
  süresini uzatmaz.
- 🔴 **Kendi kendini besleyen döngü yasağı:** yazıcının kendi hatası **asla** `ErrorLog`'a
  yazılmaz, yalnız `ILogger`'a düşer. Aksi hâlde DB hatası → hata kaydı denemesi → DB hatası…
- 🔴 **PII maskeleme:** `Path`/`QueryString` saklanmadan önce `phone`, `otp`, `token`,
  `password`, `email` anahtarları `***` ile maskelenir (`CODE_REVIEW_CHECKLIST` §7). OTP akışında
  telefon query string'e düşebiliyor.
- **Mobil raporlama ucu:** `POST /v1/client-errors` — **anonim serbest** (çökme oturum
  açılmadan da olur), `public-write` rate limit politikası (§7 checklist maddesi), gövde
  boyutu tavanlı (mesaj 2 KB, yığın 16 KB — aşan **kırpılmaz, reddedilir**),
  `Source="mobile"` **sunucuda sabitlenir** (istemci `api` diyerek kendi kaydını sunucu hatası
  gibi gösteremesin). `EndpointAuthorizationSweepTests`'in anonim yazma listesine **bilinçli**
  eklenir.
- **Saklama:** `PurgeErrorLogsJob` (Hangfire, günlük) — çözülmüş kayıtlar 30 gün, çözülmemişler
  90 gün sonra silinir. Silinen satır sayısı loglanır. (Var olan job deseni: idempotent
  `ExecuteUpdateAsync`/`ExecuteDeleteAsync` + `[AutomaticRetry]` + `[DisableConcurrentExecution]`.)

#### Panel

- **`ErrorLogsAdmin` — yalnız-admin deseni** (`ARCHITECTURE.md` §3): `[Authorize(Roles="admin,super_admin")]` ·
  `[PanelPermission]` **YOK** · `PanelMenu.Items` satırının `Module`'ü **`null`** ·
  controller adı **`AdminOnlyControllers`'a**. Modül anahtarı verilirse izin matrisinde
  *karşılığı olmayan yetki* belirir (11.15b'nin en büyük bulgusu).
- Liste: fingerprint bazında gruplu; sütunlar Kaynak · Seviye · Kod · Mesaj (kısaltılmış) ·
  Adet · İlk/Son görülme · Durum. Filtre: kaynak, seviye, tarih aralığı, çözüldü/çözülmedi, arama.
- Sıralama `PanelSorts` üzerinden, **her anahtar `ThenBy(Id)` ile biter** (görünmez sözleşme #30).
- CSV dışa aktarma **`PanelCsv` ile** (kendi CSV'ni yazma — dört sessiz tuzak orada çözülü).
- Detay: tam yığın + `TraceId` (kopyalanabilir → Seq'te arama) + kullanıcı/cihaz bilgisi.
- "Çözüldü işaretle" (+ not) → `IAuditableCommand` → **`PanelDisplay.AuditAction` sözlüğüne
  Türkçe satır** (yoksa `PanelAuditLogTests` kırılır, bilerek).
- 🔴 **Mesaj ve yığın ekranda ham basılmaz** — içerik kısmen *istemciden* geliyor, HTML olarak
  render edilirse panelde depolanmış XSS olur. Razor kaçışı varsayılan ama `@Html.Raw` yasak;
  test bunu denetler.
- Dashboard'a "son 24 saatte N yeni hata" kartı (yalnız admin görür).

#### Mobil

- `core/observability/error_reporter.dart`: `FlutterError.onError` +
  `PlatformDispatcher.instance.onError` + `ApiClient` 5xx yakalayıcısı → uca gönderir.
- **Ateşle-unut**, kuyruklu ve **kendi hatasını raporlamaz** (döngü yasağı mobilde de geçerli).
- Cihaz bilgisi `package_info_plus` + `Platform` üzerinden; **kullanıcı kimliği yalnız
  oturum açıksa** eklenir (id, telefon değil).
- ⚠️ Yeni bir uç provider'ı değil, tek yönlü servis — `retry: apiRetry` **kullanılmaz**
  (hata raporu yeniden denenmez, kuyruk şişer).

#### Yeni görünmez sözleşmeler (§7 tablosuna eklenecek)

- **Hata günlüğü yazımı isteği düşüremez** — sink hata yutar, yalnız `ILogger`'a düşer;
  yazıcının kendi hatası **asla** tabloya gitmez.
- **`Fingerprint` tekilleştirmesi zorunludur** ve normalize edici GUID/sayı maskeler —
  kaldırılırsa tablo sessizce şişer, sorgular yavaşlar, kimse hata almaz.
- **`Source` sunucuda sabitlenir** — istemcinin gönderdiği değere güvenilmez.

**Bitti kriteri:** panelde bilerek üretilen bir 500 görünüyor · aynı hata 3 kez tetiklenince
**tek satır, adet 3** · mobilden fırlatılan bir istisna panelde `mobile` kaynağıyla listeleniyor ·
DB kapalıyken istek **normal hata zarfıyla** dönüyor (kayıt düşüyor, uygulama ayakta) ·
telefon içeren bir yol maskeli saklanıyor.

#### 12.1 kapanış notları

**Teslim edilenler:** `ErrorLog` varlığı + migration `AddErrorLogs` · `ErrorFingerprint` +
`SensitiveDataMasker` (saf, birim testli) · `ChannelErrorLogSink` (kuyruk + `BackgroundService`) ·
`PurgeErrorLogsJob` · `POST /v1/client-errors` · `ExceptionMiddleware` ve `PanelErrorLoggingMiddleware`
bağlantıları · `ErrorLogsAdmin` (liste + ayrıntı + çözme + CSV) · dashboard rozeti ·
mobil `ErrorReporter`. **Backend 567 → 605 (+38), mobil 678 → 685 (+7), analyze 0.**

🐛 **Uygulama sırasında bulunan/öğrenilen üç şey:**
1. **Ara katman sırası sezgiye ters.** `PanelErrorLoggingMiddleware` ilk yazımda
   `UseExceptionHandler`'dan **önce** kaydedilmişti. Önce kaydedilen ara katman **dışta**
   kalır, istisnayı ilk gören ise **içtekidir** → hata sayfası çalışır, kayıt **sessizce hiç
   oluşmazdı**. Doğrusu: `UseExceptionHandler`'dan **sonra** kaydetmek.
2. **Testin kendi seed'i tekilleştirmeye takıldı.** `PanelErrorLogTests` başta parmak izini
   `ErrorFingerprint.Compute` ile hesaplıyordu; "eşit kayıt 0/1/2/3" mesajları **sayılar
   maskelendiği için aynı parmak izine** düştü ve benzersiz indeks patladı. Hata testteydi,
   normalize edicide değil — tam tasarlandığı gibi davranıyordu. Seed artık rastgele parmak izi verir.
3. **BOM bayt düzeyinde denetlenmeli.** `ReadAsStringAsync` BOM'u ön ek sayıp yutuyor ve
   "BOM yok" gibi görünüyor (`PanelCsvExportTests`'teki aynı karar).

📌 **`StaffAdmin` ile dokümantasyon arasında bir tutarsızlık bulundu (12.1 kapsamı dışı, düzeltilmedi):**
`ARCHITECTURE.md` §3 yalnız-admin ekranların `Module`'ünün **null** olmasını şart koşuyor, ama
`StaffAdmin` hem `AdminOnlyControllers`'ta hem de `Module = "staff"` taşıyor. `StaffAdminController.Modules`
menüden türediği için **"staff" izin matrisinde görünüyor**: bir yönetici moderatöre "staff okuma"
yetkisi verebilir, rol kapısı yüzünden o yetki **asla çalışmaz** — yani 11.15b'nin kapattığı
"karşılığı olmayan yetki" hatasının hâlâ duran bir örneği. Düzeltmesi izin matrisini ve
seed'lenmiş izinleri etkiler; ayrı ele alınmalı.

**Doğrulama:** `dotnet test` **605/605** · `flutter analyze` **0** · `flutter test` **685/685**.
**Kuralı bilerek boz:** normalize edici GUID/sayı maskelemeyi bıraktı · maskeleyiciden
`phone`/`token` çıkarıldı · menü satırına modül anahtarı verildi · görünüm `@Html.Raw`'a çevrildi
→ **10 test kırmızı**, geri alınınca yeşil.
**Canlı (Chrome + curl):** 3 farklı mesajlı istek → **tek satır, adet 3** (sayılar normalize edildi) ·
`/v1/auth/login?phone=+90…` → tabloda **`phone=***&page=2`** (tanılama parametresi korundu) ·
gövdede `source` gönderilmeden kayıt **`mobile`** düştü · panelde Türkçe rozetler (Mobil · Çökme · Açık) ·
çözüldü işaretlendi (kim/ne zaman/not) → **aynı hata tekrar gönderildi → kayıt kendiliğinden
yeniden açıldı, adet 3→4, çözüm izi temizlendi** · dashboard'da "Son 24 saatte 2 açık hata kaydı" ·
denetim izinde **"Hata Kayıtları / Hatayı çözdü"** (ham İngilizce yok).

---

### 12.2 — Şüpheli giriş günlüğü + e-posta raporlama + `StaffAdmin` izin tutarsızlığı — [x] ✅ TAMAMLANDI (6 Ağustos 2026)

**Hedef:** "Kim, nereden, ne zaman girmeye çalıştı" sorusunun cevabı + süper admin'e uyarı.

**Bugünkü durum:** `User.FailedLoginAttempts` + `LockedOutUntil` (11.18) yalnız **sayaç**
tutuyor — deneme kaydı yok, IP yok, vatandaş tarafında (OTP) hiçbir şey yok.
`IEmailService` soyutlaması + `Email:Smtp` config bloğu **9.2'den beri hazır bekliyor**,
tek eksik gerçek gerçekleme.

#### Backend

- **Varlık `LoginAttempt : BaseEntity`** (`login_attempts`):
  `Channel` (`panel`|`mobile_otp`) · `Identifier` · `UserId Guid?` · `Succeeded bool` ·
  `FailureReason` (`bad_password`|`bad_otp`|`unknown_user`|`locked_out`|`otp_blocked`|`banned`|`inactive`|`role_denied`) ·
  `IpAddress` · `UserAgent` · `IsSuspicious bool` · `SuspicionRule string?`.
  İndeks: `(IpAddress, CreatedAt)`, `(UserId, CreatedAt)`, `(IsSuspicious, CreatedAt desc)`.
- 🔴 **`Identifier` MASKELİ saklanır** (`+90500***0001`, `adm***`). Ham telefon bir güvenlik
  tablosunda birikirse tablo kendisi bir sızıntı hedefi olur; `UserId` zaten kimliği taşıyor.
  (`CODE_REVIEW_CHECKLIST` §7 "hassas veri loglanmıyor mu".)
- **Bağlanma noktaları:** panel `AccountController.Login` (başarılı + başarısız + kilitli +
  rol reddi), `VerifyOtpCommandHandler` (geçersiz OTP, banlı, pasif, başarılı),
  `RedisOtpService` blok kararı. **Var olan kilit/hız-sınır mantığı değiştirilmez**, yalnız
  gözlemlenir.
- **`SuspiciousLoginRules` — saf sınıf, birim testli** (container gerekmez):
  - **R1** aynı hesapta 15 dk içinde ≥5 başarısız (11.18 kilidini tetikleyen eşikle **aynı** olmalı — ayrışırsa kilitlenen giriş uyarı üretmez);
  - **R2** aynı IP'den 15 dk içinde ≥3 **farklı** hesaba ≥20 başarısız (kimlik bilgisi doldurma);
  - **R3** panel kullanıcısının **daha önce hiç görülmemiş** bir IP'sinden başarılı giriş;
  - **R4** kilit süresi biter bitmez gelen başarılı giriş (kaba kuvvetin tuttuğu senaryo).
  - Eşikler `appsettings` `Security:Suspicion:*` altından okunur; **varsayılanlar koda yazılı**
    (yapılandırma boşsa kural kapanmaz — "bayrakla kapalı yol" tuzağı).
- **`SmtpEmailService`** (`Infrastructure/Notifications/`) + `DependencyInjection` switch'ine
  `case "smtp"`. Faz 9.2'nin "sağlayıcı bağlama talimatı" birebir uygulanır — **çağıran kod değişmez.**
- **`SecurityAlertJob`** (Hangfire, 5 dakikalık): işlenmemiş şüpheli kayıtları toplar, **tek
  e-postada gruplar**, `super_admin` rolündeki ve `Email`'i dolu kullanıcılara yollar.
  - 🔴 **Kısma (throttle) zorunlu:** aynı kural + aynı hedef için Redis'te `security_alert:{hash}`
    anahtarıyla **saatte en fazla 1** e-posta. Kısma olmadan bir kaba kuvvet saldırısı, yöneticinin
    posta kutusuna **kendi kendimize yaptığımız DoS**'a dönüşür ve gerçek uyarı kaybolur.
  - Alıcı yoksa (e-postası dolu super_admin yok) → **uyarı loglanır, iş patlamaz**.
  - E-posta gövdesi Türkçe, kural adı + adet + IP + zaman aralığı + panel bağlantısı;
    **parola/OTP/token içermez.**
- 🔴 **ÖN KOŞUL — `ForwardedHeaders` (10.14'ten devralındı, "iyi olur" değil ZORUNLU).**
  Bugün `Api/Program.cs`'te yalnız **bir yorum satırı** olarak duruyor. Reverse proxy arkasında
  `RemoteIpAddress` **her istek için proxy'nin IP'si** olur; bu tabloda `LoginAttempt.IpAddress`
  demek, yani:
  - **R2 herkeste yanar** (tüm denemeler tek IP'den görünür → "kimlik bilgisi doldurma" alarmı
    her gün, her kullanıcı için),
  - **R3 hiç yanmaz** (IP hep aynı → "hiç görülmemiş IP" diye bir şey kalmaz).
  Yani ForwardedHeaders olmadan bu alt-fazın **ürettiği veri gürültüden ibarettir** ve üstüne
  kurduğumuz e-posta uyarısı yanlış alarm makinesine döner. Ayrıca 10.7'nin IP bazlı hız
  sınırı da tek partition'a düşer ve `HangfireDashboardAuthorizationFilter`'ın "yerel istek"
  dalı da aynı sebeple çöker (filtrenin kendi belgesi bunu söylüyor).
  ⚠️ `KnownProxies`/`KnownNetworks` **boş bırakılmaz** — açık bırakılırsa istemci kendi
  `X-Forwarded-For` başlığını uydurup güvenlik kaydını **zehirler** (kendi IP'sini gizler,
  başkasınınkini yazdırır). Ayar `appsettings`ten, boşsa **Production'da `ProductionReadinessGuard` engeller**.
- **`ProductionReadinessGuard`'a madde:** `Security:AlertEmailEnabled=true` iken
  `Email:Provider="Dev"` ise **uygulama açılmaz**. Sessiz başarısızlığın tam örneği:
  uyarılar üretilir, log'a yazılır, kimseye gitmez.
- ✅ **`/hangfire` yetkilendirme filtresi ZATEN VAR** (`HangfireDashboardAuthorizationFilter`) —
  bu alt-fazda yalnız `ForwardedHeaders` ile tamamlanır. 12.1 + 12.2 panoya **üç yeni iş**
  ekliyor (`PurgeErrorLogsJob`, `PurgeLoginAttemptsJob`, `SecurityAlertJob`); panoya erişen
  biri `PurgeLoginAttemptsJob`'ı elle tetikleyerek **yeni topladığımız güvenlik kanıtını
  silebilir** → panonun korumasının bu fazda gözden geçirilmesi tesadüf değil.
- **`secrets/README.md`'ye SMTP satırı** — kimlik bilgileri commit edilmez.
- **Saklama:** `PurgeLoginAttemptsJob` — başarılı 90 gün, başarısız 180 gün.

#### Panel

- **`LoginAttemptsAdmin` — yalnız-admin deseni** (12.1'deki üç kuralın aynısı).
- Liste + "Yalnız şüpheli" filtresi + IP/kullanıcı/tarih filtresi + `PanelCsv` dışa aktarma.
- `UsersAdmin` ve `StaffAdmin` detayında "Son giriş denemeleri" kutusu.
- `PanelDisplay`'e `LoginFailureReason()` ve `SuspicionRule()` Türkçe karşılıkları —
  **ham İngilizce basılmaz** (değişmez kural #6, `PanelDisplayTests` denetler).

#### Yeni görünmez sözleşmeler

- **Giriş denemesinde tanımlayıcı maskelidir**; ham telefon/kullanıcı adı saklanmaz.
- **Uyarı e-postası kısılır** — kısma kaldırılırsa saldırı posta bombasına döner ve
  gerçek uyarı gürültüde kaybolur.
- **R1 eşiği `PanelLockoutPolicy` eşiğiyle aynı olmak zorundadır** — ayrışırsa hesap kilitlenir
  ama kimseye haber gitmez (görünmez sözleşme #23'ün aynı sınıfı: iki taraf farklı gerçeklik görür).

#### Ek madde — `StaffAdmin`'in "karşılığı olmayan yetki"si (12.1'de bulundu)

🐛 **Bulgu:** `ARCHITECTURE.md` §3 ve `CLAUDE.md` Değişmez Kural #4, yalnız-admin ekranların
menü satırındaki `Module`'ün **`null`** olmasını şart koşuyor. `AuditLogsAdmin`, `TrashAdmin`
ve (12.1'de eklenen) `ErrorLogsAdmin` bu kurala uyuyor. Ama **`StaffAdmin` uymuyor**: hem
`AdminOnlyControllers`'ta hem de `Module = "staff"` taşıyor.

**Neden önemli:** `StaffAdminController.Modules` menüden türüyor
(`PanelMenu.Items.Where(i => i.RequiresPermission)`) → **"staff" izin matrisinde bir satır olarak
görünüyor.** Yönetici moderatöre "Personel: okuma" yetkisi verebiliyor, kutu işaretleniyor,
kaydediliyor — ama rol kapısı (`[Authorize(Roles = "admin,super_admin")]`) yüzünden o yetki
**asla çalışmıyor.** Bu tam olarak 11.15b'nin kapattığı **"karşılığı olmayan yetki"** hatasının
hâlâ ayakta duran örneği: yöneticiye verdiğini sandığı bir yetki, sessizce hiçbir şey yapmıyor.

**Neden 12.1'de düzeltilmedi:** düzeltme izin matrisini ve **seed'lenmiş `admin_permissions`
satırlarını** etkiliyor (mevcut kurulumlarda "staff" izni verilmiş moderatörler olabilir);
hata günlüğü modülüyle aynı commit'e sıkıştırılacak bir iş değil.

**Yapılacak:**
1. `PanelMenu.Items`'taki `StaffAdmin` satırının `Module`'ü **`null`** yapılır
   (`AuditLogsAdmin`/`TrashAdmin`/`ErrorLogsAdmin` deseni).
2. `StaffAdminController` üzerindeki `[PanelPermission("staff")]` varsa kaldırılır —
   rol kapısı zaten tek başına yeterli.
3. `PanelDisplay.NonMatrixModules`'a `["staff"] = "Personel"` eklenir; aksi hâlde denetim
   izi ekranı `AuditModule => "staff"` yazan komutlarda **ham İngilizce** basar
   (12.1'de `error-logs` için tam bu sebeple açılan kapı).
4. **Migration:** `admin_permissions` tablosundan `module = 'staff'` satırları temizlenir —
   duran satırlar matriste görünmeyen ama DB'de olan ölü izinlere dönüşür.
5. `permissions` tablosunda "staff" varsa aynı şekilde ele alınır.

**Yeni test (yapısal, tekrarı engeller):** `AdminOnlyControllers`'taki **her** controller'ın
menü satırının `Module`'ü `null` olmalı. Bu tek iddia bugün `StaffAdmin` yüzünden kırmızıdır
ve düzeltmeden sonra deseni kalıcı olarak kilitler — üç ekranın uyup dördüncünün uymaması
tam da testle yakalanması gereken şey.

⚠️ **Dikkat:** `PanelDisplayTests.StaffPermissionMatrix_DerivesFromPanelMenu` ve
`PanelDisplayTests.ModuleLabel_CoversEveryPermissionModule` bu değişiklikle **bilinçli olarak**
etkilenir; testleri gevşetmek değil, beklentiyi güncellemek gerekir.

**Bitti kriteri:** 5 hatalı panel girişi → 5 kayıt + kilit + **şüpheli işareti** ·
`AdminOnlyControllers`'ın tamamı `Module = null` (yapısal test yeşil), "staff" izin matrisinden
kalktı, denetim izi "Personel" yazıyor ·
`Email:Provider=Smtp` + yerel SMTP yakalayıcı ile **gerçek e-posta düştü** · aynı saldırı
ikinci kez → **ikinci e-posta gitmedi** (kısma çalıştı) · geçersiz OTP mobil kanalda kayıtlı ·
`Identifier` maskeli · Production'da `Dev` sağlayıcıyla uygulama **açılmıyor** ·
proxy başlığıyla gelen istekte **gerçek istemci IP'si** kaydediliyor, uydurma `X-Forwarded-For`
**yok sayılıyor**.

#### 12.2 kapanış notları

**Teslim edilenler:** `LoginAttempt` varlığı + migration `AddLoginAttempts` ·
`LoginIdentifierMasker` + `SuspiciousLoginRules` (saf, birim testli) ·
`ILoginAttemptRecorder`/`LoginAttemptRecorder` · `ForwardedHeadersSetup` (Api **ve** Web) ·
`SmtpEmailService` + DI `case "smtp"` · `SecurityAlertJob` (Redis kısma) ·
`PurgeLoginAttemptsJob` · `LoginAttemptsAdmin` (liste + filtre + CSV + **"Uyarı kanalını dene"**) ·
dashboard rozeti · `UsersAdmin`/`StaffAdmin` detayında "son giriş denemeleri" ·
`ProductionReadinessGuard`'a 2 engelleyici + 1 uyarı · `StaffAdmin` matris düzeltmesi.
**Backend 605 → 663 (+58), mobil 685 (değişmedi), analyze 0.**

🐛 **CANLI DOĞRULAMADA BULUNAN ÜÇ ŞEY** (üçü de "kuralı bilerek boz" ya da gerçek kullanım sırasında çıktı):

1. 🔴 **Hız sınırı kayıt yolundan ÖNCE çalışıyordu — kısılan denemeler HİÇ kaydedilmiyordu.**
   `panel-login` limiti (9.2) controller'dan önceki bir ara katman; dakikada 5'i aşan
   denemeler `AccountController`'a hiç girmiyor ve `login_attempts`'e **tek satır bile**
   düşmüyordu. Sonuç tam da bu fazın savaştığı sınıftı: saldırgan dakikada 500 deneme
   yapar, panel "5 deneme" gösterir. ⚠️ **Kısma ne kadar iyi çalışırsa tablo o kadar çok
   yalan söylüyordu.** Çözüm: `OnRejected` içinde `rate_limited` sebebiyle kayıt (fırlatmaz;
   gövde okunamazsa kimlik boş geçer, IP + zaman yine yazılır). Canlıda 8 deneme →
   **5 `unknown_user` + 3 `rate_limited`**.

2. 🔴 **Maskeleme, yanındaki sütundan deliniyordu.** `Identifier` özenle maskeleniyordu ama
   listenin "Kullanıcı" sütunu panelin alışılmış `Username ?? Phone` desenini kullanıyordu:
   **kullanıcı adı olmayan bir vatandaş hesabında ham telefon numarası CSV'ye düşüyordu.**
   🔑 İlk yazdığım test bunu **kaçırdı** — test kullanıcısı moderatördü, adı vardı, yani
   yedek dala hiç girilmiyordu. Düzeltmeyi geri alınca test **yeşil kaldı**; kırılgan durum
   için (adı olmayan vatandaş) ayrı bir test yazılınca yakalandı. Ders: **maskeleme testi,
   maskelemenin devreye girdiği dalda kurulmalı.**

3. 🔴 **Eşik sözleşmesi yalnız KODDAKİ varsayılan için kilitliydi.** R1 eşiğini
   `PanelLockoutPolicy.MaxFailedAttempts`'e bağlayan test vardı, ama eşiği `appsettings`
   ezebiliyor ve `LoginAttemptRecorder` yapılandırmayı okuyor. Bilerek bozunca saf kural
   testi kırmızıya döndü, **uçtan uca panel testi yeşil kaldı** (yapılandırma hâlâ 5
   diyordu). `appsettings` ile sabiti eşitleyen test eklendi — **iki dosya birden**
   (Api + Web), çünkü giriş iki ayrı süreçte kaydediliyor.

🔑 **DİĞER KARARLAR:** `Identifier` **maskeli ve deterministik** (hatalı OTP dalında
`UserId` bilerek boş — o dalda kullanıcı tablosuna dokunulmuyor, 10.2 kuralı; kayıt hesaba
**maskeli kimlikle** bağlanıyor) · kural **önceliği** R2 > R1 (kimlik doldurma altındaki tek
tek hesapları R1 de yakalar; R1 önce gelseydi yönetici "20 ayrı uyarı" görüp asıl olayı
kaçırırdı) ve R4 > R3 · **R3 yalnız panel kanalında** (mobil şebekede IP her gün değişir,
orada kural yanlış alarm makinesi olurdu) · IP'yi **yalnız `LoginAttemptRecorder` okur**
(üç ayrı yerde okunsaydı `ForwardedHeaders`'ı doğru yorumlama sorumluluğu dağılırdı) ·
`UsersAdmin`'deki kutu **rol kapısıyla korunuyor** (ekran moderatöre açık, veri değil —
"ekran kapalı ama verisi başka yerde görünüyor" sessiz sızıntısı) · `SecurityAlertJob`
alıcı yoksa **patlamaz**, loglar · e-posta gövdesi **HTML-kaçırılmış** (uyarı postası
enjeksiyon taşıyıcısı olmamalı).

➕ **PLAN DIŞI EKLENENLER (kullanıcı onaylı serbest kapsam):**
- **"Uyarı kanalını dene" butonu** (`LoginAttemptsAdmin/SendTestAlert`). Gerekçe projenin
  kendi dersi: *"bayrakla kapalı yol = hiç test edilmemiş yol"* (10.11 FCM). `SecurityAlertJob`'ın
  e-posta yolu ancak **gerçek bir saldırı sırasında** ilk kez koşar — SMTP yanlışsa bunu tam
  da en kötü anda öğreniriz. Buton o yolu bugün çalıştırır; alıcı **her zaman kişinin kendisi**
  (serbest alıcı alanı paneli spam aracına çevirirdi).
- **Dashboard "şüpheli giriş" rozeti** (12.1'in hata rozetiyle aynı desen ve aynı rol kapısı).
- **`rate_limited` sebebi** (yukarıdaki 1. bulgunun çözümü).
- **`secrets/panel-admin.json`** — panel süper admin parolasının git'e girmeyen tek kaynağı.
  🐛 Gerekçe gerçek bir tekrar eden sorun: parola 11.18'de değiştirildi, kaynaktaki sabit
  (`DbSeeder.AdminPassword`) o günden beri **yalan söylüyordu** ve doğrusu hiçbir yere
  yazılamıyordu (depo herkese açık; 11.18'de tam bu yüzden gerçek bir sızıntı yaşandı).
  Dosya `secrets/*` altında → **commit edilmesi imkânsız**. Seed parolayı ona **hizalar**
  (aynıysa yazmaz — her açılışta yazsaydı `PasswordChangedAt` tazelenir ve
  `OnValidatePrincipal` yöneticiyi kendi oturumundan atardı) ve kilidi temizler.
  ⚠️ **Yalnız Development** · `MustChangePassword` **işaretlenmez** (11.18'in kuralı
  "parolayı *başkası* belirlediyse zorla"dır; burada belirleyen sahibi) ·
  ⚠️ **Testler bu dosyayı bilinçli olarak yok sayar** (iki factory'de boş değerle ezilir):
  okusaydılar testler *kimin makinesinde koştuğuna göre* farklı davranırdı.

**Yeni görünmez sözleşmeler: #34, #35, #36** (kimlik maskeli + deterministik · R1 eşiği
kilit eşiğiyle aynı · uyarı e-postası kısılır). Toplam **36**.

**Doğrulama:** `dotnet test` **663/663** · `flutter analyze` **0** · `flutter test` **685/685**.
**Kuralı bilerek boz (5 deneme, hepsi kırmızıya döndü):** R1 eşiği koddan ayrıldı ·
`appsettings` eşiği 7 yapıldı · maskeleme kaldırıldı (7 test) · `StaffAdmin` yeniden
`Module="staff"` (2 test) · görünüm `@Html.Raw`'a çevrildi · `UserName` yedeği ham telefona
döndü. **İlk denemede 2'si yeşil kaldı** → eksik testler yazıldı, sonra kırmızıya döndüler.

**Canlı (curl + Chrome + gerçek Android emülatörü):**
- 5 hatalı panel girişi → **5 kayıt + kilit + son satırda R1** · kimlik `adm***` (ham yok)
- 8 hızlı deneme → **5 `unknown_user` + 3 `rate_limited`** (kör nokta kapandı)
- **Gerçek mobil uygulamadan** hatalı OTP → `+90532***0002` · `mobile_otp` · `bad_otp` ·
  UA `Dart/3.12 (dart:io)` (isteğin uygulamadan geldiğinin kanıtı); doğru kodla giriş → başarılı kayıt
- admin'in ilk `::1` girişi → **R3 "Yeni IP'den panel girişi"** kendiliğinden yandı
- `SecurityAlertJob` → `super_admin`'e **Türkçe uyarı e-postası** (Dev sağlayıcı log'a yazdı),
  Redis kısma anahtarı TTL < 1 saat ve ikinci `SETNX` **boş** döndü (e-posta gönderilmez)
- `ForwardedHeaders`: güvenilen vekil `127.0.0.1` iken `X-Forwarded-For: 9.9.9.9` → **kaydedilen IP 9.9.9.9**;
  güvenilen vekil `10.9.9.9` iken uydurma başlık → **yok sayıldı, IP 127.0.0.1 kaldı**
- Production kapısı: `ForwardedHeaders` açık + güvenilen kaynak yok → uygulama **açılmadı**
- Panelde Türkçe rozetler (Panel · Mobil (OTP) · Hatalı parola · Kullanıcı yok · Hız sınırı ·
  Aynı hesaba yoğun deneme) · "staff" izin matrisinden **kalktı** · dashboard'da
  "Son 24 saatte 7 şüpheli giriş denemesi"

🐛 **12.2 KAPSAMI DIŞINDA BULUNAN MOBİL HATA (düzeltilmedi, sonraki faza):** canlı doğrulama
sırasında OTP ekranında hızlı ard arda gezinme `Navigator._debugCheckDuplicatedPageKeys`
assertion'ını patlattı (`'!keyReservation.contains(key)'`). Aynı sayfa anahtarı iki kez
yığına giriyor — `go_router`'ın "`context.push` ile açılan ekran redirect'in ÜSTÜNDE kalır"
ailesinden (§7 kod-dışı sözleşmeler). 🔑 **Ama asıl haber şu: çökme 12.1'in aynasına düştü** —
`error_logs`'ta `mobile` / `android` / `1.0.0+1` kaynağıyla duruyor. Gözlem katmanı, kendi
kurulduğu fazın bir sonrakinde ilk gerçek işini gördü.

---

### 12.2b — Bildirim gönderimi: teslim panosu + bağımsız push ekranı — [x] ✅ TAMAMLANDI (6 Ağustos 2026)

> **Neden bu numarada:** 11.18'den kalan son madde buydu ve gözlem ailesine ait —
> 12.1'in panel desenleri (yalnız-admin ekran, saklama işi, toplu liste) daha taze.
> **12.3'ten ÖNCE** olmasının somut sebebi: 12.3, bir modülün push'u ilk kez **otomatik**
> göndermeye başladığı yer. Gönderimin sonucunu göremeden otomatik göndermeye başlamak,
> bu fazın kapatmaya çalıştığı "sessiz hasar" sınıfının ta kendisi olurdu.
> 12.3'ün içine sıkıştırılmadı: o oturumda zaten migration + geri doldurma + duyuru üretimi
> + panel + mobil var.

> 🐛 **DEVRALINAN MADDENİN TARİFİ YANLIŞTI — 5 Ağustos'ta kodda doğrulandı.**
> 11.16b notu "şema değişikliği: FCM yanıtının saklanması" diyordu. **FCM yanıtı mesaj
> düzeyinde ZATEN saklanıyor:** `Notification.FcmSent` / `FcmSentAt` / `FcmError` var ve
> `SendPushNotificationsJob` `TokenInvalid` gelince `User.FcmToken`'ı **temizliyor** (10.11).
> Gerçek boşluk başka: **(a)** bildirimlerin panelde **hiçbir ekranı yok**
> (`ARCHITECTURE.md` modül tablosu: Bildirimler → Panel *(yok)*), **(b)** serbest bir
> gönderimi **gruplayacak anahtar yok** — duyuru bildirimleri `RelatedId` ile gruplanıyor,
> ada hoc gönderimin tutunacağı bir şey yok, **(c)** bildirim satırı üreten tek şey
> `AnnouncementNotificationGenerator`; yani yönetici tek seferlik bir push atmak için
> **duyuru oluşturmak zorunda**.

#### Backend

- **Yeni varlık `PushCampaign : BaseEntity`** (`push_campaigns`): `Title` · `Body` ·
  `TargetType` (`all`|`neighborhood`) · `TargetNeighborhoods` · `Source`
  (`announcement`|`power_outage`|`manual`) · `SourceId Guid?` · `CreatedBy` ·
  `RecipientCount` · `SentCount` · `FailedCount` · `InvalidTokenCount` · `CompletedAt`.
  `Notification.CampaignId Guid?` FK ile bağlanır — **additive, mevcut satırlar `null` kalır.**
- 🔑 **Toplam sayaç neden ayrı kolonda:** her açılışta 5.000 bildirim satırını `GROUP BY`
  ile saymak panelin en büyük tablosunu tarar. `SendPushNotificationsJob` batch'i işlerken
  sayaçları **zaten elinde olan** `sent/failed/invalidTokens` değerleriyle artırır (job bu
  üçünü bugün de hesaplıyor, yalnız log'a yazıp atıyor).
- 🔴 **`FcmSent=true` TERMİNALDİR — panel "yeniden gönder" TEKLİF ETMEZ.** Job'ın belgesi
  açık: mesaj bazlı hatalar kalıcı sayılır ve satır bir daha alınmaz. Panelde yeniden gönder
  butonu konursa **hiçbir şey yapmaz ve kimse hata almaz**; yeniden gönderim istenirse
  **yeni kampanya** açılır (yeni satırlar üretilir).
- **Bağımsız gönderim komutu** — hedefleme mantığı **kopyalanmaz**:
  `AnnouncementNotificationGenerator`'daki mahalle süzgeci + `NotificationPreferences` +
  idempotency ortak bir servise çıkarılır, hem duyuru hem manuel gönderim onu kullanır.
  ⚠️ İkinci bir hedefleme gerçeklemesi yazılırsa duyuru ile manuel gönderim **aynı mahalleye
  farklı kişi kümesi** yollar (görünmez sözleşme #23 sınıfı).
- ⚠️ **Bildirim tercihi manuel gönderimde de geçerlidir** — `Announcements=false` diyen
  kullanıcıya satır yazılmaz. Aksi hâlde "bildirimleri kapattım ama geliyor" doğar ve
  10.3'ün tercih ekranı yalan söyler.
- Saklama: `PurgeNotificationsJob` — okunmuş + 90 günden eski bildirimler; kampanya satırı
  **kalır** (özet ucuz, tarihçe değerli).

#### Panel

- **`PushCampaignsAdmin` — yalnız-admin deseni** (12.1'deki üç kuralın aynısı: rol kapısı ·
  `[PanelPermission]` YOK · menü `Module=null` · `AdminOnlyControllers`).
- Liste: Başlık · Kaynak · Hedef · **Alıcı / Gönderildi / Başarısız / Geçersiz token** ·
  Tarih · Durum. `PanelSorts` + `ThenBy(Id)` · `PanelCsv` dışa aktarma.
- Detay: hata kodlarına göre kırılım (`FcmError` gruplu) — "188 başarısız"ın **neden**i.
- **"Yeni bildirim gönder" formu:** başlık + metin + hedef (tümü / mahalle çoklu seçimi) +
  **tahmini alıcı sayısı** + `data-confirm` ile "N kişiye gönderilecek" onayı.
  🔴 Aksiyon adı izin eylemini belirler (görünmez sözleşme #19): `Send…` hiçbir önekle
  eşleşmez ve POST olduğu için sessizce **`update`**'e düşer — ekran yalnız-admin olduğu için
  bugün zararsız, ama **matris dışı olduğu `AdminOnlyControllers`'ta yazılı olmalı**.
- `IAuditableCommand` + `PanelDisplay.AuditAction` Türkçe satırı (kime, ne zaman, kaç kişiye).
- Dashboard'a "son gönderim: N/M teslim" satırı.

#### Yeni görünmez sözleşmeler

- **`FcmSent=true` terminaldir** — yeniden gönderim yeni kampanya açar, eski satır dokunulmaz.
- **Hedefleme mantığının tek sahibi vardır** — duyuru ve manuel gönderim aynı servisten geçer.
- **Kampanya sayaçları job tarafından artımlı yazılır**, sorgu anında `COUNT` ile hesaplanmaz.

**Bitti kriteri:** panelden 2 mahalleye manuel push gönderildi → kampanya satırı + doğru alıcı
sayısı · job koştu → **gönderildi/başarısız sayaçları doldu** · geçersiz token'lı kullanıcının
`FcmToken`'ı temizlendi ve sayaca yansıdı · bildirimleri kapatmış kullanıcı **listede yok** ·
**emülatörde push düştü** · aynı kampanya ikinci kez işlenince sayaç **artmadı** (idempotency).

#### 12.2b kapanış notları

**Teslim edilenler:** `PushCampaign` varlığı + migration `AddPushCampaigns` ·
`Notification.CampaignId` (additive FK) · **`INotificationDispatcher`/`NotificationDispatcher`**
(hedeflemenin tek sahibi) · `AnnouncementNotificationGenerator` ona devredildi ·
`PushCampaignStatus` (saf, birim testli) · `SendPushCampaignCommand` + `CancelPushCampaignCommand` ·
`GetPushCampaignsQuery`/`GetPushCampaignByIdQuery`/`EstimatePushRecipientsQuery`/`GetLastPushCampaignQuery` ·
`SendPushNotificationsJob` artımlı sayaç yazımı · `PurgeNotificationsJob` ·
`PushCampaignsAdmin` (liste + ayrıntı + gönderim formu + **iptal** + CSV) · dashboard satırı ·
`PanelSorts.PushCampaigns` · `PanelDisplay` üç yeni rozet ailesi.
**Backend 663 → 689 (+26), mobil 685 (değişmedi), analyze 0.**

🔑 **TESLİM EDİLEN:** "duyuruyu yayınladım, gitti mi?" sorusunun cevabı artık var. FCM'in
cevabı 10.11'den beri `notifications.fcm_*` alanlarında **saklanıyordu** — ama o satırlara
bakan bir ekran yoktu; yönetici cevabı ancak veritabanına girerek bulabiliyordu. İkinci
boşluk daha da somuttu: bildirim satırı üreten tek şey duyuru üreticisiydi, yani tek seferlik
bir push atmak için **vatandaşın duyurular listesine kalıcı bir kayıt düşürmek** gerekiyordu.

🐛 **UYGULAMA SIRASINDA BULUNAN ÜÇ ŞEY (üçü de test ya da canlı doğrulamayla çıktı):**

1. 🔴 **`Id` kolonu store-generated olduğu için FK aynı `SaveChanges` içinde kurulamıyordu.**
   Dispatcher önce kampanyayı, sonra bildirimleri ekliyor ve FK'yı `CampaignId = campaign.Id`
   ile yazıyordu — ama `Id` kolonları `gen_random_uuid()` varsayılanıyla tanımlı, yani EF
   değeri **INSERT'ten sonra** geri alıyor ve o satırda `campaign.Id` hâlâ `Guid.Empty`.
   Bütün bildirimler var olmayan bir kampanyaya bağlandı ve FK ihlaliyle patladı. Çözüm:
   bağı **gezinme özelliğinden** kurmak (`Campaign = campaign`). ⚠️ Bu hata sessiz değil
   gürültülüydü (13 test birden kırmızı) — ama yalnız var olan testler yüzünden: yeni bir
   modülde ilk kez yazılsaydı hiçbir şey onu yakalamazdı.

2. 🔴 **"Tamamlandı" ile "geri çekilecek bir şey kalmadı" aynı şey değil.** `CanCancel` ilk
   yazımda "tamamlanmamış" koşuluna bağlıydı; test hemen kırdı. Kampanya, **gönderilebilir**
   bekleyen satır kalmadığında tamamlanır — ama token'ı olmayan alıcıların satırları hâlâ
   durur, kullanıcının bildirim listesinde görünür ve o kişi yarın token kaydederse gönderilir.
   Doğru ölçüt `PendingCount`.

3. 🔴 **Panelin onay penceresi hiç açılmıyordu — ve bu 12.2'den kalan bir hataydı.**
   `data-confirm` dinleyicisi `_Layout`'ta tek yerde ve **form**un özniteliğine bakıyor;
   öznitelik butona yazılınca hiçbir şey olmaz: kod doğru görünür, Razor derlenir, hiçbir
   test kırılmaz ve **geri alınamaz aksiyon onaysız koşar**. 12.2'nin "Uyarı kanalını dene"
   butonu tam bu durumdaydı ve canlı doğrulamada tesadüfen görüldü. Üç görünüm düzeltildi
   (`LoginAttemptsAdmin` + 12.2b'nin ikisi) ve **yapısal test eklendi**
   (`PanelConfirmDialogTests`): `data-confirm` yalnız `<form>` üzerinde olabilir; tek bilinçli
   istisna `_BulkToolbar` (kendi `click` dinleyicisi var).

🔑 **DİĞER KARARLAR:** hedefleme **tek sahipli** (`INotificationDispatcher`) ve panelin
"tahmini alıcı" önizlemesi **aynı sorguyu** çağırıyor · `NeighborhoodIds` **`null` ≠ boş liste**
(null "liste yok → herkes", 10.10'dan beri testle yazılı; boş liste "hiçbir mahalle seçilmemiş
→ kimse" — bozuk JSON bu yüzden tüm şehre gidemiyor) · manuel gönderimde **deep-link yok**
(`relatedType = null`, uydurma bir tür görünmez sözleşme #18 gereği zaten iptal edilirdi) →
**mobilde sıfır değişiklik** · sayaçlar **kolonda ve artımlı** (her liste açılışında `GROUP BY`
yapmamak için) · durum **türetilmiş** (ayrı kolon olsaydı sayaçlarla ayrışabilirdi) ·
`RecipientCount` iptalde **düşürülmez** (tarihçe) · `PurgeNotificationsJob` yalnız **okunmuş**
bildirimi siler ve **kampanya satırına dokunmaz** · `AuditBehavior` `ApiResponse<Guid>` için
düzeltildi (doğrulamadan dönen bir RET, denetim izine "bildirim gönderdi" yazacaktı).

➕ **PLAN DIŞI EKLENENLER (kullanıcı onaylı serbest kapsam):**
- **Gönderim iptali** (`CancelPushCampaignCommand` + `CancelledAt`). Gerekçe: bir bildirim
  gönderildikten sonra düzeltilemez; yanlış metinle yollanan gönderimin 12.2b öncesi tek
  çaresi veritabanına elle girmekti. İptal, gönderimin **tersi değil sınırı**: `FcmSent=true`
  terminal olduğu için iletilmiş mesaja dokunmaz ve dokunmayı **teklif de etmez**.
- **`PanelConfirmDialogTests`** (yukarıdaki 3. bulgunun kalıcı kilidi).
- **Kampanya durumu + bekleyen sayısı** ekranda; iptalden sonra aynı sayının etiketi
  "Bekleyen" değil **"Geri çekilen"** olur (canlı doğrulamada yakalandı: sayı doğru, etiket
  yalan söylüyordu).

**Yeni görünmez sözleşmeler: #37, #38, #39** (`FcmSent` terminaldir · hedeflemenin tek
sahibi vardır · sayaçlar artımlıdır ve tamamlanma ölçütü "gönderilebilir bekleyen kalmadı"dır).
Toplam **39**.

**Doğrulama:** `dotnet test` **689/689** · `flutter analyze` **0** · `flutter test` **685/685**.
**Kuralı bilerek boz (5 deneme, hepsi kırmızıya döndü):** sayaç yazımı atlandı (2 test) ·
bildirim tercihi süzgeci kaldırıldı (5 test) · iptal iletilmiş satırlara da dokundu (2 test) ·
menü satırına modül anahtarı verildi (2 test) · `data-confirm` butona taşındı (1 test).

**Canlı (Chrome + gerçek Android emülatörü):**
- Panelden Cengiz Topel'e gönderim → **tahmini alıcı 5**, gönderim de **5 satır** yazdı
  (önizleme ↔ gerçek paritesi, #38)
- `SendPushNotificationsJob` koştu → **1 gönderildi, 4 bekliyor** ve kampanya
  **"Tamamlandı"** oldu (token'ı olmayan 4 alıcı kampanyayı sonsuza kadar açık bırakmadı, #39)
- **Gerçek emülatörde push düştü**: `aysedmr` hesabında ön plan bildirimi + sekme rozeti 1;
  bildirim listesinde **genel "Bildirim" kimliğiyle** göründü (deep-link yok — mobilde
  hiçbir değişiklik gerekmedi)
- İptal → **yalnız 4 gönderilmemiş satır silindi**, iletilmiş 1 satır **durdu** (#37);
  `completed_at` **tazelenmedi** (ilk tamamlanma anı korundu)
- Denetim izinde **"Bildirim Gönderimleri / Bildirim gönderdi"** ve **"Gönderimi iptal etti"**
  (ham İngilizce yok) · dashboard'da **"Son gönderim: 1 / 5 teslim"**
- CSV: **BOM + noktalı virgül + Türkçe başlıklar ve değerler**
- Alıcısı olmayan mahalleye gönderim → **"Hedeflemeye uyan kullanıcı bulunamadı — hiç bildirim
  yazılmadı"** + **"Alıcı yok"** rozeti (sessizce "gönderildi" demedi)

🐛 **12.2'DEN DEVRALINAN MOBİL ÇÖKME** ✅ *(bu başlık **12.2b günü** yazıldı ve o gün doğruydu;
**12.3'te kök neden bulundu ve kilitlendi** — kabuk rotasına `push` → mükerrer sayfa anahtarı,
tek sahip `core/router/app_nav.dart`, kilit `mobile/test/core/navigation/shell_page_key_test.dart`.
13 Ağu 2026 açık-madde denetiminde bu satır **bayat** bulundu: "HÂLÂ AÇIK" diyordu ve açık
madde arayan birini yanıltıyordu.)*

**O günkü durum (tarihsel kayıt):** kök neden doğrulanmamıştı.
`Navigator._debugCheckDuplicatedPageKeys` assertion'ı **widget testinde yeniden üretilemedi**:
yazılan test düzeltme geri alındığında da **yeşil kaldı**, yani hiçbir şey kilitlemiyordu ve
projenin kendi ölçütüne göre değersizdi → **silindi.** `error_logs`'taki yığın izi tamamen
framework karesi (uygulama karesi yok) ve zincirin tepesinde `_InheritedNotifierElement.update`
var — yani çökme **router bildirimiyle gelen bir Navigator rebuild'inde** doğuyor, doğrudan
bir `push` çağrısında değil. İki **savunma amaçlı** düzeltme yapıldı ama ikisi de kanıtlanmış
çözüm değil ve dokümanlarında böyle yazılı: (a) `phone_login_screen._submit` artık kod ekranı
zaten yığındaysa `push` etmiyor, (b) `otp_verify_screen._changePhone` **önce pop edip sonra**
durumu değiştiriyor (projenin kendi kod-dışı sözleşmesi: "`context.push` ile açılan ekran
router redirect'inin ÜSTÜNDE kalır"). **Tekrar ederse `error_logs` yine yakalayacak.**

---

---

### 12.3 — Kesinti mahalle referansı + mahalle bazlı bildirim — [x] ✅ TAMAMLANDI (7 Ağustos 2026)

**Hedef:** Kesintinin sözlükteki mahalleye bağlanması ve o mahallenin sakinlerine bildirim gitmesi.

**Bugünkü durum (koddan doğrulandı):** `PowerOutage.Neighborhood` **serbest metin**, FK yok →
hedefleme imkânsız. `PowerOutage.AnnouncementId` alanı **var ama hiç doldurulmuyor**.
`CreatePowerOutageCommandHandler` yalnız satır ekliyor — kesinti bugün **hiçbir bildirim üretmiyor.**
Buna karşılık hedefleme altyapısı **tamamen hazır**: `Neighborhood` sözlüğü, `User.PrimaryNeighborhoodId`,
`UserNeighborhood` (çoklu mahalle), `Announcement.TargetType="neighborhood"` +
`AnnouncementNotificationGenerator` (bildirim tercihi ve idempotency dâhil).

#### Backend

- **`PowerOutage.NeighborhoodId Guid?`** FK → `neighborhoods` + **`AreaDetail string?`**
  (sokak/bölge ayrıntısı — kesinti bazen mahallenin bir kısmını kapsıyor).
- 🔴 **`Neighborhood` string kolonu KALIR ve DTO'da adı değişmez.** Artık **türetilmiş**:
  `NeighborhoodId` doluysa değer sözlükten yazılır, elle düzenlenemez. Böylece mağazadaki
  eski sürümler tek satır değişmeden çalışmaya devam eder (kontrat additive, §5).
- **Geri doldurma (backfill):** mevcut satırlar `SlugHelper.Slugify` ile `neighborhoods.slug`'a
  eşleştirilir. 🔴 **Migration içinde kör SQL ile yapılmaz** — idempotent bir başlangıç adımı
  olarak koşar ve **eşleşmeyen satırları raporlar**; panel "mahallesi eşleşmemiş kesinti"
  sayısını gösterir. Eşleşme `SlugHelper` üzerinden yapılmalı (görünmez sözleşme #21 — ikinci
  bir normalleştirme yazılırsa `İ`/`ı` yüzünden sessizce yanlış eşleşir).
- **Bildirim = duyuru** (karar tablosu): `CreatePowerOutageCommand`'e `SendNotification bool` +
  `TargetNeighborhoodIds` (birden çok mahalleyi tek kesinti kapsayabilir):
  1. `AnnouncementType` "Elektrik Kesintisi" `DbSeeder`'a idempotent blok olarak eklenir;
  2. duyuru üretilir — `TargetType="neighborhood"`, `Status="active"`,
     `SendPushNotification=true`, `VisibleUntil = outage.EndTime`;
  3. `PowerOutage.AnnouncementId` doldurulur;
  4. `AnnouncementNotificationGenerator` çağrılır (idempotency ve bildirim tercihi orada zaten var).
- 🔴 **Bildirim yalnız `NeighborhoodId` doluyken gönderilebilir.** Serbest metinli kayıt
  hedeflenemez; panel bunu **buton kapalı + açıklama** ile söyler ("işlevsiz buton yok"un panel karşılığı).
- 🔴 **Güncelleme ikinci duyuru üretmez** — var olan duyuru güncellenir (saat değiştiyse
  `VisibleUntil` de). 🔴 **Silme duyuruyu ve bildirimlerini de siler** — görünmez sözleşme #24
  (silinen duyurunun bildirimleri ayakta kalırsa kullanıcı dokunup boş sayfaya düşer;
  11.15c'de tam olarak bu yaşandı, 9 ölü bildirim bulundu).
- Cache: `announcements` grubu geçersizleştirilmeli (kesinti yazımı artık duyuru yazımıdır).
- DTO additive: `neighborhoodId`, `areaDetail`. **`neighborhood` alanı aynen kalır.**
- ⚠️ Görünmez sözleşme #1 korunur: `GET /v1/power-outages` **sayfalamaz**, düz dizi döner.
  Mahalle filtresi eklense bile bu değişmez.

#### Panel

- `Create`/`Edit` formunda serbest metin → **mahalle çoklu seçimi** (duyuru formundaki
  checkbox deseninin aynısı) + ayrı "Bölge ayrıntısı" metin alanı.
- "Bu kesinti için bildirim gönder" onay kutusu; yanında **tahmini alıcı sayısı**
  ("Seçilen mahallelere kayıtlı 342 kullanıcı") — yönetici neyi tetiklediğini görmeden basmasın.
- Index'e mahalle filtresi + "mahallesi eşleşmemiş" uyarı şeridi.
- Zaman ayrımı (`PowerOutagePhaseRules`) **değişmez** — görünmez sözleşme #27 (başlangıç dâhil,
  bitiş hariç; mobil tanımıyla birebir).

#### Mobil

- `PowerOutage` modeline `neighborhoodId` eklenir; ekrana **"Mahallem"** filtresi
  (kullanıcının `PrimaryNeighborhoodId`'si). Oturum yoksa veya mahalle seçilmemişse filtre gizlenir.
- Bildirim akışı için **değişiklik gerekmez** (karar gereği) — duyuru deep-link'i zaten çalışıyor.

#### Yeni görünmez sözleşmeler

- **`power_outages.neighborhood` metni `NeighborhoodId` doluyken sözlükten türetilir**, elle yazılmaz.
- **Kesinti bildirimi bir duyurudur:** kesinti silinince duyurusu ve bildirimleri de silinir (#24 uzantısı).
- **Bildirim yalnız FK dolu kesintide gönderilebilir.**

**Bitti kriteri:** panelden mahalle seçilerek kesinti eklendi → `announcements` satırı +
`power_outages.announcement_id` dolu → o mahalleye kayıtlı kullanıcıda bildirim satırı ·
**başka mahalledeki kullanıcıda satır YOK** · kesinti silindi → duyuru + bildirimler gitti ·
`GET /v1/power-outages` **hâlâ düz dizi** ve `neighborhood` alanı dolu · geri doldurma raporu doğru.

#### 12.3 kapanış notları

**Teslim edilenler:** `PowerOutage.NeighborhoodId` (FK → `neighborhoods`, `SetNull`) + `AreaDetail` +
migration `AddPowerOutageNeighborhood` · `PowerOutageNeighborhoodMatcher` + `PowerOutageNeighborhoodResolver`
(saf, birim testli) · `PowerOutageNeighborhoodBackfill` (idempotent açılış adımı + rapor) ·
`IPowerOutageAnnouncementWriter` / `PowerOutageAnnouncementWriter` (**kesinti ↔ duyuru bağının tek sahibi**) ·
`PowerOutageAnnouncementText` (saf; TR yerel saat) · `DbSeeder.EnsurePowerOutageAnnouncementTypeAsync` ·
üç komut yeniden yazıldı (Create/Update artık `IAuditableCommand`) · panelde mahalle seçimi +
bölge ayrıntısı + bildirim onayı + **canlı tahmini alıcı** + eşleşmemiş şeridi + mahalle süzgeci +
"Bildirim" sütunu · mobil `neighborhoodId`/`areaDetail` + **kimlik öncelikli** mahalle eşleşmesi.
**Backend 689 → 729 (+40), mobil 685 → 696 (+11), analyze 0.**

🔑 **TESLİM EDİLEN:** `PowerOutage.AnnouncementId` 10.x'ten beri duran **boş bir çengeldi**;
kesinti hiçbir bildirim üretmiyordu ve `Neighborhood` serbest metin olduğu için üretemezdi de.
Artık kesinti sözlüğe bağlı ve **kendiliğinden bildirim gönderiyor**.

🔴 **EN ÖNEMLİ KARAR — kesinti bildirimi ayrı bir tür DEĞİL, BİR DUYURU.** Faz başında alınmıştı,
uygulamada bedeli görüldü ve doğru çıktı: `Announcement` + `AnnouncementNotificationGenerator` +
`SendPushNotificationsJob` + deep-link zinciri **aynen** çalıştı → **mobilde tek satır bildirim
kodu yazılmadı** ve mağazadaki eski sürümler de kesinti bildirimini alıyor. Yeni bir `relatedType`
uydurulsaydı görünmez sözleşme #18 gereği eski sürümler bildirime dokunduğunda **sessizce hiçbir
yere gitmezdi.** Tek eklenen şey kampanya **etiketi** (`PushCampaignSources.PowerOutage`) —
teslim panosunda "bu push nereden çıktı?" sorusunun cevabı "duyuru" olsaydı yönetici kesinti
gönderimlerini hiçbir süzgeçle ayıramazdı.

🐛 **UYGULAMA SIRASINDA BULUNAN DÖRT ŞEY:**
1. 🔴 **`Repository.Query()` varsayılan olarak `AsNoTracking()`.** `RemoveAsync` duyuruyu onunla
   alıp `SoftRemove` çağırıyordu: alan bağlantısız nesneye yazılıyor, `SaveChanges` onu **hiç
   görmüyor**. Duyuru "silinmiş görünüyor", `deleted_at` boş kalıyor, **hiçbir hata oluşmuyor**.
   `Update()`/`Remove()` çağıran yollarda görünmez çünkü EF nesneyi yeniden iliştirir — yalnız
   *alan yazan* işlemler sessizce kayboluyor. Testte yakalandı, `Query(tracking: true)` ile düzeltildi.
2. 🔴 **Seed'in "tablo boşsa" bloğu yeni bir satırı garanti etmez.** "Elektrik Kesintisi" duyuru
   türü `AnnouncementTypes` listesinde zaten vardı ama blok `if (!await db.AnnouncementTypes.AnyAsync())`
   ile korunuyor — yani 12.3'ten **önce ayağa kalkmış her veritabanında** o blok bir daha hiç
   koşmaz. Tür bazında idempotent bir adım yazılmasaydı kesinti bildirimi **yalnız eski
   kurulumlarda** patlardı; geliştiricinin taze veritabanında hiç görünmezdi.
3. 🐛 **Test kendi kullanıcılarını başka bir testin mahallesine yazdı.** İlk yazımda sözlüğün ilk
   iki mahallesi (`OrderBy(Name).Take(2)`) ödünç alınmıştı — `PanelPushCampaignTests` de aynı iki
   satırı kullanıyor ve iki test kullanıcısı **onun alıcı sayımına karıştı** ("2 bekleniyordu, 3
   bulundu"). Paylaşılan veritabanında **sayı iddia eden her test kendi kitlesini kurmalı**;
   ödünç alınan lookup satırı bir sonraki fazın testini kırar.
4. 📌 **Mahalle eki kırpması olmadan geri doldurma sessizce sıfır sonuç verirdi.** Sözlükte ad
   `"Cengiz Topel"`, kesinti kaydında yıllardır `"Cengiz Topel Mahallesi"`. Kırpma slug'ın
   *üstüne* yazıldı (girdisi ASCII slug, çıktısı slug) — Türkçe karakter kararı hâlâ tek yerde
   (`SlugHelper`, madde 21). ⚠️ `"Yenimahalle"` içindeki "mahalle" **ek değil**: ek yalnız
   ayraçtan sonra gelirse ektir, aksi hâlde geriye "yeni" kalır ve mahalle hiç eşleşmez.

➕ **PLAN DIŞI (raporlandı):**
- **`AreaDetail` mahalle adından ayrıldı** — plandaydı ama sebebi uygulamada netleşti: sözlük
  eşleşmesini imkânsız kılan asıl şey, sokak bilgisinin mahalle metnine sıkıştırılmasıydı.
- **Bildirim ek mahallelere genişletilebiliyor** (`TargetNeighborhoodIds`): bir trafo arızası
  komşu mahalleyi de karartabilir. Kesintinin kendi mahallesi **her zaman** dâhil.
- **Panel Index'e "Bildirim" sütunu + Bildirim Gönderimleri bağlantısı** — 12.2b'nin panosuna
  kesinti ekranından köprü.
- **Create/Update artık `IAuditableCommand`** (eskiden yalnız Delete izliyordu).
- **Bitiş ≤ başlangıç doğrulaması** (eskiden hiç yoktu — ters saatli kesinti kaydedilebiliyordu).
- **Silme onayı artık neyi sildiğini söylüyor** ("duyurusu ve gönderilen bildirimleri de kaldırılacak").

🐛 **12.2'DEN DEVRALINAN MOBİL ÇÖKMENİN KÖK NEDENİ BULUNDU VE KİLİTLENDİ.** İki oturumdur
"yeniden üretilemedi" diye açık duran `Navigator._debugCheckDuplicatedPageKeys` artık
**deterministik olarak üretilebiliyor**:
`go_router` imperative sayfalara **rastgele** (`_getUniqueValueKey`, 32 karakter), kabuk
(`StatefulShellRoute`) sayfalarına ise **`route.hashCode`** anahtarı verir — yani kabuk anahtarı
**deterministik**. `RouteMatchList._createNewMatchUntilIncompatible` bir kabuk rotasını yığındaki
kabukla **yalnız kabuk en üstteyse** birleştirir; araya kabuk dışı bir sayfa girmişse birleştirmez
ve listeye **aynı anahtarla ikinci bir `ShellRouteMatch`** ekler:
`[ShellRouteMatch=114994750, ImperativeRouteMatch=…, ShellRouteMatch=114994750]` → Navigator patlar.
🔑 **Gerçek hayattaki tetikleyici tam da 12.3 ile yakınlaştı:** kesinti bildirimi artık
kendiliğinden gidiyor ve `PushCoordinator.openNotification` hedefe **`push`** ediyordu — hedef
`/ilanlar/:id` gibi bir sekme **alt** rotasıysa ve kullanıcı o an bir modül ekranındaysa uygulama
dokunur dokunmaz çökerdi. Düzeltme tek sahipli: **`lib/core/router/app_nav.dart`** (kabuk rotası,
kabuk en üstte değilken `go` edilir; karar **router'a sorulur**, elle rota listesi tutulmaz).
📌 `module_grid`'de elle yazılmış `AppRoutes.tabs` kontrolü **doğru sezgiye sahipti ama yalnız
sekme köklerini tanıyordu** — alt rotalar kapsam dışıydı; o da `AppNav`'a çekildi.
Regresyon testi `test/core/navigation/shell_page_key_test.dart` (5 test) ve **çakışma kare
basılmadan ölçülüyor**: assertion'ın gerçekten atılmasına izin vermek widget ağacını bozuk
bırakıyor ve artık istisnalar aynı binding'i paylaşan sonraki testlere sızıyor.

🔴 **GÖRÜNMEZ SÖZLEŞMELERE #40, #41, #42 EKLENDİ** (mahalle adı sözlükten türetilir · kesinti
bildirimi bir duyurudur ve kesintiyle birlikte silinir, güncelleme ikinci duyuru üretmez ·
bildirim yalnız FK'sı dolu kesintide gönderilebilir). Toplam **42**.
§7 kod-dışı sözleşmelere ayrıca **kabuk rotası `push` edilmez** maddesi eklendi.

**Doğrulama:** `dotnet test` **729/729** · `flutter analyze` **0** · `flutter test` **696/696**.
**Kuralı bilerek boz:** 7 deneme (türetilmiş ad · FK kapısı · silmede bildirim temizliği ·
`tracking: true` · `SlugHelper` normalleştirmesi · mahalle eki kırpması · mobil kimlik önceliği ·
`AppNav`) → **hepsi kırmızı**, geri alınınca yeşil.
**Canlı (Chrome + gerçek Android emülatörü):** geri doldurma açılışta **2/2 eşleştirdi** ·
panelde mahalle seçildi → **tahmini alıcı 5**, gönderim de **5 satır** (önizleme ↔ gerçek paritesi) ·
`push_campaigns.source = power_outage`, **tamamlandı** · bildirim yalnız **Cengiz Topel**'in 5
sakinine yazıldı, Şehit Kansu'daki kullanıcıya **YOK** · duyuru gövdesi **TR yerel saatle**
"7 Ağustos 22:00 – 8 Ağustos 02:00" (UTC 19:00 kaydı; gün aşımında bitiş **tarihi** de yazıldı) ·
`visible_until` = kesinti bitişi · **emülatörde push düştü** (`aysedmr`), ön plan bildirimi +
rozet · kesinti kartında **"Mahalleniz"** rozeti ve bölge ayrıntısı · "Sadece Cengiz Topel"
süzgeci → "2 kesinti mahalle filtresi yüzünden gizli" · **kabuk dışı bir modül ekranındayken
bildirime dokunuldu → duyuru detayı açıldı, ÇÖKME YOK** (tür rozeti "Elektrik Kesintisi",
"8 Ağustos 2026, 02:00 tarihine kadar geçerli").

---

### 12.4 — Etkinlik konumu: il / ilçe — [x] ✅ TAMAMLANDI (9 Ağustos 2026)

**Hedef:** "Bu etkinlik nerede?" sorusunun listede cevaplanması ve çevre il/ilçe etkinliklerinin
görünür olması.

**Bugünkü durum:** `Event.City` entity'de **var**, panelde formu **yok**, DTO'da **yok** →
kolon her kayıtta `null`. `Event.IsLocal` DTO'da var, panel set etmiyor (hepsi `false`),
mobil parse ediyor ama **hiçbir widget kullanmıyor**. Yani konum modeli yarım ve yarısı ölü kod.

#### Backend

- **Yeni sözlük `District : BaseEntity`** (`districts`): `Name` · `Slug` · `ProvinceName` ·
  `IsCenter bool` · `IsActive` · `DisplayOrder`. Seed (idempotent): **Osmaniye'nin 7 ilçesi**
  (Merkez, Kadirli, Düziçi, Bahçe, Hasanbeyli, Sumbas, Toprakkale) + **çevre il merkezleri**
  (Adana, Hatay, Kahramanmaraş, Gaziantep). Panelden yönetilebilir (`LookupsAdmin`'e sekme).
- **`Event.DistrictId Guid?`** FK.
- **`Event.IsLocal` türetilir:** yazma anında `DistrictId == Kadirli` hesaplanır. Kolon **kalır**
  (DTO'da var, mobil okuyor — silmek kırıcı olurdu, §5).
- **`Event.City` kolonu düşürülmez ama artık okunmaz/yazılmaz** (§6: tablo/kolon düşürmüyoruz).
  Bu dosyada ölü olduğu **yazılı** — yoksa bir sonraki oturum onu "gerçek" sanır.
- **Geri doldurma:** mevcut tüm etkinlikler `DistrictId = Kadirli`, `IsLocal = true`.
  Bu **doğru bir varsayım**: panelde başka seçenek hiç olmadı.
- DTO additive: `districtId` · `districtName` · `provinceName` · **`locationLabel`**.
  🔴 Etiket **sunucuda tek yerde** üretilir ("Kadirli" · "Osmaniye / Merkez" · "Adana") —
  istemcide üretilirse panel ile mobil aynı etkinliği farklı yazar ve kimse hata almaz (#23 sınıfı).
- Sorgu: `districtId` filtresi + `onlyLocal bool?`. Bilinmeyen değer **varsayılana düşer**
  (§5: istemci hatası listeyi bozmaz). **Varsayılan sıralama değişmez** (#30).

#### Panel

- Etkinlik formuna ilçe dropdown'ı (il başlıklarına göre gruplu, `<optgroup>`).
- Index'e ilçe filtresi + sütun + CSV kolonu (`PanelCsv`).
- `PanelDisplay`'e `DistrictLabel()`.

#### Mobil

- `Event` modeline `locationLabel`/`districtName`/`provinceName`; `EventCard`'a **konum rozeti**.
- Filtre şeridine "Kadirli · Osmaniye · Çevre iller".
- 🔴 **Golden zorunlu, uzun Türkçe metinle ve 1.4 ölçekte.** `Row` içine yeni bir `Text`
  girmesi bu projede **7+ kez** `RenderFlex overflow` üretti (`EventCard` dâhil) →
  `Flexible`/`Expanded` + ellipsis.
- Erişilebilirlik: rozet ekran okuyucuda anlamlı etiket taşımalı.

**Bitti kriteri:** panelden "Osmaniye / Merkez" etkinliği eklendi → `GET /v1/events` `locationLabel`
dolu → **emülatörde kartta göründü** · "Kadirli" filtresi çevre ilçe etkinliğini eliyor ·
eski etkinlikler `IsLocal=true` · golden'lar güncellendi ve **PNG farkı gözle incelendi**.

#### 12.4 kapanış notları

**Teslim edilenler:** `District` varlığı + `DistrictConfiguration` + migration `AddEventDistricts` ·
`Event.DistrictId` FK (`SetNull`) + `ix_events_district` · `DbSeeder.EnsureDistrictsAsync`
(11 satır, **satır bazında** idempotent) · `EventDistrictBackfill` ·
`Features/Lookups/DistrictLocation.cs` (`DistrictDefaults` + `DistrictLabel`) ·
`Features/Events/{EventDistrictResolver,EventLocationScope,EventProjection}` ·
DTO'ya 4 alan + sorguya 3 süzgeç · `GetDistrictsAdminQuery` + `Create/UpdateDistrictCommand` ·
`Views/Shared/_DistrictSelect.cshtml` + `DistrictSelectViewModel` · `PanelDisplay.DistrictLabel()` ·
`EventsAdmin` form/filtre/sütun/CSV · `LookupsAdmin`'e "İl / İlçeler" bölümü ·
mobil `event.dart` + `events_providers.dart` (`EventPlace`) + `events_repository.dart` +
`_PlaceFilter` + `EventCard` konum rozeti + detay "İlçe" satırı ·
3 yeni test dosyası. **Backend 729 → 784 (+55), mobil 696 → 703 (+7), analyze 0.**

🔑 **TESLİM EDİLEN:** `Event.City` panelde formu **hiç olmadığı** için her kayıtta `null`'dı ve
`Event.IsLocal` panel tarafından hiç yazılmadığı için her kayıtta `false`'tu — mobil onu ayrıştırıp
**hiçbir yerde kullanmıyordu**. Yani konum modeli yarımdı ve yarısı ölü koddu. Artık etkinlik
sözlükteki bir ilçeye bağlı, `IsLocal` o bağdan **türetiliyor** ve vatandaş "bu etkinlik nerede?"
sorusunun cevabını listede görüyor. `City` kolonu **düşürülmedi** (§6) ama entity'de **ölü olduğu
yazılı** — yoksa bir sonraki oturum onu "gerçek" sanardı.

🔴 **EN ÖNEMLİ KARAR — "çevre iller" bir SUNUCU tanımıdır.** Mobil `?locationScope=nearby` diyor,
kümeyi kendisi hesaplamıyor. İstemcide "Osmaniye dışı" diye hesaplansaydı sözlüğe **yarın eklenen
bir Osmaniye ilçesini** mağazadaki eski sürümler çevre il sayardı — liste yanlış, hata yok
(görünmez sözleşme #23'ün sınıfı). Aynı sebeple `locationLabel` de sunucuda üretiliyor: panel ile
mobilin aynı etkinliği farklı yazması hiçbir yerde görünmezdi.

🔴 **İKİNCİ KARAR — ev ilçesinin çıpası bir DB bayrağı DEĞİL, kod sabiti** (`DistrictDefaults.HomeSlug`).
Sözlükte bir `IsHome` bayrağı olsaydı panelden yanlışlıkla başka bir ilçeye taşınabilir ve o andan
sonra yazılan **her** etkinlik sessizce "yerel değil" olurdu. Sabit olduğu için de tersi gerekti:
ev ilçesi satırı panelden **yeniden adlandırılamıyor ve pasifleştirilemiyor** (komut reddediyor,
form da sebebini yazıyor).

🐛 **UYGULAMA SIRASINDA BULUNAN/ÖĞRENİLEN BEŞ ŞEY:**
1. **Slug yalnız ilçe adından üretilemez.** İlk tasarımda `Slugify(Name)` vardı; **her ilin bir
   "Merkez"i** olduğu için ikinci il merkezi benzersiz indekse takılır ve sözlüğe **hiç eklenemezdi**.
   Slug il+ilçeden türetiliyor (`osmaniye-merkez`, `adana-merkez`) ve benzersizlik onun üzerinden.
2. **Liste ve detay iki ayrı `Select` bloğuydu** — dört yeni alan yalnız birine eklenseydi
   **detay ekranı sessizce konumsuz kalırdı** ve ne derleyici ne mevcut test yakalardı. Projeksiyon
   `EventProjection`'a çekildi. ⚠️ Etiket EF'e çevrilemediği için ifade ağacı ham alanları döndürüyor,
   hesap bellekte tek bir `Finish` adımında yapılıyor.
3. 🐛 **Süzgeç şeridinin bağlantıları `asp-route-*` ile ELLE sayılmış ve `sort` unutulmuştu** —
   panelin canlı denetiminde bulundu. Başlığa göre sıralanmış bir listede "Çevre iller"e tıklamak
   sıralamayı **sessizce varsayılana döndürüyordu**: hiçbir test kırılmaz, hiçbir log düşmez,
   liste yalnız "bir şekilde" yeniden sıralanır. İlginç olan, aynı sayfadaki *diğer* bağlantıların
   (sıralama başlığı, CSV butonu, sayfalama, toplu işlem) hepsinin **doğru** çalışmasıydı — çünkü
   üçü de sorgu dizesini **jenerik** taşıyor. Yani hata "yeni filtre eklendi, taşınması unutuldu"
   değil, tam tersi: **elle sayan tek bileşen benimkiydi.** `_ExportCsvButton`'ın 11.16b'den beri
   yazılı olan uyarısı (*"elle sayılsaydı unutulurdu"*) birebir gerçekleşti. Kural artık
   `Common/PanelQuery.With` içinde; regresyon `PanelEventDistrictTests.LocationChips_…` (2 test).
   📌 Not: aynı kuralın hâlâ **üç kopyası** var (`_Pagination`, `_SortableHeader`, `_ExportCsvButton`) —
   üçü de testli ve çalışıyor, buraya çekilmeleri ayrı bir temizlik adımı.
4. 🐛 **`AdCard`'ın golden referansı kendiliğinden çürüyordu** — `--update-goldens` çalıştırıldığında
   `ad_card_{light,dark}.png` de değişti, oysa `AdCard`'a dokunulmamıştı. Sebep: kart
   `AppDate.relative(ad.createdAt)` çağırıyor ve `now` **iletmiyordu**; golden fixture'ı sabit bir
   `now`'dan "2 gün önce" üretiyor ama kart **gerçek saate** bakıyordu → aylar geçince aynı fixture
   "1 Ağustos 2026" basmaya başladı. Yani referans, kodda hiçbir şey değişmeden zamanla kırmızıya
   dönecekti. Bu checklist §5'teki **"⚠️ TEKRARLAYAN (4 kez)"** maddesinin ta kendisi:
   `AnnouncementTile`, `ComplaintCard` ve `NotificationTile` aynı sebeple düzeltilmişti, **`AdCard`
   atlanmıştı**. Düzeltme: `AdCard`'a `now` parametresi + golden senaryolarında sabitleme;
   **PNG geri alındı** (drift referansa yazılmadı) ve orijinal referans yeniden yeşile döndü —
   yani sapma gerçek bir regresyon değil, tam olarak çürümeydi.
   🔑 **Ders:** `--update-goldens` sonrası **hangi dosyaların değiştiğine** bakmak, PNG'ye bakmak
   kadar önemli: dokunmadığın bir kartın referansı değiştiyse ya gerçek bir regresyon vardır ya da
   o referans zamana bağlıdır. İkisi de sessizdir.
5. **Geri doldurma bir eşleştirme değil düz bir varsayım** ("hepsi Kadirli'dir") ve bu yüzden
   12.3'ün geri doldurmasından **farklı bir riski** var: her açılışta koştuğu için, ilçesiz yeni
   bir kayıt doğabilseydi onu da ezerdi. Varsayımı iki kapı ayakta tutuyor — alan **zorunlu**
   (komut reddediyor) ve sözlükte **silme yok** (FK'nin `SetNull`'ı tetiklenmiyor). ⚠️ Tarama
   `IgnoreQueryFilters` ile: çöp kutusundan geri gelen etkinlik aksi hâlde ilçesiz dönerdi.

➕ **PLAN DIŞI:** `Common/PanelQuery.cs` + `AdCard.now` (ikisi de yukarıdaki bulguların çözümü) ·
`?locationScope` ekseni (plan yalnız `onlyLocal` diyordu — bir `bool` "çevre iller"i
ifade edemez; `onlyLocal` **korundu** ama aynı enum'a çevrilen bir kısayol olarak, yani "yerel"in
tanımı tek yerde) · **`EventProjection`** (liste/detay projeksiyon birleştirmesi) ·
Create/Update artık **`IAuditableCommand`** (etkinlik oluşturma/düzenleme ize hiç düşmüyordu) ·
panel Index'te **konum kapsamı şeridi** + arama ile ilçe süzgecinin **tek forma** alınması
(ayrı formlar birbirini sıfırlıyordu) · detay ekranında **"İlçe" satırı** ·
**`mapQuery`'ye ilçe/il eklendi** (koordinatsız bir Adana etkinliğinde "Kültür Merkezi" araması
kullanıcıyı **Kadirli'ye** götürüyordu) · paylaşım metnine konum · pasif ilçenin
**seçili kayıtta listede kalması** (düşseydi form kaydedildiğinde konum sessizce değişirdi).

#### Yeni görünmez sözleşmeler

**#43** `locationLabel` sunucuda tek yerde + liste/detay aynı projeksiyon ·
**#44** `Event.IsLocal` türetilmiştir, çıpası kod sabiti ·
**#45** ilçe zorunlu + sözlükte silme yok = `district_id IS NULL` yalnız "12.4 öncesi" demek.
(`ARCHITECTURE.md` §7; testler `PanelEventDistrictTests` + `Unit/Application/Events/`.)

**Doğrulama:** `dotnet test` **784/784** · `flutter analyze` **0** · `flutter test` **703/703**.
**Kuralı bilerek boz:** 4 deneme (form `IsLocal`'ına güven · detay kendi projeksiyonunu yazsın ·
ilçe zorunlu olmasın · bilinmeyen kapsam varsayılana düşmesin) → **hepsi kırmızı**, geri alınınca yeşil.
⚠️ **Chrome eklentisi bu oturumda bağlanamadı** (`list_connected_browsers` boş döndü); panel canlı
denetimi tarayıcı yerine **oturum açmış curl + DOM ayrıştırma** ile yapıldı — `sort` kaybı bulgusu
tam olarak o denetimden çıktı.
**Canlı (panel + gerçek Android emülatörü):** geri doldurma **3/3** (açılış logunda) ·
panelden "Osmaniye / Merkez" etkinliği eklendi, form `IsLocal=true` gönderdi ama kayıt
**`is_local=false`** düştü (türetme kazandı) · `GET /v1/events` `locationLabel="Osmaniye / Merkez"` ·
ilçesiz gönderim **reddedildi** ("İlçe seçilmelidir", 0 kayıt) · ev ilçesini yeniden adlandırma
denemesi **yok sayıldı** (slug ve ad korundu) · form ilçeleri **5 `<optgroup>`** ile grupladı ve
Kadirli **önceden seçili** geldi · panel Index'te Konum sütunu + Türkçe şerit, ham `nearby` **yok** ·
CSV'de `Konum;` sütunu ve "Osmaniye / Merkez" · **emülatörde** kartta konum rozeti (Kadirli
**vurgulu**, Adana/Osmaniye sönük), "Çevre iller" → yalnız Adana, "Kadirli" → çevre ilçeleri eledi,
detayda **"İlçe: Kadirli"** satırı, **çökme yok** · golden'lar yenilendi ve **PNG'ler gözle incelendi**
(1.4 ölçekte taşma yok, uzun ilçe adı ellipsis'e düşüyor).

---

### 12.5 — Ulaşım alan modeli: araç tipi · kalkış noktası · sefer günleri — [x] ✅ TAMAMLANDI (9 Ağustos 2026)

**Hedef:** Otobüs/minibüs ayrımı, kalkış noktası bilgisi ve **her gün olmayan seferler**.

**Bugünkü durum:** `IntercityRoute` yalnız `Destination`/`Price`/`DurationMinutes`/`Company`
taşıyor; `IntercitySchedule` yalnız `DepartureTime`. Yani araç tipi, kalkış noktası ve gün
bilgisi **hiç yok** — her sefer *her gün* varsayılıyor.

#### Backend

- **`TransportVehicleType`** enum (`Bus`, `Minibus`), DB'de **string** (`bus`/`minibus`) olarak
  saklanır (DTO değeri sabit kalsın diye). `IntercityRoute.VehicleType` — mevcut satırlar için
  varsayılan `bus`, panel bunları "gözden geçirilmeli" olarak işaretler.
- **Yeni sözlük `TransportDeparturePoint : BaseEntity`**: `Name` ("Kadirli Otogar",
  "Minibüs Garajı") · `Address` · `Latitude`/`Longitude` · `IsActive` · `DisplayOrder`.
  Koordinat **isteğe bağlı değil, amaç bu**: mobilde "yol tarifi" butonu buradan beslenir.
  `IntercityRoute.DeparturePointId Guid?`.
- **`IntercitySchedule.OperatingDays int` — 7 bitlik maske**, Pazartesi=1 … Pazar=64.
  🔴 **Mevcut tüm satırlar 127 (her gün) ile göç eder → davranış değişmez.**
- 🔴 **Gün dönüşümünün tek sahibi `OperatingDays` değer nesnesi olur.** .NET `DayOfWeek`
  **Pazar=0**'dan başlar, bizim maske **Pazartesi**'den — bu klasik bir sessiz kayma hatası
  ve iki yerde ayrı yazılırsa "Salı seferi Pazartesi görünür". Tek dönüştürücü + birim testi.
- 🔴 **`OperatingDays = 0` yasak** (doğrulama): hiçbir gün çalışmayan sefer, panelde duran ama
  mobilde **hiç görünmeyen** bir kayıttır — kimse hata almaz.
- DTO additive:
  - hat → `vehicleType` · `departurePointName` · `departurePointLatitude`/`Longitude`;
  - sefer → `days: ["mon","tue",…]` + `runsDaily: bool` (`ScheduleDto`'ya opsiyonel parametre).
- Sorgu: `vehicleType` filtresi. Arama parametresi **`searchTerm` kalır** (görünmez sözleşme #4 —
  ulaşım ve taksi bu adı kullanır, `search` yazılırsa **sessizce yok sayılır**).
- ⚠️ Görünmez sözleşme #7 korunur: şehirlerarası saat biçimi **`"07:00"`**, şehir içi
  **`"06:30:00"`**. Yeni alanlar bunu değiştirmez.
- 🔴 **Uyumluluk kararı — yazılı olsun:** uç **tüm seferleri** döndürmeye devam eder (günleri
  ayrı alanda bildirir), seferleri günlere göre **sunucuda elemez**. Mağazadaki eski sürümler
  günleri bilmediği için Pazar günü de tüm saatleri gösterir — bu bugünkü doğruluk seviyesinin
  aynısıdır, **regresyon değildir**; sunucuda elense eski sürümler için liste sebepsiz boşalırdı.

#### Panel

- `Intercity` ekranına araç tipi filtresi/sekmesi + kalkış noktası dropdown'ı.
- Sefer satırında **7 gün onay kutusu** + "Her gün" kısayolu.
- `PanelDisplay`'e `VehicleType()` ve `OperatingDays()` Türkçe karşılıkları
  ("Otobüs"/"Minibüs", "Her gün"/"Hafta içi"/"Pzt·Çar·Cum") — **ham basılmaz** (kural #6).
- `TransportDeparturePoint` yönetimi `LookupsAdmin` altında.

**Bitti kriteri:** panelden minibüs hattı + "Minibüs Garajı" + yalnız hafta içi sefer eklendi →
`GET /v1/transport/intercity-routes` `vehicleType`, `departurePointName`, `days` doğru ·
mevcut hatlar `bus` + `runsDaily:true` (davranış değişmedi) · `OperatingDays=0` **reddedildi** ·
gün dönüştürücüsü Pazar sınırında doğru.

#### 12.5 kapanış notları

**Teslim edilenler:** `TransportVehicleType` enum + `TransportVehicleTypes` (metin dönüşümünün
tek sahibi) · **`OperatingDays` değer nesnesi** (7 bitlik maske, Pazar=0 kaymasının tek çözüm
yeri) · `TransportDeparturePoint` varlığı + `DbSeeder.EnsureDeparturePointsAsync` ·
`IntercityRoute.{VehicleType,DeparturePointId}` + `IntercitySchedule.OperatingDays` +
migration `AddTransportFieldModel` · `IntercityRouteProjection` (liste+detay tek sahip) ·
`TransportDeparturePointResolver` · `UpdateIntercityScheduleCommand` (yeni) ·
kalkış noktası lookup CRUD'u · panelde araç şeridi + kalkış noktası açılır listesi +
**7 gün onay kutusu** (`_OperatingDaysPicker`) + `PanelDisplay.{VehicleType,OperatingDaysLabel}` ·
2 yeni test dosyası. **Backend 784 → 843 (+59), mobil 703 (değişmedi — 12.6'nın işi), analyze 0.**

🔑 **TESLİM EDİLEN:** `IntercityRoute` 10.8'den beri **her seferi her gün** varsayıyordu ve
araç tipi/kalkış noktası **hiç yoktu**. Kadirli'de "Adana minibüsü" ile "Adana otobüsü" ayrı
işlerdir — farklı yerden kalkar, farklı sıklıkta gider — ve bu ayrım uygulamada **hiçbir yerde
temsil edilmiyordu**. Artık hat bir araç tipine ve sözlükteki bir kalkış noktasına bağlı,
sefer de **hangi günler çalıştığını** söylüyor.

🔴 **EN ÖNEMLİ KARAR — "elemek" ile "bildirmek" farkı:** uç seferleri günlere göre **elemiyor**,
yalnız `days`/`runsDaily` ile bildiriyor. Elenseydi mağazadaki eski sürümler (`days`'i tanımıyorlar)
için liste **sebepsiz boşalırdı**; bugünkü davranış "her sefer her gün" olduğu için elememek
regresyon değil **uyumluluğun kendisidir**. Aynı sebeple migration mevcut satırlara `127` +
`bus` yazdı: **davranış birebir korundu** (canlıda doğrulandı).
**İkinci karar:** araç tipi DB'de **metin** (`bus`/`minibus`), enum sırası değil — araya üçüncü
bir tip girseydi sayısal kolon bütün kayıtları sessizce kaydırırdı.
**Üçüncü karar:** kalkış noktası **zorunlu değil** ve **geri doldurma yok**. 12.4'te ilçe
zorunluydu çünkü geri doldurmanın varsayımı ona dayanıyordu; burada 12.5 öncesi hatların kalkış
noktası *gerçekten bilinmiyor* ve "hepsi otogardan kalkar" tahmini vatandaşı **yanlış yere**
götürürdü. Panel o boşluğu **uyarı** olarak gösteriyor, doldurmuyor.

🔴 **BULUNAN/ÖĞRENİLEN ÜÇ ŞEY:**
1. **Ulaşımda da liste ve detay iki ayrı `Select` bloğuydu** — 12.4'te etkinlikte bulunan hatanın
   birebir aynısı, yani `EventProjection` dersinin **uygulanmamış ikinci örneği**. Beş yeni alan
   yalnız birine eklenseydi panelin hat düzenleme ekranı **sessizce araç tipsiz** kalırdı.
   → `IntercityRouteProjection`. 📌 Aynı sınıf hatanın başka modüllerde de durup durmadığı
   **taranmadı** — ayrı bir denetim adımı.
2. **Sefer yalnız "ekle + sil" ile yönetiliyordu.** Gün maskesi eklendiği anda bu kabul edilemez
   hâle geldi: "Pazar seferini kaldır" demek için yöneticinin saati silip yeniden yazması
   gerekirdi ve denetim izinde bu bir **silme** olarak görünürdü.
   → `UpdateIntercityScheduleCommand` (plan dışı, zorunlu).
3. **`OperatingDays` bir `struct` olduğu için lambda içinde `this` kullanılamıyor** (CS1673) —
   `ToDayOfWeeks()` ilk yazımda derlenmedi. Küçük ama değer nesnesi yazarken tekrar çıkacak
   bir tuzak: maskeyi yerel değişkene kopyala.

🐛 **CANLI CHROME DENETİMİNDE BULUNAN İKİ HATA (ikisi de düzeltildi):**
1. **Gün seçicisinde panel yalan söylüyordu.** "Hafta içi" kısayoluna basıldığında **yedi rozetin
   hepsi seçili görünüyordu**, oysa veride yalnız Pzt–Cum vardı. Sebep Tailwind'in
   `peer-checked:` kuralının **genel kardeş seçicisi** (`~`) üretmesi: yedi kutu düz kardeş
   olduğu için ilk kutu (Pzt) işaretlendiği anda kendisinden **sonraki bütün** etiketler seçili
   stilini alıyordu. Yönetici Cumartesi/Pazar'ı da dâhil sanırdı; POST edilen veri doğru olduğu
   için **hiçbir entegrasyon testi bunu yakalayamazdı** (testler gövdeye bakar, CSS'e değil).
   İlginç olan: "Hafta sonu" kısayolu **tesadüfen doğru** görünüyordu (seçili günler listenin
   sonundaydı) — yani hata yalnız bazı seçimlerde ortaya çıkıyordu. Her gün kendi
   sarmalayıcısına alındı; regresyon **yapısal** testle kilitlendi
   (`DayPicker_WrapsEachCheckboxSoPeerStylingCannotLeak`).
2. 🔴 **12.4'ün kodunda: ilçesi pasifleştirilen etkinlik HİÇ DÜZENLENEMİYORDU.** Yönetici formu
   açıp yalnız başlıktaki bir yazım hatasını düzeltmek istese bile
   *"Seçilen ilçe bulunamadı veya pasif durumda"* alıyordu — üstelik **hiç dokunmadığı bir alan**
   için. Kaydedebilmesinin tek yolu etkinliği **başka bir ilçeye taşımak**tı: başlığı düzeltmek
   için konumu değiştirmek. Tek tek **doğru** olan iki kural çarpışıyordu: form pasif ilçeyi
   *seçili tutuyor* (12.4'ün bilinçli kararı — konum sessizce değişmesin) ve resolver pasif
   ilçeyi *reddediyordu* (emekli ilçe yeniden seçilmesin). Kural artık **"pasif değer YENİ OLARAK
   seçilemez"**; kayıtta zaten duran değer korunur. ⚠️ **Aynı şekilli kod 12.5'te kendi
   yazdığım `TransportDeparturePointResolver`'da da vardı** — iki resolver aynı anda düzeltildi,
   iki regresyon testi eklendi. 🔑 **Ders:** bir kuralı yeni bir modüle kopyalarken *kuralın
   kendisi* kadar **çağrıldığı bağlam** da kopyalanıyor; 12.4'ün hatası 12.5'e sessizce miras kalmıştı.

➕ **PLAN DIŞI:** `IntercityRouteProjection` · `UpdateIntercityScheduleCommand` +
panelde **düzenlenebilir sefer satırı** · hat Create/Update komutları artık **`IAuditableCommand`**
(11.17'de atlanmıştı — hattı kimin eklediği denetim izinde **hiç** görünmüyordu) ·
sefer ekleme/güncelleme de denetim izine düşüyor (4 yeni Türkçe eylem etiketi) ·
kalkış noktası sözlüğünde **koordinat + slug benzersizliği** (`SlugHelper`) ·
liste sorgusuna **`ThenBy(Id)`** (görünmez sözleşme #30: `OrderBy(Destination)` tek başınaydı →
eşit adlı hatlarda sayfalı listede **sessiz kayıt kaybı** riski) ·
araç şeridi **`PanelQuery.With`** ile (12.4'ün `sort` kaybı hatası tekrarlanmasın) ·
arama ile araç süzgecinin **tek forma** alınması · panelde "kalkış noktası girilmemiş" ve
"N sefer her gün değil" uyarıları · `_DeparturePointSelect` + `_OperatingDaysPicker` partial'ları
(pasif noktanın seçili kayıtta kalması kuralı **tek yerde**) · admin API'ye
`PUT intercity/{id}` ve `PUT intercity/schedules/{id}`.

🔴 **GÖRÜNMEZ SÖZLEŞMELERE #46, #47, #48 EKLENDİ.** Toplam **48**.

**Doğrulama:** `dotnet test` **843/843** · `flutter analyze` **0** · `flutter test` **703/703**.
**Kuralı bilerek boz:** 4 deneme → **11 test kırmızı** (Pazar=0 kayması · `0` maskesi kapısı ·
sunucuda gün elemesi · süzgeçte bilinmeyen değerin varsayılana düşmesi), geri alınınca yeşil.
🐛 **Bu turda bir test ZAYIF çıktı:** `UnknownVehicleTypeFilter_…` bozma denemesini **yakalamadı**,
çünkü fikstürü yalnız otobüs hattıydı — "bilinmeyen değeri `bus`a düşüren" bozuk gerçekleme de
testi geçiriyordu. Test **iki tipi birden** kapsayacak şekilde güçlendirildi ve bozma tekrar
denenip kırmızıya döndüğü görüldü. 🔑 Ders: *bozma denemesi yalnız kuralı değil, **testin
kendisini** de sınar.*
**Canlı (Chrome eklentisi + gerçek panel + curl):** migration gerçek dev veritabanına uygulandı →
`GET /v1/transport/intercity-routes` mevcut hatlarda `vehicleType:"bus"`, seferlerde
`runsDaily:true` + 7 günün tamamı döndürdü (**davranış değişmedi**) · panelden **minibüs hattı
"Kozan" + "Minibüs Garajı" + yalnız hafta içi 06:30 seferi** eklendi → uçta
`vehicleType:"minibus"`, `departurePointName:"Minibüs Garajı"`, `days:[mon…fri]`,
`runsDaily:false` · `?vehicleType=minibus` yalnız Kozan'ı, **`?vehicleType=otobus` (bilinmeyen)
üç hattın hepsini** döndürdü · araç şeridine tıklamak `?search=…` parametresini **korudu** ·
gün seçilmeden kaydetme **reddedildi**, Türkçe sebebini yazdı ve **kayıt ezilmedi** ·
listede ham `bus`/`minibus` yok, kalkış noktası boş hatta "Girilmemiş" uyarısı var.
⚠️ Panelin `admin` parolası bu makinede bilinmiyordu (`secrets/panel-admin.json` yok) →
oturumu **kullanıcı açtı**, denetim o oturumda yapıldı.

**Canlı — 12.4'ün yapılamamış denetimi (kullanıcı isteğiyle bu oturumda tamamlandı):**
Konum sütununda etiket kuralının **üç dalı** da doğru (Adana · Osmaniye / Merkez · Kadirli) ·
başlığa göre sıralıyken "Çevre iller"e tıklamak `sort=title_asc`'ı **korudu** (12.4'te bulunan
`sort` kaybı gerçekten kapanmış) · CSV butonu `sort` **ve** `LocationScope`'u taşıyor ·
ev ilçesi "Bizim ilçe" rozetiyle işaretli, il/ilçe alanları salt-okunur, Aktif kutusu kapalı ve
**sebebi yazılı** · pasifleştirilen ilçe, o ilçeye bağlı etkinliğin formunda **"(pasif)" olarak
seçili kaldı**. Bu turda yukarıdaki **2 numaralı hata bulundu.**

---

### 12.6 — Ulaşım mobil: ikili kalkış · gün rozetleri · "sıradaki sefer" — [x] ✅ TAMAMLANDI (10 Ağustos 2026)

> **Ne teslim edildi:** 12.5 sunucuya araç tipi, kalkış noktası ve sefer günlerini yazmıştı;
> mobil bunların **hiçbirini okumuyordu**. Yani 12.5, vatandaş açısından hâlâ görünmezdi.
> Artık şehirlerarası liste **Tümü / Otobüs / Minibüs** olarak süzülüyor, kart kalkış noktasını
> ve **"Yol tarifi"** butonunu gösteriyor, kalkış saatlerinin altında **gün rozeti** var ve
> "sıradaki sefer" hesabı **haftanın gününü** dikkate alıyor.
> **Sunucuda tek satır değişmedi** — bu alt-faz tümüyle istemci tarafı.
>
> 🔴 **En önemli karar — "elemek ≠ bildirmek" kuralının istemci tarafı.** Uç seferleri günlere
> göre elemiyor (§7 madde 46); **istemci de elemiyor**. Hafta içi seferi Pazar günü listede
> *duruyor*, yalnız soluklaşıyor, rozeti gününü söylüyor ve "sıradaki sefer" onu atlıyor.
> Süzseydik hafta içi çalışan bir hattın kartı Pazar günü **boş** görünürdü — sunucuda
> kaçındığımız hasarın istemcide tekrarı.
> **İkinci karar:** `days` **boş ya da hiç gelmemişse anlamı "her gün"**. 12.5 öncesi kayıtlarda
> alan yok; "hiçbir gün" saymak onları ekrandan **sessizce silerdi**. Additive bir alanın
> *yokluğu*, o alan eklenmeden önceki davranışı vermek zorunda.
> **Üçüncü karar:** araç şeridi **üç** seçenekli (planda ikiliydi). Yalnız Otobüs/Minibüs
> olsaydı sunucuya yarın eklenecek üçüncü bir tip mağazadaki eski sürümlerde **hiçbir süzgeçte
> görünmezdi**; panelin şeridi de zaten aynı üçlüyü kullanıyor. Süzme **sunucuda** yapılıyor:
> sayfalı listeyi istemcide süzmek `totalCount`'u ("N hat") ve sonsuz kaydırmayı yalancı yapardı.
>
> 🐛 **BİR TEST BOŞLUĞU BULUNDU (bozma denemesi sayesinde).** `_TimePill`'in "geçti" kuralından
> gün kontrolü kaldırıldı ve **hiçbir test kırılmadı** — yani kural **kilitli değildi**.
> Hasar sessiz ve gerçek: Pazar günü bakan vatandaş, o gün **hiç kalkmamış** bir 07:00 seferini
> üstü çizili, yani "kalkmış" olarak görürdü. İki sebep birleşmişti: (a) golden'ın **%0.5 piksel
> toleransı** (anti-aliasing için bilinçli) tek bir üstü çizili hapı yutuyor, (b) semantik etiket
> `isOffDay`'i `isPast`'ten **önce** kontrol ettiği için ekran okuyucu zaten doğruyu söylüyordu.
> Kural artık `Text.style.decoration`'a bakan **davranış** testiyle ve **iki yönlü** kilitli
> (çizilmeyen kadar çizilen de denetleniyor — yoksa "hiç çizme" gerçeklemesi de yeşil kalırdı).
> 🔑 **Ders (12.5'in dersinin doğrulanması):** yeşil kalan bir bozma denemesi "kural sağlam"
> demek değil, **"test o kuralı tutmuyor"** demektir. Ve bu turda **golden'ın sınırı** öğrenildi:
> tolerans düzen hatalarını (binlerce piksel) tutar, **tek öğelik stil kararlarını tutmaz**.
>
> 🐛 **CANLI EMÜLATÖR DENETİMİNDE BULUNAN HATA (düzeltildi):** giriş cümlesi `daysAhead`'e
> bakıyordu, oysa **hattın bugün çalışıp çalışmadığına** bakmalıydı. Hafta sonu çalışan bir hat
> Pazartesi *"Bugünkü seferler bitti · Cmt 06:30"* diyordu — o gün **hiç olmamış** bir sefer
> dizisini ima ediyor. Artık "Bugün sefer yok · Cmt 06:30". Küçük bir metin farkı ama vatandaşın
> kafasındaki modeli kuran şey bu; regresyon iki testle kilitlendi.
>
> ➕ **PLAN DIŞI:** `operating_days.dart` (**mobilde gün ↔ bit dönüşümünün tek sahibi** — plan
> yalnız `departure_times.next`'in genişlemesini istiyordu, ama ikinci bir eşleme yazmamak için
> değer nesnesi şart) · `transport_vehicle.dart` + **"Tümü"** seçeneği · `IntercityFilter`
> (arama + araç tipi **tek nesnede**) · araç tipine göre **farklı kart ikonu** · tanınmayan araç
> tipinde rozetin **hiç çizilmemesi** · süzgeç yüzünden boşalan listenin **sebebini söylemesi** ·
> "Aramayı temizle" ile "Filtreleri temizle" ayrımı (araç şeridine dokunulmamışken "filtreler"
> çoğulu yalan söyler) · `departureMapQuery`'nin **Kadirli ile sınırlanması** (12.4'te etkinlikte
> yaşanan "başka şehre götürme" hatası) · paylaşım metnine araç tipi + kalkış noktası + gün bilgisi.
>
> 🔴 **GÖRÜNMEZ SÖZLEŞMELERE #49, #50 EKLENDİ.** Toplam **50**. İkisi de **istemci tarafı** —
> §7 tablosundaki ilk mobil maddeler; karşılıkları `mobile/test/features/transport/` altında.
>
> **Doğrulama:** `flutter analyze` **0** · `flutter test` **751/751** (703 → **+48**) ·
> `dotnet test` **843/843** (backend'e dokunulmadı, doküman testleri yeşil).
> **Kuralı bilerek boz:** 5 deneme → 4'ü kırmızı (12, 3, 1, 3 test), **1'i yeşil kaldı ve o
> boşluk kapatıldı**; hepsi geri alınınca yeşil.
> **Golden:** yalnız **iki yeni PNG** doğdu, mevcut referansların **hiçbiri çürümedi**
> (12.4'ün `AdCard` dersi tuttu) — PNG'ler açık/koyu temada **gözle incelendi**, 1.4 ölçekte
> taşma yok.
> **Canlı (gerçek panel + gerçek API + Android emülatörü):** panelden "Kadirli Otogarı"na
> **gerçek koordinat** girildi ve Adana hattına bağlandı · Adana'ya **yalnız hafta sonu 21:00**
> seferi eklendi · emülatörde araç şeridi çalıştı ve **"Toplam 1 hat"** doğru okundu (süzme
> sunucuda) · açılan kartta kalkış noktası + adres + **"Yol tarifi"** göründü ve buton
> **Google Haritalar'ı tam koordinatta açtı** (37.3745, 36.0972 — Kadirli) · gün rozetleri
> "Her gün ×4 + Hafta sonu" olarak çıktı · Kozan minibüsü panelden hafta sonuna alındı ve kart
> **"Bugün sefer yok · Cmt 06:30"** dedi (Pazartesi bakılıyordu) → **fazın asıl bitti kriteri**;
> sonra panelden hafta içine geri alındı. **Çökme yok.**
> ⚠️ Emülatörün **saati değiştirilemedi** (`date: Operation not permitted`) → "hafta sonunda
> bakma" senaryosu saati değil **veriyi** değiştirerek doğrulandı; aynı dalı sınayan asıl kanıt
> `departure_times_test.dart`'taki sabit tarihli testlerdir.
> 📌 **Kalkış noktası koordinatları seed'de hâlâ `null`** ve bu **bilinçli** (12.5: tahmini
> koordinat vatandaşı yanlış yere götürür). Yani "Yol tarifi" varsayılan kurulumda **adres
> aramasına** düşer; koordinatı yönetici panelden girer.

**Hedef:** 12.5'in mobil karşılığı.

- `TransportScreen` şehirlerarası sekmesi **Otobüs / Minibüs** olarak ikiye ayrılır
  (segmented control; sekme değil — üst düzeyde zaten şehirlerarası/şehir içi sekmesi var).
- Kartta kalkış noktası + **"Yol tarifi"** butonu (`AppLinks` + koordinat; koordinat yoksa
  adres metniyle harita araması — `ContactActions` deseni).
- Sefer saatlerinin yanında **gün rozeti** ("Her gün" / "Hafta içi" / "Pzt·Çar·Cum").
- 🔴 **`departure_times.dart` `next()` mantığı günü hesaba katacak** — "bugün 07:00",
  "yarın 06:30", "Pzt 07:00". Saf mantık, mevcut `departure_times_test.dart` genişler.
  Gün hesabı **`AppDate.nowInTurkey`** üzerinden (sabit UTC+3; `DateTime.now().toUtc()`
  fixture'ları bu projede **yalnız geceleri** patladı).
- ⚠️ Ulaşımda **detay rotası yoktur ve olmayacak** (id ucu yok — kart yerinde açılıyor).
  `go_router` iç içe rota tuzağına düşmemek için bu bilinçli.
- Golden: yeni kart, **uzun Türkçe metin + 1.4 ölçek + açık/koyu**.
- Erişilebilirlik: gün rozeti ve segmented control 48 dp dokunma hedefi + anlamlı etiket.

**Bitti kriteri:** emülatörde otobüs/minibüs ayrı listeleniyor · hafta içi seferi **Pazar günü
"yarın/Pzt"** olarak gösteriyor · yol tarifi butonu haritayı açıyor · golden + erişilebilirlik yeşil.

---

### 12.7 — Sosyal giriş: backend — [x] ✅ TAMAMLANDI (13 Ağustos 2026)

> **Teslim edildi.** `POST /v1/auth/social` (Google + Apple) · `user_identities` tablosu ·
> bağla/çöz uçları · `GET /v1/users/me` → `linkedIdentities[]` · panelde **"Bağlı hesaplar"**
> kutusu ve denetim izli kaldırma. **Backend 1114 → 1182 (+68).**
> Görünmez sözleşme **67 → 70** (68 · 69 · 70).
>
> 🔴 **KARAR 1 — plandan bilinçli sapma: Google için `GoogleJsonWebSignature.ValidateAsync`
> KULLANILMADI.** O metot **statik** ve **gerçek Google anahtarlarına bağlı**; yani bu fazın
> *"bir numaralı gerçek zafiyet"* dediği `aud` kuralını **hiçbir testle kilitleyemezdik** —
> tam olarak 10.11'in `FcmPushService` tuzağı (bayrakla kapalı yol = ilk koşusu canlıda olan
> yol). Yerine **tek** bir `JwksSocialTokenVerifier` yazıldı: Google ve Apple'ın ikisi de
> OIDC/RS256, fark yalnız `iss`/`aud`/JWKS adresi — yani **veri**, kod değil. İki ayrı sınıf
> yazmak aynı güvenlik kuralına **iki sahip** vermek olurdu (§7 madde 23/38/55'in sınıfı).
> Planın *niyeti* birebir korundu; değişen yalnız gerçekleme.
>
> 🔴 **KARAR 2 — `aud` kilidi İKİ YÖNLÜ.** *"Yanlış `aud`'lu jeton reddedilir"* iddiası
> **hiçbir jetonu kabul etmeyen** bir gerçeklemede de yeşil kalırdı — yani reddin sebebinin
> gerçekten o kontrol olduğunu kanıtlamazdı. `TheSameToken_IsAccepted_OnceItsAudienceIsOneOfOurs`
> birebir aynı jetonun, yalnız `aud` listesine eklendiğinde **kabul edildiğini** gösteriyor.
> 📌 §7 madde 50'nin dersinin ("çizilmeyen kadar çizilen de denetlenmeli") ve B4'ün
> ("iddiam totoloji mi?") ikinci uygulanışı.
>
> 🔴 **KARAR 3 — jeton türü ayrımı = OTP'nin korunması.** Sosyal kayıt taşıyıcısı
> (`token_type=social_registration`) **telefon TAŞIMAZ** ve telefonlu kayıt jetonunun yerine
> **geçemez**; `register` **iki jetonu birden** ister. Tek jetona indirgenseydi Google hesabı
> olan herkes **OTP'siz** hesap açar ve moderasyonun dayandığı *"her hesabın doğrulanmış bir
> telefonu vardır"* varsayımı sessizce çökerdi (§7 madde 70). 10.2'nin refresh ↔ registration
> ayrımının üçüncü ayağı.
>
> 🔴 **KARAR 4 — hesap silinince kimlik satırları FİZİKSEL silinir.** Plan bunu saymıyordu.
> İki hasar birden: (a) `provider_user_id` + e-posta **kişisel veri** ve 10.8 anonimleştirme
> **sözü veriyor**; (b) `(provider, sub)` benzersiz olduğu için satır kalsaydı o kişi aynı
> Google hesabıyla **bir daha asla** kayıt olamazdı — telefonu yeniden kayda açan silme
> akışının tam tersi ve hata mesajı *"bu hesap başka bir kullanıcıya bağlı"* olurdu:
> doğru ama anlaşılmaz.
>
> 🔴 **İZİN: panel aksiyonu `RemoveIdentity` adlandırıldı → `delete`.** `Unlink…` yazılsaydı
> hiçbir önekle eşleşmez, POST olduğu için sessizce `update`'e düşerdi — §7 madde 19'un
> **`Un…` biçimindeki en sinsi hâli** (Unarchive 12.13'te birebir yaşandı). Bir giriş
> yöntemini kaldırmak *"profil düzenleme"* değil **güvenlik etkili** bir işlemdir.
> 🔑 Bu sefer tuzak **doğuşta** yakalandı: `ActionFor`'a elle satır eklemek **gerekmedi**.
>
> 🐛 **BULUNAN GERÇEK HATA — yapılandırma DI KAYDINDA okunuyordu.** Entegrasyon süiti
> "sağlayıcı kapalı" diye **400** döndürdü: `AddInfrastructure` `builder.Build()`'den önce
> koşuyor, yani kayıt anında okunan bir değeri `ConfigureAppConfiguration` ile **ezmek
> mümkün değil** (`ARCHITECTURE.md` §8'in kendi yazdığı tuzak). Kod doğruydu ama **kendi
> testinden erişilemiyordu** — bayrakla kapalı bir yolun *test edilemez* hâli, hiç test
> edilmemiş yoldan farksızdır. Okuma `AddSingleton(sp => …)` ile çözülme anına ertelendi.
> ➕ Checklist'e satır olarak eklendi.
>
> 🐛 **`EndpointAuthorizationSweepTests` kırmızıya döndü ve HAKLIYDI.** Yeni anonim yazma ucu
> (`POST /v1/auth/social`) listeye **bilinçli** eklendi (gerekçesiyle). Anonim olması akışın
> tanımı gereği zorunlu — oturum açmak için oturum istenemez; bağla/çöz uçları ise
> `[Authorize]`, çünkü **bağlamanın tek meşru yolu** oturum sahibinin kendisidir.
>
> 🐛 **Panel CSS'inde iki sınıf eksikti** (`hover:text-red-800` · `gap-y-2`) → `npm run build`.
> 12.10'un *"beyaz üstüne beyaz"* bulgusunun aynısı; bu sefer **yazmadan önce ölçüldü**.
>
> ✅ **BOZMA TURU KOŞULDU (14 Ağu, aynı iş) — VE BİR DELİK BULDU.** Üç madde de bilerek
> bozuldu: `aud` kapatıldı → 🔴 **4 test** (saf + uç, iki katmanda birden) · kimlik çalma
> kapısı devre dışı → 🔴 1 test · **`token_type` kontrolü silindi → 🟢 YEŞİL KALDI.**
>
> 🐛 **MADDE 70'İN KİLİDİ SAHTEYDİ.** Test doğru davranışı ölçüyordu ama **yanlış sebepten**
> geçiyordu: bugünkü sosyal jetonun `phone` claim'i **zaten yok**, yani tür kontrolü tamamen
> silinse de metot `null` dönüyor. Sözleşme *"türler ayrıdır"* diyordu, test yalnızca
> *"sosyal jetonda telefon yok"*u ölçüyordu. 🔴 Bugün iki bağımsız sebep koruyor ama biri
> **tesadüfi**: sosyal jetona yarın bir `phone` claim'i eklenirse (ör. *"sağlayıcıdan gelen
> telefonu ön dolduralım"*) ayakta kalan **tek** koruma `token_type` olur ve onu silen
> değişikliği **hiçbir test yakalamazdı** → **OTP'siz kayıt**.
> ✅ Kapatıldı: `ASocialTypedToken_IsRejectedAsAPhoneToken_EvenWhenItCarriesAPhoneClaim`
> jetonu **elle** üretiyor (sosyal türde ama telefon taşıyan) + ters yön eklendi; aynı bozma
> tekrarlandı → **kırmızı**. 📌 *"İddiası zayıf test"* sınıfının **altıncı** tekrarı ve
> **ilk kez bozma turu tarafından** yakalanan hâli. 🔑 Ders: *iki bağımsız sebep koruyorsa,
> testin HANGİSİNİ tuttuğunu ölç.*
>
> ➕ **PLAN DIŞI İKİ EK (kullanıcı sözleşmesi: serbest ama raporla):**
> 1. **`GET /v1/users/me` → `linkedIdentities[]`** — plan yalnız POST/DELETE diyordu, ama
>    12.8'in "Bağlı hesaplar" ekranının **durumu okuyacak hiçbir yolu yoktu**: bağla/çöz
>    düğmeleri neyin bağlı olduğunu bilmeden çizilemez ("işlevsiz buton yok"un uç karşılığı).
>    Ayrı bir `GET` uç yerine profile **additive alan** eklendi — ekran profili zaten çekiyor
>    ve ayrı uç ikinci bir görünürlük kuralı doğururdu.
> 2. **Sosyal giriş için ayrı `LoginChannels.Social` + `bad_social_token` sebebi** — plan
>    bunu saymıyordu. `mobile_otp` sayılsaydı panelde **OTP'siz bir giriş OTP girişi gibi**
>    görünür ve *"bu hesaba hangi yoldan girildi?"* sorusunun cevabı sessizce yanlış olurdu.
>    🔑 Asıl kazanç başarısız denemede: yanlış `aud`'lu jeton denemelerinin **birikmesi**,
>    başka bir uygulamanın jetonuyla giriş girişiminin ta kendisidir — kaydedilmeseydi o
>    saldırı **tamamen görünmez** olurdu.
>
> **Doğrulama:** `dotnet test` **1182/1182**. Sosyal doğrulama testleri **ağa çıkmıyor**:
> gerçek RSA anahtarıyla imzalanmış gerçek biçimli jetonlar, sahte bir JWKS üzerinden
> **gerçek doğrulayıcıdan** geçiyor (sahte bir `ISocialTokenVerifier` yazılsaydı bu fazın
> bir numaralı kuralı hiç kilitlenmemiş olurdu).
>
> ⏭️ **Sırada 12.8** (mobil) — 🔴 **Apple Developer aboneliği hâlâ bekliyor.** Google ayağı
> bugün yazılabilir; Apple butonu `Env` bayrağıyla kapalı kalır (backend tarafı zaten
> yapılandırmayla kapalı ve bu **test edilmiş** bir dal: `ADisabledProvider_SaysSo_…`).

#### Özgün plan (referans)

**Karar gereği telefon çıpa olarak kalır.** Sosyal giriş, var olan kullanıcı için **tek buton**;
yeni kullanıcı için **kayıt formunu ön dolduran kısayol** (telefon + OTP yine istenir).

#### Backend

- **Yeni tablo `UserIdentity : BaseEntity`** (`user_identities`): `UserId` · `Provider`
  (`google`|`apple`) · `ProviderUserId` (`sub`) · `Email` · `EmailVerified bool` ·
  `DisplayName` · `LinkedAt` · `LastUsedAt`. **Unique `(Provider, ProviderUserId)`.**
  🔑 `User` tablosuna **hiç dokunulmuyor** → değişiklik tümüyle additive.
- **`POST /v1/auth/social`** `{provider, idToken}`:
  1. 🔴 **Token sunucuda doğrulanır** — istemciye asla güvenilmez. Google:
     `GoogleJsonWebSignature.ValidateAsync` + **`aud` bizim client id'lerimizden biri olmalı**.
     Apple: Apple JWKS ile imza + `iss=https://appleid.apple.com` + `aud`=bundle id + süre.
     ⚠️ **`aud` doğrulanmazsa başka bir uygulamanın token'ıyla hesaba girilebilir** — bu, sosyal
     girişin bir numaralı gerçek zafiyeti; ayrı testle kilitlenir.
  2. Identity varsa → `IsBanned`/`IsActive`/silinmiş kontrolü → mevcut `GenerateTokens` ile JWT.
  3. 🔴 **E-posta eşleşmesiyle OTOMATİK bağlama YAPILMAZ.** `User.Email` panelden elle
     girilebiliyor ve doğrulanmış değil; otomatik bağlamak **hesap ele geçirme** yolu açar.
  4. Identity yoksa → **geçici kayıt token'ı** (Faz 10.2'nin `GenerateTempToken` mekanizması,
     sosyal taşıyıcı için genişletilir; eski imza korunur) → istemci `POST /v1/auth/register`'a
     telefon + OTP + mahalle ile devam eder; kayıt bitince identity bağlanır.
- **`POST /v1/users/me/identities`** (bağla) · **`DELETE /v1/users/me/identities/{provider}`** (çöz).
  Son bağlantı çözülebilir — telefon çıpa olduğu için kullanıcı kilitlenmez. (Telefonsuz
  modelde bu bir tuzak olurdu; kararın somut kazancı budur.)
- `auth` rate-limit politikası bu uca da uygulanır.
- 🔴 **Bayrakla kapalı yol = hiç test edilmemiş yol.** `Auth:Google:ClientIds` /
  `Auth:Apple:*` boşken sağlayıcı kapalı olur; **her sağlayıcıya en az bir birim testi**
  (10.11'de `FcmPushService` tam bu yüzden ilk gerçek anahtarda patladı).
- **`ProductionReadinessGuard`:** sosyal giriş açık ama client id boşsa → uygulama açılmaz.
- **Apple abonelik beklerken:** kod yazılır ve testlenir, sağlayıcı yapılandırmayla **kapalı** durur.
- `secrets/README.md`'ye OAuth client id/secret satırları.

#### Panel

- `UsersAdmin` detayında **"Bağlı hesaplar"** rozetleri + "bağlantıyı kaldır" (audit izi +
  `PanelDisplay.AuditAction` satırı).

#### Yeni görünmez sözleşmeler

- **`aud` doğrulaması zorunludur** — kaldırılırsa başka uygulamanın token'ı geçerli olur.
- **E-posta eşleşmesiyle otomatik hesap bağlama yasaktır** (doğrulanmamış e-posta = hesap ele geçirme).
- **Sosyal giriş telefonu ATLAMAZ** — yeni kullanıcı her hâlükârda OTP'den geçer.

**Bitti kriteri:** sahte ama geçerli imzalı bir Google token'ıyla yeni kullanıcı akışı geçici
token alıyor · **yanlış `aud`'lu token reddediliyor** · ikinci girişte doğrudan JWT · banlı
kullanıcı sosyal girişle de giremiyor · bağla/çöz uçları çalışıyor · sağlayıcı kapalıyken uç
anlamlı hata dönüyor.

---

### 12.8 — Sosyal giriş: mobil — [ ]

- `google_sign_in` + `sign_in_with_apple` paketleri.
- Giriş ekranında iki buton. 🔴 **Apple butonu iOS'ta zorunlu:** App Store, başka bir sosyal
  giriş sunan uygulamada "Sign in with Apple"ı **şart koşuyor** — yoksa **uygulama reddedilir.**
  Apple Developer aboneliği gelene kadar `Env` bayrağıyla kapalı kalır ve **yayın öncesi
  açılması `Progress.md`'de yayın kontrol listesine yazılır.**
- Akış: sağlayıcı token'ı → `POST /v1/auth/social` → ya oturum ya da **ön dolu kayıt ekranı**
  (ad/e-posta/foto hazır, kullanıcı telefon + OTP + mahalle verir).
- ⚠️ `context.push` ile açılan ekran router redirect'inin **üstünde kalır** — kayıt bitince
  `addPostFrameCallback` içinde kapat (bu projede **3 gerçek hata** buradan çıktı).
- Ayarlar → "Bağlı hesaplar" ekranı (bağla/çöz).
- ⚠️ **Yeni platform izni/yeteneği = İKİ dosya** (`AndroidManifest.xml` + `Info.plist`);
  Apple Sign In ayrıca **Xcode capability** ister. `release_config_test.dart` genişletilir.
- ⚠️ OAuth client id'leri `google-services.json`/`GoogleService-Info.plist`'ten ayrı —
  ikisi de commit edilmez.
- Testler: başarı · **kullanıcı iptali** (sessizce geri dön, hata gösterme) · sağlayıcı hatası ·
  ağ hatası · zaten bağlı hesap. Türkçe hata sözlüğüne yeni kodlar (`turkish_ui_test.dart` denetler).
- Golden: giriş ekranının yeni hâli (açık/koyu, 1.4 ölçek).

**Bitti kriteri:** emülatörde Google ile giriş → yeni kullanıcı ön dolu kayıt ekranına düşüyor →
telefon+OTP ile kayıt tamamlanıyor → **ikinci girişte tek dokunuşla** oturum açılıyor ·
iptal sessiz · Apple butonu bayrak kapalıyken görünmüyor.

---

### 12.9 — Panelin dış bağımlılıklarını yerelleştirme (CDN → self-host + SRI/CSP) — [x] ✅ TAMAMLANDI (10 Ağustos 2026)

> 📌 **Nereden çıktı:** 9 Ağustos 2026'da dış bir analiz (Gemini CLI) panelde Tailwind'in CDN'den
> çekildiğini işaret etti. **Kodda doğrulandı ve bulgunun tarif edilenden ağır olduğu görüldü** —
> aşağıdaki tablo tarama sonucudur, tahmin değil.

**Hedef:** Panelin çalışması için **internet gerekmemesi** ve yöneticinin tarayıcısında üçüncü
taraf kod çalışmaması.

**Bugünkü durum — dört ayrı origin, SRI yok, CSP yok:**

| Kaynak | Nerede | Erişilemezse ne olur |
|---|---|---|
| `cdn.tailwindcss.com` | `Views/Shared/_Layout.cshtml` **ve** `Views/Account/Login.cshtml` (iki ayrı kopya) | Panel tamamen **stilsiz** |
| `cdnjs.cloudflare.com` (FontAwesome 6) | `_Layout.cshtml` | Tüm ikonlar kaybolur |
| `fonts.googleapis.com` (Inter) | `Login.cshtml` | Yazı tipi geri düşer |
| `unpkg.com` (Leaflet 1.9.4, CSS+JS) | `_LocationPickerScripts.cshtml` → **10 form, 5 modül** | 🔴 **Harita seçici tamamen ölür** |

🔴 **Bu kozmetik bir sorun değil.** `_LocationPickerScripts` duyuru · vefat · rehber · etkinlik ·
mekan formlarının **hepsinde** kullanılıyor (`Create` + `Edit` = 10 görünüm). Leaflet gelmezse
`L.` çağrıları `undefined` üzerinde patlar: yönetici boş bir kutu görür, **koordinat seçemez** ve
ekranda hiçbir hata mesajı çıkmaz. Yani "işlevsiz buton yok" kuralının panel karşılığı, ağ
kesildiği anda sessizce ihlal olur.

🔴 **Güvenlik ayağı — projenin kendi duruşuyla çelişiyor.** Dört üçüncü taraf origin,
**`super_admin` oturumu açık** bir tarayıcıda sınırsız JavaScript çalıştırıyor; `integrity=` (SRI)
**hiçbirinde yok** ve uygulamada **CSP başlığı hiç yok** (tarandı, 0 eşleşme). PII maskeleyen,
giriş denemesi loglayan, `ProductionReadinessGuard` yazan bir projede bu tutarsız: cdnjs/unpkg
tarafında bir tedarik zinciri olayı, panelin **tamamının** ele geçirilmesi demek.

⚠️ Ayrıca `cdn.tailwindcss.com` Tailwind'in **tarayıcı-içi JIT derleyicisi**; kendi dokümantasyonu
production'da kullanılmamasını söylüyor (her sayfa yüklemesinde CSS derleniyor + FOUC).

📌 **Bu, `CODE_REVIEW_CHECKLIST.md` §11'in tam olarak var olma sebebi olan hata sınıfı:**
*"hataları `flutter run` ile görünmez"*. Geliştiricinin makinesinde internet hep var, panel hep
çalışır; sorun ilk kez **belediyenin kısıtlı ağında** ya da bir CDN kesintisinde görünür.

#### Yapılacaklar

- **Tailwind'i derle.** `KadirliApp.Web/package.json` + `tailwind.config.js` + giriş CSS'i;
  çıktı `wwwroot/css/panel.css` olarak servis edilir. `content` taraması `Views/**/*.cshtml`
  olmalı — yoksa üretilen CSS'te kullanılan sınıflar **eksik kalır** ve düzen sessizce bozulur.
  ⚠️ Derlenmiş CSS **commit edilir** (repoyu klonlayan `npm` kurmadan paneli açabilmeli);
  `npm` adımı CI'da doğrulanır, geliştirici makinesinde zorunlu olmaz.
- **FontAwesome · Inter · Leaflet** `wwwroot/lib/` altına alınır (jQuery zaten orada — desen mevcut).
- **`Login.cshtml`'in kendi `<head>`'i** ortak layout'la hizalanır: bugün Tailwind'in **ikinci bir
  kopyasını** taşıyor, yani düzeltme iki yerde yapılmazsa giriş ekranı CDN'e bağlı kalır.
- **CSP başlığı** eklenir (`default-src 'self'`). ⚠️ Panelde satır içi `<script>`/`onclick` var mı
  önce taranmalı — CSP'yi `unsafe-inline` ile açmak korumayı büyük ölçüde iptal eder.
- **`ProductionReadinessGuard`'a kapı:** Production'da panel görünümlerinde dış origin kalmışsa
  uygulama **açılmaz** (11.16'daki `Otp:DevMode` kapısının aynı deseni).

#### Test

- 🔑 **Yapısal test (`release_config_test.dart`'ın panel karşılığı):** `Views/**/*.cshtml`
  **taranır**, `src=`/`href=` içinde `http://`/`https://` **hiç** olmamalı. Elle liste tutulmaz —
  liste çürür, yeni bir görünüm sessizce CDN'e döner.
- `wwwroot/css/panel.css` var ve boş değil; `PanelPagesSmokeTests` hâlâ yeşil.
- CSP başlığı yanıtta var ve `unsafe-inline` **içermiyor**.

**Bitti kriteri:** **ağ kesilir** (ya da DNS'te dört origin bloklanır) → panel açılır, stiller
yerinde, ikonlar görünür ve **etkinlik formundaki harita seçici koordinat kaydeder** ·
`Views/` taramasında dış origin **sıfır** · Production'da bilerek bırakılan bir CDN satırı
uygulamayı **açtırmıyor**.

#### Doğacak görünmez sözleşme

- **Panel dış origine bağlı olamaz** — bozulursa hata vermez, yalnız ağın iyi olduğu her yerde
  çalışmaya devam eder ve *tam olarak* kötü koşulda (kısıtlı ağ, CDN kesintisi) kırılır.
  Harita seçicide belirti bile yok: boş kutu, log yok, hata yok.

#### 12.9 kapanış notları

**Teslim edilenler:** `KadirliApp.Web/{package.json,tailwind.config.js,Assets/panel.input.css,tools/copy-vendor.mjs}` ·
`wwwroot/css/panel.css` (derlenmiş, **40 KB**) · `wwwroot/js/panel.js` (ortak davranış) ·
`wwwroot/lib/{leaflet,fontawesome,inter}` · `Common/ContentSecurityPolicyMiddleware.cs` ·
`Common/PanelAssetGuard.cs` · `_Layout` + `Login` + `_LocationPickerScripts` yerelleştirmesi ·
**17 görünümde 47 satır içi işleyicinin taşınması** · 25 satır içi bloğa nonce ·
CI'ya **sürüklenme kapısı** · 3 yeni test dosyası.
**Backend 843 → 863 (+20), mobil 751 (değişmedi — sunucuda/mobilde tek satır yok), analyze 0.**

🔑 **TESLİM EDİLEN:** Panel artık **internet olmadan çalışıyor** ve yöneticinin tarayıcısında
üçüncü taraf kod koşmuyor. Dört origin (`cdn.tailwindcss.com`, `cdnjs.cloudflare.com`,
`fonts.googleapis.com`, `unpkg.com`) sıfırlandı; 37 panel sayfası canlıda tarandı, **dış origin 0**.

🔴 **EN ÖNEMLİ KARAR: CSP'nin bedelini ödemek.** `script-src`'a `'unsafe-inline'` yazmak beş
dakikalık işti ve **korumanın kendisini iptal ederdi** — panelde basılan metnin bir kısmı
*vatandaştan* geliyor (hata kaydı mesajı, şikayet başlığı) ve depolanmış XSS §7 madde 33'ün
zaten savaştığı sınıf. Nonce yalnız `<script>` **bloklarını** kapsadığı için bedel **47 satır
içi işleyicinin taşınması** oldu. Bu sırada 7 kopya `previewImage`/`clearImage` çifti de
tekilleşti (11.15c'de 21 kopya `confirm()`, 11.18'de 5 kopya toplu işlem JS'i — bu üçüncüsü).
**İkinci karar:** `style-src`'ta `'unsafe-inline'` **kaldı** ve bu bilinçli bir taviz — Leaflet
elemanların `style` özniteliğine yazıyor; CSP3'ün `style-src-attr`'ı daha dar olurdu ama
Firefox/Safari onu yok sayıp `style-src`'a düşer, yani harita seçici **o tarayıcılarda**
kırılırdı: 12.9'un düzeltmek için var olduğu hasarın aynısı.
**Üçüncü karar:** `img-src`'ta OpenStreetMap **açık.** "Leaflet gelmedi" ile "kareler gelmedi"
aynı şey değil — ilki seçiciyi **öldürüyordu**, ikincisinde harita gri kalır ve **koordinat
seçimi çalışır.** Bir dünya haritasının görüntüsü self-host edilemez; bunu "tamamlanmamışlık"
saymak, gerçek kırılganlığı gizlerdi.
**Dördüncü karar:** yapısal denetim **derleme zamanında**, kapı **çalışma anında.** Planın metni
"Production'da görünümlerde dış origin kalmışsa açılmasın" diyordu; bu **yapılamaz ve yapılsaydı
yalan söylerdi** — Razor derlenip assembly'ye gömülüyor, yayında `.cshtml` bulunması garanti
değil, dosya tarayan bir kapı **sıfır dosya bulup yeşil geçerdi.** Çalışma anında
*gözlenebilir* olan denetleniyor: türetilmiş varlıklar yerinde mi (`PanelAssetGuard`).

🐛 **PLANIN YAZDIĞINDAN AĞIR ÇIKAN ŞEY — Tailwind `content` listesi.** Plan `Views/**/*.cshtml`
diyordu. Tarama, rozet/buton renklerinin **üç `.cs` dosyasında** yaşadığını gösterdi
(`PanelDisplay`, `PowerOutagePhase`, `BulkToolbarViewModel` — 19 sınıf dizisi). CDN sürümü
tarayıcı-içi JIT olduğu için sınıfın nerede yazıldığı **hiç önemli değildi**; derlenmiş Tailwind
yalnız gördüğünü üretir. Plan harfiyen uygulansaydı panelin **bütün durum rozetleri renksiz**
kalırdı ve ne derleyici, ne test, ne log söylerdi.

🐛 **BİR TEST ZAYIF ÇIKTI (bozma denemesi sayesinde).** Rozet sınıfı testi başta **elle seçilmiş
dört sınıfa** bakıyordu; ikisi (`bg-amber-100`, `bg-red-200`) meğer görünümlerde de geçiyordu,
yani `content`ten `**/*.cs` düşse bile **yeşil kalırlardı**. Test artık listeyi **türetiyor**
(C#'ta geçip görünümlerde geçmeyenler) ve küme boşalırsa **kendi anlamsızlığını** bildiriyor.
🔑 12.5/12.6'nın dersi üçüncü kez doğrulandı: *yeşil kalan bozma denemesi "kural sağlam" değil
"test kuralı tutmuyor" demektir.* ⚠️ Testin ilk düzeltmesi de kırmızı verdi ve **haklıydı**:
`\b` ile başlayan desen `hover:bg-amber-700` içindeki `bg-amber-700`'ü de yakalıyordu, oysa
varyantlı yardımcı CSS'e `.hover\:…` olarak çıkıyor.

🐛 **`PanelConfirmDialogTests` KIRMIZIYA DÖNDÜ VE DOĞRUYDU.** Onay dinleyicisi `_Layout`'un satır
içi bloğundan `panel.js`'e taşınınca test kırıldı — yani test gerçekten o kuralı tutuyormuş.
Yapılan şey testi gevşetmek değil beklentiyi **yeni tek sahibe** taşımak oldu; ayrıca iddia
**iki parçalı** yapıldı (dinleyici panel.js'te **var** *ve* panel.js `_Layout` tarafından
**yükleniyor**) — yalnız birincisine bakan bir test, dosya var ama sayfaya hiç dâhil
edilmiyorken de yeşil kalırdı.

🐛 **CANLI CHROME DENETİMİNDE BULUNAN İKİ ŞEY:**
1. **Nonce sayfaya HTML-kaçırılmış giriyordu.** Düz base64 `+` üretiyor, Razor öznitelikleri
   kaçırdığı için sayfada `…ErK&#x2B;7Mf…` görünüyordu. Tarayıcı doğru çözüyor, yani
   *çalışıyordu* — ama güvenlik kritik bir değeri karşılaştırmadan önce bir kodlama
   gidiş-dönüşünden geçirmek bir gün ayrışacak türden kırılganlık ve fark **hiçbir yerde
   görünmüyordu**. Nonce artık **base64url**; başlık ile sayfa **bayt bayt aynı**.
2. 🔴 **12.9 KAPSAMI DIŞI, ÖNCEDEN VAR OLAN GERÇEK HATA — mekan 0,0'a kaydediliyordu.**
   `PlacesAdmin` formu "Konum **\***" diyor, alanlarda `required` var ve JS kapısı
   (`preparePlaceForm`) "haritaya tıklayın" diyor. Ama `CreatePlaceCommand.Latitude`
   **decimal** (nullable değil) → `asp-for` alana `value="0"` basıyor, `"0"` JavaScript'te
   **truthy**, `required` de doluyu görüyor. Yani **kapı var, kapı hiç çalışmıyordu**: yeni
   mekan Gine Körfezi'ne (0,0) kaydediliyor ve mobildeki "Konuma Git" vatandaşı oraya
   götürüyordu. Kapı artık 0'ı "işaretlenmedi" sayıyor; canlıda üç senaryoda doğrulandı
   (0,0 → engellendi + Türkçe mesaj · gerçek koordinat → serbest · boş → engellendi).

➕ **PLAN DIŞI EKLENENLER (kullanıcı onaylı serbest kapsam):**
- **Leaflet yoksa panel SUSMUYOR.** Eski davranış "boş kutu, log yok, hata yok"tu; yerelleştirme
  bunu çok zorlaştırdı ama imkânsız kılmadı (yanlış statik dosya yapılandırması, eksik dağıtım).
  Artık Türkçe bir uyarı çıkıyor ve enlem/boylam alanları **elle girilebilir** hâle geliyor —
  yöneticinin elinde kaydı oluşturmanın bir yolu kalıyor. Canlıda simüle edilip doğrulandı.
- **CI sürüklenme kapısı.** `panel.css` türetilmiş **ama commit edilen** bir dosya; bilinen
  çürüme biçimi "görünüme yeni sınıf yazılır, `npm run build` unutulur, sınıf CSS'te yoktur".
  CI çıktıyı yeniden üretip `git diff` ile karşılaştırıyor.
- **`asp-append-version="true"`** — içeriği değişip adı değişmeyen bir dosya yöneticinin
  tarayıcısında günlerce eski kalırdı ("bende düzelmedi").
- **7 kopya fotoğraf önizlemesinin tekilleştirilmesi** (yukarıda).
- **Inter'in `latin-ext` altkümesi.** Türkçe'nin **ğ · ş · İ · ı** harfleri `latin`'de **yok**;
  Google Fonts CDN'i iki altkümeyi de kendiliğinden servis ediyordu, yani bu ancak
  yerelleştirdikten **sonra** doğabilecek bir kayıptı. Aynı sınıf: Leaflet'in `images/`
  klasörü — kopyalanmasaydı harita açılır, **seçilen noktanın işaretçisi görünmezdi**.
- **Inter genel `font-sans`'a EKLENMEDİ.** Bugün Inter'i yalnız giriş ekranı kullanıyor;
  `sans` yığınına koymak "yerelleştirme" değil **bütün panelin yazı tipini değiştirmek** olurdu.

🔴 **GÖRÜNMEZ SÖZLEŞMELERE #51 EKLENDİ.** Toplam **51**. Karşılığı **iki** yerde ve bu bilinçli:
`PanelExternalOriginTests` kaynağı tarar, `PanelContentSecurityPolicyTests` canlı yanıta bakar —
politikayı üretip pipeline'a **bağlamayı unutmak** mümkün ve o durumda kaynak taraması yeşil kalır.

**Doğrulama:** `dotnet test` **863/863** · `flutter analyze` **0** · `flutter test` **751/751**.
**Kuralı bilerek boz:** 5 deneme → 5 kırmızı (CDN satırı geri kondu · görünüme `onclick=`
eklendi · CSP'ye `'unsafe-inline'` eklendi · `content`ten `**/*.cs` düşürüldü · onay dinleyicisi
taşındı). **Biri ilk turda testin kendisini zayıf gösterdi** ve test güçlendirilip tekrar denendi.
**Canlı (Chrome + curl, 37 panel sayfası):** dış origin **0** · satır içi işleyici **0** ·
nonce'suz blok **0** · CSP **hepsinde** (404 sayfası dâhil) · konsolda **tek CSP ihlali yok** ·
harita yerel Leaflet ile açıldı, **8 kare** yüklendi, tıklamayla koordinat doldu
(`37.3736700 / 36.0973835`) ve **işaretçi görseli de yerelden** geldi · tekilleştirilmiş fotoğraf
önizlemesi uçtan uca çalıştı (seç → önizleme → kaldır → gizlendi) · toplu işlem 0/4/3 +
belirsiz hâl doğru, onay metnindeki `{count}` **3** oldu, iptal edilince form **gönderilmedi** ·
`data-toggle` dört değerde de doğru · `data-submit-on-change` formu gönderdi ·
mahalle "Tümünü Seç/Temizle" **10/0**.

---

### 12.10 — Moderasyon geçişinin tek sahibi: Düzenle formunun açtığı ikinci yol — [x] (10 Ağustos 2026)

> 📌 **Bu alt-faz 10 Ağustos 2026'daki dış analiz oturumunda doğdu — ama analizin *önerdiği*
> madde bu değil.** Analiz "anemik domain modeli" diyordu (soyut, 50 entity'lik bir dönüşüm
> öneriyordu ve 9 Ağustos'ta zaten ertelenmişti). O iddianın **somut ve kanamakta olan tek
> örneği** aranınca bu çıktı: nesne kendi değişmezini korumadığı için **ikinci bir yazma yolu
> onları atlıyor.** Soyut şikâyet reddedildi (aşağıdaki denetim bölümü), somut örneği faza alındı.

**Hedef:** Bir kaydın moderasyon durumunu (`pending`/`approved`/`rejected`) değiştirmenin
**tek yolu** Onayla/Reddet komutları olsun. Bugün panelin **Düzenle formundaki durum açılır
menüsü** ikinci bir yol ve o yol hiçbir kuralı uygulamıyor.

#### 🔬 Kanıt (10 Ağustos 2026, gerçek Postgres üzerinde koşturuldu)

Süresi 3 gün önce dolmuş bir ilan, panelin Düzenle formunun gönderdiği `UpdateAdCommand` ile
`approved` yapıldı:

```
Status      = approved
ExpiresAt   = 2026-08-07 12:02:34Z   (şimdi: 2026-08-10 12:02:36Z)
ApprovedBy  = NULL
ApprovedAt  = NULL
--> Vatandaş bu ilanı görebilir mi? HAYIR
```

Reddedilmiş bir ilan aynı yoldan `approved` yapıldı:

```
Status         = approved
RejectedReason = Uygunsuz gorsel.
--> Panelde yan yana: 'Onaylandı' rozeti + 'Reddedilme sebebi: Uygunsuz gorsel.'
```

Bu tam olarak **11.15c'de kapatılan iki hatanın geri gelmiş hâli** — kapatıldıkları yer
`ApproveAdCommandHandler`'dı, ama ikinci yol hiç kapatılmamıştı.

#### Kapsam: 4 modül × 2 yüzey

| Modül | Update handler | Panel Düzenle formunda durum menüsü | Atlanan kural |
|---|---|---|---|
| İlanlar | `UpdateAdCommandHandler:34` | ✔ (`pending`/`approved`/`rejected`) | #25 taze pencere · `RejectedReason` temizliği · `ApprovedBy/At` |
| Kampanyalar | `UpdateCampaignCommand:49` | ✔ (+`expired`) | `RejectedReason` temizliği · `ApprovedBy/At` |
| Vefat | `UpdateDeathNoticeCommandHandler:36` | ✔ (+`archived`) | onay izi |
| Etkinlikler | `UpdateEventCommand:90` | ✔ | onay izi |

İkinci yüzey **admin API**: `PUT /v1/admin/ads/{id}` yalnız `[RequirePermission("ads","update")]`
taşıyor ama gövdesinde `status` kabul ediyor (kampanya/etkinlik/vefat aynı).

#### 🔴 Üç ayrı sessiz hasar — hiçbiri hata vermiyor

1. **Yetki yükselmesi.** `PanelPermissionAttribute`'un kendi belgesi şunu söylüyor:
   *"Moderasyon kararları tek yetkide toplanır: 'içeriği yayına alabilir mi?' sorusu,
   'düzenleyebilir mi?'den ayrı bir güvendir."* Ama `Edit` aksiyonu **`update`** iznine düşüyor
   (§7 madde 19 — türetme **doğru**), form ise `approved` sunuyor. Yani **yalnız düzenleme
   yetkisi verilmiş moderatör moderasyon kararı verebiliyor.** Bu, §7 madde 29'daki
   `BulkApprove` hatasının **üçüncü biçimi** ve 11.15b'nin "karşılığı olmayan yetki"sinin tersi:
   burada yetki *fazladan* çalışıyor.
2. **Denetim izi yalan söylüyor.** Dört Update komutundan **üçünde `IAuditableCommand` hiç yok**
   (`UpdateAd`, `UpdateCampaign`, `UpdateDeathNotice`); dördüncüsü (`UpdateEvent`) izi
   **`update`** olarak yazıyor. Yani bu yoldan verilen bir onay kararı denetim izinde ya
   **hiç görünmüyor** ya da moderasyon kararı gibi görünmüyor → "bu ilanı kim onayladı?"
   sorusunun cevabı yok.
3. **İş kuralı atlanıyor.** Yukarıdaki kanıt. Panel "güncellendi" diyor, vatandaş hiçbir şey
   görmüyor, `ExpireAdsJob` bir saat içinde durumu sessizce geri alıyor.

#### Yapılacaklar

**Backend — tek sahip**

- Her moderasyonlu modül için **saf, container'sız test edilebilir** bir geçiş sınıfı
  (`AdSubmissionRules` / `PowerOutagePhaseRules` deseni): `AdModeration.Approve(ad, adminId, now)`
  ve `.Reject(ad, reason, now)`. #25'in taze penceresi, `RejectedReason`/`RejectedAt` temizliği
  ve `ApprovedBy`/`ApprovedAt` yazımı **yalnız burada** yaşar.
- `ApproveXCommandHandler` ve `RejectXCommandHandler` bu sınıfa **delege eder** (bugünkü
  davranış birebir korunur — kural taşınıyor, değişmiyor).
- `UpdateXCommandHandler` artık `Status`'e **dokunmaz.**
  ⚠️ **Alan DTO'dan SİLİNMEZ** (§5 — silmek kırıcı olurdu, faz "hepsi additive"). Ama
  **sessizce yok da sayılmaz**: gelen `Status` kaydın mevcut durumundan farklıysa komut
  **reddeder ve sebebini söyler** ("Durum değişikliği Onayla/Reddet ile yapılır"). Sessizce
  yutmak §7 madde 37'nin savaştığı sınıf — hiçbir şey yapmayan buton, işlevsiz butondan kötüdür.

**Panel**

- Dört Düzenle görünümünden durum açılır menüsü kaldırılır; yerine **salt-okunur**
  `_StatusBadge` + zaten var olan **Onayla / Reddet** butonları.
  🔑 Bu, "işlevsiz buton yok" kuralının **tersi**: burada sorun butonun bir şey yapmaması değil,
  **yapmaması gerekeni yapması**.

**Testler (~20)**

- Davranış: süresi dolmuş ilan Düzenle yolundan onaylanamaz (komut **reddeder**, kaydı **ezmez**
  — §7 madde 46'nın "reddetme kaydı ezmemeli" kuralı).
- Davranış: `Approve` → taze pencere **hâlâ** çalışıyor (mevcut `PanelBusinessRuleTests` korunur;
  kural taşındı, kaybolmadı).
- Davranış: yalnız `update` izni olan moderatör durumu değiştiremiyor.
- **Yapısal** (kaynak taraması, `PanelExternalOriginTests` deseni — elle liste **tutulmaz**):
  moderasyonlu modüllerin Düzenle görünümlerinde `asp-for="Status"` **yok**, ve hiçbir
  `Update*CommandHandler` `.Status =` **yazmıyor**. Elle liste tutulursa yeni modülde çürür.
- Denetim: `Approve`/`Reject` izlerinin `approve` olarak düştüğü doğrulanır.

#### Yeni görünmez sözleşme

**#52 — Moderasyon durumu yalnız `Approve`/`Reject` komutlarından yazılır; `Update*` komutları
`Status`'e dokunmaz.** Bozulursa: yetki matrisi delinir, denetim izi kararı kaydetmez ve
modülün onay kuralları (taze pencere, bayat gerekçe temizliği) atlanır — **üçü de hata
vermeden**. Karşılığı hem davranış hem **yapısal** testte olmalı: yalnız davranış testi yazılırsa
beşinci bir modül eklendiğinde kural sessizce delinir.

#### Neden bu faza sığar

Şema değişikliği **yok**, migration **yok**, DTO alanı **silinmiyor**, mobilde **tek satır
değişmiyor** — mağazadaki eski sürümler etkilenmez. Faz 12'nin "hepsi additive" sözü korunur.

#### Bitti kriteri

`dotnet test` + `flutter analyze` + `flutter test` yeşil · **kuralı bilerek boz → kırmızı
görüldü** (en az: durum menüsünü geri koy · `Update` handler'ına `.Status =` geri koy ·
taze pencereyi kaldır) · canlı panelde dört modülde doğrulandı · Memory Bank güncel · commit.

#### ✅ Ne yapıldı (10 Ağustos 2026)

**Backend — tek sahip.** Dört saf geçiş sınıfı (`AdModeration` · `CampaignModeration` ·
`DeathNoticeModeration` · `EventModeration`) + ortak `Common/Moderation/ModerationStatusGuard`.
Altı Approve/Reject handler'ı bu sınıflara **delege ediyor** (kural taşındı, değişmedi), dört
`Update*` handler'ı `Status`'e **dokunmuyor** ve farklı değer gelirse **reddedip sebebini
söylüyor**.

**Alan silinmedi.** `Status` dört DTO'da duruyor (§5), yalnız `string?` yapıldı.
⚠️ Bu nullable dönüşüm kozmetik değil **zorunluydu**: MVC'de non-nullable referans tipi
**örtük olarak zorunludur**, alan formdan kaldırıldığı anda `ModelState` kırılır ve
hiçbir düzenleme kaydedilemezdi. (§5: doğrulama gevşetmek güvenli, sıkılaştırmak kırıcı.)

**Panel.** Dört Düzenle görünümünde menü yerine ortak `_ModerationStatusField` partial'ı:
salt-okunur `_StatusBadge` + Onayla/Reddet(/Arşivle). Butonlar `formaction` kullanıyor —
HTML'de form iç içe olamaz, ayrı bir `<form>` konsaydı tarayıcı onu **sessizce atardı**.
`formenctype` override'ı da bilinçli: Düzenle formları `multipart` ve olmasaydı "Onayla"ya
basmak henüz kaydedilmemiş fotoğrafı yükleyip çöpe atardı.

**➕ PLAN DIŞI ve ZORUNLU — vefatta iki yol AÇILDI.** Planın tablosu vefatta "atlanan kural:
onay izi" diyordu; taramada çıkan şey daha ağırdı: durum menüsü o modülde **reddetmenin ve
arşivlemenin TEK yoluydu.** Karşılığı yazılmasaydı bir hatayı düzeltirken iki işlev silinmiş
olurdu. `RejectDeathNoticeCommand` (+ sebep) ve `ArchiveDeathNoticeCommand` yazıldı, panel
aksiyonları + Index butonları + admin API uçları eklendi, toplu red açıldı
(`BulkToolbarViewModel.includeReject` bayrağı **silindi** — tek kullanıcısı bu boşluktu).
⚠️ `Archive` öneki `PanelPermissionFilter`'ın **moderasyon listesine eklendi**: eklenmeseydi
POST olduğu için sessizce `update`'e düşer ve yayından kaldırma kararı düzenleme yetkisine
açılırdı (#29'un aynısı).

**➕ PLAN DIŞI (diğer).** Kampanya reddine **sebep alanı** (komut 11.15b'den beri kabul
ediyordu, panel hiç göndermiyordu → işletme sahibi *neden* reddedildiğini hiç göremiyordu) ·
`CampaignModeration.Reject`'in **onay izlerini temizlemesi** (ilanlarda 10.14(1)'de çözülmüş,
kampanyaya taşınmamıştı) · `RedisplayEditAsync` (hata sonrası durumu **DB'den tazeliyor**;
yoksa onaylı bir kaydın formunda "—" rozeti ve **"Onayla" butonu** belirirdi) ·
`panel.js`'te iki onay dinleyicisinin **tek sahibe** birleşmesi.

**🐛 YAPISAL TESTİN BULDUĞU, PLANDA OLMAYAN ÜÇÜNCÜ KOPYA.** `UpdateMyAdCommandHandler` — yani
**vatandaşın kendi ilanını düzenlemesi** — durumu `pending`'e çekip onay/red izlerini elle
temizliyordu. Meşru bir geçiş ama **üçüncü bir sahip**: ilana yarın bir onay izi alanı
eklendiğinde iki yer güncellenip üçüncüsü unutulur ve kayıt "pending ama onaylayanı dolu"
hâline düşerdi. `AdModeration.Resubmit`'e taşındı.

**🐛 `PanelConfirmDialogTests` KIRMIZIYA DÖNDÜ VE HAKLIYDI.** Yeni butonlardaki `data-confirm`
dinleyici tarafından **hiç okunmuyordu** (dinleyici yalnız formun özniteliğine bakıyordu) →
onay penceresi sessizce açılmayacaktı. `e.submitter` eklendi; toplu işlemin ayrı `click`
dinleyicisi de aynı sahibe birleştirildi (kalsaydı aynı butonda üst üste binip onayı **iki
kez** açardı, ilki ham `{count}` metniyle).
⚠️ İlk yazımda testin yeni iddiası **zayıftı** ve bozma denemesi **yeşil kaldı**; iddia
`submitter.getAttribute('data-confirm')` arayacak biçimde güçlendirildi.

**🐛 CANLI DENETİMDE BULUNAN GÖRÜNMEZ BUTON.** "Arşivle" `bg-gray-600` kullanıyordu, o sınıf
derlenmiş `panel.css`'te **yoktu** → buton **beyaz üstüne beyaz** çizildi: DOM'da var,
ölçüleri doğru (91×36), erişilebilirlik ağacı buluyor — **insan gözüyle yok.** 12.9'un "C#'ta
üretilen rozet sınıfı" maddesinin kardeşi. `npm run build` ile çözüldü.

**Testler (+46).** `Unit/Application/Moderation/` (saf) ·
`Integration/Architecture/ModerationSingleOwnerTests.cs` (**yapısal**, moderasyonlu modül
kümesini `Approve*.cs` varlığından **türetir**) · `Integration/Panel/PanelModerationOwnershipTests.cs`
(davranış, gerçek Postgres) + `PanelModeratorPermissionTests`'e iki ekleme.

**Görünmez sözleşme #52 eklendi.** Toplam **52**.

**Bitti kriteri karşılandı:** `dotnet test` **909/909** · `flutter analyze` **0** ·
`flutter test` **751/751** · kuralı bilerek boz **5 deneme → 4 kırmızı, 1 yeşil kaldı ve o test
güçlendirildi → kırmızı** · canlı panelde dört modülde doğrulandı.

---

---

## 🔚 Faz 12 dışında kalan, hâlâ açık maddeler

> 📌 **5 Ağustos, ikinci geçiş — bu liste denetlendi ve iki maddesi BAYAT çıktı.**
> Devralınan açık madde listeleri sessizce çürüyor: madde kapanıyor, listeden düşmüyor,
> bir sonraki plan onu "hâlâ açık" diye kopyalıyor. Aşağıdakiler **kaynak kodda** doğrulandı.

- ✅ **10.14/(2) — ZATEN YAPILMIŞ, listeden düştü.** `HangfireDashboardAuthorizationFilter`
  yazılmış ve `Api/Program.cs`'te bağlı: rol kapısı (`admin`/`super_admin`) → Basic auth
  (`Hangfire:Dashboard:Username/Password`, **sabit süreli** karşılaştırmayla) → kimlik bilgisi
  yapılandırılmamışsa yalnız gerçekten yerel istek. `ProductionReadinessGuard` da kimlik
  bilgisi boşsa uyarıyor. **Kalan gerçek boşluk `ForwardedHeaders`** — filtrenin kendi
  belgesi de bunu söylüyor → **12.2'ye alındı** (aşağıdaki gerekçe).
- ⚠️ **10.14/(3) — riski YANLIŞ yazılmış, bugün geçerli DEĞİL.** `docker-compose.yml`
  yalnız postgres · redis · seq içeriyor; **API compose'da hiç yok** (yerel `dotnet run` ile
  koşuyor). Yani `uploads/` konteyner katmanında değil, repo yanında düz bir klasör —
  `docker compose down` ona **dokunmuyor**. Madde, API'nin **konteynerleştirildiği gün**
  doğacak bir risk; o güne kadar yapılacak bir şey yok, çünkü volume'ü bağlanacak servis yok.
  **Deploy fazına ait, Faz 12'ye eklenemez.**
- 🍎 **Apple bekleyenler** (abonelik alınmadı): imzalama sertifikaları · TestFlight ·
  App Store Connect kaydı · **APNs `.p8`** · mağaza görselleri.
  🔴 **12.8'in Apple ayağı buna sert bağımlı:** "Sign in with Apple" yalnız paket değil,
  developer portalında yapılandırılmış bir **App ID capability**'si ister. Abonelik haftalar
  alabildiği ve 12.8 sekiz oturum sonra olduğu için **başvuru şimdi yapılmalı** — yoksa
  faz sonunda tek blokaj o olur. Abonelik gelmezse 12.8 Apple butonunu **bayrak kapalı**
  yazar ve yayın kontrol listesine "açılacak" maddesi düşer.
- 🤖 **Play bekleyenler:** `keytool` ile yayın anahtarı + Play Console hesabı → internal test.
- ✅ **Küçük borç (`uploads/` test artıkları) — ZATEN ÖDENMİŞ, listeden düştü.**
  *(11 Ağu 2026'da denetlendi.)* Madde `a165a62`'nin `git add -A`'sıyla giren 35 artığı
  işaret ediyordu ve çözümü `git rm --cached` diye yazıyordu. **İkisi de bayat:**
  `fe062de` (5 Ağu 2026, *"uploads/ takipten cikarildi + 50 yetim test artigi silindi"*)
  hem 36 dosyayı takipten çıkardı hem **`.gitignore`'a kapı koydu**.
  Bugünkü kanıt: `git ls-files uploads/` → **0 dosya**.
  🔑 **Bu maddenin kendisi, bu bölümün başındaki uyarının canlı örneği oldu:** madde kapandı,
  listeden düşmedi, iki plan turu boyunca *"hâlâ açık"* diye taşındı. `.gitignore`'daki notun
  dersi de aynı sınıftan ve daha genel: **`4085a96` ilk 39 artığı sildi ama kapı koymadı**,
  altı gün sonra `a165a62` 35 yenisini geri doldurdu — *kapı olmadan silmek işe yaramıyor.*
  📌 Yerel diskteki `uploads/` (185 dosya, 740 KB, hepsi test artığı deseninde) **git'te
  değil** — repoyu etkilemiyor. Klasörü `LocalFileStorageService` kendisi oluşturuyor;
  silmek isteğe bağlı bir geliştirici makinesi temizliğidir, faz işi değil.

### 📥 9 Ağustos 2026 — dış analiz (Gemini CLI) maddelerinin denetimi

> Dört madde **kaynak kodda doğrulandı**; dördü de gerçek. Ama "gerçek" ile "yapılmalı" aynı şey
> değil — üçü bilinçli bir tercihin sonucu ya da başka bir fazın işi. Karar ve gerekçe burada
> duruyor ki bir sonraki oturum aynı tartışmayı sıfırdan yapmasın.

- ✅ **CDN bağımlılığı → `12.9` olarak faza alındı.** (Tek "yapılmalı" çıkan madde; analiz bunu
  kozmetik sanıyordu, tarama Leaflet'in **10 formda işlevsel** bir kırılma ürettiğini gösterdi.)
- ⏸️ **Anemik domain modeli — DOĞRU ama Faz 12'ye alınmadı.** Tarama: entity'lerin
  **hiçbirinde** `private set` yok (0 dosya); `Ad`'da 24, `User`'da 32 public setter; Domain Event
  altyapısı hiç yok. **Ama bu bir kaza değil:** proje iş kurallarını bilerek **saf, container'sız
  test edilebilir** sınıflarda tutuyor (`AdSubmissionRules`, `PowerOutagePhaseRules`,
  `SuspiciousLoginRules`, `PowerOutageNeighborhoodMatcher`, `DistrictLabel`…) ve "görünmez
  sözleşmeyi testle kilitle" disiplininin taşıyıcısı tam olarak bu sınıflar. 50 entity'yi rich
  domain'e çevirmek **her handler'a** dokunur, vatandaşa **sıfır** görünür fayda üretir ve
  Faz 12'nin "hepsi additive" sözünü bozar. 🔑 İstenirse **ayrı bir faz** (Faz 13) olarak, modül
  modül ve testler yeşil kalarak yapılabilir — 12'nin içine sıkıştırılamaz.
- ⏸️ **`IQueryable` sızıntısı — DOĞRU ama en düşük öncelikli.** `IRepository<T>.Query()`
  `IQueryable<T>` döndürüyor ve `KadirliApp.Application.csproj` doğrudan
  `Microsoft.EntityFrameworkCore`'a referans veriyor, yani veri erişimi detayı Application'a
  sızıyor. Analiz bunun ".NET dünyasında pragmatik olarak yaygın" olduğunu kendisi de söylüyor.
  Kapatmanın bedeli: her sorgu için özelleşmiş repository metodu → bugün tek satırda yazılan
  filtreler arayüze taşınır ve **`Features/` içindeki dikey dilimleme zayıflar**. Faydası mimari
  saflık, maliyeti okunabilirlik. **Yapılmayacak diye karar verilmedi, sıraya alınmadı.**
- ⏸️ **IaC / CD eksikliği — DOĞRU, ama Faz 12'nin konusu değil.** Tarama: `.github/workflows/`
  altında yalnız **CI** var (`dotnet.yml`, `mobile.yml`); deploy job'ı, Dockerfile ve IaC dosyası
  **yok**. Bu bir **deploy fazı** işi ve yukarıdaki `10.14/(3)` maddesiyle aynı kaderi paylaşıyor:
  API henüz konteynerleştirilmedi, bağlanacak bir hedef ortam yok. Hedef ortam seçildiğinde
  (VPS / Azure / Hetzner) tek bir "yayına alma" fazında Dockerfile + volume + CD + IaC birlikte
  ele alınmalı — parça parça eklemek yarım bir pipeline bırakır.

📌 **Analizde bayat olan iki şey:** proje "Faz 12.3'te" deniyor (12.4 bitti) ve "700'den fazla
test" deniyor (**784 backend + 703 mobil = 1487**).

**Faz 12'ye alınanlar:** `ForwardedHeaders` → **12.2** · bağımsız push ekranı → yeni **12.2b**
(gerekçeleri kendi başlıklarında).

### 📥 10 Ağustos 2026 — ikinci dış analiz (Gemini CLI) denetimi

> 🔑 **Sonuç: analizin dört maddesinden ÜÇÜ, 9 Ağustos'ta denetlenip gerekçeli olarak
> ertelenmiş maddelerin birebir tekrarı.** Yeni bilgi getirmiyorlar; kararlar bir gün önce
> ve daha ayrıntılı gerekçeyle verilmişti (yukarıdaki bölüm). Bu not, üçüncü bir analizin
> aynı üç maddeyi üçüncü kez "CRITICAL FAILED" diye getirmesi hâlinde tartışmanın sıfırdan
> başlamaması için var.

- ✅ **CDN bağımlılığı → kapandı.** Analiz **12.9'u doğru tespit etti** ve frontend'i 10/10'a
  çıkardı. Tek "gerçekten değişti" maddesi bu.
- 🔁 **Anemik domain + Domain Event eksikliği — 9 Ağustos'ta ertelendi, karar değişmedi.**
  Tarama tekrarlandı, olgular doğru: `private set` **0 dosya**, Domain Event altyapısı **yok**,
  `KadirliApp.Domain/Class1.cs` hâlâ duran boş bir şablon artığı. Karar aynı: 50 entity'yi
  dönüştürmek her handler'a dokunur, vatandaşa **sıfır** görünür fayda üretir ve fazın
  "additive" sözünü bozar → **Faz 13 adayı.**
  ⚠️ **Domain Event için ek gerekçe (bu turda ölçüldü):** olay güdümlü mimarinin klasik
  kazancı "önbellek geçersizleştirmesini handler'dan ayırmak"tır — ama **ilan listesi
  cache'li değil** (`CacheGroups`'ta yalnız `guide`/`pharmacies`/`dashboard`/`lookups`/
  `ads-lookup` var). Proje zaten **açık tek-sahip arayüzleri** kullanıyor
  (`INotificationDispatcher`, `IPowerOutageAnnouncementWriter`) ve bunlar örtük olay
  dağıtımından **daha test edilebilir**. Somut kazanç bulunamadı.
- 🔁 **`IQueryable` sızıntısı — 9 Ağustos'ta ertelendi, karar değişmedi.** ⚠️ Analiz bunu
  "CRITICAL CHECK FAILED" diye işaretlemiş **ama aynı cümlede bilinçli bir tercih olduğunu da
  yazmış** — yani kendi notumuzu okuyup yine kritik saymış. Bu turda **canlı zarar arandı ve
  bulunamadı:** sızıntının bu kod tabanındaki bilinen tek tuzağı `Query()`'nin varsayılan
  **`AsNoTracking`** olması (12.3 canlı hatası) ve **12 `SoftRemove` çağrısının hepsi** izlenen
  nesne üzerinde; **15 dosya** bilinçli `Query(tracking: true)` yazmış, bir tanesi de yorumla
  gerekçelendirmiş. **Bugün hiçbir yerde kanamıyor** → sıraya alınmadı.
- 🔁 **IaC / CD — 9 Ağustos'ta ertelendi, karar değişmedi.** Tarama tekrarlandı:
  `Dockerfile` **yok**, `.tf`/`.bicep` **yok**, `dotnet.yml`'de deploy adımı **yok**
  (dosyanın adı "CI/CD Pipeline" ama içerik yalnız CI). Hedef ortam seçilmeden IaC yazmak
  çürüyecek kod üretir → **deploy fazı.**

📌 **Analizde bayat olan:** "Gelecek Vizyonu" bölümü projeyi **"Faz 12.4"** sanıyor (12.9 bitti).

#### 🔴 Analizin BULAMADIĞI ama denetimden çıkan şey → yeni **12.10**

"Anemik domain" soyut bir şikâyet olarak reddedildi; ama iddianın **somut karşılığı** aranınca
gerçek bir hata çıktı: `Status` alanı public olduğu için **panelin Düzenle formu ikinci bir
moderasyon yolu** açıyor ve o yol yetki kapısını, denetim izini ve onay kurallarını birden
atlıyor. **4 modül × 2 yüzey**, gerçek Postgres üzerinde kanıtlandı (çıktı 12.10'da).
🔑 **Ders:** soyut mimari eleştiriler bu projede doğrudan uygulanabilir olmuyor, ama
**"bu iddianın kanayan bir örneği var mı?"** diye sorulduğunda kanıtlanabilir bir hataya
götürebiliyor — reddedilen madde bile ücretsiz değil, aranmayı hak ediyor.

---

### 12.11 — Tek sahipliğin **derleyiciye** devri — [x] ✅ TAMAMLANDI (11 Ağustos 2026)

> **Bu alt-faz planda yoktu.** Kullanıcı Gemini CLI'ya bir mimari analiz yaptırdı ve çıkan
> `Domain_Analysis_Evidence.md` "projedeki **anemik domain** bir kodlama tercihi değil, canlıda
> hasar üretmiş yapısal bir zafiyettir" diyordu. Oturum bu iddiayı **koda karşı doğrulamakla**
> başladı.

#### Analizin üç kanıtının denetimi

| Kanıt | Verdikt | Gerekçe |
|---|---|---|
| **1. Anemik domain canlı hasar üretti** (12.10'un `ExpiresAt`/`ApprovedBy` bulgusu) | **Kanıt bayat, iddia geçerli** | Alıntıladığı canlı hasar 12.10'da **bulunup düzeltilmiş** bir hatanın kanıt notuydu; o yol bugün kapalı. Ama "tek sahiplik derleyiciyle değil bir taramayla korunuyor" iddiası ayaktaydı → aşağıdaki bulgu. |
| **2. Domain events yokluğu → yan etkiler her handler'a kopyalanmış** | **Yanlış** | Alıntıladığı `UpdateMyAdCommandHandler` tekrarı *aynı oturumda* (12.10) `AdModeration.Resubmit`'e taşınmıştı — yani fixin açıklaması, hatanın kanıtı olarak sunulmuş. Dahası yan etkiler handler'ların **içinde değil**: önbellek geçersizleştirme `ICacheInvalidator`, denetim izi `IAuditableCommand` işaretleyicileri + MediatR **pipeline davranışları** üzerinden koşuyor (`CacheInvalidationBehavior` · `AuditBehavior`). Amaç olarak domain event'lerin yaptığı işi, komut seviyesinde ve daha az hareketli parçayla yapan bir mekanizma zaten var. |
| **3. CD / IaC (Terraform) eksik** | **Bayat + kapsam dışı** | Dayandığı alıntı ("panelin admin parolası bu makinede bilinmiyor") 11.18'de çözüldü: parola `secrets/panel-admin.json`'da ve açılışta hizalanıyor. Kalanı **var olmayan** bir production için altyapı yazmaktır; CI (`.github/workflows/`) zaten çalışıyor. |

🔑 **Analizin bir cümlesi ayrıca düzeltilmeli:** *"zengin domain olsaydı o hata derleyici
seviyesinde imkânsız olurdu"* — 12.10'un **üç** hasarından yalnız **birini** (iş kuralı) kapatır.
Yetki yükselmesi (`Edit` → `update`) ve denetim izinin yalan söylemesi zengin domain'le de
yaşanırdı: bir `Update` handler'ı `ad.Approve()` **çağırmakta serbesttir**.

#### 🔴 Denetimin bulduğu gerçek delik (analizin göremediği, ama iddiasını haklı çıkaran)

```
KadirliApp.Application/Features/Ads/Commands/ExtendMyAd/ExtendMyAdCommand.cs:64
    ad.Status = "approved";      ← AdModeration dışında, BEŞİNCİ yazıcı
```

12.10'un yapısal testi (`ModerationSingleOwnerTests`) bu satırı **hiç taramıyordu** ve sebebi
**12.9'un dersinin birebir tekrarıydı**: test moderasyonlu **modül listesini** türetiyordu
(elle liste tutmuyordu, bu doğruydu) ama taradığı **dosya adı desenini**
(`Update*`/`Approve*`/`Reject*`/`Archive*`) elle tutuyordu — `ExtendMyAd*` hiçbirine uymuyor.

**Hasar yoktu:** bir ilan `expired`'a yalnız `ExpireAdsJob` üzerinden (`approved` iken)
düşebildiği için onay izi zaten doluydu, kayıt bozulmuyordu. Bulgunun değeri kaydın bozulması
değil, korumanın **tesadüfen** çalışıyor olmasıydı — kurala dayanarak değil.

🔑 **Ders: bir taramanın KAPSAMI da elle tutulan bir listedir.** 12.9 "elle liste tutan kapı,
listeye girmeyeni korumaz" demişti; 12.10 bunu *modül* listesinde çözdü, *dosya deseni*
listesinde tekrarladı.

#### Teslim edilenler

**Karar: testi genişletmemek.** Kolay yol deseni `Extend*` ile büyütmekti; o da bir sonraki
uymayan dosyada aynı şekilde delinirdi. Bunun yerine koruma **taramanın erişemeyeceği yere**
taşındı:

- **Moderasyon alanları `init`** oldu (`Status` · `ApprovedBy` · `ApprovedAt` ·
  `RejectedReason` · `RejectedAt`) — `Ad` · `Campaign` · `DeathNotice` · `Event`.
- **Geçişler varlığın metotlarına indi:** `Ad.Approve/Reject/Resubmit/Extend` ·
  `Campaign.Approve/Reject` · `DeathNotice.Approve/Reject/Archive` · `Event.Approve/Reject`.
- **Dört `…Moderation.cs` sınıfı silindi** (façade olarak bırakılmadı: alan `init` olduğu için
  Application katmanındaki bir sınıf ona zaten yazamaz — hiçbir şey yapmayan bir dolaylama
  katmanı olurlardı). Kurallar **birebir korundu**, yalnız yer değiştirdi.
- `ExtendMyAdCommandHandler`'ın geçişi `Ad.Extend`'e taşındı.
- **`ModerationSingleOwnerTests`**: `ModerationCommands_DelegateToTheTransitionClass` (artık
  derleyici garantisi) kaldırıldı, yerine iki yeni test geldi (aşağıda).
- **Backend 909 → 913 (+4), mobil 751 (değişmedi — tek satır dokunulmadı), analyze 0.**

**🔴 EN ÖNEMLİ KARAR: `init`, `private set` DEĞİL.** `private set` nesne başlatıcıyı da kapatır
ve `new Ad { Status = "pending" }` yazan **~40 çağrı yerini** (oluşturma, `MockDataSeeder`,
~25 test dosyası) fabrika metoduna çevirmeyi gerektirirdi. `init` **yüklenmiş varlığa** yazmayı
kapatır — hasarın tamamı zaten oradan geliyordu. Kazanç aynı, bedel yok: **tek test dosyası bile
mutasyona uğramadı** (denetimde görüldü ki hiçbir test `Status`'ü kurulumdan *sonra* yazmıyor).

**İkinci karar: kapsam DAR.** Bu **genel bir "zengin domain modeli" kararı değil**. Analizin
istediği 50 varlığı `private set` + fabrikaya çevirmekti; projede canlı hasar üretmiş **tek**
değişmez moderasyon durumudur ve kapatılan o oldu. Gerisi kanıtsız risk olurdu — projenin kendi
kuralı (§6 "modül kaldırırken tabloyu düşürme") ile aynı muhafazakârlık.

**🔴 Yeni tehlike ve onun da kilidi (§7 madde 53).** `init`'i bozan biri `CS8852` alır ve o
hatayı çözmenin **kolay** yolu geçişi varlığa taşımak değil, alanı **`set`'e geri açmaktır** —
o an her şey derlenir, **bütün testler yeşil kalır** ve koruma sessizce kaybolur. Bu yüzden
`init`'in kendisi kilitlendi:

- `EveryModeratedEntity_ExposesItsModerationFieldsAsInitOnly` — moderasyonlu varlık kümesi
  **türetiliyor** (`Domain/Entities/` altında `public void Approve(` bildiren dosyalar) ve alan
  listesi de türetiliyor: varlıkta hangi kolon *varsa* o denetleniyor. (`Event`'te yalnız
  `Status` var — onay izi `audit_logs`'ta; `Campaign`/`DeathNotice`'ta `RejectedAt` yok. Elle
  liste bu farkları "eksik alan" sanıp haksız yere kırılırdı.)
- `EveryModeratedModule_HasAnEntityThatOwnsItsTransitions` — iki kümeyi bağlar: moderasyonlu
  **modül** sayısı ile geçiş metodu tanımlayan **varlık** sayısı ayrışırsa kırmızı. Beşinci bir
  moderasyonlu modül eklenip varlığı açık setter'la bırakılırsa bunu başka hiçbir test söylemez.

➕ **PLAN DIŞI:** `Campaign.Reject` ve `DeathNotice.Reject`'ten **kullanılmayan `now` parametresi
düştü** (o iki varlıkta `RejectedAt` kolonu yok; parametre 12.10'dan beri hiçbir yere yazmıyordu —
`EventModeration`'ın *"simetri için boş parametre taşımak ilk okuyana yalan söyler"* kararının
geç uygulanması) · `Ad.PublishDays` sabiti varlığa taşındı · `Ad.Extend`'e üç birim testi, biri
**ters yön**: uzatma **sahte onay izi yazmamalı** — "her uzatmada `ApprovedBy`'ı doldur"
gerçeklemesi bu iddia olmadan yeşil kalırdı.

⚠️ **Bu korumanın DIŞINDA kalan yol (bilinçli ve belgeli):** `ExpireAdsJob` ve `ArchiveDeathsJob`
durumu `ExecuteUpdateAsync` ile **SQL seviyesinde** yazar — set-tabanlı tek UPDATE atomik ve
idempotent olsun diye. `init` onları etkilemez (ifade ağacı setter çağırmaz) ve etkilememeli.

**Doğrulama:** `dotnet test` **913/913** · `flutter analyze` **0** · `flutter test` **751/751**.
**Kuralı bilerek boz:** 4 deneme → **4 kırmızı** (`Ad.Status` `init`→`set` · `Event.Approve`
yeniden adlandırıldı → kardinalite testi · `Ad.Extend` sahte onay izi yazdı · `Ad.Extend` her
zaman bugünden uzattı). **5. "deneme" testle değil derleyiciyle doğrulandı:** dört
`…Moderation.cs` silindiği anda `ExtendMyAdCommand.cs(64,13): error CS8852` çıktı — yani deliğin
kendisi **derleme hatası olarak** ortaya çıktı ve bulguyu kanıtlayan da bu oldu.

🔑 **Bu oturumun genel dersi (12.10'un dersinin devamı):** dış mimari eleştirilerin *kanıtı*
bayat olabiliyor — 12.10 ve 12.11'de de öyleydi, alıntılanan hatalar zaten düzeltilmişti. Ama
*"bu iddianın bugün kanayan bir örneği var mı?"* sorusu iki oturumda da **kanıtlanabilir bir
hataya** götürdü. Reddedilen madde bile ücretsiz değil, aranmayı hak ediyor.

---

# 📰 HABERLER MODÜLÜ (12.12 – 12.15) — 11 Ağustos 2026'da planlandı

> **Bu blok planda YOKTU** — 12.11 bittikten sonra kullanıcı isteğiyle açıldı ve Faz 12'nin
> sonuna eklendi. **Sıra: 12.7/12.8 (sosyal giriş) hâlâ önce mi sonra mı, karar kullanıcının** —
> teknik bir bağımlılık yok, iki blok birbirine dokunmuyor.
>
> **Ne isteniyor?** Kendi haber sitemiz `silagazetesi.com.tr` (WordPress) bir REST API sunuyor.
> Haberleri **kendi veritabanımıza** çekip panelden özelleştirerek uygulamada yeni bir
> **Haberler** modülünde göstereceğiz.
>
> 🔑 **Temel mimari karar (kullanıcı koydu, doğru): mobil WordPress'e ASLA bağlanmaz.**
> Zincir tek yönlü: `WordPress → (Hangfire senkron) → bizim Postgres → /v1/news → mobil`.
> Gerekçe kullanıcının kendi cümlesi: *"bu modül ile alakalı özellik geliştirmesi yapabilmemiz
> lazım."* Mobil WP'ye bağlansaydı override, kategori görünürlüğü, bildirim, arama ve önbellek —
> hepsi imkânsız olurdu; üstelik uygulama **başka birinin çalışma süresine** bağımlı olurdu.
> Bu, projedeki **ilk dış servis entegrasyonu** (FCM ve SMS dışında) — yeni bir hasar sınıfı
> getiriyor: *kaynak sessizce değişebilir ve biz haberi hiç alamayabiliriz.*

## 📊 Planlamadan önce ÖLÇÜLEN gerçekler (11 Ağustos 2026, canlı API)

> Aşağıdakiler varsayım değil; `curl` + 400 haberlik korpus taramasıyla doğrulandı.
> Plandaki kararların çoğu **doğrudan bu ölçümlerden** çıktı.

| Ölçüm | Değer | Plana etkisi |
|---|---|---|
| Yayınlanmış haber | **27.284** (`X-WP-Total`), 273 sayfa | İlk dolum bir "tek istek" işi değil → **iki imleçli** tasarım |
| Yeni/güncellenen | **~5/gün** | Senkron 15 dk'da bir fazlasıyla yeter; otomatik push **zehirli** olur |
| Kategori | **15** (Gündem 9753 · Haberler 4999 · Yerel Haberler 4673 · Son Dakika 2520 · Genel 1392 · Siyaset 983 · Eğitim 920 · Spor 541 · Kültür&Sanat 510 · E-Gazete 366 · Ekonomi 296 · Tarım 264 · Özel Haber 242 · Resmi İlanlar 145 · Bilim-Teknoloji 77) | Sözlük tablosu; **`LookupsAdmin`'e bölüm**, ayrı ekran değil |
| **Çoklu kategori** | Bir haber birden çok kategoride (`[49,51,52]`) | 🔴 Görünürlük semantiğini belirleyen tek olgu — bkz. 12.13 |
| **Öne çıkan görsel `full` boyutu** | 40 haberin **39'unda 650×368** | 🔴 **"Büyük görsel" YOK.** Detayda bile 650px; 3x telefonda yukarı ölçeklenir |
| Evrensel boyutlar | `thumbnail` 150×85 · `medium` 300×170 · `full` 650×368 | Liste = `medium`, detay = `full` |
| `large` / `medium_large` | 40 haberde **1** | ⚠️ **Bağlanılamaz** |
| `jannah-image-*` | 40/40 var **ama WP temasından geliyor** | ⚠️ Tema değişirse **sessizce kaybolur** → yalnız yedek zincirde, asla tek kaynak değil |
| Metin arası görsel | Haberlerin **%35'inde** (1–3 adet) | `content.rendered` içinde ham `<img>` |
| Metin arası görsel origin'i | 247 kendi sitesi · **25'i `fbcdn.net` / `outlook.live.net`** | 🔴 İmzalı, **süreli** URL'ler — zamanla 403 olur |
| İçerik HTML'i | `p` 3674 · `div` 864 · `figure` 720 · `img` 272 · `br` 260 · `a` 106 · `span` 88 · `strong` 24 · **`object` 14 · `video` 4 · `form` 2** | Basit; ama `object`/`video`/`form` **temizlenmek zorunda** |
| İçerik uzunluğu | ortalama 2008, medyan 1457, max 11438 karakter | 27k haber ≈ 55 MB metin — Postgres için önemsiz |
| `modified_after` semantiği | 🔴 **SİTE-YEREL saat (UTC+3)**, `gmt_offset=3` | Aşağıdaki kanıta bak — bu fazın 1 numaralı tuzağı |
| Desteklenen parametreler | `_embed` · `_fields` · `per_page=100` · `orderby=modified\|date` · `modified_after` · `categories` | Artımlı senkron ve yük azaltma mümkün |
| **Kaynağın güvenilirliği** | Örnekleme sırasında bir sayfa **`error code: 520`** döndü | 🔴 Kaynak *kararsız* → senkron hata toleranslı olmak **zorunda** |
| `flutter_html` | **3.0.0** + `list_counter 1.0.2`, Flutter 3.44.2 ile temiz çözülüyor (`pub add --dry-run`) | Mobilde kullanılabilir |

### 🔴 Kanıt: `modified_after` yerel saatte çalışıyor

En son değişen haberin damgaları: `modified = 2026-08-11T10:11:36` (yerel), `modified_gmt = 2026-08-11T07:11:36` (UTC).

```
modified_after=2026-08-11T10:11:36  (yerel değer)  ->  X-WP-Total: 0
modified_after=2026-08-11T07:11:36  (UTC değer)    ->  X-WP-Total: 4
```

Yani WP, parametreyi **`post_modified` (yerel)** ile karşılaştırıyor. UTC damgası gönderilince
pencere 3 saat **geriye** kaydı ve 4 kayıt fazladan geldi.

🔑 **Bu, §7 madde 6'daki *"TR günü, 00:00 UTC"* tuzağının birebir kardeşi** — ve o sınıf bu
projede **4 kez** tekrarlamış (11.7/11.10/11.11/11.13). Yön kritik:
- Yerel yerine **UTC** göndermek → pencere genişler → **mükerrer kayıt** → upsert idempotent
  olduğu için **zararsız**.
- Ters yön (damgayı "UTC'ye çevireyim" diye 3 saat **ileri** almak) → her koşuda **3 saatlik
  haber sessizce atlanır**, hiçbir hata oluşmaz, panelde hiçbir belirti yok.

**Karar:** imleç `modified_gmt`'den (UTC) saklanır, sorguya **site-yerel** olarak çevrilerek
gönderilir, üstüne **bilinçli bir çakışma payı** (30 dk) eklenir. Dönüşümün **tek sahibi**
`WordPressTimeWindow` olur (`OperatingDays` / `SlugHelper` deseni) ve gidiş-dönüş testle kilitlenir.
⚠️ `DateTime.UtcNow` **asla** doğrudan `modified_after`'a yazılmaz.

## ⚙️ Blok başında alınan kararlar (kullanıcı onayladı, 11 Ağustos 2026)

| Karar | Seçim | Gerekçe |
|---|---|---|
| **Görseller** | **Aynalanır** (kapak görseli indirilip `files` modülüne yazılır) | Kaynak kararsız (520 görüldü) ve metin içi görsellerin %9'u **süreli** `fbcdn` linki. Ayrıca panelde WP görselini basmak **CSP'ye takılır** (aşağıda) ve §7 madde 9 (göreli görsel URL) bedavaya sağlanır |
| **İlk dolum derinliği** | **50 haber** | Kullanıcının gerekçesi: *"biz bunu ilk başta test edeceğiz."* Doğru karar — ama derinlik **yapılandırmadan** okunur (`News:Backfill:MaxPosts`), kod değişmeden artırılabilir |
| **Bildirim türü** | **`relatedType = "news"`** | Kullanıcı seçti, sınırı bilerek: mağazadaki **eski sürümler** bu türü tanımaz (§7 madde 18) → bildirimi listede **okur**, dokununca **hiçbir yere gitmez**. Karşı öneri ve neden reddedildiği 12.15'te |
| **Moderasyon** | **YOK** — otomatik yayın + geri alınabilir gizleme | Aşağıda, 12.13'te ayrıntılı |
| **Mobil ↔ WP** | **Doğrudan bağ yok** | Kullanıcı koydu; yukarıdaki gerekçe |

## 🔴 Bu bloğun taşıdığı ÜÇ YENİ HASAR SINIFI

Projedeki 24 modülün hiçbirinde olmayan, tamamen yeni riskler. Alt-fazların şekli bunlara göre:

1. **Kaynak sessizce susabilir.** Senkron durursa (WP kapandı, imleç bozuldu, job kuyruğu takıldı)
   uygulama **eski haberi göstermeye devam eder**, uçlar 200 döner, log temizdir, kimse hata almaz.
   → 12.13'te **bayatlık göstergesi** zorunlu (Dashboard kutusu + eşik).
2. **Kaynak, panelin yaptığını ezebilir.** Yönetici başlığı düzeltir, bir sonraki senkron üstüne
   yazar. Klasik "iki sahip" hasarı (§7 madde 23'ün sınıfı) → 12.12'de **`Source*` / `*Override`
   kolon ayrımı** ve ayrımın **derleyiciyle** korunması.
3. **Kaynakta silinen haber bizde sonsuza kadar yaşar.** `modified_after` **silmeyi hiç bildirmez** —
   WP'de yayından kalkan haber uygulamada durmaya devam eder. → 12.12'de ayrı bir **mutabakat işi**.

## 🧭 Alt-faz sırası ve gerekçesi

**12.12 (alım) → 12.13 (panel) → 12.14 (mobil) → 12.15 (bildirim).**
Sıra Faz 12'nin kendi kuralını izliyor: *önce veri doğru olsun, sonra görünsün.* Panel mobilden
**önce** geliyor çünkü kategori dışlamalarının ve override'ların uygulama açılmadan **önce**
ayarlanabilmesi gerek; bildirim **en sona** çünkü gönderilmiş bir push **geri alınamaz**
(§7 madde 37) ve elimizde önce doğrulanmış bir veri kümesi olmalı.

⚠️ **Dört alt-faz, üç değil.** İlk bakışta "panel + mobil" iki oturum gibi duruyor; ama 12.12
tek başına yeni bir entegrasyon katmanı (HTTP istemcisi + iki Hangfire işi + sanitizasyon +
görsel aynalama + iki imleç) getiriyor ve 12.15 `INotificationDispatcher`'a — projenin en
hassas **tek sahipli** arayüzüne — dokunuyor. Tarihsel ölçek karşılaştırması: 12.4 (+55),
12.5 (+59), 12.10 (+46 test) birer oturumdu; bu blok toplamda onların **üçü kadar**.

---

### 12.12 — Haberler: alım çekirdeği — [x] ✅ TAMAMLANDI (11 Ağustos 2026)

**Hedef:** WordPress'ten haberlerin **doğru, tekrarlanabilir ve ezmeyen** biçimde kendi
veritabanımıza inmesi. Bu alt-fazda **panel ekranı ve mobil ekran YOK** — çıktı, `dotnet test`
ve veritabanı üzerinden doğrulanır.

#### Alan modeli

**`NewsArticle : BaseEntity`** (`news_articles`, snake_case). Kolonlar **üç kümeye** ayrılır ve
bu ayrım bu alt-fazın kalbidir:

```
// 1) KAYNAĞIN sahibi — panel BURAYA YAZAMAZ
WpId (int, unique)          SourceTitle           SourceExcerpt
SourceContentHtml (text)    SourcePlainText (text, arama + özet yedeği)
SourceImageFileId (Guid?)   SourceImageWidth/Height
SourceUrl                   SourcePublishedAt (UTC)   SourceModifiedAt (UTC)
SourceChecksum              SourceState ("published" | "gone")

// 2) YÖNETİCİNİN sahibi — senkron BURAYA YAZAMAZ (hepsi nullable)
TitleOverride               ExcerptOverride        CoverImageFileIdOverride
OverrideUpdatedAt           OverrideUpdatedBy

// 3) BİZİM alanlarımız (WP'de karşılığı yok)
IsArchived (bool)           ArchivedReason         ArchivedBy/At
IsFeatured (bool)           FeaturedUntil (DateTime?)
AnnouncementId (Guid?)      // 12.15
```

İndeksler: `(WpId)` unique · `(SourcePublishedAt desc)` · `(IsArchived, SourceState)` ·
`(SourceModifiedAt desc)` · `SourceTitle` üzerinde arama indeksi (27k kayıt → indekssiz her
arama tam tarama).

**`NewsCategory : BaseEntity`** (`news_categories`): `WpId` unique · `Name` · `Slug` ·
`ArticleCount` · **`IsExcluded`** (varsayılan `false`) · `ShowInFilterStrip` · `DisplayOrder`.
**`news_article_categories`** çoka-çok bağ tablosu.
📌 **Silme yok** — `LookupsAdmin`'in mevcut kuralı (FK'lı sözlük verisi, yalnız bayrakla pasifleşir).

**`NewsSyncRun : BaseEntity`** (`news_sync_runs`): `StartedAt` · `CompletedAt` ·
`Trigger` (`schedule`|`manual`) · `TriggeredBy` · `Fetched` · `Created` · `Updated` · `Skipped` ·
`Failed` · `Status` · `ErrorMessage` · `CursorFrom`/`CursorTo`.
🔑 Tasarımı **`PushCampaign`'den kopyalanır** (12.2b) — sayaçlar **artımlı** yazılır, sorgu anında
`COUNT` ile hesaplanmaz (§7 madde 39).

#### 🔴 Karar 1: iki sahip, tek kolon değil — ve ayrımı DERLEYİCİ korur

Alternatif "kilitle bayrağı"ydı (`IsLocked` → senkron kilitli kaydı atlar). **Reddedildi:**

| | Kilit bayrağı | Override kolonu ✅ |
|---|---|---|
| Kaynakta yazım hatası düzeltildi | Kilitli kayıt **hiç güncellenmez**, kilit sessizce eskir | `Source*` güncellenir, override yerinde durur |
| "Kaynakta ne değişti?" | Bilgi **kaybolur** | `SourceModifiedAt` + `SourceChecksum` elde |
| Geri alma | "Kilidi aç" → ne olacağı belirsiz | "Override'ı kaldır" → kayıt kaynağa döner, **deterministik** |
| Koruma nerede | Senkron kodunun kilidi kontrol etmesine **güven** | Senkron o kolonu **göremez** |

Son satır belirleyici ve **12.11'in dersinin birebir uygulanması**: *korumayı taramanın
erişemeyeceği yere taşı.* `Source*` alanları **`init`** olur ve yalnız
`NewsArticle.ApplySourceSnapshot(snapshot)` metodundan yazılır; override'lar
`NewsArticle.SetOverrides(...)` / `ClearOverride(...)`'dan. Senkron bir gün override'a yazmaya
kalkarsa **`CS8852` derleme hatası** alır.

⚠️ Bedeli burada **sıfıra yakın** — varlık yepyeni, 12.11'deki gibi ~40 çağrı yerini fabrikaya
çevirme sorunu yok. 📌 Bu yine **genel bir "zengin domain" kararı değil** (§7 madde 53'ün kapsam
uyarısı aynen geçerli): tek bir değişmez kapatılıyor.

⚠️ **Ama `Approve` kelimesini KULLANMA.** `ModerationSingleOwnerTests.ModeratedModules()`
moderasyonlu modül kümesini `Features/<M>/` altında **`Approve*.cs` dosyası var mı** diye
türetiyor (satır 69–73) ve `ModeratedEntities().Count`'la eşitliyor (satır 311). `Features/News/`
altına `ApproveNewsCommand.cs` koyduğun **an** panel controller'ı, `_ModerationStatusField`,
`ModerationStatusGuard` çağrısı ve beş moderasyon alanının `init` olması **zorunlu hâle gelir**.
Bu blokta moderasyon **bilinçli olarak yok** (12.13) → dosya adları `Archive*` / `Unarchive*`.

#### 🔴 Karar 2: iki imleç — ileri ve geri

`modified_after` yalnız **ileri** gider. Kullanıcı ilk dolumu 50 haberle sınırladı; yarın 500 ya
da 2000 istenirse **geriye doğru** gitmek gerekecek ve tek imleçli bir tasarım bunu yapamaz.

- **İleri imleç (artımlı):** `orderby=modified&order=asc` + `modified_after=<yerel, 30 dk çakışmalı>`.
  Her koşuda yeni/güncellenen haberleri getirir. Damga `modified_gmt`'den saklanır.
- **Geri imleç (arşiv derinliği):** `orderby=date&order=desc&page=N`, `News:Backfill:MaxPosts`
  (başlangıç **50**) sayısına ulaşana kadar. Ayrı bir `ArchiveCursorPage` alanında durur.
  Ayar büyütülünce iş **kaldığı yerden** devam eder.

🔑 İkisi de **aynı upsert'e** düşer (`WpId` üzerinden), yani mükerrer çekiş zararsız.
⚠️ İki imleç **tek işte** birleşmez: artımlı iş 15 dk'da bir koşar, arşiv işi yalnız derinlik
eksikse ve **istekle** koşar (12.13'ün "Senkronu başlat" butonu).

#### 🔴 Karar 3: mutabakat işi (`ReconcileNewsJob`) — silmeyi öğrenmenin TEK yolu

`modified_after` silinen/yayından kalkan haberi **hiç bildirmez**. Bu iş olmadan:
WP'de kaldırılan bir haber uygulamada **sonsuza kadar** durur.

- Gecelik (03:00), `_fields=id` ile **yalnız kimlik** çeker (27k kimlik ≈ birkaç yüz KB, 273 istek).
- Bizde olup kaynakta olmayan `WpId` → `SourceState = "gone"`.
- 🔑 **Kayıt SİLİNMEZ**, yalnız public uçtan düşer ve panelde **"Kaynakta yok"** rozeti alır.
  Silinseydi *"haber neden gitti?"* sorusunun cevabı hiçbir yerde olmazdı.
- 🔑 **Ters yön de var:** kaynağa geri dönen haber `"published"`a döner (idempotent).
- ⚠️ İş yalnız **derinliğimiz kadarını** tarar — 50 haber çekiyorsak 27k kimlik taramak
  anlamsız; tarama penceresi arşiv derinliğiyle **aynı** olmalı, yoksa "bizde yok" ile
  "kaynakta yok" karışır ve **her eski haber `gone` işaretlenir**.

#### Sanitizasyon ve içerik

- 🔴 **Alım anında sunucuda temizlenir**, gösterim anında değil. Beyaz liste:
  `p br strong em a figure figcaption img ul ol li blockquote h2 h3 h4`.
  **Atılanlar:** `script style iframe object embed form input video` + tüm `on*=` öznitelikleri
  + `style=`. Korpusta gerçekten bulunanlar: `object` ×14, `video` ×4, **`form` ×2**.
- Yeni paket: **`Ganss.Xss` (HtmlSanitizer)** → `KadirliApp.Infrastructure`. (Katman kuralı:
  Application yalnız `INewsSourceClient` / `INewsHtmlSanitizer` arayüzlerini görür.)
- `SourcePlainText` de üretilir: arama ve **özet yedeği** için (WP `excerpt`'i HTML parçalı gelir).
- ⚠️ Temizlenmiş HTML **panelde `@Html.Raw` ile basılmaz** (12.13) — depolanmış XSS yüzeyi
  (checklist §11, §7 madde 33). Sanitizasyon bir kapı, tek kapı değil.

#### Görsel aynalama (kullanıcı kararı)

- Senkron kapak görselini indirir → var olan **`files` modülü / `IFileStorage`** üzerinden yazar →
  `SourceImageFileId`. Uçlar **göreli** URL döner (`/uploads/…`) → §7 madde 9 korunur ve mobilin
  `AppImage.url`'ü zaten doğru davranır.
- Yedek zinciri (tek sahip, `NewsImagePicker`): kapak için `full`; küçük için
  `medium → jannah-image-large → thumbnail → full`.
  ⚠️ `large`/`medium_large` **zincirde yok** (40'ta 1) ve `jannah-*` **tek kaynak değil**
  (tema değişirse kaybolur).
- Aynı görsel iki haberde geçebilir → **`SourceChecksum`/URL bazlı tekilleştirme** (aksi hâlde
  `uploads/` mükerrer dosyayla şişer).
- 📌 **Metin arası görseller aynalanmaz** (ilk sürüm): hotlink kalır, açılmazsa **zarifçe gizlenir**.
  Gerekçe: %35 haberde, %9'u süreli `fbcdn` linki — hepsini aynalamak bu alt-fazı ikiye katlar.
  ⚠️ Bu bilinçli bir borç; ikinci sürümde ele alınabilir.
- 🔴 **İndirme sınırı zorunlu:** boyut tavanı (2 MB), `Content-Type` denetimi (`image/*`),
  zaman aşımı. Kaynak bizim olsa da doğrulanmamış bir indiriciyi sınırsız bırakmak yanlış.

#### Dayanıklılık (kaynak kararsız — 520 görüldü)

- `IHttpClientFactory` + adlandırılmış istemci; zaman aşımı 30 sn; **üstel geri çekilmeli** 3 deneme.
- 🔴 **Bir sayfanın hatası bütün koşuyu düşürmez** — sayılır, `Failed`'a yazılır, koşu devam eder
  (§7 madde 29'un "kayıt başına hata partiyi durdurmamalı" kuralının aynısı).
- Senkron hatası **`ErrorLog`'a** düşer (12.1) → `ErrorFingerprint` tekilleştirmesi bedava:
  WP 20 dk boyunca 502 verirse 300 satır değil **1 satır + `OccurrenceCount`** (§7 madde 32).
- ⚠️ Hata yazma yolu isteği/koşuyu **düşüremez** (§7 madde 31).
- **`User-Agent`** açıkça set edilir (`KadirliApp-Sync/1.0`) — kaynak tarafında tanınabilir olsun.
- `[DisableConcurrentExecution]` + `[AutomaticRetry]` (mevcut job deseni).

#### Public uç

- **`GET /v1/news`** — sayfalı (`{items,…}`), süzgeçler: `categoryId` · `search` (§7 madde 4:
  çoğunluk `search` kullanıyor, `searchTerm` yalnız taksi+ulaşım) · `featured`.
- **`GET /v1/news/{id}`** · **`GET /v1/news/categories`**.
- 🔴 **Görünürlük filtresi controller'da zorlanır** (Değişmez Kural #3): `IsArchived == false`
  **ve** `SourceState == "published"` **ve** *dışlanmış kategorisi yok*.
- Sıralama varsayılanı `publishedAt desc`, **`ThenBy(Id)`** (§7 madde 30 — 27k kayıtta eşit
  tarih kesin var; ayraçsız **aynı kayıt iki sayfada, bir başkası hiç görünmez**).
- **`CacheGroups.news`** + invalidator (§7 madde 22) — senkron ve panel yazmaları temizler.
  ⚠️ Cache grubu invalidator'sız açılırsa panelde düzeltilen başlık mobilde 15 dk eski kalır.

#### Yeni görünmez sözleşmeler (§7 tablosuna eklenecek, **54'ten devam**)

- **`modified_after` site-yerel saattedir** (UTC+3); imleç `modified_gmt`'den saklanır, sorguya
  yerele çevrilerek + çakışma payıyla gider. Ters yön **her koşuda 3 saatlik haberi sessizce atlar**.
- **Senkron ile panel aynı kolona yazamaz** — `Source*` `init`, override ayrı kolon; ihlal `CS8852`.
- **Silinen haber yalnız `ReconcileNewsJob` ile öğrenilir**; kayıt silinmez, `SourceState="gone"` olur.
  İş kaldırılırsa kaldırılmış haber uygulamada **sonsuza kadar** durur ve kimse hata almaz.
- **Görsel yedek zinciri `large`/`medium_large`'a bağlanamaz** (40'ta 1) ve `jannah-*` tek kaynak
  olamaz (tema değişince kaybolur).


#### ✅ Teslim edildi (11 Ağustos 2026) — canlı doğrulamayla

**Kod:** `Domain/Entities/{NewsArticle,NewsCategory,NewsSyncRun,NewsSyncState}.cs` ·
`Application/Common/Interfaces/{INewsSourceClient,INewsHtmlSanitizer,INewsImageDownloader,INewsSyncService}.cs` ·
`Application/Features/News/` (WordPressTimeWindow · NewsImagePicker · NewsHtmlPolicy · NewsChecksum ·
NewsReadingTime · NewsSyncHealth · NewsVisibility · NewsProjection · NewsAudit · NewsSyncOptions +
`Services/{NewsSyncService,NewsImageMirror}` + 3 sorgu + 5 komut) ·
`Infrastructure/News/{WordPressNewsSourceClient,NewsHtmlSanitizer,HttpNewsImageDownloader}.cs` ·
`Infrastructure/Jobs/{SyncNewsJob,ReconcileNewsJob}.cs` · 3 EF yapılandırması + migration
(`AddNewsModule`) · `Api/Controllers/NewsController.cs` · `PanelDisplay` (3 denetim eylemi + modül
etiketi) · 6 test dosyası. **Yeni paket:** `HtmlSanitizer` (`Ganss.Xss`) → Infrastructure.
**Backend 913 → 991 (+78), mobil 751 (değişmedi — 12.14'e kadar mobilde tek satır yok).**

🔑 **TESLİM EDİLEN:** Haberler artık **bizim veritabanımızda**. Zincir tek yönlü ve kullanıcının
koyduğu gibi: `WordPress → (Hangfire, 15 dk) → Postgres → /v1/news → mobil`.

🔴 **PLANDAN İKİ SAPMA (ikisi de sessiz bir hatayı kapattığı için):**
1. **Arşiv imleci sayfa numarası DEĞİL, tarih** (`ArchiveCursorPage` → `ArchiveCursorUtc`).
   Plan `orderby=date&order=desc&page=N` diyordu; ama koşular arasında **tek bir haber
   yayınlandığı anda** bütün sayfalar bir kayar ve tam sınırdaki haber **hiçbir sayfada
   görünmez** — sonsuza kadar atlanır, hiçbir hata oluşmaz. `before=<en eski aldığımız>`
   bu sınıfa kapalı ve "derinlik büyüyünce kaldığı yerden devam etme" özelliğini birebir korur.
   Aynı düzeltme mutabakatın kimlik taramasına da uygulandı.
2. **Panel komutları 12.12'de yazıldı** (12.13'e bırakılmadı): `CacheGroups.news` eklendiği an
   `CacheContractTests` "invalidate eden komutu olmayan grup" diye kırılıyor (§7 madde 22) —
   ve haklı: grubu invalidator'sız açmak, panelde düzeltilen başlığın mobilde 15 dk eski
   kalması demek. Beş komut yazıldı (`Archive`/`Unarchive`/`UpdateOverrides`/`SetFeatured`/
   `TriggerSync`); 12.13 bunların **üstüne panel ekranı** koyacak, komut yazmayacak.

🔴 **EN ÖNEMLİ KARAR: iki sahip, tek kolon değil — ve ayrımı DERLEYİCİ koruyor.**
`Source*` alanları `init` ve yalnız `NewsArticle.ApplySourceSnapshot`'tan, `*Override` alanları
yine `init` ve yalnız `SetOverrides`/`ClearOverrides`'tan yazılıyor. Alternatif "kilit bayrağı"
reddedildi çünkü korumanın kendisi *senkron kodunun kilidi kontrol etmesine güvenmek* olurdu —
12.11'in dersinin birebir uygulanması: **korumayı taramanın erişemeyeceği yere taşı.**

🔑 **VE BU KEZ TARAMA DEĞİL YANSIMA:** `NewsSourceOwnershipTests` alan listesini **tipin
kendisinden** türetiyor (`init` erişimcisi IL'de `modreq(IsExternalInit)` taşır). 12.11'in
bulgusu *"bir taramanın KAPSAMI da elle tutulan bir listedir"* idi; yansıma o sınıfa kapalı ve
yarın eklenen bir `Source*` kolonu kendiliğinden kapsama giriyor.
🐛 **Yansıma daha ilk koşuşunda bir delik buldu:** `SourceImage` **gezinme özelliği** açık
setter'lıydı — `article.SourceImage = başkaDosya` yazmak kaydedildiğinde FK'yı da değiştirir,
yani ayrımın **üçüncü kapısı** açıktı. Kaynak taraması bunu asla göremezdi.

🐛 **İKİ GERÇEK BULGU (ikisi de "kuralı bilerek boz" turundan çıktı):**
1. **Kategori isteğinin hatası koşu defterine yazılmıyordu.** Koşu devam ediyordu (doğru) ama
   `Failed` sayacı 0 kalıyordu: kategorileri hiç alamamış bir koşu panelde **tertemiz**
   görünürdü. "0 hata" diyen bir koşu defteri, hiç defter tutmamaktan kötüdür.
2. **Kolon tavanını aşan tek bir başlık BÜTÜN partiyi düşürüyordu.** §7 madde 29'un "kayıt
   başına hata partiyi durdurmamalı" kuralı bu yolda **çalışmıyordu**, çünkü hata kayıt başına
   değil **`SaveChanges` başına** doğuyor. Kaynak bizim ama içeriğini biz yazmıyoruz.
   → `NewsColumnLimits` + kırpma; ayrıca koşu defteri zehirli bağlamda **`ExecuteUpdate`** ile
   yazılıyor (yoksa koşu satırı sonsuza kadar "çalışıyor" görünürdü).

⚠️ **DÜRÜST NOT — bir bozma denemesi KIRMIZIYA DÖNMEDİ:** sayfa hatasında `cursorIsSafe = false`
satırını kaldırmak hiçbir testi kırmadı, çünkü imleci bugün koruyan şey hemen ardındaki
`break`. Satır yine de duruyor (biri yarın "bir sayfa patladı diye durmayalım" derse koruma o
gün kaybolur) ama koda dürüst bir yorum düşüldü. Yerine, gerçekten korumasız olan yol
(toplu yazma tavanı) test edildi.

🔑 **MODERASYON YOK — ve `Approve` kelimesi bilinçli olarak KULLANILMADI.**
`ModerationSingleOwnerTests` moderasyonlu modül kümesini `Features/<M>/Approve*.cs` varlığından
türetiyor; o adla bir dosya konsa panel controller'ı, `_ModerationStatusField`,
`ModerationStatusGuard` ve beş alanın `init` olması **zorunlu** hâle gelirdi. Geçişlerin adı
`Archive`/`Unarchive` ve bu tuzak ayrıca **açıklamalı bir testle** kilitlendi.

➕ **PLAN DIŞI EKLER:** `ReadingMinutes` (türetilmiş okuma süresi, sunucuda tek yerde) ·
`NewsSyncHealth` (bayatlık eşikleri — 12.13'ün Dashboard kutusunun altyapısı) · `NewsVisibility`
(görünürlük tanımının tek sahibi; panel sayacı 12.13'te aynı sınıftan geçecek) ·
`SetNewsFeaturedCommand` + `FeaturedUntil` (süresiz manşet bayat kalır) · `TriggerNewsSyncCommand`
(checklist §11'in *"kanalı elle dene"* maddesi) · `NewsProjection.Select(includeContent)` —
gövde listede taşınmıyor ama **iki ayrı projeksiyon yazılmadı** (§7 madde 43'ün tuzağı).

**Canlı doğrulama (gerçek `silagazetesi.com.tr`, 11 Ağustos 2026):**
ilk koşu **50 haber + 15 kategori**, 50/50 kapak görseli aynalandı, **0 hata** ·
ikinci koşu `incremental`: 1 okundu, **1 atlandı, 0 mükerrer** · mutabakat: 50 kimlik, 0 `gone` ·
`GET /v1/news` göreli `/uploads/…` URL'i döndürüyor ve görsel **200** veriyor ·
DB'de `<script|form|iframe|object|video|onclick=|style=` içeren **0** kayıt, `<div|<span` **0** ·
kategori sayaçları bizdeki görünür sayıyı veriyor (Haberler 38 · Yerel Haberler 13 · E-Gazete 0).
**`dotnet test` 991/991.**

**Kuralı bilerek boz → 4 kırmızı:** `QueryFloor` yerele çevirmeyi bıraktı → 2 test ·
`SourceTitle` `init`→`set` → yansıma testi · `ApplySourceSnapshot` override'ı ezdi → geçiş testi ·
mutabakatın boş-liste kapısı kaldırıldı → arşivin tamamı `gone` olurdu · başlık kırpması
kaldırıldı → parti düştü. (5. deneme yukarıda: dürüst not.)

⏭️ **Sırada 12.13** — panel. ⚠️ `PanelMenu.Items`'a "news" satırı eklendiğinde
`PanelDisplay.NonMatrixModules`'taki geçici `["news"] = "Haberler"` satırı **silinmeli**.

**Bitti kriteri:** boş veritabanına senkron koşuyor ve **50 haber + 15 kategori** iniyor ·
ikinci koşu **hiçbir mükerrer satır üretmiyor** (idempotent) · WP'de değişen bir başlık ikinci
koşuda güncelleniyor · **elle yazılmış bir `TitleOverride` ikinci koşudan sonra hâlâ yerinde** ·
kaynaktan düşürülen bir haber mutabakat işinden sonra `gone` oluyor **ve geri gelince
`published`'a dönüyor** · `<script>`/`<form>` içeren bir gövde temizlenmiş kaydediliyor ·
kapak görseli `uploads/` altında ve DTO **göreli URL** dönüyor · kaynak 500 verdiğinde koşu
`Failed` sayacıyla **tamamlanıyor**, uygulama ayakta · `dotnet test` yeşil ·
**kuralı bilerek boz:** `modified_after`'a UTC damgası → gidiş-dönüş testi kırmızı;
`Source*` `init` → `set` → yapısal test kırmızı.

---

### 🔎 12.12 SONRASI GERİYE DÖNÜK DENETİM (11 Ağustos 2026) — 12.13'te giderilecek

> Kullanıcı isteğiyle, 12.12 commit'lendikten **sonra** kendi kodumuza yapılan statik denetim.
> Hiçbiri canlıda hasar üretmedi (50 haberlik ölçekte görünmezler) — **hepsi 27k ölçeğinde ya da
> 12.13'ün elle tetikleme butonu geldiğinde açılacak kapılar.** Sıra önem sırasıdır.

> ✅ **Yüksek olan üçü aynı oturumda kapatıldı** (aşağıda ✅ ile işaretli); orta/düşük olanlar
> ve ön koşullar 12.13'e kaldı. **995 test yeşil.**

#### 🔴 Yüksek — gerçek hata, sessiz — ✅ KAPATILDI

1. **`ResolveCategoriesAsync` "bir kez" demiyor, HER POSTTA yapıyor.** Metodun kendi yorumu
   *"sözlük koşu içinde bir kez tazelenir"* diyor ama kodda bunu sağlayan bir bayrak **yok**:
   tanınmayan bir kategori kimliği varsa (kaynakta silinmiş/gizlenmiş bir kategori — mümkün,
   çünkü `/categories` yalnız *public* olanları döndürür) o kimliği taşıyan **her haber için**
   yeni bir HTTP isteği + `SaveChanges` atılır. 50 haberde 50 fazladan istek; 500'de 500.
   🔑 Asıl mesele performans değil: **yorum yalan söylüyor** ve bu projede yalan söyleyen yorum
   bir sonraki okuyanı yanlış yönlendirir.
   ✅ **Çözüldü:** sözlük artık koşu boyunca taşınan bir `NewsCategoryCache` (sözlük + "bu
   koşuda tazelendi mi?" bayrağı); bayrak sınıf alanı **değil** çünkü tek scope'ta birden çok
   koşu olabiliyor (artımlı koşu boş imleçte arşiv koşusuna düşüyor). Tazelemeden sonra hâlâ
   tanınmayan kimlik **uyarı olarak** log'a düşüyor — haber yine iniyor ama "kategorisiz haber"in
   sebebi artık bir yerde yazılı. Kilit: `UnknownCategoryId_RefreshesTheDictionaryOnlyOncePerRun`
   (bayrağı kaldırınca 3 haber için **4 istek** ölçüldü → kırmızı).
2. **`NewsHtmlSanitizer` Singleton — ama `Ganss.Xss.HtmlSanitizer` thread-safe değil.**
   Bugün zararsız görünüyor (`SyncNewsJob` `DisableConcurrentExecution` ile serileşiyor), ama
   **12.13'ün "Senkronu başlat" butonu** zamanlanmış koşuyla **aynı anda** çalışabilir → iki
   iş aynı temizleyici örneğini paylaşır. Bozulma biçimi tam bu bloğun savaştığı türden:
   istisna değil, **karışmış/eksik temizlenmiş gövde**. → `AddScoped`/`AddTransient`
   (maliyeti ihmal edilebilir; nesne kurulumu yalnız beyaz liste kopyalaması).
3. **`news_sync_state` "tek satır" ama bunu garanti eden bir şey yok.** `LoadStateAsync`
   satır yoksa yaratıyor; **`SyncNewsJob` (15 dk, yani 03:00'te de) ile `ReconcileNewsJob`
   (03:00) boş durumda aynı anda başlarsa iki satır doğar** ve `FirstOrDefaultAsync` bundan
   sonra rastgele birini seçer → ileri imleç iki koşu arasında **ileri-geri zıplar**, aradaki
   haberler atlanır, hiçbir hata oluşmaz. `DisableConcurrentExecution` yalnız **aynı** işi
   korur, iki farklı işi değil.
   ✅ **Çözüldü:** `NewsSyncState.Singleton` (her zaman 1) + **unique indeks** (migration
   `AddNewsSyncStateSingletonGuard`) + `SingleOrDefault` + yarışı kaybedenin satırı **okuması**.
   🐛 **Migration'ın kendisi ikinci bir tuzak taşıyordu:** EF varsayılanı `0` üretmişti, yani
   var olan satır `0`, yeni eklenen satır `1` olurdu → iki farklı değer, unique indeks
   **çakışmaz**, kısıt sessizce etkisiz kalırdı. Varsayılan `1`'e çekildi, var olan satır
   güncellendi ve fazladan satır varsa **en eskisi** korunacak şekilde temizlendi.
   Kilit: `SyncState_CanNeverHaveASecondRow` + `ReconcileAndSync_ShareTheSameCursorRow`.
   📌 Bozma denemesi ders verdi: indeksi **EF yapılandırmasından** kaldırmak testi kırmadı
   (koruma veritabanında, migration'da yaşıyor); **migration'daki** `unique: true` kaldırılınca
   kırmızıya döndü — yani test doğru yere bakıyor.

#### 🟠 Orta — 27k ölçeğinde acıtır (bugün 50 kayıt var, görünmüyor) — ✅ 12.13'te KAPATILDI

4. **Arama indeksi işe yaramıyor ve yapılandırmadaki yorum bunun tersini söylüyor.**
   Sorgu `x.SourceTitle.ToLower().Contains(s)` → SQL'de `lower(...) LIKE '%s%'`; bir **btree**
   indeksi bu ikisinin hiçbirini karşılayamaz. Yani `ix_news_articles_source_title` bugün
   yalnız *sıralama* için var, "arama çıpası" değil. Üstelik arama **`SourcePlainText`**'te de
   dönüyor: 27k × ~2 KB ≈ **55 MB metinde tam tarama**, üstelik her tuş vuruşunda (mobil
   arama alanı `Debouncer`'lı ama yine de). → `pg_trgm` + GIN indeksi ya da `tsvector`;
   hangisi olursa olsun **yorum düzeltilmeli** (bugün yanlış bilgi veriyor).
5. **`GetNewsCategoriesQuery` kategori başına korelasyonlu alt sorgu üretiyor**
   (`visible.Count(a => a.Categories.Any(...))` → 15 ayrı `COUNT`). 15 dk önbellekli ama
   önbelleği **her senkron temizliyor**, yani pratikte 15 dakikada bir 27k satır üzerinde
   15 alt sorgu. → tek `GROUP BY` ile sayım.
6. **Detay önbelleği anahtar sayısı sınırsız:** `news:detail:{id}` haber başına bir Redis
   anahtarı ve hepsi `news` grubuna yazılıyor; grup kümesi 27k anahtara kadar büyüyebilir ve
   her invalidation onu dolaşır. Diğer modüllerde kayıt sayısı küçük olduğu için bu desen
   bugüne kadar sorun çıkarmadı. → detayı hiç önbelleklememek (liste zaten önbellekli) ya da
   TTL'i kısaltmak; karar 12.13'te ölçüyle verilmeli.

#### 🟡 Düşük — not düşülmeli, aciliyeti yok — ✅ 12.13'te KAPATILDI

7. **`NewsImageMirror` PAYLAŞILAN `IUnitOfWork` üzerinde `SaveChanges` çağırıyor.** İki yan
   etkisi var: (a) partinin yarısını erken commit ediyor (zararsız ama "parti" semantiğini
   bozuyor), (b) o `SaveChanges` **başka bir varlığın** hatasıyla patlarsa hata burada
   yakalanıp *"Haber görseli kaydedilemedi"* diye **yanlış** loglanıyor. → dosya kaydı ayrı
   bir işlem/scope'ta ya da yalnız `Add` + partiyle birlikte kaydetme.
8. **`GetPublishedIdWindowAsync` döngüsünde sayfa tavanı yok** (`MaxPagesPerRun` yalnız
   senkron döngülerinde uygulanıyor). `News:Backfill:MaxPosts` yanlışlıkla büyük yazılırsa
   mutabakat binlerce istek atar. → aynı tavanı buraya da geçir.
9. **`?featured=false` sessizce yok sayılıyor** (yalnız `true` süzüyor). Bilinçliyse
   dokümante edilmeli, değilse "öne çıkmayanlar" süzgeci eklenmeli.
10. **`HttpNewsImageDownloader` yönlendirme takip ediyor ve iç ağ adresine karşı kapısı yok.**
    Kaynak bizim ama indirici "doğrulanmamış" sayılmalı: kaynak bir gün ele geçirilirse
    `source_url` bulut metadata adresini (169.254.169.254) gösterebilir. → yönlendirme sonrası
    host denetimi (yalnız kaynağın alan adı) ya da `AllowAutoRedirect = false`.
11. **`News:Backfill:MaxPosts` adı beklentiyi yanlış kuruyor.** Arşiv derinleştirmesi
    `remaining = MaxPosts - TOPLAM haber sayısı` hesaplıyor; artımlı senkron yeni haber
    ekledikçe **arşiv derinliği sessizce sığlaşıyor** (ayar "arşiv derinliği" değil "toplam
    kayıt tavanı" gibi davranıyor). Karar bilinçli olabilir ama **belgelenmeli**, yoksa 12.13'te
    "derinliği 200 yaptım, 50 haber geldi" sürprizi olur.

#### 🧪 Ek bulgu: süitte flaky bir test (12.12'nin tetiklediği, ✅ kapatıldı)

- **`PanelErrorLogTests` dolu süitte bir kez kırıldı, tek başına 1 sn'de geçiyordu.**
  Sebep: `WriteThroughSinkAsync` eşzamansız yazıcıyı **koşulla** bekliyor (doğru desen) ama
  tavanı 5 sn'ydi ve bu tavan **yüke göre değil sezgiye göre** seçilmişti. 12.12'nin haber
  senkron testleri aynı tek örnekli yazıcıya olay basmaya başlayınca tavan yetmedi.
  ✅ Tavan 15 sn'ye çıkarıldı — testi yavaşlatmaz, yalnız **başarısızlık** anında beklenen
  süreyi uzatır. 🔑 Ders: "sabit gecikme yerine koşul" yetmiyor, **koşulun tavanı da**
  süitin büyümesiyle çürüyen bir sayı.

#### 📌 12.13'ün ön koşulları (bulgu değil, reçetenin atlanmış adımları)

- ~~**`news` izni `permissions` tablosuna eklenmedi**~~ → 🔬 **DENETLENDİ: böyle bir adım
  gerekmiyormuş.** `permissions`/`role_permissions` tabloları bu projede **çalışma anında hiç
  okunmuyor** (canlı veritabanında da **0 satır**, her modül için): izin denetiminin tek
  kaynağı **kullanıcı başına** `admin_permissions` ve o tablonun modül listesi
  `StaffAdminController.Modules` → `PanelMenu.Items`'tan **türüyor**. Yani "izni eklemek" =
  menüye satır eklemek. Migration yazılsaydı hiçbir şeye dokunmayan ölü veri üretilirdi.
  📌 Reçetenin 8. adımı (`ARCHITECTURE.md` §4) bu yüzden **bugünün gerçeğini yansıtmıyor** —
  ayrı bir temizlik maddesi olarak açık bırakıldı.
- ✅ **`PanelDisplay.NonMatrixModules`'taki geçici `["news"] = "Haberler"` satırı silindi**;
  yerine `["news-sync"] = "Haber Senkronu"` kondu (senkron ekranı matris dışı olduğu için
  komutu ayrı bir denetim anahtarı yazıyor: `NewsAudit.SyncModule`).

---

### 12.13 — Haberler: panel — [x] ✅ TAMAMLANDI (12 Ağustos 2026)

> 📌 **Bu alt-fazın tasarımı bir Agent tartışmasından çıktı** (11 Ağustos 2026, kullanıcı isteği).
> Agent `ARCHITECTURE.md` §3/§4/§7, `CODE_REVIEW_CHECKLIST.md` §4/§11 ve mevcut panel
> controller'larını okuyup önerileri **var olan desenlere** bağladı. İki iddiası ayrıca
> **kaynak kodda doğrulandı** (aşağıda 🔬 ile işaretli).

#### Üç ekran, üç FARKLI izin deseni

| Ekran | Desen | Gerekçe |
|---|---|---|
| **`NewsAdminController`** | `[Authorize(Roles="admin,super_admin,moderator")]` + `[PanelPermission("news")]` + `PanelMenu.Items` (`Module = "news"`) | Veri hassas değil — kendi gazetemizin **zaten yayınlanmış** içeriği. Başlık düzeltmek/haber gizlemek tam moderatör işi; `announcements`/`events` ile aynı sınıf |
| **`NewsSyncAdminController`** | `[Authorize(Roles="admin,super_admin")]` + `[PanelPermission]` **YOK** + menü satırı `Module = **null**` + `PanelMenu.AdminOnlyControllers` += `"NewsSyncAdmin"` | `PushCampaignsAdmin`'in birebir gerekçesi: bu ekran yalnız göstermiyor, **tüm içerik kümesini etkileyen bir işi tetikliyor**. Matriste olsaydı aksiyon `update`'e düşer (§7 madde 19) ve yalnız düzenleme yetkisi olan moderatör senkron tetiklerdi |
| **Kategori görünürlüğü** | `LookupsAdmin`'e **akordiyon bölümü** (`lookups` izni), ayrı controller yok | `LookupsAdminController`'ın kendi kuralı: *"silme yok (FK'lı sözlük verisi — `IsActive` ile pasifleşir)"*. 15 satırlık bir sözlük ayrı ekranı hak etmiyor |

⚠️ **Unutulursa sessiz hasar:** `permissions` tablosuna `news` satırı + rollere dağıtım migration'ı
(§4 adım 8 — yoksa moderatör **403 alır ve sebebi görünmez**) · `PanelMenu.Items`'a **iki** satır ·
`PanelDisplay.NonMatrixModules`'a `["news-sync"] = "Haber Senkronu"` (yoksa denetim izi ekranı
**ham İngilizce** basar) · `ARCHITECTURE.md` modül tablosuna satır (yoksa `ArchitectureDocTests` kırmızı).

#### 🔴 Karar: MODERASYON YOK — otomatik yayın + geri alınabilir gizleme

Haber WP'den geldiği anda **yayında** olur. Onay kuyruğu **yok**. Gerekçeler:

1. **Editoryal karar zaten verilmiş.** WP bizim; `status=publish` demek insanın onayladığı demek.
   İkinci bir onay kuyruğu aynı kararı iki kez vermektir.
2. **Günde 5 haber × onay gecikmesi = ölü modül.** 6 saat gecikmiş "Son Dakika" yanlış bilgidir.
3. **Bedeli somut** (🔬 doğrulandı): `Approve*.cs` yazıldığı an `ModerationSingleOwnerTests`
   `News` varlığını 12.11 şekline sokmayı **zorunlu kılar** — `init` alanlar, `Ad.Approve` kardeşi
   metotlar, `ModerationStatusGuard`, `_ModerationStatusField`. O bedel **canlı hasar üretmiş bir
   değişmez** için ödendi (§7 madde 53'ün "kapsam dar" notu); haberde öyle bir hasar yok.

**Bunun yerine tek yönlü, tek sahipli bir görünürlük kapısı:**

- Geçişler yalnız **`ArchiveNewsCommand` / `UnarchiveNewsCommand`**'dan. Düzenle formunda
  görünürlük anahtarı **olmayacak** — 12.10'un dersinin harfi harfine uygulanması
  (checklist §11: *"bir kaydın durumunu yazan İKİNCİ bir yol"*). `UpdateNewsCommand`'a o alan
  **hiç eklenmez**, böylece guard'a da gerek kalmaz.
- **`ArchivedReason` panelde ZORUNLU** — "neden kaldırdın?" sorusunun cevabı kayıtta dursun.
- **Toplu işlem:** `ArchiveSelected` / `UnarchiveSelected` (§7 madde 29 — `…Selected` ile biter),
  `PanelBulk.RunAsync` ile **tek-kayıt komutunu** çağırır, toplu SQL değil.
- 🔴 **`PanelPermissionAttribute.ActionFor`'a `"Unarchive"` EKLENMELİ.**
  🔬 **Doğrulandı** (satır 61): önek listesi bugün
  `Approve, Reject, Verify, Unverify, Ban, Unban, UpdateStatus, Resolve, Archive`.
  `"Unarchive"` **yok** → `"Archive"` öneki onu yakalamaz (baştan eşleşme) → POST olduğu için
  sessizce **`update`**'e düşer. Sonuç: *yayından kaldırmak `approve` isterken, yayına
  döndürmek `update` ile yapılabilir.* §7 madde 19'un birebir tekrarı ve 12.10'da `Archive`'ın
  eklenme sebebinin aynısı. Listede `Unverify`/`Unban` çiftleri zaten var → **deseni takip
  etmek**, bozmak değil. `PanelModeratorPermissionTests`'e satır eklenir.
- 🔴 **SİLME YOK.** `NewsArticle`'a `ISoftDeletable` **eklenmez**, panelde "Sil" butonu **olmaz**,
  `TrashModules.Supported`'a `news` **girmez**. Sebep: kaynak hâlâ yayındayken silinen kayıt
  **bir sonraki senkronda geri gelir** → yönetici *"sildim ama döndü"* der ve sebebi hiçbir
  yerde yazmaz. Alternatif ("senkron `deleted_at` dolu kaydı diriltmesin") **yeni bir görünmez
  sözleşme** doğurur; gizleme zaten aynı işi görüyor.
  🔑 *İşlevini gizlemenin yaptığı bir butonu koymamak, "işlevsiz buton yok" kuralının doğru okunuşu.*
- Rozetler `PanelDisplay.NewsState()` + `_StatusBadge`: **Yayında · Yayından kaldırıldı · Kaynakta yok**.
  ⚠️ Daha önce kullanılmamış Tailwind sınıfı yazılırsa **`npm run build`** — yoksa buton
  **beyaz üstüne beyaz** çizilir (12.10 canlı bulgusu).

#### 🔴 Karar: yöneticinin düzenleyebilecekleri + "override bayatlaması"

| Alan | Karar | Gerekçe |
|---|---|---|
| **Başlık** | ✅ Override | WP başlıkları SEO uzunluğunda ve **BÜYÜK HARFLE**; mobil kart 2 satır |
| **Özet** | ✅ Override | WP `excerpt`'i HTML parçalı gelir, kartta çirkin |
| **Kapak görseli** | ✅ Override (`files`'a yüklenen dosya) | 🔴 Ölçülen gerçek: `full` = **650px**, manşette yumuşak. Yöneticiye daha iyi görsel koyma yolu **şart** |
| **Gövde** | ❌ (ilk sürüm) | Bir override tüm içeriği dondurur ve sanitizasyonun **ikinci sahibini** doğurur. İkinci sürümde "sona not ekle" gibi **eklemeli** bir alan daha güvenli |
| **Kategori (kayıt bazında)** | ❌ | WP ile kalıcı ayrışma üretir. Karar **kategori bazında**, tek yerde |
| **Öne çıkar** | ✅ `IsFeatured` + **`FeaturedUntil`** + aynı anda tavan (5) | Tarihsiz "öne çıkan" 3 ay sonra **bayat manşet** üretir ve kimse fark etmez. Tavan aşılırsa komut **sebebini söyler** (checklist §11'in sınıfı) |

Efektif değer **türetilir ve tek sahibi vardır**: `NewsProjection` →
`Title = TitleOverride ?? SourceTitle`. **Liste ile detay aynı projeksiyondan** geçer
(§7 madde 43, `EventProjection` dersi: 12.4'te iki ayrı `Select` bloğu detay ekranını sessizce
konumsuz bırakacaktı). Panel de kendi biçimini yazmaz, aynı sınıfa delege eder.

🔴 **İkinci sessiz hasar — override bayatlar.** Senkron artık ezmiyor, ama kaynakta başlık
değişince override eski metne dayanmaya devam eder ve **bunu kimse bilmez.**
→ `SourceModifiedAt > OverrideUpdatedAt` olan kayıtlar için:
- Index'te **sayaç + süzgeç**: *"Kaynağı güncellenmiş, elle düzenlenmiş 7 haber"*.
- Details'te **yan yana**: "Kaynakta: … / Panelde: …" + tek tıkla **"kaynağa dön"**.

📌 Bu, `PowerOutagesAdminController`'daki `ViewBag.UnmatchedCount` deseninin aynısı
(*"sayı görünmezse yönetici sebebini hiçbir zaman anlamaz"*). **Opsiyonel değil** — 12.3'ün
"eşleşmemiş mahalle" sayacı olmasaydı o faz da yarım kalırdı.

#### 🔴 Karar: kategori görünürlüğü — semantik DIŞLAMA'dır

```
Görünürlük kuralı: haberin DIŞLANMIŞ bir kategorisi varsa uygulamada GÖRÜNMEZ.
```

Yani "en az bir görünür kategorisi olmalı" (OR) **değil**. Bu tercihi ölçüm zorluyor: bir haber
`[49,51,52]` gibi çoklu üye. OR semantiğinde **E-Gazete'yi kapatmak işe yaramaz** — o haberler
"Haberler"e de ait olduğu için görünmeye devam eder; yönetici anahtarı çevirir, **hiçbir şey olmaz**
(§7 madde 37'nin *"panelin en sinsi yalan biçimi"*). Dışlama semantiği tek kurallı ve
yöneticinin gerçekte istediği şey.

- **Yeni kategori varsayılanı: DIŞLANMAMIŞ (görünür).** WP'de yarın açılan bir kategori sessizce
  dışlanırsa oraya giren haberler **hiç görünmez** ve sebebi hiçbir yerde yazmaz. Panel bunun
  yerine **bildirir**: *"Senkronda 1 yeni kategori bulundu: Tarım"* (12.5'in "kalkış noktası boş"
  kararıyla aynı fikir: **doldurma, bildir**).
- 🔴 **Dışlama önizlemesi zorunlu:** *"Bu kategoriyi dışlarsanız 366 haber uygulamadan kalkar
  (41'i zaten başka bir dışlanmış kategoride)."* Ve bu sayı **gerçek sorgunun kendisinden**
  gelmeli — 12.2b'nin `EstimateRecipients` dersi (§7 madde 38: önizleme "342 kişi" der, gönderim
  280 yazar, fark hiçbir yerde görünmez).
- **`ShowInFilterStrip` AYRI bir eksendir.** 15 kategori mobil şeride sığmaz; 5–6'sı şeritte,
  gerisi yalnız süzgeçte. Görünürlük ile şerit üyeliğini **tek bayrakta birleştirme**.
- 📌 **Yerel/çevre kapsam ekseni EKLENMEZ.** 12.4'ün `locationScope`'u etkinlik içindi çünkü
  etkinliğin gerçekten bir yeri var. Gazete zaten yerel; "Yerel Haberler" **bir kategori**.

#### 🔴 Karar: senkron gözlemlenebilirliği — Hangfire panosu cevap DEĞİL

`/hangfire` *"job koştu mu"*yu gösterir, *"kaç haber geldi"*yi göstermez; ayrıca `ARCHITECTURE.md`
§3 panoya erişimin kendisini bir risk olarak işaretliyor.

| İhtiyaç | Yeniden kullanılan desen |
|---|---|
| "Kaç haber geldi, ne zaman, kim tetikledi" | **`PushCampaignsAdmin` panosunun birebir kopyası** → `NewsSyncAdmin` |
| "Hata var mı" | **`ErrorLogsAdmin`** (12.1) — tekilleştirme bedava |
| Liste + süzgeç + CSV + sayfalama | `PanelCsv.CollectAsync` · `_Pagination.cshtml` · `PanelSorts` (`ThenBy(Id)`) |
| Tablo sınırsız büyümesin | `PurgeErrorLogsJob` deseninde **`PurgeNewsSyncRunsJob`** (30 gün). Günde 96 satır → yılda ~35k |

🔴 **En kritik parça — bayatlık uyarısı.** Senkron sessizce durursa uygulama eski haberi
göstermeye devam eder, uçlar 200 döner, log temizdir. → **Dashboard'a kutu:**
*"Son başarılı senkron: 12 dk önce · 5 yeni"*, eşik aşılırsa (> 2 saat) kırmızı.
⚠️ E-posta uyarısı **eklenirse kısma zorunlu** (§7 madde 36).

#### 🔴 Karar: elle senkron butonu — VAR, ama kuyruğa atan ve kilitli

Olmalı, çünkü checklist §11: *"kanalın kendisi bayrakla kapalı yoldur… panele elle tetiklenen
bir 'kanalı dene' yolu koy."* Tuzaklar ve karşılıkları:

1. **Uzun süren iş** → buton `BackgroundJob.Enqueue` yapıp **hemen döner**, yeni koşunun
   detayına yönlendirir. İstek içinde koşturmak panelin timeout'unu yer, yönetici F5'ler,
   **ikinci koşu** başlar.
2. 🔴 **Çift tıklama / eşzamanlı koşu** → kilit **veritabanında**: `completed_at IS NULL` üzerinde
   **partial unique index**. Redis kilidi **yanlış araç** — bu projede Redis bilinçli olarak
   fail-open (§7 madde 36), yani **tam yarış anında** kilidi açar. §7 madde 32'nin dersi birebir:
   *"benzersiz indeks Api/Web yarışını yakalar."*
3. **Yalan buton (§7 madde 37)** → koşu sürerken buton **kapalı çizilir ve sebebini yazar**
   (*"Bir senkron zaten çalışıyor — 14:02'de başladı"*); koşul **sunucudan** gelir (`CanTrigger`),
   görünüm kendi koşulunu yazmaz (12.2b `CanCancel` dersi). Buton ne yaptığını da söyler:
   *"Kaynaktan yeni/güncellenen haberleri çeker. Panelde yaptığınız düzenlemeler korunur."*
4. 🔴 **Aksiyon adı `Create`** (`SyncNow` **değil**): `SyncNow` hiçbir önekle eşleşmez, POST
   olduğu için `update`'e düşer (§7 madde 19). `Create` hem semantik olarak doğru ("yeni bir
   **koşu kaydı** oluştur") hem `create` iznine düşer — `PushCampaignsAdminController.Create`'in
   `Send` yerine seçilme gerekçesinin aynısı.
5. `data-confirm="…"`; satır içi `onclick` **yasak** (§7 madde 51), dinleyici `panel.js`'te.
6. `IAuditableCommand` + `AuditModule = "news-sync"` + `PanelDisplay.AuditActions` satırı.
7. 📌 **"Tam yeniden çekme" (imleç sıfırlama) butonu KONULMAZ** — ayrı ve çok daha tehlikeli;
   gerekirse CLI işi.

#### Ekranlar

```
NewsAdmin/       Index (liste+süzgeç+toplu işlem+CSV+sayfalama) · Details · Edit
NewsSyncAdmin/   Index (koşu geçmişi + son durum + "Senkronu başlat") · Details
LookupsAdmin/    + "Haber Kategorileri" akordiyon bölümü
```

- **Index sütunları:** küçük görsel (**aynalanmış**, göreli URL) · başlık (+ override varsa
  "Düzenlendi" rozeti) · kategoriler · yayın tarihi · durum rozeti · ⭐ öne çıkan.
- **Süzgeçler:** kategori · durum · "elle düzenlenmiş" · **"kaynağı güncellenmiş"** · öne çıkanlar
  · tarih aralığı · arama.
- **Sıralama:** `published_desc` (varsayılan — modülün doğal sırası) · `modified_desc` ·
  `title_asc`; hepsi **`ThenBy(Id)`**.
- 🔴 **Details'te gövde `@Html.Raw` ile BASILMAZ.** WP HTML'i `<img>` içeriyor → hem dış origin
  (CSP: 🔬 `PanelExternalOriginTests`'in regex'i **çalışma zamanında oluşan** URL'yi göremez,
  yani **test yeşil kalır ama tarayıcı görseli engeller** — boş kutu, konsolda tek ihlal,
  yöneticiye hiçbir mesaj) hem depolanmış XSS yüzeyi. Düz metin önizlemesi + "Kaynakta aç" bağlantısı.
- **Edit'te görünmeyecekler:** durum/görünürlük anahtarı · kategori seçimi · gövde.
  Her override alanının yanında **"kaynağa dön"** butonu — form içinde ikinci aksiyon olduğu için
  `formaction` + `formenctype="application/x-www-form-urlencoded"` **şart**
  (checklist §11: iç içe `<form>` tarayıcı tarafından **sessizce atılır**).
- **Global arama:** `GlobalSearch`'e `news` eklenebilir; `SourceTitle` indeksi **şart**.

**Bitti kriteri:** moderatör `news` iznini alınca listeyi görüyor, **senkron ekranını
göremiyor** · Düzenle formunda görünürlük anahtarı **yok**, Arşivle/Geri al **var** ·
`Unarchive` **`approve`** iznine düşüyor (bilerek bozup testin kırmızıya döndüğü görüldü) ·
override yazıldı → senkron koştu → **override yerinde, `Source*` güncellendi** ·
"kaynağı güncellenmiş" sayacı canlıda 0'dan 1'e çıkıyor · kategori dışlama önizlemesi
**gerçek sorgudan** geliyor ve dışlanınca haber `GET /v1/news`'ten **düşüyor** ·
"Senkronu başlat" **iki kez** basıldığında ikinci koşu **açılmıyor** ve sebebi yazıyor ·
CSV BOM + `;` ile iniyor · konsolda **CSP ihlali yok** · panelde ham İngilizce/`¤` yok.

#### ✅ Teslim edildi (12 Ağustos 2026) — canlı doğrulamayla

**Kod:** `Web/Controllers/{NewsAdmin,NewsSyncAdmin}Controller.cs` + `Views/{NewsAdmin,NewsSyncAdmin}/`
(5 görünüm) + `Views/Shared/_NewsSyncStatusCard.cshtml` (tek sahip: Dashboard · Haberler · pano) +
`LookupsAdmin`'e "Haber Kategorileri" bölümü + `PanelMenu` (2 satır) + `PanelDisplay`
(4 rozet sözlüğü + tazelik) + `PanelPermissionAttribute` (`Unarchive` öneki) +
`BulkToolbarViewModel.Visibility` (üçüncü kalıp) + `Application/Features/News/`
(`NewsStates` · `NewsSearch` · `NewsAdminProjection` + 4 panel sorgusu + `INewsSyncQueue`) +
`Infrastructure/Jobs/{NewsSyncTriggerJob,PurgeNewsSyncRunsJob}.cs` + 2 migration +
3 test dosyası. **Backend 995 → 1034 (+39), mobil 751 (değişmedi — 12.14'e kadar mobilde tek satır yok).**

🔑 **TESLİM EDİLEN:** Haberler artık **panelden yönetiliyor**: başlık/özet/kapak override'ı,
geri alınabilir gizleme, öne çıkarma, kategori görünürlüğü ve — en önemlisi — **senkronun
sustuğunu gösteren bir yer**.

🔴 **12.12 SONRASI DENETİMİN KALAN 8 BULGUSU DA KAPATILDI (4–11).** Bir tanesi bir **ölçümle
çürüdü** ve düzeltme yine de gerekliydi (aşağıda).

🔴 **EN ÖNEMLİ KARAR: eşzamanlılık kilidi + KURTARMA birlikte yazıldı.** Plan yalnız kısmi
unique indeksi söylüyordu; o hâliyle **kalıcı bir kilit** üretirdi: süreç öldürülürse (deploy,
OOM) satır sonsuza kadar `completed_at IS NULL` kalır ve indeks **bütün gelecek koşuları**
engellerdi — hiçbir hata vermeden, yalnız haberler akmayı bırakarak. Yani arızayı önleyen
koruma tam da o arızanın sebebi olurdu. → `ReapStuckRunsAsync` (30 dk, `ExecuteUpdate`, kaydı
**silmez kapatır**). Ayrıca buton koşuyu istek içinde çalıştırmıyor, **kuyruğa atıyor**
(`INewsSyncQueue`): istek içinde koşsaydı panelin zaman aşımı dolar, yönetici F5'ler ve
**ikinci koşu** başlardı — engellemeye çalıştığımız şeyi butonun kendisi üretirdi.

🔬 **ÖLÇÜM BİR BULGUYU ÇÜRÜTTÜ (dürüst not).** Denetimin 4. bulgusu *"`Contains` sağlayıcıda
`strpos`'a çevrilir, hiçbir indeks karşılayamaz"* diyordu. `ToQueryString()` ile ölçüldü:
Npgsql 8 `Contains`'i de **`lower(...) LIKE @p ESCAPE '\'`** olarak çeviriyor ve parametreyi
kaçırıyor — yani ne "strpos" vardı ne de joker açığı. **Ama bulgunun SONUCU doğruydu, sebebi
başkaydı:** `lower(kolon) LIKE '%x%'` bir **btree** indeksiyle karşılanamaz, yani 12.12'nin
"aramanın çıpası" diye koyduğu indeks gerçekten çalışmıyordu. Asıl düzeltme sorgu değil
**GIN/trigram ifade indeksleri** oldu (`EXPLAIN` ile doğrulandı: `Bitmap Index Scan`).
`NewsSearch` yine de duruyor ama gerekçesi **küçültüldü ve yazıldı**: en az uzunluk kuralının
sahibi + desenin sağlayıcı çevirisine değil bize ait olması.

🐛 **BOZMA TURUNDA BİR TEST YEŞİL KALDI (ve bu bir test kusuruydu).** Sorguyu `Contains`'e geri
çevirmek `TheSearchQueryShape_CanUseTheTrigramIndex`'i kırmadı: test **ham SQL** üzerinden plan
ölçüyordu, yani bizim sorgumuza hiç bakmıyordu. İki ayağa çıkarıldı — (1) handler'ın
**gerçekten ürettiği** SQL (`ToQueryString`), (2) o şeklin indekse ulaşabilmesi (`EXPLAIN` +
`enable_seqscan=off`). 12.10'un dersi dördüncü kez: *iddiası zayıf bir test, testsizlikten kötüdür.*

🐛 **`PanelConfirmDialogTests` KIRMIZIYA DÖNDÜ VE HAKLIYDI:** `data-confirm` beş yerde
**butona** yazılmıştı; dinleyici formun özniteliğine bakıyor. 🔑 **Doğru çözüm muafiyet
listesini büyütmek değildi** (liste dosya adına bakıyor — "Edit.cshtml" yazmak projedeki
**bütün** Düzenle formlarını muaf kılardı, testin kendi yorumunun uyardığı "muafiyet çöplüğü"):
ikinci aksiyonlar forma **kardeş** yapıldı ve senkron butonları **üç ayrı forma** bölündü.
Yan kazanç: iç içe `<form>` riski ve gereksiz multipart gövde de aynı anda yok oldu.

➕ **PLAN DIŞI EKLER:** `NewsStates` (durum türetmesinin tek sahibi; `gone` > `archived`
önceliği bir *sebebi* korumak için) · `NewsAdminProjection` (panel liste **ve** ayrıntı tek
projeksiyon — §7 madde 43) · `AffectedCount`'un **iki yönlü** okunuşu ("dışlarsam kaç kalkar" /
"kaldırırsam kaç geri gelir", ikincisi yalnız **başka dışlanmış kategorisi olmayanları** sayar) ·
`NewsSyncStatuses.Skipped` + `NewsSyncOutcome.Blocked` (kilide takılan koşu **hata değil**) ·
`PurgeNewsSyncRunsJob` · Dashboard kutusu (moderatöre de açık — boş listeye bakan moderatörün
sebebi görebilmesi gerek) · `?featured=false` (denetim bulgusu 9) · indiricinin **SSRF kapısı**
(her yönlendirme sıçraması ayrı denetleniyor, iç ağ adresleri reddediliyor).

⚠️ **PLANDAN BİR SAPMA:** `News:Backfill:MaxPosts` → **`MaxTotalPosts`** (denetim bulgusu 11).
Davranış **değişmedi**, ad yanlıştı: ayar "arşiv derinliği" değil **toplam kayıt tavanı** gibi
davranıyor ve eski ad *"derinliği 200 yaptım, 50 haber geldi"* sürprizini kuruyordu. Eski
anahtar yedek olarak okunmaya devam ediyor.

**Canlı doğrulama (Chrome + gerçek Postgres + gerçek kaynak, 12 Ağustos 2026):**
Dashboard kutusu "Taze · 50 yayında / 50 toplam" · başlık override'ı yazıldı → **elle senkron
koşturuldu** (`manual`, tamamlandı) → **override yerinde**, `/v1/news` etkin başlığı döndürüyor ·
"Spor" dışlandı → önizleme **"5 haber kalkar"** dedi, `/v1/news` **50 → 45** düştü, kategori
uçtan **kaybol**du ve diğer sayaçlar da düştü (Haberler 38→34) · dışlama kaldırıldı → **50**,
ters önizleme **"5 haber geri gelir"** · haber gerekçeyle kaldırıldı → liste 49, detay **404** →
geri alındı → 50 · ikinci "çalışıyor" satırı veritabanı tarafından **reddedildi**
(`ux_news_sync_runs_single_active`) ve panel butonu **kapalı çizilip sebebini yazdı** ·
denetim izinde `archive`/`unarchive`/`sync` **Türkçe** görünüyor · konsolda **CSP ihlali yok**.
**`dotnet test` 1034/1034.**

**Kuralı bilerek boz → 5 deneme, 4 kırmızı:** `Unarchive` öneki silindi → 2 test ·
joker kaçışı kaldırıldı → 3 test · `NewsStates` önceliği ters çevrildi → 1 test ·
kilit indeksinden `UNIQUE` düşürüldü → 2 test. **5.'si (arama sorgusunu `Contains`'e geri
çevirmek) YEŞİL KALDI** → test güçlendirildi (yukarıda).

⏭️ **Sırada 12.14** — mobil. ⚠️ `?featured=false` ve `search` en az uzunluğu kontrata girdi
(`API_CONTRACT.md`).



---

### 12.14 — Haberler: mobil — [x] ✅ TAMAMLANDI (12 Ağustos 2026)

**Hedef:** 12 modüllük ızgaraya **13.'sü** olarak Haberler'in girmesi. → Girdi; ayrıca plan
dışı üç ek yapıldı (aşağıda ayrı başlık).

#### Yapılanlar (plan)

- **Modül kaydı** `kAppModules`'a eklendi (`news`, `/haberler`, `ready: true`), ızgarada
  **Duyurular'ın hemen ardında** — ikisi de "şehirde ne oluyor" sorusunun cevabı ve haber,
  duyurunun *dış kaynaklı* kardeşi.
- **Rotalar:** `/haberler` + alt rota `/haberler/:id` (detaydan geri liste konumuna dönsün
  diye), **kardeş** rota `/kaydedilen-haberler`.
- **Liste:** `PagedFeedController` (yeniden yazılmadı) + `AppScaffold` + `PagedListFooter`,
  her uç provider'ında `retry: apiRetry`. Kategori şeridi sunucudan besleniyor, süzme
  **sunucuda** (`?categoryId=`), arama ve kategori **tek filtre nesnesinde** (`NewsFilter`).
- **Detay:** kapak (16:9) · **tüm** kategoriler · başlık · tarih + okuma süresi + "Güncellendi"
  · `flutter_html` gövde · "Kaynakta oku" + kaynak künyesi · paylaş.
- **`flutter_html: ^3.0.0`** eklendi (planın `pub add --dry-run` ölçümü doğrulandı).
- **Bildirim eşlemesi** (`news → /haberler/:id`) **bu sürümde** yazıldı. 12.15'e bırakılsaydı
  §7 madde 18'in kabul edilen sınırı (*eski sürümler türü tanımaz, dokununca hiçbir yere
  gitmez*) bir sürüm daha uzardı.

#### 🔴 Plan dışı üç ek (kullanıcı sözleşmesi: "serbest, ama raporla")

1. **Manşet şeridi (`?featured=true`).** Panelde "öne çıkar" anahtarı 12.13'te yazılmıştı ve
   uç onu süzüyordu, ama **mobil karşılığı yoktu** → yöneticinin bastığı anahtar hiçbir işe
   yaramıyordu (§7 madde 37'nin *"panelin en sinsi yalan biçimi"*). Şerit yalnız **süzgeçsiz**
   listede çizilir (kullanıcı "Spor" seçmişken başka kategoriden manşet basmak, süzgecin
   çalışmadığı izlenimi verirdi) ve **alınamazsa sessizce hiç çizilmez** — ana liste aynı
   haberleri zaten taşıyor, hata mesajı göstermek kullanıcıya çözemeyeceği bir sorunu
   anlatmak olurdu. Kart oranı kaynağın `full` boyutuna (650×368 ≈ 16:9) **bilerek** yakın:
   farklı bir oran, zaten sınırlı olan görseli ayrıca kırpardı.
2. **"Bu kategoriden" (ilgili haberler).** Yeni uç **gerektirmedi** — var olan `?categoryId=`
   kullanıldı. Okunan haber listeden **elenir** (yoksa kullanıcı zaten açık olan habere geri
   dönen bir kart görürdü) ve bu yüzden tavandan **bir fazlası** istenir. Boş/hatalı durumda
   bölüm **hiç çizilmez**.
3. **"Kaydedilenler" — çevrimdışı çalışan yerel yer imi listesi.** Sunucuda tutmak
   `[Authorize]` demek olurdu; oysa bu uygulamada **misafir gezinme birinci sınıf** (11.3) ve
   "sonra okurum" en çok misafirin ihtiyacı. 🔑 **Kaydın anlık görüntüsü** saklanıyor, yalnız
   kimliği değil → §7 madde 62.

#### 🔴 Doğan görünmez sözleşmeler

- **61 — haber gövdesinin tek çizim sahibi `NewsBody`.** İstemci **ikinci beyaz liste yazmaz**
  (temizlik alım anında sunucuda), `<a>` `url_launcher`'a bağlı, `<img>` önbellekli ve
  **açılmazsa hiç yer kaplamıyor**. Üçü de bozulunca sessiz: kaybolan etiket, ölü bağlantı,
  kırık kutu. ⚠️ Metin arası görseller **aynalanmıyor** ve %9'u süreli `fbcdn` linki (12.12
  ölçümü) — yani zamanla mutlaka 403 olacaklar.
- **62 — "Kaydedilenler" kaydın ANLIK GÖRÜNTÜSÜNÜ saklar.** Yalnız `id` saklansaydı kaynakta
  yayından kalkan haber listede *"bulunamadı"* satırına dönüşürdü: kullanıcı neyi kaydettiğini
  bile göremezdi. Gövde saklanmaz (tek haber 11 KB'a çıkıyor, `SharedPreferences` **bütün
  dosyayı belleğe alır**), liste **tavanlı** (100) ve **bozuk tek satır listeyi düşürmez**.

#### 🐛 Bu oturumda bulunanlar

- **`app_modules_test.dart`'ın faz deseni çürümüştü.** `expect(module.phase, matches(RegExp(r'^11\.\d+$')))`
  yazıyordu; iddia "alt-faz dolu ve biçimli" olmalıyken **faz numarasını çiviliyordu** ve 13.
  modül (12.14) testi kırdı. Kuralın kendisi değil, **elle tutulan deseni** eskimişti —
  `CODE_REVIEW_CHECKLIST` §2'nin *"bir taramanın kapsamı da elle tutulan bir listedir"*
  dersinin küçük tekrarı. Desen `^\d+\.\d+[a-z]?$` yapıldı.
- 🔬 **Taşma testi yanlış şeyi kilitliyordu.** Bozma turunda `NewsCard`'ın meta satırından
  `Flexible` kaldırıldı ve test **yeşil kaldı**: gerçek veride o metinler (`"3 saat önce"`,
  `"4 dk okuma"`) hiçbir zaman satırı taşıracak kadar uzun olmuyor. Kartın asıl riski taşma
  değil **sınırsız büyüme**ymiş → `maxLines`/`overflow` doğrudan iddia edildi ve o bozma
  **kırmızıya döndü**. (`NewsFeaturedCard`'ın taşma testi ise gerçekten kilitliyor: `Flexible`
  kaldırılınca kırmızı.) Ders checklist'e yazıldı.
- **Test fixture'larında görsel kullanılamıyor:** `CachedNetworkImage`'in yer tutucusu sonsuz
  shimmer çalıştırıyor ve `pumpAndSettle` kilitleniyor. Projedeki diğer kart testleri de
  görselsiz — sebep ilk kez burada yazıya döküldü. Kartın **görselli** düzeni ayrı bir dosyada,
  `pump()` ile denetleniyor.
- **Canlı denetimde:** simülatör penceresi taşındığı anda sabit ekran koordinatları sessizce
  yanlış yere tıklıyordu. Yardımcı betik artık pencere konumunu **her çağrıda yeniden okuyor**
  (`scratchpad/pt.sh`) — bu bir ürün hatası değil, denetim aracının hatasıydı.

#### Testler

**+63 mobil test** (751 → **814**): `news_article_test` (14) · `news_card_test` (8) ·
`news_screen_test` (16) · `news_detail_screen_test` (14) · `news_body_test` (7) +
**erişilebilirlik** (3: kart 1.4 ölçekte taşmıyor · gövde 1.4 ölçekte taşmıyor · "Kaydedildi"
metinle de söyleniyor) + golden (`news_card_light/dark` · **`news_body_light/dark`**).
Backend **1034** (değişmedi — 12.14 sunucuya dokunmadı; `ArchitectureDocTests` mobil `news/`
klasörünü doküman güncellenene kadar bilerek kırmızı tuttu).

🔑 **Gövde golden'ı ilk yazımda atlanmıştı** (plan onu açıkça istiyordu) ve eklendiğinde
**gerçek bir riski kilitlediği** görüldü: `body` stilinden `color` kaldırılınca paket kendi
siyahını basıyor ve **koyu temada metin siyah üstüne siyah** oluyor — ekran açılır, hata
vermez, yalnız okunamaz. Bozma turunda golden kırmızıya döndü. ⚠️ Gövde senaryosu bilinçli
olarak **kısa ve sabit**: `flutter_html` çıktısı sürümle kayabilir ve uzun bir referans her
yükseltmede kırılıp insanı `--update-goldens` refleksine iterdi.

**Bozma turu (kuralı boz → kırmızıya dönüyor mu):** `onLinkTap` kaldırıldı ✅ · `<img>`
uzantısı kaldırıldı ✅ · 2 karakter eşiği kaldırıldı ✅ · ilgili haberlerde okunan haber
elenmedi ✅ · başlıktan `maxLines` kaldırıldı ✅ · `NewsFeaturedCard`'dan `Flexible`
kaldırıldı ✅ · `NewsCard`'dan `Flexible` kaldırıldı ❌ (yukarıda).

**Canlı doğrulama (emülatör + panel):** ızgaradan Haberler açılıyor · 54 haber listeleniyor,
görseller ve göreli tarihler doğru · detayda gövde biçimli, **metin arası görsel** çiziliyor,
"Kaynakta oku" ve "Bu kategoriden" çalışıyor · **panelden bir haber "Öne çıkar" yapıldı →
`/v1/news?featured=true` 1 kayıt döndü → mobilde pull-to-refresh sonrası "Öne çıkanlar"
manşeti çıktı** (uçtan uca zincir) · senkron canlı akıyor (koşu sırasında yeni haber düştü).

📌 **Kalan borç:** yazı boyutu ayarı (okuma konforu) ve metin arası görsellerin aynalanması
ikinci sürüme bırakıldı (blok listesi 12.15 sonrasında).

#### ➕ 12.14b — kapatılan iki borç (aynı oturum, kullanıcı isteği)

**1. Metin arası görseller artık aynalanıyor** (backend). 12.12 bunu bilinçli bir borç olarak
ertelemişti; borcun **son kullanma tarihi** vardı: gövde görsellerinin **%9'u imzalı/süreli**
`fbcdn`/`outlook` linki → mutlaka 403'e düşecekler ve istemci onları *zarifçe gizlediği* için
(§7 madde 61) hasarın **hiçbir belirtisi olmayacaktı**.

- `NewsBodyImages` (saf): `<img src>` bulma + yeniden yazma. Regex kullanıyor ve tek gerekçesi
  girdinin **rastgele HTML değil** kendi temizleyicimizin dar çıktısı olması — bu **görünmez**
  bir bağımlılık, testler gerçek sanitizer çıktısıyla besleyerek kilitliyor.
- `NewsImageMirror.MirrorToUrlAsync` + `files.metadata` üzerinden **koşular arası
  tekilleştirme** (gövde görselleri `source_image_url`'de görünmez — o kolon yalnız kapağı tanır).
- `MirrorNewsBodyImagesJob` (saatlik, turlu, idempotent): **12.14 öncesinden** kalan kayıtları
  onarır. Senkron yalnız *kaynakta değişen* haberi yeniden yazdığı için o kayıtlar başka türlü
  hiç düzelmezdi — ve tam da onların görselleri en eski, yani çürümeye en yakın olanlar.
- 🔴 **En önemli sıra kararı:** sağlama **aynalamadan ÖNCE**, kaynağın gövdesiyle hesaplanıyor.
  Sonrasıyla hesaplansaydı aynalanmış gövde kaynağınkine hiçbir zaman eşitlenemez ve her koşu
  haberi "değişmiş" sayıp **sonsuza kadar** yeniden yazardı. Bozma turunda kırmızıya döndü.
- 🔴 **Yeniden deneme YOK** ve sebebi ölçüme dayanıyor: imzalı bir adresin hatası **kalıcıdır**;
  sağlamaya "eksik kaldı" yazmak günde **96 boşuna istek** demekti. Geçici hata bir sonraki
  içerik değişiminde ya da işin elle tetiklenmesiyle telafi ediliyor.
- 🐛 **İki test kırıldı ve kırılma doğruydu:** yeni tekilleştirme koşular arası çalıştığı için
  görsel adresi paylaşan iki test, ikincisinde indiriciye hiç uğramadı. Her teste kendi adresi
  verildi (temizliği genişletmekten ucuz) ve ders checklist'e yazıldı.

**2. Okuma boyutu** (mobil, plan dışı ek). Uygulama sistem ölçeğine zaten saygı duyuyor ama
sistem ölçeğini değiştirmek **bütün telefonu** değiştirmek demek; haber okuyan biri yalnız o
metni büyütmek ister. Denetim bir **döngü değil seçim listesi** (döngüsel bir "A" butonu kaç
adım kaldığını söylemez). 🔴 Çarpım **tavanlı** (`kNewsMaxTextScale = 1.6`): tavansız
1.4 × 1.3 = 1.82 olur ve ekranın en dar yerleri hiç denenmemiş bir ölçekte çizilirdi.
Ölçek **yalnız başlık ve gövdeye** uygulanıyor — rozetler ve meta satırı düzenin taşıyıcısı.
🐛 Kalıcılık iddiası widget testinde **flaky**ydi (sahte saat `SharedPreferences`'ın platform
kanalını beklemiyor) → denetleyici seviyesine taşındı.

**Testler:** backend 1034 → **1053** (+19), mobil 814 → **821** (+7).
**Bozma turu:** sağlama sırası ✅ · aynalanmış adresin yeniden indirilmesi ✅ · geri
doldurmanın sağlamaya dokunması ✅ — üçü de kırmızıya döndü.

---

### 12.14 — özgün plan (referans)


**Hedef:** 12 modüllük ızgaraya **13.'sü** olarak Haberler'in girmesi.

- **Modül kaydı** `lib/core/navigation/app_modules.dart` → `kAppModules`'a
  `AppModule(id: 'news', label: 'Haberler', route: '/haberler', ready: true, …)`.
  `app_modules_test.dart` kartın açılabilir bir ekrana gittiğini kendiliğinden denetler.
- **Rotalar kardeş:** `/haberler` ve `/haberler/:id` (§7 kod-dışı: iç içe rota üst ekranı da kurar).
  ⚠️ Sekme **alt** rotasına gezinme `AppNav`'dan (§7 kod-dışı, 12.3'ün çökme kök nedeni).
- **Liste:** `PagedFeedController` (yeniden yazma) + `AppScaffold` + `PagedListFooter`.
  Her uç provider'ına **`retry: apiRetry`**.
- **Kategori şeridi:** sunucudan gelen `ShowInFilterStrip` kategorileri + **"Tümü"**.
  🔴 Süzme **sunucuda** (`?categoryId=`), istemcide değil — yoksa `totalCount` ve sonsuz
  kaydırma **yalancı** olur (checklist §5). Şerit + arama **tek filtre nesnesinde**
  (`IntercityFilter`/`GuideFilter` deseni) — ayrı tutulursa şeride dokunmak aramayı sessizce düşürür.
  Boşalan liste **sebebini söyler** ("hiç haber yok" ≠ "bu kategoride haber yok").

#### 🔴 Görseller — kullanıcının sorusunun ölçülmüş cevabı

Kullanıcının önerisi doğru yönde ama kaynak beklenenden **fakir**:

- **Liste kartı → `medium` (300×170).** 360dp genişlikte tam genişlik kart 3x'te ~1080px ister;
  300px yetmez → kartlar **küçük görselli** (ör. 96–120dp) tasarlanmalı, tam genişlik manşet değil.
  ⭐ **Öne çıkan** haber tam genişlik olacaksa **`full`** kullanılmalı.
- **Detay → `full` (650×360).** 🔴 Bu bile 3x'te yukarı ölçeklenir; **kabul edilen bir yumuşaklık**.
  Yöneticinin `CoverImageFileIdOverride` ile daha iyi görsel koyabilmesi (12.13) tam bu yüzden var.
- `cached_network_image` zaten bağımlılıkta; `AppImage.url` mutlak/göreli ayrımını **zaten
  doğru** yapıyor — aynalanmış görseller göreli geldiği için §7 madde 9 korunuyor.
- Görsel açılmazsa **zarif yer tutucu** (metin arası hotlink'ler süreli — %9'u `fbcdn`).

#### 🔴 İçerik gösterimi — `flutter_html`

- **`flutter_html: ^3.0.0`** (Flutter 3.44.2 ile temiz çözüldüğü `pub add --dry-run` ile
  doğrulandı: `+ flutter_html 3.0.0`, `+ list_counter 1.0.2`).
- Gövde **sunucuda temizlenmiş** geliyor (12.12) → istemcide ikinci bir beyaz liste **yazılmaz**
  (tek sahip kuralı). İstemci yalnız **stil** verir (tipografi token'ları, koyu tema).
- `<a>` dokunuşu → `url_launcher` (`AppLinks` deseni). `<img>` → `cached_network_image`.
- ⚠️ **Golden testte dikkat:** `flutter_html` çıktısı sürüm değişiminde kayabilir. Detay
  ekranının golden'ı **sabit, kısa bir gövdeyle** kurulmalı; uzun Türkçe metin senaryosu
  **kartlar** için (§8 kuralı).
- ⚠️ Tarih gösteren kartlara **`now` enjekte edilebilmeli** (bu projede **4 kez** tekrarlamış
  golden tuzağı).
- 🔑 **Alternatif ölçüldü ve reddedilmedi, ertelendi:** gövdeyi sunucuda **blok JSON**'a
  (`[{type:"paragraph"},{type:"image"}]`) çevirmek de mümkündü — korpusta yalnız
  `p/figure/img/strong/a` var. Daha saf olurdu (paket yok, golden kararlı, XSS yüzeyi sıfır)
  ama gazete yarın tablo/gömülü içerik kullanırsa **sessizce kaybolurdu**. `flutter_html`
  şüphede kalınca **göstermek** yönünde — projenin "additive alanın yokluğu kaydı gizlememeli"
  ilkesiyle aynı yön.

#### Testler

Model ayrıştırma · liste boş/yükleniyor/hata · kategori şeridi **uca gidiyor** ve aramayı koruyor ·
görsel yokken kart bozulmuyor · 1.4 yazı ölçeğinde **taşma yok** (bu projede 7+ kez tekrarlamış
`RenderFlex` sınıfı) · Türkçe hata sözlüğü (`turkish_ui_test.dart`) · golden: liste kartı +
öne çıkan kart + detay (açık/koyu, 1.0 ve 1.4).

**Bitti kriteri:** emülatörde ızgaradan Haberler açılıyor, 50 haber listeleniyor · kategori
şeridi süzüyor ve **sayfalama tutarlı** · detayda gövde biçimli render ediliyor, görseller
açılıyor, bağlantılar tarayıcıda açılıyor · **görsel olmayan haber** kartı bozmuyor ·
`flutter analyze` 0 · `flutter test` yeşil.

---

### 12.15 — Haberler: bildirim — [x] ✅ TAMAMLANDI (13 Ağustos 2026)

**Hedef:** Yöneticinin bir haberi **tek tıkla** push olarak gönderebilmesi.

#### 🔴 Karar: `relatedType = "news"` (kullanıcı seçti) — ve bilinen sınırı

Karşı öneri **değerlendirildi ve reddedildi**: kesinti modülünün yaptığı gibi (§7 madde 41)
haber bildirimini **bir Duyuru** olarak üretmek, `relatedType="announcement"` demek. Avantajı:
**eski sürümler dâhil herkes** dokununca bir yere gider. Bedeli: her haber push'u bir
`Announcement` satırı açar → **haber, Duyurular listesinde de görünür** → iki modül birbirine
karışır. Kullanıcı bu bedeli ödemek istemedi; karar onun.

⚠️ **Kabul edilen sınır (§7 madde 18):** mağazadaki **eski sürümler** `news` türünü tanımaz →
bildirimi listede **okur**, dokununca **hiçbir yere gitmez** ve hata da almaz.
🔑 **Zorunlu hafifletme:** push gövdesi **kendi kendine yeterli** olmalı (başlık + özetin ilk
cümlesi) — eski sürümdeki kullanıcı gezinemese bile **bilgiyi almış** olsun. Gövdesi
*"Detay için dokunun"* diyen bir bildirim, o sürümlerde **yalan söyler**.
⚠️ `app_notification.dart`'taki eşlemeye `news → /haberler/:id` eklenir (12.14 ile aynı sürümde).

#### Gönderim

- **ELLE, otomatik değil.** Günde 5 haber otomatik push'a çevrilirse kullanıcı bildirimleri
  **tümden kapatır** ve o andan sonra *kesinti* bildirimini de almaz — yani otomatik haber
  push'u **başka modüllerin bildirimlerini zehirler**. 5/gün için tek tık yeter.
- 🔴 Hedeflemenin **tek sahibi `INotificationDispatcher`** (§7 madde 38) — ikinci bir gerçekleme
  **yazılmaz**. Hedef `all` (haberin mahallesi yok); "kaç kişiye gidecek" önizlemesi
  **aynı sorgudan** (`EstimateRecipientsAsync`).
- `PushCampaignSources`'a `News = "news"` (additive) + `PanelDisplay.PushSources`'a Türkçe satır —
  yoksa Bildirim Gönderimleri panosu **"Bilinmeyen kaynak"** basar ve `PanelDisplayTests` kırılır.
- Panel deseni **`PowerOutagesAdminController`**: buton yalnız **arşivlenmemiş + kaynağı yayında**
  kayıtta aktif; gönderilmişse buton **yerine** "Gönderildi → kampanyaya git"
  (§7 madde 37: terminal alan geri alınmayı **teklif etmez**); koşul **sunucudan** gelir;
  mesaj **sayıyı söyler** (*"Bildirim 1.243 kişiye yazıldı"* / *"Hedeflemeye uyan kullanıcı yok"*).
- 🔴 **Temizlik:** haber arşivlenirse/`gone` olursa bildirimleri de **fiziksel** düşer
  (§7 madde 24/41 — 11.15c'de silinen duyurunun **9 ölü bildirimi** canlıda yaşandı).
  Güncelleme **ikinci bildirim üretmez** (bir başlık düzeltmesi şehre ikinci push atmamalı).

📌 **Kategori bazlı abonelik: bu alt-fazda YOK.** Kullanıcının istediği özellik, ama yeni bir
`NotificationPreferences` ekseni demek → mobil sürüm + eski sürümlerde tanımsız davranış +
`INotificationDispatcher`'ın hedefleme sözleşmesinin genişlemesi. **Kendi alt-fazını hak ediyor**
(12.16 adayı) ve önce 12.15'in elle gönderimi canlıda doğrulanmalı.
⚠️ Genişletildiğinde **ikinci bir dispatcher yazılmaz**, var olan **tek sahip genişletilir**.

**Bitti kriteri:** panelden bir haber gönderiliyor → kampanya satırı açılıyor, sayaçlar artıyor ·
emülatörde bildirim düşüyor, dokununca **haber detayı** açılıyor · aynı haber **ikinci kez**
gönderilemiyor (buton yerine bağlantı) · haber arşivlenince bildirimleri düşüyor ·
bildirimi kapatmış kullanıcı **almıyor** · gövde **tek başına anlamlı**.

#### ✅ Teslim edildi (13 Ağustos 2026)

**Yapılanlar (plan):**

- **`PushCampaignSources.News`** (additive) + `PanelDisplay.PushSources["news"]` Türkçe rozeti
  + denetim izinde ayrı satır (`send-notification` → *"Haber bildirimi gönderdi"*).
  🔑 `manual`'dan **ayrı** bir kaynak olmak zorundaydı ve fark `SourceId`: elle gönderimin
  gidilecek bir kaydı yok, haberinkinin var — ve o kimlik "ikinci kez gönderilemez"
  kuralının veritabanındaki çıpası.
- **`SendNewsNotificationCommand`** — hedeflemenin tek sahibi `INotificationDispatcher`
  (§7 madde 38), hedef `all` (haberin mahallesi yok), `relatedType`/`NotificationType`
  = `"news"`, `RelatedId` = haber kimliği.
- **Panel deseni `PowerOutagesAdminController`'dan**: buton yalnız gönderilebilir kayıtta
  aktif, gönderilmişse **buton yerine** "Gönderim kaydına git", koşul **sunucudan**
  (`NewsNotificationPreviewDto`), mesaj **sayıyı söyler** ("Bildirim 3 kişiye yazıldı" /
  "hedeflemeye uyan kullanıcı bulunamadı").
- **Temizlik:** haber arşivlenirse ya da `gone` olursa bildirimleri **fiziksel** düşer;
  güncelleme ikinci bildirim üretmez (üretemez — işaret terminal).
- Mobil tarafta **tek satır değişmedi**: `news → /haberler/:id` eşlemesi 12.14'te yazılmıştı.

#### 🔴 Karar 1: kolon `announcement_id` DEĞİL — reddedilmiş tasarım DÜŞÜRÜLDÜ

12.12 `news_articles.announcement_id`'yi *"haberin bildirimi bir **duyuru** olarak açılır"*
varsayımıyla açmıştı (kesinti modülünün yolu, §7 madde 41). **12.15 o yolu reddetti** —
her haber push'u bir `Announcement` satırı açardı, yani haber **Duyurular listesinde de**
görünür ve iki modül birbirine karışırdı (kullanıcı kararı).

Kolon **hiç yazılmamıştı** (canlıda ölçüldü: 56 satırın 56'sı `NULL`) ve adı artık
reddedilmiş bir tasarımı anlatıyordu → düşürüldü. Yerine `notification_campaign_id` (FK →
`push_campaigns`, `SetNull`) + `notification_sent_at` + `notification_sent_by` +
`notification_recipient_count` geldi, dördü de **`init`** (§7 madde 53'ün deseni).
🐛 **EF bu göçü RENAME olarak üretmek istedi** (ikisi de nullable `uuid`; sezgisel
eşleştirme). Teknik olarak çalışırdı ama **niyeti gizlerdi** — migration elle `DropColumn`
+ `AddColumn`'a çevrildi.

#### 🔴 Karar 2: gönderim TERMİNAL ve kural ÜÇ katmanda birden yaşıyor

Panelin butonu (görünüm) · komutun `NotificationSent` denetimi (sunucu) ·
**`push_campaigns` üzerinde kısmi unique indeks** (`source = 'news' AND source_id IS NOT NULL`).

🔑 **Üçüncüsü neden gerekli:** ilk ikisi bir **yarışı** yakalayamaz. Gönderim ile işaretleme
aynı `SaveChanges` içinde değil — kampanya kimliği ancak dispatcher yazdıktan **sonra**
doğuyor. İki eşzamanlı istek ikisi de "gönderilmemiş" görüp **şehre iki push** atabilirdi.
⚠️ İndeksin kapsamı **bilerek dar**: duyuru/kesintide aynı kaynağa ikinci gönderim
**meşrudur** (§7 madde 37) ve genel bir unique indeks o yolu sessizce kapatırdı.
⚠️ 12.13'ün *"koruma ile kurtarma birlikte yazılır"* dersi burada **gerekmiyor** ve sebebi
önemli: senkron kilidinin aksine bu satır **terminal** — yarıda kalmış bir hâli yok.
⚠️ `MarkNotificationSent` ikinci çağrıda kaydı **değiştirmez** ve `false` döner. Sessizce
ezseydi ilk kampanyanın kimliği kaybolur, panel yenisine bağlanır ve *iki kez gönderildiği*
**hiçbir yerde görünmezdi**.

#### 🔴 Karar 3: planın koşulu EKSİKTİ — görünmezliğin ÜÇ ekseni var

Plan butonun koşulunu *"arşivlenmemiş + kaynağı yayında"* diye yazıyordu. Oysa haberin
görünmezliğinin **üç** ekseni var (§7 madde 58/59) ve üçüncüsü sinsi: **dışlanmış
kategorideki** bir haber panelde *"Yayında"* görünür ama uygulamada **yoktur**. Bildirimi
gönderilseydi vatandaş bildirimi alır, dokunur ve **boş sayfaya** düşerdi — 11.15c'de
duyurularda birebir yaşanan hasar (§7 madde 24).

Kural `NewsNotificationRules`'ta ve ölçüt **`NewsVisibility.Published` sorgusunun kendisi**;
bellek kopyası yazılmadı (ayrıştıkları an §7 madde 23). Aynı sınıfı hem önizleme hem komut
çağırıyor — ayrı yazılsalardı 12.2b'nin dersi tekrarlanırdı (*görünüm kendi koşulunu
yazarsa komutun reddedeceği bir buton çizilir*).

#### 🔴 Karar 4: gövde KENDİ KENDİNE YETERLİ (`NewsNotificationText`)

§7 madde 18'in kabul edilmiş bedelinin **tek hafifletmesi**: eski sürümler `news` türünü
tanımaz → bildirimi **okur**, dokununca **hiçbir yere gitmez**. Gövde her koşulda haberin
**ilk cümlesini** taşıyor (özet override → kaynak özeti → düz metin → **son çare başlık**)
ve **asla boş olamıyor** — `PushCampaign.Body` `IsRequired` ve FCM boş gövdeli mesajı kimi
cihazda **hiç göstermiyor**, yani özetsiz bir haberin bildirimi sessizce buharlaşırdı.
Tavan 180 karakter (500 değil): bildirim **gölgede** okunuyor.

#### 🔴 İzin: "SendNotification" öneki ELLE eklendi — §7 madde 19'un DÖRDÜNCÜ tekrarı

(`BulkApprove` 11.18 · `Archive` 12.10 · `Unarchive` 12.13 · `SendNotification` 12.15.)
Ad hiçbir önekle eşleşmiyor ve POST olduğu için sessizce **`update`**'e düşerdi. Bu, listedeki
**en ağır** sessiz yetki yükselmesi olurdu: haber ekranı izin matrisinin **içinde** (moderatöre
açık), yani *yalnız başlık düzeltme yetkisi olan bir moderatör tüm şehre push atabilirdi.*
Önek `approve`'a bağlandı; altıncı bir eylem uydurulmadı (matris beş eylem tanıyor ve
"approve" bu projede *"içeriği şehre ulaştırma kararı"* kovası).

#### ➕ Plan dışı üç ek (kullanıcı sözleşmesi: "serbest, ama raporla")

1. **Gönderim önizlemesi.** Kart, gidecek **başlığın ve gövdenin kendisini** ve
   *"@N kişiye gönderilecek"* sayısını gösteriyor. Sayı **gönderimin kendi sorgusundan**
   (`EstimateRecipientsAsync`, §7 madde 38) — ayrı bir sayım "342" der, gönderim 280 yazar
   ve fark hiçbir yerde görünmez (12.2b'nin tuzağı). Metin de gerçekten gidenle **aynı**:
   ayrışsaydı yönetici okuduğu metni onaylamış olmazdı.
2. **"Bildirimi gönderilmemiş" süzgeci** (+ listede "Bildirildi" rozeti + CSV sütunu).
   Asıl işe yarayan taraf `false`: *"bugün hangi haberi duyurmayı atladım?"* sorusunun tek
   cevabı. Yalnız "gönderilmiş" sunulsaydı süzgeç bir **rapor** olurdu, **iş listesi**
   olmazdı. Ölçüt `notification_sent_at` — `notification_campaign_id` **değil**, çünkü FK
   `SetNull` ve kampanya bir gün temizlenirse kayıt sessizce "hiç gönderilmemiş" tarafına düşerdi.
3. **Panodan habere geri bağlantı.** Haber ekranından kampanyaya bağlantı vardı, tersi
   yoktu: pano bir **çıkmaz sokaktı**, yönetici *"bu hangi haberdi?"* sorusunu ancak başlığı
   arayarak cevaplayabilirdi.

#### 🐛 Bu oturumda bulunanlar

- **EF, yeni indeksi eskisinin ÜSTÜNE yazdı.** `HasIndex(x => new { Source, SourceId })`
  ikinci kez çağrıldığında EF ikisini **aynı indeks** sayıyor: üretilen migration, duyuru
  idempotency'sinin dayandığı `ix_push_campaigns_source_source_id`'yi **DROP** ediyordu.
  Ne derleyici ne test söylerdi — yalnız `AnnouncementNotificationGenerator`'ın "bu duyurunun
  kampanyası var mı?" sorgusu büyüyen bir tabloyu tam taramaya başlardı. Çözüm **adlı aşırı
  yükleme + `HasDatabaseName`** (ikincisi de şart: snake_case eklentisi aksi hâlde
  `…_source_source_id1` bırakıyor). 🔑 Bunu yakalayan bir test değil, **üretilen SQL'i okuma
  kuralıydı** (checklist §6).
- **Mobil süitte bir test kırmızıydı ve haklıydı** (12.15 ile ilgisiz, oturumda düzeltildi):
  `transport_screen_test`'in *"sıradaki kalkış"* iddiası **duvar saatine** bağımlıydı.
  Daha önce iki kez yamanmıştı; kalan hata `soonTimes()`'ın gün taşmasını **sabit 23:30**'a
  kırpmasıydı → saat 23:30'u geçince fixture'ın ürettiği seferler **geçmişte** kalıyor, kart
  haklı olarak *"Bugünkü seferler bitti"* diyor. 🔑 Asıl hata yamanın kendisi değil **yeri**:
  ekran testi `now` enjekte edemiyor, yani iddia günün son yarım saatinde *tanım gereği*
  doğru olamaz. İddia `now` enjekte edilebilen **kart** testine taşındı (checklist §5'in
  4 kez tekrarlamış maddesi), ekrana saatten bağımsız kısım kaldı.

#### Testler

**Backend 1053 → 1099 (+46)**, mobil 821 → **822** (+1, yukarıdaki taşınan iddia).
Yeni dosyalar: `Unit/Application/News/NewsNotificationTextTests` (14) ·
`NewsNotificationRulesTests` (7) · `NewsArticleTransitionTests`'e bildirim bölümü (3) ·
`Integration/Panel/PanelNewsNotificationTests` (18, gerçek Postgres).

**Bozma turu (kuralı boz → kırmızıya dönüyor mu):** izin öneki kaldırıldı ✅ (2 test) ·
kategori dışlaması kapısı kaldırıldı ✅ (2 test) · arşivde bildirim temizliği kaldırıldı ✅ ·
`MarkNotificationSent` terminalliği kaldırıldı ✅. Dördü de kırmızıya döndü.

#### Yeni görünmez sözleşmeler

**64** (üç katmanlı terminallik) · **65** (görünmezliğin üç ekseni) · **66** (bildirim
temizliği + kendi kendine yeterli gövde). Toplam **63 → 66**.

#### ➕ 12.15b — 12.15'in bıraktığı tercih deliği (aynı oturum, kullanıcı kararı)

**Bulgu:** `NotificationDispatcher` **her kaynağı** `NotificationPreferences.Announcements`'a
bağlıyordu ve `NotificationTopic` enum'ında `news` **yoktu**. 12.15 gönderimi eklediği an bu
iki yönlü sessiz bir hataya dönüştü:
1. "Duyurular"ı kapatan kullanıcı **haber bildirimlerini de** kaybediyordu — ayar ekranı
   bunu hiçbir yerde söylemiyordu.
2. Daha kötüsü tersi: haber push'u istemeyen kullanıcının **tek çıkışı** "Duyurular"ı
   kapatmaktı, o da §7 madde 41 gereği **kesinti bildirimini** öldürüyordu. Yani 12.15'in
   *"otomatik değil, elle gönderim"* gerekçesinin (**bildirim yorgunluğu → kullanıcı hepsini
   kapatır → kesintiyi de almaz**) korktuğu senaryo **tek anahtarla** ulaşılabilir durumdaydı.

**Yapılan:** `NotificationPreferences.News` ekseni + tercihin **kaynağa göre** seçilmesi
(`PushPreferenceTopics` — tek sahip, dispatcher'ın gövdesine gömülmedi çünkü aynı cevabı
önizleme de vermek zorunda) + public DTO/PATCH alanı + mobil ayar satırı (yedinci anahtar,
ızgaradaki sırayla Duyurular'ın ardında).
⚠️ Kesinti **duyuru ekseninde bırakıldı** (§7 madde 41): kendi eksenine taşınsaydı bugün
kesinti bildirimi alanların bir kısmı hiçbir tercih değiştirmeden **sessizce** susardı.
⚠️ Bilinmeyen kaynak **bugünkü davranışa** düşer, "süzme"ye değil — §5'in *"şüphede kalınca
göster"* kuralının bilinçli **tersi**: burada bedel, tercihini kapatmış birine bildirim
göndermek.

#### 🔬 12.15b'nin en değerli anı: bir VARSAYIM ölçümle çürüdü

Alan `public bool News { get; set; } = true;` yazıldı ve testin ilk hâli *"anahtarsız JSON
`true` okunur"* diye iddia ediyordu. **Gerçek Postgres'te `false` çıktı:** tercihler
`OwnsOne(...).ToJson()` ile tek JSON kolonda saklanıyor ve **EF'in JSON materyalizasyonu
varsayılan başlatıcıyı ÇALIŞTIRMIYOR**.

Varsayıma güvenilseydi 12.15b **mevcut bütün kullanıcıları** haber bildiriminden sessizce
çıkarırdı: uçlar 200 döner, panel kampanya satırını yine açar, hiçbir hata oluşmaz — tek
belirti *"kimse haber bildirimi almıyor"* olurdu ve sebebi hiçbir yerde yazmazdı. Canlıda
ölçüldü: **13 satırın 13'ünde** anahtar yoktu. Çözüm `BackfillNewsNotificationPreference`
migration'ı (`'{"News": true}'::jsonb || mevcut` — sağ operand kazandığı için **açık tercih
ezilmez**, `WHERE` ile **idempotent**).
🔑 Test **silinmedi ve beklentisi çevrilmedi**: ölçüm belgeye dönüştürüldü
(`MissingJsonKey_MaterialisesAsFalse`), çünkü asıl kilit odur — biri yarın migration'ı
"gereksiz" sanıp kaldırırsa sebebini o test anlatır.

#### 🐛 Bu oturumda bulunan üç şey daha

- **Geri doldurma testi bozma turunda YEŞİL kaldı** ve bu dosyaya dürüstçe yazıldı:
  migration'lar bir kez koşar, test veritabanı koşular arasında yeniden kullanılır, yani
  `TheBackfill_LeftNoUserRowWithoutTheNewsKey` bir migration regresyonunu **ancak sıfırdan
  kurulan bir veritabanında** yakalar. Duman testi olarak duruyor, "kilitli" sayılmıyor.
- 🐛 **Kurulum SQL'i bir testi iddiasız bıraktı.** İki testin çakışmasını çözmek için
  `CleanAsync`'e konan onarım **bütün tabloyu** kapsıyordu ve geri doldurma testini her
  koşuda kendisi onarıyordu. Onarım yalnız kendi satırlarına daraltıldı — *"iddiası zayıf
  test, testsizlikten kötüdür"* dersinin yeni bir biçimi: bu kez testi zayıflatan şey
  **kurulumun kendisiydi**.
- 🐛 **Yeni test kullanıcıları ilgisiz bir testi kırdı.** Dört satır, seed'deki süper admini
  kullanıcı listesinin **ilk sayfasından** düşürdü ve `PanelUsabilityTests` kırmızıya döndü.
  İki taraflı düzeltildi: test kendi satırlarını **siliyor**, ve o iddia artık `?search=`
  ile satır sayısından **bağımsız** (iddia "listede süper admin var" değil, "rol Türkçe
  basılıyor" idi zaten).
- 🐛 `ExecuteSqlRaw` gövdesine JSON literali yazma tuzağına **iki kez** düşüldü: gövde
  `string.Format` gibi okunuyor, JSON'un `{`'i yer tutucu sanılıyor.

**Testler:** backend 1099 → **1106** (+7), mobil 822 (iki test 12.15b yüzünden **haklı
olarak** kırıldı ve düzeltildi: anahtar listesi ve "6 anahtar" sayısı — sayı artık listeden
**türetiliyor**). Görünmez sözleşme **66 → 67**.
**Bozma turu:** haber ekseni tekrar duyuruya bağlandı ✅ · önizleme kaynağı söylemedi ✅ ·
`PanelNewsNotificationTests`'in kullanıcısı duyuru yerine haber eksenine geçirilince eski
kurulum kırıldı ✅ (ayrımın kanıtı) · geri doldurma boşaltıldı ❌ (yukarıda).

---


## 📌 Bu blok için açık kalan / bilinçli ertelenen maddeler

### 🔍 PLANLANDI (13 Ağustos 2026) — GÖRÜNMEZ SÖZLEŞME DENETİMİ

> **Durum: FAZ 0 KOŞULDU (13 Ağustos 2026, ikinci oturum).** Tasnif tamamlandı, çıktısı
> **`Memory_Bank/Contract_Audit.md`** (67 satırlık kalıcı tablo + bulgular). Faz A ve Faz B
> henüz koşulmadı. Aşağıdaki reçete metni **olduğu gibi duruyor** — Faz 0'ın sonucu bir
> sonraki başlıkta.

#### Neden: doğru soru "testi var mı?" değil

67 maddenin **67'sinin de** bir testi var. Bu projede beş fazda beş kez kanıtlanan hata
sınıfı başka: **iddiası zayıf test.**

| Faz | Kilit neydi | Ne oldu |
|---|---|---|
| 12.11 | Kaynak taraması | Taramanın **kapsamı** elle tutuluyordu → `ExtendMyAdCommand` hiç taranmadı, dosya yeşilken ham `Status` yazıyordu |
| 12.6 | Golden | %0.5 piksel toleransı (anti-aliasing için bilinçli) **üstü çizili tek hapı yuttu** |
| 12.13 | Davranış testi | Test ham SQL'e bakıyordu, **bizim sorgumuza değil** |
| 12.14 | Taşma testi | `Flexible` kaldırıldı, test **yeşil kaldı** (gerçek veride o metinler hiç taşmıyor) |
| 12.15b | Migration + davranış | `Up()` boşaltıldı, test **yeşil kaldı** (migration bir kez koşar) |

Yani denetimin sorusu: **hangi maddenin kilidi sahte?**

#### ⚠️ Ön koşul: T1 ve T2

Denetim güvenilir bir zemin ister. **T1** (birikin test kullanıcıları) sonuçları zehirler:
bozma turunda kırmızıya dönen bir test, gerçekten bozduğumuz kural yüzünden mi yoksa satır
sayısı yüzünden mi kırıldı ayırt edilemez. **T2** ise denetimin bulacağı ilk maddenin ta
kendisi. İkisi denetimden **önce** ya da denetimin ilk adımı olarak kapatılmalı.

#### Faz 0 — Tasnif (kod okumak yeterli, test koşturmak gerekmez)

Her maddeyi **kilidinin cinsine** göre etiketle. Geçmişte delik çıkan cinsler:

| Kilit cinsi | Risk | Gerekçe |
|---|---|---|
| **Kaynak taraması** | 🔴 Yüksek | Taramanın *kapsamı* da elle tutulan bir listedir (12.11) |
| **Yalnız golden** | 🔴 Yüksek | %0.5 tolerans küçük semantik farkı yutar (12.6) |
| **Kuruluma / tek koşuya bağlı** | 🔴 Yüksek | Migration, seed, `CleanAsync` onarımı (12.15b, iki kez) |
| **Doküman testi** | 🟠 Orta | Atıfların *gerçekliğini* denetler, maddenin **doğruluğunu** değil |
| **İstemci tarafı** | 🟠 Orta | Ayrı koşucu; sunucu tarafı değişince sessizce ayrışır |
| **Davranış testi (gerçek Postgres)** | 🟢 Düşük | Gerçeği ölçer |
| **Derleyici güvencesi** (`init`/CS8852) · **veritabanı kısıtı** (unique indeks) | 🟢 En düşük | Taramanın erişemeyeceği yerde (12.11'in dersi) |

**Çıktı:** `Memory_Bank/` altında 67 satırlık bir tablo — *madde no · kilit cinsi · risk ·
kilidi taşıyan dosya*. Bu tablo kalıcı: sonraki oturumlar baştan tasnif etmez, tablodan
devam eder.

#### 🎯 Ön hipotez (tasnif başlamadan önceki şüpheliler)

Bugünkü bilgiyle en kırılgan görünenler — tasnif bunları **doğrulamalı ya da çürütmeli**:

- **51** (panel dış origin): kaynak taraması, kapsamı `Views/**`. 12.9'un kendi dersi
  taramanın kapsamı üzerineydi.
- **52** (moderasyon tek sahipliği): yapısal tarama ayağı; 12.11'de bir kez delik çıktı ve
  koruma derleyiciye taşındı — ama **tarama ayağı duruyor** ve hâlâ dosya adı deseni tutuyor.
- **50** (üstü çizili "kalktı"): golden'dan **davranış testine** taşındı, iddianın **iki
  yönlü** olduğu doğrulanmalı (çizilmeyen kadar çizilen de).
- **49–50 · 61–62** (istemci tarafı): sunucu karşılıklarıyla birlikte kırılıyorlar mı?
- **67** (geri doldurma): T2, zaten biliniyor.
- **6** ("TR günü 00:00 UTC"): projede **4 kez** tekrarlamış sınıf; kilidi bu tekrar sayısına
  yetiyor mu?
- **30** (benzersiz sıralama ayracı): test ayracın **varlığını** mı yoksa **benzersizliğini**
  mi iddia ediyor? 11.18'in dersi tam olarak ikisinin farkıydı.
- **`ArchitectureDocTests` ailesi**: atıfların gerçekliğini denetliyor, **maddenin
  doğruluğunu değil**. Kanıtı elimizde: `ARCHITECTURE.md` §4 adım 8 `permissions`/
  `role_permissions` tablolarını anlatıyor ama 12.13'te ölçüldü — o tablolar **çalışma anında
  hiç okunmuyor** (canlıda 0 satır). Doküman **çürük** ve hiçbir test bunu söylemiyor.

#### Faz A — Bozma turu (yalnız kırılgan alt kümede)

Madde başına protokol, sırayla: kuralı **boz** → **yalnız o maddenin** testini koş →
**kırmızıya döndüğünü gör** → **geri al** → tabloya işle (`kilitli` / `tesadüfen yeşil`).

⚠️ Bozma **anlamlı** olmalı: kodu derlenmez yapmak bir bozma değildir. Kuralın *ihlal edilmiş
ama çalışan* hâlini üret — gerçek bir geliştiricinin yanlışlıkla yazabileceği hâli.

#### Faz B — Bulunan delikleri kapat

🔑 **Doğru soru "testi genişletsem yeter mi?" DEĞİL**, 12.11'in dersi: *"korumayı taramanın
erişemeyeceği yere taşıyabilir miyim?"* — derleyici (`init` → CS8852), veritabanı kısıtı
(unique indeks), ya da türetilen (elle tutulmayan) bir kapsam.

⚠️ **Yapılmayacak:** 67 maddede kör bozma turu. Maliyeti birkaç oturum, bilgi getirisi ise
tasnifin işaret ettiği alt kümeden fazla değil — 🟢 etiketli maddelerin (derleyici/DB kısıtı)
bozulması zaten **derlenmiyor** ya da **veritabanı reddediyor**.

### ✅ FAZ 0 SONUCU (13 Ağustos 2026) — tasnif koşuldu

**Çıktı: `Memory_Bank/Contract_Audit.md`** — 67 satırlık kalıcı tablo (*madde · kilit cinsi ·
risk · kilidi taşıyan dosya*). Yöntem: her maddenin kilidini taşıyan dosya **açılıp okundu**;
test adı yeterli sayılmadı, **iddianın şekli** incelendi. Test koşturulmadı (Faz 0'ın tanımı).

**Dağılım:** 🟢🟢 6 (derleyici/DB kısıtı) · 🟢 45 (davranış/saf birim) · 🟠 9 · 🔴 7.
**Faz A'nın alt kümesi 16 madde**; kalan 51'de kör bozma turu yapılmayacak.

**Ön hipotezlerin sonucu:** 51 ✅ doğrulandı (tarama yalnız `Views/**`) · 52 ✅ doğrulandı
(modül kümesi türetiliyor ama **dosya adı deseni hâlâ elle**: `Update*.cs`) · 50 ❌ çürüdü
(iddia iki yönlü, golden bağımlılığı yok) · 30 iddia şekli ✅ sağlam ama **kapsamı** yalnız
Announcements · 6 🟠 kapsam eksik · 67 ✅ (T2, biliniyordu) · `ArchitectureDocTests` ailesi ✅
(atıfların gerçekliğini denetliyor, maddenin doğruluğunu değil).

🔑 **Denetimin ilk gerçek kazancı — hata sınıfının ALTINCI biçimi:** *sözleşme bir modülün
adını taşıyor, kilit başka bir modülde duruyor.* Plandaki şüphelilerden **bağımsız yedi delik**
bulundu ve hiçbiri bozma turu gerektirmedi (varlıkları kod okunarak kanıtlandı):

- **B1 · madde 16** — push `data` sözlüğünün anahtarları. Test yalnız `notificationId`'nin
  **varlığını** soruyor; `type`/`relatedId`/`relatedType` hiçbir yerde iddia edilmiyor.
  Anahtar yeniden adlandırılsa **deep-link ölür**, iki süit de yeşil kalır (mobil kendi elle
  yazdığı sözlükle test ediyor — yani madde 18 ile madde 16 arasında **bağ yok**).
- **B2 · madde 26** — `?status=pending` public uçta etkisiz olmalı. Kural **vefat** modülünde
  ölçülü, **ilan** modülünde değil; oysa 10.5'te iletişim telefonlarıyla sızan **ilandı**.
- **B3 · madde 15** — kategori filtresinin **tam eşleşme** olduğunu hiçbir test ölçmüyor.
- **B4 · madde 17** — `unreadCount`'un **filtreden bağımsız** olduğu iddia edilmiyor.
- **B5 · madde 21** — `DbSeeder.Slugify → SlugHelper` **delegasyonu** test edilmiyor; ikinci
  bir gerçekleme geri gelse 10.9–11.15b'nin `İ` hatası sessizce dirilir.
- **B6 · madde 19** — izin öneki eşlemesi `[InlineData]` ile **elle** tutuluyor, gerçek aksiyon
  kümesinden türetilmiyor. Tuzak dört kez tekrarladı ve her seferinde çözüm *listeye elle
  eklemek* oldu — yani koruma değil **ritüel**.
- **B7 · madde 6** — sözleşme üç gün alanı sayıyor, test ikisini ölçüyor (`funeralDate` yok).

📌 Ders: `ARCHITECTURE.md` §7'nin *"1–22 `InvisibleContractsTests.cs`"* satırı **yanlış** —
o dosyada 12 test var, 13–22 başka dosyalarda yaşıyor. Doküman testi bunu söylemiyor çünkü
**atıfların gerçekliğini** denetliyor, maddenin doğruluğunu değil.

### ✅ FAZ B (aynı oturum) — B1–B7 KAPATILDI

Yedi deliğin yedisi de kapatıldı ve **yedisinin de bozma turu koşuldu** (kural *ihlal edilmiş
ama çalışan* hâle getirildi → yalnız o maddenin testi koşuldu → kırmızı görüldü → geri alındı):

| # | Kapatma | Bozma | Sonuç |
|---|---|---|---|
| B1 | Anahtar adlarının tek sahibi **`PushDataKeys`** (yeni sınıf); test anahtar kümesini **düz metin** iddia ediyor — sabiti yeniden adlandırmak testi kurtarmaz | `RelatedType = "related_type"` | 🔴 ✅ |
| B2 | `PublicAdsList_IgnoresTheStatusFilter_SoPendingAdsCanNeverLeak` | `OnlyPublished && string.IsNullOrWhiteSpace(dto.Status)` | 🔴 ✅ |
| B3 | `AdCategoryFilter_IsAnExactMatch_NotAHierarchicalOne` | süzgeci hiyerarşik yaptık | 🔴 ✅ |
| B4 | `NotificationsTests` — `?limit=1`'de sayaç **2** kalmalı | `UnreadCount = items.Count(…)` | 🔴 ✅ |
| B5 | `SlugGeneration_HasASingleOwner_EvenThroughItsWrappers` | `BusinessRules.Slugify`'a `ToLowerInvariant()` kopyası | 🔴 ✅ |
| B6 | `EveryWriteAction_SaysWhatItIs_InsteadOfSilentlyFallingBackToUpdate` — kapsam **yansımayla türetiliyor** | `ActionFor`'dan `SendNotification` önekini sildik | 🔴 ✅ (test aksiyonu **adıyla** söyledi) |
| B7 | `DayOnlyDateFields_…`'a `funeralDate` ayağı | `x.FuneralDate.AddHours(3)` | 🔴 ✅ |

🔑 **B4'te bir tuzağa düşülüp çıkıldı (dürüst not):** ilk yazılan iddia *"süzgeçli ve süzgeçsiz
istek aynı sayacı versin"*di ve bu uçta **totolojidir** — süzgeç zaten "okunmamışlar" olduğu
için hiçbir makul bozma onu kıramazdı. Yani denetlediğimiz hata sınıfının (**iddiası zayıf
test**) bir yenisi üretilmek üzereydi. İddia **sayfalamaya** bağlandı (`?limit=1` → sayaç yine
2) ve ancak o zaman kırılabilir oldu.

🔑 **B6 hem düzeltme hem ölçüm oldu:** yeni yapısal test **ilk koşusunda iki gerçek vaka
buldu** — `NewsAdminController.ResetOverrides` ve `NewsAdminController.Feature`. Madde 19'un
tuzağı, sayılan dört tekrardan sonra sessizce **beşinci ve altıncı** kez tekrarlamış ve kimse
fark etmemişti. İkisinin izni de **bilinçli olarak değiştirilmedi** (davranış değişikliği bu
denetimin kapsamı değil) ama artık **yazılı**: gerekçeleriyle `deliberateFallbacks` listesinde.
⚠️ `Feature` sınırda: manşet şeridi vatandaşın ilk gördüğü yer — `approve` kovasına taşınırsa
**adı da** değişmeli. 📌 Asıl kazanç listenin içeriği değil **varsayılanın yönü**: adı bir şey
söylemeyen yazma aksiyonu artık sessizce `update`'e değil **kırmızıya** düşüyor.

**Testler:** backend 1106 → **1110**, mobil 822 (değişmedi). Görünmez sözleşme sayısı **67**
(yeni sözleşme doğmadı — var olanların kilitleri sağlamlaştı).
**Kalan:** 11 madde Faz A'ya (3 🔴: 51 · 52 · 67 — üçü de `Contract_Audit.md`'de gerekçeli).

### ✅ T1 ve T2 KAPATILDI (aynı oturum) — Faz A'nın ön koşulu tamam

**T1 — dört sınıf artık kendi `users` satırlarını siliyor.** `PanelNewsNotificationTests` ·
`PanelPushCampaignTests` · `PanelPowerOutageNeighborhoodTests` · `PushNotificationsJobTests`
(sonuncusunun **hiç** temizliği yoktu, `IAsyncLifetime` bile). Kapsam **dar**: her sınıf yalnız
kendi telefon numaralarını siler (12.15b'nin dersi — geniş temizlik başka bir testin iddiasını
iddiasız bırakır).

🐛 **İlk yazım dört testi birden kırdı ve ders tam bu maddenin konusu:** kullanıcı silme,
kampanya/duyuru temizliğinin **içine** kondu; oysa `PanelPushCampaignTests` ve
`PanelPowerOutageNeighborhoodTests`'te o temizlik `InitializeAsync`'in **sonunda** da
çağrılıyor — yani kurulum kendi kurduğu kullanıcıları siliyordu.
🔑 *Temizliğin **kapsamı** kadar **çağrıldığı yer** de sözleşmenin parçasıdır.* Kullanıcı silme
ayrı bir `CleanUsersAsync`'e alındı ve yalnız `DisposeAsync`'ten çağrılıyor.
(`PanelNewsNotificationTests` kırılmadı çünkü orada temizlik kurulumdan **önce** koşuyor.)

**T2 — karar: SQL paylaşılan bir sabite çıkarıldı** (kullanıcı seçimi; plandaki üç seçenekten
biri **ölçümle elendi**). Yeni tek sahip `Infrastructure/Persistence/NotificationPreferenceBackfill`;
migration onu çağırıyor, yeni test eski biçimli (anahtarsız) bir satırı **kendi eliyle** üretip
**aynı metni** koşturuyor.

🔬 **Planın gerekçesi YANLIŞTI ve ölçümle düzeltildi.** Yazılı sebep *"migration bir kez koşar
ve test veritabanı koşular arasında yeniden kullanılıyor"*du. Ölçüldü: `WebPanelApplicationFactory`
her koşuda **yeni** bir Testcontainers konteyneri kuruyor (`WithReuse` yok, migration'lar her
koşuda baştan uygulanıyor). Gerçek sebep başka: migration **boş** bir `users` tablosunda koşar,
satırları sonradan EF yazar ve EF her zaman **tam** JSON yazar → *anahtarsız satır test
ortamında hiç doğmaz*, yani iddia **tanım gereği vakumdu**.
🔑 Ayrım pahalıydı: yanlış sebebe inanan biri planın 2. seçeneğini (*"migration testleri için
tek kullanımlık veritabanı"*) seçerdi ve **o çözüm işe yaramazdı** — sıfırdan kurulan bir
veritabanında da eski biçimli satır yoktur. (12.13'ün dersi: *yanlış bir sebep, yanlış bir
düzeltmeden pahalıdır.*)

🔬 **Bozma turu bir şey daha ölçtü:** "açık tercih ezilmemeli" iddiasını **iki** mekanizma
birden koruyor — `WHERE NOT (… ? 'News')` ve `||` operand sırası — ve **yalnız birini** bozmak
testi **yeşil bırakıyor** (ikisi de ayrı ayrı ölçüldü), ikisi birden bozulduğunda kırmızıya
dönüyor. Bu bir test zaafı **değil**, derinlemesine savunma; ama iddianın **davranış** olarak
yazılması gerektiğini gösterdi ("tercih sağ çıkar"), gerçeklemenin şekli olarak değil —
yoksa savunmanın bir ayağını kaldıran zararsız bir düzenleme de testi kırar ve test **yanlış
şeyi** kilitler. İlk iddia (eksik anahtar tamamlanır) **tek başına** kilitli: ifade
etkisizleştirildiğinde kırmızıya döndü.

**Testler:** backend 1110 → **1111**.

### ✅ FAZ A KOŞULDU (aynı oturum) — DENETİM BİTTİ

10 maddelik kırılgan alt kümenin **hepsinde** bozma turu koşuldu (kuralı *çalışır ama ihlal
edilmiş* hâle getir → yalnız o testi koş → sonucu yaz → geri al).

**Altısı kilitli çıktı:** 18 (tanınmayan tür duyuruya düşürüldü) · 29 (`ApproveSelected` →
`BulkApprove`, 7 test kırıldı) · 49 (`days` boşken "hiçbir gün", 3 test) · 50 (gün kontrolü
düşürüldü — golden değil **davranış** testi yakaladı) · 62 (anlık görüntüden başlık silindi,
3 test) · 51/52'nin ana ayakları (görünüme `unpkg`, CSP'ye `'unsafe-inline'`, guard çağrısının
silinmesi).

**Beş delik bulundu ve kapatıldı:**

| # | Delik | Kapatma |
|---|---|---|
| **27** | Kesintinin başlangıç sınırı **yalnız panelde** kilitliydi; mobil ayak hiç iddia etmiyordu — bozma tüm mobil süiti yeşil bıraktı | `power_outage_model_test.dart` → *"tam BAŞLANGIÇ anında sürüyor"* (ayna artık iki taraflı) |
| **30** | Süpürme 8 sıralama haritasından **yalnız Announcements**'ı geziyordu; `Campaigns.end_asc`'in ayracı düşürüldü, hiçbir test kırılmadı | `EverySortMapInTheProject_…` — harita listesi `PanelSorts`'tan **yansımayla**; iki satırın tüm alanları eşit tutulup her anahtarda kararlılık ölçülüyor |
| **51** | Tarama yalnız `Views/**`; aynı bağımlılığı `panel.css`'e `@import` olarak yazmak **üç ayağı da** yeşil bırakıyordu (12.9'un yerelleştirdiği fontun sessiz dönüşü) | `NoCommittedPanelAsset_LoadsAResourceFromAnExternalOrigin` — `wwwroot` (lib hariç) **dizinden türetilerek** taranıyor, yorumlar eleniyor |
| **52** | 🔴 **12.11'in dersi bu dosyada hâlâ ayaktaydı:** modül kümesi türetiliyordu ama dosyalar `Update*.cs` **deseniyle** bulunuyordu → `ReviseAdCommand.cs` hiç taranmadı | `EveryStatusCarryingCommand_CallsTheGuard_RegardlessOfItsFileName` — kapsam **tipten** kurulur (`Status` taşıyan her `IRequest<>`), guard komutun **kendi klasöründe** aranır |
| **61** | *"İstemci kırpmaz"* iddiası **metnin** kaldığına bakıyordu; `<blockquote>` etiketleri silinince metin duruyor ve test yeşil kalıyordu | İddia doğru değişmeze çevrildi: `Html`'e giden veri sunucudan gelenin **birebir aynısı** |

🔑 **En değerli bulgu 52:** 12.11 korumayı derleyiciye taşımıştı ama **taramanın kendisi**
aynı kalmıştı ve tam olarak aynı biçimde delikti. Ders üçüncü kez doğrulandı: *bir taramanın
kapsamı da elle tutulan bir listedir* — çözüm listeyi büyütmek değil, kapsamı **türetmek**.

📌 **Beş deliğin dördü "kapsam", biri "iddia şekli".** Bu projede zayıf test, çoğunlukla
*yanlış şeye* bakan test değil, **doğru şeye ama dar bir kümede** bakan testtir.

**Sonuç: 67 maddenin tamamı bugün 🟢 ya da 🟢🟢.** Tablo: `Memory_Bank/Contract_Audit.md`.
**Testler:** backend 1111 → **1114**, mobil 822 → **824**.

### 🧹 DOKÜMAN BAKIM BORÇLARI KAPATILDI (13 Ağustos 2026, aynı oturum)

Denetim bittikten sonra açık maddeler tek tek **gerçeğe karşı** doğrulandı (kutulara değil,
koda/şemaya bakılarak). Sonuçlar:

- ✅ **`docs/openapi.json` yenilendi** — ama önce bir **iddia çürüdü**: *"üç alt-faz geride,
  `featured`/`locationScope` yok"* demiştik; ölçünce görüldü ki ikisi de **vardı**
  (şemada `Featured`/`LocationScope` olarak, büyük harfle — grep harf duyarlılığından
  yanılmıştı). Canlı Swagger ile derin karşılaştırma yapıldı: **143 uç birebir aynı, tek
  gerçek fark** `UpdateNotificationPreferencesCommand.news` alanıydı (12.15b). Dosya
  yeniden üretildi, fark **4 satır**. 🔑 Ders yine aynı: *bir iddianın sebebini de sonucunu
  da ölç* — yanlış bir "geride kaldı" teşhisi, gereksiz bir yeniden yazıma götürürdü.
- ✅ **`ARCHITECTURE.md` §4 adım 8 düzeltildi.** Adım *"izni `permissions` tablosuna ekle"*
  diyordu; kaynak taramasıyla doğrulandı ki `RolePermission` yalnız `AppDbContext` ve kendi
  yapılandırmasında geçiyor, **hiçbir sorgu ona dokunmuyor**. Gerçek yol:
  `PanelMenu.Items` → izin matrisi (`StaffAdminController.Modules` **oradan türüyor**) →
  yetkiler `admin_permissions`'a yazılıyor → `IPermissionService.HasAsync` **yalnız onu**
  okuyor. 📌 Bu madde, doküman testinin yakalayamadığı çürüme sınıfının canlı örneğiydi:
  atıfları geçerliydi, dilbilgisi sağlamdı ve **yanlıştı**.
- ✅ **22 bayat kutu hizalandı.** Denetim izi, çöp kutusu, toplu işlem, CSV, global arama,
  sütun sıralaması, oturum iptali, parola politikası, CI, `ARCHITECTURE.md`, mobil bildirim
  merkezi/FCM/deep-link… **21'i yapılmıştı, kutusu hiç işaretlenmemişti** — kutulara bakan
  biri projeyi bugün yanlış okurdu. Tek gerçekten açık kalan `uploads/` volume'ü ve o da
  **deploy fazına ait** (API compose'da değil, bağlanacak servis yok) — kutusu açık bırakıldı,
  gerekçesi yanına yazıldı.
- ✅ **`KadirliApp.Domain/Class1.cs` silindi** — iki dış analizin "şablon artığı" diye
  işaret ettiği boş sınıf; hiçbir yerden referans verilmiyordu.

### ✅ Faz A'nın bıraktığı iki küçük açık da kapatıldı

- **`Feature` izni `approve`'a taşındı** (kullanıcı kararıyla). Aksiyon adı hiçbir önekle
  eşleşmediği için sessizce `update`'e düşüyordu — yani yalnız **başlık düzeltme** yetkisi
  olan bir moderatör, mobil ana ekranın **manşet şeridini** belirleyebiliyordu. §7 madde
  19'un **beşinci** tekrarı. ⚠️ `Feature` tek anahtar (aç/kapa aynı aksiyon) olduğu için
  `Un…` çifti gerekmedi; ileride ayrı bir `Unfeature` yazılırsa **elle** eklenmeli.
  Kilit iki ayaklı: teori satırı + **davranış testi** (`Feature_IsRejected_ForAModerator…`,
  durum koduna değil **kaydın `IsFeatured` alanına** bakıyor — bir 302 hem "reddedildi" hem
  "yapıldı" olabilir). Bozma turu: önek geri alındı → **3 test kırmızı**.
- **Madde 67'nin duman testi dürüst adlandırıldı**:
  `TheBackfill_LeftNoUserRowWithoutTheNewsKey` → `SmokeCheck_NoUserRowLacksTheNewsKey_VacuousOnAFreshDatabase`.
  Eski ad bir **güvence vaat ediyordu** ("geri doldurma koştu"), oysa iddia bu ortamda
  vakum. Test silinmedi (gerçek bir ortamda değeri var) ama artık **adı sınırını söylüyor** —
  bu projede yeşil ama boş bir güvence, testsizlikten kötüdür.

### 🧹 Test altyapısı — 12.15b'nin bıraktığı iki açık madde (⬆️ İKİSİ DE KAPANDI, yukarı bak)

Bunlar ürün hatası değil, **denetim aracının** hatası; ama bu projede denetim aracının
hatası ürün hatasına dönüşüyor (yeşil kalan bir test, olmayan bir güvence).

- **T1 — Dört entegrasyon sınıfı kalıcı `users` satırı bırakıyor.**
  `PanelNewsNotificationTests` (12.15, **bu oturumda yazıldı**), `PanelPushCampaignTests`,
  `PanelPowerOutageNeighborhoodTests`, `PushNotificationsJobTests`. Test veritabanı koşular
  arasında **yeniden kullanılıyor** (migration bozma turunda kanıtlandı), yani satırlar
  birikiyor. 12.15b'de bu, dört yeni kullanıcının seed'deki süper admini kullanıcı
  listesinin **ilk sayfasından** düşürmesiyle patladı ve `PanelUsabilityTests` **kendisiyle
  ilgisiz** bir sebeple kırıldı. O tek iddia `?search=` ile bağımsızlaştırıldı ama **kaynak
  duruyor**: satır sayısına bağlı bir sonraki iddia aynı şekilde kırılacak.
  🔑 Doğru düzeltme iki taraflı ve ikisi de gerekli: (a) her sınıf kendi satırlarını
  **silsin** (`NotificationPreferenceAxisTests` deseni), (b) satır sayısına bağlı iddialar
  süzgeçle bağımsızlaştırılsın. ⚠️ Temizlik **yalnız kendi satırlarını** kapsamalı —
  aksi hâlde başka bir testin iddiasını iddiasız bırakır (12.15b'de birebir yaşandı).
  📌 Ders şu ki bu oturumda öğrenilip **aynı oturumda tekrarlandı**: 12.15'in test dosyası
  dersten önce yazılmıştı ve geriye dönüp düzeltilmedi.

- **T2 — §7 madde 67'nin geri doldurma yüzü davranış testiyle KİLİTLENEMİYOR.**
  Migration bir kez koşar ve test veritabanı yeniden kullanılır; `Up()` boşaltıldığında
  `TheBackfill_LeftNoUserRowWithoutTheNewsKey` **yeşil kaldı**. Bugün duman testi olarak
  duruyor ve sınırı dosyasında yazılı. Karar verilmedi; üç seçenek var:
  1. **Kabul et ve işaretle** (bugünkü hâl) — maliyet sıfır, güvence sıfır.
  2. Migration testleri için **tek kullanımlık bir veritabanı** (ayrı fixture): gerçek
     güvence, bedeli süit süresi.
  3. Geri doldurmayı migration'dan **açılış adımına** taşı (`*Backfill.cs` deseni, 12.3/12.4):
     idempotent olduğu için her açılışta koşar ve **davranış testiyle kilitlenebilir**.
     ⚠️ Bedeli: sonsuza kadar koşan bir açılış sorgusu ve "bu ne zaman bitecek?" sorusu.
  📌 Seçenek 3 bu projenin var olan desenine en yakın olanı; karar verilmeden yapılmamalı.


- **Kategori bazlı bildirim aboneliği** → **12.18 adayı** *(13 Ağu 2026'da 12.16'dan kaydı —
  KVKK bloğu öne alındı; yukarıda gerekçe).*
- ~~**Metin arası görsellerin aynalanması** → ikinci sürüm.~~ ✅ **12.14b'de KAPANDI**
  (`MirrorNewsBodyImagesJob` + §7 madde 63). 📌 Bu satır 13 Ağu 2026'daki açık-madde
  denetiminde **bayat** bulundu: madde kapanmıştı ama listeden düşmemişti — bu bölümün
  kendi uyardığı tuzağın (`uploads/` artıkları) birebir tekrarı.
- **Gövde override'ı** → ikinci sürümde **eklemeli** bir alan olarak (tam override değil).
- **Arşiv derinliği** bugün **50**. `News:Backfill:MaxPosts` büyütülünce geri imleç kaldığı
  yerden devam eder; 27.284'ün tamamı istenirse ~273 istek + (aynalama ile) ~1.6 GB görsel —
  **o karar ayrıca verilmeli**, kod değişikliği gerektirmiyor.
- **`docs/openapi.json` + `Memory_Bank/API_CONTRACT.md`** her alt-fazda güncellenir (§4 adım 10).
- **`ARCHITECTURE.md` modül tablosu** 12.12'de değil, modül **panelde göründüğünde** (12.13)
  tam satırını alır; `ArchitectureDocTests` aksi hâlde kırılır.

---

# ⚖️ KVKK RIZA YÖNETİMİ (12.16 – 12.17) — 13 Ağustos 2026'da planlandı

> **Nereden çıktı:** kullanıcı isteği. *"Kullanıcı kayıt olurken, kayıt bitmeden önce KVKK
> için iznini almamız gerekli — ve bu metin değişebilmeli, admin panelden düzenlenebilmeli."*
>
> 🔴 **Bugünkü durum ÖLÇÜLDÜ (13 Ağu 2026, kaynak taraması): hiçbir şey yok.**
> `kvkk|aydınlatma|gizlilik|privacy|consent|rıza` deseni `*.cs` · `*.dart` · `*.cshtml`
> dosyalarında **sıfır** işlevsel eşleşme veriyor (tek geçtiği yer `DeleteMyAccountCommand`'in
> *yorumunda*, hesap silmenin gerekçesi olarak). Yani: kayıt akışında **onay kutusu yok**,
> **metin yok**, **rıza kaydı yok**, panelde **düzenlenebilir bir belge yok**.
>
> 🔑 **Bu maddenin aciliyeti bir yasal görüşten değil, bir ZAMANLAMA gerçeğinden geliyor:**
> uygulama **henüz mağazada değil** (Apple aboneliği bekliyor, Play anahtarı üretilmedi).
> Yani bugün rızayı **zorunlu** yapmanın bedeli **sıfır**. Yayından sonra aynı şeyi yapmak
> §5'in kırıcı-değişiklik kuralına girer: `POST /v1/auth/register` yeni bir zorunlu alan
> istemeye başladığı gün, mağazadaki **her eski sürümde kayıt 400 döner** ve uygulama
> yeni kullanıcı alamaz hâle gelir. **Bu, o kapının açık olduğu son andır.**

## 📌 Neden "bir onay kutusu" değil

Bu maddenin tamamı tek bir cümleden türüyor: **metin panelden değiştirilebiliyorsa, rıza
kaydı metnin HANGİ HÂLİNE verildiğini bilmek zorundadır.**

Aksi hâlde şu olur ve **hiçbir yerde hata görünmez**: 5.000 kullanıcı v1 metnine onay verir,
yönetici metni düzenler, artık elimizde *"5.000 kişi bu metne onay verdi"* diyen bir kayıt
vardır ve **o metin artık ortada yoktur**. Kayıt teknik olarak duruyordur, kanıt olarak
yoktur. KVKK'nın istediği şey tam olarak *"neye, ne zaman, nasıl rıza verildi"*dir; bu
yüzden aşağıdaki modelin merkezinde **sürüm** var, "onaylandı" bayrağı değil.

📌 Bu, projenin zaten iki kez öğrendiği hasar sınıfının (§7 madde 55 — *"senkron panelin
yazdığını ezer"*) üçüncü biçimi: **bir kayıt, kendisini anlamlı kılan bağlamı kaybediyor.**

## ⚙️ Alınan kararlar — ✅ **KULLANICI ONAYLADI (14 Ağustos 2026)**

> Kullanıcının onayı bir kayıtla geldi: *"yeter ki projenin yapısını, mimarisini bozmayalım."*
> Bu, sekiz kararın **hepsini** bağlayan bir kısıt olarak yazıya geçiriliyor ve 12.16'nın
> bitti-kriterine ek üç madde koyuyor:
>
> 1. **Yeni modül 18 adımlı reçeteyi izler** (`ARCHITECTURE.md` §4) — atlanan adım bu projede
>    doğrudan yeni bir görünmez sözleşmeye dönüşüyor.
> 2. **Var olan hiçbir kural ikinci bir sahiple çoğaltılmaz.** KVKK bloğunun üç temas noktası
>    var ve üçü de **var olana bağlanır**, yenisi yazılmaz: rıza geri alma → var olan
>    `DELETE /v1/users/me` (10.8), izin matrisi → `PanelMenu.Items`'tan **türeyen** modül
>    anahtarı, denetim izi → `IAuditableCommand`.
> 3. **Hiçbir DTO alanı silinmez/yeniden adlandırılmaz** (§5). `register`'a eklenen `consents`
>    alanı **additive**'dir; zorunluluğu bir **yapılandırma kapısıyla** açılır ki mağazaya
>    çıkılmış olsa bile eski sürümler tek commit'te kırılmasın.
>
> ⚠️ Kod **bu oturumda yazılmadı** (kullanıcı kararı: *"geri kalanına bir sonraki oturumda
> devam edeceğiz"*). Aşağıdaki tablo artık bir *öneri* değil **onaylanmış karar** kaydıdır.

| Karar | Öneri | Gerekçe |
|---|---|---|
| **Rıza neye bağlanır?** | Kullanıcının **gördüğü sürüme** (`legal_document_versions.id`) | Sunucunun *o anki* sürümüne bağlanırsa bir yarış doğar: kullanıcı v1'i okurken yönetici v2'yi yayınlar → kayıt **okunmamış bir metne** rıza der |
| **Yayınlanmış sürüm düzenlenebilir mi?** | **Hayır** — yeni sürüm açılır | Düzenlenebilseydi rıza kaydının işaret ettiği metin sessizce değişirdi (bu bloğun var olma sebebi). İhlal **`CS8852`** olmalı (12.11/12.12 deseni: alanlar `init`) |
| **Zorunlu ↔ isteğe bağlı rıza** | **Ayrı belgeler**, `IsMandatory` bayrağı | 🔴 KVKK'nın en sık ihlal edilen kuralı: *"hizmet için gerekli işleme"* ile *"ticari elektronik ileti"*yi **tek kutuda** toplamak rızayı **geçersiz** kılar. Zorunlu olan kaydı bloklar, isteğe bağlı olan **bloklamaz** |
| **Onay kutusu ön işaretli mi?** | **Hayır** | Ön işaretli kutu KVKK'da rıza sayılmaz. Mobil tarafın testle kilitlenecek maddesi |
| **Metin değişince ne olur?** | Sürüm başına **`RequiresReconsent`** bayrağı | Yazım hatası düzeltmesi yeniden onay istemez, kapsam değişikliği ister. Karar **panelde** verilir; bayrak olmasaydı ya herkesi gereksiz rahatsız ederdik ya da esaslı bir değişiklik **hiç kimseye ulaşmazdı** |
| **Rıza geri alınabilir mi?** | İsteğe bağlı olanlar **evet**; zorunlu olanın geri alınması = **hesap silme** | Zaten var olan `DELETE /v1/users/me` (10.8) bu maddenin karşılığı — yeni bir yol açılmıyor, var olana **bağlanıyor** |
| **Hesap silinince rıza kaydı ne olur?** | 🔴 **KALIR** (kullanıcı satırı anonimleşir) | ⚠️ **12.7'nin `user_identities` kararının bilinçli TERSİ** ve fark yazılmalı: sosyal kimlik *kanıt değeri olmayan kişisel veridir* → silinir; rıza kaydı **işlemenin hukuki dayanağının kanıtıdır** → silinirse geçmişte yapılmış işlemenin dayanağı kaybolur |
| **Saklama süresi işi (`Purge…Job`)** | **YAZILMAZ** | Projedeki her yeni tabloya saklama süresi işi yazma refleksi (`CODE_REVIEW_CHECKLIST` §11) burada **yanlış** olur: kanıtı süreyle silmek, kanıtı hiç tutmamakla aynı kapıya çıkar |

## 🗺️ Alt-faz haritası

| # | Alt-faz | Katman | Şema | Tahmini test |
|---|---|---|---|---|
| **12.16** | **KVKK: belge yönetimi + rıza kaydı** ✅ | backend + panel | ✔ | ~35 backend → **gerçekleşen: 62** |
| **12.17** | **KVKK: mobil** (kayıt akışında rıza · metin ekranı · ayarlardan görüntüleme) | mobil | — | ~20 mobil |

⚠️ **Numaralandırma değişti:** *kategori bazlı bildirim aboneliği* 12.16 adayıydı,
**12.18'e** kaydı. Gerekçe kullanıcının kendi ifadesi: *"bu çok önemli, hatta çoğu konudan
daha önemli"* — ve yukarıdaki zamanlama gerçeği (mağazaya çıkmadan yapılırsa bedeli sıfır).

---

### 12.16 — KVKK: belge yönetimi + rıza kaydı — [x] ✅ TAMAMLANDI (14 Ağustos 2026)

#### Alan modeli

- **`LegalDocument : BaseEntity`** (`legal_documents`) — *belge türü*, sürümlerin sahibi:
  `Type` (`kvkk_aydinlatma` | `acik_riza` | `kullanim_kosullari` | `gizlilik_politikasi`) ·
  `Title` · `IsMandatory bool` · `ShowAtRegistration bool` · `SortOrder` · `IsActive`.
  **Unique `(Type)`.** ⚠️ Tür değerleri DTO'ya çıkar → **kontrattır** (§7 madde 47'nin
  `vehicle_type` kararının aynısı: metin saklanır, enum sırası değil).
- **`LegalDocumentVersion : BaseEntity`** (`legal_document_versions`):
  `DocumentId` · `VersionNumber int` · `Body text` (HTML) · `Summary` ·
  `PublishedAt DateTime?` · `PublishedBy Guid?` · `RequiresReconsent bool` · `EffectiveFrom`.
  **Unique `(DocumentId, VersionNumber)`** + **kısmi unique** `(DocumentId) WHERE published_at IS NOT NULL AND superseded_at IS NULL`
  → *"aynı anda en fazla bir yayında sürüm"* kuralı **veritabanında** yaşasın (§7 madde 60'ın
  kısmi indeks deseni; kodda unutulsa bile ikinci satır INSERT'te reddedilir).
  🔴 `Body`/`VersionNumber`/`PublishedAt` **`init`** → yayınlanmış sürümü değiştirmek
  **`CS8852`**; geçişler varlığın metotlarında (`Publish()` · `Supersede()`).
- **`UserConsent : BaseEntity`** (`user_consents`):
  `UserId` · `DocumentVersionId` · `Granted bool` · `GrantedAt` · `RevokedAt DateTime?` ·
  `IpAddress inet?` · `UserAgent` · `Source` (`registration` | `settings` | `reconsent`).
  **Unique `(UserId, DocumentVersionId)`.**
  ⚠️ `Granted=false` satırı da yazılır — *"sormadık"* ile *"sorduk, hayır dedi"* farkı
  KVKK'da anlamlıdır ve yalnız `true` yazılırsa bu fark **hiçbir yerde durmaz**.

#### Uçlar

- **`GET /v1/legal/documents`** *(anonim)* — kayıt ekranının göstereceği belgeler:
  tür · başlık · özet · **`versionId`** · `isMandatory` · `body`.
  🔴 **Anonim olmak ZORUNDA** — kullanıcı henüz kayıtlı değil. `EndpointAuthorizationSweepTests`'in
  anonim listesine **bilinçli** eklenir.
- **`GET /v1/legal/documents/{type}`** *(anonim)* — tek belge (ayarlar ekranından okuma).
- **`POST /v1/auth/register`** gövdesine **`consents: [{versionId, granted}]`** eklenir.
  🔴 **Zorunlu belgelerin hepsi `granted=true` gelmeden kayıt TAMAMLANMAZ** — ve eksik
  gelirse komut **sebebini söyler** (`MISSING_CONSENT` + hangi belge). Sessizce kaydetmek,
  bu bloğun kapatmaya çalıştığı hasarın ta kendisi olurdu.
  🔴 **Rıza satırları kullanıcı ile AYNI `SaveChanges`'te yazılır** (12.7'nin
  `AttachToNewUserAsync` deseni — `users.id` store-generated, bağ gezinme özelliğinden
  kurulur). Ayrı yazılsalardı araya düşen bir hata **rızasız bir hesap** bırakırdı ve
  o hesabın hukuki dayanağı hiçbir yerde olmazdı.
- **`GET /v1/users/me/consents`** · **`POST /v1/users/me/consents`** — isteğe bağlı rızayı
  ayarlardan verme/geri alma; ve **yeniden onay** akışı (`RequiresReconsent`).

#### Panel — `LegalDocumentsAdmin`

- Belge listesi → sürümler → **yeni sürüm oluştur** → **önizle** → **yayınla**.
- 🔴 **`Publish` öneki `PanelPermissionFilter.ActionFor`'a ELLE EKLENECEK → `approve`.**
  Bu, **§7 madde 19'un YEDİNCİ tekrarı** olur (BulkApprove 11.18 · Archive 12.10 ·
  Unarchive 12.13 · SendNotification 12.15 · ResetOverrides + Feature Faz 0 ·
  **Publish 12.16**). Eklenmezse POST olduğu için sessizce `update`'e düşer ve sonuç
  listedeki en ağırlarından olur: *yalnız başlık düzeltme yetkisi olan bir moderatör,
  şehrin tamamının onayladığı hukuki metni değiştirebilir.*
- ⚠️ Yayınlanmış sürüm **düzenlenemez**; form onu salt-okunur gösterir ve "yeni sürüm aç"
  der (12.10'un `_ModerationStatusField` deseni).
- **"Kaç kişi onayladı"** sayacı — sürüm başına. 🔑 Sayı **gerçek rıza sorgusundan** gelmeli,
  ayrı bir sayaç kolonundan değil (§7 madde 59'un önizleme dersi).
- **Rıza defteri** ekranı: kim · hangi sürüm · ne zaman · nereden. ⚠️ **Yalnız admin**
  (`AdminOnlyControllers` deseni — IP ve tarayıcı taşıyor, §3).
- Denetim izi: `publish_legal_version` → `PanelDisplay.AuditAction` satırı.

#### Doğacak görünmez sözleşmeler (§7 tablosuna, **71'den devam**)

| # | Sözleşme | Bozulursa ne olur |
|---|---|---|
| 71 | **Rıza, kullanıcının GÖRDÜĞÜ sürüme yazılır** — sunucunun o anki yayında sürümüne değil | Yönetici kullanıcı formu okurken yeni sürüm yayınlarsa kayıt, kullanıcının **hiç görmediği** bir metne rıza verdiğini söyler. Hiçbir hata oluşmaz; kanıt sessizce yanlış olur |
| 72 | **Yayınlanmış sürüm DEĞİŞTİRİLEMEZ** (alanlar `init`, ihlal `CS8852`); değişiklik yeni sürümdür | Değiştirilebilseydi bütün geçmiş rıza kayıtları **retroaktif olarak** başka bir metni işaret ederdi — tablo dolu, kanıt yok |
| 73 | **Zorunlu rıza kaydı ile kullanıcı satırı AYNI işlemde yazılır** | Ayrı yazılsalardı araya düşen bir hata **rızasız hesap** bırakırdı: uygulama çalışır, uçlar 200 döner ve o hesabın verisini işlemenin dayanağı **hiçbir yerde olmaz** |
| 74 | **Hesap silinince rıza kaydı KALIR** (12.7'nin `user_identities` kararının bilinçli tersi) | Silinseydi geçmişte yapılmış işlemenin hukuki dayanağı kaybolurdu. ⚠️ Kullanıcı satırı anonimleşir, rıza satırı **anonim kullanıcıya** bağlı kalır |

#### Bitti kriteri

Panelden yeni bir KVKK sürümü yayınlanıyor · yayınlanmış sürüm **düzenlenemiyor** (derleme
hatası + panelde salt-okunur) · zorunlu rıza olmadan `register` **reddediliyor ve sebebini
söylüyor** · rıza satırı **kullanıcıyla aynı işlemde** yazılıyor (bozma turu: rıza yazımı
kaldırılınca kayıt da geri alınıyor mu?) · `?type=` ucu **anonim** çalışıyor ·
`Publish` aksiyonu **`approve`** iznine düşüyor (yalnız `update` yetkisi olan moderatör
reddediliyor) · hesap silindiğinde rıza satırı **duruyor**.


#### ✅ Ne teslim edildi (14 Ağustos 2026)

**Backend 1182 → 1244 (+62), mobil 824 (değişmedi — 12.16 mobile dokunmadı).
Görünmez sözleşme 70 → 74.**

- **Üç tablo:** `legal_documents` (belgenin *kimliği*) · `legal_document_versions`
  (*metnin kendisi*) · `user_consents` (kimin hangi **sürüme** rıza verdiği).
- **İki anonim uç:** `GET /v1/legal/documents(?registrationOnly=)` ve `.../{type}`.
- **`register` gövdesinde additive `consents`** + `GET/POST /v1/users/me/consents`.
- **Panel:** *Hukuki Metinler* (matriste, `legal`) — belge ayarları · sürüm geçmişi ·
  taslak aç/düzenle · önizle · **yayınla**; ve *Rıza Defteri* (**yalnız admin**).
- **18 adımlı reçete** eksiksiz izlendi; tek bilinçli sapma aşağıda (admin API controller'ı).

#### ⚙️ Alınan kararlar

| # | Karar | Gerekçe |
|---|---|---|
| 1 | **`DbSeeder` belgelerin yalnız KABUĞUNU açar, metnini SEED ETMEZ** | Seed edilmiş bir "örnek KVKK metni" er ya da geç yayına çıkar ve vatandaş, hiçbir hukukçunun okumadığı bir metne rıza vermiş olur. 12.5'in "tahmini koordinat yazma" kuralının hukuki metin hâli: **yanlış doldurmak boş bırakmaktan kötüdür.** Sonuç: taze kurulumda zorunlu belge yok → kayıt akışı **birebir eskisi gibi** çalışıyor |
| 2 | **Yayında sürümü olmayan belge ZORUNLU OLAMAZ** (`LegalConsentRules`) | Planda yoktu, denetimde bulundu. Sayılmasaydı `IsMandatory` işaretli ama metni yayınlanmamış bir belge **kaydı tamamen kilitlerdi**: istemci gösterecek metin bulamaz, sunucu onaysız kaydı reddeder, uygulama **hiç yeni kullanıcı alamaz**. Belirti "kayıt olmuyor", sebep hiçbir ekranda yazmaz — 12.15'in *"görünmezliğin KAÇ ekseni var?"* dersinin tekrarı. Panel bu tutarsızlığı **uyarı olarak** gösteriyor, kural onu **yutmuyor** |
| 3 | **Yayında olmayan sürüme rıza YAZILMAZ** (kullanıcı formu doldururken yeni sürüm yayınlandıysa kayıt reddedilir) | Alternatifi — eski sürümün onayını kabul etmek — yürürlükten kalkmış bir metne dayanan kayıt üretirdi. Yayınlama nadir, sessizce yanlış kanıt **geri alınamaz** |
| 4 | **Public uçlar ÖNBELLEKLENMİYOR** (projede benzer sözlük uçları önbellekli) | §7 madde 22'nin hasarı burada en kötü biçimini alırdı: yönetici yeni sürümü yayınlar, vatandaş 15 dk **yürürlükten kalkmış metni** okur ve **ona** rıza verir. Uç kayıt akışında **bir kez** çağrılıyor; kazanç ihmal edilebilir. 📌 Grup açıp her mutasyona invalidator yazmak da bir sonraki komutta unutulabilirdi — kural testi *"her grubun invalidator'ı var mı"*ya bakar, *"her mutasyon invalidate ediyor mu"*ya değil |
| 5 | **`Publish` öneki `ActionFor`'a ELLE eklendi → `approve`** | §7 madde 19'un **YEDİNCİ** tekrarı. Eklenmeseydi POST olduğu için sessizce `update`'e düşerdi: *yalnız başlık düzeltme yetkisi olan moderatör, şehrin tamamının onayladığı hukuki metni değiştirebilirdi* — üstelik yayınlama **tek yönlü** |
| 6 | **Rıza defteri AYRI controller ve matris DIŞI** | `AdminOnlyControllers` deseni: satırlar **IP ve tarayıcı imzası** taşıyor (`LoginAttemptsAdmin` ile birebir aynı gerekçe). Defterde **silme/düzeltme aksiyonu yok** — düzeltilebilen bir kanıt kanıt değildir |
| 7 | **Admin API controller'ı (`/v1/admin/legal`) YAZILMADI** — reçetenin 7. adımından bilinçli sapma | Modül **panel-only**. Aynı karar 12.12–12.13'te Haberler, 12.1'de Hata Kayıtları, 12.2'de Giriş Denemeleri ve 12.2b'de Bildirim Gönderimleri için verilmişti: hiçbir istemcinin çağırmadığı bir uç kümesi, bakımı yapılmayan **ikinci bir yüzeydir** |
| 8 | **Zorunluluk kapısı `Legal:RequireConsentAtRegistration`, varsayılan `true`** | Uygulama mağazada değil → bugün bedeli sıfır. ⚠️ Değer **çözülme anında** okunuyor, DI kaydında değil — 12.7'nin bulduğu gerçek hatanın tekrarı olmasın diye |

#### 🐛 BOZMA TURU KOŞULDU — VE **GERÇEK BİR HATA BULDU**

Altı kilit tek tek bozuldu:

| Bozma | Sonuç |
|---|---|
| Doğrulama `SaveChanges`'ten **sonraya** alındı (madde 73) | 🔴 kırmızı |
| Rıza **belgeye** bağlandı (madde 71, kısmi) | 🟢 **yeşil kaldı** — bkz. aşağıdaki not |
| Sunucu sürümü **kendi** seçti (madde 71, tam) | 🔴 **5 test** kırmızı |
| `TryRevise` yayınlanmış sürümü de yazdı (madde 72) | 🔴 **4 test** kırmızı |
| Hesap silme rızaları da sildi (madde 74) | 🔴 kırmızı |
| `Publish` öneki kaldırıldı (madde 19) | 🔴 kırmızı |

🔬 **İkinci bozmanın yeşil kalması bir delik DEĞİL, bir ölçüm:** `Validate`'i belgeye
çevirmek davranışı değiştirmiyor, çünkü kabul edilen küme zaten **yalnız yayındaki
sürümleri** içeriyor — yani iki gerçeklem semantik olarak aynı. Kilidin **taşıyıcısı
`Validate` değil `LiveVersionsAsync`**; bu, 12.7'nin *"iki bağımsız sebep koruyorsa hangisini
tuttuğunu ölç"* dersinin uygulanması ve `Contract_Audit.md`'ye o hâliyle yazıldı.

🔴 **VE BOZMA TURU PLANDA OLMAYAN GERÇEK BİR HATA BULDU (fazın en değerli anı).**
`Publish` komutu "eskiyi yürürlükten kaldır + yeniyi yayınla"yı **tek `SaveChanges`**'te
yapıyordu ve testler üst üste **üç kez yeşil** koştu. Bozma turunda beklenmedik bir davranış
görülünce ölçüldü: aynı senaryonun **8 koşusundan 5'i**
`23505: duplicate key value violates unique constraint
"ix_legal_document_versions_one_live_per_document"` ile düşüyordu.

- **Sebep:** kısmi unique indeks Postgres'te **deyim başına** denetlenir ve **ertelenemez**
  (`DEFERRABLE` yalnız *kısıtlarda* var; kısmi unique indeks kısıt olamaz çünkü UNIQUE
  constraint `WHERE` kabul etmez). EF ise aynı tablonun UPDATE'lerini **birincil anahtar
  sırasına** göre gönderiyor ve anahtarlar `gen_random_uuid()` — yani sıra **rastgele**.
  Yeni sürüm önce yazıldığında iki satır bir an için indeksin koşulunu sağlıyor.
- **Neden bu kadar sinsi:** hata **her seferinde değil**, yayınlanan sürümün GUID'i
  eskisinden küçük geldiğinde çıkıyor. *"Bende çalışıyor"* diyen geliştirici **haklı**.
- **Çözüm:** iki `SaveChanges`, **tek işlemde**. Bunun için `IUnitOfWork`'e
  `ExecuteInTransactionAsync` eklendi. 🐛 Yan bulgu: var olan `BeginTransactionAsync`
  **bugüne kadar hiç çağrılmamış** ve çağrıldığı an patlıyor —
  `EnableRetryOnFailure` elle açılan işlemleri reddediyor. Yani arayüzde **çalışmayan bir
  kapı** duruyordu; artık uyarısı yazılı ve doğru kapı yanında.
- **Kilit:** `LegalPublishTests` geçişi **10 kez** tekrarlıyor (tek turluk bir test bu hatayı
  **%37 olasılıkla kaçırırdı** — ve tam olarak öyle de olmuştu).
  🔑 **Ders: rastgeleliğe bağlı bir hata, tek koşuluk bir testle kilitlenemez.**

#### 🐛 Projenin kendi korumaları iki hata daha yakaladı

- **`data-confirm` `<button>`'a yazılmıştı** → `PanelConfirmDialogTests` kırmızı. `panel.js`'in
  dinleyicisi **submit olayında formun** özniteliğine bakıyor; butonda duran bir `data-confirm`
  onay penceresini **sessizce hiç açmaz** → geri alınamaz bir aksiyon (yayınlama) tek tıkla,
  uyarısız koşardı. Öznitelik forma taşındı.
- **Bir Razor YORUMUNDA geçen açı parantezli betik etiketi** → `PanelExternalOriginTests`
  kırmızı (görünümün ham metnini tarıyor). Koruma gevşetilmedi, **yorum** düzeltildi.
- ➕ Ayrıca iç içe `<form>` yazılmıştı (yayınla butonu düzenleme formunun içinde): tarayıcı
  içtekini sessizce atar — buton çizilir, tıklanır, hiçbir şey olmaz. `form=` bağıyla çözüldü.

#### 📌 Bitti kriteri — madde madde

- ✅ Panelden yeni bir KVKK sürümü yayınlanıyor (`LegalPublishTests`, 10 tur)
- ✅ Yayınlanmış sürüm **düzenlenemiyor**: derleyici (`CS8852`) + komut reddi + panelde form
  **hiç çizilmiyor** (`PanelLegalTests`)
- ✅ Zorunlu rıza olmadan `register` **reddediliyor ve sebebini söylüyor** (`MISSING_CONSENT`
  + belge adı)
- ✅ Rıza satırı **kullanıcıyla aynı işlemde** yazılıyor — bozma turu koşuldu, kırmızı
- ✅ `{type}` ucu **anonim** çalışıyor; tanınmayan tür **404** (varsayılana düşmüyor)
- ✅ `Publish` aksiyonu **`approve`** iznine düşüyor (yalnız `update` yetkisi olan moderatör
  reddediliyor — iddia **duruma değil kaydın kendisine** bakıyor)
- ✅ Hesap silindiğinde rıza satırı **duruyor**, kullanıcı **anonimleşiyor** (iki yönlü iddia)
- ✅ Onayın üç ek kısıtı: 18 adımlı reçete (tek bilinçli sapma **yazılı**) · **ikinci sahip
  yok** (rıza geri alma → mevcut `DELETE /v1/users/me`, izin → `PanelMenu`'den türeyen
  anahtar, iz → `IAuditableCommand`) · `consents` **additive** + **yapılandırma kapısı**

#### ⏭️ 12.16'dan çıkan açık maddeler

- **12.17 (mobil)** — backend hazır, uçlar canlı. ⚠️ 12.17 yazılana kadar zorunlu bir belge
  **yayınlanmamalı**: yayınlandığı an emülatördeki kayıt akışı `MISSING_CONSENT` alır
  (kapı `Legal:RequireConsentAtRegistration=false` ile geçici olarak kapatılabilir).
- **Metin yazımı bir İNSAN işi** — panelde belgeler kabuk hâlinde duruyor, metinleri
  yönetici/hukukçu yazacak. Kod bunu bekliyor, tahmin etmiyor.

---

### 12.17 — KVKK: mobil — [x] ✅ TAMAMLANDI (15 Ağustos 2026)

- **Kayıt ekranına rıza adımı** (`/kayit`): zorunlu belgeler için **ön işaretsiz** onay
  kutuları + belge adına dokununca **tam metin** ekranı (`/yasal/:type`).
  🔴 Onay kutusu ön işaretli olamaz — testle kilitlenir.
- **"Devam et" butonu**, zorunlu kutuların hepsi işaretlenene kadar **kapalı** ve
  **sebebini yazar** (§7 madde 42'nin "buton kapalıysa sebebini söyle" kuralı).
- ⚠️ Belgeler alınamazsa (ağ hatası) kayıt **açılmaz ve sebebini söyler** — 🔴 burada
  *"şüphede kalınca göster"* kuralı (§5) **GEÇERSİZ**: metni gösteremiyorken rıza almak,
  rıza almamaktır. Bu, projedeki varsayılan yönün bilinçli tersi ve **yazılmalı**.
- **Ayarlar → "Yasal metinler"**: yayında olan metinleri okuma + **isteğe bağlı rızayı
  geri alma** + *"onayladığınız sürüm: v3, 12.08.2026"*.
- **Yeniden onay akışı**: `RequiresReconsent` işaretli yeni sürüm varsa uygulama açılışında
  tek seferlik ekran. ⚠️ Kapatılabilir olmalı (zorunlu belgede kapatılamaz).
- Türkçe hata sözlüğüne yeni kodlar (`MISSING_CONSENT` — `turkish_ui_test.dart` denetler).
- Golden: kayıt ekranının yeni hâli (açık/koyu, 1.4 ölçek) + metin ekranı.

**Bitti kriteri:** emülatörde kayıt, rıza verilmeden **tamamlanamıyor** · metin panelden
değiştirilip yayınlanınca **uygulamada yeni metin görünüyor** (uçtan uca zincir) ·
ayarlardan onaylanan sürüm görünüyor · isteğe bağlı rıza geri alınabiliyor.

#### ✅ Ne teslim edildi (15 Ağustos 2026)

**Backend 1244 → 1251 (+7), mobil 824 → 865 (+41). Görünmez sözleşme 74 → 77.**

- **Yeni mobil modül `features/legal/`** (13. modülün yanına 14. klasör; backend modülü
  29 numaralı satırın mobil sütununu doldurdu): dört ekran — `/yasal` (Ayarlar › Yasal
  metinler) · `/yasal/:type` (metnin tam hâli) · `/yasal-surum/:id` (**onayladığınız** metin) ·
  `/yasal-onay` (yeniden onay).
- **Kayıt akışında rıza adımı:** ön işaretsiz kutular, her satırda "… — oku" bağlantısı ve
  sürüm rozeti, kapalı butonun **sebebini yazması**.
- **Ayarlarda:** *"Onayınız: v2 · 15 Ağustos 2026"*, isteğe bağlı izni **verme/geri alma**,
  zorunlu izinde karşılığın (hesap silme) **yazılması**.
- **Yeniden onay kapısı** sekme kabuğunu sarıyor; zorunlu belgede kapatılamaz ama **çıkışı
  var** (hesap silme).
- ➕ **Plan dışı backend eki:** `GET /v1/legal/versions/{id}` (aşağıda).
- ➕ **Plan dışı ortak bileşen:** `core/widgets/rich_html_body.dart` — HTML gövde çiziminin
  ortak çekirdeği; `NewsBody` sahipliğini koruyup çizimi ona delege ediyor.

#### ⚙️ Alınan kararlar

| # | Karar | Gerekçe |
|---|---|---|
| 1 | 🔴 **Metin gösterilemiyorken KAYIT AÇILMAZ** (§7 madde 76) | Projedeki varsayılan yönün (§5 *"şüphede kalınca göster"*) **bilinçli tersi**: metni gösteremiyorken rıza almak **rıza almamaktır** ve alınan onay hiç alınmamışla aynı kapıya çıkar. ⚠️ `AsyncLoading` dalı da kapalı — yalnız hata dalı kapatılsaydı hızlı davranan kullanıcı metinler inerken kaydı tamamlardı |
| 2 | 🔴 **Kararın tek sahibi `ConsentSelection`** (saf sınıf) | Üç ekran (kayıt · yeniden onay · ayarlar) aynı kuraldan geçiyor. Ayrı yazılsalardı projenin en sık hasar sınıfı doğardı (§7 madde 23/38/65): bir ekran kutuyu zorunlu sayar, diğeri saymaz ve kullanıcı **hangi ekrandan geldiğine göre** farklı kural görür |
| 3 | 🔴 **Hukuki metin ekranları yönlendirme istisnası** (`AppRoutes.isLegalReading`) | *"Kayıt yarım kaldıysa tek çıkış kayıt ekranıdır"* kuralı bu ekranları da kapatıyordu → "oku" bağlantısı kullanıcıyı geri fırlatır ve geriye **okumadan onaylamaktan başka seçenek kalmazdı**; yani bloğun tamamı boşa giderdi |
| 4 | **Yeniden onay kapısı kabukta, `redirect`'te DEĞİL** | `GoRouter.redirect` eşzamanlı; rıza durumu bir ağ isteğinin sonucu. Redirect'e taşımak ya açılışta `await` beklemek (uygulamayı kilitler) ya da veriyi router'da önbelleğe almak (**ikinci sahip**) demekti. ⚠️ İşaret **kullanıcı başına** tutuluyor: tek `bool` olsaydı ikinci hesabın bekleyen onayı **sessizce hiç sorulmazdı** |
| 5 | **Kapatılamayan ekranın ÇIKIŞI var** | Zorunlu belgede geri tuşu kapalı ama "Hesabı sil" duruyor — 12.7'nin *"son sosyal bağlantı da çözülebilmeli"* gerekçesiyle aynı: kapatılamayan ve çıkışı olmayan ekran kullanıcıyı hesabından **kilitler** |
| 6 | **İyimser güncelleme YOK** (bildirim anahtarlarının bilinçli tersi) | Orada bedel bir bildirimin gelmemesiydi; burada ekranda "onaylandı" yazıp sunucuda yazılmamış bir rıza **var olmayan bir kanıttır** — ve kullanıcı onayladığını sanır |
| 7 | ➕ **`GET /v1/legal/versions/{id}` yazıldı** (plan dışı) | 12.16 rızayı sürüme bağladı ve `consentedVersionId`'yi söylüyordu — ama o kimlikten **metne** giden yol **yoktu**: yeni sürüm yayınlandığı an vatandaş kabul ettiği metni bir daha **hiç göremiyordu**. Kanıt bizdeydi, **sahibinde** değildi. 🔴 Taslak **404**, yürürlükten kalkmış sürüm **döner**, belgenin `IsActive`'ine **bakılmaz** (kanıt tek bir panel anahtarıyla kaybolamaz) |
| 8 | ➕ **`RichHtmlBody` ortak çekirdeğe çıkarıldı** | Hukuki metin de HTML çiziyor ve bir feature başka feature'ın `presentation`'ına bakamaz. İkinci bir kopya yazılsaydı iki dosya **ayrı ayrı doğru** başlar, zamanla ayrışırdı (birinde `onLinkTap` bağlı, diğerinde değil) — ve hiçbiri hata vermezdi. **Sahiplik değişmedi:** `NewsBody` hâlâ haber gövdesinin sahibi, `RichHtmlBody` yalnız *nasıl çizildiğini* biliyor |

#### 🐛 BOZMA TURU KOŞULDU — **9 kilit, 9 kırmızı**

| Bozma | Sonuç |
|---|---|
| `ConsentSelection.initial` **her kutuyu işaretledi** (madde 75) | 🔴 **üç dosya birden** |
| Belgeler alınamazken kayıt **açık** kaldı (madde 76) | 🔴 kırmızı |
| Reddedilen karar sunucuya **gönderilmedi** | 🔴 iki dosya |
| Yönlendirmedeki hukuki metin **istisnası kaldırıldı** (madde 76) | 🔴 kırmızı |
| Yeniden onay kapısı **hiç çalışmadı** | 🔴 **altı test** |
| Zorunlu rızada "geri al" butonu **çizildi** | 🔴 kırmızı |
| `isReconsent` her zaman `false` gitti | 🔴 kırmızı |
| Zorunlu belgede ekran **kapatılabilir** yapıldı | 🔴 kırmızı |
| Backend: **taslak sürüm de döndü** (madde 77) | 🔴 kırmızı |

#### 🐛 CANLI DOĞRULAMA **GERÇEK BİR HATA BULDU** — ve hata 12.16'daydı

🔴 **Panelden yeni sürüm açmak HİÇ ÇALIŞMIYORDU.** Emülatör testinden önce panelde bir KVKK
metni yayınlamak gerekiyordu; "Taslak oluştur" **500** verdi:
`ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type
'timestamp with time zone'`.

- **Sebep:** panelin `<input type="date">` alanı MVC'de `Kind=Unspecified` bir `DateTime`
  üretiyor; Npgsql `timestamptz` kolonuna yalnız **UTC** yazıyor. Projede bu dönüşümün deseni
  zaten vardı (`PowerOutage`, `Announcement`, `DeathNotice` komutları `DateTime.SpecifyKind`
  çağırıyor) — **12.16 onu atlamıştı.**
- **Neden bu kadar ağır:** 12.16'nın bir numaralı kuralı *"yayınlanmış metin değiştirilemez,
  değiştirmenin **tek yolu** yeni sürümdür"*. O tek yol kapalıydı; yani modül **canlıda
  kullanılamaz** hâldeydi ve hiçbir test bunu söylemiyordu.
- 🔑 **Testler neden görmedi:** 12.16'nın bütün testleri `DateTime.UtcNow` veriyordu
  (`Kind=Utc`). Kural doğru ölçülüyordu ama **panelin gerçekte ürettiği değerle değil** —
  12.7'nin *"iki bağımsız sebep koruyorsa hangisini tuttuğunu ölç"* dersinin kardeşi.
  🔑 **Yeni ders: bir alanı test ederken, o alana GERÇEKTE ne geldiğini ölç.**
- **Çözüm:** `LegalDates.FromPanel` (tek sahip — `Create` ve `Update` iki çağıran; ayrı
  yazılsalardı biri düzeltilip diğeri unutulduğunda **taslağı düzenlemek** yine 500 verirdi).
  ⚠️ Saat **kaydırılmaz, yalnız etiketlenir**: yönetici "15.08.2026" yazdığında kastettiği o
  takvim günüdür (§7 madde 6'nın dört kez tekrarlamış tuzağı).
- **Kilit:** `PanelLegalTests.CreateVersion_AcceptsADateComingFromTheForm_NotOnlyAUtcStampFromCode`
  — tarihi **form alanı olarak** gönderiyor. Bozma turu koşuldu → **kırmızı**.

#### 🐛 Projenin kendi korumaları bir hata daha yakaladı

- **`ConsentCheckTile`'da gerçek bir taşma vardı** ve **widget testi ilk koşuşunda** buldu:
  `RenderFlex overflowed by 65 pixels` (400 dp genişlikte). İçteki `Flexible` yalnız kendi
  satırında iş görüyordu; `InkWell` esnek olmadığı için dıştaki satır onun doğal genişliğini
  istiyordu. Bu, projenin **yedi kez** tekrarlamış taşma sınıfının sekizincisiydi.
  → `Expanded` eklendi ve golden sayfası (`consent_check_tile_{light,dark}.png`) 360 dp **ve**
  1.4 yazı ölçeğinde kilitledi.
- **`ArchitectureDocTests` kırmızıya döndü** çünkü `lib/features/legal/` modül tablosunda
  yoktu — doküman bilerek çürüyemiyor (§4 adım 18).

#### 📌 Bitti kriteri — madde madde (**hepsi CANLI doğrulandı**, Android emülatörü + panel)

- ✅ Panelden metin yazıldı ve **yayınlandı** → `GET /v1/legal/documents` iki belge döndü
- ✅ Kayıt ekranında kutular **ön işaretsiz** geldi, "Kaydı Tamamla" **kapalıydı** ve
  *"Devam etmek için "Açık Rıza Metni" onayı gerekli."* yazıyordu
- ✅ "… — oku" bağlantısı kayıt akışının **içinden** açıldı (yönlendirme istisnası çalıştı) ve
  panelde yazılan HTML **biçimli** çizildi (başlık · liste · kalın · alıntı · bağlantı)
- ✅ Kutu işaretlenince buton **açıldı**; kayıt tamamlandı ve veritabanında **iki satır** oluştu:
  `acik_riza → granted=true`, `ticari_ileti → granted=false` — *"sorduk, hayır dedi"* gerçekten
  kaydedildi; `source=registration` ve `ip_address` **sunucuda** dolduruldu
- ✅ Ayarlar › Yasal metinler: *"Onayınız: v1 · 15 Ağustos 2026"*, zorunlu belgede "geri al"
  butonu **hiç çizilmedi** ve karşılığı (hesap silme) yazıldı
- ✅ İsteğe bağlı izin **verildi** → `source` sunucuda **`settings`** oldu; sonra "İzni geri al"
  butonu çıktı
- ✅ Panelden **v2 yayınlandı** (`RequiresReconsent` işaretli) → uygulama açılışında
  **yeniden onay ekranı geldi**, kutu **ön işaretsizdi**, geri tuşu **yoktu**, "Hesabı sil"
  çıkışı **vardı** → onaylandı ve satır `source=reconsent` olarak yazıldı.
  🔑 **v1 kaydı silinmedi** — kanıt duruyor
- ✅ Panelden **v3 yayınlandı** (`RequiresReconsent` **işaretsiz**) → yeniden onay ekranı
  **açılmadı** (kullanıcı gereksiz rahatsız edilmedi) ve ayarlarda
  **"Onayladığınız metni oku (v2)"** butonu belirdi
- ✅ O butondan **v2 metni açıldı** ve ekran *"Bu metin artık yürürlükte değil — 15 Ağustos 2026
  tarihinde yerini yeni bir sürüme bıraktı. Onayladığınız metin bu."* dedi

#### ⏭️ 12.17'den çıkan açık madde

- 📌 **Hukuki metinlerin GERÇEK içeriği hâlâ yazılmadı** — ve bu bilinçli: kod metni
  **seed etmiyor** (12.16 kararı 1). Bugün yerelde yayında olan metinler **test metnidir**.
  Yayından önce hukukçunun yazdığı metin panele girilmeli; zincirin geri kalanı hazır.

---

### 📌 KVKK bloğunun bilinçli olarak KAPSAM DIŞI bıraktıkları

Bunlar *"yapılmadı"* değil, **"bu blokta yapılmayacağı kararlaştırıldı"**:

- **İlgili kişi başvuru formu (KVKK m.11)** — veri sahibinin "verilerimi ver / sil / düzelt"
  başvurusu. Silme zaten var (10.8); **dışa aktarma yok**. Ayrı bir madde, ayrı bir faz.
- **Veri işleme envanteri / VERBİS kaydı** — kod işi değil, kurumsal bir yükümlülük.
- **Çerez politikası** — panelin kendi çerezi var ama panel **halka açık değil**.
- **Yaş doğrulama** — `Age >= 13` bugün kayıtta zorlanıyor ama **veli onayı** akışı yok.

---

### 🧹 12.7 oturumunda kapatılan iki küçük bayat madde

- 🐛 **`Class1.cs` iki dosya daha duruyordu.** `c594d29` (13 Ağu, doküman bakım turu)
  *"`Class1.cs` silindi"* diye kaydedildi ama gerçekte yalnız **`KadirliApp.Domain/Class1.cs`**
  silinmişti; `Application` ve `Infrastructure` altındaki ikisi yerinde duruyordu (ikisi de
  boş, referanssız). 12.7'de silindi (`grep Class1` → **0**).
  🔑 Bu, bu dosyanın kendi uyardığı sınıfın **birebir örneği**: *madde kapandı sayıldı,
  listeden düştü, gerçekte açıktı.* Ve tam olarak `uploads/` artıklarında iki plan turu
  boyunca yaşananın aynısı. 📌 Ders: *"X silindi"* yazarken **kaç tane X olduğunu say**.
- **Açık maddeler panosu** güncellendi: 12.7 satırı **silindi** (panonun kuralı gereği
  kapanan satır işaretlenmez, silinir), 12.16/12.17 (KVKK) ve 12.7'nin **koşulamayan bozma
  turu** eklendi, *kategori bazlı bildirim aboneliği* 12.18'e kaydırıldı.

---

### 📥 14 Ağustos 2026 — ÜÇÜNCÜ dış analiz denetimi (kod okuyan analiz)

> **Bu analiz öncekilerden farklı: gerçekten kod okumuş.** 9 ve 10 Ağustos'taki iki Gemini
> turu README/doküman aktarımıydı; bu tur entity, controller, handler, middleware ve DI
> kayıtlarına bakmış ve **iki gerçek delik** bulmuş. Ama her maddesi doğru değil ve
> **doğru bulduklarının bir kısmını yanlış sebeple** açıklıyor.
>
> Yöntem: her madde kodda **ölçüldü** (grep + satır sayımı + test kaynağı taraması), ezberden
> değil. Aşağıdaki sayıların hepsi 14 Ağu 2026 ölçümüdür.

#### 🔴 GERÇEK BULGU 1 — `/Dashboard/Seed`: analiz doğru yeri buldu, yanlış sebebi yazdı

Analiz bunu bir **katman ihlali** ("Web controller `AppDbContext` enjekte ediyor") olarak
raporladı. Katman kısmı en önemsiz tarafı — `Web → Infrastructure` referansı mimaride
**yasal** (§1), yani derleyici zaten şikâyet etmiyor. Ölçüldüğünde asıl mesele üç ayrı
şey çıktı ve üçü de **güvenlik/veri** tarafında:

| # | Bulgu | Kanıt |
|---|---|---|
| a | **Ortam kapısı YOK** — `IsDevelopment()` kontrolü hiç yazılmamış | `DashboardController.cs`'te `IWebHostEnvironment` **hiç enjekte edilmemiş** |
| b | **`[HttpGet]`** — yani `AutoValidateAntiforgeryToken` bunu **kapsamaz** (yalnız POST/PUT/DELETE'i doğrular) | `Program.cs:45` + `DashboardController.cs:110-112` |
| c | Butonu düz `<a href="/Dashboard/Seed">` | `Views/Dashboard/Index.cshtml:22` |

🔑 **Bileşik hasar:** Production'da **boş kalan her tabloya sahte veri basılabilir** —
`MockDataSeeder` `+905321110001` gibi uydurma telefonlar, sahte ilanlar, sahte vefat
ilanları yazıyor (310 satır, 20 tablo). Ve GET olduğu için bir yöneticinin ziyaret ettiği
kötü niyetli bir sayfadaki tek bir `<img src="…/Dashboard/Seed">` **onun oturumuyla** bunu
tetikler; yönetici hiçbir şey tıklamamış olur.

🟢 **Hafifletici (ölçüldü, abartmamak için):** `MockDataSeeder` **tablo bazında idempotent**
— 20 bloğun hepsi `if (!await db.X.AnyAsync())` ile korunuyor. Yani üzerinde gerçek veri
olan bir tabloya **dokunmaz**. Gerçek risk: production'da **henüz boş olan** modüller
(bugün Mekanlar, Rehber, Taksi gibi yeni açılmış bir modül boşsa oraya sahte kayıt iner).
Yani "veritabanını mahveder" değil, **"canlıda sahte içerik yayınlar"**.

📌 Bu maddenin en can sıkıcı yanı: `CODE_REVIEW_CHECKLIST` §4'te **kardeş kural zaten var**
(*"Varsayılan admin şifresi / hassas bilgi `IsDevelopment()` koşulu olmadan ekrana basılıyor
mu?"* — 11.15c'de giriş sayfası için yazılmış) ve `ProductionReadinessGuard` da var; ikisi de
bu aksiyonu **kapsamıyor**. Kapsam deliği — Faz A'nın dersinin (*"kapsam dizinden mi, tipten
mi, elden mi?"*) yedinci tekrarı.

#### 🔴 GERÇEK BULGU 2 — `User.cs`'te ÇÜRÜMÜŞ bir yorum (analiz doğru dosyayı gösterdi, sebebi ıskaladı)

Analiz §8'de şunu yazdı: *"`User.cs`'teki `NotificationPreferences.News` property'sinin
yorumu 20 satır. Bu kadar uzun yorum bakım yükü."* — **Doğru dosya, doğru satır, yanlış
teşhis.** O yorumun sorunu uzunluğu değil: **yalan söylüyor.**

`KadirliApp.Domain/Entities/User.cs:97-100` bugün şunu diyor:

> *"🔬 Anahtarsız JSON'un gerçekten `true` materyalize olduğu **ölçüldü**
> (`NotificationPreferenceTests.MissingJsonKey_DefaultsToOptedIn`)"*

İki ayrı biçimde yanlış:

1. **Atıf yaptığı test YOK.** `NotificationPreferenceTests` diye bir sınıf yok; gerçek sınıf
   `NotificationPreferenceAxisTests` ve metot adı `MissingJsonKey_DefaultsToOptedIn` **değil**.
2. **İddia ölçümün TERSİ.** Gerçek test `MissingJsonKey_MaterialisesAsFalse`
   (`NotificationPreferenceAxisTests.cs:171`) ve iddiası:
   `preferences.News.Should().BeFalse("EF'in JSON materyalizasyonu varsayılan başlatıcıyı
   ÇALIŞTIRMIYOR — geri doldurmanın var olma sebebi bu")`.

🔑 **Neden tehlikeli:** `20260812213106_BackfillNewsNotificationPreference` migration'ının
**bütün varlık sebebi** bu ölçümün `false` çıkmasıdır. Bugünkü yorumu okuyan biri
*"zaten `true` materyalize oluyormuş, bu migration gereksiz"* sonucuna varır ve onu siler —
o an **13/13 kullanıcı** haber bildiriminden sessizce düşer. Testin kendi `<remarks>`'ı tam
bu senaryodan korkuyor (*"Biri yarın migration'ı 'gereksiz' diye kaldırırsa…"*) ama varlığın
yorumu **onun tam tersini** söylüyor.

🔑 Bu, `ARCHITECTURE.md` §4 adım 8'deki `permissions`/`role_permissions` vakasının **birebir
ikizi**: atıfları geçerli görünüyor, dilbilgisi sağlam ve **yanlış**. Doküman testi *sarkan
işaretçi* garantisi verir, **doğruluk** garantisi değil.

➕ **İkinci (zararsız) bayat yorum:** `CreateAdCommandHandler.cs:25` —
*"Ads/Validators altındaki FluentValidation sınıfları pipeline'a hiç kayıtlı değil"*.
O klasör **artık yok** (`find … -name "*Validator*.cs"` → 0 dosya).

#### 🟠 DOĞRU, ama analizin bulduğundan DAHA KESKİN — ölü durum enum'ları

Analiz *"Status alanları string, enum kullanılmalı"* dedi. Ölçüm daha kötü bir şey gösterdi:
**enum'lar zaten yazılmış ve dördü de ölü.**

| Enum | Kendi dosyası dışında kullanım |
|---|---|
| `AdStatus` | **0** (yalnız `PanelDisplay.cs:16`'da bir **yorumda** `<c>AdStatus</c>` olarak geçiyor) |
| `CampaignStatus` | **0** |
| `DeathStatus` | **0** |
| `EventStatus` | **0** |

Ham string sayımı (üretim kodu): `"pending"` **150** · `"approved"` **48** · `"rejected"` **21**
· `"active"` **21** · `"expired"` **8** · `"archived"` **5**.

🔴 **Ama analizin önerdiği çözüm (enum'a çevir) YANLIŞ ve bu projede kırıcı:** durum değeri
DB'de `varchar` ve **DTO'da string olarak mobile çıkıyor** → §5 gereği tipini değiştirmek
kontrat kırar. Doğru çözüm projede **zaten var**:
`Application/Features/PushCampaigns/PushCampaignStatus.cs` → `PushCampaignStatuses` adında
`const string` sınıfı. Yani proje doğru deseni **bir modülde bulmuş, dört moderasyonlu
modüle uygulamamış**. 12.11 tek sahipliği derleyiciye devretti (`init` → `CS8852`) ama
korumayı **değerin kendisine** taşımadı: `ad.Approve("apprved")` bugün **derlenir**.

#### 🟡 KISMEN DOĞRU — çerçevelemesi yanlış olanlar

- **"Web katmanı disiplinsiz, API disiplinli."** Ölçüm: 30 panel controller'ının **12'si**
  `IUnitOfWork`/`IRepository` alıyor. Ama **11'i salt-okunur** (form açılırken
  `ViewBag.Neighborhoods` gibi dropdown doldurma). Yazan **tek** yer `AccountController`
  (2 `SaveChanges`, ikisi de `PanelLockoutPolicy` sayacı) ve orası **bilinçli**: kimlik
  doğrulama akışı, henüz bir kullanıcı bağlamı yokken koşuyor. 🔑 Asıl risk katman değil,
  **MediatR'ı atlayan yazmanın `AuditBehavior` + `CacheInvalidationBehavior`'ı atlaması**;
  bugün o risk gerçekleşmiyor.
- **"Dönüş tipi tutarsız, çift zarf riski."** Tutarsızlık **gerçek** (173 handler'ın **23'ü**
  kendi `ApiResponse<T>`'sini dönüyor) ama **çift sarma olmuyor**:
  `ApiResponseWrapperFilter.IsAlreadyWrapped` bunu 10.13'ten beri yakalıyor ve kodun kendisi
  buna *"eski desen"* diyor. Bilinen, kontrollü bir borç — hata değil.
- **"`User.cs` çok şişman, SRP ihlali."** Dosya **103 satır**, 26 property. Satır olarak
  şişman değil; sorumluluk olarak dağınık olduğu doğru. Ama bölmenin bedeli ölçülmeli:
  `User.Phone` **42 dosyanın** kimlik çıpası (§7 madde 70). Faz 13 adayı, acil değil.

#### ❌ YANLIŞ ya da UYDURMA

- **"Muhtemelen FluentValidation kullanılıyor… iki kaynaklı doğrulama riski."**
  `AbstractValidator` → **0 sınıf**. `ValidationBehavior` → **yok**, pipeline'da yalnız
  `Caching`/`CacheInvalidation`/`Audit` var. FluentValidation paketi **yalnız
  `ValidationException` tipi** için duruyor (18 `throw`). Yani **ikinci kaynak yok**;
  tek kaynak var ve o da elle. 🔑 Analiz burada yanlış bir *rahatlama* üretiyor:
  gerçek durum "iki kaynak çakışabilir" değil, **"95 komut, 0 validator"**.
- **"Test coverage bilinmiyor, `UnitTest1.cs` ismi umut vermiyor."** `UnitTest1.cs`
  **yok** (`find` → 0). Gerçek: **109 test dosyası, 891 `[Fact]`/`[Theory]`**.
  Bu cümle uydurma ve analizin en zayıf anı.
- **"Yorumlar koddan fazla satır kaplıyor."** Ölçüldü:
  **93.159 kod / 9.652 yorum → %9,4**. Bu düşük-normal bir oran. ⚠️ *Ama* yorum
  **yoğunlaşmış**: 12 dosya %52–73 arasında (`PushCampaign.cs` %72,6 · `Ad.cs` %58,1 ·
  `NewsArticle.cs` %52,5) — ve bunlar tam olarak **görünmez sözleşme taşıyan** dosyalar,
  yani kaza değil tasarım.
- **"Comment rot riski"** — risk **gerçek** (yukarıda iki örneği bulundu) ama analizin
  ölçütü yanlış. Sarkan işaretçi taraması yapıldı: yorumlardaki **396** tip atfının
  **0'ı kırık**; 6 test atfının **1'i** kırık. Yani bu projede yorum çürümesi *"olmayan
  şeye atıf"* biçiminde **neredeyse yok**; çürüme **"geçerli atıf + yanlış iddia"**
  biçiminde geliyor ve onu hiçbir doküman testi yakalayamaz.

#### 🔁 ÜÇÜNCÜ KEZ GELEN, ZATEN GEREKÇELİ REDDEDİLMİŞ MADDELER

- **`Repository.Query()` → `IQueryable` sızıntısı.** 9 Ağu'da denetlendi, 10 Ağu'da tekrar
  geldi, 13 Ağu'da **canlı zarar arandı ve bulunamadı** (12 `SoftRemove` çağrısının hepsi
  izlenen nesnede). Karar değişmedi. Açık maddeler panosu D bölümünde duruyor.
- **Anemik domain / Domain Events.** Üçüncü tekrar. 12.11'de **hedefli** olarak çözüldü
  (canlı hasar üretmiş tek değişmez). Genel dönüşüm Faz 13 adayı.

---

### 🗺️ PLAN — Faz 12.19: "denetimin bulduğu üç delik" *(bir sonraki oturum)*

> Üçü de **additive**, üçü de tek oturumluk. Sıra **hasar büyüklüğüne** göre, çabaya göre değil.

**12.19a — `/Dashboard/Seed`'i kapat** *(en yüksek öncelik: canlıda sahte içerik + CSRF)*
1. Aksiyonu `[HttpPost]`'a çevir, view'daki `<a href>`'i `data-confirm` taşıyan bir `<form>`
   yap. ⚠️ `data-confirm` **formun** üzerinde olmalı, butonun değil (12.16 hatası).
2. `IWebHostEnvironment` enjekte et; `!env.IsDevelopment()` ise aksiyon **404** dönsün ve
   buton **hiç çizilmesin** (§11: menü gizlemek yetmez, yolun kendisi koşullu olmalı —
   `/gelistirici/ag` dersi).
3. `AppDbContext` bağımlılığını controller'dan düşür: `SeedMockDataCommand` (Application)
   yaz, `MockDataSeeder`'ı oradan çağır. Böylece **audit izi de** düşer (bugün hiç düşmüyor).
4. **Testler:** `PanelSeedActionTests` → (a) Production'da 404, (b) GET reddediliyor,
   (c) moderatör 403, (d) dolu tabloya dokunmuyor.
5. 🔑 **Kapsamı türet, elle liste yazma:** `ProductionReadinessGuardTests`'e *"panelde
   Production'da açık kalan, veritabanına toplu yazan aksiyon var mı?"* sorusunu **yansımayla**
   sor — yoksa yarın yazılacak ikinci bir seed aksiyonu aynı deliği açar.

**12.19b — `User.cs`'in çürümüş yorumunu düzelt ve sınıfı KAPAT**
1. `User.cs:97-100`'ü gerçekle değiştir: anahtarsız JSON **`false`** materyalize olur,
   `BackfillNewsNotificationPreference` bu yüzden **zorunludur**, kilit
   `NotificationPreferenceAxisTests.MissingJsonKey_MaterialisesAsFalse`.
2. `CreateAdCommandHandler.cs:25`'teki ölü `Ads/Validators` atfını sil.
3. 🔑 **Asıl iş bu:** yorumdaki **test/metot atıflarını** doğrulayan bir doküman testi yaz
   (`CommentReferenceTests`) — bu oturumda yazdığım tarama zaten çalışıyor ve **1 kırık atıf**
   buldu. Kapsam **dizinden** türetilsin (`**/*.cs`), elle liste tutulmasın.
   ⚠️ Bu test *sarkan işaretçiyi* yakalar, **yanlış iddiayı yakalayamaz** — dosyanın kendisi
   bunu dürüstçe yazsın (madde 67'nin `VacuousOnAFreshDatabase` dürüstlüğü deseni).

**12.19c — Durum değerlerini sabite bağla** *(§5'i kırmadan)*
1. `AdStatuses` · `DeathStatuses` · `EventStatuses` · `CampaignStatuses` → `const string`
   sınıfları (`PushCampaignStatuses` deseninin birebir kopyası, **enum değil**: DB `varchar`
   ve değer DTO'da mobile çıkıyor → tip değişimi §5 gereği kırıcı).
2. Ölü dört enum'u **sil** (`AdStatus`/`CampaignStatus`/`DeathStatus`/`EventStatus`) —
   0 kullanım, ölçüldü. ⚠️ `PanelDisplay.cs:16`'daki yorum atfı da güncellensin.
3. Varlıkların geçiş metotlarını sabitlere bağla (`Ad.Approve` içindeki `"approved"` →
   `AdStatuses.Approved`). ⚠️ Kolon değeri **birebir aynı kalmalı** — bu bir yeniden
   adlandırma değil, **tek sahip** çalışması.
4. **Test:** `ModerationSingleOwnerTests`'e bir ayak daha — moderasyonlu varlıkların geçiş
   metotlarında **ham durum literali** kalmadığı, kapsam **tipten** türetilerek denetlensin.
5. 📌 **Kapsam bilinçli olarak dar:** `"active"`/`"scheduled"` (duyuru) ve haber durumları
   bu turda **dışarıda** — duyuruda moderasyon yok, haber durumu zaten türetiliyor
   (§7 madde 58).

**Kapsam DIŞI (bilinçli):** `User` entity'sinin bölünmesi · `IQueryable` sızıntısı ·
genel anemik-domain dönüşümü · handler dönüş tipi tekilleştirmesi. Dördü de **gerçek** ama
dördü de *"çalışan bir şeyi güzelleştirmek"*; üçü zaten gerekçeli olarak Faz 13'e bırakıldı,
dördüncüsü (`ApiResponse` ikiliği) **hata üretmiyor** (`IsAlreadyWrapped` kapıyor).

---

## ✅ FAZ 12.19 TAMAMLANDI — "denetimin bulduğu üç delik" *(16 Ağustos 2026)*

> Backend **1251 → 1276** (+25) · mobil **865** (12.19 mobile dokunmadı) ·
> görünmez sözleşme **77 → 80**. Üçü de additive: hiçbir DTO alanı silinmedi, hiçbir tablo
> düşürülmedi, **hiçbir migration yazılmadı** (kolon değeri birebir aynı kaldı).
> 🐛 Bozma turu: **15 kilit, 15 kırmızı** (biri ikinci denemede — aşağıda).

### 12.19a — `/Dashboard/Seed` kapatıldı

**Delik neydi (14 Ağu denetimi, bir numaralı bulgu).** Aksiyonun **üç** ayrı sorunu vardı ve
tehlikeli olan **bileşimleriydi**: ortam kapısı hiç yazılmamıştı, `[HttpGet]` olduğu için
`AutoValidateAntiforgeryToken` global filtresi onu **kapsamıyordu** (filtre yalnız
POST/PUT/DELETE doğrular) ve butonu düz bir `<a href>` idi. Sonucu somut: bir yöneticinin
ziyaret ettiği kötü niyetli sayfadaki tek bir `<img src="…/Dashboard/Seed">` etiketi, **onun
oturumuyla** canlıda boş kalan her tabloya sahte içerik yazdırırdı — sahte ilan, uydurma
telefon, **sahte vefat ilanı** — ve yönetici hiçbir şey tıklamamış olurdu.

🟢 **Hafifletici (ölçüldü, abartmamak için):** `MockDataSeeder` tablo bazında idempotent,
yani dolu bir tabloya dokunmaz. Gerçek risk canlıda **henüz boş** modüllerdi. Bu hafifletici
artık bir varsayım değil, **ölçüm**: `MockDataSeederTests` iki koşuyu tek testte yapıp
ikincisinin **0 satır** yazdığını iddia ediyor.

🔴 **KARAR 1 — kapı controller'da DEĞİL, boru hattında.** Plan `if (!env.IsDevelopment())`
diyordu; o `if` aynı sınıftan bir hatayı **bir kez daha** mümkün kılar — yarın yazılacak
ikinci bir bakım aksiyonunda unutulabilir ve unutulduğunu hiçbir şey söylemez. Kapı
`DevelopmentOnlyBehavior` + `IDevelopmentOnlyCommand` işaretleyicisine taşındı: kapsam artık
**tipten türüyor** ve komut hangi host'tan (Api · Web · Hangfire) çağrılırsa çağrılsın
korunuyor. ⚠️ **Sıra kuralın parçası**: `AuditBehavior` izi handler *döndükten sonra* yazar,
kapı ondan sonra dursaydı reddedilen komut çoktan koşmuş olurdu → boru hattının **en başında**
ve bu ayrıca testli.

🔴 **KARAR 2 — kapının yönü "izin ver", "reddet" değil.** `IAppEnvironment` bilinçli olarak
`IsProduction` **taşımıyor**: `!IsProduction()` yazan bir kapı, `Staging`/`Test` gibi bugün
var olmayan bir ortam adı eklendiği gün **sessizce açılır**; `IsDevelopment()` ise sessizce
*kapanır*. Sessizce kapanan bir kapı fark edilir, sessizce açılan fark edilmez. Testin
`Staging`/`Test` satırları tam bu yüzden var.

🔴 **KARAR 3 — Production'da 403 değil 404.** 403 *"burada bir şey var ama sana kapalı"* der
ve yolun varlığını doğrular.

➕ **Üçüncü delik plan dışıydı ve en sessiziydi: denetim izi hiç düşmüyordu.** Controller
`AppDbContext`'i doğrudan alıp MediatR'ı atlıyordu — katman olarak yasaldı (§1:
`Web → Infrastructure` meşru), yani **derleyici bunu asla söylemezdi**. Canlıda sahte içerik
basabilen tek aksiyonun *"kim çalıştırdı?"* sorusunun cevabı hiçbir yerde yazmıyordu.
Artık `SeedMockDataCommand` (`AuditModule = "system"`, `AuditAction = "seed"`);
`PanelDisplay`'e Türkçe karşılıkları eklendi (**kırmızı** rozet, bilinçli: bu satırın izde
görünmesi sahte veri yazılmış olması demektir).

➕ **Plan dışı — mesaj artık YALAN SÖYLEMİYOR.** Aksiyon *her* koşuda "Örnek veriler
başarıyla eklendi." diyordu; dolu bir veritabanında hiçbir satır yazmadan **aynı cümleyi**
kuruyordu ve yönetici farkı göremiyordu. Artık ne yazdığını söylüyor
(`5 satır eklendi (2 tablo): ads (5), announcements (3)` / *"Hiçbir tabloya dokunulmadı…"*).
🔑 Sayım seeder'ın **içine** değil sarmalayıcıya kondu: 20 bloğa dağıtılan bir sayaç, 21. blok
eklendiğinde **sessizce eksik** kalırdı. Önce/sonra satır sayısı farkı ise kapsamı **EF
modelinden** türetir (tek `UNION ALL` sorgusu).

🐛 **Panel testinin ilk koşusu ikinci bir gerçek tutarsızlık buldu:** Dashboard'ın boş durum
metni *"Paneli Test Verileriyle Doldur butonuyla örnek veri ekleyebilirsiniz"* diyordu ve o
buton **ekranda olmayabiliyordu**. 12.19a'da Production'da somutlaştı, ama tutarsızlık daha
eskiydi: aynı cümle **11.15b'den beri moderatöre de** gösteriliyordu ve moderatör o butonu
hiç görmüyordu. Kaynağı tek sahibe bağlandı (`Model.CanSeedMockData`).

➕ **Plan dışı ek — panelde ORTAM ROZETİ.** Panel geri alınamaz ve şehir ölçekli işlerin
yapıldığı yer (bütün şehre push — 12.15'te **terminal**; hukuki metin **yayınlamak** —
12.16'da **değiştirilemez**), buna karşılık ekranda *"burası hangi kurulum?"* sorusuna cevap
veren **hiçbir şey yoktu**: geliştirme paneli ile canlı panel piksel piksel aynıydı.
🔴 Rozetin **yönü kuralın kendisi**: "CANLI" yazan bir rozet, unutulduğu ya da yanlış
yapılandırıldığı anda **canlıyı güvenli gösterirdi**. Ters yönde en kötü ihtimal geliştirme
panelinin süslenmemiş kalmasıdır — sessiz hasar üretmeyen tek yön bu. İddia iki yönlü.

🔑 **Kapsam türetildi, elle liste yazılmadı** (planın 5. maddesi):
`DevelopmentOnlyCommandTests` boru hattı kaydını **ve sırasını**, panel aksiyonlarının
`[HttpPost]` + ortam kapısını, ve `MockDataSeeder`'a host'tan **doğrudan erişim kalmadığını**
denetliyor — hepsi `IDevelopmentOnlyCommand`'i uygulayan **tiplerden** türetilerek.
⚠️ Ayrıca `TheScan_ActuallyFindsThePanelActions`: yukarıdaki iki tarama **boş kümede de yeşil
kalırdı**, o yüzden taramanın gerçekten bir şey bulduğu ayrıca iddia ediliyor.

🐛 **Mevcut bir test kırıldı ve kırılması DOĞRUYDU:**
`PanelModeratorPermissionTests.Moderator_CannotSeedMockData` GET → 302 bekliyordu, artık
**405** geliyor (GET rol kapısına *hiç ulaşmıyor*). Yapılan şey beklentiyi gevşetmek değil,
iddiayı **gerçek kapıya taşımak** oldu: rol kapısı ancak aksiyonun gerçek yöntemiyle
denenebilir. Test şimdi ikisini birden söylüyor.

⚠️ **Panel testi GERÇEK seeder'ı kullanmıyor, sahtesini kullanıyor** ve gerekçesi ölçülmüş
bir risk: panel testlerinin hepsi **tek** Postgres konteynerini paylaşıyor
(`PanelCollection`). Gerçek seeder orada koşsaydı 400+ testin altındaki veritabanına 20 tablo
dolusu sahte kayıt basar ve "boş liste"/"kesin sayı" iddiası taşıyan bir testi **koşum
sırasına göre** kırardı. Seeder'ın kendi davranışı `MockDataSeederTests`'te, **kendi**
veritabanında ölçülüyor.

### 12.19b — çürümüş yorum düzeltildi, sınıf kapatıldı

`User.cs`'teki yorum *"anahtarsız JSON'un gerçekten `true` materyalize olduğu ölçüldü"*
diyordu ve **var olmayan bir teste** atıf yapıyordu. İkisi de yanlıştı: gerçek ölçüm
`NotificationPreferenceAxisTests.MissingJsonKey_MaterialisesAsFalse` ve iddiası **tam tersi**.
🔑 Tehlikesi somut: `20260812213106_BackfillNewsNotificationPreference` migration'ının
**bütün varlık sebebi** o ölçümün `false` çıkmasıdır; yorumu okuyan biri migration'ı
"gereksiz" sayıp silseydi **o an** mevcut kullanıcıların hepsi haber bildiriminden sessizce
düşerdi. Yorum gerçekle değiştirildi ve *neden* bu paragrafın orada durduğu da yazıldı.
Ayrıca `CreateAdCommandHandler`'daki ölü `Ads/Validators` atfı silindi (o klasör artık yok).

**Asıl iş `CommentReferenceTests` oldu** ve kapsamı planın istediğinden geniş — üç ayak:
1. **Test atıfları** (`<c>BirşeyTests.Metot</c>`) — planın istediği.
2. **Nitelikli `<see cref="Tip.Üye"/>`** — ⚠️ **ölçüldü ve sezgiye ters**: `cref` yalnız XML
   belge üretimi açıkken çözülür, bu çözümde **hiçbir projede açık değil**, yani
   `<see cref="OlmayanTip"/>` **uyarı bile üretmiyor**. *"Derleyici zaten bakıyor"* varsayımı
   bu depoda **yanlış**.
3. ➕ **Dosya yolu atıfları** (plan dışı ve bu projede birincisinden değerli): bir kuralın
   "tek sahibi" çoğunlukla bir **dosya** olarak yazılıyor (`core/router/app_nav.dart` ·
   `wwwroot/js/panel.js`) ve bu yolların çoğu **mobil tarafta**, yani C# derleyicisinin
   göremeyeceği yerde. Ölçüt *"yol, depodaki bir dosyanın **sonekidir**"* — yorumlar kısmi
   yol yazıyor, tam yol araması yanlış kırmızı üretirdi.

🐛 **Test İLK KOŞUSUNDA ikinci bir gerçek çürük buldu:** `PanelAssetGuard.cs` →
`PanelExternalOriginTests.EveryLocalAssetReference_Exists`; gerçek ad `…_ExistsOnDisk`.

⚠️ **Bu kilit bilinçli olarak EKSİK ve bunu kendisi yazıyor** (madde 80, `Contract_Audit`'te
tek 🟠): tarama *sarkan işaretçiyi* yakalar, **yanlış iddiayı yakalayamaz**. `User.cs` yorumu
ikisini birden taşıyordu ve tehlikeli olan yarısı ikincisiydi. Adlandırma/belge dürüstlüğü
madde 67'nin `SmokeCheck_…_VacuousOnAFreshDatabase` deseni.

### 12.19c — durum değerleri sabite bağlandı (§5 kırılmadan)

Dört ölü enum (`AdStatus` · `CampaignStatus` · `DeathStatus` · `EventStatus`) **silindi** —
kendi dosyaları dışında **0** kullanım (ölçüldü). Yerine `Domain/Enums/ModerationStatuses.cs`:
ortak çekirdek + dört modül sınıfı (`AdStatuses.Approved` gibi), `PushCampaignStatuses` (12.2b)
ve `TransportVehicleTypes` (12.5) desenlerinin birebir kopyası.

🔴 **`enum` DEĞİL `const string` ve bu bir tercih değil zorunluluk:** değer DB'de `varchar` ve
**DTO'da metin olarak mobile çıkıyor** → tipini değiştirmek §5'i, yani mağazadaki eski
sürümleri kırardı. Denetimin *"enum kullanılmalı"* önerisi doğru sorunu görmüş, **yanlış
çözümü** yazmıştı.

🔑 **Kapattığı delik:** 12.11 *kimin* yazdığını derleyiciye bağlamıştı (`init` → CS8852) ama
*ne* yazıldığını bağlamamıştı — `Ad.Approve` içinde `_status = "apprved"` yazmak 12.19c'ye
kadar **derleniyordu** ve hasarı tamamen sessiz olurdu: kayıt yazılır, panel "Bilinmeyen
durum" rozeti çizer, mobil listede ilan **hiç görünmez** (§3), hiçbir hata oluşmaz.

⚠️ **Yeniden adlandırma DEĞİL:** kolonda duran metinler birebir aynı, migration **yok**.

**Kilit iki yandan da türetiliyor** (`ModerationSingleOwnerTests`'in yeni ayağı): denetlenecek
varlıklar `ModeratedEntities()`'ten (dosya taraması), **yasak kelime dağarcığı** ise
`Domain.Enums`'taki `*Statuses` sınıflarının sabitlerinden **yansımayla**. Ters yön de var
(`EveryModeratedEntity_ActuallyUsesTheStatusConstants`) — silinen dört enum tam olarak
"yazılmış, doğru ve hiç kullanılmayan" şeylerdi.

📌 **Kapsam bilinçli olarak dar tutuldu:** duyuru (`active`/`scheduled`) ve haber durumları
dışarıda (duyuruda moderasyon yok, haber durumu türetiliyor — §7 madde 58); handler/sorgu
tarafındaki ~250 literal de **bu turda dokunulmadı**. Kapatılan şey *canlı hasar üretebilen*
yer: geçişin kendisi.

### 🐛 Bozma turu — 15 kilit, 15 kırmızı (biri ikinci denemede)

İlk turda **14/15** kırmızıydı. **13 numaralı bozma yeşil kaldı ve haklıydı:** bozma
`<see cref="DevelopmentOnlyBehavior{TRequest,TResponse}.OlmayanUye"/>` biçiminde **jenerik**
bir atıftı ve testin iki ayrı deliği vardı — deseni (`(?<type>\w+)`) `{…}` bloğuna hiç
uymuyordu, üstelik jenerik tiplerin `Type.Name`'i **arite soneki** taşıdığı için
(``DevelopmentOnlyBehavior`2``) sözlükte de bulunamıyordu. Yani **jenerik atıflar hiç
denetlenmiyordu**. İkisi de düzeltildi; hem jenerik hem düz kırık cref artık **kırmızı**.
🔑 **Ders: kapsam doğru olabilir ama DESEN dar olabilir.** Faz A'nın *"kapsam dizinden mi,
tipten mi, elden mi?"* sorusunun üçüncü biçimi bu — bütün dosyalar taranıyordu, taramanın
**gözü** dardı.

### ✅ Canlı doğrulama (panel + emülatör, uçtan uca)

| Ne | Sonuç |
|---|---|
| Panelde ortam rozeti | ✅ **"Development"** çiziliyor |
| Seed butonu | ✅ `<form method="post">` + `data-confirm` **formun üzerinde**; eski `<a href>` **yok** |
| `GET /Dashboard/Seed` | ✅ **405** (Method Not Allowed) |
| `POST /Dashboard/Seed` | ✅ 302 → *"Hiçbir tabloya dokunulmadı — örnek verinin gireceği tabloların hepsi zaten dolu."* (dev DB dolu; **mesaj artık doğruyu söylüyor**) |
| Denetim izi | ✅ `audit_logs` → `system` / `seed`; panelde **"Sistem" / "Örnek veri bastı"** |
| Android emülatörü (Pixel 9, API 37) | ✅ Uygulama açıldı, 6 uç 200 (`/v1/users/me/consents` dâhil — 12.17 zinciri ayakta) |

### Bu fazın kalıcı dersleri

1. 🔑 **Bir kapı, unutulabildiği yerde durmamalı.** Controller'daki `if` doğru davranışı
   üretir ama **ikinci kez yazılmak zorundadır**; boru hattındaki kapı kapsamını tipten
   türetir ve ikinci aksiyonu **yazan kişi farkında olmadan** korur.
2. 🔑 **Kapının yönünü seçerken "hangi yanlış sessizdir?" diye sor.** `!IsProduction()`
   sessizce **açılır**, `IsDevelopment()` sessizce **kapanır**.
3. 🔑 **Bir mesajın doğru olması, doğru şeyi söylediği anlamına gelmez.** "Başarıyla eklendi"
   teknik olarak yanlış değildi — hiçbir şey eklenmemiş olsa bile "başarılı"ydı.
4. 🔑 **`<see cref>` bu depoda denetlenmiyor.** *"Derleyici zaten bakıyor"* varsayımı ölçüldü
   ve yanlış çıktı.
5. 🔑 **Kapsam ile desen ayrı şeylerdir.** Bütün dosyaları taramak, tarama deseninin dar
   olmadığı anlamına gelmez (bozma turunun bulduğu delik).

---

# 🔬 DENETİM OTURUMU — kod analizi · canlı buton testi · TEST KALİTESİ ölçümü *(16 Ağustos 2026)*

> **Bu bir faz değil, bir ÖLÇÜM oturumudur.** Kod değişikliği yapılmadı; yapılması gerekenler
> aşağıda **12.20** başlığı altında maddelendi. Oturumun sorusu üçtü:
> *(1) kodda bulgu var mı, (2) panelde ölü buton var mı, (3) **~2100 testin ne kadarı doğru
> yeri test ediyor?***
>
> Üçüncü soru bu oturumun asıl konusuydu ve cevabı ölçüldü — tahmin edilmedi.

## 📊 Oturumun kurduğu ortam (üçü de canlı doğrulandı)

| Parça | Durum |
|---|---|
| Postgres · Redis · Seq | ✅ `docker compose up -d` |
| API | ✅ `http://localhost:5005` — açılışta haber senkronu koştu (1 okundu, 0 yeni) |
| Panel | ✅ `http://localhost:5203` — **DEVELOPMENT** rozeti çiziliyor (12.19'un eki) |
| iOS simülatörü (iPhone 17 Pro Max) | ✅ uygulama açıldı, giriş ekranı |
| **Android emülatörü (Pixel 9, API 37)** | ✅ uygulama açıldı, `10.0.2.2:5005`'e bağlandı |

**Yeşil taban:** `dotnet test` **1276/1276** · `flutter analyze` **0 sorun** ·
`flutter test` **865/865**.

---

## 1️⃣ Panel canlı buton testi — **210 sayfa, SIFIR ölü buton**

Panelin **bütün** sayfaları oturum çereziyle gezildi ve her sayfada dört şey arandı.
Tarama iki turda koştu: **90 sayfa** (menüden BFS) + **120 sayfa** (Create/Edit/Details/
Properties/Schedule/Versions ekranları — buton yoğunluğunun asıl yeri).

| Aranan | Bulunan |
|---|---|
| Ölü link (`href="#"` · `href=""` · `javascript:`) | **0** |
| Sahipsiz buton (`type=button`, forma bağlı değil, tanınan `data-*` yok) | **0** |
| Satır içi `on*=` işleyicisi (§7 madde 51 ihlali) | **0** |
| 4xx/5xx dönen sayfa | **0** |

📌 **Yanlış alarm olarak elenen iki şey — ikisi de bilinçli tasarım:**

1. **7 adet "boş form action"** → `_BulkToolbar.cshtml`'in **bilerek boş** `<form>`'u.
   Butonlar ona `form="…"` + `formaction` ile bağlanıyor; sebep partial'ın kendi yorumunda
   yazılı: tabloyu forma sarmak **iç içe form** üretirdi, tarayıcı içtekini sessizce atardı
   ve satır butonları çalışmaz hâle gelirdi.
2. **31 adet "onay penceresi olmayan yıkıcı aksiyon"** (`Reject` · `Ban`) → bunlar
   `<details>` popover'ı içinde **gerekçe soran** formlar. Gerekçe yazmak, onay
   penceresinden **daha güçlü** bir onaydır. Onay penceresi taşıyan 115 aksiyonun
   **87'sinde `data-confirm` formun üzerinde**, 28'inde butonda — `panel.js` ikisini de
   okuyor, yani 12.16'nın *"butona yazılmış `data-confirm` sessizce hiç açılmaz"* hatası
   **tekrarlamamış**.

🔑 **Sonuç: panelde ölü buton yok.** *"İşlevsiz buton kalmasın"* (Faz 6) bugün ayakta.

---

## 2️⃣ Test kalitesi — **BOZMA TURU: 11 değişmez bozuldu, 11'i de kırmızıya döndü**

*"Testi var" ≠ "kilitli"* sorusunun tek dürüst cevabı ölçümdür. On bir görünmez sözleşme
tek tek bozuldu ve **her bozmadan sonra suite'in TAMAMI** koşuldu (hedefli filtre değil —
soru *"herhangi bir test yakalıyor mu?"*).

### Backend (her biri 1276 testin tamamıyla ölçüldü)

| # | Bozma | Madde | Sonuç |
|---|---|---|---|
| M1 | `SlugHelper`'dan Türkçe **`'İ'` eşlemesi** silindi | 21 | 🔴 **5 test** |
| M2 | `PushDataKeys.RelatedType` → `"related_type"` (deep-link anahtarı) | 16 | 🔴 **1 test** |
| M3 | `ErrorFingerprint`'ten **GUID maskeleme** kaldırıldı | 32 | 🔴 **2 test** |
| M4 | `LoginIdentifierMasker` kimliği **HAM** döndürdü | 34 | 🔴 **8 test** |
| M5 | `PushPreferenceTopics`: haber ekseni duyuruya çökertildi | 67 | 🔴 **3 test** |
| M6 | `NewsVisibility`: kategori **dışlama** semantiği kaldırıldı | 59 | 🔴 **4 test** |
| M7 | `EventDistrictResolver`: `IsLocal` türetimi `true`'ya sabitlendi | 44 | 🔴 **3 test** |

### Mobil (her biri 865 testin tamamıyla ölçüldü)

| # | Bozma | Madde | Sonuç |
|---|---|---|---|
| N1 | `ConsentSelection.initial` **ön işaretli** başlatıldı | 75 | 🔴 **10 test** |
| N2 | `OperatingDays.fromCodes(null)` → "hiçbir gün" | 49 | 🔴 **1 test** |
| N3 | Boş maske artık "her gün"e düşmüyor | 49 | 🔴 **3 test** |
| N4 | Gün↔bit eşlemesi **bir gün kaydırıldı** (`1 << weekday % 7`) | 49 | 🔴 **12 test** |

### Bu turun okunması

- **11/11.** Rastgele seçilmiş değil, **en pahalı hasarı üreten** on bir değişmez seçildi
  (KVKK rızasının geçerliliği · hesap ele geçirme · kişisel veri sızıntısı · deep-link'in
  ölmesi · yanlış mahalleye bildirim · mağazadaki eski sürümlerin listesinin boşalması).
  Hepsi gerçekten kilitli.
- ⚠️ **En ince kilit M2** (tek test). Bu bir kusur **değil** — `PushNotificationsJobTests`
  anahtar kümesini **düz metin** iddia ediyor (Faz 0/B1'in bilinçli kararı: sabiti yeniden
  adlandırmak testi kurtarmaz). Ama kilidin **tek** ayağı olduğu bilinsin.
- **En kalın kilit N4** (12 test) ve bu doğru: Dart'ın `weekday`'i maskeyle *tesadüfen*
  hizalı olduğu için (§7 madde 49c) kaymayı yakalamak yedi günün **tek tek** denetlenmesini
  gerektiriyor — test öyle yazılmış.

### İddiasız test taraması

118 test dosyasındaki **her** `[Fact]`/`[Theory]` gövdesi tarandı: **iddia içermeyen test
YOK.** İlk turda 15 şüpheli çıktı, hepsi **ifade gövdeli** metotlardı
(`=> X.Should().Be(…)`) — tarayıcının kusuru, testlerin değil.

---

## 3️⃣ Bulgular — üç madde (**hiçbiri canlı hasar üretmiyor, ikisi kalıntı**)

### 🟠 B1 — `HomeController`: **kimliksiz erişilebilen, İngilizce, iskele artığı iki sayfa**

**Ölçüm (canlı, çerezsiz istek):**

```
302  /              → giriş                 302  /Dashboard   → giriş
302  /AdsAdmin      → giriş                 302  /StaffAdmin  → giriş
200  /Home/Index    ← kimlik doğrulaması YOK
200  /Home/Privacy  ← kimlik doğrulaması YOK
200  /Home/Error    ← (bu DOĞRU, aşağıya bak)
```

**Ne olduğu.** `HomeController` panelin **tek** `[Authorize]` taşımayan controller'ı
(statik olarak doğrulandı) ve `Program.cs`'te `FallbackPolicy` **yok** — yani öznitelik
yoksa aksiyon anonimdir. Dört aksiyonu var:

| Aksiyon | Gerekli mi? | Bugünkü içerik |
|---|---|---|
| `Error` | ✅ **EVET** — `app.UseExceptionHandler("/Home/Error")` yeniden çalıştırıyor | markalı hata sayfası |
| `StatusCode` | ✅ **EVET** — `app.UseStatusCodePagesWithReExecute("/Home/StatusCode")` | 11.15c'de yazıldı, markalı |
| `Index` | ❌ **HAYIR** | `dotnet new mvc` iskelesi: *"Welcome / Learn about building Web apps with ASP.NET Core"* |
| `Privacy` | ❌ **HAYIR** | iskele: *"Use this page to detail your site's privacy policy."* |

**Neden bir bulgu.** Üç ayrı kuralı birden deliyor ve üçü de **sessiz**:

1. **Değişmez kural #6 ihlali** (*"Arayüz Türkçe"*). Panelde bugün İngilizce metin basan
   **tek** yer burası.
2. **`_Layout` ile çiziliyor** — yani anonim ziyaretçi panelin kabuğunu, varlık
   adreslerini, ortam rozetini ve *"Şifremi Değiştir" / "Çıkış Yap"* bağlantılarını görüyor.
   🟢 **Menü `<nav>` boş geliyor** (`PanelMenuProvider` role göre süzüyor), yani **modül
   envanteri sızmıyor** — bulgunun ciddiyetini sınırlayan şey bu.
3. 🔴 **`/Home/Privacy` bir GİZLİLİK METNİ adresidir** ve bugün orada
   *"Use this page to detail your site's privacy policy"* yazıyor. Proje az önce
   **12.16–12.17'de bütün bir KVKK bloğunu** kapattı; o bloğun açık kalan tek maddesi
   *"hukuki metinlerin gerçek içeriği"*. Tahmin edilebilir bir adreste duran, İngilizce,
   yer tutucu bir gizlilik politikası, o bloğun tam olarak savaştığı şeydir.

**🔑 Asıl bulgu testte, kodda değil — ve projenin KENDİ tekrarlayan sınıfı.**
`PanelAuthenticationTests` doğru kurulmuş bir **yapısal** testtir: kapsamı assembly'den
türetir, elle controller listesi tutmaz. Ama bir muafiyet listesi var:

```csharp
private static readonly HashSet<string> AnonymousControllers = new(StringComparer.Ordinal)
{
    "AccountController", // giriş sayfasının kendisi
    "HomeController"     // hata sayfası (/Home/Error) + gizlilik metni
};
```

Muafiyet **CONTROLLER granülaritesinde**. Gerekçesi yalnız `Error`/`StatusCode`'u
karşılıyor ama muafiyet **dört aksiyonu birden** kapsıyor. Yarın `HomeController`'a
yazılacak beşinci bir aksiyon da **kendiliğinden anonim doğar ve hiçbir test kırılmaz.**
Faz A'nın dersi (*"kapsam dizinden mi, tipten mi, elden mi?"*) burada **dördüncü** bir
biçimde karşımıza çıkıyor: kapsam türetilmiş ama **muafiyet kaba**.

> 📌 Denetim şunu da ölçtü: bütün test paketinde **yalnız 4 elle tutulan muafiyet noktası**
> var (`AnonymousControllers` · `PanelModeratorPermissionTests`'in iki satır içi `Name !=`
> muafiyeti · `deliberateFallbacks` · `LegalImmutabilityStructureTests.Exempt`). Üçü
> gerekçeli ve dar. **Sorunlu olan tek tanesi bu.**

#### 🔧 B1 — yapılacaklar (sırayla)

1. **`HomeController.Index` ve `HomeController.Privacy` aksiyonlarını SİL**, `Views/Home/Index.cshtml` ve `Views/Home/Privacy.cshtml` dosyalarını da sil. İkisi de hiçbir yerden referans almıyor (`asp-action` · `Url.Action` · `PanelMenu.Items` üçünde de yok — ölçüldü) ve varsayılan rota `Dashboard/Index` olduğu için `/`'ı da beslemiyorlar.
2. ⚠️ **`Index.cshtml`'i silmeden önce `PanelExternalOriginTests.cs`'in ~75. satırındaki yorumu düzelt.** O yorum `Home/Index.cshtml`'e **dosya yolu olarak** atıf yapıyor (regex'in neden yalnız `<link href>`'e baktığını anlatan 🐛 notu). Dosya silinince **§7 madde 80 devreye girer ve `CommentReferenceTests` KIRMIZIYA döner** — bu doğru davranıştır, ama fark edilmezse "alakasız test kırıldı" sanılır. Yorum, olayı dosya yolu vermeden anlatacak şekilde yeniden yazılmalı.
3. **`HomeController` sınıfına `[Authorize(Roles = "admin,super_admin,moderator")]` ekle**, kalan iki aksiyona (`Error`, `StatusCode`) **`[AllowAnonymous]`** koy. Aksiyon özniteliği sınıf özniteliğini ezer; hata sayfaları anonim çalışmaya devam eder (`UseExceptionHandler`/`UseStatusCodePagesWithReExecute` boru hattı yeniden çalıştırdığı için bu **şart** — `[Authorize]` kalsaydı 500 alan kullanıcı giriş sayfasına atılırdı).
4. **`AnonymousControllers` listesinden `"HomeController"` satırını KALDIR.** Adım 3'ten sonra sınıf `[Authorize]` taşıdığı için test onu kendiliğinden kapsar. 🔑 **Kazanç: muafiyet listesi 2'den 1'e iner ve kapsam elle tutulan bir addan `[Authorize]` özniteliğinin varlığına — yani TÜRETİLEN bir ölçüte — geçer.** Listede kalan tek ad `AccountController` olur ve o gerçekten baştan sona anonimdir (giriş sayfası).
5. **Doğrulama:** `curl -s -o /dev/null -w "%{http_code}" http://localhost:5203/Home/Index` → **404** dönmeli; `/Home/Error` → **200** kalmalı.
6. **Bozma turu (zorunlu):** adım 3–4'ten sonra `HomeController`'a `[AllowAnonymous]` taşıyan **sahte** bir beşinci aksiyon ekle ve `PanelAuthenticationTests`'in **kırmızıya döndüğünü gör**. Dönmüyorsa muafiyet hâlâ kaba demektir. *(Bu, testin bugün yakalayamadığı senaryonun ta kendisi.)*

---

### 🟡 B2 — `wwwroot/lib/bootstrap`: **7,2 MB, sıfır referans, herkese açık servis ediliyor**

**Ölçüm:**

```
lib/ toplam ................. 9,3 MB
  bootstrap ................. 7,2 MB   ← %77'si  · 45 dosya · git'te TAKİPLİ
  fontawesome ............... 1,0 MB   (1 görünümde referans)
  jquery .................... 512 KB   (2 görünümde)
  inter ..................... 244 KB   (1 görünümde)
  leaflet ................... 228 KB   (1 görünümde)
  jquery-validation(+unobtr.) 192 KB   (1'er görünümde)

bootstrap'e kaynak referansı: 0
  (Views · *.cs · *.json · panel.css · panel.js · site.js — hepsi tarandı;
   "bootstrap" kelimesi YALNIZ obj/ altındaki derleme çıktılarında geçiyor)

curl http://localhost:5203/lib/bootstrap/dist/css/bootstrap.min.css       → 200, 162 720 bayt
curl http://localhost:5203/lib/bootstrap/dist/js/bootstrap.bundle.min.js  → 200,  78 468 bayt
```

**Ne olduğu.** `dotnet new mvc` iskelesinin kalıntısı — B1 ile **aynı kökenden**. Panel
12.9'da Tailwind'e geçti; Bootstrap o gün ölmüş ama **silinmemiş**. Bugün depoda duruyor,
klonlayan herkes indiriyor ve statik dosya ara katmanı onu **anonim olarak servis ediyor**.

**Neden bir bulgu (ve neden yalnız 🟡).** Canlı hasar üretmiyor: hiçbir sayfa yüklemiyor,
yani vatandaşa/yöneticiye bir bayt bile gitmiyor. Üç gerçek maliyeti var:

1. **Depo ağırlığı** — `lib/`'in %77'si, ve `lib/` **bilinçli olarak commit ediliyor**
   (12.9 kararı: "klonlayan `npm` kurmadan paneli açabilmeli").
2. **Yanlış harita** — `CLAUDE.md` ve `ARCHITECTURE.md` yerelleştirilen kütüphaneleri
   **`leaflet · fontawesome · inter · jquery`** diye sayıyor. Bootstrap **listede yok ama
   diskte var**: doküman ile gerçek ayrışmış durumda.
3. 🔑 **Kilidin tek yönlü olduğunu gösteriyor.** `PanelAssetGuard` +
   `PanelExternalOriginTests.EveryLocalAssetReference_ExistsOnDisk`
   *"referans verilen her varlık diskte var mı?"* diye soruyor. **Tersi hiç sorulmuyor:**
   *"diskteki her varlığa referans var mı?"* Bu yüzden ölü bir varlık sonsuza kadar
   yaşayabilir ve **hiçbir test bunu söylemez.**

#### 🔧 B2 — yapılacaklar

1. **`KadirliApp.Web/wwwroot/lib/bootstrap/` dizinini sil** (`git rm -r`). Öncesinde son bir kez `grep -rn "bootstrap" KadirliApp.Web --exclude-dir=obj --exclude-dir=lib` koş — çıktı **boş** olmalı.
2. **Varlık araç zincirini denetle:** `KadirliApp.Web/package.json` ve kopyalama betiği bootstrap'i **yeniden üretiyor mu**? Üretiyorsa oradan da çıkar; yoksa `npm run build` silineni geri getirir ve CI'ın sürüklenme kapısı bunu "eksik varlık" sanır.
3. **`ARCHITECTURE.md` §2**'deki `wwwroot/lib/*` satırını gerçekle hizala (bugün dört kütüphane sayıyor, diskte **yedi** dizin var — `jquery-validation` ve `jquery-validation-unobtrusive` de listede yok, **onlar kullanılıyor** ama yazılmamış).
4. ➕ **Asıl kalıcı düzeltme — kilidi ÇİFT YÖNLÜ yap:** `PanelExternalOriginTests`'e bir eş test ekle → *"`wwwroot/lib` altındaki her **üst düzey dizin**, en az bir görünümden referans almalı."* Kapsam **dizinden türer** (elle liste yok), yeni eklenen bir kütüphane kendiliğinden kapsanır ve ölen bir kütüphane **sessizce kalamaz**. 🔑 Bu, madde 51'in bugün eksik olan ikinci yönüdür ve B2'nin tekrarlamasını imkânsız kılar.

---

### ⚪ B3 — `ARCHITECTURE.md` mobil test dosyası sayısı bir eksik

`ARCHITECTURE.md` §2 mobil `test/` satırı: **"865 test (81 dosya)"**.
Ölçüm: test sayısı **865 — doğru**; dosya sayısı **82** (hepsinde en az bir `test(`/
`testWidgets(` var, boş dosya yok). 12.17'de eklenen dosyalardan biri sayıya yansımamış.

#### 🔧 B3 — yapılacak
`81` → `82`. *(Tek satır. `ArchitectureDocTests` bu sayıyı denetlemiyor — denetleseydi
zaten kırmızı olurdu; yani bu sayı **bilinçli olarak kilitsiz** ve elle güncelleniyor.)*

---

## ✅ 12.20 — "iskele kalıntıları + iki kilidin eksik yönü" *(16 Ağustos 2026'da TAMAMLANDI)*

> Planın üç adımı da yazıldı; kayıt aşağıda **"12.20 TESLİM"** başlığında. Bu blok planın
> **orijinal metnidir**, karşılaştırılabilsin diye duruyor.

| Adım | İş | Etki |
|---|---|---|
| 12.20a | B1: iki iskele aksiyonu+görünümü sil · `[Authorize]`+`[AllowAnonymous]` · muafiyet listesini 1'e indir · `PanelExternalOriginTests` yorumunu düzelt | 🔴 kural #6 ihlali + anonim yüzey kapanır, **muafiyet türetilen bir ölçüte geçer** |
| 12.20b | B2: `lib/bootstrap` sil · doküman hizala · **`lib/` için çift yönlü kilit** yaz | 🟡 7,2 MB gider, madde 51'in **ikinci yönü** kapanır |
| 12.20c | B3: doküman sayısı düzelt | ⚪ tek satır |

**Bozma turu şartı (her üçü için):** 12.20a'nın kilidi, sahte bir `[AllowAnonymous]`
aksiyonuyla **kırmızıya dönmek zorunda**; 12.20b'nin kilidi, `lib/` altına referanssız
sahte bir dizin açılınca **kırmızıya dönmek zorunda**. Dönmüyorlarsa kilit yanlış şeye
bakıyordur.

---

## 🔑 Bu oturumun kalıcı dersleri

1. **Bir yapısal testin kapsamı türetilmiş olabilir ama MUAFİYETİ kaba olabilir.**
   `PanelAuthenticationTests` kapsamı assembly'den türetiyor — kitabına uygun. Deliği
   açan şey kapsam değil, **controller granülaritesindeki iki satırlık muafiyet listesi**.
   Faz A'nın sorusuna bir soru daha ekleniyor: *"kapsam nereden geliyor?"*ün yanına
   ***"muafiyet hangi granülaritede?"***
2. **Tek yönlü kilitler ölü kod biriktirir.** *"Referans verilen varlık diskte var mı?"*
   sorusunun tersi sorulmadığı için 7,2 MB ölü kütüphane fark edilmeden yaşadı. Bir
   *"X ⊆ Y"* kilidi yazarken **"Y ⊆ X gerekiyor mu?"** diye sor.
3. **İskele kalıntıları aynı kökten gelir ve BİRLİKTE ölür.** B1 ve B2'nin ikisi de
   `dotnet new mvc`'den; biri bulununca **diğeri de aranmalı**. (Aranan üçüncüsü —
   `site.js` — kontrol edildi: yaşıyor ve kullanılıyor.)
4. 🟢 **Bozma turu 11/11 kırmızı verdi.** Bu projede *"testi var"* ile *"kilitli"* arasındaki
   fark **kapatılmış durumda** — ve bunu söyleyen şey kutu sayısı değil, **ölçüm**.

---

---

# ✅ 12.20 TESLİM — *"iskele kalıntıları + iki kilidin eksik yönü"* (16 Ağustos 2026)

> **Yeşil taban:** `dotnet test` **1284/1284** (1276 → **+8**) · `flutter analyze` **0** ·
> `flutter test` **865/865** (mobil koda dokunulmadı). Migration **yok**, DTO değişikliği
> **yok**, mobil tek satır değişiklik **yok**. Görünmez sözleşme **80 → 81**.

## 12.20a — `HomeController` (B1)

**Yapılanlar (planın 6 adımı, sırayla):**

1. `Views/Home/Index.cshtml` + `Views/Home/Privacy.cshtml` ve karşılık gelen iki aksiyon **silindi**.
2. Silmeden **önce** `PanelExternalOriginTests`'in `Home/Index.cshtml`'e dosya yolu olarak
   atıf yapan yorumu düzeltildi (§7 madde 80 — planın uyarısı doğruydu).
3. `HomeController` `[Authorize]`, kalan iki aksiyon (`Error` · `StatusCode`) `[AllowAnonymous]`.
4. `AnonymousControllers` muafiyeti **2 → 1**; ikinci muafiyet **aksiyon** granülaritesine
   indi (`AnonymousActions`, her satır gerekçeli).

**🐛 Planın öngörmediği: rol listesi bir YALANDI ve bir test bunu söyletti.**
İlk yazımda panelin alışılmış deseni (`Roles = "admin,super_admin,moderator"`) refleksle
kopyalandı ve `PanelModeratorPermissionTests` **anında kırmızıya döndü** — haklıydı: o rol
listesi *"bu bir modül ekranıdır ve moderatöre açıktır"* demektir, o zaman da
`[PanelPermission]` + menü satırı + matriste bir anahtar gerekir. Burası bir modül değil,
panelin **hata yüzeyi**. Doğrusu **rolsüz `[Authorize]`**: *"geçerli bir panel oturumu"*.
Panele zaten yalnız o üç rol girebildiği için kapsam aynı, ama **iddia dürüst** — izin
matrisinde karşılığı olmayan bir yetki belirmiyor (11.15b'nin en büyük bulgusunun tekrarı
önlendi).

### ➕ PLAN DIŞI (12.20a'nın asıl kazancı): **panel artık FAIL-CLOSED**

Planın 3. adımı `HomeController`'ı kapatıyordu; ama B1'in kök nedeni o sınıf **değildi**,
denetimin kendi cümlesiydi: *"`Program.cs`'te `FallbackPolicy` **yok** — yani öznitelik
yoksa aksiyon **anonimdir**."* O varsayılan durdukça aynı hata her yeni controller'da
mümkün kalırdı ve onu tutan tek şey yine **elle tutulan bir muafiyet listesi** olurdu.

`AddAuthorization`'a `FallbackPolicy = RequireAuthenticatedUser()` eklendi. Karşılığında
gerçekten anonim olması gereken **üç yer** bunu artık **açıkça** söylüyor:
`AccountController.Login` (GET+POST) · `AccountController.Logout` · `HomeController`'ın iki
hata aksiyonu · `MapInfrastructureHealthEndpoints`'in üç probe'u.

🔑 **Neden bu doğru yön:** koruma bir **taramadan framework davranışına** taşındı — §7
madde 53'ün *"korumayı taramanın erişemeyeceği yere taşıyabilir miyim?"* sorusunun cevabı.
Yapısal test **kalktı değil, ikinci hat oldu**: fallback *"unutulan aksiyon kapalı doğsun"*
der, test *"açıkça açılan aksiyon gerekçeli olsun"* der.

🔴 **Ölçülmüş bir bedeli var ve bilinçli kabul edildi.** Fallback policy **hiçbir uca
eşleşmeyen** isteklere de uygulanıyor: oturumsuz bir ziyaretçi var olmayan bir panel
adresinde artık markalı 404 yerine **302 → giriş** alıyor. Kabul edildi — oturumsuz birine
hangi adreslerin var olduğunu söylememek daha doğru — ve markalı 404 **oturumlu**
kullanıcıda (yani onu görmesi gereken kişide) canlı doğrulandı.

⚠️ **`/health/*` unutulsaydı hasar sessiz olurdu:** orkestratörün probe'u 302 alır, konteyner
"sağlıksız" damgası yer ve **sebep hiçbir logda görünmezdi**. (12.21 yayın hattı bunun
üstüne kurulacak.)

## 12.20b — `lib/bootstrap` ve madde 51'in ikinci yönü (B2)

`wwwroot/lib/bootstrap/` (**7,2 MB · 45 dosya**) `git rm -r` ile düştü. `package.json` ve
`tools/copy-vendor.mjs` denetlendi: bootstrap'i **üretmiyorlar** (yalnız leaflet ·
fontawesome · inter), yani `npm run build` silineni geri getirmiyor.

**Asıl iş kilit:** `EveryLocalAssetOnDisk_IsReferencedBySomething` —
*"`wwwroot/lib` altındaki her **dizin** ve `wwwroot/{css,js}` altındaki her **dosya** en az
bir yerden başvuru almalı."* Kapsam **dizinden türer**, elle liste yok.

### ➕ PLAN DIŞI: ters kilit yazılır yazılmaz **iki kalıntı daha** düştü

Denetimin 3. dersi (*"iskele kalıntıları aynı kökten gelir ve BİRLİKTE ölür"*) doğruydu —
ama denetimin aradığı üçüncüsü hakkında verdiği hüküm **yanlıştı**: *"`site.js` kontrol
edildi: yaşıyor ve kullanılıyor."* Ölçüldü, **kullanılmıyordu**:

| Dosya | Ölçüm | İçerik |
|---|---|---|
| `wwwroot/js/site.js` | **0 referans** | yalnız `dotnet new mvc`'nin yorum satırı |
| `wwwroot/css/site.css` | **0 referans** | ölü Bootstrap kuralları (`.btn:focus`, `.form-control:focus`) |
| `Views/Shared/_Layout.cshtml.css` | **0 referans** | izole CSS; ancak `~/KadirliApp.Web.styles.css` bağlanmışsa yüklenir, **hiçbir görünüm bağlamıyor** — içerik yine saf Bootstrap (`.navbar-brand`, `.nav-pills`, `.btn-primary`) |

🔑 **Bu, ters kilidin var olma sebebinin kanıtı:** üçü de yıllardır depodaydı, üçü de
anonim servis ediliyordu ve **hiçbir test söylemiyordu**. Denetim `site.js`'e *bakmıştı*
bile — insan gözü "referans var mı" sorusunu bir dizin taramasından **daha kötü**
cevaplıyor.

⚠️ **Kilidi yazarken bir tuzak vardı:** eşleşmede sondaki bölü işareti şart.
`lib/jquery-validation` araması `lib/jquery-validation-unobtrusive`'i de yakalıyor — ölü bir
dizin **canlı komşusunun referansına sığınarak** hayatta kalabilirdi.

## 12.20c — doküman (B3)

`ARCHITECTURE.md` §2: mobil test dosyası **81 → 82** (ölçüldü: 82 dosya, hepsinde en az bir
`test(`/`testWidgets(`). Aynı bölümdeki `lib/*` satırı da gerçekle hizalandı — bugün **altı**
kütüphane var ve `jquery-validation` + `jquery-validation-unobtrusive` doküman listesinde
hiç yazmıyordu (kullanılıyorlar).

## 🧪 Bozma turu — **4 bozma, 4 kırmızı** (biri ikinci denemede)

| # | Bozma | Beklenen | Sonuç |
|---|---|---|---|
| 1 | `wwwroot/lib/` altına referanssız sahte dizin | ters kilit kırmızı | 🔴 ✔ |
| 2 | `wwwroot/css/` altına referanssız sahte dosya | ters kilit kırmızı | 🔴 ✔ (mesaj dosyayı adıyla söyledi) |
| 3 | `HomeController`'a `[AllowAnonymous]`'lu sahte 5. aksiyon | muafiyet testi kırmızı | 🔴 ✔ *(12.20a öncesinde bu **yeşil kalırdı** — bulgunun tam kendisi)* |
| 4 | Öznitelik**siz** yeni bir controller | fallback kapatmalı | 🔴 ✔ **ama ikinci denemede** |

🔬 **4 numaralı bozma ilk kurulumunda YANLIŞ ŞEYİ ÖLÇÜYORDU** — ve bunu yakalayan şey
§7 madde 70'in dersiydi (*"iki bağımsız sebep koruyorsa testin **hangisini** tuttuğunu
ölç"*). İlk deneme öznitelik**siz** bir aksiyonu `HomeController`'a ekledi; aksiyon
kapalı çıktı ama **sınıftaki `[Authorize]` yüzünden**, fallback yüzünden değil. Ölçüm
yeniden kuruldu: hiçbir öznitelik taşımayan **yeni bir controller** açıldı ve iki yönlü
ölçüldü — fallback açıkken **302**, fallback satırı yorumlandığında **200**. Yani
B1'in hâli birebir yeniden üretildi ve koruyanın gerçekten fallback olduğu kanıtlandı.

## 🐛 Projenin kendi korumaları **iki hatamı** yakaladı (ikisi de ilk tam koşuda)

1. `PanelModeratorPermissionTests` → rol listesi (yukarıda).
2. `CommentReferenceTests` (madde 80) → **kendi yazdığım yeni testin yorumu** sildiğim iki
   dosyaya (`site.js` · `site.css`) **dosya yolu olarak** atıf yapıyordu. Yani madde 80,
   yazıldığı fazdan bir faz sonra, **onu yazan kişiyi** yakaladı. Yorum yolları kaldırılıp
   olayı yol vermeden anlatacak biçimde yeniden yazıldı.

## ✅ Canlı doğrulama (panel `:5203`, oturumsuz + oturumlu)

```
OTURUMSUZ                                  OTURUMLU (super_admin)
302  /Home/Index      ← eskiden 200        404  /Home/Index      (markalı, Türkçe)
302  /Home/Privacy    ← eskiden 200        404  /Home/Privacy
200  /Home/Error      (markalı hata)       404  /BuBirSayfaDegil → "Hata 404 · Sayfa
200  /account/login                             bulunamadı · İstenen adres: …"
200  /health/live     ← fallback'ten muaf  200  /Dashboard/Index
302  /lib/bootstrap/dist/css/bootstrap.min.css  ← eskiden 200, 162 720 bayt
302  /js/site.js · /css/site.css                ← eskiden 200
200  /css/panel.css · /lib/leaflet/leaflet.js   ← yaşayan varlıklar yerinde
```

## 🔑 Bu alt-fazın kalıcı dersleri

1. **Bir yapısal testin kapsamı türetilmiş olabilir ama MUAFİYETİ kaba olabilir.**
   Faz A'nın sorusuna (*"kapsam dizinden mi, tipten mi, elden mi?"*) kalıcı olarak bir
   soru daha eklendi: ***"muafiyet hangi granülaritede?"***
2. **Tek yönlü kilitler ölü kod biriktirir ve biriktirdiklerini hiçbir zaman söylemez.**
   Bir *"X ⊆ Y"* kilidi yazarken **"Y ⊆ X gerekiyor mu?"** diye sor.
3. **Bir denetimin "kontrol ettim, temiz" hükmü de bir iddiadır ve ölçülebilir.**
   Denetim `site.js` için *"yaşıyor ve kullanılıyor"* demişti; bir `grep` yeterliydi.
4. **Bir kapıyı kapatmanın en iyi yolu, varsayılanı ters çevirmektir.** Muafiyet listesini
   daraltmak B1'i çözüyordu; `FallbackPolicy` **B1'in sınıfını** çözdü.
5. 🔴 **Bir yeri temizlerken orada duran İŞLEVİ de siliyor olabilirsin.** `/Home/Privacy`
   İngilizce bir yer tutucuydu ve gitmesi doğruydu — ama gittiğinde panelde **hiç**
   gizlilik metni adresi kalmadı ve mağazalar yayında bunu **istiyor**. Silme kararı
   doğru, ama boşluk **panoya yazıldı** (12.10'un *"bir yolu kapatırken o yolun tek
   taşıyıcısı olduğu işlevi de siliyor musun?"* dersinin doküman tarafı).

# ✅ 12.21 — YAYIN HATTI (paketleme + teslim) *(16 Ağustos 2026'da TAMAMLANDI)*

> Teslim kaydı aşağıda **"12.21 TESLİM"** başlığında. Bu blok planın **orijinal metnidir**;
> planın bir öncülü ölçümle çürüdüğü için karşılaştırılabilir kalması önemli.

> 🍎 **APPLE'A BAĞLI HİÇBİR ADIM YOKTUR.** Apple Developer aboneliği hâlâ onaylanmadı;
> bu başlık bilinçli olarak **tamamen Apple'sız** kuruldu — hiçbir adımı TestFlight,
> APNs `.p8`, sertifika ya da App Store Connect beklemiyor. Kapsam **backend + panel**.
> (iOS yayını 12.8/11.16 notlarında beklemeye devam ediyor.)

## 📊 Planlamadan ÖNCE ölçülen gerçekler (16 Ağu 2026)

**Eksik olan `Dockerfile` değil — eksik olan TESLİM. Çalışma zamanı zaten üretim-bilinçli:**

| Ne | Durum |
|---|---|
| `/health/live` · `/health/ready` · `/health` | ✅ **VAR** (`Infrastructure/Health/HealthEndpoints.cs`; `live` bilerek `Predicate = _ => false`, yani süreç ayakta mı der, bağımlılığa bakmaz) |
| `ProductionReadinessGuard` | ✅ **VAR ve birim testli** — yayına hazır olmayan yapılandırmada açılışı durduruyor. ⚠️ Kendisi *"bayrakla kapalı kod yolu"* (yalnız Production'da koşar) ve testi tam bu yüzden yazılmış |
| `PanelAssetGuard` | ✅ **VAR** — Production'da panel varlıkları eksikse **uygulama açılmıyor** (§7 madde 51) |
| Sırların dosyadan okunması | ✅ `secrets/` deseni kurulu, git'e girmiyor |
| **`Dockerfile`** (Api ya da Web) | ❌ **YOK** |
| **`.dockerignore`** | ❌ **YOK** |
| **`docker-compose.prod.yml`** | ❌ **YOK** (mevcut compose yalnız *bağımlılıkları* kaldırıyor: Postgres·Redis·Seq. API ve panel **içinde değil**) |
| **IaC** (`.tf` / `.bicep` / k8s) | ❌ **YOK** *(⚠️ `find` ile çıkan üç `.tf`, `mobile/build/` altındaki **firebase-ios-sdk** kalıntısıdır — bizim değil)* |
| **Deploy adımı** | ❌ **YOK** — `.github/workflows/dotnet.yml`'in adı **"NET CI/CD Pipeline"** ama içerik yalnız CI |

🔑 **Bu tablo tek bir şey söylüyor: iş, uygulamayı üretime hazırlamak DEĞİL — zaten hazır.
İş, onu bir yere GÖTÜRECEK hattı yazmak.** Bu, tahmin edilenden küçük bir faz.

## ⚠️ Ölçümün bulduğu iki ek şey

1. 🐛 **`dotnet.yml` kendi kendini yalanlıyor ve bunu KENDİSİ yazıyor.** Dosyanın başındaki
   yorum (11.14 denetimi) diyor ki: entegrasyon testleri **Testcontainers** kullanıyor,
   yani `services: postgres/redis` blokları **ve** *"Apply Migrations for Test Database"*
   adımı **artık gereksiz** — *"çalışan bir hattı doğrulayamadan değiştirmemek için
   bilinçli olarak bırakıldı, ilk yeşil koşudan sonra kaldırılabilir."* O koşu çoktan
   yeşile döndü. Bugün her CI koşusu **iki gereksiz konteyner** kaldırıyor, **`dotnet-ef`
   aracını global kuruyor** ve **hiç kullanılmayan bir veritabanına migration uyguluyor**.
2. 🔴 **`uploads/` bugün risksiz ama hattın açılacağı gün riskli.** Dizinde **983 giriş**
   var ve API bugün compose'da olmadığı için host dosya sisteminde yaşıyor. API
   konteynerleştiği an bu **kalıcı bir volume** olmak zorunda; olmazsa ilk yeniden
   dağıtımda vatandaşın yüklediği **bütün ilan/vefat/mekan görselleri gider** ve
   uçlar 404 değil — istemci onları *zarifçe gizlediği* için **hiçbir belirti olmaz**
   (§7 madde 61'in aynı hasar sınıfı).

## 🗺️ Adımlar

### 12.21a — Paketleme *(Docker)*

1. **`KadirliApp.Api/Dockerfile`** — çok aşamalı (`sdk:8.0` build → `aspnet:8.0` runtime),
   `dotnet publish -c Release`, **non-root kullanıcı**, `EXPOSE 8080`,
   `HEALTHCHECK` → `/health/ready`.
2. **`KadirliApp.Web/Dockerfile`** — aynı desen. ⚠️ **Panel varlıkları özel durum:**
   `wwwroot/css/panel.css` ve `wwwroot/lib/*` **commit edilmiş türetilmiş dosyalar** (12.9).
   İmajda `npm` adımı **gerekmiyor** — ama `.dockerignore` `node_modules`'ü dışlamalı,
   `wwwroot`'u **dışlamamalı**. Yanlış yazılmış tek satırlık bir `.dockerignore`,
   `PanelAssetGuard` sayesinde konteyneri **açılmaz** yapar (bu iyi haber: sessiz değil).
3. **`.dockerignore`** — `bin/`, `obj/`, `node_modules/`, `mobile/`, `uploads/`, `secrets/`,
   `.git/`. 🔴 `secrets/` **mutlaka** dışlanmalı: imaja girerse sır, imajı çeken herkeste olur.
4. **`docker-compose.prod.yml`** — api + web + postgres + redis + seq. Zorunlular:
   - `uploads/` için **adlandırılmış volume** (yukarıdaki 🔴),
   - **Seq kimlik doğrulaması** (`SEQ_FIRSTRUN_ADMINPASSWORD` + API key) — yerelde bilinçli
     kapalı, üretimde log'lar **PII taşıyor** (§7 madde 33'ün maskelemesi *hata kaydı* için,
     Seq'e giden her şey için değil),
   - sırlar **ortam değişkeniyle** (dosya değil) geçilmeli,
   - `depends_on` + healthcheck koşulu.

### 12.21b — Teslim *(CD)*

5. **Önce temizlik:** `dotnet.yml`'den `services:` bloklarını, `dotnet-ef` kurulumunu ve
   *"Apply Migrations for Test Database"* adımını **kaldır** (dosyanın kendi yorumu bunu
   zaten söylüyor). Beklenen kazanç: koşu süresi ve iki konteynerlik gürültü.
   ⚠️ Bunu **ayrı bir commit'te** yap ve CI'ın hâlâ yeşil koştuğunu gör — yoksa deploy
   adımı eklenirken kırılan şeyin hangisi olduğu bilinmez.
6. **İmaj yayınlama:** `main`'e push'ta iki imajı `ghcr.io`'ya bas, etiket = commit SHA
   (**`latest` DEĞİL**: `latest` hangi sürümün canlıda olduğunu söyleyemez ve geri alma
   yolunu kapatır).
7. **Dosya adını gerçeğe uydur:** ya `name:` alanını **"NET CI"** yap, ya da deploy
   adımını gerçekten ekle. 🔑 Bugünkü hâli *"panelin en sinsi yalan biçimi"*nin
   (§7 madde 37) CI karşılığı: dosya adı yapmadığı bir işi yaptığını söylüyor.
8. **Migration stratejisi — KARAR GEREKİYOR (kod değil, tercih):** bugün her iki uygulama
   da açılışta `Migrate()` çağırıyor. Konteynerleşince **iki replika aynı anda** migration
   koşabilir. Üç seçenek: (a) tek seferlik `migrate` job'ı, (b) yalnız Api koşsun,
   (c) Postgres advisory lock. ⚠️ Bu bir *"sonra düşünürüz"* maddesi **değil**: yarış
   ilk çok-replikalı dağıtımda doğar ve belirtisi **bozuk bir şema** olur.

### 12.21c — Doğrulama *(bozma turu)*

9. `docker compose -f docker-compose.prod.yml up` → panel açılıyor, `/health/ready` **200**,
   `/Dashboard/Seed` **404** (Production, §7 madde 78), ortam rozeti **"CANLI değil"
   yazmıyor** (12.19'un rozeti *canlı olmayanı* işaretler).
10. **Bozma:** `.dockerignore`'a `wwwroot` ekle → konteyner **açılmamalı**
    (`PanelAssetGuard`). Açılıyorsa guard imajda çalışmıyordur.
11. **Bozma:** `uploads` volume'ünü kaldır, konteyneri yeniden başlat → var olan bir
    görselin **kaybolduğunu gör**. Kaybolmuyorsa volume yanlış yere bağlanmıştır.

📌 **Kapsam dışı (bilinçli):** 🍎 Apple'ın tamamı · 🤖 Play yayın anahtarı (`keytool`) —
ikincisi Apple'dan bağımsız ve **istenirse ayrıca yapılabilir**, ama bu başlığın konusu
*sunucu tarafı teslim*. Gerçek bir sunucu/alan adı seçimi de bu başlığın dışında:
12.21 hattı **kurar**, nereye gideceği ayrı bir karardır.

---

---

# ✅ 12.21 TESLİM — *yayın hattı* (16 Ağustos 2026)

> **Yeşil taban:** `dotnet test` **1290/1290** (1284 → **+6**) · `flutter analyze` **0** ·
> `flutter test` **865/865** (mobil koda dokunulmadı). Migration **yok**, DTO değişikliği
> **yok**. Görünmez sözleşme **81 → 82**.

## 🔬 PLANIN ÖNCÜLÜ ÖLÇÜMLE ÇÜRÜDÜ

Plan şöyle diyordu: *"Eksik olan `Dockerfile` değil — eksik olan TESLİM. Çalışma zamanı
zaten üretim-bilinçli."* **Yarısı doğruydu.** API `Production`'da başlatılmak istendi ve
**hiçbir `Sms:Provider` değeriyle açılmadığı** ölçüldü:

```
Sms__Provider=Dev     → ProductionReadinessGuard: "OTP kodu SMS ile gönderilmez …
                        HİÇBİR kullanıcı giriş yapamaz"           (Program.cs:206)
Sms__Provider=Netgsm  → AddInfrastructure: "Bilinmeyen SMS sağlayıcısı: 'Netgsm'"
                        (DependencyInjection.cs:88 — builder.Build()'den ÖNCE)
```

İki kapı da **tek başına doğru**, birlikte **geçilemez**. Ve bu bir hata değil, projenin
**tek gerçek yayın blokajıdır**: SMS gitmiyorsa hiç kimse kayıt olamaz/giremez, yani sistem
zaten yayına hazır değildir. Yanlış olan tek şey **hiçbir yerde yazmıyor** olmasıydı.

🐛 **Ve bunu hiçbir test söylemiyordu, çünkü testin kendisi de aynı hatayı yapıyordu:**
`ProductionReadinessGuardTests.HealthyProductionSettings()` *"sağlıklı üretim
yapılandırması"* olarak **`Sms:Provider = "Netgsm"`** veriyordu — projede öyle bir sağlayıcı
**hiç yoktu**. Kapı yıllarca **hiçbir zaman var olamayacak** bir yapılandırmayla doğrulandı:
test yeşildi, iddia doğruydu, **senaryo hayaliydi**.
🔑 **Ders:** 12.17'nin *"bir alanı test ederken o alana GERÇEKTE ne geldiğini ölç"*
kuralının kardeşi — ***bir yapılandırmayı test ederken o değerin gerçekten SEÇİLEBİLİR
olduğunu ölç.***

**Yapılan:** sağlayıcı listesi tek sahibe çekildi (`SmsProviders`, DI haritasından
**türetiliyor**), readiness kapısının mesajı artık *"bugün seçebileceğin başka bir değer
yok ve şunu yapman gerekiyor"* diyor, `SmsProviderAgreementTests` iki kapının uyumunu
**iki yönlü** kilitliyor (üretime uygun sağlayıcı yokken kapı bunu **söylemek**, varken
**adıyla saymak** ve o adın DI'de çözülebildiğini **kanıtlamak** zorunda).

📌 **Panel bu blokajın dışında:** kimlik doğrulaması kullanıcı adı/parola olduğu için panel
`Production`'da **bugün de açılıyor** — canlı doğrulandı.

## 12.21a — Paketleme

| Dosya | Not |
|---|---|
| `KadirliApp.Api/Dockerfile` · `KadirliApp.Web/Dockerfile` | Çok aşamalı, **non-root** (`app`, uid 1654 — doğrulandı), `EXPOSE 8080`, `HEALTHCHECK → /health/ready`. Boyut: **393 MB / 399 MB** |
| `.dockerignore` | `secrets/` · `uploads/` · `mobile/` · `KadirliApp.Tests/` · `node_modules` dışlandı; **`wwwroot` bilerek dışlanmadı** |
| `docker-compose.prod.yml` | api + web + postgres + redis + seq; `uploads` **paylaşılan adlandırılmış volume**, Postgres portu **kapalı**, Seq **kimlik doğrulamalı** |
| `.env.prod.example` + `.gitignore` | 🔴 `.env` **`.gitignore`'da DEĞİLDİ** — 12.21a'da eklendi. Şablondaki her satır bir sır ve 11.18'de bir parola tam olarak böyle sızmıştı |

🐛 **`aspnet:8.0` imajında `curl` YOK.** HEALTHCHECK'e yazılan komut çalıştırılamayınca
Docker konteyneri **"unhealthy"** sayar — uygulama gayet iyi koşarken. Ölçülmeseydi ilk kez
**dağıtım sırasında** görünürdü; bu yüzden CI'a *"üretim imajları derleniyor mu"* kapısı da
eklendi.

## 12.21b — Teslim

- **CI temizliği** (11.14'ün bıraktığı borç): `services:` blokları, `dotnet-ef` global
  kurulumu ve *"Apply Migrations for Test Database"* adımı **kaldırıldı** — Testcontainers
  zaten kendi kabını kaldırıyordu. Dosyanın **kendi yorumu** bunu söylüyordu.
- **`name:` alanı gerçeğe uydu:** *"NET CI/CD Pipeline"* → **`.NET CI`**. Teslim ayrı bir
  dosyaya alındı: **`release.yml`** — `main`'e push'ta iki imajı `ghcr.io`'ya basar,
  etiket **commit SHA** (`latest` **değil**: hangi sürümün canlıda olduğunu söyleyemez ve
  geri alma yolunu kapatır).
- 📌 **Kapsam bilinçli olarak dar:** hat **dağıtmaz**. Hangi sunucuya gideceği kod değil
  **karardır** ve o karar verilmedi; adı da bunu söylüyor (*"Release"*, *"Deploy"* değil).

### 🔴 KARAR — migration yarışı: **Postgres advisory lock** (§7 madde 82)

Plan üç seçenek sayıyordu; seçilen **(c)** ve gerekçesi:

| Seçenek | Neden değil |
|---|---|
| (a) tek seferlik `migrate` job'ı | Dağıtım hattına **sıralama borcu** yazar: job atlanırsa uygulamalar eski şemayla açılır ve arıza ancak o kolona dokunan ilk istekte görünür |
| (b) yalnız Api koşsun | Panel önce açılırsa **göç edilmemiş** şemaya bakar; üstelik bu bir *başlatma sırası* bağımlılığıdır ve o sıranın korunduğunu hiçbir şey denetlemez |
| **(c) advisory lock** ✅ | Kilit **veritabanında** — §7 madde 60'ın kararının aynısı (Redis bilinçli olarak fail-open, §7 madde 36). 🔑 **Üstelik kurtarması yapısı gereği var:** oturuma bağlı olduğu için süreç ölünce Postgres kilidi kendiliğinden bırakır → 12.13'ün `ReapStuckRuns` borcu burada **doğmuyor** |

⚠️ Kapsam yalnız `Migrate()` **değil**, seed bloklarının tamamı: her blok *"tablo boş mu?"*
diye sorup yazıyor, yani yalnız göçü sarmak sorunun **yarısını** çözerdi.
⚠️ Kilit **kendi bağlantısında** alınıyor — `DbContext`'inkinde alınsaydı EF havuzdaki
bağlantıyı komutlar arasında bırakabilir ve kilit **sessizce düşerdi**.

## 12.21c — Doğrulama ve bozma turları

**Canlı yığın** (`docker compose -f docker-compose.prod.yml up -d`), api+web **healthy**:

```
:8080/health/live 200   :8080/health/ready 200   :8080/v1/announcements 200
:8090/health/ready 200  :8090/account/login 200
oturumlu: /Dashboard/Index 200 · /Home/Index 404 · POST /Dashboard/Seed 404 (§7 madde 78)
ortam rozeti: YOK (rozet CANLI OLMAYANI işaretler — Production'da doğru)
Seed butonu: hiç çizilmiyor (Production)
```

📌 **Yan gözlem:** taze bir üretim dağıtımında panel, seed'lenmiş varsayılan parolayı
**değiştirmeden hiçbir ekranı açmıyor** (11.18'in `MustChangePassword` akışı konteynerde de
çalışıyor) — `secrets/` imaja girmediği için parola dosyası orada yok, yani davranış doğru.

| # | Bozma | Sonuç |
|---|---|---|
| 1 | `.dockerignore`'a `wwwroot/` | 🔴 Konteyner **açılmadı**: *"WebRootPath boş → panelin statik varlıkları hiç servis edilemez"* |
| 2 | Yalnız `wwwroot/css/panel.css` dışlandı (daha sinsi hâli) | 🔴 **Açılmadı** ve dosyayı **adıyla** söyledi: *"panel TAMAMEN STİLSİZ açılır (`npm run build:css` atlanmış)"* |
| 3 | `uploads` volume'ü olmadan aynı imaj | 🔴 Dosya **KAYIP**; volume ile konteyner yeniden yaratıldığında **duruyor** (iki yönlü) |
| 4 | `SchemaMigrationLock`'tan `pg_advisory_lock` çağrısı silindi | 🔴 `TwoStartupsAtOnce_DoNotOverlap` kırmızı |
| 5 | Test fabrikalarında yükleme yönlendirmesi kapatıldı | 🔴 **11 dosya** depoya sızdı (aşağıya bakın) |

🐛 **Bozma 5 ilk denemede YANLIŞ ŞEYİ ÖLÇTÜ** — yönlendirme yalnız bir fabrikada kapatıldı
ve fark **0** çıktı. Sebep: `Environment.SetEnvironmentVariable` **süreç geneli**, yani
diğer fabrika hâlâ yönlendiriyordu. İki bağımsız sebep vardı ve ölçüm yanlışını (§7 madde
70'in dersi) bir kez daha tekrarladı; ikisi birden kapatılınca gerçek rakam göründü.

## 🐛 PLAN DIŞI BULGU — `uploads/`'ın %92'si test çöpüydü

Kullanıcının sorusu üzerine ölçüldü ve plan bunu görmemişti (plan yalnız *"983 giriş
kaybolur"* diyordu):

| Ölçüm | Sonuç |
|---|---|
| Diskteki dosya | **1208** |
| `files` tablosundaki satır | **95** |
| **Yetim** (diskte var, DB'de yok) | **1113** (4,3 MB) |
| Kırık (DB'de var, diskte yok) | **0** |
| Haber gövdelerinde metin olarak anılan | 29 — **hiçbiri yetim değil** |

Ad kalıpları sebebi söylüyordu: `a.png`·`b.png`·`contract.png`·`ilanda.png` **101'er kez**,
`govde-601.jpg`·`eski-701.jpg` **53'er kez** — yani entegrasyon testlerinin fixture'ları.
**Kök neden:** test fabrikaları bağlantı dizesini eziyordu ama
`FileStorage:UploadDirectory`'yi **ezmiyordu**; dosyalar depoya, satırları ise atılabilir
Testcontainers veritabanına gidiyordu. Tek bir test sınıfı **11 dosya** bırakıyordu (ölçüldü).

🔑 **12.21'de kritikleşti:** bu faz o klasörü **kalıcı bir üretim volume'üne** çeviriyor —
temizlenmeseydi her koşunun çöpü vatandaşın gerçek görselleriyle aynı kalıcı depoya taşınırdı.
**Düzeltme:** fabrikalar yükleme klasörünü geçici bir dizine yönlendiriyor (⚠️ **ortam
değişkeniyle** — klasör `builder.Build()`'den *önce* okunuyor, §8'in bilinen tuzağı).
Doğrulandı: tam süit **0** yeni dosya bırakıyor.
🧹 1124 yetim **silinmedi, karantinaya alındı** (geri alınabilir); `uploads/` **20 MB → 15 MB**,
DB'nin işaret ettiği 95 dosyanın **hepsi yerinde**.

## 🔴 12.21d — İLK KOŞUDA CI KIRMIZI ÇIKTI (ve biri BENİM DEĞİLDİ)

İlk push'tan sonra iki iş akışı da düştü. Sebepleri ayrıydı ve **biri bu oturumdan önce
de kırmızıydı** (13:31 koşusu, 12.21 hiç yazılmamışken — ölçüldü).

### 🐛 (1) `.gitignore` BİR TEST DOSYASINI YUTUYORDU — *bu oturumun en ciddi bulgusu*

```
.gitignore:6   [Rr]elease/          ← Visual Studio derleme çıktısı için konmuş
                ↑ mobile/test/release/ ile de EŞLEŞİYOR
```

`mobile/test/release/release_config_test.dart` — Faz 11.16'nın **yayın yapılandırması
testleri** (AndroidManifest izinleri · Info.plist kullanım açıklamaları · dev rotalarının
yayına sızmaması, **8 test**) — **hiçbir zaman commit edilmemiş**. Yalnız yazıldığı
makinede yaşıyordu.

Hasar iki katmanlı ve **ikincisi tamamen sessizdi**:

1. **CI kırmızıydı**: `CodeReviewChecklistDocTests` var olmayan bir dosyaya atıf buluyordu.
   (Bu yüzey doğru çalıştı — madde 80'in kilidi, dosyanın yokluğunu **CI'da** yakaladı.)
2. 🔴 **Depoyu klonlayan hiç kimsede o korumalar YOKTU.** O testlerin var olma sebebi
   `CODE_REVIEW_CHECKLIST` §11'in açılış cümlesidir: *"bu bölümün ortak özelliği, hataları
   `flutter run` ile görünmez — her madde ilk kez **mağazadan inen** uygulamada ortaya
   çıkar."* Yani **tam da en geç fark edilecek hata sınıfının tek bekçisi, tek bir diskte
   duruyordu.**

🔑 Ders: **bir `.gitignore` deseni, adı tesadüfen uyan bir KAYNAK klasörünü de yutabilir** —
ve bunu hiçbir şey söylemez, çünkü dosya *geliştiricinin makinesinde vardır* ve orada her
şey yeşildir. Bu, projenin *"bayrakla kapalı yol = hiç test edilmemiş yol"* sınıfının
**sürüm kontrolü tarafındaki** biçimi.

### 🐛 (2) `release.yml` — depo adı küçük harf olmak zorunda *(bu benim hatamdı)*

```
ERROR: invalid tag "ghcr.io/atahanblcr/KadirliApp/api:c0628c9…":
       repository name must be lowercase
```

`github.repository` = `atahanblcr/KadirliApp` (büyük harfli). ⚠️ **Yerelde asla görünmez**:
`docker build -t kadirliapp-api` yazarken adı biz seçiyoruz, büyük harf hiç doğmuyor.
Ad artık koşuda küçük harfe çevriliyor.

### 🔬 (3) Bir varsayım daha ölçümle çürüdü

*"Mobil testler CI'da hiç koşmuyor"* diye `dotnet.yml`'e bir iş eklendi; sonra **ölçüldü**:
`mobile.yml` **zaten var** ve yeşil (11.14'te yazılmış). Yalnız `paths: mobile/**` ile
tetiklendiği için son iki commit'te koşmamıştı — *"koşmadı"* ile *"yok"* aynı şey değil.
Yinelenen iş **geri alındı**.

✅ **Üç iş akışı da yeşil** ve imajlar gerçekten yayınlandı:
`ghcr.io/atahanblcr/kadirliapp/{api,web}:45d01a4…` (+ `:main`).

---

## 🔑 Bu alt-fazın kalıcı dersleri

1. **Bir ayarı REDDEDEN kapı yazarken, kabul edeceği bir değerin var olduğunu ölç.**
   İki kapı ayrı ayrı doğru olup birlikte **geçilemez** olabilir — ve bunun belirtisi,
   birbirinden habersiz iki hata mesajıdır.
2. **Bir yapılandırmayı test ederken o değerin gerçekten seçilebilir olduğunu ölç.**
   `"Netgsm"` yıllarca "sağlıklı üretim"i temsil etti ve hiç var olmadı.
3. **Açılışta koşan her iş, konteynerleşince eşzamanlı koşar.** Kilit veritabanında olmalı;
   ve advisory kilit, kurtarmasını **yapısı gereği** getirdiği için burada doğru araçtır.
4. **Testin dosya sistemine yazdığı yer de bir temizlik borcudur.** Veritabanı satırı
   atılabilir bir kapta, dosya ise gerçek klasörde biriktiğinde oran **%92'ye** çıkabiliyor
   ve bunu hiçbir şey söylemiyor.
5. 🔴 **Bir `.gitignore` deseni, adı tesadüfen uyan bir KAYNAK klasörünü yutabilir.**
   `[Rr]elease/` sekiz testi depoya hiç sokmadı ve hasar *geliştiricinin makinesinde
   görünmezdi* — orada dosya vardı. Sorulacak soru: *"bu desen yalnız ÇIKTI mı eşliyor?"*
6. 🐛 **İki `compose` dosyası, proje adı verilmezse aynı volume'ü paylaşır.** Belirtisi bir
   hata *olabilir* (bizde oldu); olmasaydı üretim yığını geliştirme verisinin üstüne
   sessizce açılırdı.

# ⚡ 12.22 — PERFORMANS / ÖLÇEK *(16 Ağustos 2026'da planlandı — ✅ **19 Ağustos 2026'da TAMAMLANDI**, teslim raporu en altta)*

> 🔑 **Bu başlığın birinci kuralı: ÖNCE ÖLÇ, SONRA OPTİMİZE ET.** Proje bugün 80 görünmez
> sözleşmenin her birini *ölçerek* kilitlemiş durumda — ama **performans hakkında tek bir
> ölçüm yok**. Bu başlık optimizasyon değil, önce **ölçüm altyapısı** kuruyor. Ölçmeden
> yazılan her optimizasyon, bu projenin diğer her yerde reddettiği şeydir: **kanıtsız karar**.

## 📊 Bugünkü duruş — ölçüldü (16 Ağu 2026, statik)

| Eksen | Ölçüm | Yorum |
|---|---|---|
| İndeksler | **94** `HasIndex` + **GIN/trigram** (12.13'te eklendi) | 🟢 Yapısal olarak sağlam |
| Sayfalama tavanı | public **50**, admin **200** (`Pagination.Clamp`) | 🟢 Tavan var ve zorlanıyor |
| **N+1 riski — sorgu yolunda** | `Features/**/Queries/` altında **0** şüpheli nokta | 🟢 Liste uçları temiz |
| N+1 riski — yazma yolunda | 15 nokta (`Commands/` + `Services/`) | 🟡 **N sınırlı** (bir ilanın görselleri, bir personelin izinleri). `NewsSyncService`/`NewsImageMirror` bilerek öğe başına |
| **Önbellek kapsamı** | **6 grup**, 85 sorgu handler'ının **10'u** | 🟡 Yalnız *sözlük tipi* veri (`Lookups`·`Guide`·`Pharmacies`·`AdsLookup`·`News`·`Dashboard`). **Sıcak liste uçları (ilan·etkinlik·duyuru) cache'li DEĞİL** — bu bilinçli (Faz 12 notu), ama **hiç ölçülmedi** |
| **Yük testi / benchmark** | ❌ **HİÇBİRİ YOK** (`k6`·`jmx`·`BenchmarkDotNet` — üçü de aranıp bulunamadı) | 🔴 Bu başlığın var olma sebebi |
| p95 / gecikme verisi | ❌ **YOK** — Seq log topluyor ama **istek süresi ölçülmüyor** | 🔴 |

🔑 **Duruş şu: yapı doğru kurulmuş, ama HİÇ ÖLÇÜLMEMİŞ.** Bu yüzden performans puanı
düşük — kötü olduğu için değil, **bilinmediği** için. Bugün *"yavaş mı?"* sorusunun
cevabı **hiçbir yerde yok**, ve bir yanıt gecikirse bunu **kimse fark etmez**
(§7'nin sessiz hasar sınıfının performans karşılığı).

## 🗺️ Adımlar

### 12.22a — Ölçüm altyapısı *(önce bu; optimizasyon YOK)*

1. **İstek süresi log'a düşsün.** Serilog request logging (`UseSerilogRequestLogging`)
   ya da bir `IPipelineBehavior<,>` ile **komut/sorgu başına süre**. 🔑 MediatR davranışı
   olarak yazılırsa kapsam **tipten türer** — yarın eklenecek her handler kendiliğinden
   ölçülür (12.19a'nın dersi, aynen geçerli). ⚠️ **Yavaş sorgu eşiği** olmalı
   (ör. >500 ms → `Warning`); her isteği `Information`'a yazmak Seq'i çöplüğe çevirir
   ve gerçek uyarı o çöplükte kaybolur (§7 madde 36'nın *"kendimize DoS"* dersi).
2. **`k6` senaryosu** (`perf/` klasörü, ~80 satır) — en sıcak beş public uç:
   `/v1/ads` · `/v1/news` · `/v1/announcements` · `/v1/power-outages` · `/v1/events`.
   Şehir ölçeğine göre hedef: **50 eşzamanlı kullanıcı**, 2 dakika.
3. **Taban çizgisini KAYDET.** Çıktı `Memory_Bank/`'e bir tabloya yazılsın:
   *uç · p50 · p95 · hata oranı · sorgu sayısı*. 🔑 **Bu adım atlanamaz** — taban çizgisi
   olmadan sonraki oturumun "iyileştirdim" iddiası ölçülemez, yani bu projenin
   kabul etmediği türden bir iddia olur.

### 12.22b — Yalnız ölçümün gösterdiğini düzelt

4. **`/v1/power-outages` özel olarak ölçülmeli.** §7 madde 1 gereği **sayfalamıyor**,
   düz dizi dönüyor ve mobil süren/planlı ayrımını **tam listeden** yapıyor. Bugün kayıt
   sayısı küçük olduğu için sorun yok; **büyüdüğünde sessizce yavaşlar** ve sözleşme
   sayfalamayı yasakladığı için çözüm sayfalama **olamaz** (eski sürümler kırılır).
   Ölçüm bunu gösterirse çözüm ya cache ya da tarih penceresi olur — **ikisi de kontrat
   kararı**, kod kararı değil.
5. **Cache kapsamı ölçümle genişletilsin, refleksle değil.** Sıcak liste uçları bilerek
   cache'siz; k6 p95'i kabul edilebilirse **öyle kalsın**. ⚠️ Genişletilecekse §7 madde 22
   zorunlu: grup adı **yalnız `CacheGroups` sabiti** ve **her gruba invalidate eden bir
   komut** — yoksa panelde güncellenen veri mobilde sessizce eski kalır (ne log, ne istisna).
6. **Haber senkronunun maliyeti ayrı ölçülsün** — 15 dakikada bir koşuyor ve tek dış
   bağımlılığımız. Ölçüt: koşu süresi + istek sayısı + `MirrorNewsBodyImagesJob`'ın
   indirme hacmi. ⚠️ *"Arşiv derinliği 50 → tamamı"* kararı (panoda **B** bölümünde açık)
   **~273 istek + ~1,6 GB görsel** demek; o kararın ön koşulu bu ölçümdür.

### 12.22c — Doğrulama *(bozma turu)*

7. **Bozma:** `Pagination.MaxLimit`'i 50 → 5000 yap, k6'yı tekrar koş. p95 **belirgin
   biçimde bozulmalı**. Bozulmuyorsa senaryo yeterince zorlamıyordur — yani ölçüm
   yalancıdır ve bir sonraki oturum ona güvenip yanlış karar verir.
8. **Bozma:** bir GIN indeksini düşür, `?search=` senaryosunu koş → **fark görünmeli**.
   Görünmüyorsa 12.13'te eklenen indeksler sorgunun gerçekten kullandığı indeksler
   değildir (12.13'ün *"btree `LIKE '%x%'`'i karşılayamaz"* bulgusunun ikinci yönü).

📌 **Bilinçli kapsam dışı:** dağıtık cache · okuma replikası · CDN. Bunların hiçbiri
**şehir ölçeğinde** gerekçelendirilemez ve bu proje kanıtsız iyileştirmeyi zaten
iki kez reddetti (anemik domain · `IQueryable` sızıntısı — ikisi de *ölçülüp* ertelendi).

🔑 **12.22'nin başarı ölçütü bir hız değil, bir CÜMLEDİR:** oturum sonunda
*"en sıcak beş ucun p95'i şudur"* diye **yazılı bir sayı** olmalı. Bugün o cümle yok.


---

# ✅ 12.22 TESLİM — PERFORMANS / ÖLÇEK *(19 Ağustos 2026)*

> ## 🔑 Fazın başarı ölçütü bir hız değil bir CÜMLEYDİ. Cümle artık var:
> ## > *En sıcak altı public ucun **p95'i 14–19 ms**, hata oranı **%0,00** — 50 eşzamanlı kullanıcı, 2 dakika, uç başına 100.643 istek.*
>
> Sayıların tamamı, ölçüm koşulları ve **ölçümün nasıl yalan söyleyebileceği**:
> **`Memory_Bank/Performance_Baseline.md`**.

**Yeşil taban:** `dotnet test` **1325/1325** (1290 → **+35**) · `flutter analyze` **0** ·
`flutter test` **865/865**. **Mobilde tek satır değişiklik yok**, **DTO değişikliği yok**.
Migration **1** (yalnız indeks — şema değişmedi, snapshot değişmedi).
Görünmez sözleşme **82 → 84**.

## Teslim edilenler

**12.22a — ölçüm altyapısı (optimizasyon YOK).**
`PerformanceBehavior<,>` (MediatR halkası — kapsam **tipten türer**, yarınki her handler
kendiliğinden ölçülür) · `RequestHistogram` (saf, sabit kovalı, **birleştirilebilir**) ·
`RedisRequestMetrics` + `RequestMetricsFlushService` (15 sn'de bir, **mutlak**, fail-open) ·
panelde **Performans** ekranı (yalnız-admin) · `perf/baseline.js` (k6) + `perf/README.md` ·
`Memory_Bank/Performance_Baseline.md`.

**12.22b — yalnız ölçümün gösterdiği.**
🟢 İki **ölü** trigram indeksi düzeltildi (`FixDeadTrigramIndexes`) ·
🟢 sıcak uçlar **cache'siz bırakıldı** (ölçülmüş gerekçeyle) ·
🟡 kesinti tarih penceresi ve arşiv derinliği **kontrat kararı olarak panoya** taşındı.

**12.22c — bozma turu: 4 bozma, 4 kırmızı** (biri kendi hatamı buldu, aşağıda).

## 🔴 Kararlar

**1 — Ölçüm halkası `CachingBehavior`'ı SARAR (boru hattında ortam kapısından hemen sonra).**
Sıra bir stil tercihi değil, **sayının doğruluğu**: ölçülen şey *"handler ne kadar sürdü"*
değil **"çağıran ne kadar bekledi"**. Cache HIT'te handler hiç koşmaz ama bekleyen yine
bekler — halka cache'in *içine* konsaydı sıcak uçların p95'i **sistematik olarak iyi**
görünür ve bunu hiçbir şey söylemezdi. §7 madde **83**.

**2 — Gecikme ham örnek değil, SABİT KOVALI HİSTOGRAM olarak saklanır.**
Üç gereksinim aynı anda karşılanmak zorundaydı: sınırlı bellek · **birleştirilebilirlik** ·
ucuzluk. Birleştirilebilirlik pazarlık konusu değildi çünkü **API ve panel ayrı
süreçlerdir**: süreç içi bir ölçüm, panelde *doğru görünen yanlış* bir p95 basardı — ve
yanlış sayı basan bir ölçüm ekranı, ölçüm olmamasından **kötüdür** (ilkinde kimse bilmez,
ikincisinde herkes yanlış bilir). Bedeli bilinçli: yüzdelikler **yaklaşıktır ve gerçeğin
ÜSTÜNÜ söyler**. Yaklaşıklığın *yönü* sözleşmenin parçası — altını söyleyen bir ölçer
yavaşlığı **gizler**.

**3 — Sıcak liste uçları CACHE'SİZ KALIYOR.** p95 **19 ms**, yavaş handler **0**.
Plan zaten *"kabul edilebilirse öyle kalsın"* diyordu. Cache eklemek §7 madde 22'yi
(grup adı + invalidate eden komut) **ölçülmemiş bir kazanç için** borçlanmak olurdu;
bedeli gerçek (panelde güncellenen veri mobilde sessizce eski kalır), kazancı ölçülmemiş.

**4 — `/v1/power-outages`: ölçüldü, KARAR VERİLMEDİ ve verilmemesi doğru.**
Sayfalamayan uç doğrusal büyüyor: 10.000 satır → **3,7 MB**, 20.000 → **7,5 MB** gövde.
🔑 **Planın beklentisi düzeldi:** darboğaz sorgu değil (20k'da sunucu **31 ms**, `EXPLAIN`
10 ms), **gövde**. Bu yüzden çözüm **cache olamaz** — cache sunucu zamanını düşürür,
gövdeyi düşürmez. Tek çözüm **tarih penceresi**, o da bir **kontrat** kararıdır:
ölçüldü ki mobil listede **geçmiş kesintileri de gösteriyor** (*"Sona erdi"*), yani pencere
mağazadaki eski sürümlerde **görünen** bir davranış değişikliği olur. Karar panoya taşındı.
📌 `start_time` indeksi de **eklenmedi ve bu da ölçüldü**: tam sıralı okumada paralel
`Seq Scan` + `quicksort` zaten en iyi plan — eklenseydi §7 madde 84'ün cezalandırdığı şey
olurdu (yer kaplayan, güncellenen, kullanılmayan indeks).

## 🐛 Bulunan gerçek hatalar

**A — İKİ TRİGRAM İNDEKSİ ÖLÜYDÜ ve Haziran 2026'dan beri öyleydi.**
`ix_ads_title_trgm` ve `ix_places_name_trgm` **ham kolon** üzerineydi
(`GIN (title gin_trgm_ops)`), oysa projedeki **her** arama `x.Kolon.ToLower().Contains(...)`
yazıyor → Postgres'e `lower(kolon) LIKE '%…%'` gidiyor. İfade indeksinde ifade **birebir**
eşleşmek zorundadır: `title` ≠ `lower(title)` → indeks **sessizce** kullanılmaz.
🔬 20.005 satırda ölçüldü: `Seq Scan`, *Rows Removed by Filter: 19.994*, **29,2 ms** →
düzeltmeden sonra `BitmapOr`, **0,75 ms (39×)**.
🔑 **Hasarın ikinci katmanı daha sinsi:** indeks **vardı**. *"Arama yavaş, indeks var mı?"*
sorusunun cevabı **yanlış bir 'var'**dı.
🔑 **Ve tek indeks yetmezdi:** sorgu `title OR description` biçiminde; Postgres `BitmapOr`
kurabilmek için **her iki tarafta** indeks ister. Yalnız başlığı düzeltseydik plan yine tam
tarama seçerdi ve *"düzelttik"* deyip geçerdik. Bu da ölçümle görüldü.
✅ `FixDeadTrigramIndexes` + §7 madde **84** + `TrigramIndexTests` (kapsam **`pg_indexes`'ten
türer**, migration taramasından değil).

**B — Yük testi ilk koşusunda UYGULAMAYI DEĞİL, HIZ LİMİTİNİ ölçtü.**
API'de IP başına global limit var (300/60 sn); yük üreticisi **tek IP**'dir → limit ilk
saniyede doldu, iki dakika boyunca **429** döndü. 🔴 **Ve 429 hızlı döndüğü için tablo ÇOK
İYİ göründü: p95 = 1,7 ms.** Ölçümün yalan söylemesinin en sinsi biçimi — *iyi* bir sayı.
✅ `perf/baseline.js` artık `rate_limited` metriğiyle koşuyu **kırmızıya** düşürüyor ve
çıktıya çözümü yazıyor.

**C — Ölçüm betiğim iki dakika koştu, 5,4 milyon yineleme "tamamladı", tablo BOŞ çıktı ve
koşu BAŞARILI göründü.** k6 metrikleri yalnız *init* bağlamında kabul ediyor; tembel
kurulan trend her yinelemede istisna attı. 🔑 **Ölçüm altyapısının kendi sessiz hatası** —
bu fazın kendi konusunun başına geldi. ✅ `checks: ['rate>0.99']` + *"satır yoksa bu bir
taban çizgisi DEĞİLDİR"* kapısı eklendi.

**D — Panel, API'nin ölçümlerini SESSİZCE düşürüyordu (canlı doğrulamada bulundu).**
İki süreç farklı kova sürümleriyle koşarken `TryParse` karşı tarafın **9 kaydını** reddetti.
Reddetmek **doğru** karardı (bayat sayıları yanlış kovalara dağıtmak veriyi kaybetmekten
kötüdür); yanlış olan **sessizce** yapmaktı — tablo eksilir ama "eksik" olduğunu söylemez.
✅ Reddedilen kayıt sayılıyor, `Degraded` işaretleniyor, sebep loglanıyor ve ekran yazıyor.

**E — Ekran 40 satırda kesiyordu ve BUNU SÖYLEMİYORDU** (kendi testim yakaladı, aşağıda).
✅ *"@N handler'dan @M tanesi gösteriliyor"* satırı eklendi.

**F — `AuditAction "reset"` için Türkçe etiket UNUTULDU.**
Denetim izi *"Bilinmeyen işlem (reset)"* basıyordu (Değişmez Kural #6).
🔑 **Yakalayan şey benim testim değil, projenin kendi koruması oldu:**
`PanelAuditLogTests.AuditAction_HasTurkishLabel_ForEveryActionInSource` kapsamını
**kaynaktan türetiyor**. §7 madde 19 ailesinin bir kardeşi — ve kapsamı türeten bir testin
değeri tam olarak bu.

## 🧪 Bozma turu — 4 bozma, 4 kırmızı

| Bozma | Beklenen | Ölçülen |
|---|---|---|
| `Pagination.MaxLimit` 50 → 5000 | p95 belirgin bozulmalı | `?limit=5000` · p50 **3,4 → 40,2 ms** (11,8×), gövde **17 KB → 1,66 MB** (97×) |
| Üç haber GIN indeksi düşürüldü | fark görünmeli | 30.180 satırda `BitmapOr` **6,8 ms** → `Seq Scan` **46,2 ms** (6,8×) |
| `ix_ads_title_trgm` ölü hâline döndürüldü | `TrigramIndexTests` kırmızı | kırmızı (ilk denemede) |
| k6 trend'i init bağlamı dışında | koşu kırmızı olmalı | **ilk yazımda yeşil görünüyordu** → `checks` eşiği eklendi |

🔑 İlk ikisi birlikte bir şey söylüyor: **senaryo gerçekten zorluyor.** Bozulmasaydı ölçüm
yalancı olurdu ve bir sonraki oturum ona güvenip yanlış karar verirdi.

🐛 **Kendi testim de bir kez kırmızıya düştü ve haklıydı:**
`TheScreen_ShowsRealMeasurements` tek başına **yeşil**, tam süitte **kırmızı** koştu —
panel testleri tek uygulamayı paylaşıyor, süit boyunca yüzlerce handler ölçülüyor ve tablo
40 satırda kesiyor. 🔑 **Ders: paylaşılan durum üzerine kurulan bir iddia, o durumu ÖNCE
kendisi kurmalı.** (Ve kesme davranışı ekrandan da eksikti — bulgu **E**.)

## 📌 Plan dışı yapılanlar (kullanıcı sözleşmesi: serbest ama raporla)

1. **Panelde Performans ekranı** — plan yalnız *"taban çizgisini bir tabloya yaz"* diyordu.
   Yazılı tablo **bir gün sonra bayattır**; ekran **her zaman** bakar. Ayrıca süreçler arası
   birleştirme, ekran olmadan hiç gerekmezdi.
2. **`RequestMetricsSnapshot.Degraded` + kaynak listesi** — boş bir tablonun *"hiç istek
   gelmedi"* mi *"ölçüm çalışmıyor"* mu olduğunu ayırt etmek için.
3. **Sayaç sıfırlama** (denetim izli) — taban çizgisi ölçümünün ön koşulu.
4. **Kova çözünürlüğü ölçümle ayarlandı** — 15 ve 75 ms eklendi (k6 19 ms derken panel
   ≤25 ms diyordu; %30 fazla).
5. **`perf/README.md`** — *"iki ölçüm neden var"* tablosu (k6 dışarıdan, panel içeriden).
6. **~102 haber çekildi** (78 → 180). ⚠️ **Tüm arşiv (273 istek) BİLEREK ÇEKİLMEDİ** —
   kullanıcının açık talimatı. Ayar `News:Backfill:MaxPosts` **50'de bırakıldı** (arşiv
   derinliği hâlâ açık bir üründ kararı); geçici olarak 180'e çıkarılıp geri alındı.
   📌 Yerel veritabanında 180 haber kalması zararsız: mutabakat penceresinin **tabanı**
   (`floor`) daha eski kayıtları koruyor (ölçüldü).

## 🔬 Yan gözlem (12.22'nin kapsamı dışında ama yazılmalı)

**API ve panel İKİ AYRI Hangfire sunucusu çalıştırıyor, aynı kuyruk üzerinde.**
`AddInfrastructure` ikisinde de `AddHangfireServer()` çağırıyor. Bu oturumda somut bir
belirti üretti: panelden tetiklenen arşiv koşusu **panel sürecinde** koştu ve o süreçte
`News:Backfill:MaxPosts` farklıydı → iş **hiçbir şey yapmadan** "tamamlandı" dedi.
🔑 Hasarın biçimi tanıdık: **hata yok, log temiz, sonuç yanlış.** İki sürecin yapılandırması
ayrışırsa hangi işin nerede koşacağı **belirsizdir**. Bugün ikisi de aynı `appsettings`'i
okuduğu için sorun yok; 12.21 onları **ayrı konteynerlere** aldığı için bu artık ayrışabilir.
📌 Bu bir bulgu değil bir **risk**; kapatılması ayrı bir karar (panelde Hangfire sunucusunu
kapatmak ya da kuyrukları ayırmak).

---

# ✅ 12.23 TESLİM — *SharedPreferences sertleştirmesi* (20 Ağustos 2026)

**Nereden çıktı:** kullanıcının sorusundan — *"projede `SharedPreferences` kullanımı var mı,
başarılı mı, abim sorun yaşadığını söylüyor, Flutter'dan vazgeçmeyi düşünüyor."* Önce
**denetim** koşuldu (kod değişikliği yok), altı bulgu çıktı; sonra beşi kapatıldı, altıncısı
**tetikleyici koşuluyla** açık maddeler panosuna yazıldı.

**Yeşil taban:** `flutter analyze` **0** · `flutter test` **904/904** (865 → **+39**) ·
`dotnet test` **1325/1325**. Backend'e **tek satır** dokunulmadı, DTO değişikliği **yok**,
migration **yok**. Görünmez sözleşme **84 → 86**.

## 🔬 Önce ölçüm: envanter

`shared_preferences` **7 anahtar / 6 dosya**: `settings.themeMode` · `news.textScale` ·
`auth.guestChoice` · `auth.cachedUser` · `ads.draft` · `taxis.recentCalls` · `news.saved`.

🔑 **En önemli bulgu bir yokluktu ve olumluydu: oturum jetonları burada DEĞİL.**
`token_store.dart` aramaya takılıyor ama sebebi `encryptedSharedPreferences: true` satırı —
jetonlar `flutter_secure_storage`ta (Keychain / EncryptedSharedPreferences). Yani
*"SharedPreferences sorunları"*nın en tehlikeli sınıfı (kimlik bilgisini düz metin XML'de
tutmak) bu projede **hiç yaşanmıyor**. Sunucu verisi önbelleği de prefs'te değil.

📌 **Abinin iddiasına dürüst cevap:** `SharedPreferences` Flutter'ın icadı değil — Android'in
kendi API'si, iOS'ta `NSUserDefaults`; eklenti ince bir köprü. Native'e geçmek bu sorunları
**çözmez**, aynı iki API doğrudan kullanılır (üstelik Google native Android'de de
`SharedPreferences`'ı DataStore lehine geride bırakıyor — geçiş **iki kez** yapılırdı).
Ama iddia boş da değildi: aşağıdaki **S2**, sahada en çok bildirilen *"oturum bozuldu / dış
servis çalışmıyor"* vakasının birebir kendisi ve **bizde de açıktı**.

## 🔴 KARAR 1 (S1) — tercih deposu açılışı öldüremez

`main()` çıplak bir `await SharedPreferences.getInstance()` bekliyordu. Patlarsa belirti
**siyah ekran**, sebep **hiçbir yerde**: `FlutterError.onError` ve
`PlatformDispatcher.onError` o satırdan **20 satır sonra** bağlanıyor.

🔑 **Karar yeni bir kalıp değil, var olanın ikinci uygulaması:**
`FirebasePushMessaging.tryInitialize()` zaten *"uygulama hiçbir durumda push yüzünden
açılamaz hâle gelmez"* diyor — ve prefs push'tan **önce** koşuyor.

🔑 **Bellek içine düşmek güvenli ve bu ÖLÇÜLDÜ, varsayılmadı** — yedi anahtarın yokluğu tek
tek sayıldı: tema/okuma boyutu varsayılana döner · `auth.guestChoice` okunamayınca kullanıcı
**Giriş ekranını görür** (misafire *sessizce* düşmez) · `auth.cachedUser` boşalır ama
**oturum düşmez**, çünkü jetonlar ayrı depoda ve `bootstrap()` yine `hasSession()`'a bakıyor.
Hiçbiri bir şeyi *yanlış* yapmıyor, yalnız *unutuyor*.

🔴 **Ama sessiz olamaz — ve asıl tasarım kararı bu.** Bellek içi depoda yazma **başarılı
GÖRÜNÜR**: kullanıcı haberi kaydeder, yer imi dolar, ertesi gün liste boştur. Durum
`preferencesDegradedProvider` ile taşınıyor ve **Ayarlar ekranı bunu yazıyor**
(12.21b'nin dersi: *blokaj doğruydu, eksik olan dürüstlüktü*).

⚠️ **İki kapalı yol ölçüldü:** `SharedPreferences.setMockInitialValues` paket tarafından
`@visibleForTesting` işaretli → üretimde `flutter analyze` kırmızı. `SharedPreferences`
somut sınıf, `implements` edilemez. Kullanılan yol: `InMemorySharedPreferencesStore`
(**public**, `PlatformInterface` belirteçli). ⚠️ İkinci `getInstance()` çalışıyor çünkü
paket ilk hatada `_completer`'ı `null`'a çekiyor — **yeniden deneme paketin tasarımı**,
bizim şansımız değil (kaynağı okundu).

➕ **Plan dışı:** `sharedPreferencesProvider` `core/theme/theme_controller.dart`'tan
`core/preferences/app_preferences.dart`'a **taşındı**. Bir altyapı provider'ının sahibi tema
denetleyicisi olamazdı (dört özellik ondan `show` ile içe aktarıyordu) ve 12.23 yanına ikinci
bir provider koyuyordu. Altı çağrı yeri güncellendi, davranış aynı.

## 🔴 KARAR 2 (S2) — Android'de yedekleme kapalı, **iki yönden**

`AndroidManifest.xml`'de `allowBackup` / `dataExtractionRules` / `fullBackupContent`
**hiçbiri yoktu** → Android varsayılanı **AÇIK**. Buluta ve yeni cihaza giden iki şey:
- `auth.cachedUser` — **düz metin profil** (ad · mahalle · e-posta). 12.16–12.17'de bütün
  bir KVKK bloğu kapatılmışken kişisel verinin sessizce Google Drive'a gitmesi.
- `flutter_secure_storage`ın **`EncryptedSharedPreferences`** dosyası — şifreleme anahtarı
  **cihaza bağlı**, yedekten dönen cihazda **çözülemez**, okuma anında patlar.
  🔑 **Bu, Flutter'da en sık bildirilen *"oturum bozuldu"* vakası ve suçlusu Android.**

🔴 **`allowBackup="false"` TEK BAŞINA YETMEZ — ölçüldü, tahmin edilmedi.** Android belgesi
birebir: API 31+ hedefleyen uygulamalarda *"bazı üreticilerin cihazlarında `allowBackup`
bulut yedeklemesini kapatır ama **cihazdan cihaza aktarımı kapatmaz**"*. Yalnız o yazılsaydı
koruma modern Android'lerin bir kısmında **yarım ölü** olur ve *"yedekleme kapalı mı?"*
sorusunun cevabı **yanlış bir 'evet'** olurdu — **12.22'nin ölü trigram indeksiyle aynı
hasar sınıfı** (*"indeks var mı?"* → yanlış bir "var"). Bu yüzden **ikisi birden** yazıldı.

⚠️ **Dışlama anahtar seviyesinde YAPILAMAZ** (araştırıldı): kural dosyası *dosya* seviyesinde
çalışır, yedi tercih anahtarı ve şifreli jeton deposu **aynı `sharedpref` alanında** yaşıyor
→ seçim hep-ya-hiç. **Feda edilen:** cihaz değiştiren kullanıcı tema · okuma boyutu ·
kaydedilenlerini kaybeder. Kabul edildi, çünkü *"kaydedilenler tek cihaza bağlıdır"* zaten
**yazılı** bir sınır (§7 madde 62) — karar var olan sözleşmeyi **bozmuyor, teyit ediyor**.

## Diğer üç bulgu

**S3 — `_writeCachedUser` / `_clearCachedUser` `Future`'ı yere düşürüyordu.** `void`
dönüyorlardı: yazma başarısız olsa kimse bilmez, üstelik yakalanmamış hata
`PlatformDispatcher.onError`'a **bağlamsız** düşer. `Future<void>` yapıldı; çağrı yerlerinde
`await` ya da **açık** `unawaited(...)` (projede zaten var olan deyim). Beklememek doğruydu —
`applyProfile` iyimser güncelleme yapan **senkron** bir metot — ama beklememeyi *yazmak*
gerekiyordu.

**S4 — `AdDraftStore`'un yorumu dört fazdır yanlıştı.** *"Taslak her değişiklikte saklanır"*
diyor ve gerekçe olarak *"kullanıcı telefonu kilitlerse / bir aramaya cevap verirse"*
senaryosunu sayıyordu. Ölçüm: `_saveDraft()`'in **dört** çağrı yeri var (kategori seçimi ·
iki adım geçişi · geri tuşu diyaloğu) ve `WidgetsBindingObserver` kod tabanında **hiç
yoktu** → **yorumun saydığı senaryo, tam olarak kapsanmayan senaryoydu.** Kapsanan tek şey
geri tuşuydu. `didChangeAppLifecycleState(paused/hidden)` eklendi **ve yorum gerçeğe
hizalandı**. ⚠️ Yazma eşzamansız: bu **en iyi çabadır, garanti değil** — ve kod bunu yazıyor.

**S5 — üç deponun birim testi hiç yoktu.** `SavedNewsStore` · `AdDraftStore` ·
`RecentTaxiCallsStore`: `test/`'te **sıfır** referans. Yani §7 madde 62'nin dört yüzü
(tavan · gövde düşürme · bozuk satır toleransı · anlık görüntü) **ölçülmüyordu**; tek dolaylı
kapsam `news_screen_test.dart`'ın prefs'e **ham string** ile beslediği iki senaryoydu — ve o
ham string (`'news.saved'`) anahtar değişseydi **yeşil kalırdı**. 28 test yazıldı, anahtar
artık `SavedNewsStore.prefsKey`'den okunuyor.

## 🔬 S6 — İNCELENDİ, YAPILMADI (tetikleyici koşuluyla panoya yazıldı)

`SharedPreferencesAsync` geçişi. Ayrıntı ve **nasıl yapılacağı** açık maddeler panosunda
(bölüm B). Özeti: legacy API'nin tek gerçek zaafı **isolate'ler arası tutarsızlık**, bu
projede arka plan isolate'i prefs'e **hiç dokunmuyor** (ölçüldü) → bugün gerekmiyor.
🔴 Tetikleyici: *arka plan isolate'inde prefs'e yazma ihtiyacı doğduğu gün.*

## Teslim edilen dosyalar

**Yeni:** `mobile/lib/core/preferences/app_preferences.dart` ·
`mobile/android/app/src/main/res/xml/data_extraction_rules.xml` ·
`mobile/test/core/preferences/app_preferences_test.dart` ·
`mobile/test/features/news/saved_news_store_test.dart` ·
`mobile/test/features/ads/ad_draft_store_test.dart` ·
`mobile/test/features/taxis/recent_taxi_calls_store_test.dart`
**Değişen:** `main.dart` · `theme_controller.dart` · `auth_controller.dart` ·
`ad_form_screen.dart` · `ad_draft_store.dart` (yorum) · `settings_screen.dart` ·
`AndroidManifest.xml` · `pubspec.yaml` + altı import + üç test yardımcısı.

## 🧪 Bozma turu — **14 kilit, 14 kırmızı**

| # | Mutasyon | Sonuç |
|---|---|---|
| 1 | S1 fallback kaldırıldı (siyah ekran geri geldi) | 🔴 |
| 2 | `isDegraded` **her zaman** `true` | 🔴 |
| 3 | Ayarlar'daki bozulma şeridi kaldırıldı | 🔴 |
| 4 | `allowBackup="false"` silindi | 🔴 |
| 5 | `dataExtractionRules` bağı silindi (D2D açık kalır) | 🔴 |
| 6 | `<device-transfer>` bölümü silindi (yarım koruma) | 🔴 |
| 7 | Arka plan taslak kancası kaldırıldı | 🔴 |
| 8 | `news.saved` tavanı kaldırıldı | 🔴 |
| 9 | Bozuk satır toleransı kaldırıldı | 🔴 |
| 10 | Gövde depoya yazılıyor | 🔴 |
| 11 | Taksicinin **telefonu** depoya sızıyor | 🔴 |
| 12 | Taksi tavanı (3) kaldırıldı | 🔴 |
| 13 | Taslak 7 gün sınırı kaldırıldı | 🔴 |
| 14 | "Anlamlı taslak" kuralı kaldırıldı | 🔴 |

🔑 **2 ve 6 bilinçli olarak eklendi ve turun değerli yarısı onlar.** İkisi de *"koruma var
görünürken yok"* sınıfını ölçüyor: 2 olmadan her açılışta uyaran bir gerçekleme yeşil
kalırdı (uyarının değeri **nadir** olmasından geliyor), 6 olmadan tek yönlü bir yedekleme
kapısı *"kapattık"* sanılırdı.

## 🔑 Fazın dersi

**Bir yorum SIKLIK iddia ediyorsa (*"her değişiklikte"*, *"otomatik olarak"*), o iddia madde
80'in kapsamındadır ama `CommentReferenceTests` onu YAKALAYAMAZ** — sarkan bir işaretçi değil,
**yanlış bir iddia**. Madde 80 kendi sınırını zaten yazıyordu; 12.23 o sınırın **canlı
örneğini** buldu: dört fazdır duran bir yorum, kapsamadığı senaryoyu gerekçe olarak
sayıyordu. Çağrı yerlerini **saymaktan** başka yolu yok.
