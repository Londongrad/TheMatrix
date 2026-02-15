using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public sealed class SyncCityEnvironmentCommandValidator : AbstractValidator<SyncCityEnvironmentCommand>
    {
        public SyncCityEnvironmentCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.ClimateZone)
               .NotEmpty();

            RuleFor(x => x.Hemisphere)
               .NotEmpty();

            RuleFor(x => x.UtcOffsetMinutes)
               .InclusiveBetween(
                    from: -14 * 60,
                    to: 14 * 60);

            RuleFor(x => x.SyncedAtUtc)
               .Must(x => x is null || x.Value.Offset == TimeSpan.Zero)
               .WithMessage("SyncedAtUtc must be in UTC (Offset=00:00).");
        }
    }
}
