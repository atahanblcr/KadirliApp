using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.16 — <b>görünmez sözleşme 72</b>'nin <i>birinci</i> ayağının bekçisi:
/// yayınlanmış metni koruyan şey bir test değil <b>derleyicidir</b>, ve bu test o
/// derleyici güvencesinin <b>sökülmesini</b> yakalar.
/// </summary>
/// <remarks>
/// <para>
/// 🔑 Ayrım 12.11'in dersidir (§7 madde 53): <c>version.Body = "…"</c> yazan bir satır bugün
/// <b><c>CS8852</c></b> alır. Yarın biri o hatayı "düzeltmek" için alanı <c>set</c>'e açarsa
/// <b>derleme yeşile döner</b>, davranış testleri de yeşil kalır (kapı hâlâ
/// <c>TryRevise</c>'da) — ve koruma <b>sessizce yarıya iner</b>. Bu test tam o anı yakalar.
/// </para>
/// <para>
/// ⚠️ <b>Kapsam TİPTEN türetiliyor</b>, elle tutulan bir alan listesinden değil — Faz A
/// denetiminin beş deliğinden <b>dördü</b> kapsam deliğiydi: kilit doğru şeye bakıyordu ama
/// <i>dar bir kümede</i>. Burada ölçüt <c>LegalDocumentVersion</c>'ın <b>tüm</b> yazılabilir
/// özellikleridir; yarın eklenen bir <c>Body2</c> kolonu kendiliğinden kapsama girer.
/// </para>
/// <para>
/// 📌 Container gerektirmez — saf yansıma (<c>NewsSourceOwnershipTests</c> deseni; kaynak
/// taraması <b>değil</b>: <c>init</c> erişimcisi IL'de <c>modreq(IsExternalInit)</c> taşır,
/// yani soru kaynağa bakmadan kesin cevaplanabilir).
/// </para>
/// </remarks>
public class LegalImmutabilityStructureTests
{
    private static bool IsInitOnly(PropertyInfo property) =>
        property.SetMethod is { } setter &&
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

    /// <summary>
    /// Değişmezliğin kapsamı dışında kalan alanlar ve <b>her birinin gerekçesi</b>.
    /// </summary>
    /// <remarks>
    /// ⚠️ Bu liste elle tutuluyor ve bu bilinçli — ama yönü <b>tersine</b> çevrilmiş
    /// olduğu için güvenli: elle tutulan şey "denetlenecekler" değil <b>muafiyetler</b>.
    /// Yeni bir alan eklendiğinde varsayılan <b>denetlenmek</b>tir; muafiyet isteyen kişi
    /// buraya bir satır yazmak ve gerekçesini söylemek zorunda kalır. (Faz A'nın dersi:
    /// elle tutulan bir liste ancak <i>kapsamı daraltmıyorsa</i> güvenlidir.)
    /// </remarks>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal)
    {
        [nameof(LegalDocumentVersion.DocumentId)] = "FK — EF ilişkiyi kurarken yazar",
        [nameof(LegalDocumentVersion.Document)] = "gezinme özelliği",
        [nameof(BaseEntityMarker.Id)] = "BaseEntity — store-generated",
        [nameof(BaseEntityMarker.CreatedAt)] = "BaseEntity — altyapı damgası",
        [nameof(BaseEntityMarker.UpdatedAt)] = "BaseEntity — altyapı damgası"
    };

    /// <summary>Yalnız <see cref="Exempt"/>'in <c>nameof</c>'ları için — <c>BaseEntity</c> soyut.</summary>
    private sealed class BaseEntityMarker : KadirliApp.Domain.Common.BaseEntity;

    [Fact]
    public void EveryContentFieldOfAPublishedVersion_IsInitOnly()
    {
        var properties = typeof(LegalDocumentVersion)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true })
            .Where(p => !Exempt.ContainsKey(p.Name))
            .ToList();

        // 🔑 Testin kendisi "hiçbir şey denetlemiyor" hâline düşerse söylesin: alan adları
        // toptan değişirse yukarıdaki süzgeç boş küme döndürebilir ve test sessizce yeşil
        // kalırdı (§7 madde 67'nin "vakum test" dersi).
        properties.Should().HaveCountGreaterThan(4,
            "sürümün içerik alanları bulunamadı — bu test hiçbir şey denetlemiyor olabilir");

        var offenders = properties.Where(p => !IsInitOnly(p)).Select(p => p.Name).ToList();

        offenders.Should().BeEmpty(
            "yayınlanmış metni koruyan BİRİNCİ hat derleyicidir (§7 madde 72): bu alanlar " +
            "`init` olmalı, `set` DEĞİL — açık setter'lar: {0}. CS8852 aldıysanız çözüm alanı " +
            "açmak değil, yazmayı `TryRevise`/`Publish`/`Supersede` metotlarına taşımaktır.",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Rıza kaydının kararı da <b>yalnız metotlarından</b> yazılabilir: bir kayıt
    /// "onaylandı"ya elle çevrilebilseydi, defterdeki <c>decided_at</c>/<c>revoked_at</c>
    /// ikilisi kararla <b>tutarsız</b> kalırdı ve kanıt sessizce anlamsızlaşırdı.
    /// </summary>
    [Fact]
    public void EveryDecisionFieldOfAConsent_IsInitOnly()
    {
        string[] decisionFields =
        [
            nameof(UserConsent.Granted),
            nameof(UserConsent.DecidedAt),
            nameof(UserConsent.RevokedAt),
            nameof(UserConsent.Source)
        ];

        var properties = typeof(UserConsent)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => decisionFields.Contains(p.Name))
            .ToList();

        properties.Should().HaveCount(decisionFields.Length,
            "alan adları değiştiyse bu test hiçbir şey denetlemiyor olabilir");

        var offenders = properties.Where(p => !IsInitOnly(p)).Select(p => p.Name).ToList();

        offenders.Should().BeEmpty(
            "rıza kararı yalnız `Grant`/`Revoke`/`Deny` ile yazılmalı — açık setter'lar: {0}",
            string.Join(", ", offenders));
    }

    /// <summary>
    /// Geçiş metotlarının <b>varlığı</b> ayrıca kilitli: biri yeniden adlandırılırsa
    /// yukarıdaki testlerin hata mesajları yalan söylemeye başlar ve — daha önemlisi —
    /// yazmanın meşru bir yolu kalmaz.
    /// </summary>
    [Theory]
    [InlineData(typeof(LegalDocumentVersion), "TryRevise")]
    [InlineData(typeof(LegalDocumentVersion), "Publish")]
    [InlineData(typeof(LegalDocumentVersion), "Supersede")]
    [InlineData(typeof(UserConsent), "Grant")]
    [InlineData(typeof(UserConsent), "Revoke")]
    [InlineData(typeof(UserConsent), "Deny")]
    public void TheTransitionMethods_Exist(Type type, string method) =>
        type.GetMethod(method, BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("yazmanın tek meşru yolu bu metot");

    /// <summary>
    /// ⚠️ <b>Bu modülde moderasyon YOK ve <c>Approve*</c> dosya adı YASAK.</b>
    /// </summary>
    /// <remarks>
    /// <c>ModerationSingleOwnerTests</c> moderasyonlu modül kümesini
    /// <c>Features/&lt;M&gt;/</c> altında <b><c>Approve*.cs</c> dosyası var mı</b> diye
    /// türetiyor. Buraya o adla bir dosya konduğu an panel controller'ı,
    /// <c>_ModerationStatusField</c>, <c>ModerationStatusGuard</c> çağrısı ve beş moderasyon
    /// alanının <c>init</c> olması <b>zorunlu hâle gelir</b> — hepsi bu modülde karşılığı
    /// olmayan şeyler. Aynı kural 12.12'de Haberler için kondu; buradaki test onu
    /// <b>Legal</b> için de yazılı hâle getiriyor (kural bir yorumda kalırsa çürür).
    /// </remarks>
    [Fact]
    public void TheLegalModule_HasNoModerationFiles()
    {
        var featuresRoot = System.IO.Path.Combine(RepositoryRoot(), "KadirliApp.Application", "Features", "Legal");
        System.IO.Directory.Exists(featuresRoot).Should().BeTrue();

        var moderationFiles = System.IO.Directory
            .GetFiles(featuresRoot, "Approve*.cs", System.IO.SearchOption.AllDirectories)
            .Select(System.IO.Path.GetFileName)
            .ToList();

        moderationFiles.Should().BeEmpty(
            "hukuki metinlerde moderasyon YOK: yayına çıkmak bir moderasyon kararı değil " +
            "`Publish`'tir. `Approve*.cs` adı, modülü ModerationSingleOwnerTests'in " +
            "moderasyonlu küme tanımına sokar ve karşılığı olmayan beş kural dayatır. " +
            "Bulunanlar: {0}", string.Join(", ", moderationFiles));
    }

    private static string RepositoryRoot()
    {
        var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }
}
