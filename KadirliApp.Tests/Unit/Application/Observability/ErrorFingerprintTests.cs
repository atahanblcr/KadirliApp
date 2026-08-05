using FluentAssertions;
using KadirliApp.Application.Common.Observability;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.Observability;

/// <summary>
/// Faz 12.1 — hata tekilleştirmesinin çekirdeği.
///
/// 🔑 Buradaki iddiaların hepsi tek bir soruya bakıyor: **"aynı hata, aynı satıra düşüyor mu?"**
/// Düşmezse tekilleştirme *çalışıyormuş gibi görünür* ama hiç çalışmaz — ve bunun tek
/// belirtisi tablonun sessizce şişmesidir. Kod okunarak fark edilmeyen bir bozulma sınıfı.
/// </summary>
public class ErrorFingerprintTests
{
    [Fact]
    public void SameError_WithDifferentGuid_ProducesSameFingerprint()
    {
        // Gerçek senaryo: "Ad {id} bulunamadı" — her istekte farklı GUID.
        var a = ErrorFingerprint.Compute("api", "NOT_FOUND",
            "Ad 3f2a9c14-8b1e-4a55-9f0d-2b7c6e1d4a88 bulunamadı", null);
        var b = ErrorFingerprint.Compute("api", "NOT_FOUND",
            "Ad 91bc0e77-1122-4d33-8e44-55aa66bb77cc bulunamadı", null);

        a.Should().Be(b,
            "GUID normalize edilmezse her istek ayrı parmak izi üretir ve tekilleştirme HİÇ çalışmaz");
    }

    [Fact]
    public void SameError_WithDifferentNumbers_ProducesSameFingerprint()
    {
        var a = ErrorFingerprint.Compute("api", "INTERNAL_ERROR", "Sayfa 12 için 250 kayıt çekilemedi", null);
        var b = ErrorFingerprint.Compute("api", "INTERNAL_ERROR", "Sayfa 7 için 40 kayıt çekilemedi", null);

        a.Should().Be(b);
    }

    [Fact]
    public void SameError_WithDifferentTimestamps_ProducesSameFingerprint()
    {
        var a = ErrorFingerprint.Compute("api", "CONFLICT", "2026-08-05T14:30:00Z anında çakışma", null);
        var b = ErrorFingerprint.Compute("api", "CONFLICT", "2026-01-02T09:05:11Z anında çakışma", null);

        a.Should().Be(b);
    }

    [Fact]
    public void DifferentErrors_ProduceDifferentFingerprints()
    {
        var notFound = ErrorFingerprint.Compute("api", "NOT_FOUND", "Ad bulunamadı", null);
        var conflict = ErrorFingerprint.Compute("api", "CONFLICT", "Ad bulunamadı", null);
        var otherText = ErrorFingerprint.Compute("api", "NOT_FOUND", "Duyuru bulunamadı", null);

        notFound.Should().NotBe(conflict, "kod parmak izinin parçasıdır");
        notFound.Should().NotBe(otherText, "mesajın şekli parmak izinin parçasıdır");
    }

    [Fact]
    public void DifferentSources_ProduceDifferentFingerprints()
    {
        // Mobilde ve sunucuda aynı metinli hata aynı satıra düşmemeli: biri istemcinin,
        // diğeri sunucunun sorunu — panelde ayrı ayrı görünmeliler.
        var api = ErrorFingerprint.Compute("api", "TimeoutException", "Zaman aşımı", null);
        var mobile = ErrorFingerprint.Compute("mobile", "TimeoutException", "Zaman aşımı", null);

        api.Should().NotBe(mobile);
    }

    [Fact]
    public void StackTrace_LineNumbers_AreStripped()
    {
        // ⚠️ Satır numarası atılmazsa aynı hata, kod bir satır kaydığı için YENİ bir
        // derlemede yepyeni bir kayıt gibi görünür ve "bu hata ne zamandır var?" sorusu
        // her yayında sıfırlanır.
        const string stack1 = "   at KadirliApp.Api.Foo.Bar() in /src/Foo.cs:line 42\n   at Baz()";
        const string stack2 = "   at KadirliApp.Api.Foo.Bar() in /src/Foo.cs:line 87\n   at Baz()";

        ErrorFingerprint.Compute("api", "INTERNAL_ERROR", "patladı", stack1)
            .Should().Be(ErrorFingerprint.Compute("api", "INTERNAL_ERROR", "patladı", stack2));
    }

    [Fact]
    public void DifferentStackOrigin_ProducesDifferentFingerprint()
    {
        var a = ErrorFingerprint.Compute("api", "INTERNAL_ERROR", "patladı",
            "   at KadirliApp.Api.Ads.Create()");
        var b = ErrorFingerprint.Compute("api", "INTERNAL_ERROR", "patladı",
            "   at KadirliApp.Api.Events.Create()");

        a.Should().NotBe(b, "aynı mesaj farklı yerden doğuyorsa ayrı sorundur");
    }

    [Fact]
    public void Fingerprint_IsAlways32HexChars()
    {
        // Kolon varchar(32) — daha uzun üretilirse yazma anında patlar.
        var fingerprint = ErrorFingerprint.Compute("mobile", "StateError", new string('x', 5000), null);

        fingerprint.Should().HaveLength(32);
        fingerprint.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public void NullMessageAndStack_DoNotThrow()
    {
        var act = () => ErrorFingerprint.Compute("api", "INTERNAL_ERROR", null, null);
        act.Should().NotThrow("hata kaydı yolu HİÇBİR koşulda fırlatmamalı");
    }

    [Fact]
    public void Normalize_IsCaseInsensitive()
    {
        ErrorFingerprint.Normalize("Kayıt Bulunamadı")
            .Should().Be(ErrorFingerprint.Normalize("kayıt bulunamadı"));
    }
}
