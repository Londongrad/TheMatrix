using FluentValidation;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap
{
    public sealed class CompleteCityEconomyBootstrapCommandValidator
        : AbstractValidator<CompleteCityEconomyBootstrapCommand>
    {
        public CompleteCityEconomyBootstrapCommandValidator()
        {
            RuleFor(x => x.CityId).NotEmpty();
            RuleFor(x => x.OperationId).NotEmpty();
        }
    }
}
