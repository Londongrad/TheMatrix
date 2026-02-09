using Matrix.BuildingBlocks.Domain.Events;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Weather
{
    public sealed record ClimateProfileChangedDomainEvent(
        CityId CityId,
        WeatherClimateProfile PreviousProfile,
        WeatherClimateProfile CurrentProfile,
        SimTime AtSimTime) : DomainEventBase;
}
