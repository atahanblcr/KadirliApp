import os

app_path = '/Users/atahanblcr/desktop/dotnet/KadirliApp/KadirliApp.Application/Features'
api_path = '/Users/atahanblcr/desktop/dotnet/KadirliApp/KadirliApp.Api/Controllers'

def ensure_dir(path):
    os.makedirs(path, exist_ok=True)

# 1. Users
users_dto_path = os.path.join(app_path, 'Users', 'DTOs')
users_q_path = os.path.join(app_path, 'Users', 'Queries', 'GetUserProfile')
users_c_path = os.path.join(app_path, 'Users', 'Commands', 'UpdateUserProfile')
ensure_dir(users_dto_path)
ensure_dir(users_q_path)
ensure_dir(users_c_path)

with open(os.path.join(users_dto_path, 'UserProfileDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.Users.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = default!;
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}
''')

with open(os.path.join(users_dto_path, 'UpdateUserProfileDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.Users.DTOs;

public class UpdateUserProfileDto
{
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}
''')

with open(os.path.join(users_q_path, 'GetUserProfileQuery.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQuery : IRequest<ApiResponse<UserProfileDto>>
{
    public Guid UserId { get; set; }
}

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, ApiResponse<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetUserProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.FailureResponse("USER_NOT_FOUND", "Kullanıcı bulunamadı.");
        }

        var dto = new UserProfileDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Email = user.Email,
            Username = user.Username,
            ProfilePhotoUrl = user.ProfilePhotoUrl
        };

        return ApiResponse<UserProfileDto>.SuccessResponse(dto);
    }
}
''')

with open(os.path.join(users_c_path, 'UpdateUserProfileCommand.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Users.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommand : IRequest<ApiResponse<UserProfileDto>>
{
    public Guid UserId { get; set; }
    public UpdateUserProfileDto Dto { get; set; } = default!;
}

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, ApiResponse<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<UserProfileDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return ApiResponse<UserProfileDto>.FailureResponse("USER_NOT_FOUND", "Kullanıcı bulunamadı.");

        user.Email = request.Dto.Email ?? user.Email;
        user.Username = request.Dto.Username ?? user.Username;
        user.ProfilePhotoUrl = request.Dto.ProfilePhotoUrl ?? user.ProfilePhotoUrl;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new UserProfileDto
        {
            Id = user.Id,
            Phone = user.Phone,
            Email = user.Email,
            Username = user.Username,
            ProfilePhotoUrl = user.ProfilePhotoUrl
        };

        return ApiResponse<UserProfileDto>.SuccessResponse(dto);
    }
}
''')

# Users Controller
with open(os.path.join(api_path, 'UsersController.cs'), 'w') as f:
    f.write('''using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Application.Features.Users.Queries.GetUserProfile;
using KadirliApp.Application.Features.Users.Commands.UpdateUserProfile;
using KadirliApp.Application.Features.Users.DTOs;

namespace KadirliApp.Api.Controllers;

public class UsersController : ApiControllerBase
{
    [HttpGet("{id}/profile")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var response = await Sender.Send(new GetUserProfileQuery { UserId = id });
        return Success(response);
    }

    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateUserProfileDto dto)
    {
        var response = await Sender.Send(new UpdateUserProfileCommand { UserId = id, Dto = dto });
        return Success(response);
    }
}
''')

# 2. Announcements
announcements_dto_path = os.path.join(app_path, 'Announcements', 'DTOs')
announcements_q1_path = os.path.join(app_path, 'Announcements', 'Queries', 'GetAnnouncements')
announcements_q2_path = os.path.join(app_path, 'Announcements', 'Queries', 'GetAnnouncementById')
announcements_c1_path = os.path.join(app_path, 'Announcements', 'Commands', 'CreateAnnouncement')
announcements_c2_path = os.path.join(app_path, 'Announcements', 'Commands', 'UpdateAnnouncement')
announcements_c3_path = os.path.join(app_path, 'Announcements', 'Commands', 'DeleteAnnouncement')
ensure_dir(announcements_dto_path)
ensure_dir(announcements_q1_path)
ensure_dir(announcements_q2_path)
ensure_dir(announcements_c1_path)
ensure_dir(announcements_c2_path)
ensure_dir(announcements_c3_path)

with open(os.path.join(announcements_dto_path, 'AnnouncementDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.Announcements.DTOs;

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
''')

with open(os.path.join(announcements_dto_path, 'CreateAnnouncementDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.Announcements.DTOs;

public class CreateAnnouncementDto
{
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public Guid TypeId { get; set; }
}
''')

with open(os.path.join(announcements_dto_path, 'UpdateAnnouncementDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.Announcements.DTOs;

public class UpdateAnnouncementDto
{
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
}
''')

with open(os.path.join(announcements_q1_path, 'GetAnnouncementsQuery.cs'), 'w') as f:
    f.write('''using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;

public class GetAnnouncementsQuery : IRequest<ApiResponse<List<AnnouncementDto>>>
{
}

public class GetAnnouncementsQueryHandler : IRequestHandler<GetAnnouncementsQuery, ApiResponse<List<AnnouncementDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnnouncementsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<AnnouncementDto>>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var announcements = await _unitOfWork.Repository<Announcement>().Query()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AnnouncementDto
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Body,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<AnnouncementDto>>.SuccessResponse(announcements);
    }
}
''')

with open(os.path.join(announcements_q2_path, 'GetAnnouncementByIdQuery.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementById;

public class GetAnnouncementByIdQuery : IRequest<ApiResponse<AnnouncementDto>>
{
    public Guid Id { get; set; }
}

public class GetAnnouncementByIdQueryHandler : IRequestHandler<GetAnnouncementByIdQuery, ApiResponse<AnnouncementDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnnouncementByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AnnouncementDto>> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Repository<Announcement>().GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return ApiResponse<AnnouncementDto>.FailureResponse("NOT_FOUND", "Duyuru bulunamadı.");

        return ApiResponse<AnnouncementDto>.SuccessResponse(new AnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Body = announcement.Body,
            Status = announcement.Status,
            CreatedAt = announcement.CreatedAt
        });
    }
}
''')

with open(os.path.join(announcements_c1_path, 'CreateAnnouncementCommand.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncement;

public class CreateAnnouncementCommand : IRequest<ApiResponse<Guid>>
{
    public CreateAnnouncementDto Dto { get; set; } = default!;
}

public class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnnouncementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = new Announcement
        {
            Title = request.Dto.Title,
            Body = request.Dto.Body,
            TypeId = request.Dto.TypeId,
            Status = "active"
        };

        await _unitOfWork.Repository<Announcement>().AddAsync(announcement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(announcement.Id);
    }
}
''')

with open(os.path.join(announcements_c2_path, 'UpdateAnnouncementCommand.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Application.Features.Announcements.DTOs;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.UpdateAnnouncement;

public class UpdateAnnouncementCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
    public UpdateAnnouncementDto Dto { get; set; } = default!;
}

public class UpdateAnnouncementCommandHandler : IRequestHandler<UpdateAnnouncementCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAnnouncementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Repository<Announcement>().GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Duyuru bulunamadı.");

        announcement.Title = request.Dto.Title;
        announcement.Body = request.Dto.Body;

        _unitOfWork.Repository<Announcement>().Update(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
''')

with open(os.path.join(announcements_c3_path, 'DeleteAnnouncementCommand.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementCommand : IRequest<ApiResponse<bool>>
{
    public Guid Id { get; set; }
}

public class DeleteAnnouncementCommandHandler : IRequestHandler<DeleteAnnouncementCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAnnouncementCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _unitOfWork.Repository<Announcement>().GetByIdAsync(request.Id, cancellationToken);
        if (announcement == null)
            return ApiResponse<bool>.FailureResponse("NOT_FOUND", "Duyuru bulunamadı.");

        _unitOfWork.Repository<Announcement>().SoftRemove(announcement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResponse(true);
    }
}
''')

with open(os.path.join(api_path, 'AnnouncementsController.cs'), 'w') as f:
    f.write('''using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncements;
using KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementById;
using KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncement;
using KadirliApp.Application.Features.Announcements.Commands.UpdateAnnouncement;
using KadirliApp.Application.Features.Announcements.Commands.DeleteAnnouncement;
using KadirliApp.Application.Features.Announcements.DTOs;

namespace KadirliApp.Api.Controllers;

public class AnnouncementsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Success(await Sender.Send(new GetAnnouncementsQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetAnnouncementByIdQuery { Id = id }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        return Success(await Sender.Send(new CreateAnnouncementCommand { Dto = dto }));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnnouncementDto dto)
    {
        return Success(await Sender.Send(new UpdateAnnouncementCommand { Id = id, Dto = dto }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeleteAnnouncementCommand { Id = id }));
    }
}
''')

# 3. PowerOutages
po_dto_path = os.path.join(app_path, 'PowerOutages', 'DTOs')
po_q1_path = os.path.join(app_path, 'PowerOutages', 'Queries', 'GetPowerOutages')
po_q2_path = os.path.join(app_path, 'PowerOutages', 'Queries', 'GetPowerOutageById')
po_c1_path = os.path.join(app_path, 'PowerOutages', 'Commands', 'CreatePowerOutage')
po_c2_path = os.path.join(app_path, 'PowerOutages', 'Commands', 'UpdatePowerOutage')
po_c3_path = os.path.join(app_path, 'PowerOutages', 'Commands', 'DeletePowerOutage')
ensure_dir(po_dto_path)
ensure_dir(po_q1_path)
ensure_dir(po_q2_path)
ensure_dir(po_c1_path)
ensure_dir(po_c2_path)
ensure_dir(po_c3_path)

with open(os.path.join(po_dto_path, 'PowerOutageDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class PowerOutageDto
{
    public Guid Id { get; set; }
    public string? Neighborhood { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
}
''')

with open(os.path.join(po_dto_path, 'CreatePowerOutageDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class CreatePowerOutageDto
{
    public string? Neighborhood { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
}
''')

with open(os.path.join(po_dto_path, 'UpdatePowerOutageDto.cs'), 'w') as f:
    f.write('''using System;

namespace KadirliApp.Application.Features.PowerOutages.DTOs;

public class UpdatePowerOutageDto
{
    public string? Neighborhood { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Reason { get; set; }
}
''')

with open(os.path.join(po_q1_path, 'GetPowerOutagesQuery.cs'), 'w') as f:
    f.write('''using System.Collections.Generic;
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
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Reason = x.Reason
            })
            .ToListAsync(cancellationToken);

        return ApiResponse<List<PowerOutageDto>>.SuccessResponse(outages);
    }
}
''')

with open(os.path.join(po_q2_path, 'GetPowerOutageByIdQuery.cs'), 'w') as f:
    f.write('''using System;
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
''')

with open(os.path.join(po_c1_path, 'CreatePowerOutageCommand.cs'), 'w') as f:
    f.write('''using System;
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
''')

with open(os.path.join(po_c2_path, 'UpdatePowerOutageCommand.cs'), 'w') as f:
    f.write('''using System;
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
''')

with open(os.path.join(po_c3_path, 'DeletePowerOutageCommand.cs'), 'w') as f:
    f.write('''using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;

public class DeletePowerOutageCommand : IRequest<ApiResponse<bool>>
{
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
''')

with open(os.path.join(api_path, 'PowerOutagesController.cs'), 'w') as f:
    f.write('''using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutages;
using KadirliApp.Application.Features.PowerOutages.Queries.GetPowerOutageById;
using KadirliApp.Application.Features.PowerOutages.Commands.CreatePowerOutage;
using KadirliApp.Application.Features.PowerOutages.Commands.UpdatePowerOutage;
using KadirliApp.Application.Features.PowerOutages.Commands.DeletePowerOutage;
using KadirliApp.Application.Features.PowerOutages.DTOs;

namespace KadirliApp.Api.Controllers;

public class PowerOutagesController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Success(await Sender.Send(new GetPowerOutagesQuery()));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Success(await Sender.Send(new GetPowerOutageByIdQuery { Id = id }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePowerOutageDto dto)
    {
        return Success(await Sender.Send(new CreatePowerOutageCommand { Dto = dto }));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePowerOutageDto dto)
    {
        return Success(await Sender.Send(new UpdatePowerOutageCommand { Id = id, Dto = dto }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Success(await Sender.Send(new DeletePowerOutageCommand { Id = id }));
    }
}
''')

print("Code generated successfully.")
