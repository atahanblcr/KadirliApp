/*
 * Faz 12.9 — panelin ORTAK istemci davranışı.
 *
 * Bu dosya iki sebeple doğdu:
 *
 * 1) **CSP.** Panel artık `script-src 'self' 'nonce-…'` ile korunuyor ve
 *    `unsafe-inline` YOK. Nonce yalnız `<script>` BLOKLARINI kapsar; satır içi
 *    `onclick=` / `onchange=` öznitelikleri nonce ile kurtarılamaz — onları
 *    çalıştırmanın tek yolu `'unsafe-inline'` (ya da `script-src-attr`) açmaktı,
 *    ki bu tam olarak korumanın kendisini iptal ederdi: panelde gösterilen
 *    metinlerin bir kısmı VATANDAŞTAN geliyor (hata kayıtları, şikayet başlıkları)
 *    ve depolanmış XSS bu projenin zaten savaştığı bir sınıf (§7 madde 33).
 *
 * 2) **Tekrar.** `previewImage`/`clearImage` çifti YEDİ görünümde birebir
 *    kopyalanmıştı; tek fark dosya girdisinin id'siydi. 11.15c'de 21 kopya
 *    `confirm()`, 11.18'de 5 kopya toplu işlem JS'i aynı sebeple tekilleştirilmişti —
 *    bu üçüncüsü.
 *
 * 🔑 Kural: buradaki her davranış DELEGE dinleyicidir (document üzerinde) ve
 * `data-*` özniteliğiyle tetiklenir. Yeni bir görünüm bu davranışları kazanmak
 * için JS YAZMAZ, yalnız özniteliği koyar.
 */
(function () {
    'use strict';

    // ————————————————————————————————————————————————————————————
    // Faz 11.15c — silme/geri alınamaz aksiyonlar için ORTAK onay.
    //
    // Önceki hâl: 21 ayrı görünümde inline onsubmit="return confirm('… emin misiniz?')".
    // İki sorunu vardı: (1) hiçbiri NEYİN silindiğini yazmıyordu, yanlış satırı silmek
    // kolaydı; (2) kayıt adını inline JS dizesine gömmek kırılgan — Razor öznitelikleri
    // HTML-encode ettiği için tırnak içeren bir başlık ("Ali'nin arabası") JS dizesini
    // bozuyordu. data-confirm özniteliği bu sorunun ikisini birden çözer: metin
    // öznitelikte güvenle taşınır, buradan getAttribute ile okunur.
    //
    // Faz 12.10 — GÖNDEREN BUTONUN kendi onayı da okunuyor (`e.submitter`) ve toplu
    // işlemin ayrı `click` dinleyicisi buraya KATILDI (tek sahip).
    //
    // Sebep: moderasyon bloğu (_ModerationStatusField) Düzenle formunun İÇİNDE duruyor
    // ve HTML'de form iç içe olamıyor, bu yüzden Reddet/Arşivle butonları hedefi
    // `formaction` ile değiştiriyor. Yani tek formda üç ayrı aksiyon var ve her biri
    // farklı bir onay metni ister — formun tek özniteliği bunu taşıyamaz.
    //
    // 🐛 Buton desteği PanelConfirmDialogTests kırmızıya dönünce eklendi: buton üzerindeki
    // data-confirm, dinleyici olmadan SESSİZCE hiç açılmıyordu — testin var olma sebebi.
    // ⚠️ Önce BUTONA bakılır: buton `formaction` ile farklı bir aksiyona gidiyorsa formun
    // genel metni yanlış şeyi anlatırdı.
    // ⚠️ {count} yer tutucusu toplu işlem içindir (11.18): orada asıl risk "yanlış satır"
    // değil, KAÇ satır olduğunu fark etmemektir. Ayrı bir `click` dinleyicisi olarak
    // yaşıyordu; 12.10'da buraya alındı, yoksa iki dinleyici aynı butonda üst üste
    // binip onay penceresini İKİ KEZ açardı (ilki ham "{count}" metniyle).
    // ————————————————————————————————————————————————————————————
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || !form.hasAttribute) return;

        var submitter = e.submitter;
        var message = (submitter && submitter.getAttribute && submitter.getAttribute('data-confirm'))
            || (form.hasAttribute('data-confirm') ? form.getAttribute('data-confirm') : null);

        if (!message) return;

        if (message.indexOf('{count}') !== -1) {
            var scope = submitter && submitter.closest ? submitter.closest('[data-bulk-scope]') : null;
            message = message.replace('{count}', String(scope ? selectedBulkCount(scope) : 0));
        }

        if (!window.confirm(message)) {
            e.preventDefault();
        }
    });

    // ————————————————————————————————————————————————————————————
    // Faz 11.18 — TOPLU İŞLEM: tek dinleyici, bütün listeler.
    // ————————————————————————————————————————————————————————————
    // ⚠️ Kapsam "form" DEĞİL "[data-bulk-scope]" bölümüdür: kutular tablonun içinde
    // ama hedef formun DIŞINDA durur (iç içe form olmasın diye HTML5 `form=`
    // özniteliğiyle bağlanırlar). closest('form') aransaydı satırdaki tek-kayıt
    // formunu bulur ve seçim sayacı hep 0 kalırdı.
    function bulkRows(scope) {
        return Array.prototype.slice.call(scope.querySelectorAll('[data-bulk-row]'));
    }

    // 12.10: onay metnindeki {count} de buradan besleniyor (yukarıdaki submit dinleyicisi).
    function selectedBulkCount(scope) {
        return bulkRows(scope).filter(function (c) { return c.checked; }).length;
    }

    function bulkRefresh(scope) {
        var count = selectedBulkCount(scope);

        // Butonlar hiçbir şey seçilmeden çalışmaz: boş POST atıp "Hiçbir kayıt
        // seçilmedi" hatası almak, yöneticiye hiçbir şey öğretmeyen bir tur olurdu.
        scope.querySelectorAll('[data-bulk-submit]').forEach(function (b) { b.disabled = count === 0; });

        var wrapper = scope.querySelector('[data-bulk-count-wrapper]');
        var empty = scope.querySelector('[data-bulk-empty]');
        var counter = scope.querySelector('[data-bulk-count]');
        if (counter) counter.textContent = String(count);
        if (wrapper) wrapper.classList.toggle('hidden', count === 0);
        if (empty) empty.classList.toggle('hidden', count > 0);

        // "Tümünü seç" kutusu satırlarla senkron kalır; kısmi seçimde belirsiz görünür.
        var all = scope.querySelector('[data-bulk-select-all]');
        if (all) {
            var total = bulkRows(scope).length;
            all.checked = total > 0 && count === total;
            all.indeterminate = count > 0 && count < total;
        }
    }

    document.addEventListener('change', function (e) {
        var el = e.target;
        if (!el || !el.closest) return;
        var scope = el.closest('[data-bulk-scope]');
        if (!scope) return;

        if (el.hasAttribute('data-bulk-select-all')) {
            bulkRows(scope).forEach(function (c) { c.checked = el.checked; });
        } else if (!el.hasAttribute('data-bulk-row')) {
            return;
        }

        bulkRefresh(scope);
    });

    // 📌 Faz 12.10: buradaki `click` dinleyicisi (toplu işlem onayı + {count} doldurma)
    // yukarıdaki tek `submit` dinleyicisine taşındı. İki ayrı dinleyici kalsaydı
    // 12.10'un buton desteğiyle birlikte aynı butonda üst üste biner ve onay penceresi
    // İKİ KEZ açılırdı — ilki ham "{count}" metniyle.

    document.querySelectorAll('[data-bulk-scope]').forEach(bulkRefresh);

    // ————————————————————————————————————————————————————————————
    // Faz 12.9 — TEK GÖRSEL ÖNİZLEME (yedi kopyanın yerine).
    //
    // Kullanımı iki öznitelik:
    //   <input type="file" data-image-input data-image-preview="#imagePreviewWrap">
    //   <button type="button" data-image-clear="#coverInput">
    //
    // Önizleme görseli sarmalayıcının İÇİNDEKİ ilk <img>'dir — id ile değil.
    // (Yedi görünümün hepsinde sarmalayıcı tek bir <img> taşıyor; id'ye bağlanmak
    // sekizinci görünümde sessizce farklı bir id yazılmasına açık kapı bırakırdı.)
    // ————————————————————————————————————————————————————————————
    function imagePreviewWrapOf(input) {
        var selector = input.getAttribute('data-image-preview');
        return selector ? document.querySelector(selector) : null;
    }

    document.addEventListener('change', function (e) {
        var input = e.target;
        if (!input || !input.hasAttribute || !input.hasAttribute('data-image-input')) return;

        var wrap = imagePreviewWrapOf(input);
        if (!wrap) return;

        var img = wrap.querySelector('img');
        if (img && input.files && input.files[0]) {
            img.src = URL.createObjectURL(input.files[0]);
            wrap.classList.remove('hidden');
        }
    });

    document.addEventListener('click', function (e) {
        var button = e.target && e.target.closest ? e.target.closest('[data-image-clear]') : null;
        if (!button) return;

        var input = document.querySelector(button.getAttribute('data-image-clear'));
        if (!input) return;

        input.value = '';
        var wrap = imagePreviewWrapOf(input);
        if (wrap) wrap.classList.add('hidden');
    });

    // ————————————————————————————————————————————————————————————
    // Faz 12.9 — küçük ortak davranışlar (satır içi öznitelikten taşındı).
    // ————————————————————————————————————————————————————————————

    // <button data-history-back> — "İptal" butonlarının tamamı.
    document.addEventListener('click', function (e) {
        var button = e.target && e.target.closest ? e.target.closest('[data-history-back]') : null;
        if (!button) return;
        e.preventDefault();
        window.history.back();
    });

    // <select data-submit-on-change> — filtre şeritlerindeki "seç ve süz" kutuları.
    document.addEventListener('change', function (e) {
        var el = e.target;
        if (!el || !el.hasAttribute || !el.hasAttribute('data-submit-on-change')) return;
        if (el.form) el.form.submit();
    });

    // <select data-toggle-target="#x" data-toggle-when="A B"> — seçili değer listedeyse
    // hedefi göster, değilse gizle. (İlan kategorisi özellik formundaki tek kullanım.)
    // ⚠️ Görünürlük `hidden` SINIFIYLA değiştirilir, style.display ile değil:
    // satır içi stil yazmak CSP'nin style-src'ında istisna gerektirirdi.
    function refreshToggleTarget(el) {
        var target = document.querySelector(el.getAttribute('data-toggle-target'));
        if (!target) return;
        var allowed = (el.getAttribute('data-toggle-when') || '').split(/\s+/).filter(Boolean);
        target.classList.toggle('hidden', allowed.indexOf(el.value) === -1);
    }

    document.addEventListener('change', function (e) {
        var el = e.target;
        if (!el || !el.hasAttribute || !el.hasAttribute('data-toggle-target')) return;
        refreshToggleTarget(el);
    });

    document.querySelectorAll('[data-toggle-target]').forEach(refreshToggleTarget);
})();
