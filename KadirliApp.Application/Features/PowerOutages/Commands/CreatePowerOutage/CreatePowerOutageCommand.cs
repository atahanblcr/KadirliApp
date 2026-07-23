using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.PowerOutages.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.CreatePowerOutage;

public class CreatePowerOutageCommand : IRequest<ApiResponse<Guid>>
{
    public CreatePowerOutageDto Dto { get; set; } = default!;
}

public class CreatePowerOutageCommandHandler : IRequestHandler<CreatePowerOutageCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePowerOutageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(CreatePowerOutageCommand request, CancellationToken cancellationToken)
    {
        var outage = new PowerOutage
        {
            Neighborhood = request.Dto.Neighborhood,
            StartTime = request.Dto.StartTime,
            EndTime = request.Dto.EndTime,
            Reason = request.Dto.Reason
        };

        await _unitOfWork.Repository<PowerOutage>().AddAsync(outage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(outage.Id);
    }
}
