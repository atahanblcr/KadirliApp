using FluentValidation;
using KadirliApp.Application.Features.Ads.Dtos;

namespace KadirliApp.Application.Features.Ads.Validators;

public class UpdateAdDtoValidator : AbstractValidator<UpdateAdDto>
{
    public UpdateAdDtoValidator()
    {
        RuleFor(x => x.Title).MinimumLength(3).MaximumLength(200).When(x => x.Title != null);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.ContactPhone)
            .Matches(@"^(\+90|0)?5\d{9}$").WithMessage("Geçerli bir telefon giriniz")
            .When(x => x.ContactPhone != null);
    }
}
