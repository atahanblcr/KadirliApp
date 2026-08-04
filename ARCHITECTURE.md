# KadirliApp — Mimari Haritası

> **Bu doküman ne işe yarar?** *"Yarın bir modül eklemek, değiştirmek ya da kaldırmak
> istersek yapının bozulmayacağını nereden bileceğiz? Neyin nerede ve ne amaçla olduğunu
> nasıl bulacağız?"* — Bu dosya o sorunun cevabıdır.
>
> **Bu doküman ne DEĞİLDİR?** Kronolojik bir günlük değil (o `Memory_Bank/Progress.md`),
> öğretici bir rehber değil (o `DOTNET_MASTERCLASS.md`), istemci kontratı değil
> (o `Memory_Bank/API_CONTRACT.md`). Burası **harita**: bugün neyin nerede olduğu.
>
> Son güncelleme: 3 Ağustos 2026 (Faz 11.15b — panel emniyet ağı + izin matrisi).

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
| `KadirliApp.Domain/Enums/` | `AdStatus`, `UserRole`, `PropertyType`… |
| `KadirliApp.Domain/Common/` | `BaseEntity` (Id/CreatedAt/UpdatedAt), `ISoftDeletable` |
| `KadirliApp.Application/Features/<Modül>/` | **20 modül**, her biri `Commands/`, `Queries/`, `Dtos/` |
| `KadirliApp.Application/Common/` | `IUnitOfWork`, `IRepository<T>`, istisnalar, davranışlar |
| `KadirliApp.Infrastructure/Persistence/` | `AppDbContext`, `Configurations/`, `DbSeeder`, `MockDataSeeder` |
| `KadirliApp.Infrastructure/Migrations/` | EF migration'ları |
| `KadirliApp.Infrastructure/Jobs/` | Hangfire işleri (aşağıda) |
| `KadirliApp.Infrastructure/{Caching,Files,Identity,Notifications,Health}/` | Redis, dosya depolama, JWT, FCM, health-check |
| `KadirliApp.Api/Controllers/` | **18 public controller** (`/v1/*`) |
| `KadirliApp.Api/Controllers/Admin/` | **18 admin controller** (`/v1/admin/*`) + ortak taban |
| `KadirliApp.Api/Authorization/` | `RequirePermissionAttribute` + policy sağlayıcı |
| `KadirliApp.Web/Controllers/` + `Views/` | **20 panel controller** + Razor görünümleri |
| `KadirliApp.Tests/` | `Unit/` + `Integration/` (aşağıda test haritası) |
| `secrets/` | **git'e girmez**; `secrets/README.md` neyin nasıl edinileceğini anlatır |

### Mobil (`mobile/`)

| Yol | İçerik |
|---|---|
| `lib/core/config/` | `Env` (flavor, base URL, `--dart-define` override'ları) |
| `lib/core/network/` | İki Dio istemcisi, `EnvelopeInterceptor`, `AuthInterceptor`, `ApiClient`, hata sözlüğü, `retry_policy` |
| `lib/core/router/` | `app_routes.dart`, `app_shell.dart` (4 sekmeli `StatefulShellRoute`), tek redirect noktası |
| `lib/core/navigation/app_modules.dart` | 🔑 **Modül kaydı** — ızgara + rota + uç listesi tek yerde |
| `lib/core/theme/` | Renk token'ları, `AppPalette` ThemeExtension, açık/koyu tema |
| `lib/core/paging/paged_feed.dart` | 🔑 Ortak sayfalama çekirdeği (yarış, mükerrer eleme, filtre) |
| `lib/core/push/` | 🔑 Push soyutlaması: `PushMessaging` arayüzü + `NoopPushMessaging` + Firebase gerçeklemesi. **Yapılandırma yoksa uygulama push'suz açılır, çökmez** |
| `lib/core/widgets/` | `AppScaffold`, `AppButton`, `AppCard`, `ContactActions`, `LookupDropdown`, `MonthCalendar`… |
| `lib/core/utils/` | `AppDate` (sabit UTC+3), `AppMoney`, `AppLinks`, `AppImage`, `AppShare`, `Debouncer` |
| `lib/features/<modül>/data/` | Model + repository (yalnız burası Dio görür) |
| `lib/features/<modül>/application/` | Provider'lar, denetleyiciler, saf mantık |
| `lib/features/<modül>/presentation/` | Ekranlar + `widgets/` |
| `test/` | 613 test; klasör yapısı `lib/`'i aynalar |

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
| 11 | **Ulaşım** | `Transport/` | `transport/intercity-routes`, `transport/intracity-routes` | `TransportAdmin` | `transport` | `transport/` | `/ulasim` *(detay rotası YOK — §6)* |
| 12 | **Kesintiler** | `PowerOutages/` | `power-outages`, `power-outages/{id}` | `PowerOutagesAdmin` | `power-outages` | `power_outages/` | `/kesintiler`, `/kesintiler/:id` |
| 13 | **Şikayet/İstek** | `Complaints/` | `POST complaints`, `complaints/my` | `ComplaintsAdmin` | `complaints` | `complaints/` | `/sikayet`, `/sikayet-bildir` |
| 14 | **Bildirimler** | `Notifications/` | `notifications`, `…/{id}/read`, `notifications/read-all`, `notifications/fcm-token` | *(yok)* | — | `notifications/` (+ `core/push/`) | Bildirim sekmesi |
| 15 | **Kimlik** | `Auth/` | `auth/login`, `auth/verify-otp`, `auth/register`, `auth/refresh`, `auth/logout` | `Account` | — | `auth/` | `/giris`, `/kayit` |
| 16 | **Kullanıcı** | `Users/` | `users/me`, `users/me/notifications`, `users/me/ads`, `users/me/favorites`, `DELETE users/me` | `UsersAdmin` | `users` | `profile/`, `settings/` | Profil sekmesi, `/ayarlar` |
| 17 | **Dosyalar** | `Files/` | `files/upload`, `DELETE files/{id}` | *(yok)* | — | `files/` | *(ekran yok — ortak repo)* |
| 18 | **Sözlükler** | `Lookups/` | `neighborhoods` (+ modül içi `cemeteries`/`mosques`/`categories`) | `LookupsAdmin` | `lookups` | `lookups/` | *(ekran yok)* |
| 19 | **Personel** | `Staff/` | *(public uç yok)* | `StaffAdmin` | `staff` | *(yok)* | — |
| 20 | **Panel istatistik** | `Dashboard/` | *(public uç yok)* | `Dashboard` | `dashboard` | *(yok)* | — |
| 21 | **Denetim izi** | `Audit/` | *(public uç yok)* | `AuditLogsAdmin` | *(matris dışı — yalnız admin)* | *(yok)* | — |
| 22 | **Çöp kutusu** | `Trash/` | *(public uç yok)* | `TrashAdmin` | *(matris dışı — yalnız admin)* | *(yok)* | — |

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

⚠️ **Yalnız admin'e açık bir ekran** ekliyorsanız (Faz 11.17: `AuditLogsAdmin`, `TrashAdmin`)
desen farklıdır: `[Authorize(Roles = "admin,super_admin")]` + `[PanelPermission]` **yok** +
`PanelMenu.Items` satırının `Module`'ü **`null`** + `AdminOnlyControllers`'a controller adı.
Modül anahtarı verirseniz izin matrisinde moderatöre dağıtılabilen ama rol kapısı yüzünden
asla çalışmayacak bir yetki belirir — 11.15b'nin en büyük bulgusu ("karşılığı olmayan yetki")
tam olarak buydu.

### Arka plan işleri (Hangfire)

| İş | Ne yapar | Sıklık |
|---|---|---|
| `ExpireAdsJob` | Süresi dolan ilanı `approved` → `expired` | Saatlik |
| `ArchiveDeathsJob` | `auto_archive_at` geçen vefat ilanını arşivler | Günlük |
| `PublishScheduledAnnouncementsJob` | Zamanlanmış duyuruyu yayınlar + bildirim satırı üretir | Dakikalık |
| `SendPushNotificationsJob` | Gönderilmemiş bildirimleri FCM'e yollar | Dakikalık |

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
`Integration/Panel/PanelSortingTests.cs` içinde (panelin canlı denetiminde bulundular ve
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
