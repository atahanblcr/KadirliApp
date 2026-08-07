using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;

public class GetPowerOutagesQuery : IRequest<ApiResponse<List<PowerOutageDto>>>
{
}

public class GetPowerOutagesQueryHandler : IRequestHandler<GetPowerOutagesQuery, ApiResponse<List<PowerOutageDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPowerOutagesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<PowerOutageDto>>> Handle(GetPowerOutagesQuery request, CancellationToken cancellationToken)
    {
        var outages = await _unitOfWork.Repository<PowerOutage>().Query()
            .OrderByDescending(x => x.StartTime)
            .Select(x => new PowerOutageDto
            {
                Id = x.Id,
                Neighborhood = x.Neighborhood,
                NeighborhoodId = x.NeighborhoodId,
                AreaDetail = x.AreaDetail,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Reason = x.Reason,
                AnnouncementId = x.AnnouncementId
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<PowerOutageDto>>.SuccessResponse(outages);
    }
}
