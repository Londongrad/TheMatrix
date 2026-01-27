using Matrix.CityCore.Domain.Cities;
using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Domain.Events.Weather
{
    public sealed record CityWeatherCreatedDomainEvent(
        CityId CityId,
        WeatherState InitialState,
        WeatherClimateProfile ClimateProfile,
        SimTime AtSimTime) : DomainEventBase;
}