using System;
using System.Threading;
using System.Threading.Tasks;
using KadirliApp.Application.Common.Interfaces;
using KadirliApp.Domain.Entities;
using MediatR;

namespace KadirliApp.Application.Features.Complaints.Commands;

public class CreateComplaintCommand : IRequest<Guid>
{
    public Guid? UserId { get; set; }
    public string? Type { get; set; }
    public string? RelatedModule { get; set; }
    public Guid? RelatedId { get; set; }
    public string Subject { get; set; } = default!;
    public string Message { get; set; } = default!;
}

public class CreateComplaintCommandHandler : IRequestHandler<CreateComplaintCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateComplaintCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateComplaintCommand request, CancellationToken cancellationToken)
    {
        var complaint = new Complaint
        {
            UserId = request.UserId,
            Type = request.Type,
            RelatedModule = request.RelatedModule,
            RelatedId = request.RelatedId,
            Subject = request.Subject,
            Message = request.Message,
            Status = "pending"
        };

        await _uow.Repository<Complaint>().AddAsync(complaint, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return complaint.Id;
    }
}
