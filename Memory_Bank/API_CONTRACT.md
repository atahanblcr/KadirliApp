# KadirliApp — API Kontratı (Flutter El Kitabı)

> **Amaç:** Flutter mobil istemcisinin tek referansı. Zarf şeması, hata kodları, auth akışı, sayfalama, tarih/görsel kuralları ve public uç envanteri.
> **Makine-okur şema:** `docs/openapi.json` (OpenAPI 3.0; `openapi_generator`/`dio` ile kod üretimi için). Bu doküman insan rehberi, openapi.json kesin şema — **çeliştiğinde openapi.json + mevcut kod kazanır.**
> Son gözden geçirme: **9 Ağustos 2026 (Faz 12.4)** — **etkinlik uçları additive olarak genişledi.**
> `EventResponseDto`'ya dört alan (`districtId` · `districtName` · `provinceName` · **`locationLabel`**),
> `GET /v1/events`'e üç süzgeç (`districtId` · **`locationScope`** · `onlyLocal`) eklendi.
> **Hiçbir alan silinmedi/yeniden adlandırılmadı** → mağazadaki eski sürümler tek satır değişmeden
> çalışır; yalnız `isLocal` artık **doğru** değeri taşıyor (12.4 öncesinde her kayıtta `false`'tu).
> 🔑 `locationLabel` **sunucuda** üretilir ve `districts` için public bir lookup ucu **bilinçli olarak
> eklenmedi**: istemcinin ilçe listesine ihtiyacı yok, etiket zaten DTO'da hazır geliyor.
> Önceki: **6 Ağustos 2026 (Faz 12.2b)** — public yüzeyde **değişiklik yok.**
> 12.2b bildirim *gönderimini* panele taşıdı ama kampanya bir **panel kavramı**: yeni public uç
> eklenmedi, `NotificationDto`'ya `campaignId` **bilinçli olarak konmadı** (istemcinin gönderim
> tarihçesiyle işi yok) ve `GET /v1/notifications` şekli aynı kaldı — yani mağazadaki eski
> sürümler tek satır değişmeden çalışmaya devam ediyor. Aynı sebeple manuel gönderimde
> `relatedType`/`relatedId` **boştur**: uydurma bir tür, görünmez sözleşme #18 gereği mobilde
> zaten gezinmeyi iptal ederdi.
> Önceki içerik güncellemesi: 5 Ağustos 2026 (Faz 12.1 — `POST /v1/client-errors`).
>
> Son güncelleme: 4 Ağustos 2026 (Faz 11.17). Kapsam: 10.1–10.12'nin public yüzeyi + mobil fazlarının
> additive eklemeleri (11.10 `?sort=date_asc` · **11.18 `?sort=` duyuru/vefat/kampanya uçlarında ve etkinlikte yeni anahtarlar — hepsi isteğe bağlı, verilmezse eski sıra birebir korunur** · 11.11 `/v1/places/categories` · 11.15c bildirimlerde
> "hedefi yaşayan" süzgeci ve `/v1/ads`'in public'te yok sayılan `?status=` parametresi ·
> **11.17 `schedules[].isActive`**). 11.17 **yeni public uç eklemedi** (denetim izi ve çöp kutusu
> yalnız paneldedir); uç sayısı **136**'da kaldı, `docs/openapi.json` bu oturumda yenilendi.

---

## 1. Temel Bilgiler

| Konu | Değer |
|---|---|
| Base URL (dev) | `http://localhost:5005` |
| API prefix | Tüm uçlar `/v1/...` |
| İçerik tipi | `application/json` (dosya yükleme hariç → `multipart/form-data`) |
| Path biçimi | **Tamamen küçük-harf / kebab-case** (`/v1/power-outages`, `/v1/users/me`). Routing case-insensitive ama kanonik biçim kebab'dır — codegen bunu kullanmalı. |
| Kimlik | `Authorization: Bearer <accessToken>` (JWT) |

---

## 2. Yanıt Zarfı

**Her** yanıt aynı zarfla döner (`ApiResponseWrapperFilter` + `ExceptionMiddleware`).

### Başarılı
```json
{
  "success": true,
  "data": { /* uç-özel gövde; liste, nesne, Guid, bool veya null olabilir */ },
  "meta": {
    "timestamp": "2026-07-25T09:12:33.4210000Z",
    "path": "/v1/neighborhoods",
    "traceId": "00-abc123...-def456-01"
  }
}
```
- **`meta` TÜM başarılı yanıtlarda vardır** (Faz 10.13'te tutarlılık sağlandı — eskiden bazı uçlarda null'dı).
- `meta.traceId`: destek/hata ayıklamada sunucu loglarıyla (Seq) eşleşir. Kullanıcıya hata gösterirken traceId'yi de iletmek destek için faydalıdır.

### Hatalı
```json
{
  "success": false,
  "error": { "code": "NOT_FOUND", "message": "İlan bulunamadı." },
  "meta": { "timestamp": "...", "path": "...", "traceId": "..." }
}
```
- `message` **Türkçe ve kullanıcıya gösterilebilir** metindir; yine de istemci `code`'a göre dallanmalı (mesaj metni değişebilir).

---

## 3. Hata Kodları Sözlüğü

| HTTP | code | Anlamı / ne zaman |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Girdi doğrulaması başarısız (FluentValidation + handler kuralları). En yaygın hata. |
| 400 | `INVALID_OTP` | OTP yanlış/süresi geçmiş/deneme aşıldı. |
| 400 | `INVALID_PASSWORD` | Mevcut şifre yanlış (şifre değiştirme). |
| 400 | `INVALID_ROLE` | Geçersiz rol ataması (panel/staff). |
| 400 | `USERNAME_CHANGE_LIMIT` | Kullanıcı adı 30 günde bir değişebilir; ihlal. |
| 400 | `NEIGHBORHOOD_CHANGE_LIMIT` | Birincil mahalle 30 günde bir değişebilir; ihlal. |
| 400 | `SELF_DELETE_FORBIDDEN` | Admin/staff kendi hesabını `/users/me`'den silemez (yalnız Role=User). |
| 400 | `DUPLICATE` | Benzersizlik ihlali (bazı akışlarda; çoğu yerde `CONFLICT`). |
| 401 | `UNAUTHORIZED` | Token yok/geçersiz/süresi geçmiş/iptal edilmiş. → refresh dene, olmazsa login. |
| 403 | `FORBIDDEN` | Yetki yok (başkasının kaynağı, admin ucu vb.). |
| 404 | `NOT_FOUND` | Kayıt yok VEYA görünürlük gereği gizli (pending/pasif kayıt public'e 404). |
| 409 | `CONFLICT` | Çakışma (ör. uzatma hakkı doldu, benzersiz alan çakışması, FK bağımlılığı). |
| 429 | `RATE_LIMITED` | Hız limiti aşıldı. `Retry-After` header'ına bak. |
| 500 | `INTERNAL_ERROR` | Beklenmeyen sunucu hatası. `traceId` ile bildir. |

> **⚠️ Bilinen istisna:** `GET /v1/announcements/{id}` bulunamayınca mevcut kontrat gereği **HTTP 200 + `success:false` + `error.code=NOT_FOUND`** döner (diğer uçlar gerçek 404 verir). İstemci announcements detayında `success` alanını da kontrol etmeli. (Davranış geriye-uyumluluk için korundu.)

---

## 4. Kimlik Doğrulama Akışı

Telefon + OTP tabanlı. Üç token türü: **access** (kısa ömür, `Authorization` header'ında), **refresh** (uzun ömür, yenileme için), **temp** (yalnız yeni kullanıcı kaydı için, 30 dk).

```
1) POST /v1/auth/login        { "phone": "+9050..." }
   → { "message": "OTP gönderildi", "expiresIn": 300, "retryAfter": 60, "otp": "123456" }
     // OTP SMS ile gider. "otp" alanı YALNIZ Otp:DevMode=true iken YANITA EKLENİR
     // (prod'da alan hiç yoktur, null değil).
     // ⚠️ DÜZELTME (30 Tem 2026): bu doküman önceden `expiresInSeconds` /
     // `retryAfterSeconds` / `devOtp` yazıyordu; AuthController.Login gerçekte
     // yukarıdaki adları döndürüyor (canlı curl ile doğrulandı, Faz 11.3).

2) POST /v1/auth/verify-otp   { "phone": "+9050...", "otp": "123456" }
   → KAYITLI:      { "isNewUser": false, "accessToken": "..", "refreshToken": "..", "expiresIn": 86400 }
   → YENİ KULLANICI:{ "isNewUser": true,  "tempToken": ".." }    // henüz hesap YOK

3a) (yalnız yeni) POST /v1/auth/register
    { "tempToken": "..", "username": "ahmet", "primaryNeighborhoodId": "<guid>", "age": 30 }
    → { "accessToken": "..", "refreshToken": "..", "expiresIn": 86400 }

4) POST /v1/auth/refresh      { "refreshToken": ".." }
   → { "accessToken": "..", "refreshToken": ".." }   // jti ROTASYONU: eski refresh iptal olur, YENİSİNİ sakla

5) POST /v1/auth/logout       [Authorize] { "refreshToken": ".." }
   → refresh iptal + cihaz FcmToken'ı temizlenir
```

**Kurallar:**
- `accessToken` süresi dolunca (401 `UNAUTHORIZED`) → `refresh` ile yenile; refresh de reddedilirse (401) → login akışına dön.
- Refresh **tek kullanımlık** (rotasyon): her yenilemede dönen yeni refresh saklanmalı, eskisi çalışmaz.
- `tempToken` ve `refreshToken` ayrı imzalı → `[Authorize]` uçlarında access token yerine kullanılamaz.
- Login/verify-otp/register/refresh **anonim**; logout `[Authorize]`.
- FCM: giriş sonrası `POST /v1/notifications/fcm-token { token }` ile cihaz token'ı kaydedilmeli (push için).

---

## 5. Sayfalama

Sayfalı uçlar `data` içinde şu zarfı döner (`PagedResult<T>`):
```json
{
  "items": [ /* ... */ ],
  "totalCount": 137,
  "pageSize": 20,
  "currentPage": 1,
  "totalPages": 7
}
```
- Query parametreleri: `?page=1&limit=20`.
- **Limit clamp'i:** public uçlarda en fazla **50**, admin uçlarında 200. Aşan değer sessizce sınıra çekilir (`items`/`pageSize` gerçek değeri raporlar).
- İstisna: `GET /v1/notifications` sayfalıdır AMA toplam okunmamış sayısı `data.unreadCount` olarak zarf İÇİNDE ek taşınır (meta değil).
  "Filtre-bağımsız" demek **`?unreadOnly=` ve sayfadan bağımsız** demektir — `unreadCount` her zaman kullanıcının
  tüm okunmamışlarını sayar. ⚠️ Ama 11.15c'den beri **"hedefi yaşayan" süzgecine tabidir** (bkz. §Bildirimler):
  liste ile sayaç aynı sorgudan türer, bu yüzden ayrışmaları mümkün değil.

---

## 6. Tarih / Saat

- Tüm tarih-saat alanları **UTC, ISO-8601** (`2026-07-25T09:12:33Z`). İstemci yerel saate (Europe/Istanbul, UTC+3) çevirmeli.
- Yalnız-saat alanları (ulaşım kalkış saatleri) `"HH:mm"` biçiminde string.
- Nöbetçi eczane "bugün" hesabı sunucuda Türkiye saatine göre yapılır.

---

## 7. Görsel / Dosya URL'leri

- Görsel URL'leri **göreli** döner: `"/uploads/<guid>_<ad>.png"`.
- **Kural:** istemci başına **API origin'ini ekler** → `http://localhost:5005/uploads/...`.
- Prod'da mutlak CDN/domain isteniyorsa sunucuda `FileStorage:BaseUrl` ayarlanır (ör. `https://cdn.kadirli.app`) → URL'ler mutlak döner; istemci kuralı bozulmaz (mutlaksa olduğu gibi kullan).
- Yükleme: `POST /v1/files/upload` (`multipart/form-data`, alan adı `file`), `[Authorize]`, ≤10 MB, yalnız jpeg/png/webp (magic-byte doğrulanır). Yanıt dosya kaydını (id + url) döner; id'ler ilgili create/update uçlarına verilir.

---

## 8. Hız Limitleri (Rate Limiting)

| Kapsam | Limit | Uçlar |
|---|---|---|
| Global | 300/dk/IP | Tüm uçlar (varsayılan) |
| Auth | 5/dk/IP | `/v1/auth/*` |
| Public-write | 15/dk/IP | Anonim/hafif yazma: deaths/complaints/ads POST, track-phone/whatsapp, files/upload |

429'da `Retry-After` (saniye) header'ı döner. İstemci backoff uygulamalı.

---

## 9. CORS

Mobil **native istemci CORS kullanmaz** (bu bölüm yalnız Flutter WEB / tarayıcı hedefi içindir). İzinli origin'ler sunucuda `Cors:Origins`'ten okunur; liste boşsa hiçbir tarayıcı-origin'ine izin verilmez.

---

## 10. Public Uç Envanteri

> `[A]` = `Authorization: Bearer` gerekir. İşaretsiz = anonim. Tam istek/yanıt şeması için `docs/openapi.json`.

### Auth
- `POST /v1/auth/login`, `/verify-otp`, `/register`, `/refresh` — anonim
- `POST /v1/auth/logout` `[A]`

### Kullanıcı (me)
- `GET|PATCH|DELETE /v1/users/me` `[A]` (DELETE: yalnız Role=User)
- `GET /v1/users/me/ads` `[A]`, `GET /v1/users/me/favorites` `[A]`, `PATCH /v1/users/me/notifications` `[A]`

### İlanlar (Ads)
- `GET /v1/ads`, `GET /v1/ads/{id}`, `GET /v1/ads/categories`, `GET /v1/ads/categories/{id}/properties` — anonim
  - ⚠️ **Faz 11.15c:** şemada (`openapi.json`) bir **`?status=`** parametresi görünür ama **public uçta YOK SAYILIR**
    (yalnız panelin onay kuyruğu içindir; `/v1/events`'teki `status` ile aynı durum). Public liste her zaman
    yalnız `approved` **ve süresi geçmemiş** ilanları döner. Mobil bu parametreyi **göndermemeli** — gönderirse
    hata almaz, sessizce yok sayılır ve yanlış beklenti oluşur.
- `POST /v1/ads` `[A]`, `PUT|DELETE /v1/ads/{id}` `[A]`, `POST /v1/ads/{id}/extend` `[A]`, `POST|DELETE /v1/ads/{id}/favorite` `[A]`
- `POST /v1/ads/{id}/track-phone`, `/track-whatsapp` — anonim (sayaç)

### Bildirimler
- `GET /v1/notifications` `[A]`, `PATCH /v1/notifications/{id}/read` `[A]`, `POST /v1/notifications/read-all` `[A]`, `POST /v1/notifications/fcm-token` `[A]`
- 🔑 **Faz 11.15c — "hedefi yaşayan" süzgeci (DAVRANIŞ DEĞİŞİKLİĞİ, mobilde görünür):**
  Hedefi artık **yayında olmayan** bildirim listede **hiç dönmez**. Bir bildirim şu üç durumda kaybolur:
  duyuru silinir · duyuru `draft`a çekilir · duyurunun `visibleUntil`'i geçer.
  **Neden:** aksi hâlde kullanıcı bildirimi görüyor, dokunuyor ve `GET /v1/announcements/{id}` `NOT_FOUND`
  döndüğü için boş sayfaya düşüyordu (canlı kanıtlı: silinen duyurunun 9 bildirimi ayakta kalmıştı).
  ⚠️ **`unreadCount` AYNI süzgeçten geçer** — rozet ile liste asla ayrışmaz.
  📌 Mobil tarafta yapılacak bir şey yok: liste kısalır, deep-link zaten çalışan hedeflere gider.
  📌 Bugün bildirim üreten tek modül **duyurular**; başka bir modül bildirim üretmeye başlarsa
  (vefat/etkinlik/kampanya) o modülün de süzgece dalı yazılmalı — yoksa o türden bildirimler
  süzgeçten **muaf** kalır (silinmez, ama ölü bağlantı olabilir).

### Duyurular / Kesintiler
- `GET /v1/announcements` (sayfalı, `?typeId=`, **`?sort=created_desc|created_asc|title_asc|title_desc`** — 11.18, varsayılan `created_desc`, bilinmeyen değer varsayılana düşer), `GET /v1/announcements/types`, `GET /v1/announcements/{id}` (⚠️ 200+success:false quirk)
- `POST /v1/announcements/{id}/view`, `/click` — anonim (sayaç)
- `GET /v1/power-outages`, `GET /v1/power-outages/{id}`
  - ⚠️ **Sayfasız, düz dizi** (görünmez sözleşme #1) — Faz 12.3'te alan eklendi, **şekil değişmedi**.
  - 🆕 **Faz 12.3 (additive):** `neighborhoodId` (Guid?), `areaDetail` (string?), `announcementId` (Guid?).
  - 🔴 `neighborhood` alanı **aynen duruyor ve adı değişmedi**, ama artık `neighborhoodId`
    doluyken **sözlükten türetiliyor**: değer `neighborhoods.name` ile birebir aynıdır.
    Eski sürümler etkilenmez — üstelik ad üzerinden yaptıkları eşleşme artık yazım farkı
    olmadığı için **daha güvenilir** çalışır ("Cengiz Topel Mahallesi" → "Cengiz Topel").
    FK'sı olmayan (12.3 öncesinden kalan, geri doldurmada eşleşmemiş) kayıtlarda alan hâlâ
    serbest metindir ve `neighborhoodId` **`null`** gelir.
  - `areaDetail` = mahallenin hangi kısmı ("Atatürk Caddesi ve çevresi"). Önce mahalle
    metnine sıkıştırılıyordu; ayrılmasının sebebi sözlük eşleşmesini mümkün kılmaktı.
  - `announcementId` dolu ⇒ bu kesinti için **bildirim gönderilmiş**. Kesinti bildirimi ayrı
    bir tür değil, **bir duyurudur**: push `data.relatedType` yine **`announcement`** taşır ve
    deep-link `/duyurular/:id`'ye gider → **mobilde değişiklik gerekmez**, mağazadaki eski
    sürümler de kesinti bildirimini alır (görünmez sözleşme #18 korunur).
  - ⚠️ Kesinti silinirse duyurusu ve **onun bildirimleri de silinir** (#24'ün uzantısı).

### Etkinlik / Kampanya / İşletme
- `GET /v1/events` (sayfalı; `?search=` başlık+mekan, `?categoryId=`, `?startDate=`/`?endDate=` (`yyyy-MM-dd`, gün dahil), `?isFree=`, **`?sort=date_asc|date_desc|title_asc|title_desc`** — varsayılan `date_desc`, bilinmeyen değer varsayılana düşer; `title_*` Faz 11.18'de panel sütun sıralaması için eklendi). ⚠️ **Yalnız `approved` döner** (`status` parametresi public uçta yok sayılır); `eventDate` "TR günü 00:00 UTC", `eventTime` ayrı `"HH:mm:ss"` alanı → **saat dilimi kaydırılmaz**.
- `GET /v1/events/{id}`, `/events/categories` (sayfasız `{id,name,slug}` listesi), `/events/calendar?year=&month=` (sayfasız, o ayın onaylı etkinlikleri — ince DTO)
- **Faz 12.4 — konum (additive, hem listede hem detayda):** `districtId` · `districtName` ("Kadirli", "Merkez") · `provinceName` ("Osmaniye", "Adana") · **`locationLabel`** — kullanıcıya gösterilecek **hazır** metin: `"Kadirli"` · `"Osmaniye / Merkez"` · `"Adana"`.
  🔴 **Etiketi istemci kurmaz**, sunucuda tek yerde üretilir (`DistrictLabel`); ayrı kurulsaydı panel ile mobil aynı etkinliği farklı yazardı ve kimse hata almazdı (görünmez sözleşme #43). Konumu bilinmeyen kayıtta dördü de `null` — istemci rozeti **hiç çizmez**.
  `isLocal` alanı 10.x'ten beri vardı ama panel hiç yazmıyordu (her kayıtta `false`); 12.4'ten beri **türetiliyor**: "ilçesi Kadirli mi" (#44). Alan adı ve tipi **değişmedi** → eski sürümler kırılmaz, yalnız değer artık doğru.
- **Faz 12.4 — konum süzgeçleri:** `?districtId=` (tek ilçe) · **`?locationScope=local|province|nearby|away`** · `?onlyLocal=true|false` (kısayol, aynı enum'a çevrilir).
  `local` = Kadirli · `province` = Osmaniye'nin tamamı (Kadirli dâhil) · `nearby` = **Osmaniye dışı** · `away` = Kadirli dışı.
  🔴 **"Çevre iller" bir SUNUCU tanımıdır** — istemci yalnız `nearby` der, kümeyi kendisi hesaplamaz: sözlüğe yarın eklenen bir Osmaniye ilçesini eski sürümler çevre il sayardı. ⚠️ Bilinmeyen değer **varsayılana düşer** (liste boşalmaz, 400 gelmez).
- `GET /v1/campaigns` (sayfalı; `?search=` kampanya başlığı + işletme adı, **`?sort=created_desc|created_asc|title_asc|title_desc|end_asc|end_desc`** — 11.18, varsayılan `created_desc`). ⚠️ Public uç **yalnız onaylı VE tarihi geçerli** kampanyaları döner (`OnlyActive` sabit) → süresi dolan kampanya listede de detayda da yok (`{id}` **404**). ⚠️ `discountCode` gövdede geliyor ama mobil onu göstermez; kod `view-code` ile açılır (sayaç esnafın ölçümü).
- `GET /v1/campaigns/{id}`; `POST /v1/campaigns/{id}/view-code` `[A]` → `{code, viewedAt}`; aynı kullanıcı ikinci kez isterse **aynı kayıt** döner (sayaç artmaz), **kodsuz kampanyada 400 VALIDATION_ERROR**

### Vefat / Eczane / Taksi / Mekan / Rehber / Ulaşım
- `GET /v1/deaths` (**`?sort=funeral_desc|funeral_asc|name_asc|name_desc`** — 11.18, varsayılan `funeral_desc`), `/deaths/{id}`, `/deaths/cemeteries`, `/deaths/mosques`; `POST /v1/deaths` `[A]` (moderasyona düşer)
- `GET /v1/pharmacies`, `/pharmacies/{id}`, `/pharmacies/on-duty?date=`, `/pharmacies/schedule?year=&month=`
- `GET /v1/taxis/drivers`, `/taxis/drivers/{id}`; `POST /v1/taxis/drivers/{id}/call` `[A]` (telefon döner)
  - ⚠️ Arama parametresi **`searchTerm`** (diğer modüllerde `search`) — ad **ve plakada** arar; yanlış ad sessizce yok sayılır.
- `GET /v1/places`, `/places/categories` (11.11'de eklendi — `{id,name,slug}`), `/places/{id}`
  - ⚠️ `place.amenities` DB'de `jsonb` ama DTO'da `string` → yanıtta **JSON içeren metin** gelir (`"{\"WC\":true,\"Wi-Fi\":false}"`), nesne değil. Anahtarda **olmayan** olanak "belirtilmemiş" demektir, "yok" değil.
  - ⚠️ Liste **ada göre** sıralı (başka sıralama seçeneği yok); arama yalnız **adda** koşar.
- `GET /v1/guide/categories`, `/guide/categories/{id}`, `/guide/items`, `/guide/items/{id}`
- `GET /v1/transport/intercity-routes`, `/transport/intracity-routes` — **sayfalı**; yalnız aktif hatlar (ve şehirlerarasında yalnız aktif seferler)
  - ⚠️ Arama parametresi **`searchTerm`** (taksi gibi; `search` sessizce yok sayılır). Şehirlerarası: hedef + firma; şehir içi: hat adı + numara.
  - ⚠️ **Detay ucu YOK** — kalkış saatleri (`schedules[].departureTime`) ve duraklar (`stops[]`, `stopOrder` sıralı) **liste gövdesinde** gelir.
  - ⚠️ Saat biçimleri **farklı**: şehirlerarası `departureTime` **`"07:00"`**, şehir içi `firstDeparture`/`lastDeparture` `TimeSpan` → **`"06:30:00"`**. İkisi de **tarihsiz duvar saati** → saat dilimi kaydırılmaz.
  - 🆕 **Faz 11.17 (additive):** `schedules[].isActive` eklendi. Bu uçta **her zaman `true`**'dur
    (liste sorgusu zaten yalnız aktif seferleri döndürür); alan panelin tek-kayıt sorgusu için var,
    orada pasif seferler de dönüp bu bayrakla işaretleniyor. **Mobil bu alanı okumak zorunda değil.**
  - 📌 **Faz 11.17'de bu hatlar panelden yönetilebilir oldu** (şehirlerarası hat + kalkış saati +
    şehir içi durak). Uç davranışı **değişmedi**; yalnız içeriğin kaynağı artık `psql` değil panel.
  - 🆕 **Faz 12.5 (additive) — şehirlerarası hatta beş, sefere iki yeni alan:**

    | Alan | Tip | Not |
    |---|---|---|
    | `vehicleType` | `"bus"` \| `"minibus"` | 12.5 öncesi satırlar **`"bus"`** ile göç etti. Türkçe karşılığı istemcide üretilir. |
    | `departurePointId` | `guid?` | `null` = kalkış noktası **girilmemiş** (12.5 öncesinden kalma). |
    | `departurePointName` | `string?` | Sözlükten gelir, hatta yazılmaz. |
    | `departurePointAddress` | `string?` | Koordinat yoksa harita araması bununla yapılır. |
    | `departurePointLatitude` / `…Longitude` | `decimal?` | Mobildeki "Yol tarifi" butonunun kaynağı (12.6). |
    | `schedules[].days` | `["mon","tue","wed","thu","fri","sat","sun"]` | Sefer hangi günler çalışıyor; sıra **Pazartesi'den** başlar. |
    | `schedules[].runsDaily` | `bool` | "7 günün hepsi" kısayolu; 12.5 öncesi seferlerde **`true`**. |

    🔴 **Uç seferleri günlere göre ELEMEZ**, yalnız bildirir. Mağazadaki eski sürümler `days`'i
    tanımadığı için Pazar günü de tüm saatleri gösterir — bu **bugünkü doğruluk seviyesinin
    aynısıdır, regresyon değildir**. Sunucuda elenseydi eski sürümler için liste *sebepsiz*
    boşalırdı.
    ⚠️ `days` kodları **kontrattır**: değişirlerse eski sürümler günü tanımaz. Kod ↔ bit
    dönüşümünün tek sahibi sunucudaki `OperatingDays` (Pazartesi=1 … Pazar=64) —
    .NET `DayOfWeek` **Pazar=0**'dan başladığı için istemcide ikinci bir eşleme yazılırsa
    **"Salı seferi Pazartesi görünür"** ve kimse hata almaz.
  - 🆕 **Faz 12.5: `?vehicleType=bus|minibus` süzgeci.** ⚠️ Bilinmeyen değer **süzmez**
    (400 gelmez, liste boşalmaz) — 12.4'te `locationScope` için verilen aynı karar.
  - ⚠️ **Kalkış noktasının public sözlük ucu YOK** (ilçelerdeki karar): hattın ihtiyacı olan
    ad/adres/koordinat zaten hat gövdesinde geliyor.
  - 🆕 **Faz 12.6 — istemci tarafı (sunucuda değişiklik YOK).** 12.5'in alanları artık mobilde
    okunuyor: araç rozeti + `?vehicleType` süzgeci, kalkış noktası + "Yol tarifi", gün rozetleri
    ve **günü hesaba katan "sıradaki sefer"**.
    - 🔴 **`days` boş ya da hiç gelmemişse istemci "HER GÜN" varsayar** (`OperatingDays.fromCodes`).
      "Hiçbir gün" saymak, 12.5 öncesi kayıtları (alan yok) ve tanınmayan kod taşıyan kayıtları
      ekrandan **sessizce silerdi** — sunucunun `runsDaily` varsayılanı zaten `true`. Şüphede
      kalınca **göstermek** doğru yön; uç de zaten elemiyor.
    - 🔴 **İstemci de günlere göre ELEMİYOR:** hafta içi seferi Pazar günü de listede duruyor,
      yalnız rozeti soluk ve "sıradaki sefer" onu atlıyor. Süzseydi bir hafta içi hattının kartı
      Pazar günü **boş** görünürdü.
    - ⚠️ Gün ↔ bit dönüşümünün **mobildeki tek sahibi** `features/transport/application/operating_days.dart`.
      Dart'ın `DateTime.weekday`'i Pazartesi=1 … Pazar=7 olduğu için maskeyle *tesadüfen* hizalı —
      tam bu yüzden `1 << weekday` gibi bir ikinci eşleme derlenir, çalışır ve **günü bir kaydırır**.
    - ⚠️ Araç süzgecinde istemci **üç** seçenek sunuyor (`Tümü` / `bus` / `minibus`); "Tümü"de
      parametre **hiç gönderilmiyor**. İkili bir süzgeç olsaydı sunucuya yarın eklenecek üçüncü
      bir tip mağazadaki eski sürümlerde **hiçbir süzgeçte görünmezdi**.

### Haberler (Faz 12.12)

> 🔑 **Mobil WordPress'e ASLA bağlanmaz.** Zincir tek yönlü:
> `WordPress → (Hangfire senkron, 15 dk) → bizim Postgres → /v1/news → mobil`.
> Kaynağa bağlansaydık override, kategori görünürlüğü, bildirim, arama ve önbellek imkânsız
> olurdu; üstelik uygulama **başka birinin çalışma süresine** bağımlı olurdu.

- `GET /v1/news` — **sayfalı** (`{items,totalCount,pageSize,currentPage,totalPages}`).
  Süzgeçler: `?search=` (başlık + gövde metni; ⚠️ **`search`**, `searchTerm` değil — #4) ·
  `?categoryId=` · `?featured=true`. Varsayılan sıralama **`publishedAt desc`** (+ `ThenBy(Id)`).
- `GET /v1/news/{id}` — bulunamayan/gizlenen kayıt **404**.
- `GET /v1/news/categories` — sayfasız; `{id,name,slug,articleCount,showInFilterStrip,displayOrder}`.

**DTO alanları:** `id · title · excerpt · contentHtml · imageUrl · imageWidth · imageHeight ·
sourceUrl · publishedAt · modifiedAt · readingMinutes · isFeatured · categories[{id,name,slug}]`

- 🔴 **`contentHtml` YALNIZ detayda dolu, listede `null`.** 27k kayıtlık bir modülde sayfa
  başına 20 gövde taşımak hiç okunmayacak ~40 KB demek. Liste kartı `excerpt` kullanır.
- 🔴 **`title`/`excerpt`/`imageUrl` "etkin" değerdir**: yönetici bir override yazdıysa o, yoksa
  kaynağınki. **İstemci iki alanı birleştirmez** — birleştirseydi mağazadaki eski sürümler
  panel düzeltmesini hiç görmezdi.
- `imageUrl` **göreli**dir (`/uploads/…`, görünmez sözleşme #9): kapak görseli kaynaktan
  **indirilip aynalanır**. Kaynak görselinin `full` boyutu bile **650×368** (40 haberin 39'unda
  ölçüldü) → istemci "büyük görsel" beklememeli; yöneticinin koyduğu kapak varsa `imageWidth`/
  `imageHeight` **`null`** gelir (boyutu istemci ölçer).
- `contentHtml` **alım anında sunucuda temizlenmiştir** (beyaz liste:
  `p br strong em a figure figcaption img ul ol li blockquote h2 h3 h4`) → istemci **ikinci bir
  beyaz liste yazmaz**. ⚠️ Metin arası görseller **aynalanmaz** (hotlink; %9'u süreli `fbcdn`
  linki) → açılmayanı **zarifçe gizle**.
- `readingMinutes` sunucuda üretilir (200 kelime/dk, en az 1) — istemcide hesaplanmaz; liste
  ucu gövdeyi zaten taşımıyor.
- **Görünürlük:** arşivlenmemiş **ve** kaynağı yayında (`gone` değil) **ve** dışlanmış
  kategorisi olmayan kayıtlar. `articleCount` kaynağınki değil, **bizde görünen** sayıdır.

### Şikayet / Dosya / Lookup
- `POST /v1/complaints` — **anonim gönderim açık**; oturum varsa sunucu `user_id` claim'ini kendisi bağlar (istemci kullanıcı kimliği yollamaz). Yanıt: oluşan kaydın **Guid**'i. Gövde: `{subject, message, type?, relatedModule?, relatedId?}`.
  - ⚠️ **Sunucuda doğrulayıcı YOK** → zorunlu alan denetimi istemcide.
  - ⚠️ `type` **serbest metin** (sözlük ucu yok). Mobilin kullandığı değerler: `complaint | request | suggestion | content | app | other`; tanınmayan değer ham gösterilir.
- `GET /v1/complaints/my` `[A]` — sayfalı, **filtre parametresi yok**. ⚠️ Anonim gönderimlerde `user_id` NULL kaldığı için **hiçbir kullanıcının listesinde görünmez**. `status`: `pending | in_progress | resolved | rejected` (panelin kullandığı sabitler); `adminNotes` = kullanıcıya dönen **yanıt**.
- `POST /v1/files/upload` `[A]`, `DELETE /v1/files/{id}` `[A]`
- `GET /v1/neighborhoods`

---


## Hata bildirimi (Faz 12.1)

- `POST /v1/client-errors` — **anonim serbest**, `public-write` hız sınırına tabi (15/dk/IP).
  Gövde: `{code, message, level?, stackTrace?, path?, appVersion?, platform?, osVersion?}`.
  Yanıt her zaman **`202 Accepted`** + `{accepted: bool}`.

  🔑 **`source` alanı YOKTUR ve gönderilmemelidir** — sunucuda `mobile` olarak sabitlenir.
  İstemci `api` diyebilseydi kendi çökmesini sunucu hatası gibi gösterirdi.
  Aynı sebeple `traceId`, IP ve kullanıcı kimliği de gövdeden değil **bağlamdan** alınır
  (oturum varsa JWT'den).

  ⚠️ **Neden anonim:** çökme çoğu zaman oturum açılmadan önce olur (açılış ekranı, giriş
  akışı); `[Authorize]` konsaydı raporlanamayan hatalar tam da en kritik olanlar olurdu.

  ⚠️ **Tavanlar aşılırsa istek REDDEDİLİR, kırpılmaz:** `message` 2.000, `stackTrace` 16.000
  karakter. Sunucu kırpsaydı kesilen yığın farklı bir parmak izi üretir ve aynı hata iki ayrı
  kayda düşerdi (tekilleştirme sessizce bozulur). İstemci kendi tarafında kırpar.

  ⚠️ **İstemci bu ucu yeniden DENEMEZ** (`retry: apiRetry` bilinçli olarak yok) ve yanıtı
  beklemez: hata raporu yeniden denenirse zaten sorunlu olan sistem daha çok yorulur.
  Yanıtın `202` olması da bundan — istemci raporun akıbetiyle ilgilenmemeli.

  📌 Sunucu aynı hatayı **parmak izine göre tekilleştirir**: aynı çökme yüz kez gönderilse
  bile panelde tek satır, adet 100 olur. İstemcinin kusursuz bir kuyruk tutması gerekmez.

## 11. Görünürlük Kuralları (mobilin bilmesi gerekenler)

- Public listelerde yalnız **onaylı/aktif + süresi geçmemiş** kayıtlar döner. Pending/pasif/süresi geçmiş kayıt public'e **404** (varlık sızıntısı yok).
- Kullanıcı kendi ilanını `GET /v1/users/me/ads` ile her statüde görür (pending/rejected dahil; `rejectedReason` red gerekçesini taşır).
- Kullanıcı düzenlemesi (`PUT /v1/ads/{id}`) ilanı **yeniden moderasyona** düşürür (status=pending).
