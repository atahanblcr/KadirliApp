# KadirliApp — API Kontratı (Flutter El Kitabı)

> **Amaç:** Flutter mobil istemcisinin tek referansı. Zarf şeması, hata kodları, auth akışı, sayfalama, tarih/görsel kuralları ve public uç envanteri.
> **Makine-okur şema:** `docs/openapi.json` (OpenAPI 3.0; `openapi_generator`/`dio` ile kod üretimi için). Bu doküman insan rehberi, openapi.json kesin şema — **çeliştiğinde openapi.json + mevcut kod kazanır.**
> Son güncelleme: 25 Temmuz 2026 (Faz 10.13). Kapsam: 10.1–10.12 sonrası güncel public yüzey.

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
- İstisna: `GET /v1/notifications` sayfalıdır AMA filtre-bağımsız toplam okunmamış sayısı `data.unreadCount` olarak zarf İÇİNDE ek taşınır (meta değil).

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
- `POST /v1/ads` `[A]`, `PUT|DELETE /v1/ads/{id}` `[A]`, `POST /v1/ads/{id}/extend` `[A]`, `POST|DELETE /v1/ads/{id}/favorite` `[A]`
- `POST /v1/ads/{id}/track-phone`, `/track-whatsapp` — anonim (sayaç)

### Bildirimler
- `GET /v1/notifications` `[A]`, `PATCH /v1/notifications/{id}/read` `[A]`, `POST /v1/notifications/read-all` `[A]`, `POST /v1/notifications/fcm-token` `[A]`

### Duyurular / Kesintiler
- `GET /v1/announcements` (sayfalı, `?typeId=`), `GET /v1/announcements/types`, `GET /v1/announcements/{id}` (⚠️ 200+success:false quirk)
- `POST /v1/announcements/{id}/view`, `/click` — anonim (sayaç)
- `GET /v1/power-outages`, `GET /v1/power-outages/{id}`

### Etkinlik / Kampanya / İşletme
- `GET /v1/events` (sayfalı; `?search=` başlık+mekan, `?categoryId=`, `?startDate=`/`?endDate=` (`yyyy-MM-dd`, gün dahil), `?isFree=`, **`?sort=date_asc|date_desc`** — varsayılan `date_desc`, bilinmeyen değer varsayılana düşer). ⚠️ **Yalnız `approved` döner** (`status` parametresi public uçta yok sayılır); `eventDate` "TR günü 00:00 UTC", `eventTime` ayrı `"HH:mm:ss"` alanı → **saat dilimi kaydırılmaz**.
- `GET /v1/events/{id}`, `/events/categories` (sayfasız `{id,name,slug}` listesi), `/events/calendar?year=&month=` (sayfasız, o ayın onaylı etkinlikleri — ince DTO)
- `GET /v1/campaigns` (sayfalı; `?search=` kampanya başlığı + işletme adı). ⚠️ Public uç **yalnız onaylı VE tarihi geçerli** kampanyaları döner (`OnlyActive` sabit) → süresi dolan kampanya listede de detayda da yok (`{id}` **404**). ⚠️ `discountCode` gövdede geliyor ama mobil onu göstermez; kod `view-code` ile açılır (sayaç esnafın ölçümü).
- `GET /v1/campaigns/{id}`; `POST /v1/campaigns/{id}/view-code` `[A]` → `{code, viewedAt}`; aynı kullanıcı ikinci kez isterse **aynı kayıt** döner (sayaç artmaz), **kodsuz kampanyada 400 VALIDATION_ERROR**

### Vefat / Eczane / Taksi / Mekan / Rehber / Ulaşım
- `GET /v1/deaths`, `/deaths/{id}`, `/deaths/cemeteries`, `/deaths/mosques`; `POST /v1/deaths` `[A]` (moderasyona düşer)
- `GET /v1/pharmacies`, `/pharmacies/{id}`, `/pharmacies/on-duty?date=`, `/pharmacies/schedule?year=&month=`
- `GET /v1/taxis/drivers`, `/taxis/drivers/{id}`; `POST /v1/taxis/drivers/{id}/call` `[A]` (telefon döner)
- `GET /v1/places`, `/places/{id}`
- `GET /v1/guide/categories`, `/guide/categories/{id}`, `/guide/items`, `/guide/items/{id}`
- `GET /v1/transport/intercity-routes`, `/transport/intracity-routes`

### Şikayet / Dosya / Lookup
- `POST /v1/complaints` — anonim (opsiyonel); `GET /v1/complaints/my` `[A]`
- `POST /v1/files/upload` `[A]`, `DELETE /v1/files/{id}` `[A]`
- `GET /v1/neighborhoods`

---

## 11. Görünürlük Kuralları (mobilin bilmesi gerekenler)

- Public listelerde yalnız **onaylı/aktif + süresi geçmemiş** kayıtlar döner. Pending/pasif/süresi geçmiş kayıt public'e **404** (varlık sızıntısı yok).
- Kullanıcı kendi ilanını `GET /v1/users/me/ads` ile her statüde görür (pending/rejected dahil; `rejectedReason` red gerekçesini taşır).
- Kullanıcı düzenlemesi (`PUT /v1/ads/{id}`) ilanı **yeniden moderasyona** düşürür (status=pending).
