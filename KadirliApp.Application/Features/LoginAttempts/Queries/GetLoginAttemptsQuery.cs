using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Common.Security;
using KadirliApp.Application.Common.Sorting;
using KadirliApp.Application.Features.LoginAttempts.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.LoginAttempts.Queries;

/// <summary>
/// Faz 12.2 — giriş denemelerini okuyan sorgu.
///
/// ⚠️ <b>Bilinçli sınır: <c>UserAgent</c> içinde arama YOK.</b> Tarayıcı dizesi uzun,
/// tekrarlı ve indekssiz; <c>LIKE</c> ile taranırsa panelin en hızlı büyüyen ikinci
/// tablosunda tam tarama yapılır. Arama IP ve maskeli kimlik üzerinde koşar — "bu
/// tarayıcı kimdi" sorusu zaten liste satırında görünüyor.
/// (12.1'de <c>stack_trace</c> için verilen aynı karar.)
/// </summary>
public record GetLoginAttemptsQuery(QueryLoginAttemptDto QueryDto)
    : IRequest<PagedResult<LoginAttemptResponseDto>>;

public class GetLoginAttemptsQueryHandler
    : IRequestHandler<GetLoginAttemptsQuery, PagedResult<LoginAttemptResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetLoginAttemptsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<LoginAttemptResponseDto>> Handle(
        GetLoginAttemptsQuery request, CancellationToken ct)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<LoginAttempt>().Query();

        // Hesabını silmiş kullanıcının denemesi de kalır — kayıt durur, kullanıcı gider.
        // (Denetim izi ve hata günlüğündeki aynı karar: "kim" bilgisi tam da en çok
        // gerektiğinde isimsizleşmemeli.)
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(dto.Channel))
            query = query.Where(x => x.Channel == dto.Channel);

        if (dto.Succeeded is { } succeeded)
            query = query.Where(x => x.Succeeded == succeeded);

        if (dto.Suspicious == true)
            query = query.Where(x => x.IsSuspicious);

        if (!string.IsNullOrWhiteSpace(dto.Rule))
            query = query.Where(x => x.SuspicionRule == dto.Rule);

        if (!string.IsNullOrWhiteSpace(dto.Reason))
            query = query.Where(x => x.FailureReason == dto.Reason);

        // 🔑 UserId VEYA maskeli kimlik: hatalı OTP satırlarında UserId boştur
        // (hesap tablosuna dokunulmuyor). Yalnız UserId ile süzülseydi kullanıcı
        // ekranındaki kutu başarısız OTP denemelerini HİÇ göstermezdi.
        if (dto.UserId is { } userId && !string.IsNullOrWhiteSpace(dto.MaskedIdentifier))
            query = query.Where(x => x.UserId == userId || x.Identifier == dto.MaskedIdentifier);
        else if (dto.UserId is { } onlyUserId)
            query = query.Where(x => x.UserId == onlyUserId);
        else if (!string.IsNullOrWhiteSpace(dto.MaskedIdentifier))
            query = query.Where(x => x.Identifier == dto.MaskedIdentifier);

        if (dto.From is { } from)
            query = query.Where(x => x.CreatedAt >= from.Date);

        if (dto.To is { } to)
        {
            // "5 Ağustos"u seçen kişi o günün tamamını kasteder (12.1'deki aynı karar).
            var end = to.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var term = dto.Search.Trim().ToLower();
            query = query.Where(x => x.Identifier.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(dto.Ip))
        {
            // Geçersiz metin girildiyse hiçbir kayıt eşleşmemeli. Süzgeci "yok sayarsak"
            // yönetici yazdığı IP'yi süzdüğünü sanır ve TÜM listeye bakar — güvenlik
            // ekranında en kötü yanlış cevap budur.
            if (System.Net.IPAddress.TryParse(dto.Ip.Trim(), out var ip))
                query = query.Where(x => x.IpAddress != null && x.IpAddress == ip);
            else
                query = query.Where(_ => false);
        }

        var totalCount = await query.CountAsync(ct);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        // ⚠️ IPAddress.ToString() projeksiyonda SQL'e çevrilemez → ham çek, bellekte
        // biçimle (sayfa boyu kadar satır, tablo tamamı değil) — 12.1'in aynı tuzağı.
        var raw = await PanelSorts.LoginAttempts.Apply(query, dto.Sort)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.Channel,
                x.Identifier,
                x.UserId,
                // 🔴 Faz 12.2 canlı testinde yakalandı: buradaki alışılmış desen
                // `u.Username ?? u.Phone`'dur (denetim izi ve hata günlüğü öyle yazıyor)
                // ama BU tabloda o desen maskelemeyi delip geçiyordu: kullanıcı adı
                // olmayan bir VATANDAŞ hesabında ham telefon numarası CSV'ye düşüyordu.
                // Yani Identifier'ı özenle maskeleyip aynı satırın yanına ham numarayı
                // yazan bir dışa aktarma üretmiştik. Ad ve telefon AYRI çekilir,
                // yedek değer bellekte maskelenir.
                UserUsername = users.Where(u => u.Id == x.UserId).Select(u => u.Username).FirstOrDefault(),
                UserPhone = users.Where(u => u.Id == x.UserId).Select(u => u.Phone).FirstOrDefault(),
                x.Succeeded,
                x.FailureReason,
                x.IpAddress,
                x.UserAgent,
                x.IsSuspicious,
                x.SuspicionRule,
                x.CreatedAt,
                x.AlertedAt
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new LoginAttemptResponseDto
        {
            Id = x.Id,
            Channel = x.Channel,
            Identifier = x.Identifier,
            UserId = x.UserId,
            UserName = x.UserUsername
                       ?? (x.UserPhone is null ? null : LoginIdentifierMasker.MaskIdentifier(x.UserPhone)),
            Succeeded = x.Succeeded,
            FailureReason = x.FailureReason,
            IpAddress = x.IpAddress?.ToString(),
            UserAgent = x.UserAgent,
            IsSuspicious = x.IsSuspicious,
            SuspicionRule = x.SuspicionRule,
            CreatedAt = x.CreatedAt,
            AlertedAt = x.AlertedAt
        }).ToList();

        return new PagedResult<LoginAttemptResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}

/// <summary>
/// Dashboard rozeti: son 24 saatteki şüpheli deneme sayısı.
/// 12.1'in "son 24 saatte N açık hata" kartıyla aynı desen ve aynı gerekçe — kimse
/// panele "acaba bir şey mi oldu" diye günde üç kez bakmaz; sayı iniş sayfasında olmalı.
/// </summary>
public record GetSuspiciousLoginCountQuery(int WithinHours = 24) : IRequest<int>;

public class GetSuspiciousLoginCountQueryHandler : IRequestHandler<GetSuspiciousLoginCountQuery, int>
{
    private readonly IUnitOfWork _uow;

    public GetSuspiciousLoginCountQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<int> Handle(GetSuspiciousLoginCountQuery request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-request.WithinHours);
        return await _uow.Repository<LoginAttempt>().Query()
            .CountAsync(x => x.IsSuspicious && x.CreatedAt >= since, ct);
    }
}
