extern alias WebPanel;

using System.Net;
using System.Text;
using FluentAssertions;
using KadirliApp.Domain.Entities;
using KadirliApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PanelCsv = WebPanel::KadirliApp.Web.Common.PanelCsv;

namespace KadirliApp.Tests.Integration.Panel;

/// <summary>
/// Faz 11.16b — **CSV dışa aktarma** (11.18'den kalan madde).
/// </summary>
/// <remarks>
/// CSV yazmak "virgülle birleştir"den ibaret sanılır; oysa buradaki her iddia,
/// yanlış yapıldığında <b>dosyanın indirildiği ama yanlış olduğu</b> bir hataya karşılık
/// geliyor — kimse hata almaz, yönetici veriye güvenir. En sinsi olanı sessiz kırpma:
/// panel sorguları <c>Pagination.Clamp</c> ile 200 satıra kırpılıyor, tek istekle
/// "hepsini ver" demek 200 satırlık bir dosyayı "tam liste" sanmak demekti.
/// </remarks>
[Collection(PanelCollection.Name)]
public class PanelCsvExportTests : IAsyncLifetime
{
    private readonly WebPanelApplicationFactory _factory;
    private readonly string _marker = "CsvTest" + Guid.NewGuid().ToString("N")[..8];

    public PanelCsvExportTests(WebPanelApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            await db.Ads.IgnoreQueryFilters().Where(a => a.Title.Contains(_marker)).ExecuteDeleteAsync();
        });
    }

    private async Task SeedAdsAsync(params string[] titles)
    {
        await _factory.WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var categoryId = await db.AdCategories.Select(c => c.Id).FirstAsync();
            var userId = await db.Users.Select(u => u.Id).FirstAsync();

            foreach (var title in titles)
            {
                db.Ads.Add(new Ad
                {
                    Title = title,
                    CategoryId = categoryId,
                    UserId = userId,
                    Description = "test",
                    Status = "approved",
                    ContactPhone = "+905550000000",
                    Price = 1500.5m,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                });
            }

            await db.SaveChangesAsync();
        });
    }

    // ─────────────────────── Biçim (Excel uyumu) ───────────────────────

    /// <summary>
    /// 🔴 Dosya <b>UTF-8 BOM</b> ile başlar ve ayraç <b>noktalı virgül</b>dür.
    /// BOM yoksa Excel Türkçe karakterleri bozar ("İstanbul" → "Ä°stanbul");
    /// ayraç virgül olursa Türkçe yerelde tüm satır <b>tek sütuna</b> düşer.
    /// İkisi de "veri doğru, ekran yanlış" sınıfı ve kullanıcı sebebini bulamaz.
    /// </summary>
    [Fact]
    public async Task Export_StartsWithBom_AndUsesSemicolonDelimiter()
    {
        await SeedAdsAsync($"{_marker} ilanı");

        var client = await _factory.SuperAdminAsync();
        var response = await client.GetAsync($"/AdsAdmin/ExportCsv?search={_marker}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF },
            "Excel BOM olmadan kod sayfasını yanlış seçer");

        var text = Encoding.UTF8.GetString(bytes);
        text.Split('\n')[0].Should().Contain("Başlık;", "ayraç noktalı virgül olmalı");
    }

    /// <summary>Yanıt indirilebilir bir dosya olarak işaretlenir (tarayıcıda açılmaz).</summary>
    [Fact]
    public async Task Export_IsSentAsADownloadableFile()
    {
        await SeedAdsAsync($"{_marker} ilanı");

        var client = await _factory.SuperAdminAsync();
        var response = await client.GetAsync($"/AdsAdmin/ExportCsv?search={_marker}");

        response.Content.Headers.ContentDisposition!.FileName.Should().Contain("ilanlar");
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    /// <summary>Durum ham İngilizce basılmaz — değişmez kural 6 CSV için de geçerli.</summary>
    [Fact]
    public async Task Export_WritesTurkishStatusLabels_NotRawValues()
    {
        await SeedAdsAsync($"{_marker} ilanı");

        var client = await _factory.SuperAdminAsync();
        var text = await (await client.GetAsync($"/AdsAdmin/ExportCsv?search={_marker}")).Content.ReadAsStringAsync();

        text.Should().Contain("Onaylandı");
        text.Should().NotContain("approved");
    }

    /// <summary>Dışa aktarma ekrandaki filtreyi kullanır; eşleşmeyen kayıt dosyaya girmez.</summary>
    [Fact]
    public async Task Export_RespectsTheCurrentFilter()
    {
        await SeedAdsAsync($"{_marker} birinci", $"{_marker}FARKLI ikinci");

        var client = await _factory.SuperAdminAsync();
        var text = await (await client.GetAsync($"/AdsAdmin/ExportCsv?search={_marker}FARKLI")).Content.ReadAsStringAsync();

        text.Should().Contain($"{_marker}FARKLI ikinci");
        text.Should().NotContain($"{_marker} birinci");
    }

    // ─────────────────────── Formül enjeksiyonu ───────────────────────

    /// <summary>
    /// 🔴 <b>CSV formül enjeksiyonu.</b> Excel <c>=</c>, <c>+</c>, <c>-</c>, <c>@@</c> ile
    /// başlayan hücreyi FORMÜL olarak çalıştırır. İlan başlıklarını <b>vatandaş</b> yazdığı
    /// için bu gerçek bir saldırı yüzeyi: <c>=HYPERLINK(...)</c> başlıklı bir ilan,
    /// yöneticinin Excel'inde canlı bağlantıya dönüşür.
    /// </summary>
    [Theory]
    [InlineData("=HYPERLINK(\"http://kotu.site\")")]
    [InlineData("+1+1")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public void Escape_NeutralizesFormulaCells(string dangerous)
    {
        var escaped = PanelCsv.Escape(dangerous);

        // ⚠️ Hücre ayrıca tırnak/ayraç içeriyorsa RFC4180 gereği tırnaklanır; o yüzden
        // iddia "tek tırnakla başlar" değil, "kaçış tırnakları soyulunca tek tırnakla başlar".
        escaped.TrimStart('"').Should().StartWith("'",
            "formül karakteriyle başlayan hücre metne sabitlenmeli, yoksa Excel onu çalıştırır");
    }

    /// <summary>Ayraç/tırnak/satır sonu içeren hücre RFC4180'e göre tırnaklanır.</summary>
    [Theory]
    [InlineData("Ali;Veli", "\"Ali;Veli\"")]
    [InlineData("12\" ekran", "\"12\"\" ekran\"")]
    [InlineData("iki\nsatır", "\"iki\nsatır\"")]
    public void Escape_QuotesCellsThatWouldBreakTheGrid(string raw, string expected)
        => PanelCsv.Escape(raw).Should().Be(expected);

    /// <summary>Zararsız hücreye dokunulmaz — gereksiz tırnak dosyayı okunmaz yapardı.</summary>
    [Fact]
    public void Escape_LeavesOrdinaryTextAlone()
        => PanelCsv.Escape("Kadirli Belediyesi").Should().Be("Kadirli Belediyesi");

    // ─────────────────────── Sessiz kırpma (bu dosyanın en kritik testi) ───────────────────────

    /// <summary>
    /// 🔴 <b>Sayfa sınırından fazla kayıt varsa dosya HEPSİNİ içermeli.</b>
    /// </summary>
    /// <remarks>
    /// İlk yazımda dışa aktarma tek istekle <c>Limit = 5000</c> gönderiyordu; ama
    /// <c>Pagination.Clamp</c> panel sorgularını <c>AdminMaxLimit = 200</c>'e kırpıyor ve
    /// bunu <b>sessizce</b> yapıyor. Sonuç: 200 satırlık bir dosya, "tüm liste" sanılır.
    /// Bu test tam olarak o kırpmayı yakalar — eşiğin üstünde kayıt üretip hepsinin
    /// dosyada olduğunu doğruluyor.
    /// </remarks>
    [Fact]
    public async Task Export_IncludesRowsBeyondTheSinglePageLimit()
    {
        const int pageLimit = KadirliApp.Application.Common.Models.Pagination.AdminMaxLimit;
        const int count = pageLimit + 5;

        var titles = Enumerable.Range(1, count).Select(i => $"{_marker} kayit {i:D4}").ToArray();
        await SeedAdsAsync(titles);

        var client = await _factory.SuperAdminAsync();
        var text = await (await client.GetAsync($"/AdsAdmin/ExportCsv?search={_marker}")).Content.ReadAsStringAsync();

        // Başlık satırı hariç veri satırı sayısı.
        var dataRows = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1;

        dataRows.Should().Be(count,
            "dışa aktarma tek sayfayla sınırlı kalırsa {0} satır yerine {1} satır yazar ve " +
            "yönetici eksik dosyayı tam sanar", count, pageLimit);

        text.Should().Contain($"{_marker} kayit {count:D4}",
            "son sayfadaki kayıt da dosyada olmalı");
    }

    /// <summary>
    /// Tavan aşıldığında dosya <b>kırpılmaz, reddedilir</b> ve sebebi söylenir.
    /// Yarım bir dosyayı tam sanmak, dosyayı hiç alamamaktan kötüdür.
    /// </summary>
    [Fact]
    public void RejectIfTooLarge_ExplainsWhatToDo()
    {
        PanelCsv.RejectIfTooLarge(PanelCsv.MaxRows).Should().BeNull("tavana kadar serbest");

        var message = PanelCsv.RejectIfTooLarge(PanelCsv.MaxRows + 1);
        message.Should().NotBeNull();
        message.Should().Contain("filtreyi daraltın", "kullanıcıya ne yapacağı söylenmeli");
    }
}
