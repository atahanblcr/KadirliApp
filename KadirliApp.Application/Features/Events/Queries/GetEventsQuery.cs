using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Events.Dtos;
using KadirliApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KadirliApp.Application.Features.Events.Queries;

public record GetEventsQuery(QueryEventDto Dto) : IRequest<PagedResult<EventResponseDto>>;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventResponseDto>>
{
    private readonly IUnitOfWork _uow;

    public GetEventsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<EventResponseDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var query = _uow.Repository<Event>().Query();

        if (dto.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == dto.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(dto.Status))
            query = query.Where(x => x.Status == dto.Status);

        if (dto.StartDate.HasValue)
        {
            var start = System.DateTime.SpecifyKind(dto.StartDate.Value.Date, System.DateTimeKind.Utc);
            query = query.Where(x => x.EventDate >= start);
        }

        if (dto.EndDate.HasValue)
        {
            var end = System.DateTime.SpecifyKind(dto.EndDate.Value.Date, System.DateTimeKind.Utc).AddDays(1);
            query = query.Where(x => x.EventDate < end);
        }

        if (dto.IsFree.HasValue)
            query = query.Where(x => x.IsFree == dto.IsFree.Value);

        if (!string.IsNullOrWhiteSpace(dto.Search))
        {
            var search = dto.Search.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                (x.VenueName != null && x.VenueName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var (page, limit) = Pagination.Clamp(dto.Page, dto.Limit, Pagination.AdminMaxLimit);

        var items = await query
            .OrderByDescending(x => x.EventDate)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new EventResponseDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                EventDate = x.EventDate,
                EventTime = x.EventTime,
                VenueName = x.VenueName,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Organizer = x.Organizer,
                TicketPrice = x.TicketPrice,
                IsFree = x.IsFree,
                IsLocal = x.IsLocal,
                CoverImageId = x.CoverImageId,
                CoverImageUrl = x.CoverImage != null ? x.CoverImage.CdnUrl : null,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EventResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = limit
        };
    }
}
