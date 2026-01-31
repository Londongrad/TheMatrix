using FluentValidation;
using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.UseCases.Cities.FailPopulationBootstrap
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
