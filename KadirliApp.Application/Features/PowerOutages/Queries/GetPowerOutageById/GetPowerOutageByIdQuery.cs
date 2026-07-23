using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutageById;

public class GetPowerOutageByIdQuery : IRequest<ApiResponse<PowerOutageDto>>
{
    public Guid Id { get; set; }
}

public class GetPowerOutageByIdQueryHandler : IRequestHandler<GetPowerOutageByIdQuery, ApiResponse<PowerOutageDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPowerOutageByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<PowerOutageDto>> Handle(GetPowerOutageByIdQuery request, CancellationToken cancellationToken)
    {
        var outage = await _unitOfWork.Repository<PowerOutage>().GetByIdAsync(request.Id, cancellationToken);
        if (outage == null)
            return ApiResponse<PowerOutageDto>.FailureResponse("NOT_FOUND", "Elektrik kesintisi bulunamadı.");

        return ApiResponse<PowerOutageDto>.SuccessResponse(new PowerOutageDto
        {
            Id = outage.Id,
            Neighborhood = outage.Neighborhood,
            StartTime = outage.StartTime,
            EndTime = outage.EndTime,
            Reason = outage.Reason
        });
    }
}
