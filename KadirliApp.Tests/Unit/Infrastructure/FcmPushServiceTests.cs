using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using FirebaseAdmin;
using FluentAssertions;
using KadirliApp.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KadirliApp.Tests.Unit.Infrastructure;

/// <summary>
/// Faz 11.13 hazırlığı — <see cref="FcmPushService"/> kurulumu.
///
/// 🐛 Bu testler, gerçek bir service-account ilk kez bağlandığında ortaya çıkan
/// hatanın regresyonudur: FirebaseAdmin **.NET** SDK'sında
/// <c>FirebaseApp.GetInstance(name)</c> uygulama yoksa <c>ArgumentException</c>
/// FIRLATMAZ, <c>null</c> DÖNDÜRÜR (Java SDK'sı fırlatır ve kod ona göre
/// yazılmıştı). Sonuç: <c>catch</c> hiç çalışmıyor, <c>Create</c> hiç
/// çağrılmıyor, <c>GetMessaging(null)</c> *"App argument must not be null"* ile
/// patlıyordu. Hata **10.11'den beri koddaydı ama hiç çalıştırılmamıştı**,
/// çünkü <c>Fcm:Provider</c> varsayılanı <c>"None"</c> olduğu için bu sınıf hiç
/// kurulmuyordu. Anahtar bağlanır bağlanmaz Hangfire her dakika hata verdi.
///
/// ⚠️ Testler ağa çıkmaz: FirebaseAdmin kimlik doğrulamayı ilk gönderimde
/// yapar, kurulum sırasında değil. Bu yüzden **sahte ama biçimsel olarak
/// geçerli** bir service-account JSON'u yeterlidir.
/// </summary>
public class FcmPushServiceTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    private static IConfiguration Config(string? provider, string? keyPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fcm:Provider"] = provider,
                ["Fcm:ServiceAccountKeyPath"] = keyPath,
            })
            .Build();

    private static FcmPushService Create(IConfiguration cfg) =>
        new(cfg, NullLogger<FcmPushService>.Instance);

    /// <summary>Gerçek anahtar commit edilemeyeceği için biçimsel olarak geçerli sahte bir tane üretilir.</summary>
    private string WriteFakeServiceAccount()
    {
        using var rsa = RSA.Create(2048);
        var pem = new string(PemEncoding.Write("PRIVATE KEY", rsa.ExportPkcs8PrivateKey()));

        var json = JsonSerializer.Serialize(new
        {
            type = "service_account",
            project_id = "kadirliapp-test",
            private_key_id = "test-key-id",
            private_key = pem + "\n",
            client_email = "test@kadirliapp-test.iam.gserviceaccount.com",
            client_id = "1234567890",
            auth_uri = "https://accounts.google.com/o/oauth2/auth",
            token_uri = "https://oauth2.googleapis.com/token",
        });

        var path = Path.Combine(Path.GetTempPath(), $"fcm-sa-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        _tempFiles.Add(path);
        return path;
    }

    [Fact(DisplayName = "Geçerli service-account ile kurulur (GetInstance null döndürse de Create çağrılır)")]
    public void ValidServiceAccount_Configures()
    {
        var service = Create(Config("Firebase", WriteFakeServiceAccount()));

        // Düzeltmeden önce burada ArgumentNullException ("App argument must not
        // be null") fırlıyordu — testin asıl yakaladığı şey bu.
        service.IsConfigured.Should().BeTrue(
            "geçerli bir service-account verildiğinde push sağlayıcısı hazır olmalı");
    }

    [Fact(DisplayName = "İkinci kurulum mevcut FirebaseApp'i yeniden kullanır, çakışma yaratmaz")]
    public void SecondConstruction_ReusesExistingApp()
    {
        var path = WriteFakeServiceAccount();
        Create(Config("Firebase", path)).IsConfigured.Should().BeTrue();

        // Aynı ada sahip FirebaseApp zaten varken Create çağrılırsa SDK
        // ArgumentException fırlatır → ikinci kurulum GetInstance yolundan
        // dönmeli (singleton olsa da Hangfire yeniden denemeleri araya girebilir).
        Create(Config("Firebase", path)).IsConfigured.Should().BeTrue();
    }

    [Fact(DisplayName = "Yol boş/dosya yoksa ÇÖKMEZ, no-op'a düşer")]
    public void MissingFile_FallsBackToNoOp()
    {
        Create(Config("Firebase", null)).IsConfigured.Should().BeFalse();
        Create(Config("Firebase", "   ")).IsConfigured.Should().BeFalse();
        Create(Config("Firebase", "/olmayan/dizin/yok.json")).IsConfigured.Should().BeFalse();
    }

    [Fact(DisplayName = "Bozuk anahtar dosyası ÇÖKMEZ, no-op'a düşer")]
    public void CorruptFile_FallsBackToNoOp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fcm-bad-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ bu geçerli bir service-account değil }");
        _tempFiles.Add(path);

        // Sınıfın sözleşmesi: "Firebase'siz/bozuk ortam çökmez, no-op'a düşer."
        var act = () => Create(Config("Firebase", path));
        act.Should().NotThrow();
        act().IsConfigured.Should().BeFalse();
    }

    public void Dispose()
    {
        // Test süreci boyunca ayakta kalan FirebaseApp diğer testleri etkilemesin.
        FirebaseApp.GetInstance("kadirliapp-fcm")?.Delete();

        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch (IOException) { /* geçici dosya */ }
        }
        GC.SuppressFinalize(this);
    }
}
