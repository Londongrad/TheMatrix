using Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.SyncCityWeatherExposureState
{
    public sealed record SyncCityWeatherExposureStateCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc,
        WeatherImpactSnapshotInput? PreviousState,
        WeatherImpactSnapshotInput CurrentState) : IRequest<SyncCityWeatherExposureStateResult>;
}
