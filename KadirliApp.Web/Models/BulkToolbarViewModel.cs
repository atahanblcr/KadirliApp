namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 11.18 — <c>_BulkToolbar.cshtml</c>'in modeli.
///
/// Fabrika metotları (<see cref="Moderation"/>, <see cref="DeleteOnly"/>) bilinçli:
/// her liste kendi buton dizisini elle yazsaydı etiketler, ikonlar ve onay metinleri
/// modülden modüle ayrışırdı — panelin 11.15c'de yedi listede birden yaşadığı sorun
/// (bkz. <c>PanelDisplay</c>). Yeni bir liste toplu işlem kazanırken sıfırdan bir şey
/// tasarlamaz, buradaki iki kalıptan birini seçer.
/// </summary>
public sealed class BulkToolbarViewModel
{
    public required string Controller { get; init; }
    public required IReadOnlyList<BulkActionButton> Actions { get; init; }

    /// <summary>
    /// Kutuların ve butonların bağlandığı hedef formun kimliği. Tek yerden türetiliyor:
    /// çubuk ile satır kutusu farklı kimlik üretirse seçim POST'a **hiç** girmez ve
    /// panel "Hiçbir kayıt seçilmedi" der — hata vermeyen, sadece çalışmayan bir özellik.
    /// </summary>
    public static string FormIdFor(string controller) => "bulk-" + controller.ToLowerInvariant();

    public string FormId => FormIdFor(Controller);

    /// <summary>Onay/red/silme üçlüsü — moderasyon kuyruğu olan modüller için.</summary>
    /// <remarks>
    /// 📌 <b>Faz 12.10: <c>includeReject</c> bayrağı kaldırıldı.</b> Tek kullanıcısı vefat
    /// listesiydi ve sebebi "vefatta reddetme komutu yok"tu — moderasyon kuyruğu olan bir
    /// modülün reddetme yolu olmaması bir tasarım değil, bir <b>boşluktu</b>. 12.10
    /// <c>RejectDeathNoticeCommand</c>'i yazınca bayrağın anlamı kalmadı; durması,
    /// bir sonraki modülün "ben de reddetmesiz olayım" diyebileceği bir kapı bırakırdı.
    /// </remarks>
    public static BulkToolbarViewModel Moderation(string controller, string itemLabel)
    {
        var actions = new List<BulkActionButton>
        {
            new()
            {
                Action = "ApproveSelected",
                Label = "Seçilenleri Onayla",
                Icon = "fas fa-check",
                CssClass = "text-white bg-emerald-600 hover:bg-emerald-700"
            },
            new()
            {
                Action = "RejectSelected",
                Label = "Seçilenleri Reddet",
                Icon = "fas fa-times",
                CssClass = "text-white bg-amber-600 hover:bg-amber-700",
                Destructive = true,
                ConfirmText = $"Seçili {itemLabel} kayıtlarını reddetmek istediğinize emin misiniz?"
            },
            DeleteButton(itemLabel)
        };

        return new BulkToolbarViewModel { Controller = controller, Actions = actions };
    }

    /// <summary>Yalnız toplu silme — moderasyonu olmayan modüller için.</summary>
    public static BulkToolbarViewModel DeleteOnly(string controller, string itemLabel) =>
        new() { Controller = controller, Actions = new[] { DeleteButton(itemLabel) } };

    /// <summary>
    /// Faz 12.13 — <b>görünürlük</b> ikilisi: yayından kaldır / geri al. <b>Silme yok.</b>
    /// </summary>
    /// <remarks>
    /// 🔑 Üçüncü bir kalıp gerekti çünkü haberde ne moderasyon (onay/red) var ne de silme:
    /// kaynak hâlâ yayındayken silinen kayıt bir sonraki senkronda <b>geri gelir</b> ve
    /// yönetici "sildim ama döndü" der. <see cref="Moderation"/> kullanılsaydı ekranda
    /// üç işlevsiz buton dururdu — "işlevsiz buton yok" kuralının panel karşılığı.
    /// ⚠️ <c>ArchiveSelected</c> gerekçe ister; alanı görünüm <c>form="…"</c> özniteliğiyle
    /// bu çubuğun gizli formuna bağlar (iç içe <c>&lt;form&gt;</c> doğmasın diye).
    /// </remarks>
    public static BulkToolbarViewModel Visibility(string controller, string itemLabel) => new()
    {
        Controller = controller,
        Actions = new[]
        {
            new BulkActionButton
            {
                Action = "ArchiveSelected",
                Label = "Seçilenleri yayından kaldır",
                Icon = "fas fa-eye-slash",
                CssClass = "text-white bg-orange-600 hover:bg-orange-700",
                Destructive = true,
                ConfirmText = $"Seçili {{count}} {itemLabel} uygulamadan kaldırılacak. Gerektiğinde geri alabilirsiniz. Devam edilsin mi?"
            },
            new BulkActionButton
            {
                Action = "UnarchiveSelected",
                Label = "Seçilenleri yayına al",
                Icon = "fas fa-eye",
                CssClass = "text-white bg-emerald-600 hover:bg-emerald-700"
            }
        }
    };

    private static BulkActionButton DeleteButton(string itemLabel) => new()
    {
        Action = "DeleteSelected",
        Label = "Seçilenleri Sil",
        Icon = "fas fa-trash",
        CssClass = "text-white bg-red-600 hover:bg-red-700",
        Destructive = true,
        // ⚠️ Tek-kayıt silme onayı kaydın ADINI yazar (11.15c); toplu silmede ad yerine
        // SAYI yazılır — çünkü asıl risk "yanlış satır" değil, "kaç satır" olduğunu
        // fark etmemektir. Sayıyı dinleyici çalışma anında yerleştirir.
        ConfirmText = $"Seçili {{count}} {itemLabel} kaydı silinecek. Bu işlem geri alınamaz. Emin misiniz?"
    };
}

/// <summary>Faz 11.18 — <c>_BulkRowCheckbox.cshtml</c>'in modeli (kimlik + hedef form).</summary>
public sealed record BulkRowViewModel(Guid Id, string FormId)
{
    public static BulkRowViewModel For(string controller, Guid id) =>
        new(id, BulkToolbarViewModel.FormIdFor(controller));
}

public sealed class BulkActionButton
{
    /// <summary>Controller aksiyon adı. ⚠️ "…Selected" ile bitmeli — bkz. görünmez sözleşme #19.</summary>
    public required string Action { get; init; }
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required string CssClass { get; init; }
    public bool Destructive { get; init; }
    public string ConfirmText { get; init; } = string.Empty;
}
