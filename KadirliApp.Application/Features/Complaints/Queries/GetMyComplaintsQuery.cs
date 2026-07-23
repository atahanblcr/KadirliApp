using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Complaints.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Complaints.Queries;

/// <summary>
/// Faz 10.8: mobil "şikayetlerim" ekranı — yalnız UserId'ye ait kayıtlar (anonim gönderimler
/// user_id NULL olduğundan hiçbir kullanıcının listesinde görünmez).
/// </summary>
public record GetMyComplaintsQuery(Guid UserId, int Page = 1, int Limit = 20) : IRequest<PagedResult<ComplaintResponseDto>>;

public class GetMyComplaintsQueryHandler : IRequestHandler<GetMyComplaintsQuery, PagedResult<ComplaintResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetMyComplaintsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<ComplaintResponseDto>> Handle(GetMyComplaintsQuery request, CancellationToken cancellationToken)
    {
        var query = _uow.Repository<Complaint>().Query()
            .Where(x => x.UserId == request.UserId);

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(request.Page, request.Limit);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new ComplaintResponseDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Type = x.Type,
                RelatedModule = x.RelatedModule,
                RelatedId = x.RelatedId,
                Subject = x.Subject,
                Message = x.Message,
                Status = x.Status,
                AdminNotes = x.AdminNotes,
                ResolvedBy = x.ResolvedBy,
                ResolvedAt = x.ResolvedAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ComplaintResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
