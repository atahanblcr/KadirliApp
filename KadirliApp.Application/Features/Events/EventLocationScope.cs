using System;
using System.Linq;
using KadirliApp.Application.Features.Lookups;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Events;

/// <summary>Etkinlik listesinin konum kapsamı.</summary>
public enum EventLocationScope
{
    /// <summary>Varsayılan — konum süzgeci yok.</summary>
    All,

    /// <summary>Yalnız Kadirli (<c>IsLocal</c>).</summary>
    Local,

    /// <summary>Kadirli dışındaki her yer.</summary>
    Away,

    /// <summary>Osmaniye ilinin tamamı (Kadirli dâhil).</summary>
    Province,

    /// <summary>Çevre iller — Osmaniye <b>dışı</b>.</summary>
    Nearby
}

/// <summary>
/// Faz 12.4 — konum süzgecinin <b>tek sahibi</b>: hem sözlük değerlerinin adı hem de her
/// kapsamın SQL karşılığı burada.
/// </summary>
/// <remarks>
/// 🔴 <b>"Çevre iller" bir sunucu tanımıdır.</b> Mobilde "Osmaniye dışı" diye ayrıca hesaplansaydı,
/// sözlüğe yarın bir Osmaniye ilçesi eklendiğinde mağazadaki eski sürümler onu "çevre il"
/// sayardı — liste yanlış, hata yok (görünmez sözleşme #23'ün sınıfı). İstemci yalnızca
/// <c>locationScope=nearby</c> der, tanımı sormaz.
///
/// ⚠️ <b>Bilinmeyen değer varsayılana düşer</b> (<c>ARCHITECTURE.md</c> §5): bir istemci
/// hatası listeyi boşaltmaz, yalnız süzgeci uygulamaz.
///
/// 📌 <c>onlyLocal</c> ayrı bir süzgeç <b>değil</b>, bu enum'a çevrilen bir kısayoldur —
/// iki ayrı gerçekleme olsaydı "yerel" tanımı iki yerde yaşardı.
/// </remarks>
public static class EventLocationScopes
{
    public const string LocalValue = "local";
    public const string AwayValue = "away";
    public const string ProvinceValue = "province";
    public const string NearbyValue = "nearby";

    /// <summary>
    /// Sorgu parametrelerini tek bir kapsama indirger. <paramref name="raw"/> tanınırsa o
    /// kazanır; yoksa <paramref name="onlyLocal"/> kısayoluna bakılır.
    /// </summary>
    public static EventLocationScope Parse(string? raw, bool? onlyLocal)
    {
        var value = raw?.Trim().ToLowerInvariant();

        return value switch
        {
            LocalValue => EventLocationScope.Local,
            AwayValue => EventLocationScope.Away,
            ProvinceValue => EventLocationScope.Province,
            NearbyValue => EventLocationScope.Nearby,
            _ => onlyLocal switch
            {
                true => EventLocationScope.Local,
                false => EventLocationScope.Away,
                null => EventLocationScope.All
            }
        };
    }

    public static IQueryable<Event> Apply(IQueryable<Event> query, EventLocationScope scope) => scope switch
    {
        EventLocationScope.Local => query.Where(x => x.IsLocal),
        EventLocationScope.Away => query.Where(x => !x.IsLocal),
        EventLocationScope.Province => query.Where(x =>
            x.District != null && x.District.ProvinceName == DistrictDefaults.HomeProvince),
        EventLocationScope.Nearby => query.Where(x =>
            x.District != null && x.District.ProvinceName != DistrictDefaults.HomeProvince),
        _ => query
    };

    /// <summary>Panel süzgeç şeridinin Türkçe karşılığı (ham değer ekrana basılmaz — Değişmez Kural #6).</summary>
    public static string Label(EventLocationScope scope) => scope switch
    {
        EventLocationScope.Local => DistrictDefaults.HomeDistrictName,
        EventLocationScope.Away => $"{DistrictDefaults.HomeDistrictName} dışı",
        EventLocationScope.Province => DistrictDefaults.HomeProvince,
        EventLocationScope.Nearby => "Çevre iller",
        _ => "Tümü"
    };

    /// <summary>Enum → sorgu dizesi değeri (panel bağlantıları için).</summary>
    public static string? Value(EventLocationScope scope) => scope switch
    {
        EventLocationScope.Local => LocalValue,
        EventLocationScope.Away => AwayValue,
        EventLocationScope.Province => ProvinceValue,
        EventLocationScope.Nearby => NearbyValue,
        _ => null
    };
}
