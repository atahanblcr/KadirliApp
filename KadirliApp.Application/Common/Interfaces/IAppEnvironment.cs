namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 12.19a — <b>çalışılan ortamın Application katmanındaki tek soyutlaması.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Neden var:</b> "bu komut yalnız geliştirmede koşabilir" kuralının bir <i>iş kuralı</i>
/// olduğu ortaya çıktı (bkz. <see cref="IDevelopmentOnlyCommand"/>), ama Application katmanı
/// <c>Microsoft.Extensions.Hosting</c>'i tanımıyor — <c>IHostEnvironment</c> buraya
/// sızdırılamaz (§1 katman yönü). Bu arayüz o boşluğu kapatır; gerçeklemesi
/// Infrastructure'da <c>IHostEnvironment</c>'ı sarar.
/// </para>
/// <para>
/// ⚠️ <b>Bilinçli olarak dar:</b> yalnız <see cref="IsDevelopment"/> ve <see cref="Name"/>
/// var. "Production mu?" sorusu bilerek YOK — kapının doğru yönü <i>izin verme</i> yönüdür.
/// <c>!IsProduction()</c> yazan bir kapı, <c>Staging</c>/<c>Test</c> gibi bugün var olmayan
/// bir ortam adı eklendiği gün <b>sessizce açılır</b>; <c>IsDevelopment()</c> ise
/// sessizce kapanır. Sessizce kapanan bir kapı fark edilir, sessizce açılan fark edilmez.
/// </para>
/// </remarks>
public interface IAppEnvironment
{
    /// <summary>Ortam adı — hata mesajlarında "peki neredeyim?" sorusunu cevaplar.</summary>
    string Name { get; }

    /// <summary><c>ASPNETCORE_ENVIRONMENT=Development</c> mi?</summary>
    bool IsDevelopment { get; }
}
