using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.RecalculateCityEnvironmentalConditions
{
    public sealed class RecalculateCityEnvironmentalConditionsCommandValidator
        : AbstractValidator<RecalculateCityEnvironmentalConditionsCommand>
    {
        public RecalculateCityEnvironmentalConditionsCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.AtSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("AtSimTimeUtc must be in UTC.");

            RuleFor(x => x.Weather)
               .NotNull();
        }
    }
}
