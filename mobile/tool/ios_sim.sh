#!/usr/bin/env bash
#
# iOS simülatörünü komut satırından sürmek için yardımcı (canlı doğrulama).
#
# ── NEDEN VAR ────────────────────────────────────────────────────────────────
# `xcrun simctl` ekran görüntüsü alabiliyor ama **dokunuş gönderemiyor**;
# Android'deki `adb shell input tap` karşılığı iOS'ta yok. Bu script macOS'un
# erişilebilirlik (AX) katmanı üzerinden Simulator penceresine sentetik tıklama
# ve klavye olayı gönderiyor → iOS'ta da Android'deki gibi uçtan uca canlı
# doğrulama yapılabiliyor.
#
# ── İKİ KRİTİK ÖĞRENİM (31 Tem 2026'da teşhis edildi) ────────────────────────
# 1) Simulator penceresi **odakta değilken AX ağacından kayboluyor** →
#    `-1719 Geçersiz dizin`. Bu, izin sorunu SANILIP yanlış teşhis ediliyor.
#    Gerçek izin hatası `-1743`'tür. Bu yüzden her komut önce `activate` yapar.
# 2) Koordinat eşlemesinde **pencere kutusu kullanılamaz**: pencere başlık
#    çubuğu + kenar boşluğu içeriyor, cihaz ekranı ondan küçük ve kaymış.
#    Doğru referans pencerenin içindeki **AXGroup** (= cihaz ekranı).
#
# ── GEREKSİNİM ───────────────────────────────────────────────────────────────
# Claude Code'u/terminali çalıştıran uygulama için:
#   Sistem Ayarları → Gizlilik ve Güvenlik → Erişilebilirlik → izin açık.
# (`check` komutu bunu doğrular.)
#
# ── KULLANIM ─────────────────────────────────────────────────────────────────
#   tool/ios_sim.sh check                # izin + pencere + cihaz ekranı kutusu
#   tool/ios_sim.sh shot out.png         # ekran görüntüsü
#   tool/ios_sim.sh tap <x> <y>          # CİHAZ pikseli = screenshot koordinatı
#   tool/ios_sim.sh text "savrun"        # odaktaki alana yaz
#   tool/ios_sim.sh key return           # tuş gönder (return/escape/delete...)
#
# Akış: `shot` al → görüntüdeki hedefin piksel koordinatını oku → `tap` ile
# aynı koordinatı gönder. Ölçek dönüşümünü script yapıyor.
#
# ── BİLİNEN SINIRLAR ─────────────────────────────────────────────────────────
# • **Kaydırma (scroll) YOK.** System Events sürükleme/tekerlek olayı üretmiyor;
#   Python `Quartz` (pyobjc) bu makinede kurulu değil. Ekranın altında kalan
#   içerik için ya simülatör penceresini büyütün ya da o senaryoyu Android
#   emülatöründe doğrulayın.
# • Etikete göre dokunma (`AXDescription` araması) denendi, **çalışmıyor**:
#   Simulator'ın AX ağacında `entire contents` boş dönüyor (Flutter semantikleri
#   bu yolla listelenemiyor). Bilinçli olarak eklenmedi — çalışmayan komut
#   bırakmamak için.
# • macOS donanım klavyesi `text` sırasında otomatik düzeltme balonu
#   gösterebilir; yazılan metin doğru gider, balon `key escape` ile kapatılır.

set -euo pipefail

activate() {
  osascript -e 'tell application "Simulator" to activate' >/dev/null 2>&1 || true
  # Odak değişimi anlık değil; pencere AX ağacına dönene kadar kısa bekleme.
  sleep 0.4
}

device_screen_geometry() {
  osascript -e 'tell application "System Events" to tell process "Simulator" to return (get position of (first UI element of window 1 whose role is "AXGroup")) & (get size of (first UI element of window 1 whose role is "AXGroup"))'
}

# Cihaz pikseli (screenshot koordinatı) → macOS ekran koordinatı.
map_point() {
  local dev_x="$1" dev_y="$2" geom
  geom=$(device_screen_geometry 2>&1) || {
    echo "HATA: cihaz ekranı okunamadı → $geom" >&2
    echo "  -1719 = pencere odakta değil / simülatör kapalı" >&2
    echo "  -1743 = erişilebilirlik izni yok" >&2
    exit 1
  }
  IFS=', ' read -r gx gy gw gh <<<"$geom"

  local png dev_w dev_h
  png="$(mktemp -t iossim).png"
  xcrun simctl io booted screenshot "$png" >/dev/null 2>&1
  dev_w=$(sips -g pixelWidth "$png" | awk '/pixelWidth/{print $2}')
  dev_h=$(sips -g pixelHeight "$png" | awk '/pixelHeight/{print $2}')
  rm -f "$png"

  awk -v dx="$dev_x" -v dy="$dev_y" -v dw="$dev_w" -v dh="$dev_h" \
      -v gx="$gx" -v gy="$gy" -v gw="$gw" -v gh="$gh" \
      'BEGIN { printf "%d %d", gx + (dx/dw)*gw, gy + (dy/dh)*gh }'
}

case "${1:-}" in
  check)
    echo "erişilebilirlik izni:"
    if osascript -e 'tell application "System Events" to return name of first process whose frontmost is true' >/dev/null 2>&1; then
      echo "  ✓ var"
    else
      echo "  ✗ YOK → Sistem Ayarları > Gizlilik ve Güvenlik > Erişilebilirlik"
      exit 1
    fi
    activate
    echo "pencere        : $(osascript -e 'tell application "System Events" to tell process "Simulator" to return (get position of window 1) & (get size of window 1)' 2>&1)"
    echo "cihaz ekranı   : $(device_screen_geometry 2>&1)"
    ;;

  shot)
    xcrun simctl io booted screenshot "${2:?dosya adı gerekli}" >/dev/null 2>&1
    echo "kaydedildi: $2"
    ;;

  tap)
    activate
    read -r sx sy <<<"$(map_point "${2:?x gerekli}" "${3:?y gerekli}")"
    osascript -e "tell application \"System Events\" to click at {$sx, $sy}" >/dev/null
    echo "dokunuldu: cihaz($2,$3) → ekran($sx,$sy)"
    ;;

  text)
    activate
    osascript -e "tell application \"System Events\" to keystroke \"${2:?metin gerekli}\"" >/dev/null
    ;;

  key)
    activate
    case "${2:?tuş gerekli}" in
      return) osascript -e 'tell application "System Events" to key code 36' >/dev/null ;;
      escape) osascript -e 'tell application "System Events" to key code 53' >/dev/null ;;
      delete) osascript -e 'tell application "System Events" to key code 51' >/dev/null ;;
      *) echo "bilinmeyen tuş: $2 (return|escape|delete)" >&2; exit 1 ;;
    esac
    ;;

  *)
    sed -n '3,45p' "$0"
    exit 1
    ;;
esac
