using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Ads;

/// <summary>
/// Faz 10.6: CreateAd ve UpdateMyAd'ın ortak ilan validasyon kuralları (10.5'te CreateAdCommandHandler'ın
/// private metotlarıydı; kullanıcı ilan güncelleme de aynı kuralları uyguladığından buraya çıkarıldı).
/// ValidationException, ExceptionMiddleware'de 400 VALIDATION_ERROR'a çevrilir.
/// </summary>
internal static class AdSubmissionRules
{
    public const int MaxImages = 10;
    private static readonly Regex PhoneRegex = new(@"^(\+90|0)?5\d{9}$", RegexOptions.Compiled);

    /// <summary>Başlık/açıklama/fiyat/telefon kuralları; IsUserSubmission'da ek olarak cep telefonu format kontrolü.</summary>
    public static void ValidateContent(string title, string description, decimal? price, string contactPhone, bool isUserSubmission)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3 || title.Length > 200)
            throw new ValidationException("Başlık 3-200 karakter olmalıdır.");
        if (string.IsNullOrWhiteSpace(description) || description.Length > 5000)
            throw new ValidationException("Açıklama zorunludur ve 5000 karakteri aşamaz.");
        if (price is < 0)
            throw new ValidationException("Fiyat negatif olamaz.");
        if (string.IsNullOrWhiteSpace(contactPhone))
            throw new ValidationException("İletişim telefonu zorunludur.");
        if (isUserSubmission && !PhoneRegex.IsMatch(contactPhone.Trim()))
            throw new ValidationException("Geçerli bir cep telefonu giriniz (05xx...).");
    }

    /// <summary>10.3 profilePhotoFileId emsali: kullanıcı yalnız KENDİ yüklediği dosyaları ilana bağlayabilir.</summary>
    public static async Task ValidateImageOwnershipAsync(IUnitOfWork uow, IReadOnlyCollection<Guid> imageFileIds, Guid userId, CancellationToken ct)
    {
        if (imageFileIds.Count == 0) return;

        var ownedCount = await uow.Repository<Domain.Entities.File>().Query()
            .CountAsync(f => imageFileIds.Contains(f.Id) && f.DeletedAt == null && f.UploadedBy == userId, ct);
        if (ownedCount != imageFileIds.Count)
            throw new ValidationException("Görsellerden biri bulunamadı veya size ait değil.");
    }

    /// <summary>
    /// Gönderilen property değerlerini kategorinin tanımlarına karşı doğrular (yanlış kategoriye ait id,
    /// tip uyumsuzluğu, tanımsız select seçeneği → 400). Zorunlu alan denetimi yalnız isUserSubmission'da —
    /// panelin property UI'ı olmadığından admin akışını kilitlememek için.
    /// </summary>
    public static async Task<List<(Guid PropertyId, string Value)>> ValidatePropertyValuesAsync(
        IUnitOfWork uow, Guid categoryId, Dictionary<Guid, string>? propertyValues, bool isUserSubmission, CancellationToken ct)
    {
        var provided = (propertyValues ?? new Dictionary<Guid, string>())
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Trim());

        if (provided.Count == 0 && !isUserSubmission)
            return new List<(Guid, string)>();

        var definitions = await uow.Repository<CategoryProperty>().Query()
            .Where(p => p.CategoryId == categoryId)
            .Select(p => new
            {
                p.Id,
                p.PropertyName,
                p.PropertyType,
                p.IsRequired,
                Options = p.Options.Select(o => o.OptionValue).ToList()
            })
            .ToListAsync(ct);

        var unknown = provided.Keys.Where(id => definitions.All(d => d.Id != id)).ToList();
        if (unknown.Count > 0)
            throw new ValidationException("Gönderilen özelliklerden biri bu kategoriye ait değil.");

        if (isUserSubmission)
        {
            var missing = definitions.Where(d => d.IsRequired && !provided.ContainsKey(d.Id)).ToList();
            if (missing.Count > 0)
                throw new ValidationException($"Zorunlu özellikler eksik: {string.Join(", ", missing.Select(m => m.PropertyName))}");
        }

        var result = new List<(Guid, string)>();
        foreach (var (propertyId, value) in provided)
        {
            var def = definitions.First(d => d.Id == propertyId);
            switch (def.PropertyType)
            {
                // ⚠️ Faz 11.14 düzeltmesi: eskiden NumberStyles.Number kullanılıyordu; o stil
                // AllowThousands içerdiği ve .NET grup boyutlarını denetlemediği için Türkçe
                // ondalık gösterimi olan "2020,5" doğrulamadan GEÇİYOR ve 20205 olarak okunuyordu
                // (10 kat sapma, sessiz). Ondalık ayracı nokta, binlik ayracı hiç kabul edilmiyor.
                case PropertyType.Number when !decimal.TryParse(
                        value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out _):
                    throw new ValidationException($"\"{def.PropertyName}\" sayısal bir değer olmalıdır (ondalık ayracı nokta).");
                case PropertyType.Boolean when !bool.TryParse(value, out _):
                    throw new ValidationException($"\"{def.PropertyName}\" true/false olmalıdır.");
                case PropertyType.Select when !def.Options.Contains(value):
                    throw new ValidationException($"\"{def.PropertyName}\" için geçersiz seçenek: {value}");
                case PropertyType.MultiSelect when value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Any(v => !def.Options.Contains(v)):
                    throw new ValidationException($"\"{def.PropertyName}\" için geçersiz seçenek içeriyor: {value}");
                case PropertyType.Text when value.Length > 500:
                    throw new ValidationException($"\"{def.PropertyName}\" 500 karakteri aşamaz.");
            }
            result.Add((propertyId, value));
        }

        return result;
    }
}
