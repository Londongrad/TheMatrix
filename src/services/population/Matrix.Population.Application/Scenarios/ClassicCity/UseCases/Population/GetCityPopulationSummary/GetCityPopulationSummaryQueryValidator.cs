using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary
{
    public sealed class GetCityPopulationSummaryQueryValidator : AbstractValidator<GetCityPopulationSummaryQuery>
    {
        public GetCityPopulationSummaryQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
