using FluentValidation;

namespace Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed class InitializeCityEconomyCommandValidator
        : AbstractValidator<InitializeCityEconomyCommand>
    {
        public InitializeCityEconomyCommandValidator()
        {
            RuleFor(x => x.CityId).NotEmpty();
            RuleFor(x => x.SimulationKind).NotEmpty().MaximumLength(64);
            RuleFor(x => x.CreatedAtUtc).Must(x => x.Offset == TimeSpan.Zero);
        }
    }
}
