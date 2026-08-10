/**
 * Faz 12.9 — üçüncü taraf panel varlıklarını node_modules'ten wwwroot/lib'e kopyalar.
 *
 * Neden bir betik: `npm install` node_modules'ü doldurur ama node_modules **commit
 * edilmez**. Panelin çalışması için gereken dosyalar wwwroot altında ve depoda
 * durmak zorunda — depoyu klonlayan biri `npm install` çalıştırmadan paneli
 * açabilmeli (jQuery zaten bu desende duruyor).
 *
 * 🔴 Inter'de latin-ext ZORUNLU. Türkçe'nin ğ · ş · İ · ı harfleri `latin`
 * altkümesinde YOK, `latin-ext`te. Yalnız `latin` yerelleştirilseydi giriş
 * ekranındaki Türkçe metinlerin bu harfleri sessizce yedek yazı tipine düşerdi:
 * sayfa açılır, hata çıkmaz, yalnız harfler diğerlerinden farklı görünür.
 * Google Fonts CDN'i iki altkümeyi de kendiliğinden servis ediyordu, yani bu
 * ancak yerelleştirdikten sonra ortaya çıkabilecek bir kayıptı.
 */
import { cp, mkdir, rm, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const web = resolve(here, '..');
const lib = resolve(web, 'wwwroot', 'lib');
const nm = (...p) => resolve(web, 'node_modules', ...p);

/** Inter'in yerelleştirilen ağırlıkları — Login.cshtml'in CDN bağlantısıyla birebir aynı. */
const INTER_WEIGHTS = [400, 500, 600, 700];
/** ⚠️ latin-ext olmadan Türkçe harfler yedek yazı tipine düşer (yukarıdaki not). */
const INTER_SUBSETS = [
  { name: 'latin-ext', range: 'U+0100-02BA, U+02BD-02C5, U+02C7-02CC, U+02CE-02D7, U+02DD-02FF, U+0304, U+0308, U+0329, U+1D00-1DBF, U+1E00-1E9F, U+1EF2-1EFF, U+2020, U+20A0-20AB, U+20AD-20C0, U+2113, U+2C60-2C7F, U+A720-A7FF' },
  { name: 'latin', range: 'U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD' }
];

async function copyLeaflet() {
  const dest = resolve(lib, 'leaflet');
  await rm(dest, { recursive: true, force: true });
  await mkdir(dest, { recursive: true });
  await cp(nm('leaflet', 'dist', 'leaflet.css'), resolve(dest, 'leaflet.css'));
  await cp(nm('leaflet', 'dist', 'leaflet.js'), resolve(dest, 'leaflet.js'));
  // 🔴 images/ atlanamaz: leaflet.css işaretçi ve gölge PNG'lerine GÖRELİ yolla
  // başvuruyor. Yalnız css+js kopyalansaydı harita açılır, tıklama çalışır ve
  // seçilen noktanın işaretçisi görünmezdi — "çalışıyor gibi duran" bir kırılma.
  await cp(nm('leaflet', 'dist', 'images'), resolve(dest, 'images'), { recursive: true });
}

async function copyFontAwesome() {
  const dest = resolve(lib, 'fontawesome');
  await rm(dest, { recursive: true, force: true });
  await mkdir(resolve(dest, 'css'), { recursive: true });
  await cp(nm('@fortawesome', 'fontawesome-free', 'css', 'all.min.css'), resolve(dest, 'css', 'all.min.css'));
  // all.min.css webfonts'a ../webfonts/ ile başvurur; klasör adı ve konumu korunmalı.
  await cp(nm('@fortawesome', 'fontawesome-free', 'webfonts'), resolve(dest, 'webfonts'), { recursive: true });
}

async function copyInter() {
  const dest = resolve(lib, 'inter');
  await rm(dest, { recursive: true, force: true });
  await mkdir(resolve(dest, 'files'), { recursive: true });

  const faces = [];
  for (const weight of INTER_WEIGHTS) {
    for (const subset of INTER_SUBSETS) {
      const file = `inter-${subset.name}-${weight}-normal.woff2`;
      await cp(nm('@fontsource', 'inter', 'files', file), resolve(dest, 'files', file));
      faces.push(
        `/* ${subset.name} */\n` +
        `@font-face {\n` +
        `  font-family: 'Inter';\n` +
        `  font-style: normal;\n` +
        `  font-weight: ${weight};\n` +
        `  font-display: swap;\n` +
        `  src: url('./files/${file}') format('woff2');\n` +
        `  unicode-range: ${subset.range};\n` +
        `}`
      );
    }
  }

  // @fontsource'un kendi CSS'ini kopyalamak yerine tek dosya üretiyoruz: paket
  // ağırlık başına ayrı bir .css veriyor ve sekiz <link> satırı, sekiz ayrı
  // isteğe ve unutulmaya açık bir listeye dönüşürdü.
  await writeFile(
    resolve(dest, 'inter.css'),
    '/* ÜRETİLMİŞ DOSYA — elle düzenlemeyin. Kaynak: KadirliApp.Web/tools/copy-vendor.mjs */\n' +
    '/* Yeniden üretmek için: cd KadirliApp.Web && npm run vendor */\n\n' +
    faces.join('\n\n') + '\n'
  );
}

await copyLeaflet();
await copyFontAwesome();
await copyInter();
console.log('✓ wwwroot/lib güncellendi: leaflet · fontawesome · inter');
