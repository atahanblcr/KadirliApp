# KadirliApp — 30 saniyelik giriş

**Ne bu proje?** Kadirli (Osmaniye) için şehir uygulaması: duyurular, nöbetçi eczane,
ilanlar, vefat, etkinlik, kampanya, taksi, ulaşım, elektrik kesintisi, şehir rehberi,
mekanlar, şikayet/istek. Üç parça: **.NET 8 API** + **Razor admin paneli** + **Flutter mobil**.

**Durum:** **Faz 11 bitti.** Backend + panel + mobil ayakta: 13 mobil modülün tamamı gerçek,
push canlı, golden + erişilebilirlik testleri var; panel gerçek bir yönetim paneli
(denetim izi · çöp kutusu · toplu işlem · sütun sıralaması · CSV dışa aktarma · global arama)
ve güvenlik kapanışı yapılmış (oturum iptali · zorunlu parola değişimi · parola politikası ·
hesap kilidi). Yayın hazırlığının Apple gerektirmeyen kısmı tamam.

**Şimdi Faz 12** — gözlem, alan modeli ve giriş kolaylığı (+ plan dışı **Haberler** bloğu,
12.12–12.15 — **kapandı**); **hepsi additive**
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
**12.6 bitti:** 12.5'in mobil karşılığı — liste **Tümü / Otobüs / Minibüs** olarak (sunucuda)
süzülüyor, kartta kalkış noktası + **"Yol tarifi"**, saatlerin altında **gün rozeti** var ve
"sıradaki sefer" **haftanın gününü** hesaba katıyor ("Bugün sefer yok · Cmt 06:30").
🔴 İstemci de seferleri günlere göre **elemiyor** ve `days` boş/eksikse **"her gün"** sayıyor —
ikisi de "12.5 öncesi kaydı sessizce gizleme" kuralının sonucu. Gün ↔ bit dönüşümünün mobildeki
tek sahibi `features/transport/application/operating_days.dart`.
**12.9 bitti (12.7/12.8'den ÖNCE — bilinçli sıra değişikliği):** panelin dört CDN bağımlılığı
yerelleştirildi ve **nonce'lu CSP** kuruldu. Panel artık **internetsiz çalışıyor**; en işlevsel
kazanç harita seçici — `unpkg` erişilemediğinde **10 formda** yönetici boş bir kutu görüyor ve
**hiçbir hata mesajı çıkmıyordu**. 🔴 Bedeli `script-src`'ta `'unsafe-inline'` **açmamak** oldu:
nonce yalnız `<script>` bloklarını kapsadığı için **47 satır içi `on*=` işleyicisi** delege
dinleyicilere taşındı (`wwwroot/js/panel.js`). Tailwind derleniyor (`npm run build`), çıktı
**commit ediliyor** ve CI sürüklenmeyi denetliyor. ⚠️ `tailwind.config.js`'in `content` listesi
`Views/**` ile sınırlı **değil** — rozet renkleri üç `.cs` dosyasında yaşıyor.
**12.10 bitti:** moderasyon geçişinin **tek sahibi** — bir kaydın durumunu değiştirmenin tek
yolu artık Onayla/Reddet(/Arşivle). Panelin **Düzenle formundaki durum menüsü** ikinci bir
yoldu ve **üç kuralı birden** atlıyordu, üçü de sessizce: iş kuralı (süresi dolmuş ilan
"onaylanıp" mobilde hiç görünmüyordu), **yetki** (`Edit` → `update` iznine düştüğü için yalnız
düzenleme yetkisi olan moderatör moderasyon kararı verebiliyordu) ve **denetim izi** (karar ya
hiç ya da `update` olarak düşüyordu). Kapı `ModerationStatusGuard`'da.
**12.11 bitti:** 12.10'un korumasının **kendisi delik çıktı** — dış analiz (Gemini) "anemik domain
bir tercih değil, zafiyet" dedi; kanıtı bayattı (alıntıladığı hata 12.10'da düzeltilmişti) ama
**iddiası doğruydu** ve denetim somut deliği buldu: `ExtendMyAdCommand` ham `ad.Status = "approved"`
yazıyordu ve 12.10'un yapısal testi onu **hiç taramıyordu** (test modül listesini türetiyor ama
taradığı **dosya adı desenini** elle tutuyordu → 12.9'un dersinin birebir tekrarı). Hasar yoktu,
**koruma tesadüfen çalışıyordu.** Çözüm testi genişletmek değil, korumayı taramanın erişemeyeceği
yere taşımak oldu: moderasyon alanları dört varlıkta **`init`**, geçişler **varlığın metotlarında**
(`Ad.Approve/Reject/Resubmit/Extend` · `Campaign` · `DeathNotice` · `Event`). `entity.Status = …`
artık **CS8852 derleme hatası**. 🔴 Kapsam bilinçli olarak dar: bu **genel bir "zengin domain"
kararı değil** — 50 varlık dokunulmadı, yalnız canlı hasar üretmiş tek değişmez kapatıldı.
🔴 Alan **DTO'dan silinmedi** (§5) ama **sessizce de yutulmuyor**: farklı değer gelirse komut
**reddedip sebebini söylüyor**. ➕ Vefatta durum menüsü aynı zamanda **reddetmenin ve
arşivlemenin tek yoluydu** → iki komut plan dışı olarak **yazıldı** (yoksa hata düzeltilirken
iki işlev silinirdi).
**12.12 bitti (plan dışı blok — kullanıcı isteği): Haberler modülünün alım çekirdeği.**
`silagazetesi.com.tr` (WordPress) artık **bizim veritabanımıza** iniyor; zincir tek yönlü:
`WordPress → (Hangfire, 15 dk) → Postgres → /v1/news → mobil`. Projedeki **ilk dış içerik
entegrasyonu** ve üç yeni hasar sınıfı: kaynak **sessizce susabilir** (→ bayatlık damgası),
kaynak **panelin yaptığını ezebilir** (→ `Source*` / `*Override` ayrımı, ikisi de `init`,
ihlal **CS8852**) ve `modified_after` **silmeyi hiç bildirmez** (→ gecelik `ReconcileNewsJob`;
kayıt silinmez, `gone` olur). 🔴 `modified_after` **site-yerel saatte** (ölçüldü) — imleç UTC
saklanır, sorguya çevrilerek + 30 dk payla gider; ters yön her koşuda **3 saatlik haberi
sessizce atlar**. Tek sahip `WordPressTimeWindow`. ⚠️ Bu modülde **moderasyon YOK** ve
`Approve*` dosya adı **yasak** (`ModerationSingleOwnerTests` modül kümesini o addan türetiyor)
— gizleme geri alınabilir bir **arşivlemedir**. Canlı: 50 haber + 15 kategori, 50/50 görsel
aynalandı, ikinci koşu 0 mükerrer.
**12.13 bitti (plan dışı blok, devam): Haberler paneli + denetimin kalan 8 bulgusu.**
Haberler artık **yönetilebiliyor**: başlık/özet/kapak **override**'ı (senkron ezemez — `init`),
**geri alınabilir gizleme** (silme YOK: kaynak yayındayken silinen kayıt bir sonraki senkronda
geri gelirdi), öne çıkarma, **kategori görünürlüğü** (semantik **dışlama**: dışlanmış tek bir
kategorisi olan haber görünmez — OR semantiğinde "E-Gazete"yi kapatmak *hiçbir işe yaramazdı*)
ve **senkronun sustuğunu gösteren bir yer** (Dashboard + pano, eşikler `NewsSyncHealth`).
🔴 En önemli karar: eşzamanlılık kilidi (**kısmi unique indeks**, Redis değil — o fail-open)
**kurtarmasıyla birlikte** yazıldı; yalnız kilit, süreç öldüğünde **bütün gelecek koşuları**
sessizce engelleyen kalıcı bir kilide dönerdi. Buton koşuyu istek içinde çalıştırmaz, **kuyruğa
atar**. 🔬 Denetimin "arama `strpos` üretiyor" bulgusu **ölçümle çürüdü** (Npgsql `Contains`'i de
`LIKE` yapıyor) ama **sonucu doğruydu**: btree `LIKE '%x%'`'i karşılayamaz → asıl düzeltme
**GIN/trigram** indeksleri. 🐛 Bozma turunda **bir test yeşil kaldı** (ham SQL'e bakıyordu,
bizim sorgumuza değil) → iki ayağa çıkarıldı.
**12.14 bitti (plan dışı blok, devam): Haberler mobil — 13. modül.**
Haberler artık **uygulamada**: ızgarada 13. kart, kategori şeridi (süzme **sunucuda**),
`flutter_html` ile biçimli gövde, "Kaynakta oku", paylaş. 🔴 Gövde istemcide **ikinci kez
temizlenmiyor** — temizliğin tek sahibi sunucu (12.12); ikinci bir beyaz liste, gazetenin
yarın kullanacağı bir etiketi **sessizce yutardı**. Metin arası görseller aynalanmadığı için
(%9'u süreli `fbcdn`) açılmayan görsel **yer tutucu bile göstermeden gizleniyor**.
➕ **Plan dışı üç ek:** **manşet şeridi** (`?featured=true` — panelin "öne çıkar" anahtarının
mobil karşılığı **yoktu**, yani yönetici anahtarı çeviriyor hiçbir şey olmuyordu),
**"Bu kategoriden"** (yeni uç gerektirmedi) ve **çevrimdışı çalışan "Kaydedilenler"**
(kaydın **anlık görüntüsü** saklanır — yalnız `id` saklansaydı kaynakta kalkan haber listede
*"bulunamadı"* satırına dönerdi). 🐛 Bozma turunda **bir taşma testi yeşil kaldı**: kartın asıl
riski taşma değil **sınırsız büyüme**ymiş → `maxLines` doğrudan iddia edildi.
➕ **12.14b (aynı oturum): iki borç kapatıldı.** **Metin arası görseller artık aynalanıyor** —
borcun son kullanma tarihi vardı (%9'u imzalı/süreli `fbcdn` linki → mutlaka 403 olacaklardı ve
istemci onları zarifçe gizlediği için **hiç kimse hata almayacaktı**). 🔴 Sağlama **aynalamadan
ÖNCE** hesaplanır; sonrasıyla hesaplansaydı her koşu haberi "değişmiş" sayıp sonsuza kadar
yeniden yazardı. 🔴 Yeniden deneme **yok** (imzalı adresin hatası kalıcı). 12.14 öncesi kayıtlar
`MirrorNewsBodyImagesJob` ile onarılıyor. Ayrıca **okuma boyutu** ayarı (çarpım **tavanlı**).
**12.15 bitti — Haberler bloğu (12.12–12.15) KAPANDI.** Yönetici bir haberi panelden **tek
tıkla** şehre duyurabiliyor; `relatedType="news"` mobilde `/haberler/:id`'ye gidiyor (eşleme
12.14'te yazıldığı için **mobilde tek satır değişmedi**). 🔴 Gönderim **terminal** ve kural
**üç katmanda**: buton · komut · **kısmi unique indeks** — ilk ikisi yarışı yakalayamaz
(gönderim ile işaretleme aynı `SaveChanges`'te değil, kampanya kimliği sonra doğuyor →
**şehre iki push**). 🔴 **Planın koşulu eksikti:** görünmezliğin üç ekseni var, plan ikisini
sayıyordu — **dışlanmış kategorideki** haber panelde "Yayında" görünür ama uygulamada
**yoktur**; bildirimi gönderilseydi vatandaş boş sayfaya düşerdi. 🔴 12.12'nin
`announcement_id` kolonu **düşürüldü**: reddedilmiş bir tasarımı anlatıyordu ve hiç
yazılmamıştı (56/56 `NULL`, ölçüldü). 🔑 Gövde **kendi kendine yeterli** — eski sürümler
`news` türünü tanımıyor, dokununca hiçbir yere gitmiyor; "Detay için dokunun" diyen bir
bildirim onlara **yalan söylerdi**. ➕ Plan dışı: **gönderim önizlemesi** · **"bildirimi
gönderilmemiş" süzgeci** · **panodan habere geri bağlantı**.
🐛 EF, yeni indeksi eskisinin **üstüne yazdı** (aynı kolon kümesi = aynı indeks) ve üretilen
migration duyuru idempotency indeksini **DROP** ediyordu — yakalayan test değil, **üretilen
SQL'i okuma** kuralıydı.

➕ **12.15b (aynı oturum): 12.15'in bıraktığı tercih deliği.** Dispatcher **her kaynağı**
`Announcements`'a bağlıyordu ve `news` ekseni yoktu → "Duyurular"ı kapatan haberleri de
kaybediyor, haber istemeyenin **tek çıkışı** duyuruyu kapatmak — o da **kesinti bildirimini**
öldürüyordu. Tercih artık **kaynağa göre** (`PushPreferenceTopics`; kesinti bilerek duyuru
ekseninde kaldı). 🔬 **Bir varsayım ölçümle çürüdü:** `= true` yazılmasına rağmen **EF'in JSON
materyalizasyonu varsayılan başlatıcıyı çalıştırmıyor** (anahtarsız JSON `false` okunuyor,
canlıda 13/13) → geri doldurma migration'ı **zorunluydu**; test silinmedi, ölçüm belgeye
çevrildi.

**12.7 bitti — sosyal giriş: backend.** `POST /v1/auth/social` (Google + Apple),
`user_identities` tablosu, bağla/çöz uçları ve panelde "Bağlı hesaplar".
🔑 **Telefon çıpa olarak kaldı:** sosyal kayıt jetonu **telefon taşımaz**, kayıt yine
OTP'den geçer ve `register` **iki jetonu birden** ister — tek jetona indirgenseydi Google
hesabı olan herkes OTP'siz hesap açar ve moderasyonun dayandığı varsayım **sessizce**
çökerdi. 🔴 **Plandan bilinçli sapma:** `GoogleJsonWebSignature.ValidateAsync` **statik ve
gerçek Google anahtarlarına bağlı** olduğu için fazın bir numaralı kuralını (`aud`)
testle kilitleyemezdik → iki sağlayıcı için **tek** `JwksSocialTokenVerifier` yazıldı
(ikisi de OIDC/RS256; fark yalnız `iss`/`aud`/JWKS = **veri**, kod değil). `aud` kilidi
**iki yönlü**: aynı jeton `aud` listesine eklenince **kabul ediliyor** — yoksa "hiçbir
jetonu kabul etme" gerçeklemesi de yeşil kalırdı. 🔴 **E-posta eşleşmesiyle otomatik
bağlama YOK** (`User.Email` panelden elle giriliyor ve doğrulanmıyor). 🔴 Hesap silinince
kimlik satırları **fiziksel** silinir (kişisel veri + benzersizlik: kalsaydı o kişi aynı
Google hesabıyla **bir daha asla** kayıt olamazdı). 🐛 Bulunan gerçek hata: yapılandırma
**DI kaydında** okunuyordu → kod doğruydu ama **kendi testinden erişilemiyordu**.

**✅ 12.16 bitti — KVKK belge yönetimi + rıza kaydı.** Üç tablo (`legal_documents` ·
`legal_document_versions` · `user_consents`), iki **anonim** uç (`/v1/legal/documents`,
`.../{type}`), `register` gövdesinde **additive** `consents`, `GET/POST /v1/users/me/consents`,
panelde **Hukuki Metinler** (matriste) + **Rıza Defteri** (yalnız admin).
🔑 Modelin merkezinde **sürüm** var, "onaylandı" bayrağı değil: yayınlanmış metin
**değiştirilemez** (`init` → **CS8852** + varlığın kapısı + panelde form hiç çizilmez),
değişiklik **yeni sürümdür** ve aynı anda **en fazla bir yayında sürüm** olabilir (kısmi
unique indeks). 🔴 **Metin SEED EDİLMEZ** — yalnız belgenin kabuğu açılır; seed edilmiş bir
"örnek KVKK metni" er ya da geç yayına çıkar. Sonucu: taze kurulumda zorunlu belge yok, yani
kayıt akışı **birebir eskisi gibi**. 🔴 **Yayında sürümü olmayan belge ZORUNLU OLAMAZ**
(planda yoktu): olsaydı uygulama **hiç yeni kullanıcı alamazdı** ve sebep hiçbir ekranda
yazmazdı. 🔴 Hesap silinince rıza **KALIR** (12.7'nin `user_identities` kararının bilinçli
tersi: kimlik *kişisel veri*, rıza *kanıt*). ➕ `IUnitOfWork.ExecuteInTransactionAsync`
eklendi (aşağıdaki hataya bakın).
🐛 **Bozma turu PLANDA OLMAYAN GERÇEK BİR HATA buldu:** "eskiyi yürürlükten kaldır + yeniyi
yayınla" tek `SaveChanges`'teydi ve testler **üç kez yeşil** koştu; ölçüldüğünde **8 koşudan
5'i** `23505` ile düşüyordu. Kısmi unique indeks **deyim başına** denetlenir, EF ise
UPDATE'leri **birincil anahtar sırasına** (yani `gen_random_uuid()` → **rastgele**) gönderir.
🔑 **Ders: rastgeleliğe bağlı bir hata, tek koşuluk bir testle kilitlenemez** (`LegalPublishTests`
10 tur koşar).

**✅ 12.17 bitti — KVKK mobil. KVKK bloğu (12.16–12.17) KAPANDI.** Yeni mobil modül
`features/legal/`: dört ekran (`/yasal` · `/yasal/:type` · `/yasal-surum/:id` ·
`/yasal-onay`), kayıt akışında **ön işaretsiz** rıza adımı, ayarlarda *"Onayınız: v2"* +
isteğe bağlı izni verme/geri alma, sekme kabuğunu saran **yeniden onay kapısı**.
🔴 **Metin gösterilemiyorken kayıt AÇILMAZ** — projedeki *"şüphede kalınca göster"*
kuralının (§5, §7 madde 49) **bilinçli tersi** ve tersliği yazılmak zorundaydı: metni
gösteremiyorken rıza almak **rıza almamaktır**. `AsyncLoading` dalı da kapalı.
🔴 **Kararın tek sahibi `ConsentSelection`** (saf; `initial` **boş** kümeyle başlar). Ön
işaretli kutu KVKK'da rıza sayılmaz ve kural **tek karakterle** bozulabiliyor — bozulduğunda
hiçbir şey hata vermez, kayıt **hızlanır** bile, yalnız toplanan bütün rızalar **geçersiz**
olur. Kilit hem saf hem davranış ayaklı.
🔴 **Hukuki metin ekranları yönlendirme istisnası** (`AppRoutes.isLegalReading`): *"kayıt
yarım kaldıysa tek çıkış kayıt ekranıdır"* kuralı onları da kapatıyordu → "oku" bağlantısı
kullanıcıyı geri fırlatır ve geriye **okumadan onaylamaktan başka seçenek kalmazdı**.
➕ **Plan dışı ek: `GET /v1/legal/versions/{id}`** — 12.16 rızayı sürüme bağladı ve
`consentedVersionId`'yi söylüyordu ama o kimlikten **metne** giden yol **yoktu**: yeni sürüm
yayınlandığı an vatandaş kabul ettiği metni bir daha göremiyordu. Kanıt bizdeydi, **sahibinde**
değildi. Taslak **404**, yürürlükten kalkmış sürüm **döner**, belgenin `IsActive`'ine
**bakılmaz**. ➕ `core/widgets/rich_html_body.dart` (HTML gövde çiziminin ortak çekirdeği;
`NewsBody` sahipliğini korur, çizimi delege eder).
🐛 **CANLI DOĞRULAMA GERÇEK BİR HATA BULDU ve hata 12.16'daydı: panelden yeni sürüm açmak
HİÇ ÇALIŞMIYORDU** (`<input type="date">` → `Kind=Unspecified` → Npgsql `timestamptz`'i
reddediyor → 500). Yani *"metni değiştirmenin tek yolu yeni sürümdür"* kuralının **tek yolu
kapalıydı**; testler görmedi çünkü hepsi `DateTime.UtcNow` veriyordu. Tek sahip
`LegalDates.FromPanel` (saat **kaydırılmaz, etiketlenir**). 🔑 **Ders: bir alanı test ederken,
o alana GERÇEKTE ne geldiğini ölç.** 🐛 Widget testi ilk koşuşunda `ConsentCheckTile`'da
**gerçek bir taşma** buldu (taşma sınıfının 8. tekrarı). Bozma turu: **9 kilit, 9 kırmızı**.

**✅ 12.19 bitti — üçüncü dış analiz denetiminin bulduğu üç delik.** (a) `/Dashboard/Seed`
Production'da **açıktı**, `[HttpGet]` olduğu için `AutoValidateAntiforgeryToken` onu
**kapsamıyordu** ve `AppDbContext`'i doğrudan alarak MediatR'ı atlıyordu (→ **denetim izi hiç
düşmüyordu**). Üçünün bileşimi somut bir zafiyetti: yöneticinin gezdiği kötü niyetli bir
sayfadaki tek bir `<img src="…/Dashboard/Seed">`, **onun oturumuyla** canlıda boş kalan her
tabloya sahte içerik — sahte ilan, uydurma telefon, **sahte vefat ilanı** — yazdırırdı.
🔴 **Kapı bilinçli olarak controller'da DEĞİL**: `IDevelopmentOnlyCommand` + boru hattının
**en başındaki** `DevelopmentOnlyBehavior` (kapsam **tipten türer**, yarınki ikinci bakım
aksiyonu kendiliğinden korunur; sıra da kuralın parçası — `AuditBehavior` izi handler
*döndükten sonra* yazar). 🔴 **Kapının yönü "izin ver"**: `!IsProduction()` yazan bir kapı
`Staging`/`Test` eklendiği gün **sessizce açılır**, `IsDevelopment()` sessizce *kapanır*.
(b) `User.cs`'in yorumu **ölçümün tersini** söylüyordu ve **var olmayan bir teste** atıf
yapıyordu — o ölçüm `BackfillNewsNotificationPreference` migration'ının **bütün varlık
sebebi**. ➕ `CommentReferenceTests` (test adı · `<see cref>` · **dosya yolu**) yazıldı ve
**ilk koşusunda ikinci bir gerçek çürük buldu**. 🔬 *"`<see cref>`'i derleyici denetler"*
varsayımı **ölçümle çürüdü**: XML belge üretimi hiçbir projede açık değil, kırık cref
**uyarı bile üretmiyor**. ⚠️ Bu kilit bilinçli olarak **eksik ve bunu kendisi yazıyor**
(sarkan işaretçiyi yakalar, **yanlış iddiayı yakalayamaz** — denetimdeki tek 🟠).
(c) Dört **ölü** durum enum'u silindi (0 kullanım, ölçüldü), geçişler
`AdStatuses.Approved` gibi **`const string`** sabitlerine bağlandı — **enum değil**: değer
DB'de `varchar` ve DTO'da mobile çıkıyor (§5). Kolon değeri **birebir aynı**, migration
**yok**. ➕ **Plan dışı:** aksiyonun mesajı artık *ne olduğunu* söylüyor (eskiden dolu bir
veritabanında da "başarıyla eklendi" diyordu) · panelde **ortam rozeti** (panel şehir ölçekli
ve geri alınamaz işlerin yeri ama *"burası hangi kurulum?"*un cevabı hiçbir yerde yoktu;
🔴 rozet **canlı olmayanı** işaretler — "CANLI" yazan bir rozet unutulduğunda canlıyı
*güvenli* gösterirdi) · `MockDataSeeder`'a host'tan doğrudan erişimi yasaklayan yapısal test.
🐛 Bozma turu **15 kilit, 15 kırmızı — biri ikinci denemede**: jenerik bir kırık cref
(`Foo{T,U}.OlmayanUye`) yeşil kaldı, çünkü desen `{…}`'ye uymuyor *ve* jenerik `Type.Name`
arite soneki taşıyordu. 🔑 **Ders: kapsam doğru olabilir ama DESEN dar olabilir.**

**✅ 12.20 bitti — iskele kalıntıları + iki kilidin eksik yönü.** `dotnet new mvc`'den kalan
`/Home/Index` ve `/Home/Privacy` silindi (ikisi de **kimliksiz 200** dönüyordu; ikincisi
*tahmin edilebilir bir gizlilik metni adresinde* İngilizce bir yer tutucuydu) ve
`wwwroot/lib/bootstrap` (**7,2 MB · 0 referans**) düştü.
🔴 **Asıl kazanç plan dışı: panel artık FAIL-CLOSED.** Bulgunun kök nedeni o sınıf değil,
`FallbackPolicy`'nin **yokluğuydu** — öznitelik yoksa aksiyon anonim doğuyordu. Kapı
`Program.cs`'e kuruldu (`RequireAuthenticatedUser`), koruma bir **taramadan framework
davranışına** taşındı; yapısal test **ikinci hat** oldu ve muafiyeti **controller değil
AKSİYON** granülaritesinde. Anonim kalması gereken üç yer bunu artık açıkça söylüyor:
giriş akışı · hata sayfaları · **`/health/*` probe'ları** (sonuncusu unutulsaydı orkestratör
302 alır, konteyner "sağlıksız" damgası yer ve **sebep hiçbir logda görünmezdi**).
🔴 Ölçülmüş bedeli kabul edildi: fallback **hiçbir uca eşleşmeyen** isteklere de uygulanıyor →
oturumsuz ziyaretçi olmayan bir adreste markalı 404 yerine **302 → giriş** alıyor (markalı 404
oturumluda korunuyor, canlı doğrulandı). 🐛 `[Authorize]`'a refleksle yazılan **rol listesi**
bir yalandı ve `PanelModeratorPermissionTests` anında yakaladı: rol listesi *"bu bir modül
ekranıdır"* demektir, burası panelin **hata yüzeyi** → rolsüz `[Authorize]`.
➕ **Madde 51'in ikinci yönü yazıldı** (*"diskteki her varlığa başvuran var mı?"*) ve kilit
yazılır yazılmaz **iki kalıntı daha** düştü (`site.js` · `site.css` · `_Layout.cshtml.css`) —
üstelik denetimin *"`site.js` yaşıyor ve kullanılıyor"* hükmü **ölçümle çürüdü**.
🔑 **Ders: tek yönlü kilitler ölü kod biriktirir ve biriktirdiklerini hiçbir zaman söylemez.**
🧪 Bozma turu **4/4 kırmızı — biri ikinci denemede**: öznitelik**siz** aksiyon
`HomeController`'a eklendiğinde kapalı çıkıyordu ama **sınıftaki `[Authorize]` yüzünden**;
ölçüm öznitelik taşımayan **yeni bir controller** ile yeniden kuruldu (fallback açıkken 302,
kapalıyken **200**). 🐛 `CommentReferenceTests` (madde 80) **kendi yazdığım testin yorumundaki**
sarkan dosya yollarını yakaladı — yani madde 80 onu yazan kişiyi bir faz sonra yakaladı.

**1284 backend + 865 mobil test, 81 görünmez sözleşme.**

**⏭️ Sırada:** 🚢 **12.21 yayın hattı** (Apple gerektirmiyor; fail-closed panel + anonim
`/health/*` onun zeminini kurdu) · ⚡ **12.22 performans/ölçek** (*önce ÖLÇ*) · 12.8 sosyal
giriş mobil (🔴 Apple aboneliği bekliyor, **Google ayağı bugün yazılabilir**) · **12.18 adayı**
kategori bazlı bildirim aboneliği (⚠️ ikinci bir dispatcher **yazılmaz**, var olan tek sahip
genişletilir).
🆕 **12.20'nin açtığı tek karar maddesi:** `/Home/Privacy` silindi ve panelde artık **hiç**
gizlilik metni adresi yok — mağazalar yayında **herkese açık bir URL istiyor**, metin bugün
yalnız mobil uygulamanın içinde okunabiliyor. Altyapı hazır (anonim
`GET /v1/legal/documents/{type}`), eksik olan **karar**: panelde anonim bir sayfa açmak
12.20a'nın az önce kapattığı yüzeyi yeniden açmak demek.
📌 **KVKK'da açık kalan tek madde kod işi DEĞİL:** hukuki metinlerin **gerçek içeriği** bir
**insan/hukukçu** tarafından yazılmalı — bugün yayında olanlar test metnidir ve kod metni
**seed etmiyor** (bilinçli).
🐛 **12.7'nin bozma turu koşuldu ve BİR DELİK BULDU:** madde 70'in testi doğru davranışı
ölçüyordu ama **yanlış sebepten** geçiyordu (sosyal jetonun `phone` claim'i zaten yok →
`token_type` kontrolü silinse de `null` dönüyor). İki bağımsız sebep koruyordu ama biri
**tesadüfi**. İddia elle üretilen bir jetonla (sosyal türde **ama telefon taşıyan**) gerçek
değişmeze çevrildi. 🔑 **Ders: iki bağımsız sebep koruyorsa, testin HANGİSİNİ tuttuğunu ölç.**
🐛 **12.16'nın bozma turu koşuldu ve GERÇEK BİR HATA BULDU** (yukarıda). Ayrıca projenin
kendi korumaları iki hatamı yakaladı: `data-confirm` **butona** yazılmıştı (dinleyici
**formun** özniteliğine bakıyor → onay penceresi sessizce hiç açılmazdı) ve bir **Razor
yorumunda** geçen açı parantezli betik etiketi CSP taramasını kırdı.
⚠️ `?featured=false` ve aramanın **en az 2 karakter** kuralı kontrata girdi (`API_CONTRACT.md`).
⚠️ Yeni bir `Un…` aksiyonu yazarsan (ya da `SendNotification` gibi hiçbir önekle eşleşmeyen
bir moderasyon aksiyonu) önekini `PanelPermissionFilter.ActionFor`'a **elle ekle**:
`Archive` öneki `Unarchive`'ı yakalamaz ve aksiyon sessizce `update` iznine düşer
(bu tuzak 7 kez tekrarladı: 11.18 · 12.10 · 12.13 · 12.15 · Faz 0'da iki kez · **12.16 `Publish`**).
⚠️ Aynı kolon kümesine **ikinci bir EF indeksi** eklerken adlı aşırı yükleme + `HasDatabaseName`
kullan; yoksa EF öncekini **sessizce ezer** ve migration onu `DROP` eder (12.15 bulgusu).
⚠️ JSON kolonda (`OwnsOne(...).ToJson()`) saklanan bir nesneye alan eklerken **varsayılan
başlatıcıya güvenme**: EF onu materyalizasyonda çalıştırmıyor, eksik anahtar `false` okunuyor
→ **geri doldurma migration'ı** şart (12.15b bulgusu). Ve `ExecuteSqlRaw` gövdesine JSON
literali yazma — `{` yer tutucu sanılıyor.
⚠️ **Veritabanına toplu yazan ya da yalnız geliştirmeye ait bir aksiyon** yazacaksan ortam
kontrolünü **controller'a yazma**: komut `IDevelopmentOnlyCommand`'i uygulasın, kapıyı
`DevelopmentOnlyBehavior` tutsun (12.19a). Aksiyon ayrıca **`[HttpPost]`** olmak zorunda —
`AutoValidateAntiforgeryToken` global filtresi yalnız POST/PUT/DELETE doğrular, yani bir
`[HttpGet]` aksiyon CSRF korumasının **tamamen dışındadır**.
⚠️ **Panelde yeni bir controller/aksiyon yazarken** artık varsayılan **REDDET**'tir
(`FallbackPolicy`, 12.20a): `[Authorize]` yazmayı unutan aksiyon anonim değil **kapalı** doğar.
Gerçekten anonim olması gerekiyorsa `[AllowAnonymous]` **ve** `PanelAuthenticationTests`'in
`AnonymousActions` listesine **gerekçeli** bir satır şart. ⚠️ `[Authorize]`'a **rol listesi**
yazmak *"bu bir modül ekranıdır"* demektir → `[PanelPermission]` + menü satırı + matris anahtarı
gerekir; modül değilse **rolsüz** `[Authorize]` yaz.
⚠️ **`wwwroot`'a dosya eklerken** en az bir yerden başvurulmalı — `wwwroot/lib` altındaki her
**dizin** ve `wwwroot/{css,js}` altındaki her **dosya** artık iki yönlü kilitli (12.20b).
⚠️ Moderasyon durumu yazarken ham literal (`"approved"`) **kullanma**, `AdStatuses.Approved`
gibi sabitleri kullan (§7 madde 79) — `ModerationSingleOwnerTests` yasak kelime dağarcığını
`*Statuses` sınıflarından **yansımayla** türetiyor.
⚠️ Yorumda bir **test adı**, **`Tip.Üye`** ya da **dosya yolu** anarsan gerçek olmak zorunda
(§7 madde 80) — ⚠️ `<see cref>`'i **derleyici denetlemiyor** (XML belge üretimi kapalı).
⚠️ Moderasyon alanına yazarken `CS8852` alırsan **çözüm alanı `set`'e açmak değil**, geçişi
varlığın bir metoduna taşımaktır (§7 madde 53 — açarsan test kırılır, bilerek). Ayrıca daha önce
kullanılmamış bir Tailwind sınıfı yazdıysan `npm run build` çalıştır — yoksa buton
**beyaz üstüne beyaz** çizilir (12.10 canlı bulgusu).
📋 **Görünmez sözleşme denetiminin Faz 0'ı (tasnif) ve
bulduğu yedi deliğin kapatılması (B1–B7) bitti:** çıktı `Memory_Bank/Contract_Audit.md` —
67 maddenin her birinin **kilit cinsi · risk · kilidi taşıyan dosya**. 67'sinin de testi
vardı; sorun **iddiası zayıf testler** (beş fazda beş kez patladı). Kapatılanlar: 6 · 15 ·
16 · 17 · 19 · 21 · 26; yedisinin de **bozma turu koşuldu**. 🔑 En değerlisi **madde 19**:
adı hiçbir önekle eşleşmeyen bir panel yazma aksiyonu artık sessizce `update` iznine değil
**kırmızıya** düşüyor (kapsam yansımayla türetiliyor) — o test ilk koşusunda **iki gerçek
vaka** buldu. ✅ **T1/T2 ve Faz A da bitti — DENETİM KAPANDI.** Faz A'da **beş delik daha** bulunup
kapatıldı (27 · 30 · 51 · 52 · 61); dördü **kapsam** deliğiydi ve dördü de kapsamı
**türeterek** (dizinden/tipten/yansımayla) kapatıldı. 🔑 En önemlisi **52**: 12.11 korumayı
derleyiciye taşımıştı ama **taramanın kendisi** hâlâ `Update*.cs` deseni tutuyordu — aynı
delik, aynı dosyada. **67 maddenin tamamı bugün 🟢/🟢🟢.**
🧹 Ardından **doküman bakım borçları kapatıldı**: `openapi.json` yenilendi (tek gerçek fark
`news` alanıydı — "üç alt-faz geride" teşhisi **ölçümle çürüdü**), `ARCHITECTURE.md` §4 adım 8
düzeltildi (`permissions`/`role_permissions` **çalışma anında hiç okunmuyor**), Progress.md'nin
**22 bayat kutusu** hizalandı, `Class1.cs` silindi. ➕ `Feature` aksiyonu artık `approve`
iznine tabi (manşet şeridi = §7 madde 19'un 5. tekrarı).
Plan: `Memory_Bank/Progress.md` → "FAZ 12".

> 🔑 **Panel süper admin parolası** `secrets/panel-admin.json`'dadır (git'e girmez; biçim ve
> davranış: `secrets/README.md`). Dosya varsa açılışta parola ona **hizalanır** — "parola neydi?"
> sorusu artık kaynağa değil o dosyaya sorulur.

## Çalıştır

```bash
docker compose up -d                          # Postgres · Redis · Seq
dotnet run --project KadirliApp.Api           # http://localhost:5005  (Swagger: /swagger)
dotnet run --project KadirliApp.Web           # admin paneli
# Panel varlıkları (YALNIZ Tailwind sınıflarını / 3. taraf sürümünü değiştirdiyseniz):
cd KadirliApp.Web && npm install && npm run build   # → wwwroot/css/panel.css + wwwroot/lib/*
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
| **"Ne kaldı, hangi faz açık?"** | **`Memory_Bank/Progress.md` → en üstteki 🚦 AÇIK MADDELER PANOSU** (yalnız açıklar; kapanan satır silinir) |
| **"Bu görünmez sözleşme gerçekten kilitli mi, kilidi nerede?"** | **`Memory_Bank/Contract_Audit.md`** (81 madde × kilit cinsi/risk/dosya) |
| "Bu .NET kalıbı ne demek?" | `DOTNET_MASTERCLASS.md` |
| "Mobil tasarım sistemi / UX kuralları?" | `Memory_Bank/MOBILE_UX_PLAN.md` |
| "Uçların makine-okur şeması?" | `docs/openapi.json` |
| "Mobil kurulum / canlı doğrulama komutları?" | `mobile/README.md` |
| "Kod review istiyorum, nelere dikkat edilmeli?" | `CODE_REVIEW_CHECKLIST.md` |

⚠️ **`ARCHITECTURE.md` §7 "Görünmez sözleşmeler"i okumadan backend'e dokunma.** Orada
listelenen 81 bağımlılık bozulduğunda kimse hata almaz — mobil sadece sessizce yanlış
davranır. Hepsi testle kilitli: 1–22 `InvisibleContractsTests.cs`, 23–26 `PanelBusinessRuleTests.cs`,
27 `PanelPowerOutageFilterTests.cs`, 28 `PanelTrashTests.cs`,
29 `PanelBulkActionTests.cs`, 30 `PanelSortingTests.cs`,
31–33 `PanelErrorLogTests.cs` + `Unit/Application/Observability/`,
34–36 `PanelLoginAttemptTests.cs` + `Unit/Application/Security/`,
37–39 `PanelPushCampaignTests.cs` + `Unit/Application/Notifications/`,
40–42 `PanelPowerOutageNeighborhoodTests.cs` + `Unit/Application/PowerOutages/`,
43–45 `PanelEventDistrictTests.cs` + `Unit/Application/Events/`,
46–48 `PanelTransportFieldModelTests.cs` + `Unit/Application/Transport/`,
**49–50 istemci tarafı** → `mobile/test/features/transport/`
(`operating_days_test.dart` · `departure_times_test.dart` · `transport_screen_test.dart`),
**51** → `Integration/Architecture/PanelExternalOriginTests.cs` (kaynak taraması) +
`Integration/Panel/PanelContentSecurityPolicyTests.cs` (canlı yanıt) +
`Unit/Web/PanelAssetGuardTests.cs` (yayın kapısı),
**52–53** → `Integration/Architecture/ModerationSingleOwnerTests.cs` (yapısal) +
`Integration/Panel/PanelModerationOwnershipTests.cs` (davranış) + `Unit/Application/Moderation/`,
**54–57** → `Unit/Application/News/` + `Integration/News/` +
`Integration/Architecture/NewsSourceOwnershipTests.cs` (yapısal — **kaynak taraması değil
yansıma**), **58–60** → `Integration/Panel/PanelNewsTests.cs` + `Unit/Application/News/`
(`NewsSearchTests` · `NewsStatesTests`), **61–62 istemci tarafı** →
`mobile/test/features/news/` (`news_body_test.dart` · `news_screen_test.dart` ·
`news_detail_screen_test.dart` · `news_card_test.dart`),
**71–74** → `Unit/Application/Legal/` + `Integration/Legal/`
(`LegalConsentTests` · `LegalPublishTests`) + `Integration/Panel/PanelLegalTests.cs` +
`Integration/Architecture/LegalImmutabilityStructureTests.cs` (**yansıma** — kaynak taraması değil),
**75–76 istemci tarafı** → `mobile/test/features/legal/`
(`consent_selection_test.dart` · `register_consent_test.dart` · `reconsent_test.dart` ·
`legal_documents_screen_test.dart`), **77** → `Integration/Legal/LegalVersionEndpointTests.cs`
(**iki yönlü**: taslak 404 *ve* yayınlanmış sürümün döndüğü),
**78–80** → `Unit/Application/Common/DevelopmentOnlyBehaviorTests.cs` (saf, **iki yönlü**) +
`Integration/Architecture/DevelopmentOnlyCommandTests.cs` (**yansıma** — boru hattı kaydı *ve
sırası*) + `Integration/Panel/PanelSeedActionTests.cs` (davranış; ⚠️ **sahte** `IMockDataSeeder`
ile — gerçeği paylaşılan panel veritabanına yazardı) +
`Integration/Dashboard/MockDataSeederTests.cs` (seeder'ın **kendi** veritabanı) +
`ModerationSingleOwnerTests`'in 12.19c ayağı + `Integration/Architecture/CommentReferenceTests.cs`,
**68–70** → `Unit/Infrastructure/SocialTokenVerifierTests.cs` +
`Unit/Infrastructure/JwtProviderSocialTokenTests.cs` +
`Unit/Application/Auth/SocialProvidersTests.cs` + `Integration/Auth/SocialLoginTests.cs`,
**81** → `Integration/Panel/PanelAuthenticationTests.cs` (**üç ayaklı**: `FallbackPolicy`'nin
varlığı **çalışan uygulamanın servislerinden** okunur · muafiyet **aksiyon** granülaritesinde ·
iskele adresleri **oturumlu** istemciyle 404 doğrulanır — anonim yanıt "silindi" ile
"korumalı"yı ayırt edemez, ikisi de 302'dir).

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
