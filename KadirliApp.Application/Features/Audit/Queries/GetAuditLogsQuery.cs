using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Audit.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Audit.Queries;

/// <summary>
/// Faz 11.17: denetim izini okuyan ilk sorgu. <c>AuditBehavior</c> 10.9(i)'den beri her
/// hassas yazma komutunu <c>audit_logs</c>'a yazıyordu ama okuyan tek ekran/uç yoktu.
///
/// ⚠️ <b>Bilinçli sınır: <c>details</c> üzerinde serbest metin araması YOK.</b> Kolon
/// <c>jsonb</c>; LINQ'te <c>.Contains()</c> yazmak <c>like_escape(jsonb, unknown)</c>
/// hatası verir, belleğe alıp süzmek ise panelin en hızlı büyüyen tablosunu tümüyle
/// belleğe çeker (checklist §8 — eski <c>UsersAdmin</c> hatası). Bunun yerine yapılandırılmış
/// süzgeçler var; "bu kaydı kim sildi?" sorusunun cevabı zaten <see cref="QueryAuditLogDto.AffectedId"/>.
/// </summary>
public record GetAuditLogsQuery(QueryAuditLogDto QueryDto) : IRequest<PagedResult<AuditLogResponseDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetAuditLogsQueryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<AuditLogResponseDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var dto = request.QueryDto;
        var query = _uow.Repository<AuditLog>().Query();

        // ⚠️ Aktörün hesabı silinmiş olabilir — iz kalır, kullanıcı gider. Soft-delete
        // süzgeci burada KAPATILIR: aksi hâlde "silinen personelin bıraktığı izler"
        // isimsizleşir ve denetim izinin en çok ihtiyaç duyulduğu durumda değeri düşer.
        var users = _uow.Repository<User>().Query().IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(dto.Module))
            query = query.Where(x => x.Module == dto.Module);

        if (!string.IsNullOrWhiteSpace(dto.Action))
            query = query.Where(x => x.Action == dto.Action);

        if (dto.UserId is { } userId)
            query = query.Where(x => x.UserId == userId);

        if (dto.AffectedId is { } affectedId)
            query = query.Where(x => x.AffectedId == affectedId);

        if (dto.From is { } from)
            query = query.Where(x => x.CreatedAt >= from.Date);

        // Kullanıcı "31 Temmuz"u seçtiğinde o günün tamamı kastedilir — gün başını almak
        // (00:00) o günün tüm kayıtlarını sessizce eler.
        if (dto.To is { } to)
        {
            var end = to.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            // Yalnız düz metin kolonlar: etkilenen tip. Personel adı ayrı tabloda olduğu
            // için alt sorguyla süzülür (join projeksiyonu aşağıda zaten yapılıyor).
            var term = dto.Search.Trim().ToLower();

            query = query.Where(x =>
                (x.AffectedType != null && x.AffectedType.ToLower().Contains(term)) ||
                users.Any(u => u.Id == x.UserId && u.Username != null && u.Username.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        // ⚠️ Enum.ToString() ve IPAddress.ToString() SQL'e çevrilemez → ham çekip bellekte
        // biçimle (sayfa boyu kadar satır, tablo tamamı değil).
        var raw = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new
            {
                x.Id,
                x.CreatedAt,
                x.UserId,
                UserName = users.Where(u => u.Id == x.UserId).Select(u => u.Username).FirstOrDefault(),
                UserRole = users.Where(u => u.Id == x.UserId).Select(u => (Domain.Enums.UserRole?)u.Role).FirstOrDefault(),
                x.Module,
                x.Action,
                x.AffectedId,
                x.AffectedType,
                x.Details,
                x.IpAddress,
                x.UserAgent
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(x => new AuditLogResponseDto
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            UserId = x.UserId,
            UserName = x.UserName,
            UserRole = x.UserRole?.ToString(),
            Module = x.Module,
            Action = x.Action,
            AffectedId = x.AffectedId,
            AffectedType = x.AffectedType,
            Details = x.Details,
            IpAddress = x.IpAddress?.ToString(),
            UserAgent = x.UserAgent
        }).ToList();

        return new PagedResult<AuditLogResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
