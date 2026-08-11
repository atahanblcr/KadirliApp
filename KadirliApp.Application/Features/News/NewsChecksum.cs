using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KadirliApp.Application.Common.Interfaces;

namespace KadirliApp.Application.Features.News;

/// <summary>
/// Faz 12.12 — kaynaktaki bir gönderinin <b>anlamlı içeriğinin</b> parmak izi.
///
/// 🔑 Ne işe yarar: <c>modified_after</c> penceresi çakışma payıyla (30 dk) geniş tutulduğu
/// için her koşuda <b>zaten aldığımız</b> kayıtlar geri gelir. Sağlama aynıysa satır
/// <b>hiç yazılmaz</b> — <c>UpdatedAt</c> boşuna değişmez, <c>Updated</c> sayacı yalan
/// söylemez ve panelde "bu haber 96 kez güncellendi" gibi bir gürültü doğmaz.
///
/// ⚠️ <b>Aynalanmış dosya kimliği sağlamaya GİRMEZ.</b> Girseydi, görsel indirmesi bir kez
/// başarısız olan bir haber her koşuda "değişmiş" görünür ve sonsuza kadar yeniden yazılırdı.
/// ⚠️ <see cref="NewsSourcePost.ModifiedAtUtc"/> de girmez: kaynak bazen içerik değişmeden
/// damgayı tazeliyor (tema/eklenti güncellemesi) — damgaya bakan bir sağlama, <b>her</b>
/// haberi periyodik olarak yeniden yazardı.
/// </summary>
public static class NewsChecksum
{
    public static string Compute(
        string title,
        string? excerpt,
        string contentHtml,
        string? imageUrl,
        IEnumerable<int> categoryWpIds)
    {
        // Kategori sırası kaynakta değişebilir; sıralanmadan hesaplanırsa aynı içerik
        // farklı sağlama üretir ve tekilleştirme sessizce hiç çalışmaz.
        var categories = string.Join(",", categoryWpIds.Distinct().OrderBy(x => x));

        var payload = string.Join("", title, excerpt ?? string.Empty, contentHtml, imageUrl ?? string.Empty, categories);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
