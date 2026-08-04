#!/usr/bin/env python3
"""Kadirli marka görsellerini üretir — Faz 11.16.

Neden bir betik? İkon bir "sanat dosyası" değil, **türetilebilir** bir çıktı olsun
istiyoruz: renkler `MOBILE_UX_PLAN` token'larından, harf uygulamanın kendi yazı
tipinden (Nunito) geliyor. Marka rengi değişirse ikon elle yeniden çizilmez,
bu betik yeniden koşturulur.

Üretilenler (hepsi `assets/branding/` altına):
  icon.png             1024x1024, tam kanama, ALFA YOK   → iOS + Android legacy
  icon_foreground.png  1024x1024, saydam, güvenli alanda → Android adaptive ön plan
  splash.png            768x768,  saydam                 → açılış ekranı logosu

Kullanım (repo kökündeki mobile/ dizininden):
    python3 tool/generate_branding.py
Ardından ikon boyutlarını türetmek için:
    dart run flutter_launcher_icons
    dart run flutter_native_splash:create

⚠️ Pillow gerekiyor (`pip install pillow`) ve yalnız marka değişince koşturulur —
CI'da çalışmaz, çıktılar repoda durur.
"""

import math
import os

from PIL import Image, ImageDraw, ImageFont

# ── Tasarım token'ları (Memory_Bank/MOBILE_UX_PLAN.md ile birebir) ──────────────
PRIMARY = (0x2C, 0x7A, 0x57)       # #2C7A57  marka yeşili
PRIMARY_DEEP = (0x21, 0x5B, 0x41)  # #215B41  koyu uç (degrade)
PRIMARY_LIGHT = (0x46, 0xB0, 0x83)  # #46B083 koyu temanın yeşili
ACCENT = (0xE0, 0x8A, 0x3C)        # #E08A3C  turuncu vurgu (filiz sapı)
SURFACE = (0xFF, 0xFF, 0xFF)       # beyaz    monogram

HERE = os.path.dirname(os.path.abspath(__file__))
MOBILE = os.path.dirname(HERE)
FONT = os.path.join(MOBILE, "assets", "fonts", "Nunito-Bold.ttf")
OUT_DIR = os.path.join(MOBILE, "assets", "branding")

# Süper örnekleme çarpanı — kenarlar pürüzsüz olsun diye büyük çizip küçültüyoruz.
SS = 4


def gradient(size, top, bottom):
    """Dikey degrade bir kare üretir."""
    img = Image.new("RGB", (size, size), top)
    draw = ImageDraw.Draw(img)
    for y in range(size):
        t = y / max(size - 1, 1)
        draw.line(
            [(0, y), (size, y)],
            fill=tuple(round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)),
        )
    return img


def leaf_polygon(cx, cy, length, width, angle_deg):
    """Bir yaprak (iki yayın kesişimi — 'vesica') çokgeni döndürür.

    Yaprak, uçları sivri iki simetrik yaydan oluşur; yarıçapı uzunluk ve
    genişlikten türetiyoruz ki serbest bir 'elle çizim' olmasın.
    """
    half_len = length / 2.0
    half_wid = width / 2.0
    # İki yayın yarıçapı: uçlardan geçen ve ortada half_wid şişen daire.
    radius = (half_len**2 + half_wid**2) / (2 * half_wid)
    offset = radius - half_wid  # yay merkezlerinin eksene uzaklığı
    span = math.asin(half_len / radius)  # uçların merkeze göre açısı

    points = []
    steps = 64
    for side in (1, -1):
        for i in range(steps + 1):
            t = -span + (2 * span) * (i / steps)
            x = radius * math.sin(t)
            y = side * (radius * math.cos(t) - offset)
            points.append((x, y))

    rad = math.radians(angle_deg)
    cos_a, sin_a = math.cos(rad), math.sin(rad)
    return [
        (cx + x * cos_a - y * sin_a, cy + x * sin_a + y * cos_a)
        for (x, y) in points
    ]


def tapered_stem(x0, y0, x1, y1, w0, w1):
    """Kalınlığı w0'dan w1'e daralan bir sap çokgeni (uçları sivri sap)."""
    dx, dy = x1 - x0, y1 - y0
    length = math.hypot(dx, dy) or 1.0
    nx, ny = -dy / length, dx / length  # birim dik vektör
    return [
        (x0 + nx * w0 / 2, y0 + ny * w0 / 2),
        (x1 + nx * w1 / 2, y1 + ny * w1 / 2),
        (x1 - nx * w1 / 2, y1 - ny * w1 / 2),
        (x0 - nx * w0 / 2, y0 - ny * w0 / 2),
    ]


def draw_mark(size, mark_color=SURFACE):
    """Monogram + filizi saydam bir katmana çizer, **gerçek sınır kutusuna göre
    ortalanmış** olarak döndürür.

    `mark_color`: harfin ve yaprağın rengi. İkonda beyaz (yeşil zemin üstünde),
    açılış ekranında marka yeşili (açık zemin) ya da açık yeşil (koyu zemin).

    ⚠️ Ortalama neden hesapla yapılmıyor: filiz K'nın sağına taştığı için harfi
    tuvale ortalamak **bileşimi** sola kaydırıyor. Önce serbestçe çiziyor, sonra
    çizilen her şeyin sınır kutusunu alıp onu ortalıyoruz.
    """
    pad = size  # taşmalara yer bırak; sonunda kırpılıyor
    layer = Image.new("RGBA", (size + 2 * pad, size + 2 * pad), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)

    # "K" — uygulamanın kendi yazı tipi. Nunito'nun yuvarlak-sıcak karakteri
    # markanın "komşu şehir uygulaması" tonuyla tutarlı. Harf **hero**;
    # ikonun 48 px'te okunmasını sağlayan tek unsur o.
    font = ImageFont.truetype(FONT, int(size * 0.72))
    kx, ky = pad + size * 0.16, pad + size * 0.20
    draw.text((kx, ky), "K", font=font, fill=mark_color, anchor="lt")
    _, k_top, k_right, _ = draw.textbbox((kx, ky), "K", font=font, anchor="lt")

    # Filiz — K'nın sağ üst kolunun ucundan çıkan KÜÇÜK bir yaprak.
    # ⚠️ İki tur denendi: (1) yaprak K'nın çapraz koluyla birleşip "roket" gibi
    # okundu, (2) büyük tutulunca harfle yarışıp 48 px'te silueti bozdu. Kural:
    # yaprak **açıkça ikincil** olmalı — küçük boyutta zarifçe kaybolması normal,
    # silueti bozması değil.
    stem_x0, stem_y0 = k_right - size * 0.055, k_top + size * 0.075
    stem_x1, stem_y1 = stem_x0 + size * 0.055, stem_y0 - size * 0.070
    draw.polygon(
        tapered_stem(stem_x0, stem_y0, stem_x1, stem_y1, size * 0.030, size * 0.016),
        fill=ACCENT,
    )
    draw.polygon(
        leaf_polygon(
            stem_x1 + size * 0.048,
            stem_y1 - size * 0.040,
            size * 0.155,
            size * 0.070,
            -40,
        ),
        fill=mark_color,
    )

    # Ortalama **harfe** göre yapılır (filiz değil): kırpma kutusu K'nın
    # merkezinde durur ama çizilen her şeyi kapsayacak kadar büyütülür.
    # Böylece harf tam ortada kalır, filiz yalnız boşluğu doldurur.
    # ⚠️ Doğrudan `layer.getbbox()` ile kırpmak harfi sola kaydırıyordu.
    k_left, k_top2, k_right2, k_bottom = draw.textbbox((kx, ky), "K", font=font, anchor="lt")
    k_cx, k_cy = (k_left + k_right2) / 2, (k_top2 + k_bottom) / 2
    full = layer.getbbox()
    half_w = max(k_cx - full[0], full[2] - k_cx)
    half_h = max(k_cy - full[1], full[3] - k_cy)
    cropped = layer.crop(
        (round(k_cx - half_w), round(k_cy - half_h), round(k_cx + half_w), round(k_cy + half_h))
    )
    scale = min(size / cropped.width, size / cropped.height)
    cropped = cropped.resize(
        (max(1, round(cropped.width * scale)), max(1, round(cropped.height * scale))),
        Image.LANCZOS,
    )
    centered = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    centered.paste(
        cropped,
        ((size - cropped.width) // 2, (size - cropped.height) // 2),
        cropped,
    )
    return centered


def build_icon(size=1024):
    """Tam kanama ikon — iOS ve Android legacy için. **Alfa kanalı yok.**"""
    big = size * SS
    base = gradient(big, PRIMARY, PRIMARY_DEEP)
    # Markayı tuvale sığdırmıyoruz, NEFES payı bırakıyoruz: iOS ikonu köşelerden
    # yuvarlatarak maskeler, kenara dayanan bir mark orada sıkışık görünür.
    inner = int(big * 0.66)
    mark = draw_mark(inner)
    base.paste(mark, ((big - inner) // 2, (big - inner) // 2), mark)
    # iOS ikonları saydamlık KABUL ETMEZ; "RGB" olarak kaydetmek bunu garanti eder.
    return base.resize((size, size), Image.LANCZOS).convert("RGB")


def build_foreground(size=1024):
    """Android adaptive ön planı — içerik ortadaki güvenli alanda kalmalı.

    Adaptive ikonun dış %25'i sistem tarafından kırpılabilir (yuvarlak, kare,
    squircle...). Markayı %58'e küçültüp ortalıyoruz ki hiçbir maskede kesilmesin.
    """
    big = size * SS
    layer = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    inner = int(big * 0.58)
    mark = draw_mark(inner)
    layer.paste(mark, ((big - inner) // 2, (big - inner) // 2), mark)
    return layer.resize((size, size), Image.LANCZOS)


def build_splash(size=768, mark_color=PRIMARY):
    """Açılış ekranı logosu — saydam zemin, **tema başına ayrı renk**.

    🐛 Canlıda yakalanan hata: ilk sürümde açılış logosu ikonla aynı BEYAZ
    marktı. Açılış ekranının zemini ise açık tema rengi (#FAF9F6) — yani beyaz
    logo neredeyse görünmüyordu. İkonun zemini yeşil, açılış ekranınınki değil;
    aynı görseli iki yerde kullanmak sessizce görünmez bir logo üretti.
    """
    big = size * SS
    layer = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    inner = int(big * 0.74)
    mark = draw_mark(inner, mark_color)
    layer.paste(mark, ((big - inner) // 2, (big - inner) // 2), mark)
    return layer.resize((size, size), Image.LANCZOS)


def build_splash_android12(size=1152, mark_color=PRIMARY):
    """Android 12+ açılış ekranı logosu.

    ⚠️ Android 12'nin splash API'si logoyu **dairesel bir maskeye** oturtuyor:
    tuval 1152 px ise güvenli alan yalnız ortadaki **768 px çaplı daire**.
    🐛 Canlıda görüldü: normal splash görseli kullanılınca filiz maskenin dışında
    kalıp **kesildi** — ekranda K ve havada duran turuncu bir sap kaldı.
    Bu yüzden mark, dairenin İÇİNE sığacak biçimde (köşegeni ≤ çap) küçültülüyor.
    """
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    safe_diameter = size * 2 / 3
    # Kare markın köşegeni daireye sığmalı: kenar = çap / √2 (biraz da pay).
    inner = int(safe_diameter / math.sqrt(2) * 0.96)
    mark = draw_mark(inner * SS, mark_color).resize((inner, inner), Image.LANCZOS)
    canvas.paste(mark, ((size - inner) // 2, (size - inner) // 2), mark)
    return canvas


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    for name, image in (
        ("icon.png", build_icon()),
        ("icon_foreground.png", build_foreground()),
        ("splash.png", build_splash(mark_color=PRIMARY)),
        ("splash_dark.png", build_splash(mark_color=PRIMARY_LIGHT)),
        ("splash_android12.png", build_splash_android12(mark_color=PRIMARY)),
        ("splash_android12_dark.png", build_splash_android12(mark_color=PRIMARY_LIGHT)),
    ):
        path = os.path.join(OUT_DIR, name)
        image.save(path)
        print(f"  ✓ {os.path.relpath(path, MOBILE)}  {image.size[0]}x{image.size[1]} {image.mode}")


if __name__ == "__main__":
    main()
