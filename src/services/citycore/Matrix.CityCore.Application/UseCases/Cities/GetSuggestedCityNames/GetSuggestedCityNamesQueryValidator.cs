using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Cities.GetSuggestedCityNames
{
    public sealed class GetSuggestedCityNamesQueryValidator : AbstractValidator<GetSuggestedCityNamesQuery>
    {
        public GetSuggestedCityNamesQueryValidator()
        {
            RuleFor(x => x.Count)
               .InclusiveBetween(1, 25);
        }
    }
}