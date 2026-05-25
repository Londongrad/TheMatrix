using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    RecalculateCityEnvironmentalConditions
{
    public sealed record RecalculateCityEnvironmentalConditionsCommand(
        Guid CityId,
        DateTimeOffset AtSimTimeUtc,
        CityWeatherSystemInput Weather) : IRequest<RecalculateCityEnvironmentalConditionsResult>;
}
