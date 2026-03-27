using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles
{
    public sealed class GetCityStockpilesQueryValidator : AbstractValidator<GetCityStockpilesQuery>
    {
        public GetCityStockpilesQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
