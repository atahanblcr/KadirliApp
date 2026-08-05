using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Common.Sorting;
using KadirliApp.Application.Features.ErrorLogs.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.ErrorLogs.Queries;

/// <summary>
/// Faz 12.1 — hata kayıtlarını okuyan sorgu.
///
/// ⚠️ <b>Bilinçli sınır: yığın metninde arama YOK.</b> <c>stack_trace</c> onbinlerce
/// karakterlik bir kolon; <c>LIKE</c> ile taranırsa panelin en hızlı büyüyen tablosunda
/// indekssiz tam tarama yapılır. Arama mesaj/kod/yol üzerinde koşar — "bu hata nerede
/// doğdu" sorusunun cevabı zaten kaydın detayında.
/// (Aynı gerekçe <c>GetAuditLogsQuery</c>'nin <c>details</c> kolonu için de yazılmıştı.)
/// </summary>
public record GetErrorLogsQuery(QueryErrorLogDto QueryDto) : IRequest<PagedResult<ErrorLogResponseDto>>;

public class GetErrorLogsQueryHandler : IRequestHandler<GetErrorLogsQuery, PagedResult<ErrorLogResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetErrorLogsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<ErrorLogResponseDto>> Handle(GetErrorLogsQuery request, CancellationToken ct)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<ErrorLog>().Query();

        // Hatayı alan kullanıcı hesabını silmiş olabilir — kayıt kalır, kullanıcı gider.
        // Denetim izindeki kararın aynısı: soft-delete süzgeci kapalı, yoksa "kim aldı"
        // bilgisi tam da en çok gerektiğinde isimsizleşir.
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(dto.Source))
            query = query.Where(x => x.Source == dto.Source);

        if (!string.IsNullOrWhiteSpace(dto.Level))
            query = query.Where(x => x.Level == dto.Level);

        if (dto.IsResolved is { } resolved)
            query = query.Where(x => x.IsResolved == resolved);

        // Süzgeç "son görülme" üzerinden: kullanıcı "bugün ne oldu" diye bakar, hatanın
        // ilk doğduğu tarihe değil. Aylar önce başlayıp hâlâ süren hata bugünde görünmeli.
        if (dto.From is { } from)
            query = query.Where(x => x.LastSeenAt >= from.Date);

        if (dto.To is { } to)
        {
            // "5 Ağustos"u seçen kişi o günün tamamını kasteder — gün başını almak
            // o günün tüm kayıtlarını sessizce eler (GetAuditLogsQuery'deki aynı karar).
            var end = to.Date.AddDays(1);
            query = query.Where(x => x.LastSeenAt < end);
        }

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var term = dto.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Message.ToLower().Contains(term) ||
                x.Code.ToLower().Contains(term) ||
                (x.Path != null && x.Path.ToLower().Contains(term)) ||
                (x.TraceId != null && x.TraceId.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        // ⚠️ IPAddress.ToString() SQL'e çevrilemez → ham çek, bellekte biçimle
        // (sayfa boyu kadar satır, tablo tamamı değil).
        var raw = await PanelSorts.ErrorLogs.Apply(query, dto.Sort)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.Source,
                x.Level,
                x.Code,
                x.Message,
                x.Path,
                x.Method,
                x.StatusCode,
                x.TraceId,
                x.UserId,
                UserName = users.Where(u => u.Id == x.UserId).Select(u => u.Username ?? u.Phone).FirstOrDefault(),
                x.IpAddress,
                x.UserAgent,
                x.AppVersion,
                x.Platform,
                x.OsVersion,
                x.Fingerprint,
                x.OccurrenceCount,
                x.FirstSeenAt,
                x.LastSeenAt,
                x.IsResolved,
                ResolvedByName = users.Where(u => u.Id == x.ResolvedBy).Select(u => u.Username ?? u.Phone).FirstOrDefault(),
                x.ResolvedAt,
                x.ResolvedNote
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new ErrorLogResponseDto
        {
            Id = x.Id,
            Source = x.Source,
            Level = x.Level,
            Code = x.Code,
            Message = x.Message,
            Path = x.Path,
            Method = x.Method,
            StatusCode = x.StatusCode,
            TraceId = x.TraceId,
            UserId = x.UserId,
            UserName = x.UserName,
            IpAddress = x.IpAddress?.ToString(),
            UserAgent = x.UserAgent,
            AppVersion = x.AppVersion,
            Platform = x.Platform,
            OsVersion = x.OsVersion,
            Fingerprint = x.Fingerprint,
            OccurrenceCount = x.OccurrenceCount,
            FirstSeenAt = x.FirstSeenAt,
            LastSeenAt = x.LastSeenAt,
            IsResolved = x.IsResolved,
            ResolvedByName = x.ResolvedByName,
            ResolvedAt = x.ResolvedAt,
            ResolvedNote = x.ResolvedNote
        }).ToList();

        return new PagedResult<ErrorLogResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}

/// <summary>Tek kaydın tam ayrıntısı (yığın dâhil) — liste yığını taşımaz, ağır.</summary>
public record GetErrorLogByIdQuery(Guid Id) : IRequest<ErrorLogResponseDto?>;

public class GetErrorLogByIdQueryHandler : IRequestHandler<GetErrorLogByIdQuery, ErrorLogResponseDto?>
{
    private readonly IUnitOfWork _uow;

    public GetErrorLogByIdQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<ErrorLogResponseDto?> Handle(GetErrorLogByIdQuery request, CancellationToken ct)
    {
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        var x = await _uow.Repository<ErrorLog>().Query()
            .Where(e => e.Id == request.Id)
            .Select(e => new
            {
                Entity = e,
                UserName = users.Where(u => u.Id == e.UserId).Select(u => u.Username ?? u.Phone).FirstOrDefault(),
                ResolvedByName = users.Where(u => u.Id == e.ResolvedBy).Select(u => u.Username ?? u.Phone).FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (x is null) return null;

        var e2 = x.Entity;
        return new ErrorLogResponseDto
        {
            Id = e2.Id,
            Source = e2.Source,
            Level = e2.Level,
            Code = e2.Code,
            Message = e2.Message,
            StackTrace = e2.StackTrace,
            Path = e2.Path,
            Method = e2.Method,
            StatusCode = e2.StatusCode,
            TraceId = e2.TraceId,
            UserId = e2.UserId,
            UserName = x.UserName,
            IpAddress = e2.IpAddress?.ToString(),
            UserAgent = e2.UserAgent,
            AppVersion = e2.AppVersion,
            Platform = e2.Platform,
            OsVersion = e2.OsVersion,
            Fingerprint = e2.Fingerprint,
            OccurrenceCount = e2.OccurrenceCount,
            FirstSeenAt = e2.FirstSeenAt,
            LastSeenAt = e2.LastSeenAt,
            IsResolved = e2.IsResolved,
            ResolvedByName = x.ResolvedByName,
            ResolvedAt = e2.ResolvedAt,
            ResolvedNote = e2.ResolvedNote
        };
    }
}
