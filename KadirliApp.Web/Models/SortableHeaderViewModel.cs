namespace KadirliApp.Web.Models;

/// <summary>
/// Faz 11.18 — <c>_SortableHeader.cshtml</c>'in modeli: tıklanınca sıralamayı değiştiren
/// tablo başlığı (11.15c B grubu: "sütun sıralaması yalnız İlanlar'da, o da açılır liste").
///
/// 🔑 Bağlantı **mevcut query string'i korur**, yalnız <c>sort</c> ve <c>page</c>
/// parametrelerini değiştirir. `_Pagination.cshtml` ile aynı kural: filtreli bir listede
/// sıralamaya tıklayan yönetici filtresini kaybetmemeli. <c>page</c> ise **sıfırlanır** —
/// 7. sayfadayken sıralama değiştirmek, artık başka kayıtların bulunduğu 7. sayfaya
/// düşmek demektir ve kullanıcı "kayıtlar nereye gitti?" diye sorar.
/// </summary>
public sealed record SortableHeaderViewModel(
    string Label,
    string AscendingKey,
    string DescendingKey,
    string? CurrentSort,
    string? Title = null)
{
    /// <summary>Bu sütun şu an sıralamayı belirliyor mu?</summary>
    public bool IsActive =>
        string.Equals(CurrentSort, AscendingKey, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(CurrentSort, DescendingKey, StringComparison.OrdinalIgnoreCase);

    public bool IsAscending =>
        string.Equals(CurrentSort, AscendingKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Tıklanınca gidilecek anahtar: etkin ve artan ise azalana çevirir, aksi hâlde artan.
    /// (Etkin olmayan sütun ilk tıklamada **artan** başlar — "A'dan Z'ye" beklentisi.)
    /// </summary>
    public string NextSort => IsActive && IsAscending ? DescendingKey : AscendingKey;

    public string Icon => !IsActive
        ? "fas fa-sort text-gray-300"
        : IsAscending ? "fas fa-sort-up text-indigo-600" : "fas fa-sort-down text-indigo-600";

    /// <summary>Ekran okuyucu için: sıralama durumu görsel oktan başka bir yolla da anlaşılmalı.</summary>
    public string AriaSort => !IsActive ? "none" : IsAscending ? "ascending" : "descending";
}
