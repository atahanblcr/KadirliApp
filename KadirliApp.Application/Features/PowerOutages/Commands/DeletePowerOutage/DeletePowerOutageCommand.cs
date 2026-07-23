using KadirliApp.Application.Common.Auditing;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;

public class DeletePowerOutageCommand : IRequest<ApiResponse<bool>>, IAuditableCommand
{
    public string AuditModule => "power-outages";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "PowerOutage";

    public Guid Id { get; set; }
}

public class DeletePowerOutageCommandHandler : IRequestHandler<DeletePowerOutageCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeletePowerOutageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(DeletePowerOutageCommand request, CancellationToken cancellationToken)
    {
        var outage = await _unitOfWork.Repository<PowerOutage>().GetByIdAsync(request.Id, cancellationToken);
        if (outage == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Elektrik kesintisi bulunamadı.");

        _unitOfWork.Repository<PowerOutage>().Remove(outage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
