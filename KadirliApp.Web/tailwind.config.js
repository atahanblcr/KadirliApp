/**
 * Faz 12.9 — panelin Tailwind yapılandırması.
 *
 * 🔴 `content` LİSTESİ BU DOSYANIN EN KRİTİK PARÇASI.
 * Tailwind, taranan dosyalarda GÖRDÜĞÜ sınıfları üretir; görmediğini üretmez.
 * CDN sürümü (tarayıcı içi JIT) çalışma anında DOM'a bakıyordu, yani sınıfın
 * nerede yazıldığı hiç önemli değildi. Derlenmiş sürümde önemli — ve bu fark
 * sessizdir: eksik sınıf hata vermez, yalnız stil uygulanmaz.
 *
 * ⚠️ Bu projede Tailwind sınıfları YALNIZ .cshtml'de DEĞİL:
 *   · Common/PanelDisplay.cs      → durum/rol rozetlerinin renkleri
 *   · Common/PowerOutagePhase.cs  → süren/planlı/bitti rozetleri
 *   · Models/BulkToolbarViewModel.cs → toplu işlem butonlarının renkleri
 * Bunlar taranmasaydı panelin BÜTÜN durum rozetleri renksiz kalırdı ve
 * ne derleyici, ne test, ne de log bunu söylerdi. (Planın metni yalnız
 * `Views/**` diyordu; tarama sırasında bulundu.)
 *
 * 📌 `max-w-9xl` bilinçli olarak TANIMLANMADI: CDN sürümünde de tanımlı
 * değildi, yani bugün de hiçbir şey yapmıyor. Burada tanımlamak "yerelleştirme"
 * değil sessiz bir düzen değişikliği olurdu.
 */
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Views/**/*.cshtml',
    './Common/**/*.cs',
    './Models/**/*.cs',
    './Controllers/**/*.cs',
    './wwwroot/js/**/*.js'
  ],
  theme: {
    extend: {
      fontFamily: {
        // Login ekranı 12.9 öncesinde Google Fonts'tan Inter çekip bunu bir
        // <style> bloğuyla body'ye uyguluyordu. Yazı tipi artık yerel
        // (wwwroot/lib/inter) ve seçim `font-inter` yardımcı sınıfıyla yapılıyor —
        // <style> bloğu kalktığı için CSP'nin style-src'ında istisnaya gerek kalmadı.
        //
        // ⚠️ Inter, varsayılan `sans` yığınına EKLENMEDİ ve bu bilinçli: bugün
        // Inter'i yalnız giriş ekranı kullanıyor, panelin geri kalanı Tailwind'in
        // varsayılan sistem yığınında. `sans`a eklemek "yerelleştirme" değil
        // BÜTÜN PANELİN yazı tipini değiştirmek olurdu — 12.9 bir görünüm fazı değil.
        inter: [
          'Inter',
          'ui-sans-serif',
          'system-ui',
          '-apple-system',
          'Segoe UI',
          'Roboto',
          'Helvetica Neue',
          'Arial',
          'sans-serif'
        ]
      }
    }
  },
  plugins: []
};
