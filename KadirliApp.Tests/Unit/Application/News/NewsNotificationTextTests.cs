using FluentAssertions;
using KadirliApp.Application.Features.News;
using Xunit;

namespace KadirliApp.Tests.Unit.Application.News;

/// <summary>
/// Faz 12.15 — <b>bildirim metninin kendi kendine yeterliliği.</b>
/// </summary>
/// <remarks>
/// 🔴 Bu dosyanın kilitlediği şey bir biçim tercihi değil, <b>kabul edilmiş bir uyumluluk
/// borcunun tek hafifletmesi</b>: bildirim <c>relatedType="news"</c> ile gidiyor ve
/// mağazadaki eski sürümler bu türü tanımıyor (§7 madde 18) → dokununca hiçbir yere
/// gitmiyorlar. Gövde <i>"Detay için dokunun"</i> deseydi o kullanıcılara <b>yalan</b>
/// söylemiş olurduk. Gövdenin her koşulda <b>bilgi taşıması</b> bu yüzden bir testle
/// kilitli — kod okunarak anlaşılmayan bir gereklilik.
/// </remarks>
public class NewsNotificationTextTests
{
    // ── Başlık ──────────────────────────────────────────────────────────────

    [Fact]
    public void Title_PrefersTheAdminOverride()
    {
        // Panelde başlığı düzelten yönetici, düzelttiği başlığın gitmesini bekler.
        // Kaynağınki gönderilseydi düzeltme "kaydedildi" der, şehre eski hâli giderdi.
        NewsNotificationText.Title("Yönetici başlığı", "Kaynak başlığı")
            .Should().Be("Yönetici başlığı");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_FallsBackToTheSource_WhenTheOverrideIsBlank(string? blank)
    {
        // "Boş metin" diye bir override yok — varlığın SetOverrides'ı da böyle davranıyor.
        NewsNotificationText.Title(blank, "Kaynak başlığı").Should().Be("Kaynak başlığı");
    }

    [Fact]
    public void Title_IsClampedToTheColumnLimit()
    {
        var giant = string.Join(' ', Enumerable.Repeat("kelime", 200));

        var title = NewsNotificationText.Title(null, giant);

        // push_campaigns.title kolonu 200 — tavan olmasaydı tek bir uzun başlık BÜTÜN
        // partiyi düşürürdü (§ checklist: "kolon tavanını aşan tek alan batch'i öldürür").
        title.Length.Should().BeLessThanOrEqualTo(NewsNotificationText.MaxTitleLength);
        title.Should().EndWith("…");
    }

    // ── Gövde: kendi kendine yeterlilik ─────────────────────────────────────

    [Fact]
    public void Body_UsesTheFirstSentenceOfTheExcerpt()
    {
        var body = NewsNotificationText.Body(
            excerptOverride: null,
            sourceExcerpt: "Kadirli'de yeni köprü açıldı. Tören saat 14:00'te başladı ve akşama kadar sürdü.",
            plainText: "gövde metni",
            title: "Başlık");

        body.Should().Be("Kadirli'de yeni köprü açıldı.");
    }

    [Fact]
    public void Body_PrefersTheAdminExcerptOverride()
    {
        NewsNotificationText.Body("Yönetici özeti.", "Kaynak özeti.", "gövde", "Başlık")
            .Should().Be("Yönetici özeti.");
    }

    [Fact]
    public void Body_FallsBackToThePlainText_WhenThereIsNoExcerpt()
    {
        // Kaynakta özeti olmayan haber var (WP `excerpt` boş dönebiliyor); o hâlde gövdenin
        // ilk cümlesi gelir. Boş bırakmak bildirimi bilgisiz yapardı.
        NewsNotificationText.Body(null, null, "Belediye duyurdu. İkinci cümle.", "Başlık")
            .Should().Be("Belediye duyurdu.");
    }

    [Fact]
    public void Body_FallsBackToTheTitle_WhenNothingElseExists()
    {
        // 🔴 EN ÖNEMLİ İDDİA: gövde ASLA boş olamaz. `PushCampaign.Body` IsRequired ve FCM
        // boş gövdeli mesajı kimi cihazlarda hiç göstermez — yani özetsiz bir haberin
        // bildirimi sessizce buharlaşırdı.
        NewsNotificationText.Body(null, null, null, "Kadirli'de kar yağışı başladı")
            .Should().Be("Kadirli'de kar yağışı başladı");
    }

    [Fact]
    public void Body_IsNeverAnEmptyString()
    {
        NewsNotificationText.Body("  ", "  ", "  ", "Başlık").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Body_NeverAsksTheUserToTapWithoutSayingWhatHappened()
    {
        // Bu, sınıfın var olma sebebinin doğrudan iddiası: gövde HABERİN kendisinden
        // türer, sabit bir çağrı metninden değil. "Detay için dokunun" gibi bir metin
        // buraya bir gün elle eklenirse bu test onu görmeli.
        var body = NewsNotificationText.Body(
            null, "Belediye üç mahallede su kesintisi duyurdu.", null, "Su kesintisi");

        body.Should().Contain("su kesintisi");
        body.Should().NotContain("dokun");
    }

    // ── Cümle bölme ve temizlik ─────────────────────────────────────────────

    [Theory]
    [InlineData("Bitti! İkinci.", "Bitti!")]
    [InlineData("Öyle mi? Evet.", "Öyle mi?")]
    [InlineData("Devam ediyor… Sonra.", "Devam ediyor…")]
    public void FirstSentence_KeepsThePunctuation(string input, string expected)
        => NewsNotificationText.FirstSentence(input).Should().Be(expected);

    [Fact]
    public void FirstSentence_ReturnsEverything_WhenThereIsNoSentenceEnd()
    {
        // ⚠️ "Cümle bulamadım → boş dön" yazılsaydı noktalamasız tek satırlık bir özet
        // bildirimi GÖVDESİZ bırakırdı ve sebebi hiçbir yerde görünmezdi.
        NewsNotificationText.FirstSentence("Noktalaması olmayan bir özet")
            .Should().Be("Noktalaması olmayan bir özet");
    }

    [Fact]
    public void Body_CollapsesLineBreaks()
    {
        // Kaynağın düz metni `\n` taşıyor (gövde HTML'inden türetiliyor) ve bildirim tek
        // satırda çizilir: satır sonu temizlenmezse bazı cihazlarda gövde ilk satırdan
        // sonra kesilir — hata yok, yalnız yarım bilgi.
        var body = NewsNotificationText.Body(null, null, "Birinci satır\n\nikinci satır devam ediyor", "Başlık");

        body.Should().NotContain("\n");
        body.Should().Be("Birinci satır ikinci satır devam ediyor");
    }

    [Fact]
    public void Body_IsClampedAtAWordBoundary()
    {
        var longSentence = string.Join(' ', Enumerable.Repeat("kelime", 100));

        var body = NewsNotificationText.Body(null, longSentence, null, "Başlık");

        body.Length.Should().BeLessThanOrEqualTo(NewsNotificationText.MaxBodyLength + 1); // + "…"
        body.Should().EndWith("…");
        // Kelime ortasından kesilmemeli: "kelim…" okunabilir bir şey değil.
        body.Should().NotContain("kelim…");
    }
}
