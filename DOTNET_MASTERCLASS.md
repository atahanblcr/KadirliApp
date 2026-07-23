# KadirliApp — .NET 8 Master İnşa Rehberi (Web API + ASP.NET Core MVC Panel)

> **Amaç:** Mevcut KadirliApp yapısının (NestJS + TypeORM + PostgreSQL backend, Next.js admin panel, Flutter mobil) **birebir aynısını ve daha kapsamlısını**, .NET 8 LTS ekosisteminde sıfırdan, "ilmek ilmek" inşa etmek. Bu doküman bir **referans + adım adım kurulum rehberidir**; her katmanı, her dosyayı, veritabanı şemasını, index ve stored procedure stratejisini, controller/model/view bağlantısını en ince ayrıntısına kadar açıklar.
>
> **Hedef Stack (bu projede seçilen kararlar):**
> | Karar | Seçim | Neden |
> |-------|-------|-------|
> | Platform | **.NET 8 LTS** | Olgun, geniş dökümante, uzun destek (Kasım 2026'ya kadar). |
> | API | **ASP.NET Core Web API** | Mobil (Flutter) ve panelin tükettiği JSON API. |
> | Panel | **ASP.NET Core MVC (Razor Views)** | Klasik Controller + Model + View (.cshtml). Tek solution içinde. |
> | Veri erişimi | **EF Core 8 + Dapper hibrit** | Yazma/CRUD/migration → EF Core; ağır okuma + rapor + stored procedure → Dapper. |
> | Veritabanı | **PostgreSQL 15+ (Npgsql)** | Mevcut şema zaten PostgreSQL; birebir taşınır. |
> | Cache/Kuyruk | **Redis (StackExchange.Redis) + Hangfire** | OTP/cache + zamanlanmış işler (cron, oto-arşiv). |
> | Kimlik | **JWT (access+refresh) + OTP (Redis) + bcrypt** | Mevcut akışın aynısı. |
> | Mobil | **Mevcut Flutter aynen kullanılır** | API kontratı korunduğu için tek satır Dart değişmez. |

---

## İçindekiler

1. [Mimari Felsefe & Katmanlı Tasarım](#1-mimari-felsefe--katmanlı-tasarım)
2. [Solution & Proje Yapısı](#2-solution--proje-yapısı)
3. [Veritabanı Tasarımı — Tablolar, İlişkiler, Index Stratejisi](#3-veritabanı-tasarımı)
4. [Stored Procedure & Fonksiyon Stratejisi (Hız Katmanı)](#4-stored-procedure--fonksiyon-stratejisi)
5. [Domain Katmanı — Entity'ler & Enum'lar](#5-domain-katmanı)
6. [Persistence Katmanı — EF Core DbContext & Configuration](#6-persistence-katmanı)
7. [Dapper Katmanı — SP Çağrıları & Hızlı Okuma](#7-dapper-katmanı)
8. [Repository & Unit of Work Pattern](#8-repository--unit-of-work-pattern)
9. [DTO'lar, Mapping & Validation](#9-dtolar-mapping--validation)
10. [Application (Service) Katmanı](#10-application-service-katmanı)
11. [Cross-Cutting: Response Envelope, Exception, Pagination](#11-cross-cutting-altyapı)
12. [Kimlik Doğrulama & Yetkilendirme (JWT + OTP + Permission)](#12-kimlik-doğrulama--yetkilendirme)
13. [Web API Controller Katmanı — Tüm Endpoint Haritası](#13-web-api-controller-katmanı)
14. [ASP.NET Core MVC Panel — Controller / Model / View Bağlantısı](#14-aspnet-core-mvc-panel)
15. [Caching, Background Jobs, File Upload, Bildirim](#15-caching-background-jobs-file-upload)
16. [Modül Modül Eşleme Tablosu (NestJS → .NET)](#16-modül-eşleme-tablosu)
17. [Performans Reçetesi (Index + SP + Connection Pool)](#17-performans-reçetesi)
18. [Tam Dikey Kesit Örneği — Ads (İlan) Modülü Uçtan Uca](#18-tam-dikey-kesit-ads-modülü)
19. [Konfigürasyon, Docker, CI/CD, Deployment](#19-konfigürasyon-docker-cicd)
20. [İnşa Sırası — "İlmek İlmek" Yol Haritası](#20-inşa-sırası-yol-haritası)

---

## 1. Mimari Felsefe & Katmanlı Tasarım

Mevcut NestJS yapısı zaten **modüler + katmanlı**: her modül `controller → service → repository(TypeORM) → entity`. .NET tarafında bunu **Clean Architecture'ın pragmatik bir varyantı** ile karşılıyoruz. Aşırı soyutlamadan kaçınıyoruz (her şeyi interface'leyip 7 katman yapmak hız düşmanıdır); ama test edilebilirlik ve net sorumluluk ayrımı için 4 net katman:

```
┌──────────────────────────────────────────────────────────────┐
│  Sunum (Presentation)                                          │
│  ├── KadirliApp.Api        → Web API Controllers (mobil+panel) │
│  └── KadirliApp.Web        → MVC Panel (Controller+Model+View) │
├──────────────────────────────────────────────────────────────┤
│  Uygulama (Application)                                        │
│  └── KadirliApp.Application → Servisler, DTO, Validator, Mapper│
│                               (iş kuralları burada)            │
├──────────────────────────────────────────────────────────────┤
│  Altyapı (Infrastructure)                                     │
│  └── KadirliApp.Infrastructure → EF Core DbContext, Dapper,   │
│       Repository, Redis, JWT, SMS, FileStorage, Hangfire jobs │
├──────────────────────────────────────────────────────────────┤
│  Çekirdek (Domain)                                            │
│  └── KadirliApp.Domain     → Entity'ler, Enum'lar, sabitler   │
└──────────────────────────────────────────────────────────────┘
```

**Bağımlılık yönü (dependency rule):** Dış katman içe bağımlıdır, iç katman dışı bilmez.
`Api/Web → Application → Domain` ve `Infrastructure → Application/Domain`.
`Domain` hiçbir şeye bağımlı değildir (saf POCO).

**NestJS karşılığı:**
| NestJS kavramı | .NET karşılığı |
|---|---|
| `@Module` | C# proje + `ServiceCollection` extension (`AddXModule()`) |
| `@Controller` | `[ApiController]` sınıfı |
| `@Injectable() Service` | `XService : IXService` (DI ile) |
| TypeORM `Repository<T>` | EF Core `DbSet<T>` + generic `Repository<T>` |
| `Entity` (`@Entity`) | POCO + `IEntityTypeConfiguration<T>` |
| `DTO` + `class-validator` | DTO record + **FluentValidation** |
| `Guard` (`canActivate`) | **Authorization Policy / Handler** veya `IAuthorizationFilter` |
| `Interceptor` (transform) | **Result Filter / Middleware** |
| `ExceptionFilter` | **Exception Middleware** |
| `@nestjs/schedule` (Cron) | **Hangfire** recurring jobs |
| Bull queue | **Hangfire** background jobs |
| `ConfigModule` + Joi | `IOptions<T>` + `appsettings.json` + validation |

---

## 2. Solution & Proje Yapısı

### 2.1 Solution oluşturma

```bash
mkdir KadirliApp && cd KadirliApp
dotnet new sln -n KadirliApp

# Katman projeleri
dotnet new classlib   -n KadirliApp.Domain          -f net8.0
dotnet new classlib   -n KadirliApp.Application      -f net8.0
dotnet new classlib   -n KadirliApp.Infrastructure   -f net8.0
dotnet new webapi      -n KadirliApp.Api              -f net8.0
dotnet new mvc         -n KadirliApp.Web              -f net8.0   # Razor Views panel

# Solution'a ekle
dotnet sln add **/*.csproj

# Proje referansları (bağımlılık yönü)
dotnet add KadirliApp.Application      reference KadirliApp.Domain
dotnet add KadirliApp.Infrastructure   reference KadirliApp.Application
dotnet add KadirliApp.Api              reference KadirliApp.Infrastructure
dotnet add KadirliApp.Web              reference KadirliApp.Infrastructure
```

### 2.2 NuGet paketleri

```bash
# Infrastructure
dotnet add KadirliApp.Infrastructure package Microsoft.EntityFrameworkCore --version 8.*
dotnet add KadirliApp.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.*
dotnet add KadirliApp.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 8.*
dotnet add KadirliApp.Infrastructure package Dapper
dotnet add KadirliApp.Infrastructure package StackExchange.Redis
dotnet add KadirliApp.Infrastructure package BCrypt.Net-Next
dotnet add KadirliApp.Infrastructure package Hangfire.Core
dotnet add KadirliApp.Infrastructure package Hangfire.PostgreSql

# Application
dotnet add KadirliApp.Application package FluentValidation.DependencyInjectionExtensions
dotnet add KadirliApp.Application package AutoMapper

# Api / Web (ortak)
dotnet add KadirliApp.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.*
dotnet add KadirliApp.Api package Swashbuckle.AspNetCore           # Swagger (NestJS'te yoktu, biz ekliyoruz)
dotnet add KadirliApp.Api package Hangfire.AspNetCore
dotnet add KadirliApp.Web package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.*
```

### 2.3 Klasör yerleşimi (özellik bazlı / feature-folder)

NestJS'te modüller (`ads`, `deaths`, ...) klasörlere ayrılmıştı. Aynı **feature-folder** yaklaşımını koruyoruz; katman içinde özelliğe göre gruplama:

```
KadirliApp.Domain/
├── Entities/
│   ├── User.cs, Neighborhood.cs, Ad.cs, AdCategory.cs, ... (her tablo bir POCO)
├── Enums/
│   └── UserRole.cs, AdStatus.cs, ...
└── Common/
    └── BaseEntity.cs, ISoftDeletable.cs, IAuditable.cs

KadirliApp.Application/
├── Common/
│   ├── Models/ (ApiResponse, PagedResult, PaginationParams)
│   ├── Exceptions/ (AppException, NotFoundException, ...)
│   └── Interfaces/ (IUnitOfWork, IRepository, IRedisService, IJwtService...)
├── Features/
│   ├── Ads/
│   │   ├── Dtos/ (CreateAdDto, UpdateAdDto, QueryAdDto, AdResponseDto)
│   │   ├── Validators/ (CreateAdDtoValidator)
│   │   ├── IAdsService.cs
│   │   └── AdsService.cs
│   ├── Auth/ ...
│   ├── Deaths/ ...
│   └── (her modül için bir klasör)
└── Mapping/
    └── AdProfile.cs, ... (AutoMapper profilleri)

KadirliApp.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/ (AdConfiguration : IEntityTypeConfiguration<Ad>, ...)
│   ├── Repositories/ (Repository<T>, UnitOfWork)
│   ├── Dapper/ (DapperContext, queries/AdQueries.cs)
│   ├── Migrations/ (dotnet ef migrations)
│   └── Seed/ (DbSeeder.cs)
├── Identity/ (JwtService, OtpService, PermissionService)
├── Caching/ (RedisService)
├── Files/ (LocalFileStorage / S3FileStorage)
├── Notifications/ (FcmService, SmsService)
├── Jobs/ (Hangfire recurring: ExpireAdsJob, ArchiveDeathsJob, PharmacyDutyJob)
└── DependencyInjection.cs (AddInfrastructure extension)

KadirliApp.Api/
├── Controllers/ (AdsController, AuthController, ... — mobil+panel JSON)
├── Filters/ (ApiResponseFilter, ValidationFilter)
├── Middleware/ (ExceptionMiddleware)
├── Extensions/ (AddApiServices, AddJwtAuth, AddSwagger)
├── Program.cs
└── appsettings.json

KadirliApp.Web/  (MVC PANEL)
├── Controllers/ (DashboardController, AdsAdminController, ...)
├── Models/ (ViewModels: AdListViewModel, AdFormViewModel, ...)
├── Views/
│   ├── Shared/ (_Layout.cshtml, _Sidebar.cshtml, _Pagination.cshtml)
│   ├── Dashboard/ (Index.cshtml)
│   ├── AdsAdmin/ (Index.cshtml, Details.cshtml)
│   └── ...
├── Services/ (ApiClient.cs — panel API'yi mi çağırır yoksa servisleri direkt mi kullanır; aşağıda)
├── wwwroot/ (css, js, lib — Bootstrap/Tailwind)
└── Program.cs
```

> **Önemli mimari karar — Panel veriye nasıl erişir?**
> İki seçenek var:
> 1. **Doğrudan servis (önerilen, tek deploy):** `KadirliApp.Web`, `Infrastructure`'ı referans alır ve `Application` servislerini **doğrudan DI ile** çağırır. Daha hızlı (HTTP atlama yok), tek veritabanı bağlantısı havuzu. Bu rehberde bunu kullanıyoruz.
> 2. **API üzerinden (HttpClient):** Panel, `KadirliApp.Api`'yi tıpkı Flutter gibi HTTP ile tüketir. Mevcut Next.js mimarisine birebir benzer ama ekstra ağ gecikmesi. Ölçeklenince (panel ayrı sunucu) tercih edilir.
>
> Biz **#1**'i temel alıp, gerektiğinde #2'ye geçişi kolaylaştıran `IAdminFacade` arayüzü ile yazıyoruz.

---

## 3. Veritabanı Tasarımı

Mevcut PostgreSQL şeması (TypeORM `InitialSchema` migration'ı) **birebir korunuyor**. ~45 tablo var. Aşağıda tam liste, gruplandırılmış halde. UUID PK'lar `uuid_generate_v4()` (uuid-ossp) ile; .NET tarafında `Guid` + `gen_random_uuid()` (pgcrypto, PG13+ built-in) kullanacağız.

### 3.1 Tablo envanteri (modül bazlı)

**Kimlik & Kullanıcı**
- `users` — id, phone (uniq), email (uniq, nullable), password (nullable, `select:false`), username (uniq), age, role (enum: user/moderator/admin/super_admin), primary_neighborhood_id (FK), location_type, notification_preferences (jsonb), fcm_token, profile_photo_url, username_last_changed_at, neighborhood_last_changed_at, is_active, is_banned, ban_reason, banned_at, banned_by, created_at, updated_at, **deleted_at** (soft delete).
- `neighborhoods` — id, name, slug (uniq), type, population, latitude, longitude, display_order, is_active, timestamps.
- `user_neighborhoods` — (user_id, neighborhood_id) çoklu mahalle; uniq (user_id, neighborhood_id).
- `admin_permissions` — (sonradan eklenen migration) user_id, module, can_read/create/update/delete/approve (boolean'lar). **Index: user_id.**

**Yetki & Audit**
- `permissions` — module, action (uniq birlikte).
- `role_permissions` — role, permission_id (uniq birlikte).
- `audit_logs` — user_id, action, module, affected_id, affected_type, details(jsonb), ip_address(inet), user_agent, created_at.

**Duyuru & Elektrik**
- `announcement_types` — name/slug uniq, icon, color, display_order.
- `announcements` — type_id(FK), title, body, priority, target_type, target_neighborhoods(jsonb), target_user_ids(jsonb), scheduled_for, sent_at, is_recurring, recurrence_pattern, send_push_notification, source, source_url, visible_until, has_pdf, pdf_file_id(FK), has_link, external_link, view_count, click_count, status, created_by(FK), approved_by, soft delete.
- `announcement_views` — (announcement_id, user_id) uniq.
- `power_outages` — announcement_id(FK), neighborhood, start/end_time, reason, source.

**İlan (Ads) — en karmaşık modül**
- `ad_categories` — name, slug uniq, parent_id(self-FK, ağaç), icon, display_order, is_active.
- `category_properties` — category_id(FK), property_name, property_type, is_required, default_value, display_order. Uniq (category_id, property_name).
- `property_options` — property_id(FK), option_value, display_order.
- `ads` — category_id(FK), title, description, price, user_id(FK), seller_name, contact_phone, status(pending/approved/rejected/expired), approved_by, approved_at, rejected_reason, rejected_at, **expires_at**, extension_count, max_extensions, view_count, phone_click_count, whatsapp_click_count, timestamps, **deleted_at**.
- `ad_images` — ad_id(FK), file_id(FK), is_cover, display_order. Uniq (ad_id, file_id).
- `ad_property_values` — ad_id(FK), property_id(FK), value. Uniq (ad_id, property_id).
- `ad_favorites` — (user_id, ad_id) uniq.
- `ad_extensions` — ad_id, user_id, ads_watched, days_extended, extended_at.

**Vefat (Deaths)**
- `cemeteries` — name, address, lat/lng.
- `mosques` — name, address, lat/lng.
- `death_notices` — deceased_name, age, photo_file_id(FK), funeral_date, funeral_time, cemetery_id(FK), mosque_id(FK), neighborhood_id (sonradan eklendi), condolence_address, added_by(FK), status(pending/approved/rejected), approved_by, approved_at, rejected_reason, **auto_archive_at**, timestamps, soft delete.

**Eczane (Pharmacy)**
- `pharmacies` — name, address, phone, lat/lng, working_hours, pharmacist_name, is_active.
- `pharmacy_schedules` — pharmacy_id(FK), duty_date, start_time(19:00), end_time(09:00), source.

**Etkinlik (Events)**
- `event_categories` — name/slug.
- `events` — title, description, category_id(FK), event_date, event_time, duration_minutes, venue_name/address, city, lat/lng, organizer, ticket_price, is_free, age_restriction, capacity, website_url, ticket_url, cover_image_id(FK), is_recurring, recurrence_pattern, **is_local** (sonradan eklendi), status, created_by(FK), soft delete.
- `event_images` — event_id(FK), file_id(FK), display_order.

**Kampanya & İşletme (Campaigns)**
- `business_categories` — name/slug, parent_id(self-FK).
- `businesses` — user_id(FK, **nullable** — sonradan), business_name, category_id(FK), tax_number, address, phone, email, website_url, instagram_handle, logo_file_id(FK), is_verified, verified_by, verified_at.
- `campaigns` — business_id(FK), title, description, discount_percentage, discount_code, terms, minimum_amount, stock_limit, start_date, end_date, cover_image_id(FK), code_view_count, status(pending/...), approved_by, approved_at, rejected_reason, soft delete.
- `campaign_images` — campaign_id(FK), file_id(FK), display_order.
- `campaign_code_views` — campaign_id, user_id, viewed_at.

**Rehber (Guide)**
- `guide_categories` — name, slug, parent_id(self-FK), icon, color, display_order.
- `guide_items` — category_id(FK), name, phone, address, email, website_url, working_hours, lat/lng, logo_file_id(FK), description, is_active.

**Gezilecek Yerler (Places)**
- `place_categories` — name, slug, icon, display_order.
- `places` — category_id(FK), name, description, address, lat/lng (NOT NULL), entrance_fee, is_free, opening_hours, best_season, how_to_get_there, distance_from_center, cover_image_id(FK), is_active, created_by(FK).
- `place_images` — place_id(FK), file_id(FK), display_order.

**Taksi (Taxi)**
- `taxi_drivers` — user_id(FK, **nullable** — sonradan), name, phone, plaka, vehicle_info, license_file_id(FK), registration_file_id(FK), is_verified, verified_by, verified_at, is_active, total_calls, timestamps, soft delete.
- `taxi_calls` — passenger_id(FK), driver_id(FK), called_at.

**Ulaşım (Transport)**
- `intercity_routes` — destination, price, duration_minutes, company, is_active.
- `intercity_schedules` — route_id(FK), departure_time, is_active.
- `intracity_routes` — route_number, route_name, first_departure, last_departure, frequency_minutes, is_active.
- `intracity_stops` — route_id(FK), stop_name, stop_order, time_from_start.

**Dosya & Bildirim & Şikayet**
- `files` — original_name, file_name(uniq), mime_type, size_bytes, storage_path, cdn_url, thumbnail_url, module_type, module_id, uploaded_by(FK), metadata(jsonb), created_at, soft delete.
- `notifications` — user_id(FK), title, body, type, related_id, related_type, is_read, read_at, fcm_sent, fcm_sent_at, fcm_error, created_at.
- `complaints` — user_id(FK, nullable), type, related_module, related_id, subject, message, status, admin_notes, resolved_by, resolved_at, created_at.

### 3.2 Index stratejisi — KRİTİK

TypeORM şemasında neredeyse hiç açık index yoktu (sadece PK + uniq constraint'ler ve `admin_permissions.user_id`). **Bu, .NET versiyonunda gerçek dünyada en büyük performans kazancımız olacak.** Aşağıdaki index'ler "indexlerde procedureler ile hızlı çalışan yapı" hedefinin temelidir. Bunları ayrı bir migration'da ekleyeceğiz.

**Kural:** Index'i sorgu desenine göre koy. Aşağıdakiler gerçek erişim desenlerinden (controller filtreleri) türetildi:

```sql
-- ── ADS: liste sorguları status + kategori + tarih sıralı + soft delete filtreli ──
-- En sık sorgu: WHERE status='approved' AND deleted_at IS NULL ORDER BY created_at DESC
CREATE INDEX ix_ads_status_created    ON ads (status, created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX ix_ads_category          ON ads (category_id)            WHERE deleted_at IS NULL;
CREATE INDEX ix_ads_user              ON ads (user_id)                WHERE deleted_at IS NULL;
CREATE INDEX ix_ads_expires           ON ads (expires_at)            WHERE status = 'approved';
-- Fiyat aralığı + tam metin arama için:
CREATE INDEX ix_ads_price             ON ads (price)                 WHERE deleted_at IS NULL;
CREATE INDEX ix_ads_title_trgm        ON ads USING gin (title gin_trgm_ops);  -- pg_trgm: LIKE '%kelime%' hızlı

-- ── İlişki tabloları (FK index'leri — PG FK'ya OTOMATİK index KOYMAZ!) ──
CREATE INDEX ix_ad_images_ad          ON ad_images (ad_id);
CREATE INDEX ix_ad_prop_values_ad     ON ad_property_values (ad_id);
CREATE INDEX ix_ad_favorites_user     ON ad_favorites (user_id);

-- ── ANNOUNCEMENTS ──
CREATE INDEX ix_ann_status_created    ON announcements (status, created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX ix_ann_type              ON announcements (type_id);
CREATE INDEX ix_ann_scheduled         ON announcements (scheduled_for) WHERE status = 'scheduled';
-- jsonb target_neighborhoods içinde mahalle araması için GIN:
CREATE INDEX ix_ann_target_nb_gin     ON announcements USING gin (target_neighborhoods);

-- ── DEATHS: onaylı + arşiv tarihi + tarih ──
CREATE INDEX ix_deaths_status_funeral ON death_notices (status, funeral_date DESC) WHERE deleted_at IS NULL;
CREATE INDEX ix_deaths_archive        ON death_notices (auto_archive_at) WHERE status = 'approved';
CREATE INDEX ix_deaths_neighborhood   ON death_notices (neighborhood_id);

-- ── PHARMACY: nöbet tarihine göre (en sık "bugün nöbetçi") ──
CREATE INDEX ix_pharm_sched_date      ON pharmacy_schedules (duty_date);

-- ── EVENTS ──
CREATE INDEX ix_events_status_date    ON events (status, event_date) WHERE deleted_at IS NULL;
CREATE INDEX ix_events_category       ON events (category_id);

-- ── CAMPAIGNS ──
CREATE INDEX ix_campaigns_status_dates ON campaigns (status, start_date, end_date) WHERE deleted_at IS NULL;
CREATE INDEX ix_campaigns_business    ON campaigns (business_id);

-- ── NOTIFICATIONS: kullanıcının okunmamışları ──
CREATE INDEX ix_notif_user_read       ON notifications (user_id, is_read, created_at DESC);

-- ── AUDIT / ARAMA ──
CREATE INDEX ix_audit_user_created    ON audit_logs (user_id, created_at DESC);
CREATE INDEX ix_audit_module          ON audit_logs (module, created_at DESC);

-- ── USERS: panel kullanıcı arama (telefon/username/email) ──
CREATE INDEX ix_users_role            ON users (role)  WHERE deleted_at IS NULL;
CREATE INDEX ix_users_neighborhood    ON users (primary_neighborhood_id);

-- ── GUIDE / PLACES / TAXI FK index'leri ──
CREATE INDEX ix_guide_items_category  ON guide_items (category_id);
CREATE INDEX ix_places_category       ON places (category_id);
CREATE INDEX ix_place_images_place    ON place_images (place_id);
CREATE INDEX ix_taxi_calls_driver     ON taxi_calls (driver_id);
CREATE INDEX ix_intracity_stops_route ON intracity_stops (route_id, stop_order);
CREATE INDEX ix_intercity_sched_route ON intercity_schedules (route_id);
```

> **Genel kurallar:**
> - **Her FK'ya index koy.** PostgreSQL FK için otomatik index oluşturmaz; child tabloda JOIN/silme yavaşlar.
> - **Partial index** (`WHERE deleted_at IS NULL`) ile soft-delete'li tablolarda index küçük ve hızlı kalır.
> - **Composite index sırası:** eşitlik filtreleri önce, sıralama/aralık sonra (`(status, created_at DESC)`).
> - **pg_trgm + GIN** ile `LIKE '%...%'` aramalar index kullanır (`CREATE EXTENSION pg_trgm;`).
> - **jsonb için GIN** (`target_neighborhoods @> '["uuid"]'`).
> - Index ekledikten sonra `ANALYZE;` çalıştır, `EXPLAIN (ANALYZE, BUFFERS)` ile doğrula.

### 3.3 Gerekli PostgreSQL extension'ları

```sql
CREATE EXTENSION IF NOT EXISTS "pgcrypto";   -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS "pg_trgm";    -- trigram arama
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";  -- uuid_generate_v4() (mevcut şema uyumu)
```

---

## 4. Stored Procedure & Fonksiyon Stratejisi

PostgreSQL'de "stored procedure" iki biçimde gelir: `FUNCTION` (değer döner, sorguda kullanılır — bizim asıl aracımız) ve `PROCEDURE` (transaction kontrolü, `CALL` ile). **Hız + iş mantığı veri yakınında** hedefimiz için aşağıdaki kalıpları kullanıyoruz. Bunlar Dapper ile çağrılır.

### 4.1 Ne zaman SP/Fonksiyon, ne zaman EF/LINQ?

| Senaryo | Araç |
|---|---|
| Basit CRUD (create/update/delete tek kayıt) | **EF Core** (değişiklik takibi, migration kolaylığı) |
| Tek satırlık atomik sayaç artışı (view_count++) | **SP/Function** (race-condition'sız, tek round-trip) |
| Çok JOIN'li liste + sayfalama + filtre (ilan listesi) | **Dapper + SQL** (gerekirse Function) |
| Karmaşık iş kuralı + birden çok tablo + transaction (ilan onay, ilan uzatma) | **PROCEDURE** veya servis-içi EF transaction |
| Dashboard/rapor aggregation | **SQL View** + Dapper |
| Tarih bazlı toplu iş (oto-arşiv, ilan expire) | **Hangfire job → SP CALL** |

### 4.2 Örnek fonksiyonlar (gerçek iş kurallarından)

**a) İlan görüntülenme sayacı (atomik, race-free):**
```sql
CREATE OR REPLACE FUNCTION fn_increment_ad_view(p_ad_id uuid)
RETURNS integer
LANGUAGE plpgsql AS $$
DECLARE v_count integer;
BEGIN
  UPDATE ads SET view_count = view_count + 1
  WHERE id = p_ad_id AND deleted_at IS NULL
  RETURNING view_count INTO v_count;
  RETURN v_count;  -- NULL ise ilan yok
END;
$$;
```

**b) İlan onaylama (PROCEDURE — transaction + audit + bildirim kuyruğu):**
```sql
CREATE OR REPLACE PROCEDURE sp_approve_ad(
  p_ad_id uuid, p_admin_id uuid, p_days int DEFAULT 30
) LANGUAGE plpgsql AS $$
BEGIN
  UPDATE ads
  SET status = 'approved',
      approved_by = p_admin_id,
      approved_at = now(),
      expires_at = now() + (p_days || ' days')::interval,
      updated_at = now()
  WHERE id = p_ad_id AND status = 'pending';

  IF NOT FOUND THEN
    RAISE EXCEPTION 'Ad not found or not pending: %', p_ad_id
      USING ERRCODE = 'P0002';   -- servis tarafında NotFound'a map'lenir
  END IF;

  INSERT INTO audit_logs (id, user_id, action, module, affected_id, affected_type, created_at)
  VALUES (gen_random_uuid(), p_admin_id, 'approve', 'ads', p_ad_id, 'ad', now());
  -- FCM bildirimi Hangfire job ile gönderilir; burada notifications satırı eklenebilir.
END;
$$;
```

**c) Bugünün nöbetçi eczaneleri (function — sık çağrılan, cache'lenebilir):**
```sql
CREATE OR REPLACE FUNCTION fn_current_duty_pharmacies(p_date date DEFAULT current_date)
RETURNS TABLE (id uuid, name varchar, address text, phone varchar,
               latitude numeric, longitude numeric, start_time time, end_time time)
LANGUAGE sql STABLE AS $$
  SELECT p.id, p.name, p.address, p.phone, p.latitude, p.longitude,
         s.start_time, s.end_time
  FROM pharmacy_schedules s
  JOIN pharmacies p ON p.id = s.pharmacy_id AND p.is_active = true
  WHERE s.duty_date = p_date
  ORDER BY p.name;
$$;
```

**d) İlan listesi — sayfalı + filtreli (function, anahtar performans noktası):**
```sql
CREATE OR REPLACE FUNCTION fn_search_ads(
  p_category_id uuid, p_min_price numeric, p_max_price numeric,
  p_search text, p_limit int, p_offset int
)
RETURNS TABLE (id uuid, title varchar, price numeric, status varchar,
               cover_url text, created_at timestamp, total_count bigint)
LANGUAGE sql STABLE AS $$
  SELECT a.id, a.title, a.price, a.status,
         f.cdn_url AS cover_url, a.created_at,
         count(*) OVER() AS total_count          -- tek sorguda toplam + sayfa
  FROM ads a
  LEFT JOIN ad_images ai ON ai.ad_id = a.id AND ai.is_cover = true
  LEFT JOIN files f      ON f.id = ai.file_id
  WHERE a.deleted_at IS NULL
    AND a.status = 'approved'
    AND (p_category_id IS NULL OR a.category_id = p_category_id)
    AND (p_min_price   IS NULL OR a.price >= p_min_price)
    AND (p_max_price   IS NULL OR a.price <= p_max_price)
    AND (p_search      IS NULL OR a.title ILIKE '%' || p_search || '%')
  ORDER BY a.created_at DESC
  LIMIT p_limit OFFSET p_offset;
$$;
```

**e) Vefat ilanı oto-arşivleme (Hangfire'dan günlük çağrılır):**
```sql
CREATE OR REPLACE PROCEDURE sp_archive_expired_deaths()
LANGUAGE sql AS $$
  UPDATE death_notices SET status = 'archived', updated_at = now()
  WHERE status = 'approved' AND auto_archive_at < now();
$$;
```

**f) Dashboard sayıları (VIEW + tek sorgu):**
```sql
CREATE OR REPLACE VIEW vw_dashboard_counts AS
SELECT
  (SELECT count(*) FROM users WHERE deleted_at IS NULL)                         AS total_users,
  (SELECT count(*) FROM ads WHERE status='pending' AND deleted_at IS NULL)      AS pending_ads,
  (SELECT count(*) FROM death_notices WHERE status='pending')                   AS pending_deaths,
  (SELECT count(*) FROM campaigns WHERE status='pending')                       AS pending_campaigns,
  (SELECT count(*) FROM complaints WHERE status='pending')                      AS pending_complaints;
```

> Bu SP'leri EF Core migration içine `migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION ...")` ile koyarız; böylece versiyonlanır ve deploy ile birlikte gider (Bölüm 6.5).

---

## 5. Domain Katmanı

Saf POCO'lar. NestJS entity'lerinin birebir karşılığı. snake_case kolonlar EF konfigürasyonunda map'lenir; C# tarafında PascalCase.

### 5.1 Base sınıflar

```csharp
// KadirliApp.Domain/Common/BaseEntity.cs
namespace KadirliApp.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public interface ISoftDeletable { DateTime? DeletedAt { get; set; } }
```

### 5.2 Enum'lar

```csharp
// KadirliApp.Domain/Enums/UserRole.cs
namespace KadirliApp.Domain.Enums;

public enum UserRole { User, Moderator, Admin, SuperAdmin }
// PG enum 'users_role_enum' = ('user','moderator','admin','super_admin')

public enum AdStatus { Pending, Approved, Rejected, Expired }
public enum DeathStatus { Pending, Approved, Rejected, Archived }
public enum CampaignStatus { Pending, Approved, Rejected, Expired }
```

> **Enum eşleme notu:** PG'de bu değerler küçük-harf string. EF Core'da `HasConversion<string>()` + Npgsql snake_case dönüşümü ile veya custom `ValueConverter` ile eşleriz. NestJS'te bazıları gerçek PG enum (`users_role_enum`), bazıları `varchar`'dı. Biz tutarlılık için **status alanlarını `varchar` + check constraint**, sadece `role`'ü PG enum yapıyoruz (mevcut şemayla uyum).

### 5.3 Örnek entity'ler

```csharp
// KadirliApp.Domain/Entities/User.cs
using KadirliApp.Domain.Common;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Domain.Entities;

public class User : BaseEntity, ISoftDeletable
{
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Password { get; set; }              // select:false → ayrı yüklenir
    public string? Username { get; set; }
    public int? Age { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public Guid? PrimaryNeighborhoodId { get; set; }
    public string? LocationType { get; set; }
    public NotificationPreferences NotificationPreferences { get; set; } = new();  // jsonb
    public string? FcmToken { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DateTime? UsernameLastChangedAt { get; set; }
    public DateTime? NeighborhoodLastChangedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedAt { get; set; }
    public Guid? BannedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Neighborhood? PrimaryNeighborhood { get; set; }
    public ICollection<UserNeighborhood> Neighborhoods { get; set; } = new List<UserNeighborhood>();
}

public class NotificationPreferences   // jsonb POCO
{
    public bool Announcements { get; set; } = true;
    public bool Deaths { get; set; } = true;
    public bool Pharmacy { get; set; } = true;
    public bool Events { get; set; } = true;
    public bool Ads { get; set; } = false;
    public bool Campaigns { get; set; } = false;
}
```

```csharp
// KadirliApp.Domain/Entities/Ad.cs
public class Ad : BaseEntity, ISoftDeletable
{
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Price { get; set; }
    public Guid UserId { get; set; }
    public string? SellerName { get; set; }
    public string ContactPhone { get; set; } = default!;
    public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ExtensionCount { get; set; }
    public int MaxExtensions { get; set; } = 3;
    public int ViewCount { get; set; }
    public int PhoneClickCount { get; set; }
    public int WhatsappClickCount { get; set; }
    public DateTime? DeletedAt { get; set; }

    public AdCategory Category { get; set; } = default!;
    public User User { get; set; } = default!;
    public ICollection<AdImage> Images { get; set; } = new List<AdImage>();
    public ICollection<AdPropertyValue> PropertyValues { get; set; } = new List<AdPropertyValue>();
}
```

> Her tablo için bir entity. NestJS `src/database/entities/*.entity.ts` dosyalarının birebir karşılığı. Bölüm 16'daki eşleme tablosu hepsini listeler.

---

## 6. Persistence Katmanı

### 6.1 DbContext

```csharp
// KadirliApp.Infrastructure/Persistence/AppDbContext.cs
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<AdCategory> AdCategories => Set<AdCategory>();
    public DbSet<AdImage> AdImages => Set<AdImage>();
    // ... her entity için DbSet

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tüm IEntityTypeConfiguration'ları otomatik uygula
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Snake_case kolon/tablo isimleri (NestJS şemasıyla uyum)
        modelBuilder.UseSnakeCaseNamingConvention(); // EFCore.NamingConventions paketi

        base.OnModelCreating(modelBuilder);
    }

    // updated_at otomatik güncelleme (NestJS @UpdateDateColumn karşılığı)
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added)   e.Entity.CreatedAt = DateTime.UtcNow;
            if (e.State is EntityState.Added or EntityState.Modified)
                e.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
```

> `UseSnakeCaseNamingConvention` için: `dotnet add KadirliApp.Infrastructure package EFCore.NamingConventions`.

### 6.2 Örnek Entity Configuration

```csharp
// KadirliApp.Infrastructure/Persistence/Configurations/AdConfiguration.cs
using KadirliApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    public void Configure(EntityTypeBuilder<Ad> b)
    {
        b.ToTable("ads");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.Price).HasPrecision(12, 2);
        b.Property(x => x.ContactPhone).HasMaxLength(15).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");
        b.Property(x => x.MaxExtensions).HasDefaultValue(3);

        // Soft delete global filter (NestJS deleted_at IS NULL otomatiği karşılığı)
        b.HasQueryFilter(x => x.DeletedAt == null);

        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Images).WithOne().HasForeignKey(i => i.AdId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler (config'te de tanımlanabilir; biz ham SQL migration tercih ediyoruz)
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => x.CategoryId);
    }
}
```

```csharp
// User jsonb + select:false örneği
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Phone).HasMaxLength(15).IsRequired();
        b.HasIndex(x => x.Phone).IsUnique();
        b.HasIndex(x => x.Username).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();

        // jsonb (NestJS jsonb karşılığı) — Npgsql otomatik serialize eder
        b.OwnsOne(x => x.NotificationPreferences, o => o.ToJson());

        // role PG enum
        b.Property(x => x.Role).HasConversion<string>().HasColumnType("users_role_enum");

        // password: select:false → varsayılan sorguda gelmesin
        b.Property(x => x.Password).HasColumnName("password");
        // EF'de "select false" yok; servis tarafında .Select() ile dışla,
        // veya admin login'de özel sorgu: aşağıda Bölüm 12.4'e bak.

        b.HasQueryFilter(x => x.DeletedAt == null);
    }
}
```

### 6.3 Connection string & DI

```csharp
// KadirliApp.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var conn = cfg.GetConnectionString("Postgres");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(conn, npg =>
            {
                npg.EnableRetryOnFailure(3);
                npg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IDapperContext, DapperContext>();    // Bölüm 7
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis")!));
        services.AddScoped<IRedisService, RedisService>();

        // Identity
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // Files / Notifications
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IFcmService, FcmService>();

        return services;
    }
}
```

`appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=kadirliapp;Username=postgres;Password=postgres;Include Error Detail=true;Maximum Pool Size=100;Minimum Pool Size=10",
    "Redis": "localhost:6379"
  }
}
```

### 6.4 Migration komutları

```bash
# Infrastructure projesinde, startup olarak Api
dotnet ef migrations add InitialSchema  -p KadirliApp.Infrastructure -s KadirliApp.Api
dotnet ef database update                -p KadirliApp.Infrastructure -s KadirliApp.Api
```

### 6.5 SP/Index'leri migration'a koymak

EF migration `Up()` içine ham SQL:
```csharp
public partial class AddIndexesAndProcs : Migration
{
    protected override void Up(MigrationBuilder mb)
    {
        mb.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        mb.Sql(@"CREATE INDEX ix_ads_status_created ON ads (status, created_at DESC) WHERE deleted_at IS NULL;");
        mb.Sql(@"CREATE OR REPLACE FUNCTION fn_increment_ad_view(p_ad_id uuid) RETURNS integer ... $$;");
        // ... Bölüm 3.2 ve 4'teki tüm SQL'ler
    }
    protected override void Down(MigrationBuilder mb)
    {
        mb.Sql("DROP FUNCTION IF EXISTS fn_increment_ad_view(uuid);");
        mb.Sql("DROP INDEX IF EXISTS ix_ads_status_created;");
    }
}
```

---

## 7. Dapper Katmanı

EF Core yazma için; Dapper **ağır okuma + SP çağrısı** için.

```csharp
// KadirliApp.Application/Common/Interfaces/IDapperContext.cs
public interface IDapperContext { IDbConnection CreateConnection(); }

// KadirliApp.Infrastructure/Persistence/Dapper/DapperContext.cs
public class DapperContext : IDapperContext
{
    private readonly string _conn;
    public DapperContext(IConfiguration cfg) => _conn = cfg.GetConnectionString("Postgres")!;
    public IDbConnection CreateConnection() => new NpgsqlConnection(_conn);
}
```

**SP/Function çağrısı (function = SELECT; procedure = CALL):**
```csharp
// KadirliApp.Infrastructure/Persistence/Dapper/AdQueries.cs
public class AdQueries
{
    private readonly IDapperContext _ctx;
    public AdQueries(IDapperContext ctx) => _ctx = ctx;

    // fn_search_ads function'ını çağır — total_count window ile tek sorguda
    public async Task<PagedResult<AdListItem>> SearchAsync(AdSearchParams p)
    {
        using var db = _ctx.CreateConnection();
        var rows = (await db.QueryAsync<AdListItem>(
            "SELECT * FROM fn_search_ads(@cat, @min, @max, @search, @limit, @offset)",
            new { cat = p.CategoryId, min = p.MinPrice, max = p.MaxPrice,
                  search = p.Search, limit = p.Limit, offset = p.Offset })).ToList();

        var total = rows.FirstOrDefault()?.TotalCount ?? 0;
        return new PagedResult<AdListItem>(rows, total, p.Page, p.Limit);
    }

    // Atomik view artışı (function)
    public async Task<int?> IncrementViewAsync(Guid adId)
    {
        using var db = _ctx.CreateConnection();
        return await db.ExecuteScalarAsync<int?>(
            "SELECT fn_increment_ad_view(@id)", new { id = adId });
    }

    // Procedure çağrısı (CALL) — onaylama
    public async Task ApproveAsync(Guid adId, Guid adminId, int days = 30)
    {
        using var db = _ctx.CreateConnection();
        await db.ExecuteAsync("CALL sp_approve_ad(@ad, @admin, @days)",
            new { ad = adId, admin = adminId, days });
    }
}
```

> **Kural:** Dapper sınıfları Infrastructure'da; Application servisi `IAdQueries` arayüzü üzerinden çağırır (test edilebilirlik). EF + Dapper aynı transaction'ı paylaşması gerekiyorsa `UnitOfWork` üzerinden aynı `DbConnection`/`DbTransaction` geçirilir.

---

## 8. Repository & Unit of Work Pattern

```csharp
// KadirliApp.Application/Common/Interfaces/IRepository.cs
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    IQueryable<T> Query(bool tracking = false);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);               // hard delete
    void SoftRemove(ISoftDeletable e);   // deleted_at = now
}

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync();
}
```

```csharp
// KadirliApp.Infrastructure/Persistence/Repositories/Repository.cs
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    private readonly AppDbContext _db;
    public Repository(AppDbContext db) => _db = db;

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);

    public IQueryable<T> Query(bool tracking = false)
        => tracking ? _db.Set<T>() : _db.Set<T>().AsNoTracking();

    public async Task AddAsync(T e, CancellationToken ct = default) => await _db.Set<T>().AddAsync(e, ct);
    public void Update(T e) => _db.Set<T>().Update(e);
    public void Remove(T e) => _db.Set<T>().Remove(e);
    public void SoftRemove(ISoftDeletable e) => e.DeletedAt = DateTime.UtcNow;
}
```

> NestJS'te servisler doğrudan `Repository<T>` (TypeORM) inject ediyordu. Bizde de servis ya `IRepository<T>` ya da daha karmaşık modüllerde özel `IAdsRepository` kullanır. Basit modüllerde generic repo yeterli.

---

## 9. DTO'lar, Mapping & Validation

### 9.1 DTO (record) — NestJS DTO karşılığı

```csharp
// KadirliApp.Application/Features/Ads/Dtos/CreateAdDto.cs
public record CreateAdDto(
    Guid CategoryId,
    string Title,
    string Description,
    decimal? Price,
    string ContactPhone,
    string? SellerName,
    List<Guid>? ImageFileIds,
    Dictionary<Guid, string>? PropertyValues   // property_id → value
);

public record QueryAdDto(
    Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice,
    string? Search, int Page = 1, int Limit = 20);

public record AdResponseDto(
    Guid Id, string Title, string? Description, decimal? Price,
    string Status, string ContactPhone, int ViewCount,
    DateTime CreatedAt, List<string> ImageUrls);
```

### 9.2 FluentValidation (class-validator karşılığı)

```csharp
// KadirliApp.Application/Features/Ads/Validators/CreateAdDtoValidator.cs
public class CreateAdDtoValidator : AbstractValidator<CreateAdDto>
{
    public CreateAdDtoValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.ContactPhone).NotEmpty()
            .Matches(@"^(\+90|0)?5\d{9}$").WithMessage("Geçerli bir telefon giriniz");
    }
}
```

Program.cs'te: `builder.Services.AddValidatorsFromAssembly(typeof(CreateAdDtoValidator).Assembly);`
ve global olarak otomatik 400 dönmek için bir `ValidationFilter` (Bölüm 11.4) veya `AddFluentValidationAutoValidation()`.

### 9.3 AutoMapper

```csharp
public class AdProfile : Profile
{
    public AdProfile()
    {
        CreateMap<Ad, AdResponseDto>()
            .ForCtorParam("ImageUrls", o => o.MapFrom(s =>
                s.Images.Select(i => i.File!.CdnUrl).ToList()));
        CreateMap<CreateAdDto, Ad>();
    }
}
```
`builder.Services.AddAutoMapper(typeof(AdProfile).Assembly);`

---

## 10. Application (Service) Katmanı

NestJS `*.service.ts` birebir karşılığı. İş kuralları burada; controller ince kalır.

```csharp
// KadirliApp.Application/Features/Ads/IAdsService.cs
public interface IAdsService
{
    Task<PagedResult<AdListItem>> FindAllAsync(QueryAdDto dto);
    Task<AdResponseDto> FindOneAsync(Guid id);
    Task<AdResponseDto> CreateAsync(Guid userId, CreateAdDto dto);
    Task<AdResponseDto> UpdateAsync(Guid id, Guid userId, UpdateAdDto dto);
    Task RemoveAsync(Guid id, Guid userId);
    Task<object> ExtendAsync(Guid id, Guid userId, ExtendAdDto dto);
    Task AddFavoriteAsync(Guid id, Guid userId);
    Task RemoveFavoriteAsync(Guid id, Guid userId);
    Task<int?> TrackPhoneAsync(Guid id);
}
```

```csharp
// KadirliApp.Application/Features/Ads/AdsService.cs
public class AdsService : IAdsService
{
    private readonly IUnitOfWork _uow;
    private readonly IAdQueries _queries;      // Dapper
    private readonly IMapper _mapper;
    private readonly IRedisService _redis;

    public AdsService(IUnitOfWork uow, IAdQueries queries, IMapper mapper, IRedisService redis)
        => (_uow, _queries, _mapper, _redis) = (uow, queries, mapper, redis);

    // Liste → Dapper (hız)
    public Task<PagedResult<AdListItem>> FindAllAsync(QueryAdDto dto)
        => _queries.SearchAsync(new AdSearchParams(dto));

    // Detay → EF (Include'larla) + atomik view artışı (SP)
    public async Task<AdResponseDto> FindOneAsync(Guid id)
    {
        var ad = await _uow.Repository<Ad>().Query()
            .Include(a => a.Images).ThenInclude(i => i.File)
            .Include(a => a.Category)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new NotFoundException("İlan bulunamadı");

        await _queries.IncrementViewAsync(id);   // race-free SP
        return _mapper.Map<AdResponseDto>(ad);
    }

    // Oluştur → EF transaction (ad + images + property_values)
    public async Task<AdResponseDto> CreateAsync(Guid userId, CreateAdDto dto)
    {
        await using var tx = await _uow.BeginTransactionAsync();
        var ad = _mapper.Map<Ad>(dto);
        ad.UserId = userId;
        ad.Status = "pending";
        ad.ExpiresAt = DateTime.UtcNow.AddDays(30);
        await _uow.Repository<Ad>().AddAsync(ad);
        await _uow.SaveChangesAsync();

        if (dto.ImageFileIds is { Count: > 0 })
            foreach (var (fid, idx) in dto.ImageFileIds.Select((f, i) => (f, i)))
                await _uow.Repository<AdImage>().AddAsync(new AdImage {
                    AdId = ad.Id, FileId = fid, IsCover = idx == 0, DisplayOrder = idx });

        await _uow.SaveChangesAsync();
        await tx.CommitAsync();
        return await FindOneAsync(ad.Id);
    }

    // Sahiplik kontrolü + soft delete (NestJS pattern'i birebir)
    public async Task RemoveAsync(Guid id, Guid userId)
    {
        var ad = await _uow.Repository<Ad>().Query(tracking: true)
            .FirstOrDefaultAsync(a => a.Id == id) ?? throw new NotFoundException("İlan bulunamadı");
        if (ad.UserId != userId) throw new ForbiddenException("Bu ilan size ait değil");
        _uow.Repository<Ad>().SoftRemove(ad);
        await _uow.SaveChangesAsync();
    }

    public Task<int?> TrackPhoneAsync(Guid id) => _queries.IncrementPhoneClickAsync(id);
    // ... extend, favorite, update (NestJS ads.service.ts mantığını izler)
}
```

DI kaydı (`AddApplication` extension):
```csharp
public static IServiceCollection AddApplication(this IServiceCollection s)
{
    s.AddScoped<IAdsService, AdsService>();
    s.AddScoped<IAuthService, AuthService>();
    // ... tüm servisler
    s.AddAutoMapper(typeof(AdProfile).Assembly);
    s.AddValidatorsFromAssembly(typeof(CreateAdDtoValidator).Assembly);
    return s;
}
```

---

## 11. Cross-Cutting Altyapı

### 11.1 Response Envelope (TransformInterceptor karşılığı)

NestJS her başarılı yanıtı `{ success, data, meta }` ile sarıyordu. Aynı kontratı bir **Result Filter** veya basit bir wrapper ile koruyoruz (Flutter'ın değişmemesi için **birebir aynı**).

```csharp
// KadirliApp.Api/Filters/ApiResponseFilter.cs
public class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext ctx, ResultExecutionDelegate next)
    {
        if (ctx.Result is ObjectResult obj && obj.Value is not ProblemDetails)
        {
            obj.Value = new {
                success = true,
                data = obj.Value,
                meta = new { timestamp = DateTime.UtcNow.ToString("o"),
                             path = ctx.HttpContext.Request.Path.Value }
            };
        }
        await next();
    }
}
```

### 11.2 Exception Middleware (HttpExceptionFilter karşılığı)

```csharp
// KadirliApp.Api/Middleware/ExceptionMiddleware.cs
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
        => (_next, _log) = (next, log);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            var (status, code, message) = ex switch
            {
                NotFoundException     => (404, "NOT_FOUND",        ex.Message),
                ForbiddenException    => (403, "FORBIDDEN",        ex.Message),
                ConflictException     => (409, "CONFLICT",         ex.Message),
                ValidationException   => (400, "VALIDATION_ERROR", ex.Message),
                UnauthorizedException => (401, "UNAUTHORIZED",     ex.Message),
                _ => (500, "INTERNAL_ERROR", "Bir hata oluştu")
            };
            if (status >= 500) _log.LogError(ex, "{Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new {
                success = false,
                error = new { code, message },
                meta = new { timestamp = DateTime.UtcNow.ToString("o"), path = ctx.Request.Path.Value }
            });
        }
    }
}
```

Hata sınıfları (Application/Common/Exceptions):
```csharp
public class AppException(string m) : Exception(m);
public class NotFoundException(string m) : AppException(m);
public class ForbiddenException(string m) : AppException(m);
public class ConflictException(string m) : AppException(m);
public class UnauthorizedException(string m) : AppException(m);
```

### 11.3 Pagination modeli

```csharp
public record PaginationParams(int Page = 1, int Limit = 20)
{
    public int Offset => (Page - 1) * Limit;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public PagedMeta Meta { get; }
    public PagedResult(IReadOnlyList<T> items, long total, int page, int limit)
    {
        Items = items;
        var totalPages = (int)Math.Ceiling(total / (double)limit);
        Meta = new(page, limit, total, totalPages, page < totalPages, page > 1);
    }
}
public record PagedMeta(int Page, int Limit, long Total, int TotalPages, bool HasNext, bool HasPrev);
```

### 11.4 ValidationFilter (otomatik 400)

```csharp
builder.Services.AddFluentValidationAutoValidation();   // SharpGrip.FluentValidation.AutoValidation.Mvc
// veya ApiController otomatik ModelState; FluentValidation entegrasyonu ile birleştir.
```

---

## 12. Kimlik Doğrulama & Yetkilendirme

Mevcut akış: **OTP (telefon) → Redis → JWT (access 30g / refresh 90g)** kullanıcılar için; **email + bcrypt** panel girişi için; **role + admin_permissions** yetki.

### 12.1 JWT yapılandırması

```csharp
// KadirliApp.Api/Extensions/AuthExtensions.cs
public static IServiceCollection AddJwtAuth(this IServiceCollection s, IConfiguration cfg)
{
    var jwt = cfg.GetSection("Jwt");
    s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
     .AddJwtBearer(o =>
     {
         o.TokenValidationParameters = new TokenValidationParameters
         {
             ValidateIssuer = true,  ValidIssuer = jwt["Issuer"],
             ValidateAudience = true, ValidAudience = jwt["Audience"],
             ValidateLifetime = true,
             ValidateIssuerSigningKey = true,
             IssuerSigningKey = new SymmetricSecurityKey(
                 Encoding.UTF8.GetBytes(jwt["AccessSecret"]!)),
             ClockSkew = TimeSpan.Zero
         };
     });
    s.AddAuthorization(o =>
    {
        // Roller (NestJS @Roles karşılığı)
        o.AddPolicy("AdminPanel", p => p.RequireRole("admin", "super_admin", "moderator"));
        o.AddPolicy("SuperAdmin", p => p.RequireRole("super_admin"));
        // Permission policy'leri (NestJS PermissionGuard) — Bölüm 12.5
    });
    return s;
}
```

JWT claim'leri NestJS payload'ı (`{ user_id, role, phone }`) ile birebir:
```csharp
public class JwtService : IJwtService
{
    public (string access, string refresh, int expiresIn) GenerateTokens(User u)
    {
        var claims = new[] {
            new Claim("user_id", u.Id.ToString()),
            new Claim(ClaimTypes.Role, u.Role.ToString().ToLowerSnake()), // 'super_admin'
            new Claim("phone", u.Phone)
        };
        var access  = BuildToken(claims, _accessSecret, TimeSpan.FromDays(30));
        var refresh = BuildToken(claims, _refreshSecret, TimeSpan.FromDays(90));
        return (access, refresh, 2592000);
    }
}
```

### 12.2 OTP servisi (Redis — auth.service.ts birebir)

```csharp
public class OtpService : IOtpService
{
    private readonly IRedisService _redis;
    private readonly ISmsService _sms;
    private readonly IConfiguration _cfg;

    public async Task<OtpResult> RequestOtpAsync(string phone)
    {
        var devMode = _cfg.GetValue<bool>("Otp:DevMode");
        if (await _redis.ExistsAsync($"otp:block:{phone}"))
            throw new AppException("Çok fazla deneme. Lütfen bekleyin.");

        var count = await _redis.IncrAsync($"otp:rate:{phone}");
        if (count == 1) await _redis.ExpireAsync($"otp:rate:{phone}", 3600);
        if (count > _cfg.GetValue("Otp:RateLimitPerHour", 10))
            throw new AppException("Saatlik OTP limitine ulaştınız");

        var otp = devMode ? "123456" : Random.Shared.Next(100000, 999999).ToString();
        var ttl = _cfg.GetValue("Otp:TtlSeconds", 300);
        await _redis.SetAsync($"otp:code:{phone}", otp, TimeSpan.FromSeconds(ttl));
        if (!devMode) await _sms.SendAsync(phone, otp);
        return new(ttl, 60);
    }

    public async Task<bool> VerifyOtpAsync(string phone, string otp)
    {
        var stored = await _redis.GetAsync($"otp:code:{phone}")
            ?? throw new UnauthorizedException("OTP süresi dolmuş veya geçersiz");
        var attempts = await _redis.IncrAsync($"otp:attempts:{phone}");
        if (attempts == 1) await _redis.ExpireAsync($"otp:attempts:{phone}", 300);
        if (attempts > _cfg.GetValue("Otp:MaxAttempts", 3))
        {
            await _redis.SetAsync($"otp:block:{phone}", "1", TimeSpan.FromMinutes(5));
            await _redis.DelAsync($"otp:code:{phone}", $"otp:attempts:{phone}");
            throw new UnauthorizedException("Çok fazla hatalı deneme. 5 dakika bekleyin.");
        }
        if (!CryptographicOperations.FixedTimeEquals(   // timing-safe (crypto.timingSafeEqual karşılığı)
                Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(otp)))
            throw new UnauthorizedException("Geçersiz OTP");
        await _redis.DelAsync($"otp:code:{phone}", $"otp:attempts:{phone}");
        return true;
    }
}
```

### 12.3 Auth servisi akışı (verify-otp → temp_token / tam token, register, admin login, refresh)

`AuthService` (Application) yukarıdaki NestJS `auth.service.ts` mantığını birebir uygular:
- `VerifyOtp` → kullanıcı yoksa `temp_token` (registration claim, 30dk), varsa tam token.
- `Register` → temp_token doğrula, username uniq + mahalle kontrol, user oluştur, tam token.
- `AdminLogin` → email ile kullanıcı (password dahil özel sorgu), `BCrypt.Verify`, rol/ban kontrolü.
- `RefreshToken` → refresh secret ile doğrula, yeni access üret.

### 12.4 password `select:false` çözümü

EF'de "select false" yok. İki yöntem:
```csharp
// Yöntem A: Servis tarafında password'ü normalde Select etme; admin login'de özel sorgu:
var user = await _db.Users.IgnoreQueryFilters()
    .Where(u => u.Email == email)
    .Select(u => new { u.Id, u.Email, u.Role, u.Password, u.IsActive, u.IsBanned })
    .FirstOrDefaultAsync();
// Yöntem B: Password'ü ayrı bir tabloya/owned'a taşı. Biz A'yı kullanıyoruz (mevcut şema uyumu).
```

### 12.5 PermissionGuard → Authorization Handler

NestJS `PermissionGuard`: super_admin/admin bypass, moderator için `admin_permissions` tablosundan `can_<action>` kontrolü.

```csharp
// Permission attribute (NestJS @Permission(module, action) karşılığı)
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string module, string action)
        => Policy = $"perm:{module}:{action}";
}

// Dinamik policy provider + handler
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _perm;
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, PermissionRequirement req)
    {
        var role = ctx.User.FindFirstValue(ClaimTypes.Role);
        if (role is "super_admin" or "admin") { ctx.Succeed(req); return; }
        if (role == "moderator")
        {
            var userId = Guid.Parse(ctx.User.FindFirstValue("user_id")!);
            if (await _perm.HasAsync(userId, req.Module, req.Action)) ctx.Succeed(req);
        }
    }
}
```

Kullanım (controller):
```csharp
[RequirePermission("ads", "approve")]
[HttpPost("ads/{id}/approve")]
public Task<IActionResult> Approve(Guid id) => ...
```

---

## 13. Web API Controller Katmanı

NestJS controller'larının birebir karşılığı. Endpoint yolları **aynen korunur** (Flutter değişmesin). Global prefix `v1`.

```csharp
// Program.cs (Api)
app.UsePathBase("/v1");   // veya route prefix convention
```

### 13.1 Örnek controller (AdsController — ads.controller.ts birebir)

```csharp
[ApiController]
[Route("/")]                       // yollar metotlarda tam veriliyor (NestJS @Controller() boş)
public class AdsController : ControllerBase
{
    private readonly IAdsService _ads;
    public AdsController(IAdsService ads) => _ads = ads;

    private Guid UserId => Guid.Parse(User.FindFirstValue("user_id")!);

    // ── PUBLIC ──
    [HttpGet("ads")]
    public Task<PagedResult<AdListItem>> FindAll([FromQuery] QueryAdDto dto) => _ads.FindAllAsync(dto);

    [HttpGet("ads/categories")]
    public async Task<object> Categories([FromQuery(Name="parent_id")] Guid? parentId)
        => new { categories = await _ads.FindCategoriesAsync(parentId) };

    [HttpGet("ads/categories/{id:guid}/properties")]
    public Task<object> Props(Guid id) => _ads.FindCategoryPropertiesAsync(id);

    [HttpGet("ads/{id:guid}")]
    public async Task<object> FindOne(Guid id) => new { ad = await _ads.FindOneAsync(id) };

    [HttpPost("ads/{id:guid}/track-phone")]
    public Task<int?> TrackPhone(Guid id) => _ads.TrackPhoneAsync(id);

    [HttpPost("ads/{id:guid}/track-whatsapp")]
    public Task<int?> TrackWhatsapp(Guid id) => _ads.TrackWhatsappAsync(id);

    // ── AUTHENTICATED ──
    [Authorize, HttpPost("ads")]
    public Task<AdResponseDto> Create([FromBody] CreateAdDto dto) => _ads.CreateAsync(UserId, dto);

    [Authorize, HttpPatch("ads/{id:guid}")]
    public async Task<object> Update(Guid id, [FromBody] UpdateAdDto dto)
        => new { ad = await _ads.UpdateAsync(id, UserId, dto) };

    [Authorize, HttpDelete("ads/{id:guid}")]
    public Task Remove(Guid id) => _ads.RemoveAsync(id, UserId);

    [Authorize, HttpPost("ads/{id:guid}/extend")]
    public Task<object> Extend(Guid id, [FromBody] ExtendAdDto dto) => _ads.ExtendAsync(id, UserId, dto);

    [Authorize, HttpPost("ads/{id:guid}/favorite")]
    public Task AddFav(Guid id) => _ads.AddFavoriteAsync(id, UserId);

    [Authorize, HttpDelete("ads/{id:guid}/favorite")]
    public Task RemoveFav(Guid id) => _ads.RemoveFavoriteAsync(id, UserId);

    // ── USER-SCOPED ──
    [Authorize, HttpGet("users/me/ads")]
    public Task<PagedResult<AdListItem>> MyAds([FromQuery] QueryMyAdsDto dto) => _ads.FindMyAdsAsync(UserId, dto);

    [Authorize, HttpGet("users/me/favorites")]
    public Task<object> MyFavs() => _ads.FindMyFavoritesAsync(UserId);
}
```

### 13.2 Tam endpoint haritası (mevcut sistemle birebir)

> Hepsi `/v1` prefix'i altında. Aşağıdaki liste mevcut NestJS `*.controller.ts` dosyalarından çıkarıldı; .NET'te aynı yollar.

**Auth** (`/auth`): `POST admin/login`, `POST request-otp`, `POST verify-otp`, `POST register`, `POST refresh`, `POST logout`
**Users** (`/users`): `GET me`, `PATCH me`, `PATCH me/notifications`
**Files** (`/files`): `POST upload`, `DELETE :id`
**Announcements** (`/announcements`): `GET types`, `GET`, `GET :id`, `POST`, `PATCH :id`, `DELETE :id`, `POST :id/send`
**Ads** (yukarıda tam) — public + auth + me-scoped
**Deaths** (`/deaths`): `GET cemeteries`, `GET mosques`, `GET admin`, `POST :id/approve`, `POST :id/reject`, `DELETE :id`, `GET`, `GET :id`
**Pharmacy** (`/pharmacy`): `GET current`, `GET schedule`, `GET list`
**Events** (`/events`): `GET categories`, `GET`, `GET :id`
**Campaigns** (`/campaigns`): `GET`, `GET :id`, `POST :id/view-code`
**Guide** (`/guide`): `GET categories`, `GET`
**Places** (`/places`): `GET`, `GET :id`
**Transport** (`/transport`): `GET intercity`, `GET intracity`
**Taxi** (`/taxi`): `GET drivers`, `POST drivers/:id/call`
**Notifications** (`/notifications`): `GET`, `POST read-all`, `POST fcm-token`, `PATCH :id/read`

**Admin (panel API'si — `/admin/*`)** — bunlar hem MVC panel'in çağırabileceği hem de mevcut yapıdaki admin endpoint'leri:
- `/admin/dashboard`, `/admin/dashboard/module-usage`, `/admin/dashboard/activities`
- `/admin/approvals`, `/admin/ads` (+approve/reject/delete)
- `/admin/neighborhoods` (CRUD)
- `/admin/profile`, `/admin/change-password`
- `/admin/staff` (CRUD + permissions + reset-password)
- `/admin/deaths` (+cemeteries/mosques/neighborhoods CRUD)
- `/admin/campaigns` (+businesses, +business categories)
- `/admin/places` (+categories, +images reorder/set-cover)
- `/admin/guide` (categories + items)
- `/admin/taxi` (CRUD)
- `/admin/transport` (intercity/intracity routes + schedules/stops)
- `/admin/events` (+categories)
- `/admin/pharmacy` (+schedule)
- `/admin/users` (ban/unban/role)
- `/admin/complaints` (review/resolve/reject/priority)

> NestJS'te admin modülü 11 alt-controller'a bölünmüştü (`*-admin.controller.ts`). .NET'te `Controllers/Admin/` altında aynı bölünme: `AdsAdminController`, `DeathsAdminController`, ... Hepsi `[Authorize(Policy="AdminPanel")]` + ilgili `[RequirePermission]`.

---

## 14. ASP.NET Core MVC Panel

Bu, senin "controller / model / view arası bağlantı" istediğin kısım. Mevcut Next.js panelinin **16 modülü** birebir Razor MVC olarak.

### 14.1 Panel'in veriye erişimi

Bölüm 2'de kararlaştırdığımız gibi: **MVC Controller → Application Service (DI)** doğrudan. Yani panel, HTTP API'yi değil, `IAdsService` gibi servisleri direkt çağırır. Tek deploy, tek bağlantı havuzu, en hızlı.

```
Tarayıcı → MVC Controller → IAdsService (Application) → EF/Dapper → PostgreSQL
                ↓
            ViewModel → Razor View (.cshtml) → HTML
```

### 14.2 Panel kimlik doğrulama (cookie tabanlı)

Mevcut Next.js panel JWT'yi cookie'de tutuyordu. MVC panelde **cookie authentication** kullanırız (form login → cookie). API ise JWT bearer. İki şema bir arada:

```csharp
// KadirliApp.Web/Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => {
        o.LoginPath = "/account/login";
        o.AccessDeniedPath = "/account/denied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(o => {
    o.AddPolicy("AdminPanel", p => p.RequireRole("admin","super_admin","moderator"));
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
```

Login controller (form post → AdminLogin servisi → cookie):
```csharp
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet] public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var result = await _auth.AdminLoginAsync(vm.Email, vm.Password);
            var claims = new List<Claim> {
                new("user_id", result.User.Id.ToString()),
                new(ClaimTypes.Role, result.User.Role),
                new(ClaimTypes.Name, result.User.Username ?? result.User.Email!)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View(vm); }
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }
}
```

### 14.3 ViewModel (Model katmanı)

```csharp
// KadirliApp.Web/Models/Ads/AdListViewModel.cs
public class AdListViewModel
{
    public IReadOnlyList<AdListItem> Items { get; set; } = [];
    public PagedMeta Meta { get; set; } = default!;
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Status { get; set; }
}
```

### 14.4 MVC Controller (panel)

```csharp
// KadirliApp.Web/Controllers/AdsAdminController.cs
[Authorize(Policy = "AdminPanel")]
[Route("ads")]
public class AdsAdminController : Controller
{
    private readonly IAdsService _ads;
    private readonly IAdminAdsService _adminAds;   // onay/red/sil
    public AdsAdminController(IAdsService ads, IAdminAdsService adminAds)
        => (_ads, _adminAds) = (ads, adminAds);

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, Guid? categoryId, string? status, int page = 1)
    {
        var result = await _adminAds.FindAllAsync(new(search, categoryId, status, page, 20));
        return View(new AdListViewModel {
            Items = result.Items, Meta = result.Meta,
            Search = search, CategoryId = categoryId, Status = status });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
        => View(await _ads.FindOneAsync(id));

    [HttpPost("{id:guid}/approve")]
    [RequirePermission("ads", "approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _adminAds.ApproveAsync(id, CurrentUserId);
        TempData["ok"] = "İlan onaylandı";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission("ads", "approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        await _adminAds.RejectAsync(id, CurrentUserId, reason);
        TempData["ok"] = "İlan reddedildi";
        return RedirectToAction(nameof(Index));
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue("user_id")!);
}
```

### 14.5 View (.cshtml) — Controller/Model/View bağlantısının somut hali

```cshtml
@* KadirliApp.Web/Views/AdsAdmin/Index.cshtml *@
@model AdListViewModel
@{
    ViewData["Title"] = "İlan Yönetimi";
}

<div class="d-flex justify-content-between align-items-center mb-3">
    <h1>İlanlar</h1>
    <form method="get" class="d-flex gap-2">
        <input name="search" value="@Model.Search" class="form-control" placeholder="Ara..." />
        <select name="status" class="form-select">
            <option value="">Tüm durumlar</option>
            <option value="pending"  selected="@(Model.Status=="pending")">Bekleyen</option>
            <option value="approved" selected="@(Model.Status=="approved")">Onaylı</option>
            <option value="rejected" selected="@(Model.Status=="rejected")">Reddedilen</option>
        </select>
        <button class="btn btn-primary">Filtrele</button>
    </form>
</div>

@if (TempData["ok"] is string ok) { <div class="alert alert-success">@ok</div> }

<table class="table table-hover">
    <thead><tr><th>Başlık</th><th>Fiyat</th><th>Durum</th><th>Görüntülenme</th><th>Tarih</th><th></th></tr></thead>
    <tbody>
    @foreach (var ad in Model.Items)
    {
        <tr>
            <td><a asp-action="Details" asp-route-id="@ad.Id">@ad.Title</a></td>
            <td>@(ad.Price?.ToString("N0") ?? "—") ₺</td>
            <td><span class="badge bg-@StatusColor(ad.Status)">@ad.Status</span></td>
            <td>@ad.ViewCount</td>
            <td>@ad.CreatedAt.ToString("dd.MM.yyyy")</td>
            <td>
                @if (ad.Status == "pending")
                {
                    <form asp-action="Approve" asp-route-id="@ad.Id" method="post" class="d-inline">
                        @Html.AntiForgeryToken()
                        <button class="btn btn-sm btn-success">Onayla</button>
                    </form>
                }
            </td>
        </tr>
    }
    </tbody>
</table>

<partial name="_Pagination" model="Model.Meta" />

@functions {
    string StatusColor(string s) => s switch {
        "approved" => "success", "pending" => "warning", "rejected" => "danger", _ => "secondary" };
}
```

Ortak layout:
```cshtml
@* Views/Shared/_Layout.cshtml *@
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="utf-8" />
    <title>@ViewData["Title"] - KadirliApp Yönetim</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" />
</head>
<body>
    <div class="d-flex">
        <partial name="_Sidebar" />     @* 16 modül menüsü *@
        <main class="flex-grow-1 p-4">
            <partial name="_Topbar" />
            @RenderBody()
        </main>
    </div>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Sidebar (16 modül — Next.js sidebar.tsx karşılığı):
```cshtml
@* Views/Shared/_Sidebar.cshtml *@
<nav class="sidebar bg-dark text-white p-3" style="width:240px;min-height:100vh">
    <h4 class="mb-4">KadirliApp</h4>
    <ul class="nav flex-column">
        <li><a class="nav-link text-white" asp-controller="Dashboard"      asp-action="Index">Dashboard</a></li>
        <li><a class="nav-link text-white" asp-controller="AnnouncementsAdmin" asp-action="Index">Duyurular</a></li>
        <li><a class="nav-link text-white" asp-controller="AdsAdmin"        asp-action="Index">İlanlar</a></li>
        <li><a class="nav-link text-white" asp-controller="DeathsAdmin"     asp-action="Index">Vefatlar</a></li>
        <li><a class="nav-link text-white" asp-controller="CampaignsAdmin"  asp-action="Index">Kampanyalar</a></li>
        <li><a class="nav-link text-white" asp-controller="UsersAdmin"      asp-action="Index">Kullanıcılar</a></li>
        <li><a class="nav-link text-white" asp-controller="PharmacyAdmin"   asp-action="Index">Eczaneler</a></li>
        <li><a class="nav-link text-white" asp-controller="TransportAdmin"  asp-action="Index">Ulaşım</a></li>
        <li><a class="nav-link text-white" asp-controller="NeighborhoodsAdmin" asp-action="Index">Mahalleler</a></li>
        <li><a class="nav-link text-white" asp-controller="TaxiAdmin"       asp-action="Index">Taksi</a></li>
        <li><a class="nav-link text-white" asp-controller="EventsAdmin"     asp-action="Index">Etkinlikler</a></li>
        <li><a class="nav-link text-white" asp-controller="GuideAdmin"      asp-action="Index">Rehber</a></li>
        <li><a class="nav-link text-white" asp-controller="PlacesAdmin"     asp-action="Index">Gezilecek Yerler</a></li>
        <li><a class="nav-link text-white" asp-controller="ComplaintsAdmin" asp-action="Index">Şikayetler</a></li>
        <li><a class="nav-link text-white" asp-controller="StaffAdmin"      asp-action="Index">Personel</a></li>
        <li><a class="nav-link text-white" asp-controller="Settings"        asp-action="Index">Ayarlar</a></li>
    </ul>
</nav>
```

### 14.6 Form (create/update) view + POST bağlantısı

```cshtml
@* Views/PharmacyAdmin/Form.cshtml *@
@model PharmacyFormViewModel
<form asp-action="@(Model.Id == null ? "Create" : "Update")"
      asp-route-id="@Model.Id" method="post">
    @Html.AntiForgeryToken()
    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
    <div class="mb-3">
        <label asp-for="Name" class="form-label">Eczane Adı</label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    <div class="mb-3">
        <label asp-for="Address" class="form-label">Adres</label>
        <textarea asp-for="Address" class="form-control"></textarea>
    </div>
    <div class="mb-3">
        <label asp-for="Phone" class="form-label">Telefon</label>
        <input asp-for="Phone" class="form-control" />
    </div>
    <button type="submit" class="btn btn-primary">Kaydet</button>
</form>
```

ViewModel'de DataAnnotations ile validation (FluentValidation API tarafında, panel formlarında DataAnnotations + client-side):
```csharp
public class PharmacyFormViewModel
{
    public Guid? Id { get; set; }
    [Required(ErrorMessage="Ad zorunlu"), StringLength(100)]
    public string Name { get; set; } = "";
    [Required] public string Address { get; set; } = "";
    [Phone] public string? Phone { get; set; }
}
```

> **Özet — Controller/Model/View bağlantısı:**
> 1. **Controller** action'ı çağrılır (route + auth policy).
> 2. Controller, **Application servisini** çağırır → veri gelir.
> 3. Controller, veriyi **ViewModel**'e koyar, `return View(model)`.
> 4. **Razor View** `@model` ile tip-güvenli erişir, HTML üretir.
> 5. Form POST → `asp-action` ile action'a döner, `[ValidateAntiForgeryToken]` + ModelState.
> 6. Başarı → `TempData` + `RedirectToAction` (PRG pattern).

---

## 15. Caching, Background Jobs, File Upload

### 15.1 Redis cache
- OTP (yukarıda).
- Sık okunan, az değişen veriler: nöbetçi eczane (`fn_current_duty_pharmacies` sonucu 1 saat), kategori ağaçları, mahalle listesi.
```csharp
public async Task<List<DutyPharmacy>> GetCurrentDutyAsync()
{
    const string key = "pharmacy:duty:today";
    var cached = await _redis.GetAsync<List<DutyPharmacy>>(key);
    if (cached != null) return cached;
    var list = await _pharmacyQueries.GetDutyAsync(DateTime.Today);
    await _redis.SetAsync(key, list, TimeSpan.FromHours(1));
    return list;
}
```

### 15.2 Hangfire (NestJS @nestjs/schedule + Bull karşılığı)

```csharp
// Program.cs
builder.Services.AddHangfire(c => c.UsePostgreSqlStorage(connString));
builder.Services.AddHangfireServer();
// ...
app.UseHangfireDashboard("/hangfire", new() { Authorization = [new AdminDashboardAuthFilter()] });

// Recurring jobs (cron)
RecurringJob.AddOrUpdate<ExpireAdsJob>("expire-ads", j => j.RunAsync(), Cron.Hourly);
RecurringJob.AddOrUpdate<ArchiveDeathsJob>("archive-deaths", j => j.RunAsync(), Cron.Daily);
RecurringJob.AddOrUpdate<SendScheduledAnnouncements>("scheduled-ann", j => j.RunAsync(), "*/5 * * * *");
```

```csharp
public class ArchiveDeathsJob
{
    private readonly IDapperContext _ctx;
    public async Task RunAsync()
    {
        using var db = _ctx.CreateConnection();
        await db.ExecuteAsync("CALL sp_archive_expired_deaths()");   // SP
    }
}
```

FCM push (firebase-admin karşılığı): `FirebaseAdmin` NuGet → `FirebaseMessaging.DefaultInstance.SendAsync(...)`, background job ile.

### 15.3 File upload (files.controller.ts + Multer karşılığı)

```csharp
[Authorize, HttpPost("files/upload")]
public async Task<object> Upload(IFormFile file, [FromForm] string? moduleType)
{
    var saved = await _fileService.UploadAsync(file, moduleType, UserId);
    return new { file = saved };
}
```
`IFileStorage`: lokal (`wwwroot/uploads` veya ayrı `uploads/`) ya da S3/MinIO. Mevcut sistem lokal `uploads/` + static serving kullanıyordu; aynısı:
```csharp
app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});
```

---

## 16. Modül Eşleme Tablosu

| NestJS Modül | .NET Application Servis | API Controller | Panel MVC Controller | Ana Tablolar |
|---|---|---|---|---|
| auth | `AuthService`, `OtpService`, `JwtService` | `AuthController` | `AccountController` | users |
| users | `UsersService` | `UsersController` | `UsersAdminController` | users, user_neighborhoods |
| files | `FileService` | `FilesController` | — | files |
| announcements | `AnnouncementsService` | `AnnouncementsController` | `AnnouncementsAdminController` | announcements, announcement_types, announcement_views, power_outages |
| ads | `AdsService` | `AdsController` | `AdsAdminController` | ads, ad_categories, category_properties, property_options, ad_images, ad_property_values, ad_favorites, ad_extensions |
| deaths | `DeathsService` | `DeathsController` | `DeathsAdminController` | death_notices, cemeteries, mosques |
| pharmacy | `PharmacyService` | `PharmacyController` | `PharmacyAdminController` | pharmacies, pharmacy_schedules |
| events | `EventsService` | `EventsController` | `EventsAdminController` | events, event_categories, event_images |
| campaigns | `CampaignsService` | `CampaignsController` | `CampaignsAdminController` | campaigns, businesses, business_categories, campaign_images, campaign_code_views |
| guide | `GuideService` | `GuideController` | `GuideAdminController` | guide_categories, guide_items |
| places | `PlacesService` | `PlacesController` | `PlacesAdminController` | places, place_categories, place_images |
| transport | `TransportService` | `TransportController` | `TransportAdminController` | intercity_routes, intercity_schedules, intracity_routes, intracity_stops |
| taxi | `TaxiService` | `TaxiController` | `TaxiAdminController` | taxi_drivers, taxi_calls |
| notifications | `NotificationsService`, `FcmService` | `NotificationsController` | — | notifications |
| admin (11 alt) | `Admin*Service` (dashboard, staff, complaints, neighborhoods...) | `Admin/*Controller` | (panel'in tamamı) | audit_logs, complaints, admin_permissions, permissions, role_permissions, neighborhoods |

> NestJS'te "neighborhoods" ayrı modül değildi (`admin.service.ts` içindeydi) — .NET'te `NeighborhoodsService` + `NeighborhoodsAdminController` olarak ayırmak daha temiz; istersen admin servisi içinde de tutabilirsin.

---

## 17. Performans Reçetesi

"İndexlerde procedure'ler ile hızlı çalışan yapı" hedefinin operasyonel kontrol listesi:

1. **Her FK'ya index** (Bölüm 3.2). PG otomatik koymaz.
2. **Composite + partial index** sık sorgu desenlerine göre (`(status, created_at DESC) WHERE deleted_at IS NULL`).
3. **Ağır listeleri Dapper + SQL function** ile çek; `count(*) OVER()` ile tek sorguda toplam + sayfa.
4. **Atomik sayaçlar** (view/click) function ile (`UPDATE ... RETURNING`), EF round-trip yerine.
5. **`AsNoTracking()`** tüm okuma sorgularında (zaten generic repo varsayılanı).
6. **Projeksiyon** (`.Select(new Dto{...})`) — gereksiz kolon/entity çekme.
7. **N+1'den kaçın:** `Include`/`ThenInclude` veya tek SQL JOIN (Dapper multi-mapping).
8. **Connection pooling:** Npgsql `Maximum Pool Size=100` + `AddDbContextPool` (DbContext havuzu).
   ```csharp
   builder.Services.AddDbContextPool<AppDbContext>(opt => opt.UseNpgsql(conn));
   ```
9. **Redis cache** yavaş-değişen veriler için (eczane nöbeti, kategoriler, mahalleler).
10. **Hangfire job'ları** ağır toplu işleri kullanıcı isteğinden ayırır (expire, archive, FCM gönderimi).
11. **EXPLAIN ANALYZE** ile her kritik sorguyu doğrula; `pg_stat_statements` extension'ı ile yavaş sorgu avı.
12. **Pagination zorunlu** — limitsiz liste dönme. Maksimum `limit` (örn. 100) clamp et.
13. **`gin_trgm_ops`** ile arama; `ILIKE '%x%'` yerine gerekirse `to_tsvector` full-text.
14. **Response compression** (`app.UseResponseCompression()`), **output caching** public GET'lerde.

---

## 18. Tam Dikey Kesit — Ads Modülü

Bir modülün uçtan uca tüm dosyaları (şablon — diğer 14 modül bunu izler):

```
Domain/Entities/        Ad.cs, AdCategory.cs, CategoryProperty.cs, PropertyOption.cs,
                        AdImage.cs, AdPropertyValue.cs, AdFavorite.cs, AdExtension.cs
Domain/                 (status string sabitleri veya AdStatus enum)

Application/Features/Ads/
  Dtos/                 CreateAdDto, UpdateAdDto, QueryAdDto, QueryMyAdsDto, ExtendAdDto,
                        AdResponseDto, AdListItem (Dapper projeksiyonu)
  Validators/           CreateAdDtoValidator, UpdateAdDtoValidator
  IAdsService.cs / AdsService.cs
  IAdminAdsService.cs / AdminAdsService.cs   (onay/red/sil — admin)
Application/Common/Interfaces/ IAdQueries.cs
Application/Mapping/    AdProfile.cs

Infrastructure/Persistence/Configurations/  AdConfiguration, AdCategoryConfiguration, ...
Infrastructure/Persistence/Dapper/          AdQueries.cs (IAdQueries impl)
Infrastructure/Persistence/Migrations/      <SP+index migration'ları>

Api/Controllers/        AdsController.cs          (public + auth + me)
Api/Controllers/Admin/  AdsAdminController.cs     (JSON admin endpoint'leri)

Web/Controllers/        AdsAdminController.cs      (MVC panel)
Web/Models/Ads/         AdListViewModel, AdDetailViewModel
Web/Views/AdsAdmin/     Index.cshtml, Details.cshtml
```

**Akış (ilan oluşturma, uçtan uca):**
1. Flutter `POST /v1/ads` (JWT) → `AdsController.Create` → `AdsService.CreateAsync`.
2. `CreateAdDtoValidator` 400 kontrolü (otomatik).
3. `AdsService`: EF transaction → `ads` + `ad_images` + `ad_property_values` insert (status=`pending`, expires_at=+30g).
4. `TransformInterceptor`/filter → `{success,data,meta}` zarfı.
5. Panel `AdsAdminController.Index` → bekleyenler listesi (Dapper `fn_search_ads`).
6. Admin "Onayla" → `POST /ads/{id}/approve` (cookie+permission) → `AdminAdsService.ApproveAsync` → `CALL sp_approve_ad` (transaction + audit_log).
7. Hangfire job FCM bildirimi gönderir → `notifications` satırı + push.
8. `ExpireAdsJob` saatlik `expires_at < now()` olanları `expired` yapar.

---

## 19. Konfigürasyon, Docker, CI/CD

### 19.1 appsettings yapısı
```json
{
  "ConnectionStrings": { "Postgres": "...", "Redis": "..." },
  "Jwt": {
    "Issuer": "kadirliapp", "Audience": "kadirliapp",
    "AccessSecret": "<min-32-char>", "RefreshSecret": "<min-32-char>",
    "AccessExpiresDays": 30, "RefreshExpiresDays": 90
  },
  "Otp": { "DevMode": true, "TtlSeconds": 300, "MaxAttempts": 3, "RateLimitPerHour": 10 },
  "Cors": { "Origins": [ "http://localhost:5173", "https://panel.kadirli.app" ] },
  "Files": { "Storage": "Local", "BasePath": "uploads", "PublicBaseUrl": "/uploads" }
}
```
`IOptions<JwtOptions>`, `IOptions<OtpOptions>` ile tip-güvenli bağla; başlangıçta validate et (`ValidateOnStart`).

### 19.2 Docker
```dockerfile
# KadirliApp.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish KadirliApp.Api/KadirliApp.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "KadirliApp.Api.dll"]
```
`docker-compose.yml`: `postgres:15`, `redis:7`, `api`, `web` servisleri (mevcut compose'un .NET versiyonu).

### 19.3 Program.cs (Api) — bağlama
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddControllers(o => {
    o.Filters.Add<ApiResponseWrapperFilter>();
});
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();      // NestJS'te yoktu, biz ekliyoruz
builder.Services.AddHangfire(...);
builder.Services.AddResponseCompression();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();   // EN ÜSTTE
app.UsePathBase("/v1");
app.UseResponseCompression();
app.UseStaticFiles(/* uploads */);
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseHangfireDashboard("/hangfire");
// recurring jobs kaydı
app.Run();
```

### 19.4 CI/CD (GitHub Actions — mevcut backend-tests.yml karşılığı)
```yaml
jobs:
  build-test:
    runs-on: ubuntu-latest
    services:
      postgres: { image: postgres:15, env: { POSTGRES_PASSWORD: postgres }, ports: ["5432:5432"] }
      redis:    { image: redis:7, ports: ["6379:6379"] }
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet ef database update -p KadirliApp.Infrastructure -s KadirliApp.Api
      - run: dotnet test --no-build -c Release --collect:"XPlat Code Coverage"
```

### 19.5 Test (xUnit — Jest karşılığı)
- **Unit:** Servisleri mock'lanmış `IUnitOfWork`/`IRepository`/`IAdQueries` ile test et (Moq/NSubstitute).
- **Integration:** `WebApplicationFactory` + **Testcontainers** (gerçek PostgreSQL/Redis container) — NestJS E2E karşılığı.
- Hedef: mevcut projedeki gibi %75 coverage.

---

## 20. İnşa Sırası — Yol Haritası

"İlmek ilmek" — bağımlılık sırasına göre, her adımda çalışır+test edilebilir bir dilim:

**Faz 0 — İskelet (1-2 gün)**
1. Solution + 5 proje + referanslar + NuGet'ler.
2. `appsettings`, Docker compose (postgres+redis), Program.cs minimal "hello".
3. `BaseEntity`, ortak exception'lar, `ApiResponse`, `PagedResult`.

**Faz 1 — Persistence çekirdeği (2-3 gün)**
4. Tüm entity'ler (Domain) — Bölüm 5 + 16'daki tablo listesi.
5. Tüm `IEntityTypeConfiguration` + `AppDbContext`.
6. İlk migration (`InitialSchema`) → DB ile birebir mevcut şema.
7. Index + extension migration (Bölüm 3.2/3.3).
8. SP/Function/View migration (Bölüm 4).
9. `Repository<T>`, `UnitOfWork`, `DapperContext`, `RedisService`.
10. `DbSeeder` (super_admin + test user — mevcut seed.ts karşılığı; **admin'e email atamayı unutma**, mevcut bug).

**Faz 2 — Kimlik (2-3 gün)**
11. `JwtService`, `OtpService`, `AuthService`.
12. `AuthController` (request-otp, verify-otp, register, admin/login, refresh, logout).
13. JWT bearer + cookie auth + role policy + `PermissionHandler`.
14. **Test:** OTP akışı + admin login + refresh.

**Faz 3 — Çekirdek public modüller (her biri ~1-2 gün)**
15. neighborhoods, users (me), files (upload).
16. announcements, ads (en kapsamlı — Bölüm 18 şablonu), deaths.
17. pharmacy, events, campaigns.
18. guide, places, transport, taxi, notifications.
   → Her modül: entity (zaten var) → DTO+validator → service → API controller → unit test.

**Faz 4 — Admin API (2-3 gün)**
19. `Admin/*Controller` + `Admin*Service` (dashboard, approvals, staff, complaints, her modülün CRUD'u).
20. Permission policy'leri controller'lara bağla.

**Faz 5 — MVC Panel (3-5 gün)**
21. `_Layout`, `_Sidebar`, `_Topbar`, login/logout (cookie).
22. Dashboard (KPI kartları — `vw_dashboard_counts`).
23. 16 modülün Index/Details/Form view'ları + MVC controller'ları (Bölüm 14 şablonu).

**Faz 6 — Arka plan & cila (2-3 gün)**
24. Hangfire jobs (expire ads, archive deaths, scheduled announcements, FCM).
25. Redis cache (eczane nöbeti, kategoriler).
26. SMS + FCM gerçek entegrasyonu.

**Faz 7 — Kalite & deploy (2-3 gün)**
27. Integration testler (Testcontainers).
28. CI/CD pipeline.
29. Docker prod compose, deployment.
30. **Mobil bağlantı testi:** Flutter base URL'i yeni .NET API'ye çevir → tek satır değişmeden çalışmalı (kontrat aynı).

---

## Ek — Mevcut Sistemden Taşınan Bilinen Notlar (dikkat!)

Bu detaylar mevcut KadirliApp'in MEMORY_BANK/README'sinden; .NET versiyonunda baştan doğru yap:

1. **Seed admin email bug'ı:** Mevcut seed admin'e email atamıyordu, panel girişi `email` ile arıyordu → admin giremiyordu. .NET `DbSeeder`'da super_admin'e email **ata**.
2. **Response zarfı kontratı:** `{success, data, meta}` ve hata `{success:false, error:{code,message}, meta}` — Flutter buna göre parse ediyor. **Birebir koru.**
3. **OTP dev mode:** dev'de sabit `123456`. `Otp:DevMode=true`.
4. **JWT payload alan adları:** `user_id`, `role`, `phone` (snake_case claim). Flutter/panel buna bağlı.
5. **Soft delete:** ads, announcements, deaths, events, campaigns, users, taxi_drivers, files → `deleted_at`. Global query filter ile gizle.
6. **Nullable user_id:** businesses ve taxi_drivers'da user_id sonradan nullable yapıldı (admin'in kullanıcı olmadan kayıt girebilmesi için). Aynısını yap.
7. **death_notices.neighborhood_id** ve **events.is_local** kolonları sonradan eklendi — initial şemana dahil et.
8. **plaka** alanı taxi_drivers'da (Türkçe terim korunmuş) — aynen `plaka`.
9. **full_name vs deceased_name:** death_notices'te alan `deceased_name`. Karıştırma.
10. **API prefix `/v1`** ve **CORS origin listesi** env'den geliyor — koru.

---

**Bu doküman bir yaşayan rehberdir.** Her modülü Bölüm 18'deki dikey-kesit şablonuyla aç, Bölüm 17'deki performans kontrol listesini her PR'da uygula, Bölüm 20'deki sırayı takip et. Mevcut NestJS kodu (özellikle `*.service.ts` iş kuralları) **referans gerçeğindir** — bir davranıştan emin değilsen oraya bak, .NET'e birebir çevir.
