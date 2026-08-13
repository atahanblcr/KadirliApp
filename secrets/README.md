# secrets/ — gizli anahtarlar

Bu klasördeki **dosyalar git'e girmez** (`.gitignore`: `secrets/*`, yalnız bu
README hariç tutulur). Yeni bir makinede projeyi kuran kişi, aşağıdaki
dosyaları kendisi indirip buraya koyar.

---

## `firebase-service-account.json` — FCM push gönderimi (backend)

**Ne işe yarar:** Backend'in Firebase Cloud Messaging üzerinden **push
bildirimi göndermesini** sağlar (`SendPushNotificationsJob` → `FcmPushService`).
Bu dosya bir **özel anahtar** taşır; sızarsa üçüncü kişiler uygulamanın adına
bildirim gönderebilir.

**Nasıl edinilir:**
1. [Firebase Console](https://console.firebase.google.com/) → **kadirliapp** projesi
2. ⚙️ Proje ayarları → **Hizmet hesapları** sekmesi
3. **"Yeni özel anahtar oluştur"** → inen JSON'u bu klasöre
   **`firebase-service-account.json`** adıyla koy.

**Bağlanışı:** `KadirliApp.Api/appsettings.Development.json`
```jsonc
"Fcm": {
  "Provider": "Firebase",
  "ServiceAccountKeyPath": "../secrets/firebase-service-account.json"
}
```
Yol **`KadirliApp.Api/` dizinine göre** çözülür (`FileStorage:UploadDirectory`
ile aynı kural). Prod'da mutlak yol ya da ortam değişkeni tercih edilir.

**Yoksa ne olur:** Uygulama **çökmez.** `FcmPushService` başlangıçta uyarı
loglar ve **no-op**'a düşer — push gönderilmez, geri kalan her şey çalışır.
Kontrol: API açılış logunda
`FCM push sağlayıcısı hazır (service-account: …)` satırını ara.

---

## Android yayın imzalama anahtarı (Faz 11.16)

**Ne işe yarar:** Play Store'a yüklenen `.aab` dosyasını imzalar. Google Play,
bir uygulamanın **her güncellemesinin aynı anahtarla** imzalanmasını şart koşar.

> 🔴 **Bu anahtar kaybolursa uygulama bir daha güncellenemez.** Kullanıcılar
> mağazadaki sürümde çakılı kalır; yeni sürüm ancak **yeni bir uygulama kimliğiyle**
> (yani sıfırdan, mevcut kullanıcı kitlesi olmadan) yayınlanabilir. Anahtarı
> parola yöneticisinde ya da şifreli bir yedekte **iki ayrı yerde** saklayın.
> Sızarsa: başkası sizin uygulamanız adına güncelleme yayınlayabilir.

**Nasıl üretilir** (bir kez, sonra asla değişmez):
```bash
keytool -genkey -v -keystore ~/kadirli-release.jks \
  -keyalg RSA -keysize 2048 -validity 10000 -alias kadirli
```
`-validity 10000` (≈27 yıl) bilinçli: Play, anahtarın 2033'ten sonra da geçerli
olmasını istiyor.

**Bağlanışı:** `mobile/android/key.properties` (örnek dosya:
`mobile/android/key.properties.example`). Hem `key.properties` hem `*.jks`
`.gitignore`'da.

**Yoksa ne olur:**
| Komut | Davranış |
|---|---|
| `flutter build apk --release` | Çalışır, **debug anahtarıyla** imzalar + uyarı basar. Yerel deneme için uygundur. |
| `flutter build appbundle --release` | **Derleme durur** ve ne yapılacağını yazar. Mağazaya yalnız `.aab` yüklendiği için kapı tam oraya konuldu — sessizce imzasız bir yayın paketi üretilmesindense derlemenin durması tercih edildi. |

---

## `panel-admin.json` — panel süper admin parolası (Faz 12.2)

**Ne işe yarar:** Panelin `admin` (super_admin) kullanıcısının parolasının **tek doğruluk
kaynağı**. Dosya varsa `DbSeeder` parolayı buradaki değere **hizalar**; yoksa hiçbir şey
değişmez.

> 🐛 **Neden var:** panel parolası 11.18'de değiştirildi ve o günden sonra her oturumda
> "parola neydi?" sorusu yeniden doğdu — kaynaktaki sabit (`DbSeeder.AdminPassword`) artık
> yalan söylüyordu, doğrusu ise **hiçbir yere yazılamıyordu**: depo herkese açık ve
> `Memory_Bank/*.md` her oturumda push ediliyor. 11.18'de bu yüzden gerçek bir sızıntı
> yaşandı. Bu dosya `secrets/*` altında olduğu için **commit edilmesi imkânsız**.

**Dosya biçimi** — `secrets/panel-admin.json`:
```json
{
  "Panel": {
    "SuperAdmin": {
      "Password": "buraya-kendi-parolanız"
    }
  }
}
```

**Nasıl çalışır:**
- API ve panel açılışta bu dosyayı okur (`optional: true` — yoksa sorun değil).
- Parola veritabanındakinden farklıysa **hizalanır**, aynıysa hiçbir yazma yapılmaz
  (her açılışta yazsaydı `PasswordChangedAt` tazelenir ve `OnValidatePrincipal` yöneticiyi
  kendi oturumundan atardı — 11.18 dersi).
- Hizalama sırasında **hesap kilidi de temizlenir**: parolayı unutup kilitlenen kişi
  dosyayı düzeltip yeniden başlatır, 15 dakika beklemez.
- `MustChangePassword` **işaretlenmez**: 11.18'in kuralı "parolayı sahibi değil *başkası*
  belirlediyse değiştirmeye zorla"dır; burada belirleyen sahibin kendisidir.

> 🔴 **Yalnız Development'ta uygulanır.** Production'da bir dosyanın canlı yönetici
> parolasını sessizce ezmesi, eski/kopyalanmış bir dosyanın parolayı geri alması demektir.
> Yayında parola panelden değiştirilir.

**Parolayı unuttuysanız:** dosyaya yeni bir değer yazın ve API'yi (ya da paneli) yeniden
başlatın — parola o değere döner.

---

## SMTP kimlik bilgileri — güvenlik uyarısı e-postası (Faz 12.2)

**Ne işe yarar:** `SecurityAlertJob` şüpheli giriş denemelerini 5 dakikada bir toplayıp
`super_admin` rolündeki yöneticilere e-posta atar. Gerçek gönderim `SmtpEmailService`
üzerinden yapılır (`Email:Provider = "Smtp"`).

> 🔴 **Kimlik bilgileri `appsettings.json`'a YAZILMAZ.** Depo herkese açık; oraya yazılan
> bir parola commit edildiği anda **yanmış** sayılır (11.18'de gerçek bir sızıntı yaşandı).
> Değerler ortam değişkeninden ya da `appsettings.Development.json`'dan (`.gitignore`'da)
> gelir.

**Bağlanışı** — ortam değişkeniyle (çift alt çizgi = iç içe anahtar):
```bash
export Email__Provider=Smtp
export Email__Smtp__Host=smtp.example.com
export Email__Smtp__Port=587
export Email__Smtp__Username=uyari@kadirli.app
export Email__Smtp__Password='<parola>'
export Email__Smtp__FromAddress=uyari@kadirli.app
export Security__PanelBaseUrl=https://panel.kadirli.app
```

**Yerel deneme:** gerçek bir sağlayıcıya gerek yok — bir SMTP yakalayıcısı yeter
(`docker run -p 1025:1025 -p 8025:8025 mailhog/mailhog`), sonra
`Email__Smtp__Host=localhost`, `Email__Smtp__Port=1025`, `Email__Smtp__EnableSsl=false`.
Panelde **Giriş Denemeleri → "Uyarı kanalını dene"** butonu kanalı anında sınar.

**Yoksa ne olur:** `Email:Provider=Dev` iken uygulama çalışır ve e-posta **yalnız log'a**
yazılır. ⚠️ Production'da bu kombinasyon (uyarı açık + sağlayıcı Dev) `ProductionReadinessGuard`
tarafından **engellenir**: uyarılar üretilip kimseye gitmesindense uygulama hiç açılmasın.

---

## Sosyal giriş — OAuth client id'leri (Faz 12.7)

Sosyal giriş **yapılandırma gelene kadar KAPALIDIR** ve bu bilinçli: client id listesi boşsa
sağlayıcı hiç kaydedilmez, uç `SOCIAL_PROVIDER_DISABLED` der. *"Yapılandırma yoksa geçir"*
davranışı, sosyal girişin bir numaralı zafiyetinin (`aud` doğrulanmaması) en geniş hâli olurdu.

| Anahtar | Ne yazılır | Nereden alınır |
|---|---|---|
| `Auth:Social:Google:ClientIds` | **Virgülle ayrılmış** OAuth 2.0 client id listesi | Google Cloud Console → APIs & Services → Credentials → OAuth 2.0 Client IDs |
| `Auth:Social:Apple:ClientIds` | **Bundle id** (ör. `app.kadirli`) — ⚠️ Google'daki gibi bir "client id" değil | Apple Developer → Identifiers → App ID (**Sign in with Apple** capability'si açık olmalı) |
| `Auth:Social:Enabled` | `true` — yalnız **açık niyet bayrağı** | Bayrak açık ama client id boşsa `ProductionReadinessGuard` **uygulamayı açmaz** |

🔴 **Google'da client id SAYICA ÇOKTUR ve hepsi yazılmalı:** Android · iOS · (varsa) Web
ayrı client id alır. Yalnız biri yazılırsa diğer platformun kullanıcıları jeton gönderir,
`aud` tutmaz ve **"sosyal giriş doğrulanamadı"** hatası alırlar — hata mesajı sorunun
*yapılandırma* olduğunu söylemez. Panelde **Giriş Denemeleri → "Geçersiz sosyal jeton"**
satırlarının birikmesi bu durumun tek görünür işaretidir.

⚠️ **Bunlar sır DEĞİL** (client id'ler istemcide zaten görünür) ama yine de `appsettings.json`'a
yazılmaz: depo herkese açık ve buraya yazılan her satır, yarın *"bunun yanına secret'ı da
koyayım"* refleksinin davetiyesidir. Ortam değişkeniyle verin:
`Auth__Social__Google__ClientIds=111-....apps.googleusercontent.com,222-....apps.googleusercontent.com`

📌 **Apple ayağı Apple Developer aboneliğine bağlıdır** (13 Ağu 2026 itibarıyla onaylanmadı).
Backend kodu **yazılı ve testli**, sağlayıcı yalnız yapılandırmayla kapalı duruyor.

---

## Bu klasörde OLMAYAN ama gereken diğer gizli dosyalar

| Dosya | Yeri | Ne için | Nasıl |
|---|---|---|---|
| `google-services.json` | `mobile/android/app/` | Flutter'ın FCM **token alması** (Android) | Firebase Console → Android uygulaması (`app.kadirli`) → yapılandırmayı indir |
| `GoogleService-Info.plist` | `mobile/ios/Runner/` | Aynısı (iOS) | Firebase Console → iOS uygulaması (`app.kadirli`) → yapılandırmayı indir |
| **APNs Auth Key (`.p8`)** | Firebase Console'a **yüklenir**, repoda durmaz | iOS cihazlara push'un **fiilen düşmesi** | Apple Developer → Certificates, Identifiers & Profiles → Keys → APNs anahtarı oluştur → Firebase Console → Cloud Messaging → Apple app configuration'a yükle. ⚠️ **Apple Developer Program üyeliği (ücretli) gerekir** ve push **gerçek cihaz** ister; simülatörde güvenilir test edilemez. |

İlk ikisi de `.gitignore`'da; ikisi de repoda **yok**, kurulumu yapan kişi indirir.

---

## Sızarsa ne yapılır?

Service-account anahtarı yanlışlıkla paylaşılırsa: Firebase Console → Proje
ayarları → Hizmet hesapları → Google Cloud'da yönet → ilgili anahtarı **iptal
et (revoke)** ve yenisini oluştur. Anahtarı iptal etmek uygulamayı bozmaz,
yalnız yeni dosyayı buraya koymak gerekir.
