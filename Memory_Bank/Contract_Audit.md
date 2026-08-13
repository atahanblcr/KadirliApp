# 🔍 Görünmez Sözleşme Denetimi — Faz 0: TASNİF

> **Bu dosya ne işe yarar?** `ARCHITECTURE.md` §7'deki **67 görünmez sözleşmenin** her birini
> *kilidinin cinsine* göre etiketler. Soru "testi var mı?" değil (67'sinin de var),
> **"kilidi sahte mi?"**
>
> **Bu dosya kalıcıdır.** Sonraki oturumlar baştan tasnif etmez, buradan devam eder.
> Faz A (bozma turu) yalnız 🔴 ve 🟠 satırlarda koşacak.
>
> Reçetenin tamamı: `Memory_Bank/Progress.md` → *"GÖRÜNMEZ SÖZLEŞME DENETİMİ"*.
> Faz 0 koşuldu: **13 Ağustos 2026**. Yöntem: her maddenin kilidini taşıyan dosya **açılıp
> okundu**; test adı yeterli sayılmadı, iddianın **şekli** incelendi. Test koşturulmadı
> (Faz 0'ın tanımı bu).

---

## 1. Risk ölçeği

| Risk | Kilit cinsi | Neden bu seviyede |
|---|---|---|
| 🟢🟢 | **Derleyici** (`init`/CS8852) · **DB kısıtı** (unique/kısmi indeks) | Taramanın erişemeyeceği yerde — bozma denemesi *derlenmez* ya da *veritabanı reddeder* (12.11'in dersi) |
| 🟢 | **Davranış testi (gerçek Postgres)** · **deterministik saf birim** | Gerçeği ölçer; iddia kodun kendisine değil sonucuna bakar |
| 🟠 | **İstemci tarafı** · **kapsamı elle tutulan liste** · **doküman testi** | Ayrı koşucu / elle liste / atıf denetimi — sessizce ayrışabilir |
| 🔴 | **Kaynak taraması** · **yalnız golden** · **kuruluma-tek koşuya bağlı** · **İDDİA EKSİK** | Geçmişte beş kez delik çıkan cinsler + bu denetimde bulunan yeni sınıf |

🔴 **İDDİA EKSİK** bu denetimde doğan **yeni bir kilit cinsi**: madde *bir testte anılıyor*
ama sözleşmenin **iddia edilen yüzü** ile **yazılı yüzü** aynı değil. Beş fazın hata sınıfının
(iddiası zayıf test) en saf hâli: dosya var, test yeşil, koruma yok.

---

## 2. Tasnif tablosu (67 madde)

| # | Sözleşme (kısa) | Kilit cinsi | Risk | Kilidi taşıyan dosya |
|---|---|---|---|---|
| 1 | `power-outages` sayfalamaz, düz dizi | Davranış (gerçek PG) | 🟢 | `Integration/Contracts/InvisibleContractsTests.PowerOutages_ReturnFlatArray_NotPagedResult` |
| 2 | Duyuruda 200 + `success:false` | Davranış | 🟢 | `InvisibleContractsTests.Announcements_UnknownId_Returns200_WithSuccessFalse_NotHttp404` |
| 3 | `ads/{id}` artıştan ÖNCEKİ `viewCount` | Davranış | 🟢 | `InvisibleContractsTests.AdDetail_IncrementsViewCount_AndReturnsPreIncrementValue` |
| 4 | `search` ↔ `searchTerm` ayrımı | Davranış + istemci | 🟢 | `InvisibleContractsTests.SearchParameterName_…` + `mobile/…/transport_screen_test.dart` |
| 5 | `places.amenities` JSON içeren **metin** | Davranış | 🟢 | `InvisibleContractsTests.Places_Amenities_IsJsonEncodedText_NotAJsonObject` |
| 6 | Gün alanları "TR günü, 00:00 UTC" | Davranış — **kapsam eksik** | 🟠 | `InvisibleContractsTests.DayOnlyDateFields_…` (yalnız `eventDate` + `dutyDate`; **`funeralDate` iddia edilmiyor** → B7) |
| 7 | Ulaşım saatleri iki biçimli duvar saati | Davranış | 🟢 | `InvisibleContractsTests.TransportDepartureTimes_AreDatelessWallClock_InTwoDifferentFormats` |
| 8 | `UpdateMyAd` sıra/kapak bilmez | Davranış | 🟢 | `InvisibleContractsTests.UpdateMyAd_AppendsNewImagesAsNonCover_…` |
| 9 | Görsel URL'leri **göreli** | Davranış + açılış kapısı | 🟢 | `InvisibleContractsTests.ImageUrls_AreReturnedRelative_…` + `Unit/Api/ProductionReadinessGuardTests` |
| 10 | Zarf + her yanıtta `traceId` | Davranış | 🟢 | `InvisibleContractsTests.EveryResponse_IsEnvelopedAndCarriesMeta` |
| 11 | `complaints.type` serbest metin | Davranış | 🟢 | `InvisibleContractsTests.ComplaintType_IsFreeText_ServerDoesNotValidateIt` |
| 12 | Yollar kebab-case | Davranış | 🟢 | `InvisibleContractsTests.RoutePaths_AreKebabCase_AndPascalCaseIs404` |
| 13 | Sayısal özellik InvariantCulture | Saf birim | 🟢 | `Unit/Application/Ads/AdSubmissionRulesTests` (`"2020,5"` reddi ayrı testte) |
| 14 | `select` değeri metinle + harf duyarlı | Saf birim | 🟢 | `AdSubmissionRulesTests` |
| 15 | `AdCategory` filtresi **TAM EŞLEŞME** | **İDDİA EKSİK** | 🔴 | *Yok.* En yakını `Integration/Ads/AdsMobileTests.Categories_ReturnSeededHierarchy_Anonymously` — o **kategori ağacı** ucunu denetliyor, ilan **süzgecinin** semantiğini değil → B3 |
| 16 | Push `data` sözlüğünün anahtarları | **İDDİA EKSİK** | 🔴 | `Integration/Notifications/PushNotificationsJobTests` yalnız `ContainsKey("notificationId")` diyor; `type`/`relatedId`/`relatedType` **hiçbir yerde** iddia edilmiyor → B1 |
| 17 | `unreadCount` gövdenin içinde ve **filtreden bağımsız** | Davranış — **yarısı eksik** | 🔴 | `Integration/Notifications/NotificationsTests` (varlık + read-all sonrası 0) · `PanelBusinessRuleTests.UnreadCount_UsesSameLivenessFilterAsTheList` (canlılık). **"`unreadOnly=true`'da da toplam"** iddiası yok → B4 |
| 18 | `relatedType` → mobil rota; tanınmayan tür iptal | İstemci tarafı | 🟠 | `mobile/test/features/notifications/notification_link_test.dart` (sunucunun ürettiği türle bağı yok — madde 16 ile aynı boşluğun diğer ucu) |
| 19 | İzin eylemi aksiyon **adından** türer | **Kapsamı elle tutulan liste** | 🔴 | `PanelModeratorPermissionTests.ActionName_MapsToTheExpectedPermission` — `[InlineData]` **elle** yazılmış; gerçek aksiyon kümesinden türetilmiyor. Tuzak 4 kez tekrarladı (11.18 · 12.10 · 12.13 · 12.15) → B6 |
| 20 | Menü · matris · `[PanelPermission]` aynı anahtar | Yansıma (türetilmiş) | 🟢 | `PanelModeratorPermissionTests.MenuModules_MatchThePermissionMatrixModules` |
| 21 | Slug üretiminin tek sahibi `SlugHelper` | Saf birim — **tek sahiplik iddia edilmiyor** | 🔴 | `Unit/Application/Common/SlugAndPaginationTests` yalnız **helper'ın davranışını** ölçüyor; `DbSeeder.Slugify`'ın ona **delege ettiğini** hiçbir test söylemiyor → B5 |
| 22 | Cache grup adları yalnız `CacheGroups` sabitleri | Yansıma (türetilmiş) | 🟢 | `Unit/Application/Caching/CacheContractTests` (grup kümesini kaynaktan türetiyor) |
| 23 | Panel sayaçları = public görünürlük tanımı | Davranış | 🟢 | `PanelBusinessRuleTests.DashboardActiveAds_ExcludesExpiredOnes` · `…Announcements_CountsOnlyPublishedOnes` |
| 24 | Bildirim hedefi yaşadığı sürece görünür | Davranış | 🟢 | `PanelBusinessRuleTests.NotificationList_Hides…` · `DeletingAnnouncement_AlsoRemovesItsNotifications` |
| 25 | Onay, süresi dolmuş ilana taze pencere verir | Davranış + saf birim | 🟢 | `PanelBusinessRuleTests.ApprovingExpiredAd_…` + `Unit/…/Moderation/ModerationTransitionTests` |
| 26 | `QueryAdDto.Status` yalnız panel yolunda okunur | **İDDİA EKSİK (yanlış modülde)** | 🔴 | `Integration/Security/PublicVisibilityTests` aynı kuralı **vefat** için ölçüyor; sözleşmenin adını taşıdığı **ilan** ucunda (`GET /v1/ads?status=pending`, 10.5'te bir kez sızdı) test **yok** → B2 |
| 27 | Kesinti süren/planlı/bitti tanımı panel↔mobil | Saf birim (iki dilde ayrı) | 🟠 | `PanelPowerOutageFilterTests` (birim kısmı) + `mobile/…/power_outage_model_test.dart` — **iki ayna, tek kaynak değil** |
| 28 | Geri getirme ≠ yayına alma | Davranış | 🟢 | `PanelTrashTests.Restore_DoesNotPublishTheRecord` |
| 29 | Toplu aksiyon `…Selected` + tek-kayıt komutu | Davranış + yansıma | 🟠 | `PanelBulkActionTests.BulkPrefixNaming_WouldSilentlyDowngradePermission` · `…AppliesBusinessRules_NotJustStatusUpdate`. Adlandırma kuralını **tarayan** bir test yok (madde 19 ile aynı kök) |
| 30 | Her sıralama anahtarı **benzersiz** ayraçla biter | Saf birim — **kapsam elle** | 🟠 | `PanelSortingTests.EveryKey_ProducesStableOrderForTiedRows` yalnız **Announcements** haritasını süpürüyor (+ `PanelErrorLogTests` / `PanelLoginAttemptTests` kendi modülleri için). Kalan haritalar süpürülmüyor. 📌 İddia şekli **doğru**: bellek-içi sıralama kararlı olduğu için ayraç *yoksa da*, *benzersiz değilse de* kırmızıya döner |
| 31 | Hata kaydı yazımı isteği düşüremez | Davranış | 🟢 | `PanelErrorLogTests` (+ `ChannelErrorLogSink` davranışı) |
| 32 | `Fingerprint` tekilleştirmesi + benzersiz indeks | Saf birim + **DB kısıtı** | 🟢🟢 | `Unit/…/Observability/ErrorFingerprintTests` + `PanelErrorLogTests.SameError_Twice_ProducesOneRow_WithCountTwo` |
| 33 | `Source` sunucuda sabit + `Path` maskeli | Davranış + saf birim | 🟢 | `PanelErrorLogTests.ClientSuppliedMessage_IsEscaped_…` + `SensitiveDataMaskerTests` |
| 34 | Giriş denemesinde `Identifier` maskeli ve deterministik | Saf birim + davranış | 🟢 | `Unit/…/Security/LoginIdentifierMaskerTests` + `PanelLoginAttemptTests.ExportCsv_MasksThePhone_…` |
| 35 | R1 eşiği = `PanelLockoutPolicy.MaxFailedAttempts` | Saf birim (**tek kaynağı doğrudan karşılaştırıyor**) | 🟢 | `Unit/…/Security/SuspiciousLoginRulesTests` — iki sabiti birbirine eşitliyor, kopya sayı yazmıyor |
| 36 | Uyarı e-postası kısılır, koşu başına tek | Davranış | 🟢 | `PanelLoginAttemptTests` (kısma kendi testinde) + `SecurityAlertJob` davranış testleri |
| 37 | `FcmSent = true` **terminaldir** | Davranış + saf birim | 🟢 | `PanelPushCampaignTests.Cancel_RemovesOnlyUnsentNotifications` · `Unit/…/Notifications/PushCampaignStatusTests` |
| 38 | Hedeflemenin tek sahibi `INotificationDispatcher` | Davranış | 🟢 | `PanelPushCampaignTests.EstimatePreview_MatchesWhatIsActuallyWritten` · `…NeighborhoodTargetingWithoutSelection_IsRejected_NotBroadcast` |
| 39 | Kampanya sayaçları **artımlı** yazılır | Davranış | 🟢 | `PanelPushCampaignTests.Job_WritesCountersIncrementally_AndCompletesDespiteTokenlessRecipients` |
| 40 | Kesinti mahalle metni sözlükten türetilir | Davranış | 🟢 | `PanelPowerOutageNeighborhoodTests.NeighbourhoodName_IsDerivedFromTheDictionary_NotFromTheForm` |
| 41 | Kesinti bildirimi **bir duyurudur** | Davranış | 🟢 | `PanelPowerOutageNeighborhoodTests.Notification_CreatesAnnouncement_…` · `Update_Refreshes…WithoutSendingASecondNotification` · `Delete_Removes…` |
| 42 | Yalnız hedeflenebilir kesinti bildirilebilir | Davranış | 🟢 | `PanelPowerOutageNeighborhoodTests.Notification_IsRefused_WhenTheOutageHasNoDictionaryNeighbourhood` |
| 43 | `locationLabel` sunucuda, liste=detay projeksiyon | Davranış | 🟢 | `PanelEventDistrictTests.DetailAndList_ReturnTheSameLocationFields` + `Unit/…/Events/DistrictLabelTests` |
| 44 | `Event.IsLocal` **türetilmiştir** | Davranış | 🟢 | `PanelEventDistrictTests.IsLocal_IsDerivedFromTheDistrict_NotFromTheForm` · `Update_RederivesIsLocal_…` |
| 45 | İlçe zorunlu + sözlükte silme yok | Davranış | 🟢 | `PanelEventDistrictTests.Create_IsRejected_When…` · `HomeDistrict_CannotBeRenamedOrDeactivated` · `Backfill` testleri |
| 46 | `OperatingDays` tek sahip; `0` yasak; uç elemez | Saf birim + davranış | 🟢 | `Unit/…/Transport/OperatingDaysTests` + `PanelTransportFieldModelTests.Endpoint_DoesNotFilterSchedulesByDay` · `Schedule_WithNoDay_IsRejectedAndSaysWhy` |
| 47 | `vehicle_type` metin; süzgeçte bilinmeyen süzmez | Saf birim + davranış | 🟢 | `Unit/…/Transport/TransportVehicleTypeTests` + `PanelTransportFieldModelTests.UnknownVehicleTypeFilter_FallsBackToTheFullList` |
| 48 | Kalkış noktası sözlükten; pasif değer kaydı kilitlemez | Davranış | 🟢 | `PanelTransportFieldModelTests.PassiveDeparturePoint_StaysSelectedOnAnExistingRoute` · `RouteWithADeactivatedDeparturePoint_CanStillBeEdited` |
| 49 | İstemcide boş/eksik `days` = "her gün" | İstemci tarafı (davranış, golden değil) | 🟠 | `mobile/test/features/transport/operating_days_test.dart` — yedi günün **tek tek** denetlendiği doğrulandı |
| 50 | "Kalktı" yalnız bugün çalışan sefer için | İstemci tarafı — **iddia iki yönlü** | 🟠 | `mobile/…/transport_screen_test.dart`: *"bugün çalışmayan seferin üstü ÇİZİLMEZ"* **ve** *"…saati geçen seferin üstü ÇİZİLİR"*. 📌 **Ön hipotez ÇÜRÜDÜ**: golden'a bağımlılık kalmamış |
| 51 | Panel dış origine bağlanamaz, satır içi `on*=` yok | **Kaynak taraması** + canlı yanıt + açılış kapısı | 🔴 | `Integration/Architecture/PanelExternalOriginTests` (kapsam: **yalnız** `KadirliApp.Web/Views/**/*.cshtml`) + `PanelContentSecurityPolicyTests` (canlı) + `Unit/Web/PanelAssetGuardTests`. Tarama `wwwroot/js/panel.js`'i ve `Views/` dışındaki hiçbir şeyi görmüyor |
| 52 | Moderasyon durumunu yalnız geçiş komutları yazar | **Kaynak taraması** (dosya adı deseni **elle**) + davranış + derleyici | 🔴 | `ModerationSingleOwnerTests` — modül kümesi `Approve*.cs`'ten **türetiliyor** ✅ ama taranan dosyalar hâlâ `Update*.cs` deseninde ❌ (12.11'in `ExtendMyAdCommand` dersinin **aynısı hâlâ ayakta**). Davranış ayağı: `PanelModerationOwnershipTests` |
| 53 | Moderasyon alanları `init` olmak zorunda | **Derleyici** + yansıma | 🟢🟢 | `ModerationSingleOwnerTests.EveryModeratedEntity_ExposesItsModerationFieldsAsInitOnly` (alan listesi tipin **kendisinden** türetiliyor) |
| 54 | `modified_after` **site-yerel** saatte | Saf birim (gidiş-dönüş) | 🟢 | `Unit/…/News/WordPressTimeWindowTests` — dönüşüm, çakışma payı ve yön ayrı ayrı iddia ediliyor. ⚠️ *"`DateTime.UtcNow` asla doğrudan yazılmaz"* yüzü taranmıyor (düşük etki: tek sahip küçük ve okunur) |
| 55 | `Source*` ↔ `*Override` iki sahip, ikisi de `init` | **Yansıma** + derleyici | 🟢🟢 | `Integration/Architecture/NewsSourceOwnershipTests` — alan listesi tipten türetiliyor (kaynak taraması **değil**) |
| 56 | Silmeyi yalnız `ReconcileNewsJob` öğrenir | Davranış | 🟢 | `Integration/News/NewsSyncTests.Reconcile_MarksMissingArticlesGone_…` · `Reconcile_RefusesToRun_WhenTheSourceReturnsNothing` |
| 57 | Senkron sessizce susar; tek gösterge tazelik damgası | Davranış + saf birim | 🟢 | `NewsSyncTests.EveryRun_LeavesAnAuditableRow_AndUpdatesTheFreshnessStamp` + `NewsSourceRulesTests.Freshness_*` |
| 58 | Haber durumu türetilir; `gone` > `archived` | Saf birim + davranış | 🟢 | `Unit/…/News/NewsStatesTests` + `PanelNewsTests.EditForm_HasNoVisibilityToggle` |
| 59 | Kategori görünürlüğü **dışlama** semantiği | Davranış | 🟢 | `PanelNewsTests.ExcludingACategory_HidesEveryArticleInIt_EvenIfTheyHaveOtherVisibleCategories` · `TheReversePreview_CountsOnlyArticlesThatWouldActuallyComeBack` |
| 60 | Aynı anda tek senkron koşusu (kısmi unique indeks) | **DB kısıtı** + davranış | 🟢🟢 | `PanelNewsTests.ASecondRunCannotBeOpenedWhileOneIsStillActive` · `TheSyncService_ReportsBlockedInsteadOfFailing_…` |
| 61 | Gövdenin tek çizim sahibi `NewsBody` | İstemci tarafı | 🟠 | `mobile/test/features/news/news_body_test.dart` — "istemci sunucudan geleni KIRPMAZ" doğrudan iddia ediliyor |
| 62 | "Kaydedilenler" anlık görüntü saklar | İstemci tarafı | 🟠 | `mobile/…/news_screen_test.dart` (`kaydedilen haber ağa çıkmadan listelenir`, `bozuk bir kayıt bütün listeyi düşürmez`) |
| 63 | Gövde görselleri aynalanır; **sağlama aynalamadan ÖNCE** | Davranış | 🟢 | `NewsSyncTests.SecondRun_DoesNotRewriteTheArticle_…` · `Backfill_DoesNotTouchTheChecksum_…` · `TheSameBodyImage_InTwoArticles_IsStoredOnce` + `Unit/…/News/NewsBodyImagesTests` |
| 64 | Haber bildirimi terminal — kural **üç katmanda** | Davranış + **DB kısıtı** | 🟢🟢 | `PanelNewsNotificationTests.TheDatabase_RefusesASecondNewsCampaignForTheSameArticle` · `TheUniqueIndex_DoesNotAffectOtherSources` + `NewsArticleTransitionTests.MarkNotificationSent_IsTerminal_…` |
| 65 | Gönderilebilirlik görünürlüğün **üç eksenini** sorar | Saf birim + davranış | 🟢 | `Unit/…/News/NewsNotificationRulesTests` (üç eksen ayrı ayrı) + `PanelNewsNotificationTests.AnArticleHiddenByCategoryExclusion_CannotBeNotified` |
| 66 | Görünmez haberin bildirimleri fiziksel düşer | Davranış | 🟢 | `PanelNewsNotificationTests.ArchivingANotifiedArticle_DeletesItsNotifications_ButKeepsTheCampaign` + `NewsNotificationTextTests` (gövde kendi kendine yeterli) |
| 67 | Bildirim tercihi **kaynağa göre** + geri doldurma | Davranış 🟢 / **kuruluma bağlı** 🔴 | 🔴 | `Integration/Panel/NotificationPreferenceAxisTests` — eksen ayrımı ve `MissingJsonKey_MaterialisesAsFalse` **kilitli**; `TheBackfill_LeftNoUserRowWithoutTheNewsKey` bozma turunda **yeşil kaldı** (migration bir kez koşar) = **T2**, biliniyor |

### Dağılım

| Risk | Adet | Maddeler |
|---|---|---|
| 🟢🟢 En düşük (derleyici / DB kısıtı) | 6 | 32 · 53 · 55 · 60 · 64 · (9 kısmen) |
| 🟢 Düşük (davranış / saf birim) | 45 | 1–5, 7–14, 20, 22–25, 28, 31, 33–48, 54, 56–59, 63, 65, 66 |
| 🟠 Orta (istemci · elle kapsam · ayna) | 9 | 6 · 18 · 27 · 29 · 30 · 49 · 50 · 61 · 62 |
| 🔴 Yüksek | 7 | **15 · 16 · 17 · 19 · 21 · 26** (yeni) · **51 · 52 · 67** (bilinen) |

> 🔑 **Faz A'nın alt kümesi budur: 16 madde** (7 🔴 + 9 🟠). Kalan 51 maddede kör bozma turu
> **yapılmayacak** (reçetenin kendi kuralı).

---

## 3. Ön hipotezlerin sonucu

Reçete yedi şüpheli saymıştı. Tasnif hepsine cevap verdi:

| Hipotez | Sonuç | Kanıt |
|---|---|---|
| **51** panel dış origin — tarama kapsamı | ✅ **DOĞRULANDI** | Tarama `KadirliApp.Web/Views/**/*.cshtml` ile sınırlı; `wwwroot/js/panel.js` ve `Views/` dışı hiç görülmüyor. Canlı CSP ayağı + varlık kapısı riski **hafifletiyor** ama taramanın kendisi kör |
| **52** moderasyon tek sahipliği — tarama ayağı | ✅ **DOĞRULANDI** | Modül kümesi `Approve*.cs`'ten türetiliyor, ama taranan dosya deseni hâlâ **elle**: `Update*.cs`. 12.11'in `ExtendMyAdCommand` dersi *aynı testte* hâlâ ayakta |
| **50** üstü çizili "kalktı" — iki yönlü mü | ❌ **ÇÜRÜDÜ** (iyi haber) | İki ayrı test var: çizilmeyen **ve** çizilen. Golden bağımlılığı kalmamış |
| **49 · 61 · 62** istemci tarafı | 🟠 **KISMEN** | Üçü de davranış testi (golden değil) ve iddiaları doğrudan. Risk testin *şeklinde* değil **ekseninde**: sunucu tarafı değişince mobil süit yeşil kalır |
| **67** geri doldurma | ✅ **DOĞRULANDI** (zaten biliniyordu) | T2 |
| **6** "TR günü 00:00 UTC" (4 kez tekrarlamış sınıf) | 🟠 **KISMEN — yeni bulgu** | Kilit var ama **kapsamı eksik**: test `eventDate` + `dutyDate` ölçüyor, sözleşmenin saydığı **`funeralDate`'i ölçmüyor** |
| **30** benzersiz ayraç — varlık mı benzersizlik mi | ❌ **ÇÜRÜDÜ (iddia şekli)** / ✅ **DOĞRULANDI (kapsam)** | İddia şekli sağlam: bellek-içi sıralama **kararlı** olduğu için ayraç yoksa da benzersiz değilse de test kırmızıya döner. Ama süpürme **yalnız Announcements** haritasında |
| `ArchitectureDocTests` ailesi | ✅ **DOĞRULANDI** | Atıfların *gerçekliğini* denetliyor (dosya var mı, başlık duruyor mu, satır numarası çivilenmiş mi) — maddenin **doğruluğunu** denetlemiyor. Bu tasnifin kendisi de kanıt: doküman *"1–22 `InvisibleContractsTests`"* diyor, dosyada **12 test** var ve 13–22 başka dosyalarda yaşıyor |

---

## 4. Faz 0'ın ürettiği YENİ bulgular (Faz B'nin gündemi)

Tasnif, plandaki şüphelilerden **bağımsız altı delik** buldu. Hepsinin ortak şekli:
*sözleşmenin yazılı yüzü ile iddia edilen yüzü aynı değil.*

| # | Bulgu | Bugün ne oluyor | Önerilen kapatma (12.11'in sorusu: taramanın erişemeyeceği yer neresi?) |
|---|---|---|---|
| **B1** | **Madde 16** — push `data` anahtarları | `SendPushNotificationsJob.BuildData` dört anahtar yazıyor; test yalnız `notificationId`'nin **varlığını** soruyor. `relatedType` → `related_type` yeniden adlandırılsa **deep-link ölür**, iki süit de yeşil kalır (mobil kendi elle yazdığı sözlükle test ediyor) | `BuildData`'nın çıktısını **anahtar kümesi olarak** iddia eden bir test + değerlerin **metin** olduğu; ideali sözlüğü mobilin `PushPayload.fromData`'sının okuduğu adlarla **paylaşılan bir sabit listeden** üretmek |
| **B2** | **Madde 26** — `?status=pending` public uçta etkisiz | Kural **vefat** modülünde ölçülü, **ilan** modülünde değil — oysa 10.5'te iletişim telefonlarıyla sızan **ilandı**. `else if` → `if` bozması bugün **hiçbir testi kırmaz** | `ModuleVisibilitySweepTests` desenine ilan satırı; daha iyisi: süpürmenin modül listesini **türetmesi** |
| **B3** | **Madde 15** — kategori filtresi tam eşleşme | Hiçbir test kök kategorinin alt kategori ilanlarını **getirmediğini** ölçmüyor. Filtre hiyerarşik yapılsa mobil kategori şeridi anlamsızlaşır, test yeşil kalır | İki seviyeli tek bir davranış testi (kök sorgusu alt kategori ilanını **görmemeli**) |
| **B4** | **Madde 17** — `unreadCount` filtreden bağımsız | `unreadOnly=true` isteğinde sayacın **toplamı** verdiği iddia edilmiyor; sayaç filtreye bağlansa rozet sessizce yanlışlaşır | Tek davranış testi: `?unreadOnly=true` yanıtındaki `unreadCount`, filtresiz yanıtınkiyle **aynı** olmalı |
| **B5** | **Madde 21** — `SlugHelper` tek sahip | Delegasyon kodda var ve yorumda anlatılıyor, ama **hiçbir test** ikinci bir gerçeklemeyi yakalamaz. `DbSeeder`'a bir `ToLowerInvariant()` kopyası geri gelse 10.9–11.15b hatası (`İ` → mükerrer mahalle) sessizce dirilir | Yansıma/tarama değil **davranış**: seeder'ın ürettiği slug ile `SlugHelper`'ınki Türkçe `İ` içeren girdide **eşit** olmalı |
| **B6** | **Madde 19** — izin öneki listesi | `[InlineData]` elle; gerçek aksiyon kümesinden türetilmiyor. Tuzak **dört kez** tekrarladı ve her seferinde *yeni aksiyonu listeye elle eklemek* çözüm oldu — yani koruma değil, ritüel | Kapsamı **türet**: panel controller'larının POST aksiyonlarını yansımayla topla, `update`'e düşen her yenisini **açık bir muafiyet listesi** olmadan kırmızı yap (12.11'in "taramanın kapsamı da elle tutulan bir listedir" dersinin doğrudan uygulaması) |
| **B7** | **Madde 6** — `funeralDate` | Sözleşme üç alan sayıyor, test ikisini ölçüyor | Var olan teste vefat ayağını ekle (ucuz) |

📌 **B1 ve B2 aynı sınıfın iki yüzü:** *sözleşme bir modülün adını taşıyor, kilit başka bir
modülde duruyor.* Bu, "iddiası zayıf test"in beşinci fazdan sonraki **altıncı biçimi** ve
denetimin ilk gerçek kazancı.

---

## 5. Sırada ne var

1. **T1/T2** (reçetenin ön koşulu, `Progress.md` → *"Test altyapısı"*). Faz A'ya girmeden
   kapatılmalı: biriken test kullanıcıları bozma turunun sonuçlarını zehirler.
2. **Faz A — bozma turu**, yalnız yukarıdaki **16 maddelik** alt kümede. Protokol madde başına:
   kuralı **anlamlı** şekilde boz (derlenmez hâle getirmek bozma değildir) → yalnız o maddenin
   testini koş → kırmızıya döndüğünü gör → geri al → bu tabloya `kilitli` / `tesadüfen yeşil` yaz.
   ⚠️ B1–B7 için bozma turu **gereksiz**: deliğin varlığı kod okunarak kanıtlandı, doğrudan
   Faz B'ye gidebilirler.
3. **Faz B — delikleri kapat.** Soru "testi genişletsem yeter mi?" değil:
   **"korumayı taramanın erişemeyeceği yere taşıyabilir miyim?"**
