using Matrix.BuildingBlocks.Domain.Events;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Events.Weather
{
    public sealed record CityWeatherCreatedDomainEvent(
        CityId CityId,
        WeatherState InitialState,
        WeatherClimateProfile ClimateProfile,
        SimTime AtSimTime) : DomainEventBase;
}
