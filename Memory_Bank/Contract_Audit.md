# 🔍 Görünmez Sözleşme Denetimi — Faz 0: TASNİF

> **Bu dosya ne işe yarar?** `ARCHITECTURE.md` §7'deki **67 görünmez sözleşmenin** her birini
> *kilidinin cinsine* göre etiketler. Soru "testi var mı?" değil (67'sinin de var),
> **"kilidi sahte mi?"**
>
> **Bu dosya kalıcıdır.** Sonraki oturumlar baştan tasnif etmez, buradan devam eder.
>
> ✅ **DURUM (13 Ağu 2026): Faz 0 · Faz B (B1–B7) · T1/T2 · Faz A — HEPSİ KOŞULDU.**
> 67 maddenin tamamı bugün 🟢 ya da 🟢🟢. Faz A'nın 10 maddelik kırılgan alt kümesinde
> **dört delik daha** bulundu ve kapatıldı (27 · 30 · 51 · 52); altısı **kilitli çıktı**.
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
| 6 | Gün alanları "TR günü, 00:00 UTC" | Davranış | 🟢 ✅ | `InvisibleContractsTests.DayOnlyDateFields_…` — **B7 ile kapatıldı**: `funeralDate` ayağı eklendi (üç alanın üçü de ölçülüyor) |
| 7 | Ulaşım saatleri iki biçimli duvar saati | Davranış | 🟢 | `InvisibleContractsTests.TransportDepartureTimes_AreDatelessWallClock_InTwoDifferentFormats` |
| 8 | `UpdateMyAd` sıra/kapak bilmez | Davranış | 🟢 | `InvisibleContractsTests.UpdateMyAd_AppendsNewImagesAsNonCover_…` |
| 9 | Görsel URL'leri **göreli** | Davranış + açılış kapısı | 🟢 | `InvisibleContractsTests.ImageUrls_AreReturnedRelative_…` + `Unit/Api/ProductionReadinessGuardTests` |
| 10 | Zarf + her yanıtta `traceId` | Davranış | 🟢 | `InvisibleContractsTests.EveryResponse_IsEnvelopedAndCarriesMeta` |
| 11 | `complaints.type` serbest metin | Davranış | 🟢 | `InvisibleContractsTests.ComplaintType_IsFreeText_ServerDoesNotValidateIt` |
| 12 | Yollar kebab-case | Davranış | 🟢 | `InvisibleContractsTests.RoutePaths_AreKebabCase_AndPascalCaseIs404` |
| 13 | Sayısal özellik InvariantCulture | Saf birim | 🟢 | `Unit/Application/Ads/AdSubmissionRulesTests` (`"2020,5"` reddi ayrı testte) |
| 14 | `select` değeri metinle + harf duyarlı | Saf birim | 🟢 | `AdSubmissionRulesTests` |
| 15 | `AdCategory` filtresi **TAM EŞLEŞME** | Davranış | 🟢 ✅ | **B3 ile kapatıldı**: `InvisibleContractsTests.AdCategoryFilter_IsAnExactMatch_NotAHierarchicalOne` (alt kategori ilanı kendi kategorisinde görünür, **kökte görünmez**) |
| 16 | Push `data` sözlüğünün anahtarları | Davranış + **tek sahip sabit** | 🟢 ✅ | **B1 ile kapatıldı**: adların tek sahibi `Application/Features/Notifications/PushDataKeys`; `PushNotificationsJobTests` anahtar kümesini **düz metin** iddia ediyor (sabiti yeniden adlandırmak testi kurtarmaz) |
| 17 | `unreadCount` gövdenin içinde ve **filtreden bağımsız** | Davranış | 🟢 ✅ | **B4 ile kapatıldı**: `NotificationsTests` — `?limit=1` isteğinde sayaç **2** kalmalı. ⚠️ İddia bilerek **sayfalamaya** bağlandı: yalnız `unreadOnly` üzerinden kurulan eşitlik bu uçta totolojidir ve hiçbir bozma onu kırmazdı |
| 18 | `relatedType` → mobil rota; tanınmayan tür iptal | İstemci tarafı | 🟢 ✅ | `notification_link_test.dart` — **Faz A'da kilitli çıktı**: tanınmayan türü duyuruya düşürmek testi kırdı |
| 19 | İzin eylemi aksiyon **adından** türer | Davranış + **türetilmiş kapsam** | 🟢 ✅ | **B6 ile kapatıldı**: `PanelModeratorPermissionTests.EveryWriteAction_SaysWhatItIs_InsteadOfSilentlyFallingBackToUpdate` — kapsam matris controller'larından **yansımayla türetilir**; adı hiçbir şey söylemeyen yazma aksiyonu **kırmızıdır**. 🔑 Sessiz varsayılan artık **yazılı karara** dönüştü |
| 20 | Menü · matris · `[PanelPermission]` aynı anahtar | Yansıma (türetilmiş) | 🟢 | `PanelModeratorPermissionTests.MenuModules_MatchThePermissionMatrixModules` |
| 21 | Slug üretiminin tek sahibi `SlugHelper` | Saf birim + davranış | 🟢 ✅ | **B5 ile kapatıldı**: `InvisibleContractsTests.SlugGeneration_HasASingleOwner_EvenThroughItsWrappers` — sarmalayıcı (`BusinessRules.Slugify`) helper'la **aynı çıktıyı** vermeli ve seed'lenen satırların slug'ları helper'ınkiyle eşleşmeli |
| 22 | Cache grup adları yalnız `CacheGroups` sabitleri | Yansıma (türetilmiş) | 🟢 | `Unit/Application/Caching/CacheContractTests` (grup kümesini kaynaktan türetiyor) |
| 23 | Panel sayaçları = public görünürlük tanımı | Davranış | 🟢 | `PanelBusinessRuleTests.DashboardActiveAds_ExcludesExpiredOnes` · `…Announcements_CountsOnlyPublishedOnes` |
| 24 | Bildirim hedefi yaşadığı sürece görünür | Davranış | 🟢 | `PanelBusinessRuleTests.NotificationList_Hides…` · `DeletingAnnouncement_AlsoRemovesItsNotifications` |
| 25 | Onay, süresi dolmuş ilana taze pencere verir | Davranış + saf birim | 🟢 | `PanelBusinessRuleTests.ApprovingExpiredAd_…` + `Unit/…/Moderation/ModerationTransitionTests` |
| 26 | `QueryAdDto.Status` yalnız panel yolunda okunur | Davranış | 🟢 ✅ | **B2 ile kapatıldı**: `InvisibleContractsTests.PublicAdsList_IgnoresTheStatusFilter_SoPendingAdsCanNeverLeak` — sözleşmenin adını taşıdığı **ilan** ucunda üç sorgu birden denetleniyor |
| 27 | Kesinti süren/planlı/bitti tanımı panel↔mobil | Saf birim (iki ayna) | 🟢 ✅ | **Faz A'da mobil ayak DELİK çıktı** (başlangıç sınırı hiç iddia edilmiyordu, bozma yeşil kaldı) → `power_outage_model_test.dart`'a *'tam BAŞLANGIÇ anında sürüyor'* eklendi; panel ayağı `PanelPowerOutageFilterTests` |
| 28 | Geri getirme ≠ yayına alma | Davranış | 🟢 | `PanelTrashTests.Restore_DoesNotPublishTheRecord` |
| 29 | Toplu aksiyon `…Selected` + tek-kayıt komutu | Davranış + **türetilmiş kapsam** | 🟢 ✅ | **Faz A'da kilitli çıktı** — `ApproveSelected` → `BulkApprove` bozması 7 testi kırdı; B6'nın yeni testi aksiyonu **adıyla** söyledi |
| 30 | Her sıralama anahtarı **benzersiz** ayraçla biter | Saf birim — **türetilmiş kapsam** | 🟢 ✅ | **Faz A'da kapsam deliği ölçüldü** (Campaigns'in ayracı düşürüldü, hiçbir test kırılmadı) → `PanelSortingTests.EverySortMapInTheProject_…` haritaları **yansımayla** geziyor |
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
| 49 | İstemcide boş/eksik `days` = "her gün" | İstemci tarafı (davranış) | 🟢 ✅ | `operating_days_test.dart` — **Faz A'da kilitli çıktı** (3 test kırmızı) |
| 50 | "Kalktı" yalnız bugün çalışan sefer için | İstemci tarafı — iddia iki yönlü | 🟢 ✅ | `transport_screen_test.dart` — **Faz A'da kilitli çıktı** (gün kontrolü düşürüldü → kırmızı) |
| 51 | Panel dış origine bağlanamaz, satır içi `on*=` yok | Kaynak taraması (**kapsam türetildi**) + canlı yanıt + açılış kapısı | 🟢 ✅ | **Faz A'da kapsam deliği ölçüldü**: `panel.css`'e Google Fonts `@import`'u üç ayağı da yeşil bıraktı → yeni `NoCommittedPanelAsset_LoadsAResourceFromAnExternalOrigin` `wwwroot`'u (lib hariç) **dizinden türeterek** tarıyor |
| 52 | Moderasyon durumunu yalnız geçiş komutları yazar | **Yansıma** + davranış + derleyici | 🟢 ✅ | **Faz A'da 12.11'in dersi HÂLÂ açıktı**: `Update*` desenine uymayan `ReviseAdCommand.cs` hiç taranmadı → yeni `EveryStatusCarryingCommand_CallsTheGuard_RegardlessOfItsFileName` kapsamı **tipten** kuruyor |
| 53 | Moderasyon alanları `init` olmak zorunda | **Derleyici** + yansıma | 🟢🟢 | `ModerationSingleOwnerTests.EveryModeratedEntity_ExposesItsModerationFieldsAsInitOnly` (alan listesi tipin **kendisinden** türetiliyor) |
| 54 | `modified_after` **site-yerel** saatte | Saf birim (gidiş-dönüş) | 🟢 | `Unit/…/News/WordPressTimeWindowTests` — dönüşüm, çakışma payı ve yön ayrı ayrı iddia ediliyor. ⚠️ *"`DateTime.UtcNow` asla doğrudan yazılmaz"* yüzü taranmıyor (düşük etki: tek sahip küçük ve okunur) |
| 55 | `Source*` ↔ `*Override` iki sahip, ikisi de `init` | **Yansıma** + derleyici | 🟢🟢 | `Integration/Architecture/NewsSourceOwnershipTests` — alan listesi tipten türetiliyor (kaynak taraması **değil**) |
| 56 | Silmeyi yalnız `ReconcileNewsJob` öğrenir | Davranış | 🟢 | `Integration/News/NewsSyncTests.Reconcile_MarksMissingArticlesGone_…` · `Reconcile_RefusesToRun_WhenTheSourceReturnsNothing` |
| 57 | Senkron sessizce susar; tek gösterge tazelik damgası | Davranış + saf birim | 🟢 | `NewsSyncTests.EveryRun_LeavesAnAuditableRow_AndUpdatesTheFreshnessStamp` + `NewsSourceRulesTests.Freshness_*` |
| 58 | Haber durumu türetilir; `gone` > `archived` | Saf birim + davranış | 🟢 | `Unit/…/News/NewsStatesTests` + `PanelNewsTests.EditForm_HasNoVisibilityToggle` |
| 59 | Kategori görünürlüğü **dışlama** semantiği | Davranış | 🟢 | `PanelNewsTests.ExcludingACategory_HidesEveryArticleInIt_EvenIfTheyHaveOtherVisibleCategories` · `TheReversePreview_CountsOnlyArticlesThatWouldActuallyComeBack` |
| 60 | Aynı anda tek senkron koşusu (kısmi unique indeks) | **DB kısıtı** + davranış | 🟢🟢 | `PanelNewsTests.ASecondRunCannotBeOpenedWhileOneIsStillActive` · `TheSyncService_ReportsBlockedInsteadOfFailing_…` |
| 61 | Gövdenin tek çizim sahibi `NewsBody` | İstemci tarafı | 🟢 ✅ | **Faz A'da zayıf iddia bulundu**: `<blockquote>` etiketleri silindiğinde test yeşil kaldı (metin duruyordu) → yeni iddia `Html`'e giden verinin **birebir aynı** olması |
| 62 | "Kaydedilenler" anlık görüntü saklar | İstemci tarafı | 🟢 ✅ | `news_screen_test.dart` — **Faz A'da kilitli çıktı** (3 test kırmızı) |
| 63 | Gövde görselleri aynalanır; **sağlama aynalamadan ÖNCE** | Davranış | 🟢 | `NewsSyncTests.SecondRun_DoesNotRewriteTheArticle_…` · `Backfill_DoesNotTouchTheChecksum_…` · `TheSameBodyImage_InTwoArticles_IsStoredOnce` + `Unit/…/News/NewsBodyImagesTests` |
| 64 | Haber bildirimi terminal — kural **üç katmanda** | Davranış + **DB kısıtı** | 🟢🟢 | `PanelNewsNotificationTests.TheDatabase_RefusesASecondNewsCampaignForTheSameArticle` · `TheUniqueIndex_DoesNotAffectOtherSources` + `NewsArticleTransitionTests.MarkNotificationSent_IsTerminal_…` |
| 65 | Gönderilebilirlik görünürlüğün **üç eksenini** sorar | Saf birim + davranış | 🟢 | `Unit/…/News/NewsNotificationRulesTests` (üç eksen ayrı ayrı) + `PanelNewsNotificationTests.AnArticleHiddenByCategoryExclusion_CannotBeNotified` |
| 66 | Görünmez haberin bildirimleri fiziksel düşer | Davranış | 🟢 | `PanelNewsNotificationTests.ArchivingANotifiedArticle_DeletesItsNotifications_ButKeepsTheCampaign` + `NewsNotificationTextTests` (gövde kendi kendine yeterli) |
| 67 | Bildirim tercihi **kaynağa göre** + geri doldurma | Davranış | 🟢 ✅ | **T2 ile kapatıldı**: ifade `Infrastructure/Persistence/NotificationPreferenceBackfill.Statement`'a çıkarıldı; `NotificationPreferenceAxisTests.TheBackfillStatement_AddsTheMissingKey_ButNeverOverwritesAnExplicitChoice` eski biçimli satırı **kendi eliyle** üretip aynı metni koşturuyor. Eski duman testi duruyor ama "kilitli" sayılmıyor |

### Dağılım

| Risk | Tasnif anında | **Faz B'den sonra (bugün)** | Maddeler |
|---|---|---|---|
| 🟢🟢 En düşük (derleyici / DB kısıtı) | 6 | 6 | 32 · 53 · 55 · 60 · 64 |
| 🟢 Düşük (davranış / saf birim) | 45 | **61** | + 6 · 15 · 16 · 17 · 19 · 21 · 26 (B1–B7 ✅) · 67 (T2 ✅) · 18 · 27 · 29 · 30 · 49 · 50 · 51 · 52 · 61 · 62 (Faz A ✅) |
| 🟠 Orta (istemci · elle kapsam · ayna) | 9 | **0** | — |
| 🔴 Yüksek | 7 | **0** | — |

> 🔑 **Faz A koşuldu ve bitti.** 10 maddelik kırılgan alt kümenin **altısı kilitli çıktı**
> (18 · 29 · 49 · 50 · 62 + 51/52'nin ana ayakları), **dördünde delik bulundu ve kapatıldı**
> (27 · 30 · 51 · 52). Kalan 57 maddede kör bozma turu **yapılmadı** — reçetenin kendi kuralı.
> ✅ Ön koşul T1/T2 de kapandı.

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

## 5. ✅ FAZ B — B1–B7 KAPATILDI (aynı oturum, 13 Ağustos 2026)

Yedisi de kapatıldı ve **yedisinin de bozma turu koşuldu**: kural *ihlal edilmiş ama çalışan*
hâle getirildi (derlenmez yapmak bozma değildir), yalnız o maddenin testi koşuldu, kırmızıya
döndüğü görüldü, sonra geri alındı.

| # | Kapatma | Bozma turu — kuralı böyle ihlal ettik | Sonuç |
|---|---|---|---|
| **B1** | Anahtar adlarının **tek sahibi** `PushDataKeys` (yeni); `PushNotificationsJobTests` anahtar kümesini **düz metin** iddia ediyor | `RelatedType = "related_type"` (snake_case'e çevirme refleksi) | 🔴 kırmızı ✅ |
| **B2** | `InvisibleContractsTests.PublicAdsList_IgnoresTheStatusFilter…` | `if (OnlyPublished && string.IsNullOrWhiteSpace(dto.Status))` — *"istemci status verdiyse ona uy"* | 🔴 kırmızı ✅ |
| **B3** | `InvisibleContractsTests.AdCategoryFilter_IsAnExactMatch…` | Süzgeci hiyerarşik yaptık (`\|\| x.Category.ParentId == …`) | 🔴 kırmızı ✅ |
| **B4** | `NotificationsTests` — `?limit=1`'de sayaç 2 kalmalı | `UnreadCount = items.Count(i => !i.IsRead)` (sayacı **listeden türetme**) | 🔴 kırmızı ✅ |
| **B5** | `InvisibleContractsTests.SlugGeneration_HasASingleOwner…` | `BusinessRules.Slugify`'a `ToLowerInvariant().Replace(" ","-")` kopyası (10.9'un birebir hatası) | 🔴 kırmızı ✅ |
| **B6** | `PanelModeratorPermissionTests.EveryWriteAction_SaysWhatItIs…` — kapsam **yansımayla türetiliyor** | `ActionFor`'dan `"SendNotification"` önekini sildik (12.15'te elle eklenmişti) | 🔴 kırmızı ✅ — test aksiyonu **adıyla** söyledi: `NewsAdminController.SendNotification` |
| **B7** | `DayOnlyDateFields_…`'a `funeralDate` ayağı | Projeksiyonda `x.FuneralDate.AddHours(3)` (*"TR saatiyle gösterelim"* refleksi) | 🔴 kırmızı ✅ |

**Testler:** backend 1106 → **1110**. Mobil değişmedi (822).

### 🔑 B6 hem düzeltme hem ölçüm oldu

Yeni yapısal test **ilk koşusunda iki gerçek vaka buldu**: `NewsAdminController.ResetOverrides`
ve `NewsAdminController.Feature`. Yani madde 19'un tuzağı, sayılan dört tekrardan sonra
sessizce **beşinci ve altıncı** kez tekrarlamıştı ve kimse fark etmemişti.

İkisinin de izni **bilinçli olarak değiştirilmedi** (davranış değişikliği bu denetimin kapsamı
değil) ama artık **yazılı**: testteki `deliberateFallbacks` listesine gerekçeleriyle girdiler.
⚠️ `Feature` sınırda bir karar — manşet şeridi vatandaşın ilk gördüğü yer; ileride `approve`
kovasına taşınırsa **adı da** değişmeli, yoksa o satır sessizce yalan söylemeye başlar.

🔑 **Değişen şey listenin içeriği değil, varsayılanın yönü:** eskiden adı bir şey söylemeyen
aksiyon **sessizce `update`'e** düşerdi; şimdi **kırmızıya** düşüyor ve üç seçenek sunuluyor
(adı değiştir · öneki ekle · gerekçesiyle listeye yaz). Ritüel, kapıya dönüştü.

---

## 6. ✅ T1 / T2 — Faz A'nın ön koşulu (aynı oturum)

**T1** — dört sınıf (`PanelNewsNotificationTests` · `PanelPushCampaignTests` ·
`PanelPowerOutageNeighborhoodTests` · `PushNotificationsJobTests`) artık kendi `users`
satırlarını siliyor; kapsam **dar** (yalnız kendi telefon numaraları).
🐛 İlk yazım **dört testi kırdı**: kullanıcı silme, `InitializeAsync`'in sonunda da çağrılan
temizliğin içine konmuştu → kurulum kendi kurduğunu siliyordu. 🔑 *Temizliğin **kapsamı**
kadar **çağrıldığı yer** de sözleşmenin parçasıdır.*

**T2** — karar: **SQL paylaşılan bir sabite çıkarıldı** (`NotificationPreferenceBackfill.Statement`).
🔬 Planın gerekçesi **yanlıştı ve ölçümle düzeltildi**: test DB'si koşular arasında *yeniden
kullanılmıyor* (Testcontainers her koşuda yeni konteyner kurar). Gerçek sebep: migration **boş**
bir `users` tablosunda koşar, satırları sonradan EF yazar ve EF **tam** JSON yazar → anahtarsız
satır test ortamında **hiç doğmaz**. Bu ayrım pahalıydı: yanlış sebep, planın 2. seçeneğine
(*tek kullanımlık veritabanı*) götürüyordu — **o çözüm işe yaramazdı**.
🔬 Bozma turu bir şey daha ölçtü: "açık tercih ezilmez" iddiasını **iki** mekanizma birden
koruyor (`WHERE` + `||` operand sırası); **yalnız birini** bozmak testi yeşil bırakıyor, ikisi
birden kırmızıya döndürüyor. Derinlemesine savunma — ama iddianın **davranış** olarak
yazılması gerektiğini gösterdi.

---

## 7. ✅ FAZ A — BOZMA TURU (13 Ağustos 2026, aynı oturum)

10 maddelik kırılgan alt kümenin **hepsinde** koşuldu. Protokol madde başına: kuralı
**anlamlı** (derlenebilir, çalışan) biçimde boz → yalnız o maddenin testini koş → sonucu
kaydet → geri al.

| # | Bozma | Sonuç |
|---|---|---|
| **18** | Tanınmayan `relatedType`'ı duyuru rotasına düşür | 🔴 **kilitli** |
| **29** | `ApproveSelected` → `BulkApprove` | 🔴 **kilitli** (7 test; B6'nın testi aksiyonu **adıyla** söyledi) |
| **49** | `days` boşken "hiçbir gün" say | 🔴 **kilitli** (3 test) |
| **50** | Üstü çizili kararından gün kontrolünü düşür | 🔴 **kilitli** (golden değil, davranış testi) |
| **62** | Anlık görüntüden başlık/özeti düşür | 🔴 **kilitli** (3 test) |
| **51-a** | Görünüme `unpkg` script'i ekle | 🔴 **kilitli** |
| **51-b** | CSP'ye `'unsafe-inline'` ekle | 🔴 **kilitli** |
| **52-a** | `UpdateAdCommandHandler`'dan guard çağrısını sil | 🔴 **kilitli** |
| **27** | `isActive`'i "başlangıç anı hariç" yap | 🟢 **YEŞİL KALDI → delik** |
| **30** | `Campaigns.end_asc`'ten `ThenBy(Id)`'yi düşür | 🟢 **YEŞİL KALDI → delik** |
| **51-c** | `panel.css`'e Google Fonts `@import`'u ekle | 🟢 **YEŞİL KALDI → delik** |
| **52-b** | `Update*` desenine uymayan, guard çağırmayan `ReviseAdCommand.cs` ekle | 🟢 **YEŞİL KALDI → delik** |
| **61** | İstemcide `<blockquote>` etiketlerini sil | 🟢 **YEŞİL KALDI → delik** |

### Bulunan beş delik ve kapatılışları

| # | Delik | Kapatma |
|---|---|---|
| **27** | Panel başlangıç sınırını (`dâhil`) kilitliyor, **mobil ayak hiç iddia etmiyordu** — ayna tek taraflı | `power_outage_model_test.dart`: *"tam BAŞLANGIÇ anında kesinti sürüyor sayılır"* |
| **30** | Süpürme yalnız **Announcements** haritasını geziyordu (8 haritadan 1'i) | `EverySortMapInTheProject_…`: harita listesi `PanelSorts`'tan **yansımayla** okunuyor; iki satırın tüm alanları eşit tutulup her anahtarda kararlılık ölçülüyor |
| **51** | Tarama yalnız `Views/**` — aynı bağımlılık `panel.css`'e bir `@import` olarak yazılabilirdi ve **üç ayak da yeşil kalıyordu** | `NoCommittedPanelAsset_LoadsAResourceFromAnExternalOrigin`: `wwwroot` (lib hariç) **dizinden türetilerek** taranıyor; yorumlar eleniyor (Tailwind lisans başlığı bir *yükleme* değil) |
| **52** | 🔴 **12.11'in dersi bu dosyada hâlâ ayaktaydı:** modül kümesi türetiliyordu ama dosyalar `Update*.cs` **deseniyle** bulunuyordu | `EveryStatusCarryingCommand_CallsTheGuard_RegardlessOfItsFileName`: kapsam **tipten** kurulur (moderasyonlu modüllerin `Commands` ad alanındaki `Status` taşıyan her `IRequest<>`); guard komutun **kendi klasöründe** aranır |
| **61** | *"İstemci kırpmaz"* iddiası **metnin** kaldığına bakıyordu; etiket silinince metin duruyor | İddia doğru değişmeze çevrildi: `Html`'e giden veri, sunucudan gelenin **birebir aynısı** |

🔑 **Faz A'nın en değerli bulgusu 52:** 12.11 korumayı derleyiciye taşımıştı ama *taramanın
kendisi* aynı kalmıştı — ve tam olarak aynı biçimde delikti. Ders üçüncü kez doğrulandı:
**bir taramanın kapsamı da elle tutulan bir listedir**; çözüm listeyi büyütmek değil,
kapsamı **türetmek**.

📌 **Beş deliğin dördü "kapsam", biri "iddia şekli".** Yani bu projede zayıf test, çoğunlukla
*yanlış şeye bakan* test değil, **doğru şeye ama dar bir kümede** bakan testtir.

**Testler:** backend 1111 → **1114**, mobil 822 → **824**.

---

## 8. Sırada ne var

**Denetimin kendisi bitti.** Faz 0 (tasnif) · Faz B (B1–B7) · T1/T2 · Faz A — hepsi koşuldu;
67 maddenin tamamı bugün 🟢 ya da 🟢🟢 ve **on iki delik** kapatıldı (B1–B7 + 27 · 30 · 51 · 52 · 61).

Bu dosya bundan sonra **bakım** dosyasıdır:

1. **Yeni bir görünmez sözleşme yazarken** buraya bir satır ekle: madde no, kilidin **cinsi**
   ve neden o cinsin yeterli olduğu (`CODE_REVIEW_CHECKLIST` §10'da da bir satırı var).
2. **Bir kilit "kaynak taraması" ya da "elle tutulan liste" cinsindense** kapsamını sor:
   *dizinden mi, tipten mi, elden mi geliyor?* Faz A'da bulunan beş deliğin **dördü** kapsamdı.
3. **Yeni bir test yazdığında** iddiayı yazdıktan sonra sor: **"bunu nasıl bozardım?"** —
   somut cevabı yoksa iddia totoloji olabilir (T2 ve B4'te birer kez yaşandı).
4. **Kalan kırılgan cins yok** ama tek istisna dosyada dürüstçe yazılı: madde 67'nin
   *duman testi* ayağı (`TheBackfill_LeftNoUserRowWithoutTheNewsKey`) hâlâ vakumdur —
   gerçek kilit `TheBackfillStatement_…`.
