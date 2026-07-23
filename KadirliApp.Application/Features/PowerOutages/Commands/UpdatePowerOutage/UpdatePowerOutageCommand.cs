using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.UpdatePowerOutage;

public class UpdatePowerOutageCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public UpdatePowerOutageDto Dto { get; set; } = default!;
}

public class UpdatePowerOutageCommandHandler : IRequestHandler<UpdatePowerOutageCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePowerOutageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(UpdatePowerOutageCommand request, CancellationToken cancellationToken)
    {
        var outage = await _unitOfWork.Repository<PowerOutage>().GetByIdAsync(request.Id, cancellationToken);
        if (outage == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Elektrik kesintisi bulunamadı.");

        outage.Neighborhood = request.Dto.Neighborhood;
        outage.StartTime = request.Dto.StartTime;
        outage.EndTime = request.Dto.EndTime;
        outage.Reason = request.Dto.Reason;

        _unitOfWork.Repository<PowerOutage>().Update(outage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
