# KadirliApp — Mimari Haritası

> **Bu doküman ne işe yarar?** *"Yarın bir modül eklemek, değiştirmek ya da kaldırmak
> istersek yapının bozulmayacağını nereden bileceğiz? Neyin nerede ve ne amaçla olduğunu
> nasıl bulacağız?"* — Bu dosya o sorunun cevabıdır.
>
> **Bu doküman ne DEĞİLDİR?** Kronolojik bir günlük değil (o `Memory_Bank/Progress.md`),
> öğretici bir rehber değil (o `DOTNET_MASTERCLASS.md`), istemci kontratı değil
> (o `Memory_Bank/API_CONTRACT.md`). Burası **harita**: bugün neyin nerede olduğu.
>
> Son güncelleme: 9 Ağustos 2026 (Faz 12.5 — ulaşım alan modeli: araç tipi · kalkış noktası · sefer günleri).

## Hangi dokümanı ne zaman okumalı

| Soru | Doküman |
|---|---|
| "Neyin nerede? Nasıl modül eklerim?" | **Bu dosya** |
| "Mobil istemci sunucuyla nasıl konuşuyor?" | `Memory_Bank/API_CONTRACT.md` |
| "Bu karar neden böyle verilmiş?" | `Memory_Bank/Progress.md` + `Memory_Bank/Active_Context.md` |
| "Bu .NET/Clean Architecture kalıbı ne demek?" | `DOTNET_MASTERCLASS.md` |
| "Mobil tasarım sistemi / UX kuralları?" | `Memory_Bank/MOBILE_UX_PLAN.md` |
| "Uçların makine-okur şeması?" | `docs/openapi.json` |
| "Projeyi nasıl çalıştırırım?" | `CLAUDE.md` (30 saniyelik özet), `mobile/README.md` |

---

## 1. Sistem bir bakışta

```
┌──────────────────┐        ┌──────────────────────┐
│  Mobil (Flutter) │        │  Admin Panel (MVC)   │
│  mobile/         │        │  KadirliApp.Web      │
└────────┬─────────┘        └──────────┬───────────┘
         │  HTTPS /v1/*                │  Razor + cookie auth
         │  JWT Bearer                 │
         ▼                             ▼
┌───────────────────────────────────────────────────┐
│  KadirliApp.Api  (ASP.NET Core, port 5005)        │
│  Controllers/ (public) + Controllers/Admin/       │
└────────────────────────┬──────────────────────────┘
                         │  MediatR (Command/Query)
                         ▼
┌───────────────────────────────────────────────────┐
│  KadirliApp.Application  — iş kuralları           │
│  Features/<Modül>/{Commands,Queries,Dtos}         │
└────────────────────────┬──────────────────────────┘
                         ▼
┌───────────────────────────────────────────────────┐
│  KadirliApp.Domain  — varlıklar, enum'lar         │
└───────────────────────────────────────────────────┘
                         ▲
┌───────────────────────────────────────────────────┐
│  KadirliApp.Infrastructure — EF Core, Redis,      │
│  Hangfire, dosya depolama, FCM, JWT               │
└───────────────────────────────────────────────────┘

Dış servisler: PostgreSQL 15 · Redis 7 · Seq (log) · Firebase FCM (push) · SMS sağlayıcısı
```

### Katman kuralı — **yanlış yön DERLENMEZ**

Bu bir "disiplin meselesi" değil, proje referanslarıyla zorlanmış:

```
Domain      → (hiçbir şeye bağımlı değil)
Application → Domain
Infrastructure → Application
Api         → Infrastructure
Web         → Infrastructure, Application
```

`Domain`'e `Microsoft.EntityFrameworkCore` sızdıramazsınız; `Application` içinden
`DbContext`'e dokunamazsınız (`IUnitOfWork`/`IRepository<T>` arayüzleri üzerinden gidilir).
Bir katman ihlali denerseniz **build hata verir** — bu güvence bilinçlidir ve yeni gelen
birinin yanlışlıkla mimariyi bozmasını imkânsız kılar.

Aynı disiplin mobilde de var: `lib/features/<modül>/{data,application,presentation}` ve
ortak `lib/core/*`. Bir feature başka bir feature'ın `presentation`'ına bakmaz.

---

## 2. Klasör haritası

### Backend

| Yol | İçerik |
|---|---|
| `KadirliApp.Domain/Entities/` | 50+ EF varlığı (`Ad`, `Announcement`, `Place`…) |
| `KadirliApp.Infrastructure/Persistence/*Backfill.cs` | 🔑 Açılışta koşan **idempotent geri doldurmalar** (`PowerOutageNeighborhoodBackfill` 12.3, `EventDistrictBackfill` 12.4). ⚠️ **12.5'te bilinçli olarak geri doldurma YOK**: 12.5 öncesi hatların kalkış noktası gerçekten bilinmiyor ve bir tahmin ("hepsi otogardan") vatandaşı **yanlış yere** götürürdü — panel o boşluğu uyarı olarak gösteriyor, doldurmuyor — migration'da kör SQL değil, çünkü eşleştirme kuralı uygulama kodunda (§7 madde 40/45) |
| `KadirliApp.Domain/Enums/` | `AdStatus`, `UserRole`, `PropertyType`… + 🔑 saf değer nesneleri: `OperatingDays` (7 bitlik sefer gün maskesi, **Pazar=0 kaymasının tek çözüm yeri**), `TransportVehicleTypes` (12.5) |
| `KadirliApp.Domain/Common/` | `BaseEntity` (Id/CreatedAt/UpdatedAt), `ISoftDeletable` |
| `KadirliApp.Application/Features/<Modül>/` | **24 modül**, her biri `Commands/`, `Queries/`, `Dtos/` |
| `KadirliApp.Application/Common/` | `IUnitOfWork`, `IRepository<T>`, istisnalar, davranışlar |
| `KadirliApp.Infrastructure/Persistence/` | `AppDbContext`, `Configurations/`, `DbSeeder`, `MockDataSeeder` |
| `KadirliApp.Infrastructure/Migrations/` | EF migration'ları |
| `KadirliApp.Infrastructure/Jobs/` | Hangfire işleri (aşağıda) |
| `KadirliApp.Infrastructure/{Caching,Files,Identity,Notifications,Health}/` | Redis, dosya depolama, JWT, FCM, health-check |
| `KadirliApp.Infrastructure/Observability/` | 🔑 `ChannelErrorLogSink` — hata kaydının **isteği bloklamayan** yazıcısı (kuyruk + `BackgroundService`) |
| `KadirliApp.Api/Controllers/` | **18 public controller** (`/v1/*`) |
| `KadirliApp.Api/Controllers/Admin/` | **18 admin controller** (`/v1/admin/*`) + ortak taban |
| `KadirliApp.Api/Authorization/` | `RequirePermissionAttribute` + policy sağlayıcı |
| `KadirliApp.Web/Controllers/` + `Views/` | **24 panel controller** + Razor görünümleri |
| `KadirliApp.Tests/` | `Unit/` + `Integration/` (aşağıda test haritası) |
| `secrets/` | **git'e girmez**; `secrets/README.md` neyin nasıl edinileceğini anlatır |

### Mobil (`mobile/`)

| Yol | İçerik |
|---|---|
| `lib/core/config/` | `Env` (flavor, base URL, `--dart-define` override'ları) |
| `lib/core/network/` | İki Dio istemcisi, `EnvelopeInterceptor`, `AuthInterceptor`, `ApiClient`, hata sözlüğü, `retry_policy` |
| `lib/core/router/` | `app_routes.dart`, `app_shell.dart` (4 sekmeli `StatefulShellRoute`), tek redirect noktası, 🔑 `app_nav.dart` (**kabuk rotasına güvenli gezinme** — §7 kod-dışı) |
| `lib/core/navigation/app_modules.dart` | 🔑 **Modül kaydı** — ızgara + rota + uç listesi tek yerde |
| `lib/core/theme/` | Renk token'ları, `AppPalette` ThemeExtension, açık/koyu tema |
| `lib/core/paging/paged_feed.dart` | 🔑 Ortak sayfalama çekirdeği (yarış, mükerrer eleme, filtre) |
| `lib/core/push/` | 🔑 Push soyutlaması: `PushMessaging` arayüzü + `NoopPushMessaging` + Firebase gerçeklemesi. **Yapılandırma yoksa uygulama push'suz açılır, çökmez** |
| `lib/core/widgets/` | `AppScaffold`, `AppButton`, `AppCard`, `ContactActions`, `LookupDropdown`, `MonthCalendar`… |
| `lib/core/utils/` | `AppDate` (sabit UTC+3), `AppMoney`, `AppLinks`, `AppImage`, `AppShare`, `Debouncer` |
| `lib/core/observability/` | `ErrorReporter` — çökme/hata bildirimi (ateşle-unut, yeniden denemez, kendi hatasını raporlamaz) |
| `lib/features/<modül>/data/` | Model + repository (yalnız burası Dio görür) |
| `lib/features/<modül>/application/` | Provider'lar, denetleyiciler, saf mantık |
| `lib/features/<modül>/presentation/` | Ekranlar + `widgets/` |
| `test/` | **696 test** (70 dosya); klasör yapısı `lib/`'i aynalar |

---

## 3. Modül envanteri

**Nasıl okunur:** "İlan fiyatı doğrulaması nerede?" → İlanlar satırı → `Features/Ads/AdSubmissionRules.cs`.
`<M>` = modül klasör adı. Panel sütunu `KadirliApp.Web/Controllers/` + aynı adlı `Views/` klasörü.

| # | Modül | Backend `Features/` | Public uçlar (`/v1/…`) | Panel | İzin adı | Mobil `lib/features/` | Mobil rota |
|---|---|---|---|---|---|---|---|
| 1 | **İlanlar** | `Ads/` (+`AdSubmissionRules.cs`) | `ads`, `ads/{id}`, `ads/categories`, `ads/categories/{id}/properties`, `ads/{id}/favorite`, `ads/{id}/extend`, `ads/{id}/track-phone`, `ads/{id}/track-whatsapp` | `AdsAdmin`, `AdCategoriesAdmin` | `ads` | `ads/` | `/ilanlar`, `/ilanlar/:id`, `/ilan-ver`, `/ilan-duzenle/:id` |
| 2 | **Duyurular** | `Announcements/` | `announcements`, `announcements/types`, `announcements/{id}`, `…/view`, `…/click` | `AnnouncementsAdmin` | `announcements` | `announcements/` | `/duyurular`, `/duyurular/:id` |
| 3 | **Eczane** | `Pharmacies/` | `pharmacies`, `pharmacies/on-duty`, `pharmacies/schedule`, `pharmacies/{id}` | `PharmaciesAdmin` | `pharmacies` | `pharmacies/` | `/eczaneler`, `/eczaneler/:id` |
| 4 | **Vefat** | `Deaths/` | `deaths`, `deaths/{id}`, `deaths/cemeteries`, `deaths/mosques`, `POST deaths` | `DeathsAdmin` | `deaths` | `deaths/` | `/vefat`, `/vefat/:id`, `/vefat-bildir` |
| 5 | **Etkinlikler** | `Events/` | `events`, `events/categories`, `events/calendar`, `events/{id}` | `EventsAdmin` | `events` | `events/` | `/etkinlikler`, `/etkinlikler/:id` |
| 6 | **Kampanyalar** | `Campaigns/` | `campaigns`, `campaigns/{id}`, `campaigns/{id}/view-code` | `CampaignsAdmin` | `campaigns` | `campaigns/` | `/kampanyalar`, `/kampanyalar/:id` |
| 7 | **İşletmeler** | `Businesses/` | *(public uç yok — kampanyanın sahibi)* | `BusinessesAdmin` | `businesses` | *(yok)* | — |
| 8 | **Mekanlar** | `Places/` | `places`, `places/categories`, `places/{id}` | `PlacesAdmin` | `places` | `places/` | `/mekanlar`, `/mekanlar/:id` |
| 9 | **Taksi** | `Taxis/` | `taxis/drivers`, `taxis/drivers/{id}`, `taxis/drivers/{id}/call` | `TaxiAdmin` | `taxis` | `taxis/` | `/taksi`, `/taksi/:id` |
| 10 | **Rehber** | `Guide/` | `guide/items`, `guide/items/{id}`, `guide/categories`, `guide/categories/{id}` | `GuideAdmin` | `guide` | `guide/` | `/rehber`, `/rehber/:id` |
| 11 | **Ulaşım** | `Transport/` (+`OperatingDays` · `IntercityRouteProjection` · `TransportDeparturePointResolver`) | `transport/intercity-routes`, `transport/intracity-routes` | `TransportAdmin` | `transport` | `transport/` | `/ulasim` *(detay rotası YOK — §6)* |
| 12 | **Kesintiler** | `PowerOutages/` | `power-outages`, `power-outages/{id}` | `PowerOutagesAdmin` | `power-outages` | `power_outages/` | `/kesintiler`, `/kesintiler/:id` |
| 13 | **Şikayet/İstek** | `Complaints/` | `POST complaints`, `complaints/my` | `ComplaintsAdmin` | `complaints` | `complaints/` | `/sikayet`, `/sikayet-bildir` |
| 14 | **Bildirimler** | `Notifications/` | `notifications`, `…/{id}/read`, `notifications/read-all`, `notifications/fcm-token` | *(yok — gönderim panosu 26. satırda)* | — | `notifications/` (+ `core/push/`) | Bildirim sekmesi |
| 15 | **Kimlik** | `Auth/` | `auth/login`, `auth/verify-otp`, `auth/register`, `auth/refresh`, `auth/logout` | `Account` | — | `auth/` | `/giris`, `/kayit` |
| 16 | **Kullanıcı** | `Users/` | `users/me`, `users/me/notifications`, `users/me/ads`, `users/me/favorites`, `DELETE users/me` | `UsersAdmin` | `users` | `profile/`, `settings/` | Profil sekmesi, `/ayarlar` |
| 17 | **Dosyalar** | `Files/` | `files/upload`, `DELETE files/{id}` | *(yok)* | — | `files/` | *(ekran yok — ortak repo)* |
| 18 | **Sözlükler** | `Lookups/` | `neighborhoods` (+ modül içi `cemeteries`/`mosques`/`categories`) · **`districts` ve `transport_departure_points` public uç YOK** (ad/etiket/koordinat DTO'da hazır geliyor) | `LookupsAdmin` | `lookups` | `lookups/` | *(ekran yok)* |
| 19 | **Personel** | `Staff/` | *(public uç yok)* | `StaffAdmin` | *(matris dışı — yalnız admin, **12.2'de düzeltildi**)* | *(yok)* | — |
| 20 | **Panel istatistik** | `Dashboard/` | *(public uç yok)* | `Dashboard` | `dashboard` | *(yok)* | — |
| 21 | **Denetim izi** | `Audit/` | *(public uç yok)* | `AuditLogsAdmin` | *(matris dışı — yalnız admin)* | *(yok)* | — |
| 22 | **Çöp kutusu** | `Trash/` | *(public uç yok)* | `TrashAdmin` | *(matris dışı — yalnız admin)* | *(yok)* | — |
| 23 | **Global arama** | `Search/` | *(public uç yok)* | `GlobalSearch` | *(matris dışı — **sonucu süzer**, aşağıya bak)* | *(yok)* | — |
| 24 | **Hata kayıtları** | `ErrorLogs/` | `POST client-errors` *(anonim)* | `ErrorLogsAdmin` | *(matris dışı — yalnız admin)* | *(ekran yok — `core/observability/`)* | — |
| 25 | **Giriş denemeleri** | `LoginAttempts/` | *(public uç yok — kayıt giriş akışında düşer)* | `LoginAttemptsAdmin` | *(matris dışı — yalnız admin)* | *(yok)* | — |
| 26 | **Bildirim gönderimleri** | `PushCampaigns/` | *(public uç yok — kayıt gönderim anında düşer)* | `PushCampaignsAdmin` | *(matris dışı — yalnız admin)* | *(ekran yok — bildirim modülü 14. satırda)* | — |

**Mobilde ayrıca ekran taşıyan ama backend modülü olmayan klasörler:** `home/` (hub),
`common/`, `dev/` (yalnız debug: `/gelistirici/tasarim`, `/gelistirici/ag`).

### Ana Sayfa ızgarasındaki 12 modül

`mobile/lib/core/navigation/app_modules.dart` → `kAppModules`. Bu liste **tek doğruluk
kaynağıdır**: ızgara kartları, "yakında" ekranları ve (11.13'te) push deep-link eşlemesi
buradan türer. `app_modules_test.dart` her kartın açılabilir bir ekrana gittiğini
denetler — **"işlevsiz buton yok" şartı test edilebilir hâlde**.

### Panelde roller ve izinler (Faz 11.15b)

| Rol | Panelde ne görür |
|---|---|
| `super_admin` / `admin` | Her şey. İzin matrisi bu roller için **atlanır**. |
| `moderator` | Yalnız `admin_permissions`'ta **okuma izni** verilmiş modüller + Dashboard. Yazma/silme/onaylama ayrı bayraklara tabi. **Personel yönetimi, örnek veri basma, denetim izi ve çöp kutusu kapalı.** |
| diğer roller | Panele hiç giremez (`AccountController` girişte reddeder). |

Uygulama noktaları — üçü aynı modül anahtarını kullanır:
`KadirliApp.Web/Authorization/PanelPermissionAttribute.cs` (controller kapısı) ·
`KadirliApp.Web/Common/PanelMenu.cs` (menü, tek liste) ·
`StaffAdminController.Modules` (matris arayüzü).

⚠️ Yeni panel controller'ı eklerken: sınıfa `[Authorize(Roles = "admin,super_admin,moderator")]`
**ve** `[PanelPermission("<modül>")]` yazın, `PanelMenu.Items`'a satır ekleyin. Rol listesine
"moderator" yazıp özniteliği unutursanız moderatör o modülde **sınırsız** yetki kazanır —
`PanelModeratorPermissionTests` bunu yakalar.

⚠️ **Tek modüle ait olmayan bir ekran** ekliyorsanız (Faz 11.16b: `GlobalSearch`) üçüncü bir
desen var: `[Authorize(Roles = "admin,super_admin,moderator")]` + `[PanelPermission]` **yok** +
controller adı `PanelMenu.PermissionFilteredControllers`'ta. Burada izin **ekranın kapısında
değil sorgunun içinde** uygulanır: aranacak modüller `IPanelMenuProvider`'dan gelir, yani
moderatör menüde göremediği bir modülden **tek sonuç bile** almaz.
🔑 Bu istisna yalnız *kanıtlanabildiği* için güvenli: listeye ad yazmak yapısal testi
susturmaya yetmez — `GlobalSearchTests` süzmenin gerçekten çalıştığını ayrıca denetler,
`PanelModeratorPermissionTests` de listenin muafiyet çöplüğüne dönmesini engeller.

⚠️ **Yalnız admin'e açık bir ekran** ekliyorsanız (`StaffAdmin`, `AuditLogsAdmin`, `TrashAdmin`,
`ErrorLogsAdmin`, `LoginAttemptsAdmin`, `PushCampaignsAdmin`) desen farklıdır:
`[Authorize(Roles = "admin,super_admin")]` + `[PanelPermission]` **yok** +
`PanelMenu.Items` satırının `Module`'ü **`null`** + `AdminOnlyControllers`'a controller adı.
Modül anahtarı verirseniz izin matrisinde moderatöre dağıtılabilen ama rol kapısı yüzünden
asla çalışmayacak bir yetki belirir — 11.15b'nin en büyük bulgusu ("karşılığı olmayan yetki")
tam olarak buydu.

🔒 **Faz 12.2'den beri bu kural yapısal testle kilitli**
(`PanelModeratorPermissionTests.AdminOnlyControllers_AreOutsideThePermissionMatrix`):
`AdminOnlyControllers`'taki **her** controller'ın menü satırının `Module`'ü `null` olmak
zorunda. 🐛 Test yazıldığı gün **kırmızıydı**: dört ekrandan üçü kurala uyuyordu ama
`StaffAdmin` hem listede hem de `Module = "staff"` taşıyordu, yani "staff" izin matrisinde
görünüyor ve yöneticinin moderatöre verdiği o yetki hiçbir zaman çalışmıyordu. Aynı commit'te
düzeltildi; `admin_permissions`'taki ölü satırlar migration ile temizlendi.
⚠️ Komutlar hâlâ `AuditModule = "staff"` yazıyor — karşılığı `PanelDisplay.NonMatrixModules`'ta
(yoksa denetim izi ekranı ham İngilizce basar).

### Arka plan işleri (Hangfire)

| İş | Ne yapar | Sıklık |
|---|---|---|
| `ExpireAdsJob` | Süresi dolan ilanı `approved` → `expired` | Saatlik |
| `ArchiveDeathsJob` | `auto_archive_at` geçen vefat ilanını arşivler | Günlük |
| `PublishScheduledAnnouncementsJob` | Zamanlanmış duyuruyu yayınlar + bildirim satırı üretir | Dakikalık |
| `SendPushNotificationsJob` | Gönderilmemiş bildirimleri FCM'e yollar | Dakikalık |
| `PurgeErrorLogsJob` | Hata kaydı saklama süresi: çözülmüş 30 gün, çözülmemiş 90 gün | Günlük |
| `SecurityAlertJob` | İşlenmemiş şüpheli giriş denemelerini **tek e-postada** gruplar, `super_admin`'lere yollar (kural+alıcı başına saatte 1 kısma) | 5 dakikada bir |
| `PurgeLoginAttemptsJob` | Giriş denemesi saklama süresi: başarılı 90 gün, başarısız 180 gün | Günlük |
| `PurgeNotificationsJob` | **Okunmuş** bildirimler 90 gün sonra silinir; kampanya satırı **kalır** | Günlük |

⚠️ Panoya (`/hangfire`) erişen biri `PurgeLoginAttemptsJob`'ı elle tetikleyerek **yeni
topladığımız güvenlik kanıtını silebilir** — panonun korumasının 12.2'de gözden geçirilmesi
(`HangfireDashboardAuthorizationFilter` + `ForwardedHeaders`) tesadüf değil.

---

## 4. 🔑 Reçete: **Modül EKLE**

Sırayla; her adım bir öncekine dayanır. (Örnek: "Kayıp Eşya" modülü.)

### Backend

1. **Varlık** — `KadirliApp.Domain/Entities/LostItem.cs`, `BaseEntity`'den türet.
   Silinebilir olacaksa `ISoftDeletable` ekle (`DeletedAt`).
2. **Yapılandırma** — `KadirliApp.Infrastructure/Persistence/Configurations/LostItemConfiguration.cs`:
   tablo/kolon adları **snake_case**, indeksler, ilişkiler. Soft-delete varsa global query
   filtresi burada.
3. **`AppDbContext`** — `DbSet<LostItem> LostItems` ekle.
4. **Migration** —
   `dotnet ef migrations add AddLostItems -p KadirliApp.Infrastructure -s KadirliApp.Api`
   → üretilen SQL'i **oku**, sonra `dotnet ef database update`.
5. **Feature klasörü** — `KadirliApp.Application/Features/LostItems/`:
   - `Dtos/LostItemResponseDto.cs`, `Dtos/QueryLostItemDto.cs` (sayfalama/filtre)
   - `Queries/GetLostItemsQuery.cs`, `Queries/GetLostItemByIdQuery.cs`
   - `Commands/CreateLostItem/…`, `Commands/UpdateLostItem/…`, `Commands/DeleteLostItem/…`
   - MediatR handler'ları `IUnitOfWork` üzerinden çalışır — `DbContext`'e **dokunmaz**.
6. **Public controller** — `KadirliApp.Api/Controllers/LostItemsController.cs`,
   `ApiControllerBase`'den türet, `[Route("v1/lost-items")]`.
   ⚠️ Yol **kebab-case** olmalı (§6, `SlugifyParameterTransformer` çok kelimeli controller
   adını zaten çevirir; elle `[Route]` yazarken de kebab yaz).
   Public listede görünürlük filtresini **controller'da zorla**
   (`dto.IsActive = true;` gibi) — DTO'dan gelen değere güvenme.
7. **Admin controller** — `KadirliApp.Api/Controllers/Admin/LostItemsAdminController.cs`,
   **`AdminApiControllerBase`'den türet** (yetki politikası orada), her eyleme
   `[RequirePermission("lost-items", "read|create|update|delete|approve")]`.
8. **İzin adı** — `lost-items` iznini `permissions` tablosuna ekle (seed ya da migration) ve
   panel rollerine dağıt. İzin yoksa moderator 403 alır.
9. **Panel** — `KadirliApp.Web/Controllers/LostItemsAdminController.cs` +
   `Views/LostItemsAdmin/{Index,Create,Edit}.cshtml`. Mevcut bir modülün view'larını
   kopyalayıp uyarlamak en hızlısı (tablo + filtre + form deseni sabittir).
   Sınıfa **`[Authorize(Roles = "admin,super_admin,moderator")]` + `[PanelPermission("lost-items")]`**
   yazın ve **`PanelMenu.Items`'a bir satır ekleyin** (§3 "Panelde roller ve izinler").
10. **Kontrat** — `Memory_Bank/API_CONTRACT.md`'ye uçları ve DTO alanlarını yaz;
    `docs/openapi.json`'ı yenile.
11. **Testler** — en az:
    - görünürlük (`ModuleVisibilitySweepTests` desenine bir test ekle),
    - yetki (yeni uçlar `EndpointAuthorizationSweepTests`'te **kendiliğinden** kapsanır —
      anonim yazma ucu eklediysen o testin beklenen listesini bilinçli güncelle),
    - iş kuralı varsa birim testi (`AdSubmissionRulesTests` deseni).

### Mobil

12. **Model + repository** — `lib/features/lost_items/data/`:
    `lost_item.dart` (freezed değil, elle `fromJson` — proje deseni budur) +
    `lost_items_repository.dart` (yalnız burası `ApiClient` görür).
13. **Provider'lar** — `lib/features/lost_items/application/`:
    liste sayfalıysa **`PagedFeedController`** kullan (yeniden yazma).
    ⚠️ Her uç provider'ına **`retry: apiRetry`** ver (yoksa Riverpod 3 hatalı provider'ı
    sınırsız yeniden dener).
14. **Ekranlar** — `presentation/lost_items_screen.dart` (+ detay). `AppScaffold` kullan
    (pull-to-refresh + offline şeridi + kaydırma fiziği düzeltmesi orada).
15. **Rota** — `lib/core/router/app_routes.dart`'a sabit ekle, `app_router.dart`'a `GoRoute`.
    ⚠️ Form/detay rotalarını **kardeş** yap, iç içe değil (§6).
16. **Modül kaydı** — `lib/core/navigation/app_modules.dart` → `kAppModules`'a bir
    `AppModule(...)` satırı, `ready: true`. Izgara ve "yakında" ekranı kendiliğinden uyar.
17. **Testler** — `test/features/lost_items/`: model ayrıştırma + ekran durumları
    (boş/yükleniyor/hata) + varsa saf mantık.
18. **Doküman** — bu dosyadaki modül tablosuna satır ekle.
    ⚠️ `ArchitectureDocTests` tabloyu gerçekle karşılaştırıyor: satırı eklemezsen
    **`dotnet test` kırmızı olur.**

---

## 5. 🔑 Reçete: **Modül DEĞİŞTİR**

### DTO alanı eklemek — güvenli (additive)

Yeni alan eklemek kontratı **kırmaz**: mobil istemci bilmediği JSON alanlarını yok sayar,
eski sürümdeki uygulamalar çalışmaya devam eder. Yapılacaklar: DTO + Query/Command
projeksiyonu → `API_CONTRACT.md` → mobil modelde alanı oku → ekranda göster.

### DTO alanı silmek / yeniden adlandırmak — **KIRICI**

Mağazadaki eski sürümler o alanı okumaya devam eder. Kural:
1. Önce yeni alanı ekle, ikisini birlikte doldur.
2. Mobil yeni alana geç, sürüm yayınla.
3. Kullanıcıların ezici çoğunluğu güncelledikten **sonra** eski alanı sil.

Bu üç adım aynı gün yapılamaz — planla.

### Uç davranışını değiştirmek

- **Görünürlük kuralı** (hangi kayıtlar dönüyor): `ModuleVisibilitySweepTests` kırılır —
  kırılması doğrudur, testi bilinçli güncelle.
- **Sıralama / sayfalama:** sayfalar arası tutarlılığı bozmayan biçimde yap. Yeni bir
  `?sort=` değeri eklemek additive'dir (bilinmeyen değer varsayılana düşmeli);
  **varsayılanı değiştirmek** mobil listeyi sessizce ters çevirir.
- **Sayfalamayı kaldırmak/eklemek:** `data` şekli değişir (`[…]` ↔ `{items,…}`) →
  istemci ayrıştırıcısı patlar. `power-outages` bilerek sayfasız (§6).
- **Doğrulama sıkılaştırmak:** eski istemciler 400 almaya başlar. Gevşetmek güvenlidir.

### İş kuralı değiştirmek

Kural `Features/<M>/` içindedir (handler ya da `…Rules.cs`). Değiştirmeden önce:
`grep -r "KuralAdı" KadirliApp.Tests/` — hangi test kilitliyor? O testi **önce** güncelle,
sonra kuralı; böylece testin gerçekten o kuralı tuttuğunu görürsün.

### Mobil ekran değiştirmek

Ortak bileşene dokunuyorsan (`AppScaffold`, `PagedFeedController`, `ContactActions`,
`FilterChoiceChip`, `MonthCalendar`, `LookupDropdown`) **tek düzeltme 10+ ekranı
etkiler** — bu bir avantaj (11.12'de tek pull-to-refresh düzeltmesi 11 ekranı onardı) ama
tam süiti koşmadan commit etme.

---

## 6. 🔑 Reçete: **Modül KALDIR**

1. **Mobil önce.** `kAppModules`'tan satırı sil → ızgara kartı ve rota kaybolur.
   `app_routes.dart` + `app_router.dart`'tan rotaları, `lib/features/<m>/` klasörünü ve
   `test/features/<m>/` testlerini sil. Modülü referans veren ortak ekranlar
   (hub şeridi, deep-link eşlemesi) kalmışsa temizle.
2. **API uçları.** `Controllers/<M>Controller.cs` ve `Controllers/Admin/<M>AdminController.cs`
   sil. ⚠️ Mağazadaki **eski sürümler o uçları çağırmaya devam eder** → uç kaldırıldığında
   404 alır ve ekranı hata gösterir. Kabul edilebilirse sil; değilse önce uç "boş liste"
   döndürsün, bir sürüm sonra kaldırılsın.
3. **Panel.** `KadirliApp.Web/Controllers/<M>AdminController.cs` + `Views/<M>Admin/`,
   ayrıca menüdeki bağlantı (`Views/Shared/_Layout.cshtml`).
4. **Application.** `Features/<M>/` klasörünü sil. Başka modül bu DTO'ları kullanıyorsa
   derleme hatası verir — iyi haber, sessiz kalmaz.
5. **İzin.** `permissions` tablosundan modül iznini ve `role_permissions`/`admin_permissions`
   satırlarını temizleyen bir migration yaz.
6. **Veri — en kritik adım.** Tablo kalabilir mi?
   - **Soft-delete'li veriye ne olur?** `deleted_at` dolu satırlar zaten görünmüyor;
     tabloyu düşürürsen **geri dönüşü olmayan** silme yaparsın.
   - **Önerilen:** tabloyu düşürme. `DbSet`'i ve varlığı kaldır, tabloyu veritabanında
     bırak (yer kaplaması ihmal edilebilir, veri kurtarılabilir kalır). Migration'da
     yalnız yabancı anahtarları/indeksleri düşür.
   - Gerçekten silinecekse **önce yedek al** (`pg_dump -t tablo_adi`).
7. **Doküman.** Bu dosyadaki modül tablosundan satırı sil (yoksa `ArchitectureDocTests`
   kırmızı olur — bilerek), `API_CONTRACT.md`'den uçları çıkar, `openapi.json`'ı yenile.
8. **Testler.** `KadirliApp.Tests` içindeki modüle özel testleri sil; süpürme testleri
   (`EndpointAuthorizationSweepTests`) kendiliğinden uyar.

---

## 7. 🔑 GÖRÜNMEZ SÖZLEŞMELER

Koda bakarak anlaşılmayan, bozulunca **sessizce** hasar veren bağımlılıklar. Hepsi
**testle kilitli** — 1–22 `KadirliApp.Tests/Integration/Contracts/InvisibleContractsTests.cs`,
**23–26 (Faz 11.15c)** `Integration/Panel/PanelBusinessRuleTests.cs`, **27 (Faz 11.17)**
`Integration/Panel/PanelPowerOutageFilterTests.cs`, **28 (Faz 11.17)**
`Integration/Panel/PanelTrashTests.cs`, **29 (Faz 11.18)**
`Integration/Panel/PanelBulkActionTests.cs`, **30 (Faz 11.18)**
`Integration/Panel/PanelSortingTests.cs`, **31–33 (Faz 12.1)**
`Integration/Panel/PanelErrorLogTests.cs` + `Unit/Application/Observability/`,
**34–36 (Faz 12.2)** `Integration/Panel/PanelLoginAttemptTests.cs` +
`Unit/Application/Security/`, **37–39 (Faz 12.2b)**
`Integration/Panel/PanelPushCampaignTests.cs` + `Unit/Application/Notifications/`,
**40–42 (Faz 12.3)** `Integration/Panel/PanelPowerOutageNeighborhoodTests.cs` +
`Unit/Application/PowerOutages/`, **43–45 (Faz 12.4)**
`Integration/Panel/PanelEventDistrictTests.cs` + `Unit/Application/Events/`,
**46–48 (Faz 12.5)** `Integration/Panel/PanelTransportFieldModelTests.cs` +
`Unit/Application/Transport/`
içinde (panelin canlı denetiminde bulundular ve
gerçek Postgres isterler). Biri kırmızıya dönerse ya sözleşme
bilinçli değişmiştir (o zaman burayı ve mobil istemciyi aynı commit'te güncelle) ya da kazadır.

| # | Sözleşme | Bozulursa ne olur |
|---|---|---|
| 1 | `GET /v1/power-outages` **sayfalamıyor**, düz dizi döner | Mobil süren/planlı ayrımını **tam listeden** yapıyor; sayfalanırsa acil şeritte kesinti kaybolur, hata görünmez |
| 2 | Duyuru uçları bulunamayan kayıtta **200 + `success:false`** döner (diğer modüller 404) | `EnvelopeInterceptor` bunu normalleştiriyor; uç 404'e çevrilirse yorumlar ve kontrat yalan söyler |
| 3 | `GET /v1/ads/{id}` **her çağrıda** `view_count` artırır, **artıştan ÖNCEKİ** değeri döner | Semantik değişirse ekranda sayı bir fazla görünür; istemci bilerek +1 eklemiyor |
| 4 | Arama parametresi: taksi + ulaşım → **`searchTerm`**, diğerleri → **`search`** | Yanlış ad **sessizce yok sayılır** (400 gelmez); arama çalışmıyor gibi görünür |
| 5 | `places.amenities` DB'de `jsonb` ama DTO'da `string` → **JSON içeren metin** gelir | DTO nesneye çevrilirse mobil "olanaklar yok" göstermeye başlar. Anahtarda olmayan olanak "yok" değil, **"belirtilmemiş"** demektir |
| 6 | `dutyDate` / `eventDate` / `funeralDate` = **"TR günü, 00:00 UTC"**; saat ayrı alanda | İstemci saat dilimine çevirirse **gün bir geri kayar** (00:00–03:00 arası patlayan testler bundandı) |
| 7 | Ulaşım saatleri **tarihsiz duvar saati**, iki farklı biçimde: şehirlerarası `"07:00"`, şehir içi `"06:30:00"` | Tek biçim varsayan çözümleyici sessizce yanlış saat hesaplar |
| 8 | `UpdateMyAdCommand` görsel **sırasını/kapağını bilmiyor** (yalnız `newImageFileIds` + `removeImageIds`) | Mobil kapak değişiminde görselleri silip yeniden bağlıyor; uç sıra desteği kazanırsa o hile kaldırılmalı |
| 9 | Görsel URL'leri **göreli** (`/uploads/…`); origin'i istemci ekler | Sunucu mutlak URL dönerse istemci `http://…http://…` üretir |
| 10 | Her yanıt `{success, data, meta}` zarfında; `meta` **her zaman** `traceId` taşır | Hata ekranı traceId gösteriyor; boşalırsa destek "hangi istek?" diye soramaz |
| 11 | `complaints.type` sunucuda **doğrulanmıyor** (serbest metin) | Doğrulayıcı eklenirse eski sürüm istemciler 400 almaya başlar |
| 12 | Yollar **kebab-case** (`/v1/power-outages`); PascalCase 404 | Transformer kapatılırsa çok kelimeli tüm uçlar kırılır |
| 13 | İlan sayısal özellikleri **InvariantCulture**, binlik ayracı **yok** (`2020.5` ✓, `2020,5` ✗) | *(11.14'te düzeltildi — eskiden `2020,5` geçiyor ve `20205` okunuyordu)* |
| 14 | Kategori `select`/`multiSelect` değeri **seçenek metniyle** ve **harf duyarlı** doğrulanır | Seçenek metni panelden yeniden adlandırılırsa eski ilanlar güncellenemez hâle gelir |
| 15 | `AdCategory` filtresi **TAM EŞLEŞME** — kök kategori alt kategori ilanlarını getirmez | Mobil bu yüzden kategori şeridinde "içeri iniyor"; filtre hiyerarşik yapılırsa şerit tasarımı gereksizleşir |
| 16 | Push `data` sözlüğü **tam olarak** `notificationId` (her zaman) + `type` + `relatedId` + `relatedType` taşır ve hepsi **metin**tir (`SendPushNotificationsJob.BuildData`) | Anahtar adı değişirse deep-link **sessizce ölür**: bildirime dokunan kullanıcı hiçbir yere gitmez, hata da görmez |
| 17 | `GET /v1/notifications` `unreadCount`'u **sayfalı gövdenin İÇİNE** koyar (zarf `meta`'sı filtreyle sabitlendiği için) ve bu sayı **filtreden bağımsız** toplamdır | `unreadOnly=true` isteğinde de rozet doğru kalsın diye; `meta`'ya taşınırsa istemci sayacı kaybeder |
| 18 | `relatedType` değerleri mobilde **rota üretir** (`announcement`→`/duyurular/:id` …); tanınmayan tür ve GUID olmayan kimlik **gezinmeyi iptal eder** | Sunucu yeni bir `relatedType` üretmeye başlarsa mobil onu sessizce yok sayar → `app_notification.dart` eşlemesine eklenmeli |
| 19 | Panelde izin eylemi **aksiyon adından türetilir** (`PanelPermissionFilter.ActionFor`): `Approve/Reject/Verify/Ban/UpdateStatus…` → `approve`, `Delete…` → `delete`, `Create/Add…` → `create`, `Update/Edit…` → `update`, geri kalan GET → `read`, geri kalan POST → `update` | Aksiyon **yeniden adlandırılırsa** izin sessizce değişir. Örnek: `UpdateStatus` → `SetStatus` yapılsa şikayet sonuçlandırma `approve` yerine `update` iznine düşer ve düzenleme yetkisi olan moderatör moderasyon kararı verebilir hâle gelir |
| 20 | Panel menüsü (`PanelMenu.Items`), izin matrisi (`StaffAdminController.Modules`) ve `[PanelPermission("…")]` **aynı modül anahtarını** kullanır | Ayrışırlarsa yöneticinin matriste verdiği yetkinin panelde karşılığı olmaz (ya da tersi) ve sebep hiçbir yerde görünmez |
| 21 | Slug üretiminin tek sahibi `SlugHelper`; `DbSeeder.Slugify` ona delege eder | İkinci bir gerçekleme yazılırsa seed'lenen kayıtla panelden eklenen kayıt farklı slug alır. 10.9–11.15b arasında tam olarak bu oldu: `'İ'` (U+0130) `ToLowerInvariant()` ile küçülmediği için slug'a ham giriyordu ("İstasyon" ≠ "istasyon" → mükerrer mahalle) |
| 22 | Cache grup adları **yalnız `CacheGroups` sabitleri** olabilir; her cache'lenen grubun en az bir invalidate eden komutu vardır (**tek istisna `dashboard`** — bilinçli olarak 60 sn TTL'e dayanır) | Serbest metin grup adı invalidation'ı **sessizce** kapatır: panelde güncellenen veri mobilde 15 dakika eski kalır, ne log düşer ne istisna |
| 23 | Panelin **"aktif/yayında" sayaçları** public sorguların görünürlük tanımıyla **birebir aynı** olmak zorunda (`GetDashboardStatsQueryHandler` ↔ `GetAdsQueryHandler:32`, `GetAnnouncementsQuery:46`) | Ayrışırsa panel ile vatandaş **farklı gerçeklik görür** ve kimse hata almaz. 11.15c'de canlıda görüldü: panel "Aktif İlanlar 1" derken `GET /v1/ads` **0** döndürdü (süresi dolmuş ilan sayılıyordu) |
| 24 | **Bildirim, hedefi yayında olduğu sürece görünür.** `GetMyNotificationsQuery` "hedefi yaşayan" süzgeci uygular ve `unreadCount` **aynı** süzgeçten geçer; ayrıca `DeleteAnnouncementCommand` ilgili bildirimleri **fiziksel** siler | Süzgeç kalkarsa kullanıcı bildirimi görür, dokunur, `NOT_FOUND` sayfasına düşer (11.15c canlı kanıtı: silinen duyurunun 9 bildirimi ayakta kaldı). Sayaç süzgeçten ayrılırsa rozet "3 okunmamış" derken liste 1 satır gösterir |
| 25 | **İlan onayı, ilanı gerçekten görünür kılar**: `ApproveAdCommandHandler` süresi geçmiş (`ExpiresAt <= now`) ilana taze 30 günlük pencere verir | Kaldırılırsa panel "onaylandı" der, mobil hiçbir şey göstermez ve `ExpireAdsJob` bir saat içinde durumu sessizce geri alır. Koşul **duruma değil tarihe** bakar: onay kuyruğunda 30 günden fazla bekleyen `pending` ilan da aynı tuzağa düşüyordu |
| 26 | `QueryAdDto.Status` **yalnız panel/admin yolunda** okunur; public uç (`OnlyPublished=true`) onu yok sayar | `else if` `if`'e çevrilirse `GET /v1/ads?status=pending` onaylanmamış ilanları **iletişim telefonlarıyla** herkese açar (10.5'te bir kez yaşandı) |
| 27 | Panelin kesinti **süren/planlı/bitti** tanımı (`PowerOutagePhaseRules`) mobildeki `PowerOutage.isActive/isUpcoming/isPast` ile **birebir** aynı olmak zorunda: başlangıç anı **dâhil**, bitiş anı **hariç** | `GET /v1/power-outages` bilinçli olarak sayfalamaz ve tarih süzmez (madde 1); ayrım tümüyle istemcide. Tanımlar ayrışırsa yönetici "sürüyor" derken vatandaş "planlı" görür ve **kimse hata almaz** (madde 23'ün aynı sınıfı) |
| 28 | **Geri getirme, yayına alma DEĞİLDİR:** `RestoreRecordCommand` yalnız `deleted_at`'i temizler, `status`'e dokunmaz | Dokunsaydı çöp kutusu moderasyonun arka kapısı olurdu: reddedilmiş bir ilan silinip geri getirilerek yayına sokulabilirdi. Kapsam `TrashModules.Supported`'da **tek listede** — sorgu ve komut ayrı `switch` yazarsa "listede görünen ama geri getirilemeyen kayıt" doğar |
| 29 | Toplu işlem aksiyonları **`…Selected` ile biter** (`ApproveSelected`, `DeleteSelected`), `Bulk…` ile **başlamaz** | İzin eylemi aksiyon adının **önekinden** türetilir (madde 19). `BulkApprove` hiçbir moderasyon önekiyle eşleşmez, POST olduğu için sessizce **`update`**'e düşer → yalnız *düzenleme* yetkisi olan moderatör **toplu onay** yapabilir hâle gelir. Ayrıca toplu işlem her kayıt için modülün **tek-kayıt komutunu** çağırmalıdır: toplu SQL yazılırsa denetim izi, önbellek geçersizleştirmesi ve madde 25'in onay penceresi hiç çalışmaz — panel "42 ilan onaylandı" der, mobil hiçbirini göstermez |
| 30 | Her sıralama anahtarı **benzersiz bir ayraçla** (`ThenBy(Id)`) biter (`PanelSorts`) | Eşit değerli satırlarda Postgres sırayı garanti etmez: sayfalı listede **aynı kayıt iki sayfada birden görünür, bir başkası hiç görünmez** — hata vermeyen veri kaybı. ⚠️ "Bir ikincil anahtar koymak" yetmez, ayracın **benzersiz** olması gerekir (`title_asc`'in ikincili `CreatedAt`'ti ve başlığı+tarihi aynı iki kayıtta o da eşitti). Ayrıca **varsayılan anahtar** modülün eski sırasıyla birebir aynı kalmalıdır; değişirse mobil liste sessizce ters döner |
| 31 | **Hata kaydı yazımı isteği DÜŞÜREMEZ**: `IErrorLogSink.TryWrite` asla fırlatmaz, asla beklemez; yazıcının kendi hatası **asla** `error_logs`'a gitmez, yalnız `ILogger`'a düşer | `ExceptionMiddleware`'in `catch` bloğunda **senkron DB yazmak** en tehlikeli tasarım: veritabanı çöktüğünde hata yazma denemesi de patlar, istisna `catch`'in İÇİNDE doğar, yanıt zarfı hiç yazılmaz ve istemci **zarfsız ham 500** alır → madde 10 tam da her şeyin kötü gittiği anda kırılır. Yazıcı kendi hatasını tabloya yazarsa: DB hatası → kayıt denemesi → DB hatası… sonsuz döngü |
| 32 | **`Fingerprint` tekilleştirmesi zorunludur** ve `ErrorFingerprint.Normalize` GUID/sayı/tarihi maskeler; benzersiz indeks veritabanındadır | Normalize kalkarsa `"Ad {guid} bulunamadı"` her istekte ayrı parmak izi üretir → tekilleştirme **hiç** çalışmaz. Tekilleştirme kalkarsa tek bir 500 döngüsü tabloyu dakikada on binlerce satırla doldurur. İkisi de **hiçbir hata vermeden** olur; tek belirti tablonun sessizce şişmesi. Benzersiz indeks ayrıca Api/Web yarışını yakalar — olmasaydı iki süreç aynı yeni hatayı aynı anda görüp mükerrer satır üretirdi |
| 33 | Hata kaydının `Source`'u **sunucuda sabitlenir** (`POST /v1/client-errors` gövdesinde `source` alanı yoktur); `Path` **maskelenir** (`SensitiveDataMasker`) | İstemci `api` diyebilseydi kendi çökmesini sunucu hatası gibi gösterip "sunucumuzda kaç hata var?" sorusunun cevabını zehirlerdi. Maskeleme kalkarsa OTP akışındaki telefon numarası tabloya girer — kayıtlar panelde görülüyor, **CSV olarak dışa aktarılıyor** ve 90 gün saklanıyor |
| 34 | **Giriş denemesinde `Identifier` MASKELİDİR** (`LoginIdentifierMasker`) ve maskeleme **deterministiktir** — aynı telefon her zaman aynı değeri üretir | Ham saklansaydı bir güvenlik tablosu, kendisi bir sızıntı hedefine dönerdi: satırlar panelde görülüyor, **CSV'ye çıkıyor**, başarısız denemeler **180 gün** duruyor. Determinizm ayrı bir bağımlılık: hatalı OTP satırlarında `UserId` **bilerek boştur** (o dalda kullanıcı tablosuna dokunulmuyor) ve kullanıcı ekranındaki "son giriş denemeleri" kutusu onları **yalnız maskeli kimlikle** hesaba bağlar. Maskeleme rastgeleleşirse o satırlar hiçbir hesapla eşleşmez ve kutu sessizce **boş** görünür |
| 35 | **R1 eşiği `PanelLockoutPolicy.MaxFailedAttempts` ile aynı olmak zorundadır** (`SuspicionThresholds.AccountFailureThreshold`) | Ayrışırsa iki taraf farklı gerçeklik görür: eşik yüksekse hesap kilitlenir ama **uyarı hiç doğmaz** (kilit yüzünden eşiğe ulaşacak deneme zaten gelemez), düşükse kilitlenmeyen hesaplar için uyarı yağar. 11.18'in kilidi çalışmaya devam ettiği için **kimse fark etmez** — madde 23'ün aynı sınıfı |
| 36 | **Uyarı e-postası kısılır** (`security_alert:{hash}`, kural+alıcı başına saatte 1) ve `SecurityAlertJob` her koşuda **tek** e-posta üretir | Kısma kaldırılırsa bir kaba kuvvet saldırısı, yöneticinin posta kutusuna **kendi kendimize yaptığımız DoS**'a döner: uyarılar filtreye atılır ve **gerçek** uyarı da o filtreye düşer. Sistem çalışmaya devam eder, hiç hata vermez ve tamamen işe yaramaz hâle gelir. ⚠️ Redis erişilemezse **fail-open**'dır (gönderir) — güvenlik uyarısını sessizce yutmak fazladan e-postadan kötüdür; tavanı koşu başına tek e-posta zaten sağlar |
| 37 | **`Notification.FcmSent = true` TERMİNALDİR**: `SendPushNotificationsJob` o satırı bir daha almaz, mesaj bazlı hatalar (bad token vb.) kalıcı sayılır. Yeniden gönderim **yeni kampanya** açar; eski satırlara dokunulmaz (`CancelPushCampaignCommand` yalnız `FcmSent=false` satırları geri çekebilir) | Panele "yeniden gönder" butonu konursa **hiçbir şey yapmaz ve kimse hata almaz** — panelin en sinsi yalan biçimi: yönetici bastığını sanır, sayaç değişmez, log temizdir. Aynı sebeple iptal butonu iletilmiş mesajı geri almayı **teklif etmez**: geri alınamayacak bir şeyi teklif eden bir buton, işlevsiz butondan kötüdür |
| 38 | **Hedefleme mantığının tek sahibi `INotificationDispatcher`**: mahalle süzgeci + `NotificationPreferences` + gövde kırpması orada, duyuru üreticisi de panelin manuel gönderimi de **aynı** metottan geçer. Panelin "tahmini alıcı" önizlemesi de aynı sorguyu çağırır. ⚠️ `NeighborhoodIds` **`null` ≠ boş liste**: null "liste yok → herkes" (10.10'dan beri), boş liste "hiçbir mahalle seçilmemiş → kimse" | İkinci bir hedefleme gerçeklemesi yazılırsa duyuru ile manuel gönderim **aynı mahalleye farklı kişi kümesi** yollar ve iki taraf da hiç hata vermez (#23'ün aynı sınıfı). Önizleme ayrılırsa panel "342 kişiye gidecek" der, gönderim 280 satır yazar ve fark hiçbir yerde görünmez. null/boş ayrımı kalkarsa ya duyurular ölür (null'ı "kimse" saymak) ya da **bozuk bir JSON tüm şehre giden bildirime dönüşür** |
| 39 | **Kampanya sayaçları `SendPushNotificationsJob` tarafından ARTIMLI yazılır** (`sent`/`failed`/`invalidTokens`), sorgu anında `COUNT` ile hesaplanmaz; `CompletedAt`'in ölçütü "işlenen = alıcı" değil **"gönderilebilir bekleyen satır kalmadı"** | Sayaç yazımı atlanırsa pano sonsuza kadar "Kuyrukta" gösterir: bildirimler gider, `fcm_sent` dolar, hiçbir hata oluşmaz ve **yalnız pano yalan söyler** — üstelik artımlı olduğu için "bir kez daha say, düzelir" yolu yoktur. Tamamlanma ölçütü "işlenen = alıcı" yapılırsa **hiçbir kampanya tamamlanmaz**: job yalnız `FcmToken != null` satırları alır, token'ı olmayan alıcılar sonsuza kadar bekleyen görünür. ⚠️ Tamamlanma sorgusu **bu batch'in satırlarını dışlamalı** — henüz `SaveChanges` olmadığı için veritabanı onları hâlâ `fcm_sent = false` görür ve kampanya asla kapanmaz |
| 40 | **`power_outages.neighborhood` metni `NeighborhoodId` doluyken SÖZLÜKTEN TÜRETİLİR** (`PowerOutageNeighborhoodResolver`), elle yazılmaz; geri doldurma da adı kanonikleştirir | Kolon bilerek duruyor: `GET /v1/power-outages` onu düz metin veriyor ve **mağazadaki eski sürümler mahalle eşleşmesini ad üzerinden** yapıyor (`power_outage.dart → matchesNeighborhood`). Türetme kalkarsa panelde "Cengiz Topel Mah." yazan bir kayıt, kullanıcının profilindeki "Cengiz Topel" ile tutmaz ve **"sadece mahallem" süzgeci sessizce boş kalır** — 12.3 öncesinde tam olarak bu oluyordu. ⚠️ Serbest metni sözlüğe bağlayan normalleştirmenin tek sahibi `SlugHelper` (madde 21): ikinci bir gerçekleme yazılırsa `'İ'` yüzünden kesinti **yanlış mahalleye** bağlanır ve o kaydın bildirimi **başka mahallenin sakinlerine** gider |
| 41 | **Kesinti bildirimi ayrı bir tür değil, BİR DUYURUDUR** (`IPowerOutageAnnouncementWriter` — tek sahip): kesinti silinince duyurusu ve bildirimleri de silinir, **güncelleme ikinci duyuru üretmez** | Ayrı bir `relatedType` uydurulsaydı görünmez sözleşme #18 gereği eski sürümler bildirime dokunduğunda **sessizce hiçbir yere gitmezdi**. Silme temizliği #24'ün uzantısı: kalsalardı vatandaş bildirime dokunup **boş sayfaya** düşerdi (11.15c'de duyurularda birebir yaşandı, 9 ölü bildirim). Güncelleme yeniden üretseydi bir **yazım düzeltmesi** bile şehre ikinci bir push atardı. ⚠️ Yazıcı duyuruyu `Query(tracking: true)` ile almak zorunda — `Repository.Query()` varsayılan olarak **AsNoTracking**'tir ve `SoftRemove` bağlantısız nesneye yazınca duyuru "silinmiş görünür, `deleted_at` boş kalır", hiçbir hata oluşmaz (12.3'te canlı testte yakalandı) |
| 42 | **Bildirim yalnız `NeighborhoodId` DOLU kesintide gönderilebilir**; hedefsiz kayıtta komut `NotTargetable` döner ve panel bunu **söyler** | Serbest metinli kayıt hedeflenemez. Kapı kalkarsa dispatcher'a **boş mahalle listesi** gider; `NotificationDispatcher`'da boş liste "kimseye" demek (null "herkese"), yani en iyi hâlde bildirim sessizce buharlaşır — panel "gönderildi" der, kimse almaz. Panel bu yüzden hem butonu kapatır hem sebebini yazar: sessizce "gönderildi" demek bu fazın savaştığı hasar sınıfının ta kendisi |
| 43 | **`locationLabel` SUNUCUDA tek yerde üretilir** (`DistrictLabel.For`) ve liste ile detay **aynı** projeksiyondan (`EventProjection`) geçer. Panel de kendi biçimini yazmaz — `PanelDisplay.DistrictLabel()` aynı sınıfa delege eder | İstemcide üretilseydi panel "Osmaniye / Merkez", mobil "Merkez" yazardı ve **kimse hata almazdı** (madde 23'ün aynı sınıfı). Projeksiyon ayrışırsa daha sinsi: 12.4 öncesinde liste ve detay iki ayrı `Select` bloğuydu — yeni alanlar yalnız birine eklendiğinde **detay ekranı sessizce konumsuz kalır**, ne derleyici ne test yakalar. ⚠️ Ev ilçesi/ili karşılaştırması `SlugHelper`'dan geçer (madde 21): ham `ToLowerInvariant` ile Türkçe `İ` yüzünden **Kadirli etkinliği "çevre il" sayılırdı** |
| 44 | **`Event.IsLocal` TÜRETİLMİŞTİR**: yazma anında `DistrictId`'den hesaplanır (`EventDistrictResolver` — tek sahip, Create ve Update aynı metottan geçer); formdan gelen değere **güvenilmez**. Ev ilçesinin çıpası `DistrictDefaults.HomeSlug` sabitidir, veritabanındaki bir bayrak değil | Kolon 10.x'ten beri DTO'da ve mobil onu okuyor — silmek kırıcı olurdu (§5), türetmek additive. İki komutta ayrı yazılsaydı biri güncellenip diğeri unutulduğunda kayıt **ilçesi Kadirli ama `IsLocal=false`** hâline düşer ve mobilin "Kadirli" süzgeci onu **hiç göstermezdi**. Çıpa bir DB bayrağı olsaydı panelden yanlışlıkla başka ilçeye taşınabilir ve o an **bütün etkinlikler sessizce "yerel değil"** olurdu — bu yüzden ev ilçesi panelden **yeniden adlandırılamaz ve pasifleştirilemez** |
| 45 | **Etkinlikte ilçe ZORUNLUDUR** ve sözlükte **silme yoktur** (yalnız `IsActive`); `districts` FK'si `SetNull` | İkisi birlikte tek bir şeyi garanti eder: `district_id IS NULL` **yalnızca "12.4 öncesinden kalma"** demektir. `EventDistrictBackfill` her açılışta o satırları Kadirli'ye bağlıyor ve varsayımı ancak bu iki kapı ayakta tutuyor — biri kalkarsa yöneticinin **bilerek boş bıraktığı** bir kayıt bir sonraki açılışta sessizce "Kadirli" olur. ⚠️ Geri doldurma silinmişleri de tarar (`IgnoreQueryFilters`): çöp kutusundan geri gelen etkinlik ilçesiz olurdu. 🐛 **12.5 canlı denetimi:** "pasif ilçe seçilemez" kuralı kaydın *şu anki* ilçesini tanımıyordu → ilçesi sonradan pasifleştirilen etkinlik **hiç düzenlenemez** hâle geliyordu (bkz. madde 48) |
| 46 | **Sefer gün maskesinin tek sahibi `OperatingDays`** (Pazartesi=1 … Pazar=64); `0` **yasaktır** ve uç seferleri günlere göre **elemez**, yalnız `days`/`runsDaily` ile bildirir | Üç ayrı sessiz hasar tek maddede: (a) .NET `DayOfWeek` **Pazar=0**'dan başlar, maske Pazartesi'den — ikinci bir eşleme yazılırsa **"Salı seferi Pazartesi görünür"**, ne derleyici ne test yakalar; (b) `0` maskesi panelde *duran* ama mobilde *hiç görünmeyen* bir sefer üretir: yönetici saati girdiğini sanır, vatandaş asla göremez; (c) sunucu günlere göre elerse `days`'i tanımayan **mağazadaki eski sürümler için liste sebepsiz boşalır** — bugünkü davranış "her sefer her gün" olduğu için elememek regresyon değil, **uyumluluğun kendisidir**. ⚠️ Kodlar (`"mon"`…`"sun"`) DTO'ya çıkıyor, yani kontrat: yeniden adlandırılırsa eski sürümler günü tanımaz |
| 47 | **`intercity_routes.vehicle_type` METİNDİR** (`bus`/`minibus`), enum sırası değil; kayıt yolunda `TransportVehicleTypes.Normalize`'dan geçer, **süzgeç yolunda** ise tanınmayan değer **süzmez** (`NormalizeFilter` → `null`) | Sayı saklansaydı araya üçüncü bir tip girdiğinde **bütün kayıtlar sessizce kayar** ve eski sürümler yanlış tip gösterir. İki dönüşümün ayrı olması da bilinçli: tek metot olsaydı `?vehicleType=otobus` yazan istemci, tüm listeyi görmesi gerekirken **yalnız otobüsleri** görürdü — hata vermeyen yanlış liste (`ARCHITECTURE.md` §5: bilinmeyen değer 400 değil **varsayılan**). 12.5 öncesi satırlar migration'da `bus` ile göç etti; varsayılan değişirse o satırların anlamı geriye dönük değişir |
| 48 | **Hattın kalkış noktası SÖZLÜKTEN gelir** (`TransportDeparturePoint`); pasif nokta **yeni olarak seçilemez** ama var olan bağ korunur, o kayıtta **seçili kalır** ve **kaydın düzenlenmesini engellemez**; liste ile detay **aynı projeksiyondan** geçer (`IntercityRouteProjection`) | Serbest metin olsaydı "Kadirli Otogarı" on hatta ayrı yazılır ve **koordinatı düzeltmek on kaydı düzeltmek** olurdu — oysa koordinat bu tablonun varlık sebebi (12.6'nın "Yol tarifi" butonu). Pasif nokta seçili kayıttan düşseydi form kaydedildiğinde hattın kalkış noktası **sessizce boşalırdı** (12.4'te ilçe seçiminde birebir aynı karar). Projeksiyon ayrışırsa madde 43'ün aynısı: yeni bir alan yalnız listeye eklendiğinde **panelin düzenleme ekranı sessizce eksik** kalır ve ne derleyici ne test yakalar. 🐛 **"Yeni olarak" kaydı 12.5 canlı denetiminde eklendi:** kapı kaydın *şu anki* değerini tanımazsa, form pasif değeri seçili tuttuğu için (bu doğru bir karar) ikisi birlikte **düzenlenemeyen bir kayıt** üretir — yönetici yalnız fiyatı düzeltmek istese bile **hiç dokunmadığı bir alan** yüzünden reddedilir. Aynı hata 12.4'te `EventDistrictResolver`'da canlıda görüldü ve iki resolver aynı anda düzeltildi |

### Kod dışı görünmez sözleşmeler (testle kilitlenemeyenler)

- 📌 **Yapılandırma bayrağıyla kapatılmış kod yolu = hiç test edilmemiş kod yolu.**
  `FcmPushService` 10.11'de yazıldı, `Fcm:Provider="None"` olduğu için **hiç çalışmadı**;
  gerçek anahtar bağlanır bağlanmaz ilk çalıştırmada patladı (FirebaseAdmin .NET'te
  `GetInstance` null döner, Java'daki gibi fırlatmaz). **Bayrakla kapalı her yola en az bir
  birim testi yaz.**
- 📌 **`go_router` iç içe rotada ÜST ekranı da kurar.** Bir detay/form rotasını başka bir
  ekranın alt rotası yaparsan üstteki ekran arka planda kurulur, istek atar, diyalog açar.
  Form ve detay rotaları **kardeş** olmalı (ulaşım modülünde detay rotası hiç yok — id ucu
  olmadığı için).
- 📌 **`context.push` ile açılan ekran router redirect'inin ÜSTÜNDE kalır.** Durum
  değiştikten sonra ekranı `addPostFrameCallback` içinde `pop()`/`go()` ile kapat.
- 🐛 **KABUK (sekme) ROTASI, kabuk en üstte değilken `push` EDİLMEZ — `go` edilir.**
  Tek sahibi `lib/core/router/app_nav.dart` (`AppNav.push` / `AppNav.of`).
  **12.2'den devralınan ve iki oturum boyunca açık kalan çökmenin kanıtlanmış kök nedeni
  budur** (12.3'te bulundu, `test/core/navigation/shell_page_key_test.dart` ile kilitlendi):
  `go_router` imperative sayfalara **rastgele**, `StatefulShellRoute` sayfalarına ise
  **`route.hashCode`** anahtarı verir. Kabuk anahtarı deterministik olduğu için, araya kabuk
  dışı bir sayfa girmişken bir kabuk rotası `push` edilirse
  `RouteMatchList._createNewMatchUntilIncompatible` birleştirme yapamaz ve listeye **aynı
  anahtarla ikinci bir `ShellRouteMatch`** ekler → `Navigator._debugCheckDuplicatedPageKeys`.
  ⚠️ Karar elde tutulan bir rota listesinden değil **router'ın kendisinden** okunur; elle
  liste tutulsaydı yeni bir sekme eklendiğinde çürür ve çökme sessizce geri gelirdi
  (`module_grid`'in `AppRoutes.tabs` kontrolü doğru sezgiye sahipti ama yalnız sekme
  **köklerini** tanıyordu — `/ilanlar/:id` gibi **alt** rotalar, yani push bildiriminin
  deep-link hedefleri, kapsam dışındaydı).
- 📌 **Riverpod 3 hatalı provider'ları sınırsız yeniden dener** → her uç provider'ına
  `retry: apiRetry`.
- 📌 **Mobil tarihte sabit UTC+3** (`timezone` paketi yok). Türkiye 2016'dan beri kalıcı
  +03; yaz saati gelirse `AppDate`'teki tek sabit güncellenir.

---

## 8. Test haritası

### Ne nerede test edilir

| Katman | Nerede | Ne test edilir |
|---|---|---|
| Saf iş kuralı (C#) | `KadirliApp.Tests/Unit/Application/` | Kuralın kendisi; container yok, milisaniyeler |
| Altyapı (C#) | `KadirliApp.Tests/Unit/Infrastructure/` | FCM kurulumu, Hangfire dashboard yetkisi |
| Uç davranışı (C#) | `KadirliApp.Tests/Integration/<Konu>/` | Gerçek HTTP + gerçek Postgres/Redis (Testcontainers) |
| Görünmez sözleşmeler | `Integration/Contracts/InvisibleContractsTests.cs` | §7 tablosunun her satırı |
| Yetki (yapısal) | `Integration/Security/EndpointAuthorizationSweepTests.cs` | `EndpointDataSource`'tan **tüm** uçlar — yeni uç kendiliğinden kapsanır |
| Görünürlük | `Integration/Security/ModuleVisibilitySweepTests.cs` | Liste seviyesinde "gizli kayıt sızmıyor" |
| Doküman tutarlılığı | `Integration/Architecture/ArchitectureDocTests.cs` | **Bu dosya** ↔ gerçek klasörler/modüller |
| Checklist tutarlılığı | `Integration/Architecture/CodeReviewChecklistDocTests.cs` | `CODE_REVIEW_CHECKLIST.md`'nin **atıfları** ↔ gerçek test sınıfları/yardımcılar (maddelerin *doğruluğu* değil, işaret ettikleri yerlerin *varlığı*) |
| **Panel (Razor/MVC)** | `Integration/Panel/` | Gerçek panel + Postgres + Redis: oturum/yetki, her sayfanın render'ı, form yazımı + audit izi, moderatör izin matrisi |
| **Panel görsel dili** | `Integration/Panel/PanelDisplayTests.cs` | Kodun ürettiği **her** durum/rolün Türkçe karşılığı var mı, para `¤` basıyor mu, izin matrisi ↔ menü ayrışması (container gerektirmez) |
| **Panel kullanılabilirliği** | `Integration/Panel/PanelUsabilityTests.cs` | Dar ekranda menü açılıyor mu, listede ham İngilizce/`¤` sızıyor mu, 404 gövdeli mi, onay kuyruğu bağlantısı çalışıyor mu |
| **Panel ↔ vatandaş paritesi** | `Integration/Panel/PanelBusinessRuleTests.cs` | §7 madde **23–26**: sayaçlar public görünürlük tanımıyla aynı mı, onay ilanı gerçekten görünür kılıyor mu, ölü bildirim (iki katman + `unreadCount` tutarlılığı) |
| **Ulaşım paneli** | `Integration/Panel/PanelTransportTests.cs` | Şehirlerarası hat + kalkış saati + durak yazımı; iddia "kayıt oluştu" değil **"mobilin gördüğü sorguya düştü"** |
| **Denetim izi** | `Integration/Panel/PanelAuditLogTests.cs` | Gerçek bir silmenin ize düşmesi; eylem sözlüğü **kaynak taranarak** kilitli (`AuditAction => "…"`), menü satırı matris dışında mı |
| **Çöp kutusu** | `Integration/Panel/PanelTrashTests.cs` | §7 madde **28**: geri getirme `status`'e dokunmuyor, `IgnoreQueryFilters` unutulmamış, ikinci geri getirme iz bırakmıyor |
| **Kesinti süzgeci** | `Integration/Panel/PanelPowerOutageFilterTests.cs` | §7 madde **27**: süren/planlı/bitti sınır anları mobil tanımıyla birebir; tarih aralığı **kesişim** üzerinden |
| **Hata günlüğü** | `Integration/Panel/PanelErrorLogTests.cs` | §7 madde **31–33**: tekilleştirme gerçekten tek satır üretiyor mu, çözülmüş hata tekrar edince kendiliğinden açılıyor mu, istemciden gelen metin panelde **kaçırılıyor** mu (depolanmış XSS), ekran matris dışında mı |
| **Hata parmak izi / maskeleme** | `Unit/Application/Observability/` | Saf mantık: GUID/sayı/tarih normalize ediliyor mu (yoksa tekilleştirme hiç çalışmaz), yığın satır numarası atılıyor mu, hassas sorgu parametreleri maskeleniyor mu |
| **Giriş denemeleri** | `Integration/Panel/PanelLoginAttemptTests.cs` | §7 madde **34**: kimlik maskeli mi, ham kullanıcı adı CSV'ye sızıyor mu; 5 hatalı giriş → **5 kayıt + kilit + şüpheli işareti** (madde 35'in uçtan uca kanıtı); `unknown_user` ile `bad_password` ayrılıyor mu; ekran matris dışında mı; geçersiz IP süzgeci **sessizce yok sayılmıyor** mu |
| **Şüphe kuralları / kimlik maskeleme** | `Unit/Application/Security/` | Saf mantık, container'sız: R1–R4'ün sınırları, kural **önceliği** (R2 > R1, R4 > R3), R1 eşiğinin `PanelLockoutPolicy` ile eşitliği, maskelemenin **determinizmi** ve "sıradan giriş asla şüpheli değildir" |
| **Bildirim gönderimleri** | `Integration/Panel/PanelPushCampaignTests.cs` | §7 madde **37–39**: hedeflemenin tek sahibi mi (önizleme ↔ gönderim aynı sayı), bildirim tercihi manuel gönderimde de uygulanıyor mu, sayaçlar artımlı mı ve ikinci koşuda **artmıyor** mu, kampanya gerçekten **tamamlanıyor** mu, iptal yalnız gönderilmemişe dokunuyor mu, ekran matris dışında mı, istemciden gelmeyen ama panelde basılan metin **kaçırılıyor** mu |
| **Kampanya durumu** | `Unit/Application/Notifications/PushCampaignStatusTests.cs` | Saf mantık, container'sız: durum önceliği (iptal > boş > tamamlandı > gönderiliyor > kuyrukta), bekleyen sayısının negatife düşmemesi |
| **Kesinti mahalle referansı** | `Integration/Panel/PanelPowerOutageNeighborhoodTests.cs` | §7 madde **40–42**: mahalle adı formdan değil **sözlükten** yazılıyor mu, kesinti bildirimi duyuru üretip **yalnız o mahalleye** yazıyor mu, güncelleme **ikinci bildirim üretmiyor** mu, silme duyuru+bildirimleri götürüyor mu, FK'sız kayıt bildirim gönderemiyor mu, önizleme ↔ gerçek alıcı sayısı **aynı** mı, geri doldurma idempotent ve var olan bağı **ezmiyor** mu, uç hâlâ **düz dizi** mi (#1) |
| **Etkinlik konumu (il/ilçe)** | `Integration/Panel/PanelEventDistrictTests.cs` | §7 madde **43–45**: `IsLocal` formdan değil **ilçeden** türetiliyor mu (güncellemede de), ilçesiz kayıt **reddediliyor** mu, `locationLabel` her kapsamda doğru mu, **liste ile detay aynı** konum alanlarını mı döndürüyor, kapsam süzgeçleri (`local`/`province`/`nearby`) doğru mu, **bilinmeyen kapsam listeyi boşaltmıyor** mu, panel/CSV aynı etiketi mi yazıyor, form ilçeleri `<optgroup>` ile mi grupluyor, **ev ilçesi yeniden adlandırılamıyor** mu |
| **Ulaşım alan modeli** | `Integration/Panel/PanelTransportFieldModelTests.cs` | §7 madde **46–48**: gün seçilmeden eklenen sefer **her gün** mü (göç eden satırların davranışı korunuyor mu), seçilen günler mobile **kod olarak** mı düşüyor, uç seferleri güne göre **elemiyor** mu, gün seçilmeyen düzenleme **reddedilip sebebini söylüyor** mu ve kaydı **ezmiyor** mu, sefer **satır kimliği korunarak** düzenlenebiliyor mu, araç tipi kanonik mi ve bilinmeyen süzgeç değeri **listeyi boşaltmıyor** mu, ham `bus`/`minibus` panelde görünüyor mu, pasif kalkış noktası **reddedilip** var olan kayıtta **seçili kalıyor** mu, **liste ile detay aynı alanları** mı döndürüyor, araç şeridi **mevcut süzgeci koruyor** mu |
| **Sefer günleri / araç tipi** | `Unit/Application/Transport/` | Saf mantık, container'sız: **Pazar=0 kayması** (`DayOfWeek.Sunday` biti Pazartesi'ye çakışmıyor mu), `0` maskesinin geçersizliği, gün kodlarının gidiş-dönüşü ve **kontrat sırası** (Pazartesi'den), bilinmeyen kod/bitin yok sayılması, "sıradaki sefer"in hafta sonunu **doğru sarması**; araç tipinde kayıt yolu (varsayılana düşer) ile süzgeç yolunun (**süzmez**) ayrı olması |
| **Konum etiketi / kapsam** | `Unit/Application/Events/` | Saf mantık, container'sız: etiket kuralının üç dalı ("Kadirli" · "Osmaniye / Merkez" · "Adana"), Türkçe `İ`'ye rağmen ev ilçesinin tanınması (madde 21), il+ilçe slug'ının çakışmaması (her ilin bir "Merkez"i var), kapsam değerlerinin gidiş-dönüşü ve **bilinmeyen değerin varsayılana düşmesi** |
| **Mahalle eşleştirme / bildirim metni** | `Unit/Application/PowerOutages/` | Saf mantık, container'sız: "X Mahallesi" → sözlükteki "X" (ek kırpma), Türkçe `İ` (madde 21), eşleşmenin **tam** olması (yanlış mahalleye bağlamak hiç bağlamamaktan kötü); duyuru gövdesinde saatin **TR yerel** yazılması ve gün aşan kesintide bitiş **tarihinin** de yazılması |
| **Önbellek sözleşmesi** | `Unit/Application/Caching/CacheContractTests.cs` | Grup adları sabit mi, her grubun invalidator'ı var mı, anahtar filtreyle değişiyor mu |
| **Önbellek davranışı** | `Integration/Panel/CacheInvalidationTests.cs` | Gerçek Redis: önce **bayat veri döndüğü** gösterilir, sonra mutasyonun temizlediği |
| **Moderasyon** | `Integration/Panel/ModerationStateMachineTests.cs` | Vefat/etkinlik/kampanya/işletme onay-red geçişleri, soft-delete etkileşimi |
| **Arka plan işleri** | `Integration/Panel/BackgroundJobTests.cs` | Sınır tarihleri + "iki kez koşarsa mükerrer üretmez" |
| Mobil saf mantık | `mobile/test/core/**`, `features/*/…_test.dart` | `departure_times`, `paged_feed`, `AppDate`, `AppMoney`… |
| Mobil ekran | `mobile/test/features/*/…_screen_test.dart` | Boş/yükleniyor/hata durumları, filtre, taşma |
| Mobil **görsel regresyon** | `mobile/test/golden/` | Ortak bileşenler + liste kartları; 360 dp × (1.0 ve 1.4 ölçek) × açık/koyu |
| Mobil **erişilebilirlik** | `mobile/test/core/accessibility_test.dart` | WCAG AA kontrast, 48 dp dokunma hedefi, ekran okuyucu etiketi, 1.4 ölçekte taşma yok |
| Mobil **hareket** | `mobile/test/core/reduced_motion_test.dart` | "Hareketi azalt" ayarına saygı |
| Mobil **Türkçe sözleşmesi** | `mobile/test/core/turkish_ui_test.dart` | Her hata kodunun Türkçe karşılığı var, teknik/İngilizce mesaj sızmıyor |
| **Yayın yapılandırması** (Faz 11.16) | `mobile/test/release/release_config_test.dart` | Platform dosyaları (`AndroidManifest.xml`, `Info.plist`) — hataları `flutter run` ile **görünmeyen**, ilk kez mağazadan inen uygulamada çıkan sınıf. İzin gerektiren kullanım `lib/` **taranarak** bulunur (elle liste yok); dev rotalarının yayına sızmadığı da burada kilitli |
| **Production ayar kapısı** (Faz 11.16) | `Unit/Api/ProductionReadinessGuardTests.cs` | `ProductionReadinessGuard` — Production'da güvensiz ayar varsa uygulama açılmaz. Kapının "yanmış JWT sırrı" listesi `appsettings.json` ile **eşitlenmiş** durumda (dosya değişip liste değişmezse koruma sessizce kaybolurdu) |

### Nasıl koşulur

```bash
# Backend (Docker gerekli — Testcontainers Postgres+Redis ayağa kaldırır)
dotnet test KadirliApp.Tests

# Yalnız hızlı birim testleri (container yok)
dotnet test KadirliApp.Tests --filter "FullyQualifiedName~Unit"

# Mobil
cd mobile && flutter analyze && flutter test

# Yalnız görsel regresyon / golden'ları yeniden üret
cd mobile && flutter test --tags golden
cd mobile && flutter test --update-goldens test/golden   # ⚠️ CI ASLA üretmez
```

### Yeni kod için hangi test yazılır

- **Yeni iş kuralı** → `Unit/Application/` altında birim testi. Kuralı geçici olarak
  bozup testin **gerçekten kırmızı olduğunu** gör; olmuyorsa test kuralı kilitlemiyor.
- **Yeni uç** → yetki testi kendiliğinden kapsar; ayrıca görünürlük ve "mutlu yol" testi.
- **Yeni görünmez bağımlılık** → §7 tablosuna satır + `InvisibleContractsTests`'e test.
- **Mobil yeni ekran** → en az: boş durum, hata durumu, ana etkileşim.
- **Mobil yeni ortak bileşen / liste kartı** → `mobile/test/golden/` altına bir golden
  senaryosu (**uzun Türkçe metinle**; kısa örnek hiçbir düzen hatası göstermez) ve
  gerekiyorsa `accessibility_test.dart`'a dokunma hedefi/etiket iddiası.

### Bilinen test tuzakları

**Backend**
- Testcontainers → **Docker açık olmalı**, ilk koşu imaj indirir.
- **Panel testleri** (`Integration/Panel/`): `extern alias WebPanel` **şart** — Api ve Web'in
  ikisi de global namespace'te `Program` üretir. Tüm panel sınıfları `[Collection("panel")]`
  ile **tek** container çiftini paylaşır; kendi `IClassFixture`'ını açan sınıf süiti dakikalarca
  uzatır.
- `WebApplicationFactory.CreateClient()` bir **örnek metottur** — aynı adla uzantı metodu
  yazarsanız hiç çağrılmaz (yönlendirmeler sessizce izlenir). Panelde `CreatePanelClient()`.
- `ConfigureAppConfiguration` ile verilen ayar, `Program.cs`'te **erken** (`builder.Build()`
  öncesi) okunan değerlere **yetişmez** — hız sınırı gibi. Onlar ortam değişkeniyle verilir.
  (Bağlantı dizeleri yetişiyor çünkü DbContext çözülürken, yani Build sonrası okunuyor.)
- Panelde model/ViewBag'den gelen Türkçe metin **HTML varlığına çevrilir**
  (`hatalı` → `hatal&#x131;`); gövdeyi `ReadDecodedBodyAsync()` ile okuyun.
- `audit_logs.details` **jsonb** — LINQ'te `.Contains()` yazmak `like_escape(jsonb, unknown)`
  hatası verir; süzmeyi belleğe alın.
- Hangfire işleri yalnız `KadirliApp.Api`'de kayıtlı → panel scope'unda
  `ActivatorUtilities.CreateInstance` ile kurulur.
- Test veritabanı yalnız `DbSeeder`'ın **lookup** verisiyle gelir (kategori, mahalle,
  mezarlık, admin). Modül kayıtları **yok** → testin kendi verisini kurması ve
  temizlemesi gerekir (`IAsyncLifetime` + benzersiz marker deseni).
- `IgnoreQueryFilters()` yalnız gerçek EF sağlayıcısında çalışır; mock'lanmış
  `IQueryable`'da **no-op**'tur.

**Mobil**
- Varsayılan widget-test yüzeyi **800×600** → uzun ekranlarda `tap` "off-screen" diye
  reddedilir. `tester.view.physicalSize` ile gerçek telefon yüzeyi ver.
- `pumpAndSettle` **`Timer`'ı ilerletmez** (debounce testinde süreyi elle pump et) ve
  **sonsuz animasyonda kilitlenir** (shimmer, splash spinner → `settleApp()` yardımcısı).
- `apiRetry` 5xx'i **geçici** sayar → "liste gelmezse" testlerinde **kalıcı** hata (404)
  kullan, yoksa "pending timer" hatası alırsın.
- Yatay `ListView` **tembeldir** — ekran dışı chip hiç kurulmaz; şeritlerde
  `SingleChildScrollView + Row` kullan.
- Provider testlerinde sabit `Future.delayed` **flaky**; `waitUntil(condition)` kullan.
- Tarih fixture'larında `DateTime.now().toUtc()` **yalnız geceleri** patlar →
  `AppDate.nowInTurkey`.
- **Golden'da göreli tarih**: "3 saat önce" yazan bir kart gerçek saate bakıyorsa referans
  görüntü **her gün** kırılır ve insan `--update-goldens`'ı refleks hâline getirir — testin
  değeri sıfırlanır. Tarih gösteren her karta `now` enjekte edilebilmeli
  (`EventCard`/`AnnouncementTile`/`ComplaintCard` deseni) ve golden senaryosunda sabitlenmeli.

---

## 9. Çalıştırma ve ortam

```bash
# 1) Dış servisler
docker compose up -d              # Postgres 5432 · Redis 6379 · Seq 8081

# 2) API
dotnet run --project KadirliApp.Api          # http://localhost:5005 (Swagger: /swagger)

# 3) Panel
dotnet run --project KadirliApp.Web

# 4) Mobil
cd mobile && flutter pub get && flutter run
```

**Base URL kuralı (mobil):** Android emülatörü `10.0.2.2:5005`, iOS simülatörü
`localhost:5005`, gerçek cihaz `--dart-define=API_BASE_URL=http://<LAN-IP>:5005`.

**Yapılandırma anahtarları (yayın öncesi kontrol listesi):**

| Anahtar | Geliştirme | Yayın |
|---|---|---|
| `Otp:DevMode` | `true` (kod yanıtta döner) | **`false`** + gerçek SMS sağlayıcısı |
| `Fcm:Provider` | `Firebase` (service-account bağlı) | `Firebase` + iOS için APNs `.p8` |
| `FileStorage:BaseUrl` | boş (göreli URL) | prod domain |
| Hangfire dashboard | loopback serbest | temel kimlik doğrulama |
| `uploads/` | yerel klasör | kalıcı Docker volume |
| `Cors:Origins` | serbest | yalnız gerçek origin'ler |

**Sırlar:** `secrets/` klasörü `.gitignore`'da; `secrets/README.md` (commit edilir) neyin
nasıl edinileceğini ve sızarsa ne yapılacağını anlatır. Mobil tarafta
`mobile/android/app/google-services.json` ve `mobile/ios/Runner/GoogleService-Info.plist`
de commit edilmez.

---

## 10. Değişmez kurallar

1. **Katman yönü** — `Domain ← Application ← Infrastructure ← Api/Web`. İhlal derlenmez.
2. **Kontrat additive** — DTO'ya alan eklemek serbest, silmek/yeniden adlandırmak sürüm
   planı gerektirir.
3. **Public uç yalnız yayınlanmış içerik döndürür** — onaylı + aktif + silinmemiş + süresi
   geçmemiş. Filtreyi controller'da zorla.
4. **Panel uçları `AdminApiControllerBase`'den türer** ve `[RequirePermission]` taşır.
5. **"İşlevsiz buton yok"** — mobilde her buton bir uca ya da bir ekrana gider;
   `app_modules_test.dart` bunu denetler.
6. **Arayüz Türkçe**, kod ve kimlikler İngilizce; kullanıcıya teknik/İngilizce mesaj
   gösterilmez.
7. **Her alt-faz sonunda** `dotnet test` + `flutter analyze` + `flutter test` yeşil,
   Memory Bank güncel, commit atılmış.
8. **Bu dosya modül tablosunu gerçekle uyumlu tutar** — uyumsuzluk `dotnet test`'i kırar.
