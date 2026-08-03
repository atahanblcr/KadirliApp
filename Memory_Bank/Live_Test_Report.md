# Canlı Test Sonuçları (Live Test Execution)

> Son çalıştırma: **4 Ağustos 2026 — Faz 11.17** (panel: şehirlerarası ulaşım · denetim izi · çöp kutusu · kesinti filtresi; en altta). Önceki: 8 Temmuz 2026 — Auth route `/v1/auth` + yanlış OTP 400/INVALID_OTP + Otp:DevMode + Guide kategori delete testleri.

## Ortam
- `docker compose up -d` → kadirliapp_db (Postgres) + kadirliapp_redis ayakta.
- Web: `dotnet run --urls http://localhost:5002` (açılışta DbSeeder migration+seed çalıştı).
- Api: `dotnet run --urls http://localhost:5001`.

## Web Paneli Testleri (curl)

```
[1] POST /Account/Login  (username=admin, password=Admin123!)   → 302 ✅ (gerçek DB + BCrypt doğrulaması)
[2] GET  /Dashboard/Index                                        → 200 ✅
[3] GET  /Dashboard/Seed  (MockDataSeeder)                       → 200 ✅ "Örnek veriler başarıyla eklendi"
[4] Dashboard kartları (seed sonrası, DB'den gerçek sayılar):
      Toplam Kullanıcı: 5 | Aktif İlanlar: 3 | Bekleyen Onaylar: 7 | Toplam Duyuru: 3 ✅
      (7 = 2 ilan + 1 vefat + 1 etkinlik + 1 kampanya + 2 şikayet pending)
[5] 13 modül Index sayfası → hepsi 200 ✅
      AdsAdmin, AnnouncementsAdmin, UsersAdmin, DeathsAdmin, PharmaciesAdmin,
      PowerOutagesAdmin, TransportAdmin, TaxiAdmin, PlacesAdmin, GuideAdmin,
      EventsAdmin, CampaignsAdmin, ComplaintsAdmin
[6] GET  /TaxiAdmin/Create ve /TaxiAdmin/Edit?id=...             → 200 ✅ (Faz 6'da eksik kalan view'lar)
[7] POST /TaxiAdmin/Verify (doğrulanmamış İsmail Aydın)          → 302 ✅
      DB: is_verified=t, verified_by=203bf576-... (GERÇEK admin id) ✅
[8] POST /AdsAdmin/Approve (pending "Sahibinden Temiz Fiat Egea") → 302 ✅
      DB: status=approved, approved_by=203bf576-... (GERÇEK admin id) ✅
[9] Sidebar: 14 link render edildi (Etkinlikler, Kampanyalar, Taksiciler, Mekanlar, Şikayetler dahil) ✅
```

## API Testleri (curl)

> NOT (8 Tem 2026): Auth route'u `/v1/auth/*` olarak değiştirildi — aşağıdaki `/api/Auth/*` yolları tarihi kayıttır, artık 404 döner.

```
[1] POST /api/Auth/login  {"phone":"+905001112233"}
      → {"message":"OTP gönderildi","otp":"378670"} ✅ (Redis'e TTL'li yazıldı)
[2] POST /api/Auth/verify-otp  {"phone":"+905001112233","otp":"378670"}
      → {"token":"eyJhbGciOi..."} ✅ (gerçek JWT; user_id + role=user claim'leri)
[3] DB kontrolü: users tablosunda +905001112233 / role=User satırı oluştu ✅
```

## Sonuç
Faz 6 hedefi ("işlevsiz buton kalmasın") karşılandı: tüm butonlar gerçek command/query handler'lara gidiyor ve DB'ye yazıyor. Bilinen sınırlar için `Active_Context.md`'ye bakınız (SMS yok — OTP response'ta, dosya yükleme formlara bağlı değil, test projesi yok).

## Faz 7 (5 Temmuz 2026) UI İyileştirme Testleri

```
[1] dotnet build KadirliApp.sln → 0 Hata, Başarılı ✅
[2] Web ve Api projeleri (5002 ve 5001) çalıştırıldı → EF Core context migration ve startup başarılı ✅
[3] Web UI sayfaları kontrolü (Dashboard ve Controller sayfaları) → 200/302 Başarılı ✅
[4] Admin Panel mantıksal düzeltmelerin gözden geçirilmesi:
    - Duyurular (Yeni tür, mahalle seçimi, resim, konum vb.) alanları formlara yansıtılmış. ✅
    - Etkinlikler, Mekanlar, Kampanyalar ve Vefat formlarında opsiyonel alan düzeltmeleri ve harita entegrasyonu kodlanmış. ✅
```
Faz 7 eksiklerin tamamlanması olarak doğrulandı.

---

## Faz 8 — Admin API + Response Zarfı Smoke Testleri (7 Temmuz 2026)

## Ortam
- Docker: kadirliapp_db + kadirliapp_redis ayakta.
- Api: port 5005'teki eski process öldürüldü, yeni build ile `dotnet run --urls http://localhost:5005`.
- Build: `dotnet build KadirliApp.sln` → 0 hata, 0 uyarı.

## Response Zarfı Kontrat Testleri (curl)

```
[1] GET  /v1/announcements  (handler kendi ApiResponse'unu dönüyor)
      → {"success":true,"data":[...]} ✅ (çift sarma YOK)
[2] GET  /v1/ads?page=1&limit=1  (handler çıplak PagedResult dönüyor)
      → {"success":true,"data":{"items":[...]},"meta":{timestamp,path}} ✅ (filter sardı)
[3] GET  /v1/announcements/{olmayan-id}
      → {"success":false,"error":{"code":"NOT_FOUND","message":"Duyuru bulunamadı."}} ✅
[4] POST /v1/admin/users/{olmayan-id}/ban  (ExceptionMiddleware yolu)
      → HTTP 404 + {"success":false,"error":{"code":"NOT_FOUND",...},"meta":{...}} ✅
```

## Admin API Testleri (curl, super_admin JWT ile)

```
[1] GET /v1/admin/dashboard (token YOK)                          → 401 ✅
[2] OTP login akışı (+905000000001) → verify-otp → JWT alındı    ✅
[3] GET /v1/admin/dashboard
      → totalUsers:6, activeAds:4, pendingApprovals:3,
        pendingBreakdown:{ads:0,deaths:1,events:0,campaigns:1,complaints:1} ✅
[4] GET /v1/admin/dashboard/activities?limit=3                   → 200 ✅
[5] 15 liste endpoint'i → HEPSİ 200 + success:true ✅
      ads, announcements, deaths, events, campaigns, complaints, users,
      guide/categories, guide/items, pharmacies, places, power-outages,
      taxis, transport/intercity, transport/intracity
[6] POST /v1/admin/deaths/{id}/approve (pending "Emine Kaya")    → success:true ✅
      Dashboard tekrar: pendingBreakdown.deaths 1→0 ✅ (DB'ye yazıldığı doğrulandı)
[7] POST /v1/admin/users/{id}/ban  {"reason":"test"}             → success:true ✅
[8] POST /v1/admin/users/{id}/unban                              → success:true ✅
```

## Notlar
- Yanlış OTP ile verify-otp → 500 INTERNAL_ERROR döndü (bilinen eksik: 400 olmalı — Progress.md Faz 8/E). → **8 Tem'de düzeltildi**, en alttaki teste bakınız.
- Auth route'u hâlâ `/api/auth/*` (kontrat `/v1` istiyor — Progress.md Faz 8/E). → **8 Tem'de `/v1/auth` yapıldı**, en alttaki teste bakınız.

---

## Faz 8/F — Faz 7 Panel Özelliklerinin Admin API Doğrulaması (7 Temmuz 2026)

Tüm istekler super_admin JWT ile `v1/admin/*` endpoint'lerine atıldı; yazılan satırlar psql ile doğrulandı; FAZ7-TEST kayıtları test sonrası silindi.

```
[1] POST /v1/admin/announcements  (targetType=neighborhood + targetNeighborhoodIds +
    locationName/latitude/longitude)
      → success ✅  DB: target_neighborhoods=["f377..."], location_name="Kadirli Meydan",
        lat=37.3733, lng=36.0961 ✅
[2] POST /v1/admin/announcements/types (modal karşılığı)         → success ✅ (icon+color DB'de)
[3] POST /v1/admin/events  (latitude/longitude)                  → success ✅
      DB: created_by=203bf576-... (GERÇEK admin id — düzeltme sonrası) ✅
[4] GET  /v1/admin/events/calendar?year=2026&month=8             → 200, oluşturulan etkinlik listede ✅
[5] POST /v1/admin/campaigns                                     → success ✅
      DB: status=approved, approved_by=203bf576-..., approved_at dolu ✅
[6] POST /v1/admin/deaths  (mosqueId=null, neighborhoodId=null,
    condolenceAddress/Latitude/Longitude)                        → success ✅
      DB: mosque_id=NULL, neighborhood_id=NULL, condolence_lat/lng dolu ✅
[7] POST /v1/admin/places  (openingHours + amenities)
      - amenities="wc,wifi,klima" (düz metin)                    → 500 ❌ (kolon jsonb)
      - amenities="{\"WC\":true,\"Wi-Fi\":true,\"Klima\":true}"  → success ✅ (panelin formatı)
      DB: opening_hours="09:00-18:00", amenities jsonb dolu ✅
[8] POST /v1/files/upload (multipart PNG)
      - İLK DENEME → 500 ❌ (UploadFileCommandHandler Uri.LocalPath bug'ı — düzeltildi)
      - DÜZELTME SONRASI → success, {id, cdnUrl:"/uploads/..."} ✅
[9] POST /v1/admin/events (coverImageId=yüklenen dosya id'si)    → success ✅
      DB: cover_image_id=d430a997-... ✅ ; GET /uploads/<dosya> → 200 ✅
```

### Bu turda bulunan/düzeltilen bug'lar
1. EventsAdminController(API) CreatedBy set etmiyordu → düzeltildi.
2. CampaignsAdminController(API) ApprovedBy set etmiyordu → düzeltildi.
3. AdsAdminController(API) UserId set etmiyordu → düzeltildi.
4. ApiResponse<T>.Data [JsonIgnore(WhenWritingNull)] değer tiplerinde serileştirmeyi patlatıyordu
   (public POST /v1/announcements baştan beri 500 idi!) → WhenWritingDefault yapıldı.
5. UploadFileCommandHandler Uri.LocalPath göreli URL'de fırlatıyordu
   (API'den dosya yükleme baştan beri 500 idi; Web BaseUrl dolu olduğu için çalışıyordu) → Path.GetFileName.

Sonuç: dotnet build KadirliApp.sln → 0 hata, 0 uyarı. Faz 7 özelliklerinin tamamı Admin API üzerinden çalışır durumda.

---

## Auth Route + OTP Hata Kodu Düzeltmesi Testleri (8 Temmuz 2026)

## Ortam
- Docker: kadirliapp_db + kadirliapp_redis ayakta.
- Api: `dotnet run --urls http://localhost:5005`. Build: 0 hata, 0 uyarı.

```
[1] POST /v1/auth/login  {"phone":"+905001112233"}
      → 200 {"success":true,"data":{"message":"OTP gönderildi","otp":"586155"},"meta":{...}} ✅
[2] POST /v1/auth/verify-otp  (YANLIŞ otp: "000000")
      → 400 {"success":false,"error":{"code":"INVALID_OTP","message":"Geçersiz veya süresi
        dolmuş OTP."},"meta":{...}} ✅ (eskiden 500 INTERNAL_ERROR idi)
[3] POST /v1/auth/verify-otp  (doğru otp)
      → 200 {"success":true,"data":{"token":"eyJhbGciOi..."}} ✅ (JWT: user_id, role=user, phone)
[4] POST /api/Auth/login  (eski route)
      → 404 ✅ (route tamamen taşındı)
```

Yapılan değişiklikler:
- `Api/Controllers/AuthController.cs`: `[Route("api/[controller]")]` → `[Route("v1/auth")]` (kontratın `/v1` prefix'i).
- `Application/Features/Auth/Commands/VerifyOtp/VerifyOtpCommandHandler.cs`: `UnauthorizedAccessException` (middleware tanımıyordu → 500) yerine tipli hatalar — geçersiz OTP: `AppException(..., "INVALID_OTP")` → 400; banlı/pasif hesap: `ForbiddenException` → 403 FORBIDDEN.

## Otp:DevMode Aktivasyonu Testleri (8 Temmuz 2026)

Kök neden: `Infrastructure/Identity/RedisOtpService` config'deki `Otp:DevMode`'u hiç okumuyordu
(appsettings.json'da `true` olmasına rağmen her zaman rastgele OTP üretiyordu). Masterclass 12.2'ye
uygun düzeltme: DevMode'da sabit `123456`.

```
[1] POST /v1/auth/login  {"phone":"+905001112244"}  (2 kez üst üste)
      → her ikisinde de {"otp":"123456"} ✅ (sabit kod)
[2] POST /v1/auth/verify-otp  {"otp":"123456"}
      → 200 + JWT ✅
[3] POST /v1/auth/verify-otp  (yanlış otp: "999999")
      → 400 INVALID_OTP ✅ (önceki düzeltme DevMode'da da çalışıyor)
[4] Test kullanıcısı (+905001112244) DB'den silindi (temizlik) ✅
```

## Guide Kategori Delete Testleri (8 Temmuz 2026)

YENİ: `DeleteGuideCategoryCommand` (+Handler) ve `DELETE /v1/admin/guide/categories/{id}`.
Guide FK'ları `ON DELETE RESTRICT` olduğundan handler item/alt kategori ön kontrolü yapıp
`ConflictException` fırlatıyor (ön kontrol olmasa DB hatası → 500 olurdu).
Tüm istekler super_admin JWT ile (OTP DevMode 123456 üzerinden alındı).

```
[1] Boş kategori oluştur → DELETE                        → 200 {"success":true,"data":true} ✅
[2] İçinde item olan kategori → DELETE                   → 409 {"code":"CONFLICT",
      "message":"Bu kategoride rehber kayıtları var..."} ✅
[3] Alt kategorisi olan kategori → DELETE                → 409 CONFLICT ("alt kategorileri var") ✅
[4] Olmayan id → DELETE                                  → 200 {"data":false} ✅
      (DeleteGuideItemCommand ile aynı desen)
[5] Token'sız DELETE                                     → 401 ✅
[6] Temizlik: SIL-TEST kayıtları yeni endpoint'le silindi; DB'de 0 satır kaldı ✅
```

---

## Faz 10.2 — Auth: register/refresh/logout Canlı Testleri (16 Temmuz 2026)

Api :5005, `Otp:DevMode=true` (OTP=123456). Test telefonu +905332221100 (sonda silindi).

```
[1]  POST /v1/auth/login                                  → 200 + otp:123456 ✅
[2]  POST /v1/auth/verify-otp (yeni kullanıcı)            → 200 {"isNewUser":true,"tempToken":"..."} ✅
[3]  Temp token korumalı uçta (users/{id}/profile)        → 401 ✅ (RefreshSecret imzalı, JwtBearer reddeder)
[4]  Temp token refresh olarak                            → 401 UNAUTHORIZED ✅ (token_type ayrımı)
[5]  POST /v1/auth/register (username+mahalle+yaş)        → 200 accessToken(expiresIn:86400)+refreshToken(90g, jti) ✅
       DB: username=claudetest_manuel, age=30, primary_neighborhood_id doğru ✅
[6]  Access ile kendi profili                             → 200 ✅
[7]  POST /v1/auth/refresh                                → 200 yeni çift ✅
       Redis: revoked_jti:{eski jti} TTL≈7.775.944 sn (≈90 gün) ✅ (rotasyon)
[8]  ESKİ refresh tekrar                                  → 401 "iptal edilmiş" ✅
       (bu sırada auth rate limit 5/dk canlıda 429 verdi — Faz 9.2 çalışıyor ✅)
[9]  Token'sız POST /v1/auth/logout                       → 401 ✅
[10] POST /v1/auth/logout (access + refresh body)         → 200 "Çıkış yapıldı" ✅
       → aynı refresh ile refresh denemesi                → 401 ✅ ; DB: fcm_token=NULL ✅
[11] Jwt__AccessExpiresDays=0.0002 (≈17 sn) ile restart:
       verify-otp (kayıtlı kullanıcı)                     → isNewUser:false, expiresIn:17 ✅
       süre dolunca korumalı uç                           → 401 ✅
       süresi dolmuş access + GEÇERLİ refresh             → 200 yeni çift → korumalı uç 200 ✅
[12] xUnit: 17/17 yeşil (7 yeni/yenilenmiş auth testi dahil) ✅
[13] Test kullanıcıları silindi (claudetest_manuel + bu oturumun test koşusu kalıntıları) ✅
```

EKSTRA KEŞİF: Entegrasyon testleri Testcontainers'a değil dev DB'ye yazıyormuş (WebApplicationFactory
config override'ı build sırasında eklendiğinden eager connection-string okuması dev değerini görüyordu).
AddInfrastructure lazy config okumaya geçirildi; kanıt: test koşusu öncesi/sonrası dev DB users sayısı 13→13.
⚠️ Eski oturum kalıntılarının (4 test kullanıcısı + 2 dosya satırı + 2 png) silinmesi izin engeline takıldı —
SQL Progress.md 10.2 sonunda, kullanıcı onayı bekliyor.

---

## 4 Ağustos 2026 — Faz 11.17 (panel: şehirlerarası ulaşım · denetim izi · çöp kutusu · kesinti filtresi)

**Ortam:** `docker compose up -d` · API `:5005` · panel `:5203` · Chrome (super_admin) ·
Android `Pixel_9` emülatörü · iOS `iPhone 17` simülatörü — dördü de aynı anda ayakta.
⚠️ API ve panel **aynı anda** `dotnet run` edilirse `KadirliApp.Infrastructure` ref-assembly
kopyalamasında dosya kilidi oluşuyor ve panel derlemesi patlıyor → **sırayla başlatıldı**.

### 🔑 Asıl doğrulama — panel → API → telefon halkası

```
[1] Panel /TransportAdmin/Intercity                      → 200, 2 hat ✅
      fiyat "₺220,00" (PanelDisplay.TL — jenerik ¤ YOK) ✅
      sekmeler: Şehir İçi Hatlar | Şehirlerarası Hatlar  ✅
[2] Adana hattı → IntercityEdit → "21:15" + Saat Ekle    → 302, "21:15 kalkışı eklendi." ✅
[3] GET /v1/transport/intercity-routes?searchTerm=Adana
      → ['07:00','10:30','14:00','17:30','21:15']        ✅ (mobilin gördüğü uç)
[4] Android emülatörü: Ana Sayfa → Ulaşım → Şehirlerarası
      → "Kadirli → Adana" kartı açıldı → 21:15 çipi GÖRÜNDÜ ✅
      🔑 11.17 öncesi bu değişiklik yalnız psql ile yapılabiliyordu.
[5] Temizlik: DELETE FROM intercity_schedules WHERE departure_time='21:15:00' → 1 satır ✅
      uç tekrar ['07:00','10:30','14:00','17:30'] ✅
```

### Denetim izi

```
[6] /AuditLogsAdmin/Index → 200; 11.15c oturumunun GERÇEK kayıtları listelendi ✅
      "03.08.2026 18:48 · admin · Süper Yönetici · Duyurular · [Sildi] · Duyuru fb81b61f… · ::1"
      "03.08.2026 18:45 · admin · Süper Yönetici · İlanlar   · [Onayladı] · İlan 901ecf04…"
      → eylem/modül/rol/kayıt tipi TÜMÜ Türkçe, ham İngilizce sızıntısı yok ✅
      → "bu ilanı kim onayladı/sildi?" sorusu ilk kez psql'siz cevaplandı ✅
```

### Çöp kutusu

```
[7] /TrashAdmin/Index → 200; iki silinmiş kayıt bulundu ✅
      "Kadirli Yaz Konseri" (Etkinlikler) · "iPhone 13 128 GB" (İlanlar)
      → IgnoreQueryFilters gerçekten çalışıyor (global süzgeç bunları gizliyor)
      → modül çipleri + "geri getirilemeyenler" açıklaması (rehber/kullanıcı) çizildi ✅
   ⚠️ "Geri getir" CANLIDA DENENMEDİ: data-confirm tarayıcı confirm()'ini tetikliyor ve
      Chrome uzantısını kilitliyor. Davranış PanelTrashTests'te kapsandı (status korunumu
      dâhil) ve "kuralı bilerek boz" turunda kırmızıya döndüğü görüldü.
```

### Kesinti filtresi

```
[8] /PowerOutagesAdmin/Index                             → 200, 2 kesinti, rozet "Bitti" ✅
[9] ?neighborhood=karata&phase=past                      → 1 satır ("Karataş") ✅
      "Toplam 2 kaydın 1 tanesi gösteriliyor" ✅ · "Temizle" bağlantısı belirdi ✅
      → parçalı + harf duyarsız arama ✅ ; durum süzgeci gerçekten süzüyor ✅
```

**Sonuç:** dört ekran da canlıda çalıştı. Geçici veri temizlendi (21:15 kalkışı),
`audit_logs`'ta `action='restore'` satırı **0** (test kalıntısı yok).
`dotnet test` 464/464 · `flutter analyze` 0 · `flutter test` 669/669.
