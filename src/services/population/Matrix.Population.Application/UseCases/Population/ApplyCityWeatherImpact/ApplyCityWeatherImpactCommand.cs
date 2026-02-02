using MediatR;

namespace Matrix.Population.Application.UseCases.Population.ApplyCityWeatherImpact
{
    public sealed record ApplyCityWeatherImpactCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset AtSimTimeUtc,
        DateTime OccurredOnUtc,
        WeatherImpactSnapshotInput PreviousState,
        WeatherImpactSnapshotInput CurrentState) : IRequest<ApplyCityWeatherImpactResult>;
}
