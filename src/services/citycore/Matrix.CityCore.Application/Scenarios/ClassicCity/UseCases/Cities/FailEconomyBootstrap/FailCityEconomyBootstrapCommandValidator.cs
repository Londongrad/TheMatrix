using FluentValidation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap
{
    public sealed class FailCityEconomyBootstrapCommandValidator
        : AbstractValidator<FailCityEconomyBootstrapCommand>
    {
        public FailCityEconomyBootstrapCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
            RuleFor(x => x.OperationId)
               .NotEmpty();
            RuleFor(x => x.FailureCode)
               .NotEmpty()
               .MaximumLength(128);
        }
    }
}
