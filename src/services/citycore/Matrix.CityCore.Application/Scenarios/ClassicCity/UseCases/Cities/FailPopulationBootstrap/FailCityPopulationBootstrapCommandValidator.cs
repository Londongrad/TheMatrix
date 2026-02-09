using FluentValidation;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapCommandValidator
        : AbstractValidator<FailCityPopulationBootstrapCommand>
    {
        public FailCityPopulationBootstrapCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.OperationId)
               .NotEmpty();

            RuleFor(x => x.FailureCode)
               .NotEmpty()
               .MaximumLength(City.PopulationBootstrapFailureCodeMaxLength);
        }
    }
}
