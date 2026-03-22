using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions
{
    public sealed class GetCityEnvironmentalConditionsQueryValidator
        : AbstractValidator<GetCityEnvironmentalConditionsQuery>
    {
        public GetCityEnvironmentalConditionsQueryValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
