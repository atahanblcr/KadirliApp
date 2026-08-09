# KadirliApp — 30 saniyelik giriş

**Ne bu proje?** Kadirli (Osmaniye) için şehir uygulaması: duyurular, nöbetçi eczane,
ilanlar, vefat, etkinlik, kampanya, taksi, ulaşım, elektrik kesintisi, şehir rehberi,
mekanlar, şikayet/istek. Üç parça: **.NET 8 API** + **Razor admin paneli** + **Flutter mobil**.

**Durum:** **Faz 11 bitti.** Backend + panel + mobil ayakta: 12 mobil modülün tamamı gerçek,
push canlı, golden + erişilebilirlik testleri var; panel gerçek bir yönetim paneli
(denetim izi · çöp kutusu · toplu işlem · sütun sıralaması · CSV dışa aktarma · global arama)
ve güvenlik kapanışı yapılmış (oturum iptali · zorunlu parola değişimi · parola politikası ·
hesap kilidi). Yayın hazırlığının Apple gerektirmeyen kısmı tamam.

**Şimdi Faz 12** — gözlem, alan modeli ve giriş kolaylığı; 9 alt-faz, **hepsi additive**
(hiçbir DTO alanı silinmiyor, hiçbir tablo düşürülmüyor). **12.1 bitti:** hata günlüğü modülü
(`ErrorLogsAdmin`). **12.2 bitti:** şüpheli giriş günlüğü — "kim, nereden, ne zaman girmeye
çalıştı?" artık panelden görülüyor (`LoginAttemptsAdmin`), `super_admin`'e kısılmış e-posta
uyarısı gidiyor, `ForwardedHeaders` kuruldu ve `StaffAdmin` izin tutarsızlığı düzeltildi.
**12.2b bitti:** bildirim teslim panosu — "duyuruyu yayınladım, gitti mi?" artık panelde
(`PushCampaignsAdmin`), duyuru oluşturmadan **tek seferlik push** atılabiliyor ve hedefleme
tek sahibe (`INotificationDispatcher`) çekildi.
**12.3 bitti:** kesinti artık sözlükteki mahalleye bağlı (`neighborhood_id` + `area_detail`,
idempotent geri doldurma) ve **kendiliğinden bildirim gönderiyor** — kesinti bildirimi bir
*duyurudur*, yani mobilde tek satır değişmeden mağazadaki eski sürümler de alıyor.
Ayrıca **12.2'den devralınan mobil çökmenin kök nedeni bulundu ve kilitlendi**
(kabuk rotasına `push` → mükerrer sayfa anahtarı; tek sahip `core/router/app_nav.dart`).
**12.4 bitti:** etkinlik artık sözlükteki bir **ilçeye** bağlı (`districts` + `Event.DistrictId`,
idempotent geri doldurma); `IsLocal` o bağdan **türetiliyor** ve `locationLabel` **sunucuda tek
yerde** üretiliyor. Mobilde kartta konum rozeti + **Kadirli · Osmaniye · Çevre iller** şeridi var —
"çevre iller" bir *sunucu* tanımı, istemci yalnız `?locationScope=nearby` diyor.
**12.5 bitti:** ulaşım alan modeli — hat artık bir **araç tipine** (`bus`/`minibus`) ve sözlükteki
bir **kalkış noktasına** bağlı, sefer de **hangi günler çalıştığını** söylüyor
(`OperatingDays`, Pazartesi=1 … Pazar=64). 🔴 Uç seferleri günlere göre **elemiyor**, yalnız
bildiriyor — mağazadaki eski sürümler için liste sebepsiz boşalmasın diye; migration mevcut
satırlara `bus` + `127` yazdı, yani **davranış birebir korundu**.
**843 backend + 703 mobil test, 48 görünmez sözleşme.**

**⏭️ Sırada 12.6:** ulaşım mobil (ikili kalkış · gün rozetleri · "sıradaki sefer").
Plan: `Memory_Bank/Progress.md` → "FAZ 12".

> 🔑 **Panel süper admin parolası** `secrets/panel-admin.json`'dadır (git'e girmez; biçim ve
> davranış: `secrets/README.md`). Dosya varsa açılışta parola ona **hizalanır** — "parola neydi?"
> sorusu artık kaynağa değil o dosyaya sorulur.

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
listelenen 48 bağımlılık bozulduğunda kimse hata almaz — mobil sadece sessizce yanlış
davranır. Hepsi testle kilitli: 1–22 `InvisibleContractsTests.cs`, 23–26 `PanelBusinessRuleTests.cs`,
27 `PanelPowerOutageFilterTests.cs`, 28 `PanelTrashTests.cs`,
29 `PanelBulkActionTests.cs`, 30 `PanelSortingTests.cs`,
31–33 `PanelErrorLogTests.cs` + `Unit/Application/Observability/`,
34–36 `PanelLoginAttemptTests.cs` + `Unit/Application/Security/`,
37–39 `PanelPushCampaignTests.cs` + `Unit/Application/Notifications/`,
40–42 `PanelPowerOutageNeighborhoodTests.cs` + `Unit/Application/PowerOutages/`,
43–45 `PanelEventDistrictTests.cs` + `Unit/Application/Events/`,
46–48 `PanelTransportFieldModelTests.cs` + `Unit/Application/Transport/`.

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
   **Yalnız admin'e açık ekranda** (Personel, Denetim İzi, Çöp Kutusu, Hata Kayıtları,
   Giriş Denemeleri, Bildirim Gönderimleri) desen farklıdır: rol listesinde `moderator` **yok**, `[PanelPermission]`
   **yok**, menü satırının `Module`'ü **`null`** ve controller adı `AdminOnlyControllers`'ta —
   aksi hâlde izin matrisinde *karşılığı olmayan* bir yetki belirir (`ARCHITECTURE.md` §3).
   ✅ **12.2'de yapısal testle kilitlendi** (`AdminOnlyControllers_AreOutsideThePermissionMatrix`);
   `StaffAdmin`'in bilinen ihlali aynı fazda düzeltildi ve ölü izinler migration'la temizlendi.
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
