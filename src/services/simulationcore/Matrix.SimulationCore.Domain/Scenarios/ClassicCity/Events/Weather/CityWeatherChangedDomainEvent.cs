using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather
{
    public sealed record CityWeatherChangedDomainEvent(
        CityId CityId,
        WeatherState PreviousState,
        WeatherState CurrentState,
        SimTime AtSimTime) : DomainEventBase;
}
