using FluentValidation;
using KadirliApp.Application.Features.Transport.Commands;

namespace KadirliApp.Application.Features.Transport.Validators;

public class CreateIntercityRouteCommandValidator : AbstractValidator<CreateIntercityRouteCommand>
{
    public CreateIntercityRouteCommandValidator()
    {
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Company).MaximumLength(100);
    }
}
