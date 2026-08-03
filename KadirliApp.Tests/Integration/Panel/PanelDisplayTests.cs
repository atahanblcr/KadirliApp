extern alias WebPanel;

using FluentAssertions;
using KadirliApp.Domain.Enums;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.15c — **panelin ortak görsel dili** (durum/rol etiketleri + para biçimi).
///
/// Neden bu testler var: 11.15c canlı denetiminde panelin yedi ayrı listesinin
/// <c>approved</c>/<c>pending</c> için elle Türkçe yazdığı, geri kalan HER durumu
/// (<c>expired</c>, <c>archived</c>, <c>SuperAdmin</c>) gri rozetle **ham İngilizce**
/// bastığı görüldü — CLAUDE.md Değişmez Kural #6 ihlali. Kök sebep ortak bir
/// yardımcının olmamasıydı; burada o yardımcının sözlüğü kilitleniyor.
///
/// Bu testler container gerektirmez ama panel koleksiyonundadır (extern alias
/// nedeniyle aynı derleme birimindeler; ayrı fixture açmaları süiti uzatırdı).
/// </summary>
public class PanelDisplayTests
{
    /// <summary>
    /// Kodun ürettiği tüm durum değerleri. Kaynak: entity varsayılanları, komut
    /// handler'ları ve Hangfire işleri (<c>ExpireAdsJob</c> → expired,
    /// <c>ArchiveDeathsJob</c> → archived, <c>PublishScheduledAnnouncementsJob</c> → active).
    /// Yeni bir durum üretilip buraya eklenmezse test değil, PANEL sessizce bozulur —
    /// bu yüzden liste bilinçli olarak elle tutuluyor ve yorumu kaynağı gösteriyor.
    /// </summary>
    public static TheoryData<string> ProducedStatuses() => new()
    {
        "pending", "approved", "rejected", "expired",  // ilan / vefat / etkinlik / kampanya / işletme
        "archived",                                     // ArchiveDeathsJob
        "draft", "scheduled", "active",                 // duyuru yayın durumu
        "in_progress", "resolved"                       // şikayet akışı
    };

    [Theory]
    [MemberData(nameof(ProducedStatuses))]
    public void Status_HasTurkishLabel_ForEveryProducedValue(string status)
    {
        var badge = PanelDisplay.Status(status);

        badge.Label.Should().NotBeNullOrWhiteSpace();
        badge.Label.Should().NotContain("Bilinmeyen",
            "'{0}' kodda üretilen bir durum; panelde ham/İngilizce görünmemeli", status);
        badge.Label.Should().NotBeEquivalentTo(status,
            "etiket ham değerin kendisi olmamalı — Türkçe karşılığı basılmalı");
        badge.Css.Should().NotBeNullOrWhiteSpace("rozetin rengi olmalı");
        badge.Icon.Should().StartWith("fa-", "rozetin ikonu Font Awesome sınıfı olmalı");
    }

    /// <summary>
    /// Ham değerin sessizce sızmadığını gösterir: sözlükte olmayan bir durum
    /// gri bir rozetle "normalmiş gibi" basılmaz, açıkça işaretlenir.
    /// </summary>
    [Fact]
    public void Status_UnknownValue_IsFlagged_NotSilentlyPrintedRaw()
    {
        var badge = PanelDisplay.Status("some_new_status");

        badge.Label.Should().StartWith("Bilinmeyen durum");
        badge.Css.Should().Contain("red", "bilinmeyen durum dikkat çekmeli, gri rozetle normalleşmemeli");
    }

    [Theory]
    [InlineData(UserRole.User, "Vatandaş")]
    [InlineData(UserRole.Moderator, "Moderatör")]
    [InlineData(UserRole.Admin, "Yönetici")]
    [InlineData(UserRole.SuperAdmin, "Süper Yönetici")]
    public void Role_HasTurkishLabel(UserRole role, string expected)
        => PanelDisplay.Role(role).Label.Should().Be(expected);

    /// <summary>
    /// Roller panele iki biçimde geliyor: <c>UsersAdmin</c> enum'un <c>ToString()</c>'ini
    /// ("SuperAdmin"), <c>StaffAdmin</c> ise JWT/DTO biçimini ("super_admin") basıyor.
    /// İkisi de aynı Türkçe etikete düşmeli, yoksa aynı kişi iki ekranda iki farklı rol görünür.
    /// </summary>
    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("super_admin")]
    public void Role_AcceptsBothStringShapes(string raw)
        => PanelDisplay.Role(raw).Label.Should().Be("Süper Yönetici");

    [Fact]
    public void Role_UnknownValue_IsFlagged()
        => PanelDisplay.Role("root").Label.Should().StartWith("Bilinmeyen rol");

    // ── Para ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🐛 11.15c canlı bulgusu: <c>Program.cs</c> paneli InvariantCulture'a sabitlediği için
    /// <c>decimal.ToString("C2")</c> para birimini bilemiyor ve JENERİK <c>¤</c> basıyordu
    /// → ekranda <c>¤750,000.00</c>. Bu test o biçime geri dönüşü yakalar.
    /// </summary>
    [Fact]
    public void TL_UsesTurkishFormat_NotGenericCurrencySign()
    {
        var formatted = PanelDisplay.TL(750000m);

        formatted.Should().Be("₺750.000,00");
        formatted.Should().NotContain("¤", "jenerik para birimi simgesi InvariantCulture + \"C\" biçiminin izidir");
    }

    [Fact]
    public void TL_NullValue_ShowsPlaceholder_NotZero()
    {
        PanelDisplay.TL((decimal?)null).Should().Be("Belirtilmemiş");
        PanelDisplay.TL((decimal?)null, "—").Should().Be("—");
    }

    // ── İzin modülü etiketleri ──────────────────────────────────────────────────

    /// <summary>
    /// 11.15c: <c>StaffAdmin</c> izin rozetleri ham anahtar ("deaths") basıyordu.
    /// Türkçe karşılık <c>PanelMenu.Items</c>'ta zaten vardı.
    /// </summary>
    [Fact]
    public void ModuleLabel_TranslatesPermissionKeys()
    {
        PanelDisplay.ModuleLabel("deaths").Should().Be("Vefat İlanları");
        PanelDisplay.ModuleLabel("power-outages").Should().Be("Elektrik Kesintileri");
    }

    /// <summary>
    /// İzin matrisindeki her modülün Türkçe karşılığı olmalı — biri eksikse
    /// personel ekranında ham anahtar görünür.
    /// </summary>
    [Fact]
    public void ModuleLabel_CoversEveryPermissionModule()
    {
        var modules = PanelMenu.Items.Where(i => i.RequiresPermission).Select(i => i.Module!);

        foreach (var module in modules)
            PanelDisplay.ModuleLabel(module).Should().NotBe(module,
                "'{0}' izin modülünün Türkçe etiketi yok — panelde ham anahtar basılır", module);
    }

    /// <summary>
    /// 11.15c: <c>StaffAdminController.Modules</c> elle yazılmış İKİNCİ bir kopyaydı.
    /// Tek kaynağa bağlandı; bu test ayrışmayı yakalar.
    /// </summary>
    [Fact]
    public void StaffPermissionMatrix_DerivesFromPanelMenu()
    {
        var matrixKeys = WebPanel::KadirliApp.Web.Controllers.StaffAdminController.Modules
            .Select(m => m.Key).ToList();
        var menuKeys = PanelMenu.Items.Where(i => i.RequiresPermission).Select(i => i.Module!).ToList();

        matrixKeys.Should().BeEquivalentTo(menuKeys,
            "izin matrisi ile menü aynı modül anahtarlarını kullanmalı — ayrışırsa " +
            "matriste görünen modül menüde görünmez (ya da tersi) ve hata sessizdir");
    }
}
