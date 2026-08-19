# 🔍 Görünmez Sözleşme Denetimi — Faz 0: TASNİF

> **Bu dosya ne işe yarar?** `ARCHITECTURE.md` §7'deki **görünmez sözleşmelerin** her birini
> *kilidinin cinsine* göre etiketler. Soru "testi var mı?" değil (hepsinin var),
> **"kilidi sahte mi?"**
>
> 📌 **Bugün 84 madde.** Denetim 67 madde üzerinde koşuldu (aşağıdaki tablo); **68–70**
> denetimden sonra Faz 12.7'de, **71–74** Faz 12.16'da, **75–77** Faz 12.17'de,
> **78–80** Faz 12.19'da, **81** Faz 12.20a'da ve **82** Faz 12.21b'de eklendi — hepsinin kaydı bu dosyanın
> sonundaki bölümlerde.
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
| 19 | İzin eylemi aksiyon **adından** türer | Davranış + **türetilmiş kapsam** | 🟢 ✅ | ➕ **13 Ağu:** B6'nın bulduğu `Feature` **`approve`'a taşındı** (manşet şeridi = "içeriği şehre ulaştırma" kararı; 5. tekrar) — kilit teori + davranış testiyle iki ayaklı. **B6 ile kapatıldı**: `PanelModeratorPermissionTests.EveryWriteAction_SaysWhatItIs_InsteadOfSilentlyFallingBackToUpdate` — kapsam matris controller'larından **yansımayla türetilir**; adı hiçbir şey söylemeyen yazma aksiyonu **kırmızıdır**. 🔑 Sessiz varsayılan artık **yazılı karara** dönüştü |
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

| 68 | Sosyal jetonun **`aud`**'u doğrulanır (+ `iss` · süre · **RS256** · fail-closed) | Saf birim (**iki yönlü**) + davranış | 🟢 | `Unit/Infrastructure/SocialTokenVerifierTests` — 🔑 **kapsam gerçek doğrulayıcıdan geliyor**: sahte olan yalnız JWKS, jetonlar **gerçek RSA ile imzalı**. ⚠️ İddia **iki yönlü**: `TokenIssuedForAnotherApp_IsRejected_…` + `TheSameToken_IsAccepted_OnceItsAudienceIsOneOfOurs` — ikincisi olmadan "hiçbir jetonu kabul etme" gerçeklemesi de yeşil kalırdı (§7 madde 50'nin dersi). Uçta karşılığı `SocialLoginTests.TokenIssuedForAnotherApp_IsRejectedByTheEndpoint` (doğrulayıcının **bağlı olduğunu** kanıtlar — kural doğru yazılıp pipeline'a bağlanmamış olabilir, §7 madde 51'in iki-ayak dersi) |
| 69 | E-posta eşleşmesiyle **otomatik bağlama YASAK**; eşleştirme yalnız `(provider, sub)` | Davranış + **DB kısıtı** | 🟢🟢 | `SocialLoginTests.AMatchingEmail_DoesNotLinkTheAccountAutomatically` (aynı e-posta, farklı `sub` → **yeni kullanıcı**) · `AnIdentityAlreadyLinkedElsewhere_CannotBeStolen`. Benzersizlik `ix_user_identities_provider_provider_user_id`'de, "sağlayıcı başına tek bağlantı" `ix_user_identities_user_id_provider`'da — **ikisi de veritabanında**, yani kodda unutulsa bile INSERT reddediyor |
| 70 | **Sosyal giriş telefonu ATLAMAZ**; jeton türleri ayrı; silmede kimlikler gider | Saf birim + davranış | 🟢 | `JwtProviderSocialTokenTests.SocialTempToken_CannotBeUsedAsThePhoneRegistrationToken` (+ ters yön + refresh/access) · `SocialLoginTests.NewSocialUser_GetsARegistrationCarrier_NotASession` · `SocialToken_CannotStandInForThePhoneRegistrationToken` · `DeletingTheAccount_AlsoRemovesTheSocialIdentities_SoTheyCanRegisterAgain` (son test **iki iddiayı birden** tutuyor: satır gitti **ve** aynı hesapla yeniden kayıt açılabiliyor) |

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
4. **Kalan kırılgan cins yok.** Tek istisna dosyada dürüstçe yazılı ve 13 Ağu'da **adıyla da
   söylenir hâle getirildi**: madde 67'nin *duman testi* ayağı bu ortamda vakumdur →
   `SmokeCheck_NoUserRowLacksTheNewsKey_VacuousOnAFreshDatabase` (gerçek kilit
   `TheBackfillStatement_AddsTheMissingKey_ButNeverOverwritesAnExplicitChoice`).
   🔑 Test silinmedi: gerçek bir ortamda (üretimden geri yüklenmiş bir veritabanında) değeri
   var — ama **yeşil ama boş bir güvence, testsizlikten kötüdür**, o yüzden adı sınırını söylüyor.

---

## 9. Bakım kaydı — 13 Ağustos 2026 (Faz 12.7, sosyal giriş)

Üç yeni madde eklendi: **68 · 69 · 70**. Dağılım artık **🟢🟢 8 · 🟢 62 · 🟠 0 · 🔴 0** (70 madde).

Bu üçünde §8'in üç sorusuna verilen cevaplar:

1. **Kilidin cinsi ne?** 68 saf birim (+ uçta bir davranış ayağı), 69 **veritabanı kısıtı**
   (🟢🟢), 70 saf birim + davranış.
2. **Kapsam nereden geliyor?** 68'in kapsamı **gerçek doğrulayıcıdan** geliyor: sahte olan
   yalnız anahtar sunucusu, jetonlar gerçek RSA ile imzalı ve gerçek biçimde. Yani "sahte
   doğrulayıcı yazıp kendi akışımızı test etmek" tuzağına düşülmedi — o yol seçilseydi bu
   fazın **bir numaralı kuralı hiç kilitlenmemiş** olurdu.
3. **"Bunu nasıl bozardım?"** — 68'de cevabı vardı ve **iddiayı değiştirdi**: `aud` kontrolünü
   kapatan bir gerçekleme *tek yönlü* iddiayı geçemez ama **hiçbir jetonu kabul etmeyen** bir
   gerçekleme geçerdi. Bu yüzden ikinci yön (`TheSameToken_IsAccepted_OnceItsAudienceIsOneOfOurs`)
   eklendi: birebir aynı jeton, yalnız `aud` listesi değiştiği için kabul ediliyor.
   📌 Bu tam olarak B4'te öğrenilen dersin (*"iddiam totoloji mi?"*) ikinci uygulanışı.

### ✅ Bozma turu KOŞULDU (14 Ağustos 2026, kullanıcı isteğiyle aynı oturumda)

Üç maddenin üçünde de kural bilerek bozuldu ve sonuç **ölçüldü**:

| Bozma | Beklenen | Ölçülen |
|---|---|---|
| `ValidateAudience = false` | Kırmızı | 🔴 **4 test**, iki katmanda birden: `TokenIssuedForAnotherApp_IsRejected_…` · `TheSameToken_IsAccepted_OnceItsAudienceIsOneOfOurs` (saf) + `TokenIssuedForAnotherApp_IsRejectedByTheEndpoint` · `ARejectedSocialAttempt_IsRecordedForThePanel` (uç) |
| `GuardIdentityIsFreeAsync` kapısı devre dışı | Kırmızı | 🔴 `AnIdentityAlreadyLinkedElsewhere_CannotBeStolen` |
| `ValidateTempToken`'dan `token_type` kontrolü silindi | Kırmızı | 🟢 **YEŞİL KALDI — delik bulundu** ⬇ |

🐛 **MADDE 70'İN KİLİDİ SAHTE ÇIKTI ve bozma turu onu yakaladı.**
`SocialTempToken_CannotBeUsedAsThePhoneRegistrationToken` doğru davranışı ölçüyordu ama
**yanlış sebepten** geçiyordu: bugünkü sosyal jetonun `phone` claim'i **zaten yok**, yani
`token_type` kontrolü tamamen silindiğinde bile metot `null` döndürüyor. Sözleşme
*"türler ayrıdır"* diyordu, test ise yalnızca *"sosyal jetonda telefon yok"*u ölçüyordu.

🔴 **Neden önemli:** bugün iki bağımsız sebep koruyor ama biri **tesadüfi**. Sosyal jetona
yarın bir `phone` claim'i eklenirse (ör. *"sağlayıcıdan gelen telefonu ön dolduralım"*) ya da
iki üretici ortak bir yardımcıya çekilirse, ayakta kalan **tek** koruma `token_type` olur —
ve onu silen değişikliği **hiçbir test yakalamazdı**. Sonuç madde 70'in tam olarak
engellediği şey olurdu: **OTP'siz kayıt**.

✅ **Kapatıldı:** `ASocialTypedToken_IsRejectedAsAPhoneToken_EvenWhenItCarriesAPhoneClaim`
jetonu **elle** üretiyor (sosyal türde **ama telefon taşıyan**), yani tesadüfi korumanın
devre dışı kaldığı hâli kuruyor; ikinci yön (`APhoneTypedToken_WithTheSameShape_IsAccepted`)
de eklendi. Aynı bozma tekrarlandı → **kırmızı**. Kilit artık gerçek.

📌 Bu, *"iddiası zayıf test"* sınıfının **altıncı** tekrarı (12.11 tarama kapsamı · 12.6
golden toleransı · 12.13 yanlış nesneye bakan test · 12.14 taşma testi · 12.15b migration ·
**12.7 tesadüfi koruma**) — ve **ilk kez bozma turu tarafından yakalandı**, denetim turunda
değil. 🔑 Ders: *iki bağımsız sebep koruyorsa, testin HANGİSİNİ tuttuğunu ölç;* aksi hâlde
tesadüfi olan kaybolduğunda kilit de kaybolur ve kimse fark etmez.

| 71 | **Rıza, kullanıcının GÖRDÜĞÜ sürüme yazılır**; yayında olmayan sürüme yazılmaz | Davranış (gerçek Postgres) | 🟢 | `LegalConsentTests.Register_RejectsAConsentGivenToASupersededVersion` — kabul edilen kümenin tek sahibi `LegalConsentWriter.LiveVersionsAsync`. 🔬 **Bozma turunda ölçüldü:** doğrulamayı *belgeye* çevirmek testi kırmadı, çünkü asıl yükü **live süzgeci** taşıyor; sürümü sunucunun kendisinin seçtiği gerçek yanlış gerçeklem yazılınca **5 test kırmızı**. Yani kilit gerçek, ama **taşıyıcısı `Validate` değil `LiveVersionsAsync`** — kapsamı daraltan bir değişiklik oraya dokunacaktır |
| 72 | **Yayınlanmış sürüm değiştirilemez**; değişiklik = yeni sürüm; en fazla bir yayında sürüm | **Derleyici** + davranış + **DB kısıtı** | 🟢🟢 | Üç ayak: alanlar `init` → `CS8852` (ölçen şey derlemenin kendisi) · `LegalImmutabilityStructureTests` **yansımayla** o güvencenin sökülmesini kilitler (kapsam **tipten**; elle tutulan yalnız muafiyetler) · `LegalDocumentVersionTests` + `PanelLegalTests.APublishedVersion_CannotBeEditedFromThePanel_AndTheTextIsUntouched` (iddia dönüş değerine **değil metnin kendisine** bakar) · kısmi unique indeks `ix_legal_document_versions_one_live_per_document` · `LegalPublishTests` geçişi **10 kez** tekrarlar |
| 73 | **Rıza satırı kullanıcıyla AYNI işlemde yazılır**; reddedilen kayıt iz bırakmaz | Davranış (gerçek Postgres) | 🟢 | `LegalConsentTests.ARejectedRegistration_LeavesNoUserBehind_SoThePhoneStaysRegisterable` — iddia **ikinci denemenin başarısı**, "satır var mı" değil: hasarın kendisini ölçüyor. Bozma turu (SaveChanges'i doğrulamadan önceye almak) → **kırmızı** |
| 74 | **Hesap silinince rıza kaydı KALIR** (12.7'nin tersi) | Davranış + **DB kısıtı** | 🟢🟢 | `LegalConsentTests.DeletingTheAccount_KeepsTheConsentRecord_ButAnonymisesTheUser` — **iki yönlü**: rızanın durduğu *ve* kullanıcının anonimleştiği ölçülür (yoksa "hiçbir şey silmeyen" bir gerçeklem de yeşil kalırdı). FK'lar `Restrict`. Bozma turu (silme eklemek) → **kırmızı** |

📌 **12.16'nın kalıcı dersi — *rastgeleliğe bağlı bir hata, tek koşuluk bir testle
kilitlenemez.*** Madde 72'nin "en fazla bir yayında sürüm" ayağı ilk yazımda **gerçekten
kırıktı**: yürürlükten kaldırma ile yayınlama tek `SaveChanges`'teydi ve kısmi unique indeks
**deyim başına** denetlendiği için ihlal ediliyordu. Test üç kez üst üste **yeşil** koştu;
ölçüldüğünde **8 koşudan 5'i** düşüyordu. Sebep EF'in UPDATE'leri **birincil anahtar
sırasına** göre göndermesi ve anahtarların `gen_random_uuid()` olması — yani hata, GUID
sıralamasının şansına bağlıydı. 🔑 Kilit yazarken sorulacak yeni soru: *bu hata her koşuda mı
çıkar, yoksa bir olasılıkla mı?* Olasılıksa **tekrar** şart (`LegalPublishTests`: 10 tur).

---

## Faz 12.17 — KVKK mobil (madde 75–77)

| # | Sözleşme | Kilit cinsi | Risk | Kilidi taşıyan dosya + **iddianın şekli** |
|---|---|---|---|---|
| 75 | **Rıza kutusu ÖN İŞARETLİ OLAMAZ**; kararın tek sahibi `ConsentSelection` | **Saf birim + davranış** (istemci) | 🟢 | `mobile/test/features/legal/consent_selection_test.dart` (saf: `initial` boş küme **ve** o boşluğun butonu kapattığı — ikinci yön şart, yoksa "kutular boş ama kayıt yine tamamlanır" bir gerçeklem de yeşil kalırdı) + `register_consent_test.dart` (**davranış**: `Checkbox.value == false` *ve* `AppButton.onPressed == null` *ve* sebebin ekranda yazdığı) + `reconsent_test.dart` (eski onayın yeni sürüme **taşınmadığı**). ⚠️ Yalnız saf test yetmezdi: ekran kendi başlangıç değerini üretmeye başlasa saf test yeşil kalırdı. 🔬 Bozma turu: `initial` her kutuyu işaretledi → **üç dosya birden kırmızı** |
| 76 | **Hukuki metin gösterilemiyorsa kayıt AÇILMAZ** (§5'in bilinçli tersi) | **Davranış** (istemci) | 🟢 | `register_consent_test.dart`: `metinler alınamazsa kayıt AÇILMAZ ve sebebini söyler` — iddia **üç ayaklı**: hata şeridi görünür · buton `onPressed == null` · `POST /v1/auth/register` **hiç çağrılmaz** (son ayak kritik: "buton kapalı" görünüp isteğin yine gitmesi mümkün). ⚠️ Test `apiRetry`'ın iki tekrarını **bekler** ve o sırada da butonun kapalı kaldığını ölçer → `AsyncLoading` dalı da kapsanır. İkinci kilit `reconsent_test.dart`'ın `kayıt akışı yarım kalmışken hukuki metin AÇILABİLİR` testi: `AppRoutes.isLegalReading` istisnası kalkarsa "oku" bağlantısı kullanıcıyı kayıt ekranına fırlatır ve **okumadan onaylamaktan başka yol kalmaz**. 🔬 Bozma turu: iki ayrı bozma → **iki ayrı kırmızı** |
| 77 | **Rıza kaydının işaret ettiği metin, SAHİBİ tarafından okunabilir** (`GET /v1/legal/versions/{id}`) | **Davranış** (gerçek Postgres) | 🟢 | `Integration/Legal/LegalVersionEndpointTests.cs` — **iki yönlü** (§7 madde 68'in dersi): `ADraftVersion_IsNotFound…` tek başına *hiçbir sürümü döndürmeyen* bir gerçeklemede de yeşil kalırdı, bu yüzden `ASupersededVersion_IsStillReadable…` ve `ALiveVersion_SaysItIsLive…` birlikte duruyor (ikincisi `isLive`'ın **sabit false** dönmediğini kilitler — yoksa ekran yürürlükteki metnin üstüne de "artık geçerli değil" basardı). Ayrıca `ADeactivatedDocumentsVersion_IsStillReadable…`: kanıt, yöneticinin bir panel anahtarıyla kaybolamaz. 🔬 Bozma turu: taslak kontrolü kaldırıldı → **kırmızı** |

📌 **12.17'nin kalıcı dersi — *kuralın "tek karakterle bozulabilir" olması, kilidin cinsini belirler.***
Madde 75 bir `const {}` ile bozulabiliyor ve bozulduğunda **hiçbir şey hata vermiyor**: uygulama
çalışır, kayıt hızlanır, log temizdir — yalnız toplanan bütün rızalar hukuken **geçersiz** olur.
Böyle bir kuralda saf test *gerekli ama yeterli değil*: kuralın sahibi değişmeden **çağıranın**
onu atlaması mümkün, o yüzden davranış ayağı da şart. 🔑 Kilit yazarken sorulacak soru:
*bu kural bozulduğunda ortaya çıkan şey bir hata mı, yoksa sessizce geçersiz bir kayıt mı?*

---

## Faz 12.19 — denetimin bulduğu üç delik (madde 78–80)

| # | Sözleşme | Kilit cinsi | Risk | Kilidi taşıyan dosya(lar) |
|---|---|---|---|---|
| 78 | **Yalnız geliştirmeye açık komut, ortam kapısını kendi içinde taşımaz** — kapı boru hattında ve **en başta**; panel aksiyonu `[HttpPost]` + Production'da 404 | **Saf birim + yansıma + davranış** (üç ayak) | 🟢 | `Unit/Application/Common/DevelopmentOnlyBehaviorTests.cs` (saf, **iki yönlü**: aynı komut yalnız ortam değişince geçiyor → reddin sebebi *gerçekten* ortam; ⚠️ `Staging`/`Test` satırları bilinçli — kapı `!IsProduction()` yazılsaydı o iki ortamda **sessizce açılırdı**) + `Integration/Architecture/DevelopmentOnlyCommandTests.cs` (**yansıma**: boru hattı kaydı **ve sırası**, panel aksiyonlarının POST + ortam kapısı, `MockDataSeeder`'a host'tan doğrudan erişim yok; kapsam `IDevelopmentOnlyCommand`'i uygulayan **tiplerden** türer) + `Integration/Panel/PanelSeedActionTests.cs` (davranış: GET **405** · token'sız POST 400 · moderatör denied · Production **404** · buton çizilmiyor · **denetim izi düşüyor**) |
| 79 | **Moderasyon durumunun DEĞERİ de tek sahiplidir** (`AdStatuses.Approved`, ham `"approved"` değil) | **Yansıma + kaynak taraması** (kapsam **iki yandan da** türetilir) | 🟢 | `Integration/Architecture/ModerationSingleOwnerTests.cs` → `NoModeratedEntity_WritesARawStatusLiteral` (varlıklar `ModeratedEntities()`'ten, **yasak kelime dağarcığı `*Statuses` sınıflarının sabitlerinden yansımayla**) + `EveryModeratedEntity_ActuallyUsesTheStatusConstants` (ters yön: sabitler ölü kod olmasın — silinen dört enum tam olarak öyleydi) |
| 80 | **Yorumdaki atıf (test adı · `Tip.Üye` · dosya yolu) gerçek bir şeye işaret eder** | **Doküman testi (dizinden türetilen kapsam)** | 🟠 | `Integration/Architecture/CommentReferenceTests.cs` — üç ayak: test adı · nitelikli `<see cref>` · **dosya yolu**. Kapsam `**/*.cs`'ten türer |

### 🟠 80 neden 🟢 DEĞİL — ve bunu kendisi yazıyor

Bu, denetimdeki **tek bilinçli 🟠**. Sebebi kapsam değil, **iddianın sınırı**: tarama
*sarkan işaretçiyi* yakalar, **yanlış iddiayı yakalayamaz.** 12.19b'nin düzelttiği `User.cs`
yorumu ikisini birden taşıyordu — atıf kırıktı **ve** cümle ölçümün tersini söylüyordu; ikinci
yarısı bu projede daha tehlikeli olan yarıydı (bir migration'ın varlık sebebini yalanlıyordu).
Madde 67'nin `SmokeCheck_…_VacuousOnAFreshDatabase` adlandırmasıyla aynı karar: kilidin eksik
olduğu yer **kilidin kendi belgesinde yazılı**, ki kimse *"yorumlar denetleniyor"* sanmasın.

### 🐛 Bozma turu — 15 kilit, 15 kırmızı (ikinci denemede)

İlk turda **14/15** kırmızıydı; **13 numaralı bozma yeşil kaldı ve haklıydı.** Bozma
`<see cref="DevelopmentOnlyBehavior{TRequest,TResponse}.OlmayanUye"/>` biçiminde **jenerik**
bir atıftı; testin deseni (`(?<type>\w+)`) `{…}` bloğuna hiç uymuyordu ve jenerik tiplerin
`Type.Name`'i **arite soneki** taşıdığı için (`DevelopmentOnlyBehavior\`2`) sözlükte de
bulunamıyordu. İki ayrı delik, aynı yönde: jenerik atıflar **hiç denetlenmiyordu**.
🔑 Bu, projenin *"kapsam dizinden mi, tipten mi, elden mi?"* sorusunun bir üçüncü biçimi —
kapsam doğruydu (bütün dosyalar taranıyordu), **desen** dardı.

---

## 🧱 Faz 12.20a — madde 81 (fail-closed panel yetkilendirmesi)

| # | Sözleşme | Kilit cinsi | Kilidi taşıyan dosya | Risk |
|---|---|---|---|---|
| 81 | Panelde öznitelik yoksa aksiyon **kapalı** doğar (`FallbackPolicy`); anonim olması gereken üç yer bunu açıkça söyler; muafiyet **aksiyon** granülaritesinde | **framework davranışı + davranış testi** (üç ayak) | `KadirliApp.Web/Program.cs` (kapının kendisi) · `Integration/Panel/PanelAuthenticationTests.cs` (`ThePanel_FailsClosed_…` · `NoAdminPanelController_OptsOutOfAuthorization` · `TheScaffoldingPages_AreGone` + ters yönü) | 🟢 |

🔑 **Neden 🟢 ve neden 🟢🟢 değil:** kapının kendisi bir *tarama* değil **framework
davranışı** — yani `[Authorize]` yazmayı unutan biri korumasız kalmaz. Ama derleyici
güvencesi değil: `[AllowAnonymous]` yazan biri kapıyı tek satırda delebilir, onu tutan şey
yine bir test (`NoAdminPanelController_OptsOutOfAuthorization`). O test artık **aksiyon**
granülaritesinde ve bozma turunda kırmızıya döndü.

🔬 **Kilidin hangi ayağının tuttuğu ÖLÇÜLDÜ** (§7 madde 70'in dersi: *"iki bağımsız sebep
koruyorsa testin hangisini tuttuğunu ölç"*). İlk yazımda öznitelik**siz** bir aksiyon
`HomeController`'a eklenip "kapalı mı?" diye ölçüldü — kapalıydı, ama **sınıftaki
`[Authorize]` yüzünden**, fallback yüzünden değil. Ölçüm yeniden kuruldu: hiçbir öznitelik
taşımayan **yeni bir controller** açıldı. Fallback açıkken **302**, fallback kapatıldığında
**200** — yani koruyan gerçekten o.

---

## 🧱 Faz 12.21b — madde 82 (açılış göçünün advisory kilidi)

| # | Sözleşme | Kilit cinsi | Kilidi taşıyan dosya | Risk |
|---|---|---|---|---|
| 82 | Açılıştaki göç + seed bir Postgres **advisory kilidinin** arkasında koşar; anahtar bütün host'larda **aynı**, kilit **kendi bağlantısında**, kapsam **seed dahil** | **DB kısıtı + davranış** (üç ayaklı, gerçek Postgres) | `Infrastructure/Persistence/SchemaMigrationLock.cs` (kapının kendisi) · `Integration/Panel/SchemaMigrationLockTests.cs` (kesişmeme · **bırakma** · **düşen işte de bırakma**) | 🟢🟢 |

🔑 **Neden 🟢🟢:** kilidin kendisi bir uygulama kodu `if`'i değil, **veritabanının bir
özelliği** — yani "kodu atlayarak" delinemez. Ayrıca kilidin **kurtarması yapısı gereği
var**: advisory kilit oturuma bağlı olduğu için süreç ölünce Postgres onu kendiliğinden
bırakır (12.13'ün `ReapStuckRuns` borcu burada doğmuyor).

⚠️ **Kilidin yakalayamadığı tek şey ANAHTARIN AYRIŞMASI**: iki host farklı `AdvisoryKey`
yazarsa kilit fiilen yoktur ve testler yine yeşil kalır (her ikisi de kendi anahtarında
doğru davranır). Bugün anahtar tek bir `const`'ta ve iki host da aynı sınıfı çağırıyor;
ayrı bir sabit yazılması **derleyiciyle** engellenmiyor. Bu sınır **bilinçli olarak
yazılıyor** (madde 80'in dürüstlük deseni).

## Faz 12.22 — performans ölçümü (19 Ağustos 2026)

| # | Sözleşme | Kilit cinsi | Kilidi taşıyan dosya(lar) | Risk |
|---|---|---|---|---|
| 83 | İstek ölçümü **`CachingBehavior`'ı sarar** (ortam kapısından hemen sonra); gecikme **sabit kovalı histogram**'da tutulur, yüzdelik gerçeğin **üstünü** söyler ve **gerçek tepeyle tavanlanır** | **Saf + davranış + boru hattı yansıması** | `Unit/Application/Performance/RequestHistogramTests.cs` (yaklaşıklığın **yönü** · birleştirme = toplama · serileştirme **iki yönlü**) · `Unit/Application/Performance/PerformanceBehaviorTests.cs` (`Measurement_WrapsTheCache_NotTheOtherWayAround` · `Measurement_RunsAfterTheEnvironmentGuard`) · `Integration/Panel/PanelPerformanceTests.cs` (ekran **kendi ölçümünü** gösteriyor) | 🟢 |
| 84 | Her `gin_trgm_ops` indeksi **`lower(...)` ifadesi** üzerinde olmak zorunda | **Davranış — kapsam VERİTABANINDAN türer** (`pg_indexes`) | `Integration/Architecture/TrigramIndexTests.cs` (**üç ayaklı**: ölü indeks yok · hiç indeks yoksa kırmızı · **premis** hâlâ geçerli mi) · `Migrations/20260819081443_FixDeadTrigramIndexes.cs` | 🟢 |

🔑 **83'ün kilidi neden üç ayaklı:** çekirdek **saf** (kova matematiği), yeri **yansımayla**
(boru hattı sırası), sonucu **davranışla** (ekran gerçekten dolu mu) tutuluyor. Yalnız saf
test yazılsaydı halka boru hattından düşünce hiçbir şey kırılmazdı; yalnız sıra testi
yazılsaydı yanlış bir yüzdelik sessizce doğru sırada hesaplanırdı.

🔑 **84'ün asıl değeri kapsamın nereden geldiğinde:** kilit `pg_indexes`'e sorar,
migration'ları **taramaz**. Elle SQL'le, ikinci bir migration'la ya da bir seed betiğiyle
eklenen ölü bir indeks de yakalanır — Faz A'nın *"kapsam dizinden mi, tipten mi, elden mi?"*
sorusunun bu fazdaki cevabı: **veritabanından.**

⚠️ **84 bilinçli olarak DAR ve bunu kendisi yazıyor:** *var olan ama ölü* indeksi yakalar,
**eksik indeksi yakalamaz** (14 arama sorgusunda hâlâ trigram indeksi yok). Ölü indeks bir
**hatadır** (bedeli ödeniyor, karşılığı alınmıyor); eksik indeks bir **karardır** ve karar
ölçümle verilir (`Memory_Bank/Performance_Baseline.md`).

---

---

📌 **Risk dağılımı bugün: 🟢🟢 7 · 🟢 76 · 🟠 1 · 🔴 0** (84 madde).
⚠️ Tek 🟠 (madde 80) bir eksiklik değil, **bilinçli ve belgelenmiş bir sınır** (yukarıda).
