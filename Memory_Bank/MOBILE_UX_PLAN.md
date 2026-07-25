# KadirliApp — Mobil UI/UX & Tasarım Planı (Flutter)

> **Amaç:** Mobil arayüzün tek referansı. `API_CONTRACT.md` "hangi veri" sorusunu, bu doküman "nasıl görünür/hissettirir ve backend'e göre nasıl davranır" sorusunu cevaplar.
> **Yönlendiren kararlar (kullanıcı, 25 Tem 2026):** His = **Sıcak & topluluk**, ana renk = **doğa yeşili**, navigasyon = **alt sekme + ana sayfa hub** + **sağ üstte Ayarlar/Kontrol butonu**.
> Bu bir *yaşayan taslaktır* — kesin hex tonları ve ikon seti uygulama başında rafine edilecek.

---

## 0. Tasarım İlkeleri (her karar bunlara uymalı)

1. **Basitlik önce.** Her yaştan Kadirlili kullanacak → az adım, büyük dokunma alanı (min 48dp), net etiketler, jargon yok.
2. **İçerik kral, krom minimum.** Süsleme değil, bilgi öne çıksın (nöbetçi eczane, duyuru, ilan). Ekranda "ne yapmalıyım" hep belli.
3. **Sıcak ama güvenilir.** Yumuşak köşeler, sıcak nötr tonlar, doğa yeşili; ciddiyeti bozmayan samimiyet.
4. **Tutarlılık.** Aynı bileşen her yerde aynı davranır (kart, buton, liste, boş/hata durumu).
5. **Az ama anlamlı hareket.** Animasyon dikkat yönlendirir, gösteriş yapmaz; eski telefonlarda akıcı kalır.
6. **Erişilebilirlik.** Kontrast AA (≥4.5:1 metin), font ölçeklenebilir, yalnız renge güvenme (ikon+etiket birlikte).

---

## 1. Renk Paleti (doğa yeşili + sıcak nötrler)

> Placeholder tonlar — marka netleşince ince ayar yapılır. Hepsi açık+koyu tema için verildi. Flutter'da `ColorScheme` + tema token'ları olarak tanımlanacak.

### Açık tema
| Rol | Hex | Kullanım |
|---|---|---|
| **Primary (yeşil)** | `#2C7A57` | Ana aksiyon, aktif sekme, vurgular, linkler |
| Primary-dark (pressed) | `#215B41` | Basılı/hover durumu |
| Primary-container (tint) | `#E8F3EC` | Seçili arka plan, rozet zemini, yumuşak vurgu |
| **Accent (sıcak)** | `#E08A3C` | İkincil CTA / "acil" vurgusu (az kullan — göz yormasın) |
| Background | `#FAF9F6` | Ekran zemini (sıcak kırık-beyaz, saf beyaz değil) |
| Surface (kart) | `#FFFFFF` | Kartlar, sheet, appbar |
| Border/Divider | `#E7E4DD` | İnce ayraçlar, kart kenarı |
| Text-strong | `#1E2A24` | Başlık, önemli metin |
| Text-muted | `#5C6B63` | İkincil metin, açıklama, tarih |

### Koyu tema
| Rol | Hex |
|---|---|
| Primary | `#46B083` (kontrast için açıldı) |
| Background | `#121815` · Surface `#1B2420` · Border `#2A352F` |
| Text-strong `#ECF1EE` · Text-muted `#9DB0A6` |

### Anlamsal (semantic) renkler
| Durum | Açık | Kullanım |
|---|---|---|
| Success | `#2E8B57` | Onaylandı, başarı toast |
| Info | `#2F6FB0` | Bilgi, nötr durum |
| Warning | `#E0A32E` | Uyarı (ör. atanmamış nöbet) |
| Danger | `#D64545` | Hata, sil, ban, reddedildi |

> **Kural:** Modül renk kodlaması yapılacaksa (ör. kart şeridi) bu paletten türetilmeli; rastgele renk yok.

---

## 2. Tipografi

- **Font ailesi:** Türkçe diakritikleri (ç ğ ı ş ö ü) tam destekleyen, sıcak-yuvarlak bir sans → **Nunito** (öneri) veya sistem varsayılanı. Tek aile, ağırlıklarla ayrış.
- **Ölçek (mobil):** Display 28 / H1 22 / H2 18 / Body 16 / Body-sm 14 / Caption 13 / Label 12. Satır yüksekliği ~1.4.
- **Ağırlık:** Başlık = SemiBold (600), gövde = Regular (400), vurgu = Bold (700). İnce (300) kullanma (okunurluk).
- **Kural:** Ekranda en fazla 2-3 boyut aynı anda; büyük gövde (16) — küçültme.

---

## 3. İkonografi

- **Stil:** Yuvarlak köşeli, **outline (çizgi)** varsayılan + **aktif/seçili durumda dolu (filled)**. Tek set (öneri: Material Symbols Rounded veya Phosphor).
- **Modül ikon haritası (öneri):**
  | Modül | İkon | | Modül | İkon |
  |---|---|---|---|---|
  | İlanlar | 🏷️ tag | | Etkinlikler | 🎉 event |
  | Duyurular | 📢 campaign | | Kampanyalar | 🎟️ ticket |
  | Nöbetçi Eczane | 💊 pill | | Mekanlar | 📍 pin |
  | Vefat | 🕊️ (saygılı, sade) | | Şehir Rehberi | 🗺️ map |
  | Taksiciler | 🚕 taxi | | Elektrik Kesintisi | ⚡ bolt |
  | Ulaşım | 🚌 bus | | Şikayet/İstek | 📝 note |
  | Bildirimler | 🔔 bell | | Ayarlar | ⚙️ gear |
- **Kural:** İkon tek başına asla — her zaman metin etiketiyle (yaş kitlesi geniş).

---

## 4. Hareket / Animasyon Dili (az & anlamlı)

| Yer | Hareket | Süre / eğri |
|---|---|---|
| Sayfa geçişi | Slide + hafif fade | 220ms · ease-out |
| Buton basma | Scale 0.98 + renk koyulaşma | 120ms |
| Liste yükleme | **Skeleton shimmer** (spinner değil) | döngü |
| Yenileme | Pull-to-refresh (yeşil indicator) | native |
| Toast/snackbar | Alttan kayarak gir/çık | 200ms |
| Rozet/sayaç | Sayı değişince kısa "pop" | 150ms |
| Boş→dolu | İçerik hafif yukarı-fade | 200ms |

**Yapma:** otomatik oynayan büyük animasyonlar, uzun splash, gereksiz parallax. **İlke:** kullanıcı beklerken skeleton görür, asla boş beyaz ekran değil.

---

## 5. Navigasyon & Bilgi Mimarisi

### Alt sekme (4 sabit)
```
🏠 Ana Sayfa   |   🏷️ İlanlar   |   🔔 Bildirimler(rozet)   |   👤 Profil
```
- **İlanlar** ayrı sekme çünkü en aktif/pazar modülü.
- **Bildirimler** okunmamış sayısı rozetle (`GET /v1/notifications` → `data.unreadCount`).

### Ana Sayfa (Hub)
```
┌──────────────────────────────┐
│ Merhaba, Ahmet 👋      ⚙️     │  ← sağ üstte AYARLAR/KONTROL (kullanıcı isteği)
│ ┌──────────────────────────┐ │
│ │ 🟠 ACİL ŞERİDİ           │ │  ← Nöbetçi eczane (bugün) + aktif kesinti
│ │ 💊 X Eczanesi  ·  ⚡ yok │ │
│ └──────────────────────────┘ │
│ Modüller                     │
│ [🏷️][📢][💊][🕊️]           │  ← kart/ikon ızgara, dokun→modül
│ [🎉][🎟️][📍][🚕] ...        │
│ ── Öne çıkanlar ──           │
│ • Son duyuru / yeni ilanlar  │
└──────────────────────────────┘
```

### ⚙️ Ayarlar / Kontrol Ekranı (sağ üst buton → **kullanıcının özel isteği**)
Uygulamanın "kontrol merkezi". İçerik → backend uçları:
| Bölüm | Backend |
|---|---|
| **Profil** (ad, mahalle, foto düzenle) | `GET/PATCH /v1/users/me` (username & mahalle 30 günde bir) |
| **Bildirim tercihleri** (duyuru/vefat/eczane/etkinlik/ilan/kampanya aç-kapa) | `PATCH /v1/users/me/notifications` |
| **Tema** (Açık / Koyu / Sistem) | client-side (kalıcı) |
| **Hesap** — Çıkış yap | `POST /v1/auth/logout` (refresh iptal + FCM temizle) |
| **Hesap** — Hesabı sil | `DELETE /v1/users/me` (yalnız normal kullanıcı) |
| **Hakkında** — sürüm, gizlilik, iletişim/şikayet | `POST /v1/complaints` |
> Bu ekran ileride "uygulama kontrolü" için genişleyebilir (dil, veri kullanımı vb.) — mimari buna açık.

---

## 6. Modül Ekran Deseni (hepsi aynı iskelet)

**Liste ekranı:** başlık + (varsa) arama/filtre → kart listesi → pull-to-refresh → sonsuz kaydırma (`?page=&limit=`, public max 50). Backend'in desteklediği yerde filtre UI'a yansır: İlanlar `?sort=` (yeni/eski/fiyat) + arama, Duyurular `?typeId=`, Eczane tarih seçici.

**Detay ekranı:** hero görsel (varsa) → bilgiler → aksiyonlar. Aksiyonlar modüle göre: İlan → favori (`/favorite`), ara (`/track-phone`), WhatsApp (`/track-whatsapp`); Taksi → **Ara** (`/drivers/{id}/call`); Kampanya → **Kodu gör** (`/view-code`); Duyuru detayı açılınca `/view`, linke tıklayınca `/click`.

**Kullanıcının kendi içeriği:** "İlanlarım" (`/users/me/ads`) her statüde; reddedilmişse `rejectedReason` kırmızı bilgi kartında gösterilir; düzenleme yeniden moderasyona düşürür (kullanıcı uyarılır).

---

## 7. Ortak Durumlar (her ekranda tanımlı olmalı)

| Durum | Görsel |
|---|---|
| **Yükleniyor** | Skeleton kart (spinner değil) |
| **Boş** | Dostane ikon + "Henüz kayıt yok" + (varsa) aksiyon |
| **Hata** | Sıcak mesaj + "Tekrar dene" butonu; teknik detay yok |
| **Offline** | Üstte ince şerit "İnternet yok"; önbellek varsa göster |
| **Yetki gerekli** | Anonim kullanıcı korumalı aksiyona basınca → nazik "Giriş yap" yönlendirmesi |

---

## 8. Backend'e Göre Davranış (istemci kuralları)

1. **Zarf:** her yanıt `{success, data, meta}` → istemci daima `data`'yı açar; `success:false` veya HTTP≥400 → hata akışı. (⚠️ `GET /announcements/{id}` NOT_FOUND'u HTTP 200+`success:false` döner — bu ucta `success` kontrolü şart.)
2. **Hata gösterimi:** `error.code`'a göre kullanıcı mesajı (sözlük `API_CONTRACT.md`'de); `RATE_LIMITED` → "biraz sonra tekrar deneyin"; `VALIDATION_ERROR` → alan altı uyarı.
3. **Token:** access ile çağır → 401 `UNAUTHORIZED` gelince `refresh` (jti rotasyonu → yeni refresh'i sakla) → o da olmazsa login akışı. Token'lar güvenli depoda (secure storage).
4. **Görsel URL:** göreli `/uploads/...` → istemci API origin'i ekler (prod'da mutlak gelirse olduğu gibi kullan).
5. **Sayfalama:** `PagedResult{items,totalCount,pageSize,currentPage,totalPages}` → sonsuz kaydırma; `currentPage < totalPages` iken devam.
6. **Push (FCM):** giriş sonrası `POST /v1/notifications/fcm-token`; push `data`'sındaki `notificationId`/`relatedType`/`relatedId` ile ilgili ekrana **deep-link** + bildirimi okundu (`PATCH /notifications/{id}/read`).
7. **Sayaçlar:** ilan detayı açılınca telefon/WA tıklaması `/track-*`; duyuru açılınca `/view`, linke `/click` — sessiz (kullanıcı görmez).
8. **Tarih:** UTC gelir → cihaz saatine (Europe/Istanbul) çevir; "2 saat önce" gibi göreli format tercih.

---

## 9. Sıradaki Adımlar (bu plan onaylanınca)

1. Bu dokümanı onayla / ince ayar (renk tonu, navigasyon, ikon seti).
2. **Görsel önizleme:** istenirse renk paleti + örnek ekranların tıklanabilir mockup'ı (Artifact) üretilir — "kağıt üzerinde" görmek için.
3. Flutter proje iskeleti + tema token'ları + ortak bileşen kütüphanesi (buton/kart/durumlar).
4. Modül modül ekranlar (önce Auth+Ana Sayfa+Ayarlar, sonra en aktif modüller: İlanlar, Duyurular, Nöbetçi Eczane).
