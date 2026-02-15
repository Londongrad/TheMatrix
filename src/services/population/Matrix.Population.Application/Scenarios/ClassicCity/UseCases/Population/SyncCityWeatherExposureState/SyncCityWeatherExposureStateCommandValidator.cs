using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityWeatherExposureState
{
    public sealed class SyncCityWeatherExposureStateCommandValidator : AbstractValidator<SyncCityWeatherExposureStateCommand>
    {
        public SyncCityWeatherExposureStateCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.IntegrationMessageId)
               .NotEmpty();

            RuleFor(x => x.ConsumerName)
               .NotEmpty();

            RuleFor(x => x.AtSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("AtSimTimeUtc must be in UTC (Offset=00:00).");

            RuleFor(x => x.OccurredOnUtc)
               .Must(x => x.Kind is DateTimeKind.Utc or DateTimeKind.Unspecified)
               .WithMessage("OccurredOnUtc must be UTC or unspecified.");

            RuleFor(x => x.CurrentState)
               .NotNull();
        }
    }
}
