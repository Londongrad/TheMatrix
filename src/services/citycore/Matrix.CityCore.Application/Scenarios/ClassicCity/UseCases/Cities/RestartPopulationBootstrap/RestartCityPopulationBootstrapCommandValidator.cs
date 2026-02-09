using FluentValidation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed class RestartCityPopulationBootstrapCommandValidator
        : AbstractValidator<RestartCityPopulationBootstrapCommand>
    {
        public RestartCityPopulationBootstrapCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
