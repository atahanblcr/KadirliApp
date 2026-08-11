using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using Xunit;

namespace KadirliApp.Tests.Integration.Architecture;

/// <summary>
/// Faz 12.12 — <b>görünmez sözleşme #55: senkron ile panel aynı kolona yazamaz.</b>
///
/// 🔴 Bu bloğun 2 numaralı hasar sınıfı: yönetici başlığı düzeltir, bir sonraki senkron
/// üstüne yazar. Panel "kaydedildi" der, kayıt geri döner, <b>kimse hata almaz</b>.
/// Koruma bir <i>tarama</i> değil, <b>derleyici</b>: <c>Source*</c> alanları <c>init</c> ve
/// yalnız <c>ApplySourceSnapshot</c> onlara dokunuyor; <c>*Override</c> alanları da <c>init</c>
/// ve yalnız <c>SetOverrides</c>/<c>ClearOverrides</c>'tan yazılıyor.
///
/// ⚠️ <b>Bu testin var olma sebebi 12.11'in dersinin aynısı</b> (§7 madde 53): <c>init</c>'i
/// bozan biri <c>CS8852</c> alır ve o hatayı çözmenin <i>kolay</i> yolu alanı <c>set</c>'e geri
/// açmaktır — o an her şey derlenir, bütün testler yeşil kalır ve koruma sessizce kaybolur.
///
/// 🔑 <b>Tarama değil YANSIMA kullanılıyor</b> ve bu bilinçli: 12.11'in bulgusu "bir taramanın
/// KAPSAMI da elle tutulan bir listedir" idi. Yansıma, kaynak biçiminden (satır sonu, tek
/// satırlık özellik, farklı dosya adı) bağımsızdır ve alan listesini <b>tipin kendisinden</b>
/// türetir: yarın eklenen bir <c>Source*</c> kolonu kendiliğinden kapsama girer.
/// </summary>
public class NewsSourceOwnershipTests
{
    private static IReadOnlyList<PropertyInfo> PropertiesStartingWith(Type type, string prefix) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

    private static IReadOnlyList<PropertyInfo> PropertiesEndingWith(Type type, string suffix) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name.EndsWith(suffix, StringComparison.Ordinal))
            .ToList();

    /// <summary>
    /// <c>init</c> erişimcisi IL'de <c>modreq(IsExternalInit)</c> ile işaretlenir — yani
    /// "init mi, set mi?" sorusunun kaynağa bakmadan verilebilecek kesin cevabı budur.
    /// </summary>
    private static bool IsInitOnly(PropertyInfo property) =>
        property.SetMethod is { } setter &&
        setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

    private static void AssertInitOnly(IReadOnlyList<PropertyInfo> properties, string owner, string transition)
    {
        properties.Should().NotBeEmpty(
            "denetlenecek alan bulunamadı — adlandırma değiştiyse bu test sessizce hiçbir şey " +
            "denetlemiyor demektir");

        var offenders = properties.Where(p => !IsInitOnly(p)).Select(p => p.Name).ToList();

        offenders.Should().BeEmpty(
            "{0} alanları `init` olmalı, `set` DEĞİL (§7 madde 55) — açık setter'lar: {1}. " +
            "CS8852 aldıysanız çözüm alanı açmak değil, yazmayı `{2}` metoduna taşımaktır.",
            owner, string.Join(", ", offenders), transition);
    }

    [Fact]
    public void SourceOwnedFields_AreInitOnly()
        => AssertInitOnly(PropertiesStartingWith(typeof(NewsArticle), "Source"),
            "kaynağın", nameof(NewsArticle.ApplySourceSnapshot));

    [Fact]
    public void AdminOverrideFields_AreInitOnly()
        => AssertInitOnly(PropertiesEndingWith(typeof(NewsArticle), "Override"),
            "yöneticinin", nameof(NewsArticle.SetOverrides));

    /// <summary>
    /// Kategori sözlüğünde de aynı ayrım var: kaynağın alanları (<c>Name</c>/<c>Slug</c>/
    /// <c>ArticleCount</c>) ile yöneticinin alanları (<c>IsExcluded</c>/<c>ShowInFilterStrip</c>/
    /// <c>DisplayOrder</c>) ayrı metotlardan yazılır.
    /// </summary>
    [Fact]
    public void NewsCategory_KeepsTheSameTwoOwnerSplit()
    {
        var properties = typeof(NewsCategory).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name is "Name" or "Slug" or "ArticleCount" or "IsExcluded" or "ShowInFilterStrip" or "DisplayOrder")
            .ToList();

        properties.Should().HaveCount(6, "alan adları değiştiyse bu test hiçbir şey denetlemiyor olabilir");
        AssertInitOnly(properties, "kategori", nameof(NewsCategory.ApplySourceSnapshot));
    }

    /// <summary>
    /// Geçiş metotlarının <b>varlığı</b> ayrıca kilitli: biri yeniden adlandırılırsa
    /// yukarıdaki testlerin hata mesajları yalan söylemeye başlar ve — daha önemlisi —
    /// yazmanın meşru bir yolu kalmaz.
    /// </summary>
    [Fact]
    public void BothOwners_HaveExactlyOneWritePath()
    {
        foreach (var method in new[]
                 {
                     nameof(NewsArticle.ApplySourceSnapshot),
                     nameof(NewsArticle.MarkSourceGone),
                     nameof(NewsArticle.MarkSourcePublished),
                     nameof(NewsArticle.SetOverrides),
                     nameof(NewsArticle.ClearOverrides),
                     nameof(NewsArticle.Archive),
                     nameof(NewsArticle.Unarchive),
                     nameof(NewsArticle.SetFeatured)
                 })
            typeof(NewsArticle).GetMethod(method).Should().NotBeNull("{0} geçişi kaybolmamalı", method);
    }

    /// <summary>
    /// ⚠️ <b>Moderasyon kelimesinden bilinçli kaçınma.</b>
    /// <c>ModerationSingleOwnerTests.ModeratedModules()</c> moderasyonlu modül kümesini
    /// <c>Features/&lt;M&gt;/</c> altında <b><c>Approve*.cs</c> dosyası var mı</b> diye türetiyor.
    /// Haber modülünde moderasyon <b>yok</b> (otomatik yayın + geri alınabilir gizleme);
    /// buraya <c>ApproveNewsCommand.cs</c> konduğu an panel controller'ı,
    /// <c>_ModerationStatusField</c>, <c>ModerationStatusGuard</c> çağrısı ve beş moderasyon
    /// alanının <c>init</c> olması <b>zorunlu hâle gelir</b> ve süit kırmızıya döner.
    /// Bu test o tuzağı <i>açıklamalı</i> hâle getiriyor: kırıldığında ne yapılacağı belli.
    /// </summary>
    [Fact]
    public void NewsModule_DoesNotDeclareModerationCommands()
    {
        var directory = new DirectoryInfo(Path.Combine(RepositoryRoot(), "KadirliApp.Application", "Features", "News"));
        directory.Exists.Should().BeTrue();

        var moderationFiles = directory
            .GetFiles("Approve*.cs", SearchOption.AllDirectories)
            .Select(f => f.Name)
            .ToList();

        moderationFiles.Should().BeEmpty(
            "haber modülünde moderasyon YOK: gizleme geri alınabilir bir arşivlemedir " +
            "(Archive/Unarchive). `Approve*` adlı bir dosya, ModerationSingleOwnerTests'in " +
            "türetmesini bozar ve bu modülde karşılığı olmayan beş kuralı zorunlu kılar. " +
            "Bulunanlar: {0}", string.Join(", ", moderationFiles));
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(Path.Combine(dir.FullName, "KadirliApp.sln")))
            dir = dir.Parent;

        dir.Should().NotBeNull("testler çözüm kökünün altından koşmalı");
        return dir!.FullName;
    }
}
