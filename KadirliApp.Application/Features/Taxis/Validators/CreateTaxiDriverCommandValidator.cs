using FluentValidation;
using KadirliApp.Application.Features.Taxis.Commands;

namespace KadirliApp.Application.Features.Taxis.Validators;

public class CreateTaxiDriverCommandValidator : AbstractValidator<CreateTaxiDriverCommand>
{
    public CreateTaxiDriverCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Plaka).MaximumLength(20);
    }
}
