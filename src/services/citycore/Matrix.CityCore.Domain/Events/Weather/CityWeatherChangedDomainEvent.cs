using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Domain.Events.Weather
{
    public sealed record CityWeatherChangedDomainEvent(
        CityId CityId,
        WeatherState PreviousState,
        WeatherState CurrentState,
        SimTime AtSimTime) : DomainEventBase;
}
