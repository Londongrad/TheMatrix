using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Cities.RestartPopulationBootstrap
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
