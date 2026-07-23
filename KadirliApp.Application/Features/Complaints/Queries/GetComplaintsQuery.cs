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

public record GetComplaintsQuery(string? Status = null, int Page = 1, int Limit = 20) : IRequest<PagedResult<ComplaintResponseDto>>;

public class GetComplaintsQueryHandler : IRequestHandler<GetComplaintsQuery, PagedResult<ComplaintResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetComplaintsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<ComplaintResponseDto>> Handle(GetComplaintsQuery request, CancellationToken cancellationToken)
    {
        var query = _uow.Repository<Complaint>().Query();

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Status == request.Status);

        var totalCount = await query.CountAsync(cancellationToken);

        var (page, limit) = Pagination.Clamp(request.Page, request.Limit, Pagination.AdminMaxLimit);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new ComplaintResponseDto
            {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User != null ? (x.User.Username ?? x.User.Phone) : null,
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
