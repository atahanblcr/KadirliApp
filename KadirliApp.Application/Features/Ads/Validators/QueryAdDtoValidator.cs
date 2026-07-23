using FluentValidation;
using KadirliApp.Application.Features.Ads.Dtos;

namespace KadirliApp.Application.Features.Ads.Validators;

public class QueryAdDtoValidator : AbstractValidator<QueryAdDto>
{
    public QueryAdDtoValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice)
            .When(x => x.MaxPrice.HasValue && x.MinPrice.HasValue)
            .WithMessage("Maksimum fiyat, minimum fiyattan küçük olamaz.");
    }
}
