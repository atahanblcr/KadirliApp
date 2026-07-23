using FluentValidation;
using KadirliApp.Application.Features.Transport.Commands;

namespace KadirliApp.Application.Features.Transport.Validators;

public class CreateIntracityRouteCommandValidator : AbstractValidator<CreateIntracityRouteCommand>
{
    public CreateIntracityRouteCommandValidator()
    {
        RuleFor(x => x.RouteNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RouteName).NotEmpty().MaximumLength(100);
    }
}
