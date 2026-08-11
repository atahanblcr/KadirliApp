using System;

namespace KadirliApp.Application.Features.News;

/// <summary>Senkron sağlığının üç durumu — panelin rozet rengi buradan türer (12.13).</summary>
public enum NewsSyncFreshness
{
    /// <summary>Hiç koşmadı — modül yeni kuruldu ya da iş kaydı silindi.</summary>
    NeverRan,

    /// <summary>Taze.</summary>
    Fresh,

    /// <summary>Gecikmiş — bakılmalı ama henüz "durdu" denemez.</summary>
    Stale,

    /// <summary>🔴 Durmuş sayılır: uygulama <b>eski haberi göstermeye devam ediyor</b>.</summary>
    Stalled
}

/// <summary>
/// Faz 12.12 — <b>bu bloğun 1 numaralı hasar sınıfının ölçüsü.</b>
///
/// 🔴 <i>Kaynak sessizce susabilir.</i> Senkron durursa (WP kapandı, imleç bozuldu, Hangfire
/// kuyruğu takıldı) uygulama eski haberi göstermeye devam eder: uçlar <b>200</b> döner, log
/// temizdir, hiçbir kullanıcı hata almaz. Bu, projedeki diğer modüllerin hiçbirinde olmayan
/// bir arıza biçimi — çünkü diğer modüllerde veriyi <b>biz</b> giriyoruz, girilmediğinde
/// bunu bilen bir insan var.
///
/// 🔑 Eşikler burada, tek yerde: panelin rozeti (12.13), Dashboard kutusu ve — ileride —
/// bir uyarı e-postası aynı kuralı kullanmak zorunda. İkiye ayrılırsa pano "taze" derken
/// uyarı "durdu" der (§7 madde 35'in sınıfı: ikiz eşikler tek kaynaktan gelmeli).
/// </summary>
public static class NewsSyncHealth
{
    /// <summary>Artımlı iş 15 dakikada bir koşar; iki koşu kaçırmak henüz olağandır.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(45);

    /// <summary>
    /// Üç saat: kaynağın ~5 haber/gün üretimiyle bu süre "bir haber kaçırmış olabiliriz"
    /// eşiğidir. Daha uzun tutmak arızayı akşam yerine ertesi gün fark etmek demekti.
    /// </summary>
    public static readonly TimeSpan StalledAfter = TimeSpan.FromHours(3);

    public static NewsSyncFreshness Evaluate(DateTime? lastSuccessfulRunAt, DateTime now)
    {
        if (lastSuccessfulRunAt is null) return NewsSyncFreshness.NeverRan;

        var age = now - lastSuccessfulRunAt.Value;

        // ⚠️ Gelecek tarihli damga (saat kayması) "taze" sayılır: alarm çalmasın diye değil,
        // negatif bir yaşın hangi eşiği geçtiği tanımsız olduğu için.
        if (age >= StalledAfter) return NewsSyncFreshness.Stalled;
        if (age >= StaleAfter) return NewsSyncFreshness.Stale;
        return NewsSyncFreshness.Fresh;
    }
}
