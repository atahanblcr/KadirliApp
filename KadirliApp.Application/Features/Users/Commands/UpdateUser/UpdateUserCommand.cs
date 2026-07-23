using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public UpdateUserDto Dto { get; set; } = default!;
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.Id, cancellationToken);
        if (user == null)
            return ApiResponse<bool>.FailureResponse("USER_NOT_FOUND", "Kullanıcı bulunamadı.");

        user.Phone = request.Dto.Phone;
        user.Email = request.Dto.Email;
        user.Username = request.Dto.Username;
        user.Age = request.Dto.Age;
        user.Role = (UserRole)request.Dto.Role;
        user.PrimaryNeighborhoodId = request.Dto.PrimaryNeighborhoodId;
        user.LocationType = request.Dto.LocationType;
        user.IsActive = request.Dto.IsActive;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
