using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    AdvanceCityEnvironmentalConditions
{
    public sealed class AdvanceCityEnvironmentalConditionsCommandValidator
        : AbstractValidator<AdvanceCityEnvironmentalConditionsCommand>
    {
        public AdvanceCityEnvironmentalConditionsCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.FromSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("FromSimTimeUtc must be in UTC.");

            RuleFor(x => x.ToSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("ToSimTimeUtc must be in UTC.");

            RuleFor(x => x)
               .Must(x => x.ToSimTimeUtc > x.FromSimTimeUtc)
               .WithMessage("ToSimTimeUtc must be greater than FromSimTimeUtc.");
        }
    }
}
