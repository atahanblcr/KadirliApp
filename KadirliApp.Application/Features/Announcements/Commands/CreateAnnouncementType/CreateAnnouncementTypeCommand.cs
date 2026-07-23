using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Application.Common.Models;
using KadirliApp.Domain.Entities;

namespace KadirliApp.Application.Features.Announcements.Commands.CreateAnnouncementType;

public class CreateAnnouncementTypeCommand : IRequest<ApiResponse<Guid>>
{
    public string Name { get; set; } = default!;
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class CreateAnnouncementTypeCommandHandler : IRequestHandler<CreateAnnouncementTypeCommand, ApiResponse<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnnouncementTypeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<Guid>> Handle(CreateAnnouncementTypeCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return ApiResponse<Guid>.FailureResponse("VALIDATION", "Tür adı boş olamaz.");

        var repo = _unitOfWork.Repository<AnnouncementType>();

        var exists = await repo.Query().AnyAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
            return ApiResponse<Guid>.FailureResponse("DUPLICATE", "Bu isimde bir duyuru türü zaten var.");

        var maxOrder = await repo.Query().Select(x => (int?)x.DisplayOrder).MaxAsync(cancellationToken) ?? 0;

        var type = new AnnouncementType
        {
            Name = name,
            Slug = Slugify(name),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "bullhorn" : request.Icon,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#6366f1" : request.Color,
            DisplayOrder = maxOrder + 1
        };

        await repo.AddAsync(type, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<Guid>.SuccessResponse(type.Id);
    }

    private static string Slugify(string value)
    {
        var map = new (char From, char To)[] { ('ç', 'c'), ('ğ', 'g'), ('ı', 'i'), ('ö', 'o'), ('ş', 's'), ('ü', 'u') };
        var lower = value.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            var c = ch;
            foreach (var (from, to) in map)
                if (c == from) { c = to; break; }

            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (c == ' ' || c == '-' || c == '_') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }
}
