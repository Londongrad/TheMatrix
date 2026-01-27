using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Common;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;

namespace Matrix.CityCore.Domain.Events.Weather
{
    public sealed record ClimateProfileChangedDomainEvent(
        CityId CityId,
        WeatherClimateProfile PreviousProfile,
        WeatherClimateProfile CurrentProfile,
        SimTime AtSimTime) : DomainEventBase;
}