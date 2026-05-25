using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    SeedCityEnvironmentalConditions
{
    public sealed class SeedCityEnvironmentalConditionsCommandValidator
        : AbstractValidator<SeedCityEnvironmentalConditionsCommand>
    {
        public SeedCityEnvironmentalConditionsCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.CreatedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("CreatedAtUtc must be in UTC.");
        }
    }
}
