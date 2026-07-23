using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Queries.GetAnnouncementTypes;

public record AnnouncementTypeDto(Guid Id, string Name, string Slug, string? Icon, string? Color, int DisplayOrder);

public class GetAnnouncementTypesQuery : IRequest<ApiResponse<List<AnnouncementTypeDto>>>
{
}

public class GetAnnouncementTypesQueryHandler : IRequestHandler<GetAnnouncementTypesQuery, ApiResponse<List<AnnouncementTypeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAnnouncementTypesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<List<AnnouncementTypeDto>>> Handle(GetAnnouncementTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await _unitOfWork.Repository<AnnouncementType>().Query()
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new AnnouncementTypeDto(x.Id, x.Name, x.Slug, x.Icon, x.Color, x.DisplayOrder))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<AnnouncementTypeDto>>.SuccessResponse(types);
    }
}
