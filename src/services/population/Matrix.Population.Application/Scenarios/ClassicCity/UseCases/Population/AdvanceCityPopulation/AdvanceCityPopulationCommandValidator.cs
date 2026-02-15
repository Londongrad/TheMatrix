using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandValidator : AbstractValidator<AdvanceCityPopulationCommand>
    {
        public AdvanceCityPopulationCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.FromSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("FromSimTimeUtc must be in UTC (Offset=00:00).");

            RuleFor(x => x.ToSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("ToSimTimeUtc must be in UTC (Offset=00:00).");

            RuleFor(x => x.TickId)
               .GreaterThanOrEqualTo(0);

            RuleFor(x => x)
               .Must(x => DateOnly.FromDateTime(x.ToSimTimeUtc.UtcDateTime) >= DateOnly.FromDateTime(x.FromSimTimeUtc.UtcDateTime))
               .WithMessage("ToSimTimeUtc date cannot be earlier than FromSimTimeUtc date.");
        }
    }
}
