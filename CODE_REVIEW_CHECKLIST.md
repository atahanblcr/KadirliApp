 # KadirliApp — Code Review Checklist

> Bu liste jenerik bir kurumsal şablon değil; `ARCHITECTURE.md`, `Memory_Bank/API_CONTRACT.md`,
> `Memory_Bank/Active_Context.md` ve `Memory_Bank/Progress.md`'den (Faz 10–11.15c) çıkarılmıştır.
> Özellikle **"Görünmez Sözleşmeler" (§7)** ve **tekrarlayan hata sınıfları** buraya birebir
> taşınmıştır — bu proje aynı hatayı birden fazla kez üretmiş ve her seferinde not düşülmüş,
> bu checklist o dersleri PR aşamasına çekmek için var.
>
> Kullanım: yeni bir PR'da ilgili bölümü (Backend/Panel/Mobil/DB) süz, uygulanabilir maddeleri
> işaretle. Her satırdaki kod referansı "nerede kontrol edilir"i gösterir.

---

## 1. Genel / Mimari

| Kural | Açıklama | Referans |
|---|---|---|
| Katman yönü ihlal edilmemiş mi? | `Domain ← Application ← Infrastructure ← Api/Web`. `Application` içinden `DbContext`'e dokunulmamalı, yalnız `IUnitOfWork`/`IRepository<T>`. Zaten build'de kırılır ama PR'da neden kırıldığını anlamak için bilinçli kontrol et. | ARCHITECTURE.md §1 |
| Yeni modül eklendiyse **18 adımlı reçete** eksiksiz mi? | Entity → Configuration → DbContext → Migration → Feature (Dtos/Queries/Commands) → Public controller → Admin controller → İzin adı → Panel controller+view → `API_CONTRACT.md` → testler → `ARCHITECTURE.md` modül tablosu satırı. Eksik adım genelde §7'ye yeni bir görünmez sözleşme ekler. | ARCHITECTURE.md §4 |
| `ARCHITECTURE.md` modül tablosu güncellendi mi? | Yeni/değişen/silinen modül varsa tabloya satır eklenmeli/çıkarılmalı — yoksa `ArchitectureDocTests` kırmızı olur (bilerek). PR'da bunu manuel de doğrula. | ARCHITECTURE.md §10.8, `Integration/Architecture/ArchitectureDocTests.cs` |
| DTO alanı **silindi/yeniden adlandırıldı mı** (kırıcı değişiklik)? | Additive serbest; silme/rename için 3 adımlı geçiş planı (ikisini birlikte doldur → istemci geçsin → sonra sil) zorunlu, tek PR'da yapılmamalı. | ARCHITECTURE.md §5 |
| Uç davranışı değişikliği (sıralama varsayılanı, sayfalama şekli, doğrulama sıkılığı) eski istemcileri kırıyor mu? | Varsayılan sıralamayı değiştirmek listeyi sessizce ters çevirir; sayfalamayı ekleyip/kaldırmak `data` şeklini değiştirir (`[...]` ↔ `{items,...}`) ve istemci ayrıştırıcısını patlatır. | ARCHITECTURE.md §5 |

---

## 2. Backend — Domain / Application (CQRS)

| Kural | Açıklama | Referans |
|---|---|---|
| Feature klasörü doğru yapıda mı? | `Features/<Modül>/{Commands,Queries,Dtos}`; handler `IUnitOfWork` üzerinden gider, `DbContext`'e doğrudan dokunmaz. | ARCHITECTURE.md §2 |
| Public listede görünürlük filtresi **controller'da zorlanıyor mu**? | `dto.IsActive = true;` gibi — DTO'dan gelen değere güvenilmez. Onaylı+aktif+silinmemiş+süresi geçmemiş kaydı yalnız public uç döndürür. | ARCHITECTURE.md §4, Değişmez Kural #3 |
| Sayısal alan ayrıştırması **binlik ayracı kabul ediyor mu** (etmemeli)? | `NumberStyles.Number` yerine `AllowLeadingSign | AllowDecimalPoint` kullan — geçmişte `"2020,5"` sessizce `20205` olarak okunmuştu (10 kat sapma). | Progress.md Faz 11.14 gerçek hatası |
| Tarih/saat alanı **"TR günü, 00:00 UTC"** konvansiyonuna mı uyuyor? | `dutyDate`/`eventDate`/`funeralDate` gibi alanlar saat dilimine göre kaydırılmamalı; saat ayrı bir alanda (`TimeSpan`) taşınır. Bu hata sınıfı mobil+backend'de **4 kez** tekrarlamış (11.7/11.10/11.11/11.13). | ARCHITECTURE.md §7 madde 6 |
| Yeni cache'lenen sorgu: **grup sabiti + invalidator var mı**? | Cache grup adı yalnızca `CacheGroups` sabitlerinden olmalı; her grup için en az bir invalidate eden komut şart (tek bilinçli istisna: `dashboard`, 60sn TTL'e dayanır). Yoksa panelde güncellenen veri mobilde sessizce eski kalır — ne log ne istisna. | ARCHITECTURE.md §7 madde 22, `CacheContractTests` |
| `IAuditableCommand` gerekiyor mu? | Yazma/silme/onay/red gibi durum değiştiren komutlarda audit izi (`AuditBehavior`) bekleniyor mu — "kim ne zaman yaptı" panelde/log'da izlenebilir olmalı. | Progress.md Faz 10.9 denetimi |
| Onay/red durum makinesinde eski gerekçe temizleniyor mu? | Örn. `ApproveXCommandHandler` önceki `RejectedReason`'ı sıfırlamalı — unutulursa "Onaylandı" rozeti ile eski red gerekçesi yan yana görünür (ilanlarda düzeltilmiş, kampanyada bir süre unutulmuştu). | Progress.md Faz 11.15b |
| Slug/normalizasyon **tek bir yardımcıdan mı** üretiliyor? | İkinci bir gerçekleme yazma — `SlugHelper` tek sahip olmalı. Türkçe büyük `İ` (U+0130) `ToLowerInvariant()` ile küçülmez, ayrı ele alınmalı; aksi halde aynı ada iki farklı slug (mükerrer kayıt) üretilir. | ARCHITECTURE.md §7 madde 21 |
| Yeni bayrakla kapatılabilen kod yolu (`Fcm:Provider=None` gibi) test ediliyor mu? | Bayrakla kapalı yol = hiç çalıştırılmamış yol; anahtar bağlanır bağlanmaz ilk kez patlayabilir (`FirebaseApp.GetInstance` null döner, fırlatmaz). Her bayrakla kapalı yola en az bir birim testi. | ARCHITECTURE.md §7 kod-dışı sözleşmeler |

---

## 3. Backend — API / Controllers

| Kural | Açıklama | Referans |
|---|---|---|
| Yol **kebab-case** mi? | `SlugifyParameterTransformer` çok kelimeli controller adını çevirir ama elle `[Route]` yazarken de kebab kullan (`/v1/lost-items`, PascalCase 404 verir). | ARCHITECTURE.md §7 madde 12 |
| Admin controller `AdminApiControllerBase`'den mi türüyor + `[RequirePermission]` her aksiyonda var mı? | Yetki politikası tabanda tanımlı; eksik `[RequirePermission]` sessizce daha geniş erişim açabilir. | ARCHITECTURE.md §4 adım 7, Değişmez Kural #4 |
| Yeni izin adı `permissions` tablosuna eklendi mi + panel rollerine dağıtıldı mı? | İzin yoksa moderator 403 alır — "karşılığı olmayan yetki" tuzağının API tarafı. | ARCHITECTURE.md §4 adım 8 |
| Yanıt zarfı (`{success,data,meta}`) korunuyor mu, `meta.traceId` her yanıtta var mı? | İstisna yaratma (announcements'ın 200+success:false quirk'i gibi) — bilinçli değilse zarftan sapma istemciyi kırar. | API_CONTRACT.md §2 |
| Yeni hata durumunda uygun `code` kullanıldı mı (özel string değil)? | `error.code` sözlüğe (`VALIDATION_ERROR`, `CONFLICT`, `NOT_FOUND`...) uymalı; istemci `code`'a göre dallanıyor, mesaj metnine değil. | API_CONTRACT.md §3 |
| Search parametre adı tutarlı mı? | Çoğu modül `search` kullanır, taksi + ulaşım `searchTerm` kullanır — yeni endpoint eklerken hangisi olduğunu bilinçli seç ve dokümante et, yanlış ad **sessizce yok sayılır** (400 gelmez). | ARCHITECTURE.md §7 madde 4 |

---

## 4. Panel (KadirliApp.Web — Razor/MVC)

| Kural | Açıklama | Referans |
|---|---|---|
| Yeni panel controller'ına **hem** `[Authorize(Roles="admin,super_admin,moderator")]` **hem** `[PanelPermission("<modül>")]` eklendi mi? | Rol listesine `moderator` yazıp özniteliği unutmak, moderatöre o modülde **sınırsız** yetki verir — bu proje bunu canlıda yaşadı (11.15b'nin en büyük bulgusu). `PanelModeratorPermissionTests` yakalar ama PR'da elle de kontrol et. | ARCHITECTURE.md §3, Active_Context.md |
| Ekran **yalnız admin'e** açıksa desen farklı — modül anahtarı verilmedi mi? | `[Authorize(Roles="admin,super_admin")]` + `[PanelPermission]` **YOK** + `PanelMenu.Items` satırının `Module`'ü **`null`** + `AdminOnlyControllers`'a controller adı. Modül anahtarı verirsen izin matrisinde moderatöre dağıtılabilen ama rol kapısı yüzünden **asla çalışmayacak** bir yetki belirir — 11.15b'nin "karşılığı olmayan yetki" hatasının tekrarı. Örnek: `StaffAdmin`, `AuditLogsAdmin`, `TrashAdmin`. | ARCHITECTURE.md §3, `PanelAuditLogTests`, `PanelTrashTests` |
| Panelde bir zaman/durum ayrımı **istemcide de** yapılıyor mu? | Yapılıyorsa iki tanım **birebir** aynı olmalı ve testle kilitlenmeli: `PowerOutagePhaseRules` ↔ mobil `PowerOutage.isActive/isUpcoming/isPast` (başlangıç dâhil, bitiş hariç). Ayrışırsa panel "sürüyor" derken vatandaş "planlı" görür ve **kimse hata almaz** — §7 madde 27. | `PanelPowerOutageFilterTests` |
| Soft-delete edilmiş kaydı listeleyen yeni sorguya `IgnoreQueryFilters()` konuldu mu? | Global süzgeç tam olarak o satırları gizler; unutulursa ekran **her zaman boş** görünür ve hata da vermez (çöp kutusu). Geri getirme ayrıca `status`'e **dokunmamalı** — yoksa sil+geri getir ikilisi moderasyonu atlar (§7 madde 28). | `PanelTrashTests` |
| Yeni `IAuditableCommand` eklendiyse `PanelDisplay.AuditAction` sözlüğüne satır atıldı mı? | Denetim izi ekranı ham `AuditAction` değerini basmaz. Sözlük **kaynak taranarak** kilitli (`AuditAction => "…"`), eksik satır testi kırar — ama neyin Türkçesi olacağı insan kararı. | `PanelAuditLogTests.AuditAction_HasTurkishLabel…` |
| `PanelMenu.Items`'a satır eklendi mi? | Menü tek liste; eklenmezse ekran erişilebilir ama menüden görünmez (ölü buton kuralının tersi — "gizli buton"). `StaffAdminController.Modules` ve `PanelDisplay.ModuleLabel()` **buradan türer** — ikinci bir liste yazma. | ARCHITECTURE.md §3 |
| ⚠️ **TEKRARLAYAN** Durum/rol ekrana **ham** mı basılıyor? | `@item.Status` / `@user.Role` doğrudan yazılmaz → `<partial name="_StatusBadge" model="PanelDisplay.Status(...)" />`. 11.15c'de **yedi listede** `expired`/`archived`/`SuperAdmin` ham basıyordu; sebep her görünümün kendi if/else zincirini yazması ve **son `else` dalının** ham değeri geçirmesiydi. | `PanelDisplayTests`, `PanelUsabilityTests` |
| Para `PanelDisplay.TL()`'den mi geçiyor? | `ToString("C2")` **kullanılmaz**: panel `Program.cs`'te bilinçli olarak `InvariantCulture`'a sabit, bu yüzden jenerik **`¤`** basar (canlıda `¤750,000.00` görüldü). | `PanelDisplayTests.TL_UsesTurkishFormat…` |
| Yeni gezinme bağlantısı **dar ekranda** da var mı? | Menü iki yerde çizilir (kenar çubuğu + `<details>` açılır menü) ama **tek listeden** gelir (`_MenuLinks.cshtml`). Kendi `<a>` bloğunu yazarsan dar ekranda görünmez. | `PanelUsabilityTests.NarrowScreen_…` |
| Panelde gösterilen **"aktif/yayında" sayacı** public uçla aynı tanımı mı kullanıyor? | Ayrışırsa panel ile vatandaş **farklı gerçeklik görür ve kimse hata almaz**: 11.15c'de panel "Aktif İlanlar 1" derken `GET /v1/ads` 0 döndürüyordu (`ExpiresAt` yok sayılıyordu). | ARCHITECTURE.md §7 madde 23 |
| Bir kaydı **görünmez kılan/geri getiren** aksiyon, ona bağlı türetilmiş veriyi de düşünüyor mu? | Silinen duyurunun bildirimleri ayakta kalıyordu → kullanıcı bildirime dokunup boş sayfaya düşüyordu. Kaynakta temizle **ve** sorguda "hedefi yaşayan" süzgeci koru; sayaç (`unreadCount`) listeyle **aynı** sorgudan türemeli. | ARCHITECTURE.md §7 madde 24 |
| Yeni Index view'de **sayfalama UI'ı** var mı (yalnız `PagedResult` dönmek yetmez)? | Geçmişte 8 Index view `PagedResult`'a bağlıyken sayfalama arayüzü hiç yoktu → 20. kayıttan sonrasına erişilemiyordu. Ortak `_Pagination.cshtml` kullan, mevcut query string'i koru. | Progress.md Faz 11 denetim oturumu |
| Başarı/hata mesajı **yalnız `_Layout`'ta mı** basılıyor? | `TempData` key'leri harf duyarsız; view içinde ayrıca `TempData["Success"]/["Error"]` basmak çift balona yol açar. Controller'da tek standart key (`Success`/`Error`) kullan. | Progress.md Faz 11 denetim oturumu |
| Yeni yazma aksiyonu **audit izi** bırakıyor mu? | `IAuditableCommand` deseni panelde de geçerli; onay/red/silme gibi aksiyonlarda "kim yaptı" izlenebilir olmalı. | Active_Context.md güvenlik notları |
| Silme/onay gibi geri alınamaz aksiyonlarda onay **neyi sildiğini yazıyor mu**? | `onsubmit="return confirm(...)"` **yazılmaz** → `data-confirm="…"` özniteliği (dinleyici `_Layout`'ta tek yerde). Kaydın adını yaz. ⚠️ Adı inline JS dizesine gömmek kırılgandır: Razor öznitelikleri HTML-encode ettiği için tırnaklı bir başlık (`Ali'nin arabası`) dizeyi bozar; öznitelikte taşınırsa bu sorun yok. | 11.15c |
| Dar ekranda (< 1024px) bu sayfaya ulaşmanın bir yolu var mı? | Panelin kenar çubuğu `hidden lg:flex`; dar ekranda menü tamamen kayboluyor — yeni ekran eklerken bu boşluğu büyütme. | Active_Context.md |
| Varsayılan admin şifresi / hassas bilgi `IsDevelopment()` koşulu **olmadan** ekrana basılıyor mu? | Login sayfasındaki satır 11.15c'de koşula bağlandı (`@inject IWebHostEnvironment Env`); yeni debug/yardım metni eklerken tekrarlama. | 11.15c |
| ⚠️ **TEKRARLAYAN** Yeni **toplu işlem** aksiyonunun adı `…Selected` ile mi bitiyor? | İzin eylemi aksiyon adının **önekinden** türetilir (§7 madde 19). `BulkApprove` hiçbir moderasyon önekiyle eşleşmez ve sessizce **`update`**'e düşer — yalnız düzenleme yetkisi olan moderatör toplu ONAY yapabilir hâle gelir. Bu, 11.15b'nin "karşılığı olmayan yetki" hatasının üçüncü biçimi. | ARCHITECTURE.md §7 madde 29, `PanelBulkActionTests` |
| Toplu işlem, modülün **tek-kayıt komutunu** mu çağırıyor? | Toplu SQL `UPDATE` yazılırsa denetim izi (komut başına düşer), önbellek geçersizleştirmesi ve iş kuralları (ör. onayın süresi dolmuş ilana taze pencere vermesi, §7 madde 25) **hiç çalışmaz**: panel "42 ilan onaylandı" der, mobil hiçbirini göstermez. Kayıt başına hata partiyi durdurmamalı, sayılıp mesajda söylenmeli. | ARCHITECTURE.md §7 madde 29, `PanelBulkActionTests` |
| Yeni **sıralama anahtarı** benzersiz bir ayraçla (`ThenBy(Id)`) bitiyor mu? | Eşit değerli satırlarda Postgres sırayı garanti etmez → sayfalı listede aynı kayıt iki sayfada görünüp bir başkası hiç görünmez (**sessiz veri kaybı**). "Bir ikincil anahtar koymak" yetmez, ayracın benzersiz olması gerekir. Varsayılan anahtar da modülün **eski sırasıyla birebir aynı** kalmalı — değişirse mobil liste sessizce ters döner. | ARCHITECTURE.md §7 madde 30, `PanelSortingTests` |
| Yeni parola girişi/denetimi `PanelPasswordPolicy`'den mi geçiyor? | Kural 11.18 öncesi **üç ayrı handler'da** elle `Length < 6` olarak kopyalanmıştı; politikayı sıkılaştıran biri birini atlarsa o kapıdan zayıf parola girmeye devam eder. Parolayı **sahibi değil yönetici** belirlediyse (`CreateStaff`, `ResetStaffPassword`, seed) `MustChangePassword` işaretlenmeli. | `PanelPasswordSecurityTests` |
| Yetki-hassas bir alan (rol, aktiflik, ban, parola) değiştiren yeni bir yol açtın mı? | Açık oturumlar `OnValidatePrincipal` (`PanelPrincipalValidator`) ile her istekte DB'den tazeleniyor; yeni alan oraya da yansımalı. ⚠️ Parola damgası karşılaştırması **saniyeye yuvarlanır** — çerezin `IssuedUtc`'si RFC1123 ile saklandığı için saniye altını taşımaz; ham karşılaştırma, parolasını değiştiren kişiyi kendi oturumundan atar. | `PanelPasswordSecurityTests` |
| Dar ekranda menü hâlâ açılıyor mu? | Kenar çubuğu `hidden lg:flex`; dar ekranın tek gezinme yolu üstteki `<details>` menüsü. 11.15c öncesinde o buton **bir kabuktu** (`id`/`onclick`/JS yoktu) ve <1024 px'de panelde hiç menü yoktu. | `PanelUsabilityTests` |

---

## 5. Mobil (Flutter)

| Kural | Açıklama | Referans |
|---|---|---|
| Dar sütunda (360dp) veya 1.4 yazı ölçeğinde `Row` **taşması** test edildi mi? | Bu proje aynı `RenderFlex overflow` hatasını **7+ kez** farklı ekranlarda üretti (Pharmacy/Guide/Ad/Favorite/Lookup/Event/Complaint kartları). Metin `Text` yerine `Flexible`/`Expanded`+ellipsis kullan; golden test'e 1.4 ölçek senaryosu ekle. | ARCHITECTURE.md §7 kod-dışı, birden çok Progress.md girdisi |
| Yeni liste/uç provider'ına **`retry: apiRetry`** verildi mi? | Riverpod 3 hatalı provider'ları sınırsız yeniden dener; `apiRetry` yalnız bağlantı/timeout/5xx/429'da ≤2 tekrar yapar. | ARCHITECTURE.md §7 kod-dışı |
| Yeni sayfalı liste **kendi altbilgisini yazıyor mu** (yazmamalı)? | `PagedListFooter` ortak bileşeni kullan — 11 kopya bundan tekilleştirildi. | Progress.md Faz 11.15 |
| Boş/hata durumu (`EmptyView`/`ErrorView`) **kaydırılabilir mi**? | Sarmalamadan direkt kullanma; `ScrollableStateBody` olmadan pull-to-refresh boş/hata durumunda sessizce ölür. | Progress.md Faz 11.15 |
| Form/detay rotası bir listenin **kardeşi mi, alt rotası mı**? | `go_router` iç içe rotada üst ekranı da kurar — form/detay rotaları her zaman **kardeş** olmalı, yoksa arka planda gereksiz istek+diyalog açılır (11.9'da taslak diyaloğu düzenleme ekranının üstüne fırlamıştı). | ARCHITECTURE.md §7 kod-dışı |
| `context.push` ile açılan ekran, durum değiştikten sonra **`addPostFrameCallback` içinde** kapatılıyor mu? | Aksi halde ekran router redirect'inin üstünde asılı kalır (kayıt sonrası kapanmama, hesap silme sonrası sonsuz spinner gibi 3 gerçek hata buradan çıktı). | Progress.md Faz 11.5 |
| ⚠️ **TEKRARLAYAN (4 kez)** Tarih gösteren yeni bir karta (`"3 saat önce"` gibi) **`now` enjekte edilebiliyor mu**? | Enjekte edilemeyen göreli tarih golden testi her gün kırar → insan `--update-goldens`'ı refleks yapar ve testin değeri sıfırlanır. 11.15b'de `AnnouncementTile`+`ComplaintCard`, 11.15c'de `NotificationTile` bu yüzden kırıldı. 🔑 **Referansı yenilemeden önce PNG'nin NE gösterdiğine bak:** `NotificationTile`'ın referansı "20 dakika önce" değil tam tarih basıyordu — yani referansın kendisi **hatalı davranışın çıktısıydı**. | ARCHITECTURE.md §8 |
| Yeni ortak bileşen/liste kartı `test/golden/`'a eklendi mi (uzun Türkçe metinle)? | Kısa örnek hiçbir düzen hatasını göstermez; golden senaryosu **uzun** Türkçe metinle olmalı. | ARCHITECTURE.md §8 |
| Arayüz metni **Türkçe**, hata mesajı kullanıcıya **teknik/İngilizce sızmıyor** mu? | `turkish_ui_test.dart` sözlük eksiksizliğini kaynağı tarayarak kontrol eder — yeni hata koduna karşılıksız metin eklenirse kırılır. | Değişmez Kural #6, ARCHITECTURE.md §8 |
| Yeni buton bir uca/ekrana gidiyor mu (işlevsiz buton yok)? | `app_modules_test.dart` bunu mekanik doğruluyor ama yeni ekran eklerken elle de düşün. | Değişmez Kural #5 |

---

## 6. Database / Migration

| Kural | Açıklama | Referans |
|---|---|---|
| Migration üretilen SQL **okundu mu** (`dotnet ef migrations add` sonrası)? | Kör `database update` yapılmamalı; kolon tipi/index/ilişki beklenen mi kontrol et. | ARCHITECTURE.md §4 adım 4 |
| Tablo/kolon adları **snake_case** mi? | `Configurations/` altındaki EF yapılandırmasında proje konvansiyonu snake_case. | ARCHITECTURE.md §2 |
| Modül kaldırılıyorsa **tablo düşürülüyor mu** (düşürülmemeli, genelde)? | Soft-delete'li veri varsa tabloyu düşürmek geri dönüşsüz silme demektir. Önerilen: `DbSet`+entity kaldır, tabloyu bırak; gerçekten silinecekse önce `pg_dump` yedeği. | ARCHITECTURE.md §6 |
| Yeni jsonb kolon DTO'da doğru tipte mi temsil ediliyor? | `places.amenities` gibi `jsonb` bir DTO'da `string` olarak modellenmişse yanıt "JSON içeren metin" döner, nesne değil — bilinçli olmalı ve dokümante edilmeli. | ARCHITECTURE.md §7 madde 5 |
| Index gerekliliği analiz edildi mi? | Yeni sık sorgulanan kolon (özellikle filtre/arama alanı) için index düşünüldü mü. | Genel DB pratiği |
| Foreign key / normalizasyon kuralları uygulanmış mı? | Yeni tablo ilişkileri FK ile kurulmalı, gereksiz veri tekrarından kaçınılmalı. | Genel DB pratiği |

---

## 7. Security

| Kural | Açıklama | Referans |
|---|---|---|
| Yeni admin/panel aksiyonu **rol + izin** ikisini birden mi kontrol ediyor? | Bkz. §4 — tek başına rol kontrolü moderatöre sınırsız yetki verebilir. | Active_Context.md |
| Hassas veri (telefon, adres, TC vb.) loglanmıyor mu? | `AuditBehavior`/Serilog çıktısına PII sızdırmamaya dikkat. | Orijinal enterprise checklist maddesi, hâlâ geçerli |
| Yeni public yazma ucu rate-limit'e tabi mi? | Anonim POST uçları (complaints, announcements/view gibi) public-write rate limit kullanıyor — yeni anonim uç eklerken aynısını uygula. | Progress.md Faz 10.12 |
| Oturum/cookie iptali düşünüldü mü? | Panel cookie'si 8 saat sabit, `OnValidatePrincipal` yok — silinen/banlanan personelin oturumu hâlâ çalışabilir. Yeni yetki-hassas aksiyon eklerken bu boşluğu büyütme, mümkünse kapatmaya katkı ver. | Active_Context.md güvenlik notları |
| Yeni endpoint **`EndpointAuthorizationSweepTests`** kapsamına otomatik giriyor mu? | Yapısal test `EndpointDataSource`'tan tüm uçları tarar; anonim yazma ucu ekliyorsan beklenen listeyi bilinçli güncelle (kaçak varsa test kırmızı kalmalı, listeye sessizce eklenmemeli). | ARCHITECTURE.md §8 |

---

## 8. Performans / Cache

| Kural | Açıklama | Referans |
|---|---|---|
| Yeni cache'lenen query `ICacheableQuery` mi uyguluyor, anahtar her filtreyle değişiyor mu? | İki farklı sorgu aynı anahtarı paylaşırsa yanlış veri döner. | `CacheContractTests` |
| Redis erişilemezse **fail-open** mı çalışıyor? | `CachingBehavior` cache okunamazsa/yazılamazsa isteği düşürmemeli, handler'a düşmeli — yeni cache kullanan koda bu davranış korunmalı. | KadirliApp.Application/Common/Behaviors/CachingBehavior.cs |
| Bellek/memory'ye BULK data basan bir kurgu var mı? | Varsa sayfalama/streaming'e çevrilmeli; aksi halde out-of-memory riski (bkz. eski `UsersAdmin` filtresiz `ToListAsync()` hatası — panelin en büyük büyüyecek tablosunu filtresiz çekiyordu). | Progress.md Faz 11 denetim oturumu |

---

## 9. Test

| Kural | Açıklama | Referans |
|---|---|---|
| Yeni iş kuralı için birim testi var mı **ve kuralı geçici bozup kırmızı olduğu görüldü mü**? | Testi yazmak yetmez; kuralı bilerek boz, testin gerçekten kilitlediğini doğrula (proje ölçütü bu). | ARCHITECTURE.md §8 |
| Yeni uç görünürlük + "mutlu yol" testiyle kapsandı mı? | Yetki testi yapısal taramayla kendiliğinden gelir; görünürlük (`ModuleVisibilitySweepTests` deseni) ayrı eklenmeli. | ARCHITECTURE.md §8 |
| Mobil yeni ekran: boş/yükleniyor/hata durumu + ana etkileşim test edildi mi? | Minimum kapsam budur. | ARCHITECTURE.md §8 |
| Panel testi `[Collection("panel")]` kullanıyor mu? | Kendi `IClassFixture`'ını açan sınıf süiti dakikalarca uzatır; tüm panel testleri tek container çiftini paylaşmalı. | ARCHITECTURE.md §8 |
| Test sabit `Future.delayed`/gerçek saat kullanıyor mu (kullanmamalı)? | `waitUntil(condition)` kullan; sabit gecikme flaky olur, sabit `DateTime.now()` gece saatlerinde testleri kırar (bu proje bunu en az 3 kez yaşadı). | ARCHITECTURE.md §8 |

---

## 10. Dokümantasyon

| Kural | Açıklama | Referans |
|---|---|---|
| `API_CONTRACT.md` yeni/değişen uç ve DTO alanlarını yansıtıyor mu? | Flutter tarafının tek referansı; `docs/openapi.json` da yenilenmeli. | ARCHITECTURE.md §4 adım 10 |
| Yeni bir "görünmez sözleşme" (koda bakarak anlaşılmayan, bozulunca sessiz hasar veren bağımlılık) doğdu mu? | Doğduysa `ARCHITECTURE.md` §7 tablosuna satır + `InvisibleContractsTests`'e karşılık gelen test eklenmeli. | ARCHITECTURE.md §7 |
| `Memory_Bank/Progress.md`'ye oturum özeti düşüldü mü (kararlar + gerçek hatalar + doğrulama)? | Proje bu formatı kronolojik hafıza olarak kullanıyor; büyük değişikliklerde atlanmamalı. | ARCHITECTURE.md giriş tablosu |

---

## Bakım

Bu dosyanın **referansları** `CodeReviewChecklistDocTests` ile kilitlidir
(`ARCHITECTURE.md` ↔ `ArchitectureDocTests` ilişkisinin aynısı). Denetlenen şey
**maddelerin doğruluğu değil, atıflarının gerçekliği**:

- atıf yapılan her test sınıfı (`…Tests`) ve mobil test dosyası (`…_test.dart` ) **var mı**,
- atıf yapılan ortak yardımcılar (`PanelDisplay`, `PanelMenu`, `PagedListFooter`, `_StatusBadge`…)
  **hâlâ duruyor mu**,
- katman bölümlerinden biri **kaybolmuş mu**,
- satır numarasına **çivilenmiş atıf** var mı.

Bir kuralın hâlâ *iyi bir kural* olup olmadığı insan kararıdır — o kısım kilitlenemez.
Test kırıldığında yapılacak şey testi gevşetmek değil, **checklist'i güncellemektir.**

İki yazım kuralı:

1. Satırlara **dosya:satır** yazma, **sınıf/yardımcı adı** yaz — `PanelDisplay.TL()`
   taşınsa bile aranabilir; bir görünümün "129. satırı" ise ilk düzenlemede yanlış olur.
   (`Checklist_DoesNotPinLineNumbers` bunu zorluyor.)
2. Bir madde **ortak bir bileşene** dönüştüğünde (yani artık unutulamaz hâle geldiğinde)
   satırı silme — bileşenin adını yazarak bırak; yeni gelen "neden böyle" sorusunun
   cevabı burada.

Son gözden geçirme: **4 Ağustos 2026 (Faz 11.18)** — §4'e beş satır daha eklendi (toplu
işlem ad kuralı ↔ izin türetmesi, toplu işlemin tek-kayıt komutunu çağırması, sıralama
ayracının benzersizliği, parola politikasının tek sahibi, oturum tazeleme).
Önceki: **4 Ağustos 2026 (Faz 11.17)** — panelin dört yeni ekranı bu listeyle
yazıldı; §4'e dört satır eklendi (yalnız-admin ekran deseni, panel↔istemci zaman tanımı
paritesi, `IgnoreQueryFilters` + geri getirmenin `status`'e dokunmaması, denetim eylemi sözlüğü).

## Notlar

- Bu checklist, orijinal kurumsal şablondaki genel maddeleri (isimlendirme, null-check,
  okunabilirlik, gereksiz complexity) **elemedi** — onlar hâlâ geçerli, sadece burada
  tekrar edilmedi çünkü proje-spesifik değiller. PR review'da ikisini birlikte kullan.
- "⚠️ TEKRARLAYAN HATA" diye işaretlemediğim ama tablo içinde vurguladığım maddeler
  (RenderFlex taşması, timezone kayması, cache invalidator eksikliği, PanelPermission
  unutulması) bu projede **birden fazla oturumda** aynı sınıf hata olarak çıkmış —
  review'da bunlara normalden fazla ağırlık ver.
