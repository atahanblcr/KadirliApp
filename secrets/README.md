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
