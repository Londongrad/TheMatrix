using FluentValidation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap
{
    public sealed class CompleteCityPopulationBootstrapCommandValidator
        : AbstractValidator<CompleteCityPopulationBootstrapCommand>
    {
        public CompleteCityPopulationBootstrapCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.OperationId)
               .NotEmpty();
        }
    }
}
