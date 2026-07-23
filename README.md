# KadirliApp — Backend & Web Admin Panel

Kadirli için mobil uygulamanın backend'i (REST API) ve web tabanlı yönetim panelidir.
**.NET 8** üzerine, **Clean Architecture** yaklaşımıyla geliştirilmiştir.

Bu rehber, projeyi sıfırdan bir bilgisayarda ayağa kaldırmak için gereken her şeyi adım adım anlatır.

---

## 1. Proje Yapısı

Solution 6 projeden oluşur:

| Proje | Görev |
|-------|-------|
| **KadirliApp.Api** | Mobil uygulamanın konuştuğu REST API (Swagger'lı) |
| **KadirliApp.Web** | Yöneticilerin kullandığı web admin panel (MVC/Razor) |
| **KadirliApp.Application** | İş kuralları, CQRS (komut/sorgu), doğrulamalar |
| **KadirliApp.Domain** | Alan modelleri (entity'ler), enum'lar — bağımlılıksız çekirdek |
| **KadirliApp.Infrastructure** | Veritabanı (EF Core), Redis, kimlik, dış servisler, migration'lar |
| **KadirliApp.Tests** | Otomatik testler |

Bağımlılık yönü: `Api`/`Web` → `Application` → `Domain`, altyapı ise `Infrastructure` üzerinden bağlanır.

---

## 2. Gereksinimler

Kurmanız gereken sadece iki şey var:

1. **.NET 8 SDK** (`8.0.x`) — https://dotnet.microsoft.com/download/dotnet/8.0
   - Doğrulama: terminalde `dotnet --version` → `8.0.x` görmelisiniz.
2. **Docker Desktop** — https://www.docker.com/products/docker-desktop
   - PostgreSQL, Redis ve Seq'i konteyner olarak çalıştırmak için. (Docker açık olmalı.)

> İsteğe bağlı: `dotnet-ef` aracı. **Gerekmiyor** — migration'lar uygulama başlarken otomatik uygulanıyor (bkz. 4. adım). Sadece elle migration üretmek isterseniz: `dotnet tool install --global dotnet-ef`

---

## 3. Altyapıyı Başlat (PostgreSQL + Redis + Seq)

Proje kök dizininde `docker-compose.yml` hazır. Docker Desktop açıkken:

```bash
docker compose up -d
```

Bu üç servisi ayağa kaldırır:

| Servis | Ne için | Adres |
|--------|---------|-------|
| PostgreSQL 15 | Ana veritabanı | `localhost:5432` (kullanıcı: `postgres` / şifre: `postgres` / db: `kadirliapp`) |
| Redis 7 | Önbellek + OTP saklama | `localhost:6379` |
| Seq | Merkezi log görüntüleyici (isteğe bağlı) | Arayüz: http://localhost:8081 |

Kontrol: `docker compose ps` → üçünün de `running` olduğunu görmelisiniz.

> **Seq zorunlu değil.** Loglar konsola ve dosyaya da yazılıyor; Seq sadece rahat görüntüleme içindir.

---

## 4. Veritabanı — Otomatik Kurulum

Elle migration çalıştırmanıza **gerek yok.** Hem API hem Web ilk açılışta:

1. `DbSeeder` üzerinden bekleyen tüm migration'ları uygular (`MigrateAsync`),
2. Panelin çalışması için gereken başlangıç verisini ekler:
   - Bir **super_admin** kullanıcısı,
   - Lookup (referans) tabloları.

Bu işlem **idempotent**'tir: veri zaten varsa tekrar eklemez. Yani projeyi kaç kez çalıştırırsanız çalıştırın sorun olmaz.

---

## 5. API'yi Çalıştır

Yeni bir terminalde, kök dizinde:

```bash
dotnet run --project KadirliApp.Api
```

- Swagger arayüzü: **http://localhost:5005/swagger**
- Hangfire (arka plan işleri) paneli: **http://localhost:5005/hangfire**

İlk `dotnet run` bağımlılıkları indirir (`restore`) ve derler; birkaç dakika sürebilir.

### API'yi Swagger'dan test etme (OTP giriş akışı)

Uygulama **geliştirme modunda (DevMode)** çalıştığı için:

- OTP (doğrulama kodu) SMS ile gönderilmez, **kod her zaman `123456`'dır.**
- Bir telefon numarasıyla OTP isteyip ardından `123456` ile doğrulayarak token alabilirsiniz.

---

## 6. Web Admin Paneli Çalıştır

**Ayrı bir terminalde** (API çalışırken):

```bash
dotnet run --project KadirliApp.Web
```

- Panel adresi: **http://localhost:5203**
- Açılışta doğrudan giriş sayfasına yönlendirir.

### Panel giriş bilgileri

| Alan | Değer |
|------|-------|
| Kullanıcı adı | `admin` |
| Şifre | `Admin123!` |

Giriş sonrası Dashboard'a düşersiniz. Panelden duyurular, işletmeler, etkinlikler, ilanlar, eczaneler, taksi/ulaşım, kullanıcı yönetimi vb. modüllere erişebilirsiniz.

---

## 7. Portlar — Özet

| Bileşen | HTTP adresi |
|---------|-------------|
| API | http://localhost:5005 (Swagger: `/swagger`) |
| Web Panel | http://localhost:5203 |
| Seq (log) | http://localhost:8081 |
| PostgreSQL | localhost:5432 |
| Redis | localhost:6379 |

> HTTPS profili de var (API `7035`, Web `7276`). İncelerken HTTP profili en pratiğidir.

---

## 8. Testleri Çalıştır

```bash
dotnet test KadirliApp.Tests
```

> Testler çalışan bir PostgreSQL ve Redis bekler (3. adımdaki `docker compose up -d` yeterli).

---

## 9. Sıfırdan Başa — Kısa Özet (kopyala-yapıştır)

```bash
# 1) Repoyu klonla
git clone <REPO_URL>
cd KadirliApp

# 2) Altyapıyı başlat (Docker Desktop açık olmalı)
docker compose up -d

# 3) API'yi çalıştır (1. terminal)
dotnet run --project KadirliApp.Api
#    -> http://localhost:5005/swagger

# 4) Paneli çalıştır (2. terminal)
dotnet run --project KadirliApp.Web
#    -> http://localhost:5203  (admin / Admin123!)
```

---

## 10. Sık Karşılaşılan Sorunlar

| Belirti | Çözüm |
|---------|-------|
| `dotnet: command not found` | .NET 8 SDK kurulu değil (2. adım). |
| Bağlantı hatası / DB'ye ulaşılamıyor | Docker Desktop açık mı? `docker compose ps` ile 3 servisi kontrol edin. |
| Port zaten kullanımda | 5432/6379/5005/5203'ü kullanan başka uygulamayı kapatın ya da ilgili portu değiştirin. |
| Panele giriş olmuyor | Kullanıcı adı `admin`, şifre `Admin123!` (büyük A + ünlem). |
| Migration/tablo hatası | Uygulamayı bir kez çalıştırmak migration'ları uygular; DB'yi tamamen sıfırlamak için: `docker compose down -v` sonra tekrar `up -d`. |
| İlk `dotnet run` çok yavaş | Normal — ilk seferde paketleri indirip derliyor. |

---

## 11. Yapılandırma Notları

- Bağlantı dizeleri ve anahtarlar `appsettings.json` içindedir ve **yalnızca yerel geliştirme içindir** (gerçek üretim sırları değildir).
- SMS/E-posta sağlayıcıları `Dev` modundadır; gerçek gönderim yapılmaz.
- OTP DevMode kodu: `123456`.
- Dosya yüklemeleri kök dizindeki `uploads/` klasörüne yazılır.

Keyifli incelemeler! 🚀
