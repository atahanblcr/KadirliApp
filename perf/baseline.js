// Faz 12.22a — taban çizgisi yük senaryosu.
//
// 🔑 BU DOSYANIN AMACI BİR HIZ DEĞİL, BİR CÜMLE ÜRETMEK:
//    "en sıcak beş ucun p95'i şudur."
// 12.22'ye kadar o cümle hiçbir yerde yoktu — proje 82 görünmez sözleşmenin her birini
// ölçerek kilitlemişti ama performans hakkında TEK BİR ölçüm yoktu.
//
// Çalıştırma:
//   k6 run perf/baseline.js
//   k6 run -e BASE_URL=http://localhost:8080 -e VUS=50 -e DURATION=2m perf/baseline.js
//
// ⚠️ Ölçmeden önce panelden "Sayaçları sıfırla" (Performans ekranı): açılıştan beri biriken
//    sayaçlar migration'ı ve ilk isteklerdeki JIT ısınmasını da p95'e karıştırır.
//
// 🔴🔴 EN ÖNEMLİ ÖN KOŞUL — HIZ LİMİTİ (12.22a'da ölçülerek bulundu):
//    API'de IP başına global bir hız limiti var (varsayılan 300 istek / 60 sn,
//    `RateLimiting:Global:PermitLimit`). Yük üreticisi TEK BİR IP'dir, yani limit
//    ilk saniyede dolar ve geri kalan iki dakika boyunca 429 döner. O hâlde ölçülen şey
//    UYGULAMA DEĞİL, HIZ LİMİTİNİN KENDİSİDİR — ve 429 hızlı döndüğü için tablo
//    **çok iyi** görünür (p95 ≈ 1,7 ms). Ölçümün yalan söylemesinin en sinsi biçimi budur.
//    Bu yüzden API ölçüm için limiti kaldırılmış olarak başlatılmalı:
//
//      RateLimiting__Global__PermitLimit=100000000 dotnet run --project KadirliApp.Api -c Release
//
//    Aşağıdaki `rate_limited` eşiği bunu unutan koşuyu KIRMIZIYA düşürür — yani bir
//    sonraki oturum sessizce yanlış bir taban çizgisine güvenemez.

import http from 'k6/http';
import { check, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5005';
const VUS = parseInt(__ENV.VUS || '50', 10);
const DURATION = __ENV.DURATION || '2m';


// En sıcak beş public uç. ⚠️ Liste elle tutuluyor ve bu bilinçli: "sıcak" bir ÜRÜN
// kararıdır (vatandaş neye bakıyor?), kaynak koddan türetilemez.
const ENDPOINTS = [
  { name: 'ads',           path: '/v1/ads?page=1&limit=20' },
  { name: 'news',          path: '/v1/news?page=1&limit=20' },
  { name: 'announcements', path: '/v1/announcements?page=1&limit=20' },
  // 🔴 Sayfalama YOK ve olamaz (görünmez sözleşme #1: düz dizi döner, mobil süren/planlı
  //    ayrımını TAM LİSTEDEN yapar). Bugün küçük; büyüdüğünde SESSİZCE yavaşlar ve
  //    sözleşme sayfalamayı yasakladığı için çözüm sayfalama OLAMAZ. Bu yüzden ayrıca
  //    ölçülüyor (12.22b/4).
  { name: 'power_outages', path: '/v1/power-outages' },
  { name: 'events',        path: '/v1/events?page=1&limit=20' },
  // Arama ayrı bir uç sayılır: 12.13'te eklenen GIN/trigram indekslerinin gerçekten
  // kullanıldığını 12.22c'nin bozma turu bu istekle ölçer.
  { name: 'news_search',   path: '/v1/news?page=1&limit=20&search=kadirli' },
];

// 🔴 Uç başına AYRI trend ve HEPSİ init bağlamında kurulur. Tek bir global
// `http_req_duration`, altı ucun p95'ini tek sayıya ezerdi — ve "hangi uç yavaş?"
// sorusu tam da bu başlığın cevaplamak için var olduğu soru.
// 🐛 İlk yazımda trend'ler ilk kullanımda tembel kuruluyordu; k6 metrikleri YALNIZ init
//    bağlamında kabul eder, koşu 2 dakika boyunca her yinelemede istisna attı ve tablo
//    BOŞ ama koşu "tamamlandı" göründü — ölçüm altyapısının kendi sessiz hatası.
const TRENDS = {};
for (const ep of ENDPOINTS) TRENDS[ep.name] = new Trend(`ep_${ep.name}`, true);

// 🔴 429 oranı AYRI ölçülür. `http_req_failed` de kırmızıya döner ama SEBEBİ söylemez;
//    bu metrik "ölçümün önünde hız limiti var" teşhisini doğrudan yazar.
const rateLimited = new Rate('rate_limited');

// Şehir ölçeğine göre hedef: 50 eşzamanlı kullanıcı, 2 dakika (Progress.md 12.22a/2).
export const options = {
  // 🐛 k6 varsayılan olarak trend'lerde p(99)'u ve count'u HESAPLAMAZ (avg/min/med/max/p(90)/p(95)).
  //    İlk koşuda tablo p99 sütununa 0, istek sütununa "undefined" yazdı — sayı olmayan bir
  //    sayı, olmayan bir sayıdan kötüdür: okuyan kişi "p99 sıfır" diye okur.
  summaryTrendStats: ['med', 'p(95)', 'p(99)', 'max', 'count'],
  scenarios: {
    hot_endpoints: {
      executor: 'constant-vus',
      vus: VUS,
      duration: DURATION,
      gracefulStop: '10s',
    },
  },
  thresholds: {
    // 📌 Bunlar bir HEDEF değil, bir ALARM. Taban çizgisi henüz yokken "iyi" sayı
    // uydurmak, bu projenin her yerde reddettiği şeydir: kanıtsız karar. Eşikler
    // kabaca "vatandaş bekliyor" sınırına konuldu; ölçüm sonrası daraltılır.
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
    // 🔑 Bu satır bir HIZ eşiği değil, bir DOĞRULUK eşiği: senaryo hiç istek atmadan da
    // "tamamlandı" diyebilir (ilk yazımdaki init-bağlamı hatası tam olarak bunu yaptı —
    // 2 dakika koştu, 5,4 milyon yineleme "tamamladı", tablo BOŞ çıktı). Bir check
    // geçmiyorsa ölçüm yalancıdır ve k6 artık bunu KIRMIZI olarak söylüyor.
    checks: ['rate>0.99'],
    // 🔴 Tek bir 429 bile taban çizgisini geçersiz kılar (yukarıdaki ön koşula bakın).
    rate_limited: ['rate<0.001'],
  },
};

export default function () {
  group('hot', () => {
    for (const ep of ENDPOINTS) {
      const res = http.get(`${BASE_URL}${ep.path}`, { tags: { endpoint: ep.name } });
      TRENDS[ep.name].add(res.timings.duration);
      rateLimited.add(res.status === 429);
      check(res, {
        [`${ep.name} 200`]: (r) => r.status === 200,
        // 🔑 Yalnız durum kodu yetmez: zarf ("success") boş bir gövdede de 200 döner.
        [`${ep.name} gövde dolu`]: (r) => r.body && r.body.length > 20,
      });
    }
  });
}

export function handleSummary(data) {
  const rows = [];
  for (const key of Object.keys(data.metrics)) {
    if (!key.startsWith('ep_')) continue;
    const m = data.metrics[key].values;
    rows.push({
      endpoint: key.slice(3),
      count: m.count,
      p50: round(m.med),
      p95: round(m['p(95)']),
      p99: round(m['p(99)']),
      max: round(m.max),
    });
  }
  rows.sort((a, b) => b.p95 - a.p95);

  const failRate = data.metrics.http_req_failed
    ? data.metrics.http_req_failed.values.rate
    : 0;

  // 🔴 Satır yoksa bu bir taban çizgisi DEĞİLDİR. Boş bir tabloyu "ölçüm" diye
  //    yazdırmak, ölçüm yapmamaktan kötüdür: bir sonraki oturum ona güvenir.
  if (rows.length === 0) {
    return {
      stdout: '\n=== 12.22a ÖLÇÜM BAŞARISIZ ===\n' +
        'Hiçbir uç için ölçüm toplanmadı. API ayakta mı? BASE_URL doğru mu?\n' +
        'Bu çıktı bir taban çizgisi DEĞİLDİR ve Memory_Bank/Performance_Baseline.md\'ye yazılmaz.\n\n',
    };
  }

  // 🔴 429 gördüysek bu bir taban çizgisi değil, hız limitinin ölçümüdür.
  const limitedRate = data.metrics.rate_limited ? data.metrics.rate_limited.values.rate : 0;
  if (limitedRate > 0.001) {
    return {
      stdout: '\n=== 12.22a ÖLÇÜM GEÇERSİZ — HIZ LİMİTİ ===\n' +
        `İsteklerin %${(limitedRate * 100).toFixed(1)}'i 429 döndü. Ölçülen şey uygulama değil,\n` +
        'IP başına hız limiti (RateLimiting:Global:PermitLimit, varsayılan 300/60 sn).\n' +
        'API\'yi şöyle başlatıp tekrar koşun:\n' +
        '  RateLimiting__Global__PermitLimit=100000000 dotnet run --project KadirliApp.Api -c Release\n\n',
    };
  }

  let out = '\n=== 12.22a TABAN ÇİZGİSİ ===\n';
  out += `VU: ${VUS} · süre: ${DURATION} · hedef: ${BASE_URL}\n`;
  out += `hata oranı: ${(failRate * 100).toFixed(2)}%\n\n`;
  out += 'uç'.padEnd(18) + 'istek'.padStart(8) + 'p50'.padStart(10) + 'p95'.padStart(10) + 'p99'.padStart(10) + 'max'.padStart(10) + '\n';
  for (const r of rows) {
    out += r.endpoint.padEnd(18) + String(r.count).padStart(8)
      + `${r.p50} ms`.padStart(10) + `${r.p95} ms`.padStart(10)
      + `${r.p99} ms`.padStart(10) + `${r.max} ms`.padStart(10) + '\n';
  }

  return {
    stdout: out + '\n',
    'perf/last-run.json': JSON.stringify({ baseUrl: BASE_URL, vus: VUS, duration: DURATION, failRate, rows }, null, 2),
  };
}

function round(v) {
  return v === undefined ? 0 : Math.round(v * 10) / 10;
}
