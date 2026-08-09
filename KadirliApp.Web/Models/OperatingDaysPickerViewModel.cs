namespace KadirliApp.Web.Models;

/// <summary>Faz 12.5 — seferin gün seçici partial'ının modeli.</summary>
public class OperatingDaysPickerViewModel
{
    /// <summary>Aynı sayfada birden çok seçici olur (her sefer satırı bir tane) — kutu id'leri çakışmasın.</summary>
    public string GroupId { get; init; } = "days";

    /// <summary>Form alanı adı — dizi olarak bağlanır (<c>int[] days</c>).</summary>
    public string Name { get; init; } = "days";

    /// <summary>Mevcut maske; yeni kayıtta "her gün".</summary>
    public int Mask { get; init; } = Domain.Enums.OperatingDays.Daily;
}
