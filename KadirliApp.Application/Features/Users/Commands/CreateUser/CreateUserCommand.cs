using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;
using KadirliApp.Domain.Enums;

namespace KadirliApp.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommand : IRequest<ApiResponse<Guid>>
{
    public CreateUserDto Dto { get; set; } = default!;
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Phone = request.Dto.Phone,
            Email = request.Dto.Email,
            Password = string.IsNullOrEmpty(request.Dto.Password)
                ? null
                : _passwordHasher.HashPassword(request.Dto.Password),
            Username = request.Dto.Username,
            Age = request.Dto.Age,
            Role = (UserRole)request.Dto.Role,
            PrimaryNeighborhoodId = request.Dto.PrimaryNeighborhoodId,
            LocationType = request.Dto.LocationType,
            IsActive = request.Dto.IsActive
        };

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(user.Id);
    }
}
