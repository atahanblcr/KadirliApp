# ⚡ Performans taban çizgisi (Faz 12.22)

> 🔑 **12.22'nin başarı ölçütü bir hız değil, bir CÜMLEDİR.** 16 Ağustos 2026'da o cümle
> hiçbir yerde yoktu. Bugün var:
>
> ## > En sıcak altı public ucun **p95'i 14–19 ms**, hata oranı **%0,00** — 50 eşzamanlı kullanıcı, 2 dakika, 100.643 istek/uç.
>
> Bu dosyanın işi o cümleyi **ölçüm koşullarıyla birlikte** saklamaktır: taban çizgisi
> olmadan bir sonraki oturumun *"iyileştirdim"* iddiası ölçülemez, yani bu projenin kabul
> etmediği türden bir iddia olur.

**Ölçüm tarihi:** 19 Ağustos 2026 · **Ölçen:** `perf/baseline.js` (k6 v2.2.0)

---

## 1. Taban çizgisi — en sıcak uçlar

**Koşul:** 50 VU · 2 dk · `KadirliApp.Api` **Release** · Postgres 15 (Docker) · Redis 7 ·
Apple Silicon, API ve veritabanı **aynı makinede** (ağ gecikmesi ≈ 0).

| Uç | istek | p50 | **p95** | p99 | max |
|---|---:|---:|---:|---:|---:|
| `/v1/announcements?page=1&limit=20` | 100.643 | 10,0 ms | **19,2 ms** | 27,9 ms | 110,8 ms |
| `/v1/events?page=1&limit=20` | 100.643 | 9,9 ms | **19,0 ms** | 27,5 ms | 83,9 ms |
| `/v1/ads?page=1&limit=20` | 100.643 | 9,9 ms | **19,0 ms** | 27,5 ms | 104,2 ms |
| `/v1/news?page=1&limit=20&search=…` | 100.643 | 8,1 ms | **17,9 ms** | 26,7 ms | 83,0 ms |
| `/v1/news?page=1&limit=20` | 100.643 | 8,0 ms | **17,8 ms** | 26,4 ms | 85,5 ms |
| `/v1/power-outages` *(sayfalama YOK)* | 100.643 | 6,4 ms | **13,9 ms** | 21,4 ms | 73,1 ms |

**Hata oranı: %0,00.** Veri hacmi: 180 haber · 5 ilan · 4 duyuru · 5 etkinlik · 3 kesinti.

### Handler tarafı (aynı koşuda, panel → Performans)

| Handler | çağrı | ort. | p95 | max | yavaş | hata |
|---|---:|---:|---:|---:|---:|---:|
| `GetNewsQuery` | 459.322 | 5,5 ms | ≤25 ms | 220 ms | 0 | 0 |
| `GetAnnouncementsQuery` | 229.661 | 6,5 ms | ≤25 ms | 211 ms | 0 | 0 |
| `GetAdsQuery` | 229.661 | 6,4 ms | ≤25 ms | 189 ms | 0 | 0 |
| `GetEventsQuery` | 229.661 | 6,3 ms | ≤25 ms | 251 ms | 0 | 0 |
| `GetPowerOutagesQuery` | 229.661 | 3,2 ms | ≤10 ms | 175 ms | 0 | 0 |

🔑 **İki ölçüm arasındaki fark bilgi taşır:** k6 *dışarıdan* (HTTP + Kestrel + middleware),
panel *içeriden* (yalnız handler) ölçer. Sıcak uçlarda handler ~6 ms, HTTP ~19 ms → aradaki
**~13 ms boru hattı + ağ + serileştirmedir**. 🔴 **Yavaş handler sayısı 0** (eşik 500 ms).

⚠️ Panelin `≤` işareti gerçek: yüzdelikler kovalıdır ve **gerçeğin üstünü** söyler
(bkz. §7 madde 83). Bu ölçümde tam olarak görüldü — k6 19,2 ms derken panel "≤25 ms" dedi.

---

## 2. 🔴 Ölçmeden önce bilinmesi gereken iki şey

Bu ikisi **ölçüm sırasında bulundu** ve ikisi de ölçümü **sessizce yalancı** yapıyordu.

### (a) Hız limiti ölçümün önünde durur

API'de IP başına global bir hız limiti var (`RateLimiting:Global:PermitLimit`, **300/60 sn**).
Yük üreticisi **tek bir IP**'dir → limit ilk saniyede dolar, kalan iki dakika **429** döner.
🔴 **Ve 429 hızlı döndüğü için tablo ÇOK İYİ görünür:** ilk koşu p95 = **1,7 ms** yazdı.
Ölçülen şey uygulama değil, hız limitinin kendisiydi.

```bash
RateLimiting__Global__PermitLimit=100000000 dotnet run --project KadirliApp.Api -c Release
```

`perf/baseline.js` artık `rate_limited` metriğiyle bunu **kırmızıya** düşürüyor ve çıktıya
komutu yazıyor — bir sonraki oturum sessizce yanlış bir taban çizgisine güvenemez.

### (b) Sayaçları ölçümden önce sıfırlayın

Panel → **Performans → Sayaçları sıfırla.** Açılıştan beri biriken sayaçlar migration'ı ve
ilk isteklerdeki JIT ısınmasını da p95'e karıştırır.

---

## 3. Ölçek sondaları — "ne zaman sorun olur?"

### `/v1/power-outages` — sayfalanmayan uç (görünmez sözleşme #1)

Sentetik satırlarla ölçüldü, sonra **tamamı silindi**.

| satır | p50 | p95 | **yanıt gövdesi** |
|---:|---:|---:|---:|
| 3 *(bugün)* | 1,0 ms | 1,6 ms | **1 KB** |
| 500 | 1,9 ms | 3,0 ms | 187 KB |
| 2.000 | 3,8 ms | 4,5 ms | 751 KB |
| 5.000 | 8,3 ms | 10,4 ms | 1,8 MB |
| 10.000 | 15,0 ms | 21,0 ms | 3,7 MB |
| 20.000 | 31,1 ms | 39,3 ms | **7,5 MB** |

🔑 **Sonuç ve bu sonuç plandaki beklentiyi DÜZELTİYOR:** darboğaz sorgu değil, **gövde**.
20.000 satırda sunucu tarafı hâlâ **31 ms** (`EXPLAIN`: 10 ms veritabanı, gerisi
materyalizasyon + JSON) — ama vatandaşın mobil bağlantısına inen şey **7,5 MB**.

🔴 **Bu yüzden çözüm cache DEĞİL:** cache sunucu zamanını düşürür, gövdeyi düşürmez.
Ölçüm tek bir çözümü işaret ediyor: **tarih penceresi.**

🔴 **Ve bu bir KONTRAT kararıdır, kod kararı değil — bu fazda VERİLMEDİ.** Sebep ölçüldü:
mobil istemci listede **geçmiş kesintileri de gösteriyor** (`PowerOutageStatus.past` →
*"Sona erdi"*, `power_outage_tile.dart`). Yani sunucuda bir tarih penceresi açmak, mağazadaki
eski sürümlerde **görünen bir davranış değişikliğidir**. Karar sahibi ürün tarafıdır;
ölçüm hazır, karar açık (bkz. Progress.md açık maddeler panosu).

📌 **`start_time` indeksi EKLENMEDİ ve bu da ölçüldü:** sorgu tabloyu **tamamen** sıralı
okuyor; `EXPLAIN` paralel `Seq Scan` + `quicksort` gösteriyor ve bu, tam okuma için zaten
en iyi plan. Bir indeks eklemek yer kaplar, her yazmada güncellenir ve **kullanılmaz** —
tam olarak §7 madde 84'ün cezalandırdığı şey.

### Haber senkronunun maliyeti (12.22b/6)

**102 yeni haberlik gerçek bir arşiv koşusu ölçüldü** (19 Ağu 2026):

| Ölçüt | Değer |
|---|---|
| Kaynak liste isteği | **3** (2 × `posts` + 1 × `categories`) |
| İndirilen görsel | **178** (1,75 görsel/haber) |
| İndirilen hacim | **32,5 MB** (ortalama **187 KB**/görsel) |
| Süre | ~2 dakika |

🔬 **Tam arşiv (27.284 haber) için çıkarım — ve bu, dokümandaki tahmini DÜZELTİYOR:**

| | Dokümanda yazan | **Ölçüme dayalı** |
|---|---|---|
| İstek sayısı | ~273 | **~273** ✅ |
| Görsel hacmi | ~1,6 GB | **~8,9 GB** 🔴 |

🔑 **Sebep bir tahmin hatası değil, bir tasarım değişikliği:** ~1,6 GB tahmini yalnız
**kapak** görselleri aynalanırken yapılmıştı; **12.14b metin arası görselleri de aynalamaya
başladı** ve hacmi ~5,5× büyüttü. Tahmin o gün doğruydu, bugün değil — ve bunu hiçbir şey
söylemiyordu. *"Arşiv derinliği 50 → tamamı"* kararının ön koşulu buydu (Progress.md B).

---

## 4. Bozma turu — ölçüm gerçekten ölçüyor mu? (12.22c)

| # | Bozma | Beklenen | **Ölçülen** | Sonuç |
|---|---|---|---|---|
| 1 | `Pagination.MaxLimit` 50 → 5000 | p95 belirgin bozulmalı | `?limit=5000` · p50 **3,4 → 40,2 ms** (11,8×), gövde **17 KB → 1,66 MB** (97×) | 🟢 |
| 2 | Üç haber GIN/trigram indeksi düşürüldü | fark görünmeli | 30.180 satırda `BitmapOr` **6,8 ms** → `Seq Scan` **46,2 ms** (6,8×) | 🟢 |
| 3 | `ix_ads_title_trgm` ölü hâline döndürüldü | `TrigramIndexTests` kırmızı | kırmızı (ilk denemede) | 🟢 |
| 4 | k6 trend'i init bağlamı dışında | koşu kırmızı olmalı | **ilk yazımda YEŞİL görünüyordu** → `checks` eşiği eklendi | 🐛→🟢 |

🐛 **Dördüncüsü benim hatamdı ve en öğreticisi:** k6 metrikleri yalnız init bağlamında kabul
ediyor; senaryo 2 dakika koştu, **5,4 milyon yineleme "tamamladı"**, tablo bomboş çıktı ve
koşu *başarılı* göründü. Ölçüm altyapısının kendi sessiz hatası. Artık `checks: ['rate>0.99']`
ve *"satır yoksa bu bir taban çizgisi DEĞİLDİR"* kapısı var.

🔑 **1 ve 2 birlikte bir şey söylüyor:** senaryo gerçekten zorluyor. Bozulmasaydı ölçüm
yalancı olurdu ve bir sonraki oturum ona güvenip yanlış karar verirdi.

---

## 5. Kararlar — ölçümün gösterdiği, ve göstermediği

| Karar | Gerekçe |
|---|---|
| 🟢 **Sıcak liste uçları CACHE'SİZ KALIYOR** | p95 **19 ms** ve yavaş handler **0**. Plan zaten *"kabul edilebilirse öyle kalsın"* diyordu; cache eklemek §7 madde 22'yi (grup adı + invalidate eden komut) **ölçülmemiş bir kazanç** için borçlanmak olurdu. Panelde güncellenen veri mobilde sessizce eski kalır — bedel gerçek, kazanç ölçülmemiş |
| 🟢 **`ix_ads_title_trgm` + `ix_places_name_trgm` DÜZELTİLDİ** | Ölü indekslerdi (ham kolon ↔ `lower(...)` sorgu). 20.005 satırda **29,2 ms → 0,75 ms (39×)**. §7 madde **84** + `TrigramIndexTests` |
| 🟡 **14 arama sorgusunda hâlâ trigram indeksi YOK** | Rehber · vefat · taksi · ulaşım · işletme · global arama · hata kayıtları. **Ölçülmedi** → eklenmedi. Ölü bir indeksi düzeltmek bir *hata düzeltmesidir*; olmayan bir indeksi eklemek bir *karardır* ve bu faz ölçülmemiş kararı reddediyor |
| 🟡 **`/v1/power-outages` tarih penceresi: KARAR AÇIK** | Ölçüldü (yukarıda). Kontrat kararı — mobil geçmiş kesintileri gösteriyor |
| 🔴 **`start_time` indeksi EKLENMEDİ** | `EXPLAIN` tam okumada seq scan + sort'un zaten en iyi plan olduğunu gösterdi |

📌 **Bilinçli kapsam dışı (plandaki gibi):** dağıtık cache · okuma replikası · CDN.
Hiçbiri şehir ölçeğinde gerekçelendirilemez.

---

## 6. Bu ölçümü tekrarlamak

```bash
docker compose up -d
RateLimiting__Global__PermitLimit=100000000 dotnet run --project KadirliApp.Api -c Release
dotnet run --project KadirliApp.Web                        # panel (ölçümü birleştirir)
# panel → Performans → "Sayaçları sıfırla"
k6 run -e VUS=50 -e DURATION=2m perf/baseline.js
# panel → Performans  (handler tarafı)
```

⚠️ **Karşılaştırırken koşulları karşılaştırın.** Yukarıdaki sayılar *aynı makinede*
ölçüldü; gerçek dağıtımda ağ, TLS ve reverse proxy eklenir. Taban çizgisinin işi mutlak
bir hız vaat etmek değil, **aynı koşulda yeniden ölçülebilir bir referans** olmak.
