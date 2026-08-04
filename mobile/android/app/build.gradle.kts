import java.util.Properties

plugins {
    id("com.android.application")
    // Faz 11.13 — FCM (google-services.json'u okur, Firebase kimliklerini gömer).
    id("com.google.gms.google-services")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

// ── Yayın imzalama (Faz 11.16) ───────────────────────────────────────────────
// Anahtar deposu bilgileri `android/key.properties` dosyasından okunur; o dosya
// da .jks anahtarı da **git'e girmez** (bkz. .gitignore + secrets/README.md).
//
// 🔑 Anahtar YOKSA derleme kırılmaz, debug imzasına düşer. Sebep: CI ve yeni
// katılan geliştirici anahtara sahip değil; `flutter build apk` onlarda da
// çalışmalı. Ama debug imzalı bir yapı **mağazaya yüklenemez** — bu yüzden
// aşağıda ayrıca uyarı basılıyor, sessizce debug'a düşmek en tehlikeli hâl olurdu.
val keystoreProperties = Properties().apply {
    val file = rootProject.file("key.properties")
    if (file.exists()) file.inputStream().use { load(it) }
}
val hasReleaseKeystore = keystoreProperties.getProperty("storeFile") != null

// 🔑 Mağazaya YALNIZ app bundle (.aab) yüklenir. Bu yüzden kapıyı tam oraya
// koyuyoruz: anahtarsız `bundleRelease` **derlenmez**.
//
// ⚠️ Neden `assembleRelease` (APK) serbest bırakılıyor: geliştirici yayın
// modunu yerelde denemek isteyebilir (küçültme doğru mu, performans nasıl) ve
// CI de `flutter build apk --debug` koşuyor — ikisini de kırmanın faydası yok.
// Yüklenemeyecek olan APK zaten zarar veremez; tehlikeli olan **AAB**'dir.
//
// 🐛 Bu kapı neden gerekli: önce yalnız `logger.warn` yazılmıştı, ama
// `flutter build` Gradle'ın uyarılarını yutuyor — uyarı hiç görünmedi.
// Yani "sessizce debug anahtarıyla imzalanmış yayın yapısı" riski duruyordu.
val isBundlingRelease = gradle.startParameter.taskNames.any {
    it.contains("bundleRelease", ignoreCase = true)
}
if (isBundlingRelease && !hasReleaseKeystore) {
    throw GradleException(
        "\n\n❌ android/key.properties yok — yayın imzası olmadan app bundle üretilemez.\n" +
            "   Debug anahtarıyla imzalanmış bir .aab Play Store'a yüklenemez;\n" +
            "   sessizce üretilmesindense derleme burada durur.\n" +
            "   Yapılacak: android/key.properties.example dosyasını key.properties\n" +
            "   olarak kopyalayıp doldurun (adımlar: secrets/README.md).\n"
    )
}

android {
    namespace = "app.kadirli"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    defaultConfig {
        // Mağaza kimliği — YAYINDAN SONRA DEĞİŞTİRİLEMEZ (Faz 11.15).
        applicationId = "app.kadirli"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        if (hasReleaseKeystore) {
            create("release") {
                storeFile = rootProject.file(keystoreProperties.getProperty("storeFile"))
                storePassword = keystoreProperties.getProperty("storePassword")
                keyAlias = keystoreProperties.getProperty("keyAlias")
                keyPassword = keystoreProperties.getProperty("keyPassword")
            }
        }
    }

    buildTypes {
        release {
            signingConfig = if (hasReleaseKeystore) {
                signingConfigs.getByName("release")
            } else {
                // Yalnız APK yolu buraya düşebilir (AAB yukarıda durduruldu).
                println(
                    "⚠️  android/key.properties yok → release APK'sı DEBUG anahtarıyla " +
                        "imzalanıyor. Yerel deneme için uygundur, mağazaya YÜKLENEMEZ."
                )
                signingConfigs.getByName("debug")
            }
            // Yayın yapısında kod küçültme + kaynak temizliği. Flutter'ın kendi
            // ProGuard kuralları eklenti sınıflarını zaten koruyor.
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}
