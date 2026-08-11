extern alias WebPanel;

using System.Net;
using FluentAssertions;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Observability;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Observability;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelDisplay = WebPanel::KadirliApp.Web.Common.PanelDisplay;
using PanelMenu = WebPanel::KadirliApp.Web.Common.PanelMenu;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 12.1 — **hata kayıtları ekranı.**
///
/// 12.1 öncesinde panel "ne yapıldığını" gösteriyordu ama "ne olduğunu" göstermiyordu:
/// <c>AuditLog</c> yöneticinin başarılı yazma eylemlerini tutar, vatandaşın aldığı hata
/// ise yalnız Seq'e akıyordu. Bu testler ekranın var olduğunu değil, **iki sessiz hasar
/// sınıfının kapandığını** kilitliyor: tekilleştirme gerçekten çalışıyor mu, ve
/// istemciden gelen içerik panelde güvenli mi.
/// </summary>
[Collection(PanelCollection.Name)]
public class PanelErrorLogTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "ErrLogTest-" + Guid.NewGuid().ToString("N")[..8];

    public PanelErrorLogTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() =>
        await _factory.WithScopeAsync(async sp =>
            await sp.GetRequiredService<AppDbContext>().ErrorLogs
                .Where(e => e.Message.Contains(_marker)).ExecuteDeleteAsync());

    private async Task<T?> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        T? result = default;
        await _factory.WithScopeAsync(async sp => result = await query(sp.GetRequiredService<AppDbContext>()));
        return result;
    }

    /// <summary>Doğrudan tabloya kayıt koyar (yazıcı kuyruğunun zamanlamasına bağlı kalmadan).</summary>
    private async Task<Guid> SeedAsync(
        string message,
        string source = "api",
        string level = "error",
        string? path = null,
        int count = 1,
        bool resolved = false,
        DateTime? lastSeen = null)
    {
        var id = Guid.Empty;
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var now = lastSeen ?? DateTime.UtcNow;
            var entity = new ErrorLog
            {
                Source = source,
                Level = level,
                Code = "TEST_ERROR",
                Message = message,
                Path = path,
                Method = "GET",
                StatusCode = 500,
                TraceId = "00-" + Guid.NewGuid().ToString("N"),
                // ⚠️ Parmak izi burada HESAPLANMAZ, rastgele verilir — ve bunun sebebi
                // ilk koşuda öğrenildi: `ErrorFingerprint` sayıları maskelediği için
                // "eşit kayıt 0/1/2/3" gibi seed mesajları AYNI parmak izine düşüyor ve
                // benzersiz indeks patlıyordu. Yani hata testte, normalize edicide değil —
                // tam olarak tasarlandığı gibi davranıyor. Tekilleştirme davranışı zaten
                // sink üzerinden ayrıca test ediliyor; bu yardımcının işi yalnız satır koymak.
                Fingerprint = Guid.NewGuid().ToString("N"),
                OccurrenceCount = count,
                FirstSeenAt = now.AddMinutes(-10),
                LastSeenAt = now,
                IsResolved = resolved
            };
            db.ErrorLogs.Add(entity);
            await db.SaveChangesAsync();
            id = entity.Id;
        });
        return id;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Yalnız-admin deseni (ARCHITECTURE §3)
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔑 11.15b'nin en büyük bulgusu: yalnız-admin ekrana modül anahtarı verilirse izin
    /// matrisinde moderatöre dağıtılabilen ama rol kapısı yüzünden **asla çalışmayacak**
    /// bir yetki belirir. Bu test o hatanın tekrarını engelliyor.
    /// </summary>
    [Fact]
    public void MenuEntry_IsOutsidePermissionMatrix()
    {
        var item = PanelMenu.Items.Single(i => i.Controller == "ErrorLogsAdmin");

        item.Module.Should().BeNull("hata kayıtları izin matrisine dağıtılabilir bir modül değil");
        item.RequiresPermission.Should().BeFalse();
        PanelMenu.AdminOnlyControllers.Should().Contain("ErrorLogsAdmin");
    }

    [Fact]
    public async Task Moderator_CannotOpenScreen()
    {
        var moderator = await _factory.EnsureModeratorAsync("errlog-moderator", "Moderator123!");
        moderator.Should().NotBeNull();

        // ⚠️ Parola kapısı temizlenmezse her sayfa ChangePassword'e 302 döner ve bu test
        // YANLIŞ sebeple geçerdi (rol kapısını hiç sınamadan).
        await _factory.ClearMustChangePasswordAsync("errlog-moderator");

        var client = _factory.CreatePanelClient();
        await client.LoginAsync("errlog-moderator", "Moderator123!");

        var response = await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}");

        // Rol kapısı: 403 ya da giriş/erişim reddi sayfasına yönlendirme.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Found, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Admin_SeesTheScreen_AndTheRecord()
    {
        await SeedAsync($"{_marker} listede görünen hata");

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}")).ReadDecodedBodyAsync();

        html.Should().Contain(_marker);
        html.Should().Contain("Hata Kayıtları");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Tekilleştirme — bu fazın bir numaralı sessiz hasar sınıfı
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🔴 Aynı hata iki kez geldiğinde **tek satır, adet 2** olmalı. Olmazsa tek bir 500
    /// döngüsü tabloyu dakikada on binlerce satırla doldurur — ve bunu hiçbir hata vermeden
    /// yapar. Test yazıcının kendisini (kuyruk → DB) uçtan uca koşturuyor.
    /// </summary>
    [Fact]
    public async Task SameError_Twice_ProducesOneRow_WithCountTwo()
    {
        var message = $"{_marker} tekrar eden hata";

        await WriteThroughSinkAsync(message);
        await WriteThroughSinkAsync(message);

        var rows = await QueryDbAsync(db => db.ErrorLogs
            .Where(e => e.Message.Contains(_marker)).ToListAsync());

        rows!.Should().HaveCount(1, "aynı parmak izi yeni satır açmamalı");
        rows[0].OccurrenceCount.Should().Be(2);
    }

    /// <summary>
    /// Tekilleştirme "mesaj birebir aynı" demek DEĞİL: değişken parçalar normalize edilir.
    /// Edilmeseydi tekilleştirme pratikte hiç çalışmazdı (gerçek mesajların çoğunda id var).
    /// </summary>
    [Fact]
    public async Task SameError_WithDifferentIds_StillOneRow()
    {
        await WriteThroughSinkAsync($"{_marker} kayıt {Guid.NewGuid()} bulunamadı");
        await WriteThroughSinkAsync($"{_marker} kayıt {Guid.NewGuid()} bulunamadı");

        var rows = await QueryDbAsync(db => db.ErrorLogs
            .Where(e => e.Message.Contains(_marker)).ToListAsync());

        rows!.Should().HaveCount(1);
        rows[0].OccurrenceCount.Should().Be(2);
    }

    /// <summary>
    /// 🔑 Çözülmüş bir hata tekrar ederse kayıt **kendiliğinden yeniden açılır**.
    /// Açılmasaydı "çözdüm" denen bir hata sessizce devam eder, panel temiz görünür ve
    /// ekran kendi amacını yalanlardı.
    /// </summary>
    [Fact]
    public async Task ResolvedError_Recurring_ReopensItself()
    {
        var message = $"{_marker} çözülüp tekrar eden hata";
        await WriteThroughSinkAsync(message);

        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var row = await db.ErrorLogs.FirstAsync(e => e.Message.Contains(_marker));
            row.IsResolved = true;
            row.ResolvedAt = DateTime.UtcNow;
            row.ResolvedNote = "düzelttim";
            await db.SaveChangesAsync();
        });

        await WriteThroughSinkAsync(message);

        var reopened = await QueryDbAsync(db => db.ErrorLogs.FirstAsync(e => e.Message.Contains(_marker)));
        reopened!.IsResolved.Should().BeFalse();
        reopened.ResolvedAt.Should().BeNull("yeniden açılan kayıtta bayat çözüm izi kalmamalı");
        reopened.ResolvedNote.Should().BeNull();
    }

    /// <summary>Sink'e yazıp arka plan yazıcısının satırı işlemesini bekler.</summary>
    private async Task WriteThroughSinkAsync(string message)
    {
        await _factory.WithScopeAsync(sp =>
        {
            sp.GetRequiredService<IErrorLogSink>().TryWrite(new ErrorLogEntry(
                Source: ErrorLogSources.Api,
                Level: ErrorLogLevels.Error,
                Code: "TEST_ERROR",
                Message: message,
                StackTrace: "   at KadirliApp.Tests.Fake()"));
            return Task.CompletedTask;
        });

        var expected = await QueryDbAsync(db => db.ErrorLogs
            .Where(e => e.Message.Contains(_marker)).SumAsync(e => e.OccurrenceCount));

        // Yazıcı eşzamansız — satırın gelmesini bekle (sabit gecikme yerine koşul).
        // ⚠️ Tavan 12.12 sonrası 5 sn'den 15 sn'ye çıkarıldı: koşullu bekleme doğru desendi
        // ama tavanı YÜKE göre değil sezgiye göre seçilmişti. Süit 995 teste ve haber senkron
        // testleri de aynı (tek örnekli) yazıcıya olay basmaya başlayınca bu test dolu süitte
        // bir kez kırıldı, tek başına koştuğunda 1 sn'de geçti. Tavanı yükseltmek testi
        // yavaşlatmaz — yalnız BAŞARISIZLIK anında beklenen süreyi uzatır.
        for (var i = 0; i < 300; i++)
        {
            var current = await QueryDbAsync(db => db.ErrorLogs
                .Where(e => e.Message.Contains(_marker)).SumAsync(e => e.OccurrenceCount));
            if (current > expected) return;
            await Task.Delay(50);
        }

        throw new InvalidOperationException("Hata kaydı arka plan yazıcısı tarafından yazılmadı.");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Türkçe görsel dil + XSS
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EverySourceAndLevel_HasTurkishLabel()
    {
        foreach (var source in ErrorLogSources.All)
            PanelDisplay.ErrorSource(source).Label.Should().NotBe(source,
                $"'{source}' ekrana ham İngilizce basılmamalı (Değişmez Kural #6)");

        foreach (var level in ErrorLogLevels.All)
            PanelDisplay.ErrorLevel(level).Label.Should().NotBe(level);
    }

    [Fact]
    public void UnknownSourceOrLevel_IsFlagged_NotSilentlyPrintedRaw()
    {
        PanelDisplay.ErrorSource("desktop").Label.Should().Contain("Bilinmeyen");
        PanelDisplay.ErrorLevel("warning").Label.Should().Contain("Bilinmeyen");
    }

    /// <summary>
    /// 🔴 Mesajın bir kısmı <b>istemciden</b> geliyor (mobil raporu). Görünüm
    /// <c>@Html.Raw</c> kullanırsa panelde **depolanmış XSS** olur: yöneticinin
    /// tarayıcısında saldırganın betiği koşar.
    /// </summary>
    [Fact]
    public async Task ClientSuppliedMessage_IsEscaped_NotRenderedAsHtml()
    {
        await SeedAsync($"{_marker} <script>alert('xss')</script>");

        var client = await _factory.SuperAdminAsync();
        var rawHtml = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}")).Content.ReadAsStringAsync();

        rawHtml.Should().NotContain("<script>alert('xss')</script>",
            "istemciden gelen metin HTML olarak render edilmemeli");
        rawHtml.Should().Contain("&lt;script&gt;", "kaçırılmış hâli görünmeli");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Süzgeç, sıralama, denetim izi
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolvedFilter_SeparatesOpenFromResolved()
    {
        await SeedAsync($"{_marker} açık kayıt", resolved: false);
        await SeedAsync($"{_marker} kapalı kayıt", resolved: true);

        var client = await _factory.SuperAdminAsync();

        var open = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}&resolved=false")).ReadDecodedBodyAsync();
        open.Should().Contain("açık kayıt");
        open.Should().NotContain("kapalı kayıt");

        var closed = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}&resolved=true")).ReadDecodedBodyAsync();
        closed.Should().Contain("kapalı kayıt");
        closed.Should().NotContain("açık kayıt");
    }

    [Fact]
    public async Task SourceFilter_Works()
    {
        await SeedAsync($"{_marker} sunucu kaydı", source: "api");
        await SeedAsync($"{_marker} mobil kaydı", source: "mobile");

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}&source=mobile")).ReadDecodedBodyAsync();

        html.Should().Contain("mobil kaydı");
        html.Should().NotContain("sunucu kaydı");
    }

    /// <summary>
    /// Görünmez sözleşme #30: her sıralama anahtarı benzersiz bir ayraçla bitmeli.
    /// Eşit değerli satırlarda kararsız sıra, sayfalı listede **sessiz veri kaybı** demek.
    /// </summary>
    [Fact]
    public async Task EverySortKey_ProducesStableOrder_ForTiedRows()
    {
        var tied = DateTime.UtcNow;
        for (var i = 0; i < 4; i++)
            await SeedAsync($"{_marker} eşit kayıt {i}", count: 5, lastSeen: tied);

        var client = await _factory.SuperAdminAsync();

        foreach (var key in KadirliApp.Application.Common.Sorting.PanelSorts.ErrorLogs.Keys)
        {
            var first = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}&sort={key}")).ReadDecodedBodyAsync();
            var second = await (await client.GetAsync($"/ErrorLogsAdmin/Index?search={_marker}&sort={key}")).ReadDecodedBodyAsync();

            OrderOfMarkers(first).Should().Equal(OrderOfMarkers(second),
                $"'{key}' anahtarı eşit değerli satırlarda kararlı sıra üretmeli");
        }
    }

    private static List<string> OrderOfMarkers(string html) =>
        System.Text.RegularExpressions.Regex.Matches(html, @"eşit kayıt (\d)")
            .Select(m => m.Groups[1].Value).ToList();

    /// <summary>
    /// Çözme bir <c>IAuditableCommand</c>'dir → denetim izine düşer. Ayrıca eylem adının
    /// Türkçe karşılığı olmalı, yoksa denetim izi ekranı ham İngilizce basar.
    /// </summary>
    [Fact]
    public async Task Resolve_MarksRecord_AndLeavesAuditTrail()
    {
        var id = await SeedAsync($"{_marker} çözülecek hata");

        var client = await _factory.SuperAdminAsync();
        var response = await client.PostFormAsync(
            "/ErrorLogsAdmin/Resolve",
            new Dictionary<string, string> { ["id"] = id.ToString(), ["note"] = "giderildi" },
            tokenFromPath: $"/ErrorLogsAdmin/Details/{id}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Found, HttpStatusCode.Redirect);

        var row = await QueryDbAsync(db => db.ErrorLogs.FirstAsync(e => e.Id == id));
        row!.IsResolved.Should().BeTrue();
        row.ResolvedNote.Should().Be("giderildi");
        row.ResolvedBy.Should().NotBeNull("çözen kişi izlenebilir olmalı");

        var audited = await QueryDbAsync(db => db.AuditLogs
            .AnyAsync(a => a.AffectedId == id && a.Action == "resolve-error"));
        audited.Should().BeTrue("çözme kararı denetim izine düşmeli");
    }

    [Fact]
    public void AuditActionsAndModule_HaveTurkishLabels()
    {
        PanelDisplay.AuditAction("resolve-error").Label.Should().NotContain("Bilinmeyen");
        PanelDisplay.AuditAction("reopen-error").Label.Should().NotContain("Bilinmeyen");

        // ⚠️ Yalnız-admin ekranların modül anahtarı menüde YOK (Module=null) — o yüzden
        // ModuleLabel'ın ayrı bir karşılığı olmalı, aksi hâlde denetim izi "error-logs"
        // diye ham basar.
        PanelDisplay.ModuleLabel("error-logs").Should().NotBe("error-logs");
    }

    [Fact]
    public async Task ExportCsv_RespectsTheCurrentFilter()
    {
        await SeedAsync($"{_marker} dışa aktarılan", source: "mobile");
        await SeedAsync($"{_marker} elenen", source: "api");

        var client = await _factory.SuperAdminAsync();
        var response = await client.GetAsync($"/ErrorLogsAdmin/ExportCsv?search={_marker}&source=mobile");

        response.IsSuccessStatusCode.Should().BeTrue();

        // ⚠️ BOM bayt düzeyinde denetlenmeli: ReadAsStringAsync onu ön ek (preamble) sayıp
        // yutuyor ve "BOM yok" gibi görünüyor — PanelCsvExportTests'teki aynı karar.
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF },
            "BOM olmadan Excel Türkçe karakteri bozar");

        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        csv.Should().Contain("dışa aktarılan");
        csv.Should().NotContain("elenen", "dosya ekrandaki filtrenin aynısı olmalı");
    }

    [Fact]
    public async Task Details_ShowsTraceId_ForSeqCorrelation()
    {
        var id = await SeedAsync($"{_marker} ayrıntı kaydı");
        var row = await QueryDbAsync(db => db.ErrorLogs.FirstAsync(e => e.Id == id));

        var client = await _factory.SuperAdminAsync();
        var html = await (await client.GetAsync($"/ErrorLogsAdmin/Details/{id}")).ReadDecodedBodyAsync();

        // 🔑 traceId, panel kaydını Seq'teki ayrıntılı olaya bağlayan tek anahtar
        // (hata zarfının meta.traceId'siyle aynı — görünmez sözleşme #10).
        html.Should().Contain(row!.TraceId!);
    }
}
