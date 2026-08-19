# `perf/` — yük ölçümü (Faz 12.22a)

> 🔑 **Bu klasörün birinci kuralı: ÖNCE ÖLÇ, SONRA OPTİMİZE ET.** Ölçmeden yazılan her
> optimizasyon, bu projenin diğer her yerde reddettiği şeydir: **kanıtsız karar.**

## Çalıştır

```bash
brew install k6                      # macOS
k6 run perf/baseline.js              # varsayılan: localhost:5005, 50 VU, 2 dk
k6 run -e VUS=10 -e DURATION=30s perf/baseline.js
k6 run -e BASE_URL=http://localhost:8080 perf/baseline.js   # üretim yığını (12.21)
```

Çıktı iki yere gider: terminale okunabilir bir tablo, `perf/last-run.json`'a ham sayılar.

## Ölçmeden önce

1. **API ve Postgres ayakta olsun** (`docker compose up -d` + `dotnet run --project KadirliApp.Api`).
2. **Panelden `Performans → Sayaçları sıfırla`.** Açılıştan beri biriken sayaçlar
   migration'ı ve ilk isteklerdeki JIT ısınmasını da p95'e karıştırır.
3. Koşu bitince panelin **Performans** ekranına bak: k6 *dışarıdan* (HTTP) ölçer,
   panel *içeriden* (handler başına). İkisinin arasındaki fark **boru hattının ve ağın**
   maliyetidir ve o farkı okumak, tek başına iki sayıyı okumaktan daha çok şey söyler.

## Neden iki ayrı ölçüm var?

| | k6 (`perf/baseline.js`) | Panel → Performans |
|---|---|---|
| Nereden bakar | Dışarıdan, HTTP istemcisi olarak | İçeriden, MediatR boru hattından |
| Neyi görür | Ağ + Kestrel + middleware + handler | Yalnız handler (ve cache) |
| Ne zaman | Elle, yük altında | **Her zaman**, gerçek trafikte |
| Kalıcılık | `perf/last-run.json` | Redis (10 dk TTL) |

🔴 İkisi de gerekli: k6 **yük altında** ne olduğunu söyler ama yalnız koştuğunda; panel
**sürekli** bakar ama gerçek eşzamanlılığı üretmez.

## Taban çizgisi nerede yazıyor?

`Memory_Bank/Performance_Baseline.md` — ölçülmüş sayılar, ölçüm koşulları ve
**ne zaman ölçüldüğü**. Taban çizgisi olmadan bir sonraki oturumun *"iyileştirdim"*
iddiası ölçülemez, yani bu projenin kabul etmediği türden bir iddia olur.
