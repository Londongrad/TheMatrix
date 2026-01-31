using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Cities.CompletePopulationBootstrap
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
