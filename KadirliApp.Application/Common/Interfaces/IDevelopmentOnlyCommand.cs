namespace KadirliApp.Application.Common.Interfaces;

/// <summary>
/// Faz 12.19a — <b>yalnız geliştirme ortamında koşabilen komut.</b> İşaretleyici arayüz;
/// kapıyı <c>DevelopmentOnlyBehavior</c> tutar, komutun kendisi hiçbir kontrol yazmaz.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Bu arayüz bir denetim bulgusundan doğdu (14 Ağu 2026, üçüncü dış analiz).</b>
/// <c>/Dashboard/Seed</c> — <c>MockDataSeeder</c>'ı çağıran panel aksiyonu — Production'da
/// <b>açıktı</b>: ortam kapısı hiç yazılmamıştı, üstelik <c>[HttpGet]</c> olduğu için
/// <c>AutoValidateAntiforgeryToken</c> onu kapsamıyordu. Bileşik hasar: bir yöneticinin
/// ziyaret ettiği kötü niyetli sayfadaki tek bir <c>&lt;img src="…/Dashboard/Seed"&gt;</c>,
/// <b>onun oturumuyla</b> canlıda boş kalan her tabloya sahte içerik basardı
/// (uydurma telefonlar, sahte ilanlar, <b>sahte vefat ilanları</b>) ve yönetici hiçbir şey
/// tıklamamış olurdu.
/// </para>
/// <para>
/// 🔑 <b>Neden kapı controller'da DEĞİL:</b> controller'daki bir <c>if (!env.IsDevelopment)</c>
/// aynı sınıftan bir hatayı <b>bir kez daha</b> mümkün kılar — yarın yazılacak ikinci bir
/// seed/bakım aksiyonu onu <i>yazmayı unutabilir</i> ve unutulduğunu hiçbir şey söylemez.
/// Kapı boru hattına konunca kapsam <b>tipten türer</b>: bu arayüzü uygulayan her komut,
/// hangi host'tan (Api · Web · Hangfire) hangi yoldan çağrılırsa çağrılsın korunur.
/// Controller'daki 404 ikinci hattır ve amacı güvenlik değil <b>UX</b>'tir (§5:
/// "işlevsiz buton yok" — Production'da buton hiç çizilmez).
/// </para>
/// <para>
/// ⚠️ Kapının tuttuğu kural <b>izin verme</b> yönündedir (<c>IsDevelopment</c>), reddetme
/// yönünde değil — gerekçe <see cref="IAppEnvironment"/>'ın notlarında.
/// </para>
/// </remarks>
public interface IDevelopmentOnlyCommand
{
}
