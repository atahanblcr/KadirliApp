# KadirliApp — 30 saniyelik giriş

**Ne bu proje?** Kadirli (Osmaniye) için şehir uygulaması: duyurular, nöbetçi eczane,
ilanlar, vefat, etkinlik, kampanya, taksi, ulaşım, elektrik kesintisi, şehir rehberi,
mekanlar, şikayet/istek. Üç parça: **.NET 8 API** + **Razor admin paneli** + **Flutter mobil**.

**Durum:** Backend ve panel bitti (Faz 0–10). Mobil 11.15c'ye kadar bitti — 12 modülün
tamamı gerçek, push canlı, golden + erişilebilirlik testleri ayakta, panel/önbellek/moderasyon
emniyet ağı kuruldu, panelin canlı denetiminde bulunan hataların tamamı (11.15c A grubu)
düzeltildi. **11.17 ile panel gerçek bir yönetim paneli oldu**: şehirlerarası ulaşım (tek
işlevsel boşluktu), denetim izi, çöp kutusu, kesinti filtresi. **11.18 ile panelin güvenlik
kapanışı yapıldı** (oturum iptali · ilk girişte zorunlu parola değişimi · parola politikası ·
hesap kilidi) **ve toplu işlem + sütun sıralaması geldi** (534 backend + 669 mobil test).
**Sırada yayın (11.16); 11.18'den kalan: CSV dışa aktarma · global arama · bağımsız push ekranı.**

## Çalıştır

```bash
docker compose up -d                          # Postgres · Redis · Seq
dotnet run --project KadirliApp.Api           # http://localhost:5005  (Swagger: /swagger)
dotnet run --project KadirliApp.Web           # admin paneli
cd mobile && flutter pub get && flutter run   # mobil (Android emülatörü / iOS simülatörü)
```

Mobil base URL: Android emülatörü `10.0.2.2:5005`, iOS simülatörü `localhost:5005`,
gerçek cihaz `--dart-define=API_BASE_URL=http://<LAN-IP>:5005`.

## Denetle (her oturum sonunda yeşil olmalı)

```bash
dotnet test KadirliApp.Tests                  # Docker açık olmalı (Testcontainers)
cd mobile && flutter analyze && flutter test
```

Golden (görsel regresyon) testleri `flutter test` içinde koşar. Bilerek düzen
değiştirdiyseniz `flutter test --update-goldens test/golden` ile referansları
yenileyin ve **PNG farkını gözle inceleyin** — ayrıntı `mobile/README.md`.

## Hangi dokümanı ne zaman okumalı

| Soru | Dosya |
|---|---|
| **"Neyin nerede? Nasıl modül eklerim/değiştiririm/kaldırırım?"** | **`ARCHITECTURE.md`** ← harita, önce buraya bak |
| "Mobil istemci sunucuyla nasıl konuşuyor?" | `Memory_Bank/API_CONTRACT.md` |
| "Bu karar neden böyle verilmiş?" | `Memory_Bank/Progress.md` (faz faz) · `Memory_Bank/Active_Context.md` (son durum) |
| "Bu .NET kalıbı ne demek?" | `DOTNET_MASTERCLASS.md` |
| "Mobil tasarım sistemi / UX kuralları?" | `Memory_Bank/MOBILE_UX_PLAN.md` |
| "Uçların makine-okur şeması?" | `docs/openapi.json` |
| "Mobil kurulum / canlı doğrulama komutları?" | `mobile/README.md` |
| "Kod review istiyorum, nelere dikkat edilmeli?" | `CODE_REVIEW_CHECKLIST.md` |

⚠️ **`ARCHITECTURE.md` §7 "Görünmez sözleşmeler"i okumadan backend'e dokunma.** Orada
listelenen 30 bağımlılık bozulduğunda kimse hata almaz — mobil sadece sessizce yanlış
davranır. Hepsi testle kilitli: 1–22 `InvisibleContractsTests.cs`, 23–26 `PanelBusinessRuleTests.cs`,
27 `PanelPowerOutageFilterTests.cs`, 28 `PanelTrashTests.cs`,
29 `PanelBulkActionTests.cs`, 30 `PanelSortingTests.cs`.

## Değişmez kurallar

1. **Katman yönü** `Domain ← Application ← Infrastructure ← Api/Web`. Yanlış yön
   **derlenmez** (proje referanslarıyla zorlanmış) — disiplin meselesi değil.
2. **Kontrat additive.** DTO'ya alan eklemek serbest; alan silmek/yeniden adlandırmak
   mağazadaki eski sürümleri kırar → sürüm planı gerekir (`ARCHITECTURE.md` §5).
3. **Public uç yalnız yayınlanmış içerik döndürür**: onaylı + aktif + silinmemiş + süresi
   geçmemiş. Filtreyi controller'da zorla, DTO'dan gelene güvenme.
4. **Panel uçları** `AdminApiControllerBase`'den türer ve `[RequirePermission(modül, eylem)]`
   taşır. (Yapısal test bunu denetliyor.) **Razor panelinde** karşılığı
   `[Authorize(Roles = "admin,super_admin,moderator")]` + `[PanelPermission("<modül>")]` +
   `PanelMenu.Items` satırıdır — üçü aynı modül anahtarını kullanır.
   **Yalnız admin'e açık ekranda** (Personel, Denetim İzi, Çöp Kutusu) desen farklıdır:
   rol listesinde `moderator` **yok**, `[PanelPermission]` **yok**, menü satırının `Module`'ü
   **`null`** ve controller adı `AdminOnlyControllers`'ta — aksi hâlde izin matrisinde
   *karşılığı olmayan* bir yetki belirir (`ARCHITECTURE.md` §3).
5. **"İşlevsiz buton yok"** — mobilde her buton bir uca ya da bir ekrana gider.
   Modül kaydı tek yerde: `mobile/lib/core/navigation/app_modules.dart`.
6. **Arayüz Türkçe**, kod ve kimlikler İngilizce. Kullanıcıya teknik/İngilizce hata
   mesajı gösterilmez. **Panelde** durum/rol asla ham basılmaz — `PanelDisplay.Status()` /
   `.Role()` + `_StatusBadge` partial'ı kullanılır; para `PanelDisplay.TL()`'den geçer
   (panel `InvariantCulture`'a sabit olduğu için `ToString("C2")` `¤` basar).
7. **Sırlar commit edilmez**: `secrets/`, `google-services.json`, `GoogleService-Info.plist`
   `.gitignore`'da. `secrets/README.md` neyin nasıl edinileceğini anlatır.
8. **Oturum sonunda**: `dotnet test` + `flutter analyze` + `flutter test` yeşil,
   `Memory_Bank/Progress.md` ve `Active_Context.md` güncel, commit atılmış.

## Yeni bir modül mü ekleyeceksin?

`ARCHITECTURE.md` §4'teki 18 adımlı reçeteyi sırayla uygula. Son adımı atlamayın:
modülü **`ARCHITECTURE.md` tablosuna yazmadan** `dotnet test` yeşile dönmez
(`ArchitectureDocTests` dokümanı gerçekle karşılaştırıyor — doküman bilerek çürüyemiyor).
