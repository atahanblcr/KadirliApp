using System;
using System.Collections.Generic;
using System.Linq;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Trash;

/// <summary>
/// Faz 11.17 — çöp kutusunun kapsamı **tek yerde**.
///
/// Sorgu da geri getirme komutu da bu listeden türer; ikinci bir <c>switch</c> yazılırsa
/// biri güncellenip diğeri unutulur ve "listede görünen ama geri getirilemeyen kayıt"
/// (ya da tersi) ortaya çıkar.
///
/// <b>Bilinçli kapsam dışı bırakılanlar:</b>
/// <list type="bullet">
///   <item><c>User</c> — hesap silme mağaza/gizlilik gereğidir; silinen hesabı yönetici
///         geri açabilseydi kullanıcının talebi geri alınmış olurdu.</item>
///   <item><c>File</c> — kayıt değil ek; sahibi geri geldiğinde bağı zaten duruyor.</item>
///   <item><c>GuideItem</c> — <c>ISoftDeletable</c> <b>değil</b>, silmesi fiziksel.
///         Onun için geri alma <b>mümkün değil</b>; bu bir eksik değil, bilinçli fark.</item>
/// </list>
/// </summary>
public static class TrashModules
{
    /// <summary>Panel modül anahtarı → varlık tipi. Sıra ekrandaki filtre sırasıdır.</summary>
    public static readonly IReadOnlyList<(string Module, Type EntityType)> Supported = new List<(string, Type)>
    {
        ("ads", typeof(Ad)),
        ("announcements", typeof(Announcement)),
        ("deaths", typeof(DeathNotice)),
        ("events", typeof(Event)),
        ("campaigns", typeof(Campaign)),
        ("taxis", typeof(TaxiDriver)),
    };

    public static bool IsSupported(string? module) =>
        module is not null && Supported.Any(s => s.Module == module);

    public static IReadOnlyList<string> Keys => Supported.Select(s => s.Module).ToList();
}
