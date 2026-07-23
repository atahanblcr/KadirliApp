using FluentValidation;
using KadirliApp.Application.Features.Ads.Dtos;

namespace KadirliApp.Application.Features.Ads.Validators;

public class CreateAdDtoValidator : AbstractValidator<CreateAdDto>
{
    public CreateAdDtoValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.ContactPhone).NotEmpty()
            .Matches(@"^(\+90|0)?5\d{9}$").WithMessage("Geçerli bir telefon giriniz");
    }
}
