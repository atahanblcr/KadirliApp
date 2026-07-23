using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using KadirliApp.Application.Common.Auditing;
using KadirliApp.Application.Common.Caching;
using MediatR;

namespace KadirliApp.Application.Features.Guide.Commands;

public class CreateGuideItemCommand : IRequest<Guid>, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Guide };

    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WorkingHours { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateGuideItemCommand : IRequest<bool>, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Guide };

    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WorkingHours { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public record DeleteGuideItemCommand(Guid Id) : IRequest<bool>, ICacheInvalidator, IAuditableCommand
{
    public IReadOnlyCollection<string> CacheGroupsToInvalidate => new[] { CacheGroups.Guide };

    public string AuditModule => "guide";
    public string AuditAction => "delete";
    public Guid? AuditAffectedId => Id;
    public string? AuditAffectedType => "GuideItem";
}

public class CreateGuideItemCommandHandler : IRequestHandler<CreateGuideItemCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateGuideItemCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateGuideItemCommand request, CancellationToken cancellationToken)
    {
        var item = new GuideItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Phone = request.Phone,
            Address = request.Address,
            Email = request.Email,
            WebsiteUrl = request.WebsiteUrl,
            WorkingHours = request.WorkingHours,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Description = request.Description,
            IsActive = request.IsActive
        };

        await _uow.Repository<GuideItem>().AddAsync(item, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}

public class UpdateGuideItemCommandHandler : IRequestHandler<UpdateGuideItemCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateGuideItemCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateGuideItemCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<GuideItem>();
        var item = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (item == null) return false;

        item.CategoryId = request.CategoryId;
        item.Name = request.Name;
        item.Phone = request.Phone;
        item.Address = request.Address;
        item.Email = request.Email;
        item.WebsiteUrl = request.WebsiteUrl;
        item.WorkingHours = request.WorkingHours;
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;
        item.Description = request.Description;
        item.IsActive = request.IsActive;

        repo.Update(item);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public class DeleteGuideItemCommandHandler : IRequestHandler<DeleteGuideItemCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteGuideItemCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(DeleteGuideItemCommand request, CancellationToken cancellationToken)
    {
        var repo = _uow.Repository<GuideItem>();
        var item = await repo.GetByIdAsync(request.Id, cancellationToken);
        if (item == null) return false;

        repo.Remove(item);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
