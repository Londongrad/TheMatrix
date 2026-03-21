using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.RestartPopulationBootstrap
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
